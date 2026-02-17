<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useDocumentsStore } from '@/stores/documents'
import { useAuthStore } from '@/stores/auth'
import { referenceDataApi, cabinetsApi, foldersApi, documentsApi, filingPlansApi, permissionsApi, activityLogsApi, contentTypeDefinitionsApi, documentShortcutsApi, documentPasswordsApi, approvalsApi, privacyLevelsApi } from '@/api/client'
import TreeView from '@/components/common/TreeView.vue'
import FileList from '@/components/common/FileList.vue'
import FolderGrid from '@/components/common/FolderGrid.vue'
import ContextMenu from '@/components/common/ContextMenu.vue'
import SelectionToolbar from '@/components/common/SelectionToolbar.vue'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'
import MoveDocumentsModal from '@/components/documents/MoveDocumentsModal.vue'
import CreateShortcutModal from '@/components/documents/CreateShortcutModal.vue'
import ShareDocumentModal from '@/components/documents/ShareDocumentModal.vue'
import UploadDocumentModal from '@/components/documents/UploadDocumentModal.vue'
import ScanDocumentModal from '@/components/documents/ScanDocumentModal.vue'
import DocumentPasswordDialog from '@/components/documents/DocumentPasswordDialog.vue'
import DocumentAttachmentsPanel from '@/components/documents/DocumentAttachmentsPanel.vue'
import DocumentCommentsPanel from '@/components/documents/DocumentCommentsPanel.vue'
import DocumentLinksPanel from '@/components/documents/DocumentLinksPanel.vue'
import FolderContentTypeModal from '@/components/folders/FolderContentTypeModal.vue'
import { PermissionManagementModal } from '@/components/permissions'
import ApplyTemplateModal from '@/components/templates/ApplyTemplateModal.vue'
import type { MenuItem } from '@/components/common/ContextMenu.vue'
import type { Classification, Importance, DocumentType, Cabinet, Folder, TreeNode, FilingPlan, Permission, ActivityLog, Document, ApplyTemplateResult, ApprovalWorkflow, PrivacyLevel } from '@/types'
import { PermissionLevels } from '@/types'
import { UiSelect, UiConfirmDialog, UiModal, UiButton, DmsLoader } from '@/components/ui'
import type { ConfirmDialogType } from '@/components/ui/ConfirmDialog.vue'
import { useBulkOperations } from '@/composables/useBulkOperations'

const router = useRouter()
const route = useRoute()
const store = useDocumentsStore()
const authStore = useAuthStore()
const { isProcessing: isBulkProcessing, bulkDelete, bulkMove, bulkDownload } = useBulkOperations()

// Tree panel resizing
const treePanelWidth = ref(256)
const isResizing = ref(false)
const containerRef = ref<HTMLElement | null>(null)
const minTreeWidth = 180
const maxTreeWidth = 450

function onSplitterMouseDown(e: MouseEvent) {
  e.preventDefault()
  e.stopPropagation()
  isResizing.value = true

  const startX = e.clientX
  const startWidth = treePanelWidth.value

  function onMouseMove(e: MouseEvent) {
    const delta = e.clientX - startX
    const newWidth = startWidth + delta
    if (newWidth >= minTreeWidth && newWidth <= maxTreeWidth) {
      treePanelWidth.value = newWidth
    }
  }

  function onMouseUp() {
    isResizing.value = false
    document.removeEventListener('mousemove', onMouseMove)
    document.removeEventListener('mouseup', onMouseUp)
  }

  document.addEventListener('mousemove', onMouseMove)
  document.addEventListener('mouseup', onMouseUp)
}

// Selection state
const selectedDocumentIds = ref<string[]>([])

// Preview state
const showPreviewModal = ref(false)
const previewDocument = ref<Document | null>(null)

// Share state
const showShareModal = ref(false)
const shareDocument = ref<Document | null>(null)

// Password dialog state
const showPasswordDialog = ref(false)
const passwordDocument = ref<Document | null>(null)

// Bulk move modal
const showBulkMoveModal = ref(false)

// Single document move/copy
const showMoveModal = ref(false)
const showCopyModal = ref(false)
const moveDocument = ref<Document | null>(null)
const copyDocument = ref<Document | null>(null)
const clipboard = ref<{ action: 'copy' | 'cut'; document: Document } | null>(null)

// Attachments state
const showAttachmentsModal = ref(false)
const attachmentsDocument = ref<Document | null>(null)
const attachmentsCanEdit = ref(false)
const attachmentsPassword = ref<string | undefined>(undefined)
watch(showAttachmentsModal, (open) => {
  if (!open) {
    attachmentsDocument.value = null
    attachmentsPassword.value = undefined
  }
})

// Password validation dialog (for gating secured content)
const showPasswordValidation = ref(false)
const passwordValidationDocId = ref('')
const passwordValidationDocName = ref('')
const passwordValidationInput = ref('')
const passwordValidationError = ref('')
const passwordValidationLoading = ref(false)
const showPasswordText = ref(false)
const passwordValidationHint = ref<string | null>(null)
let passwordValidationCallback: ((password: string) => void) | null = null

function requestPasswordValidation(doc: Document, callback: (password: string) => void) {
  passwordValidationDocId.value = doc.id
  passwordValidationDocName.value = doc.name
  passwordValidationInput.value = ''
  passwordValidationError.value = ''
  showPasswordText.value = false
  passwordValidationHint.value = null
  passwordValidationCallback = callback
  showPasswordValidation.value = true
  // Load hint
  documentPasswordsApi.getHint(doc.id).then(r => {
    passwordValidationHint.value = r.data || null
  }).catch(() => {})
}

async function submitPasswordValidation() {
  if (!passwordValidationInput.value.trim()) {
    passwordValidationError.value = 'Please enter the password'
    return
  }
  passwordValidationLoading.value = true
  passwordValidationError.value = ''
  try {
    const response = await documentPasswordsApi.validatePassword(passwordValidationDocId.value, passwordValidationInput.value)
    if (response.data === true) {
      showPasswordValidation.value = false
      if (passwordValidationCallback) {
        passwordValidationCallback(passwordValidationInput.value)
        passwordValidationCallback = null
      }
    } else {
      passwordValidationError.value = 'Incorrect password. Please try again.'
    }
  } catch {
    passwordValidationError.value = 'Incorrect password. Please try again.'
  } finally {
    passwordValidationLoading.value = false
  }
}

function closePasswordValidation() {
  showPasswordValidation.value = false
  passwordValidationCallback = null
}

// Comments state
const showCommentsModal = ref(false)
const commentsDocument = ref<Document | null>(null)

// Links state
const showLinksModal = ref(false)
const linksDocument = ref<Document | null>(null)
const linksCanEdit = ref(false)

// Shortcut state
const showCreateShortcutModal = ref(false)
const shortcutDocument = ref<Document | null>(null)

// Modals
const showNewCabinetModal = ref(false)
const showEditCabinetModal = ref(false)
const showNewFolderModal = ref(false)
const showEditFolderModal = ref(false)
const showUploadModal = ref(false)
const showScanModal = ref(false)
const showContentTypeModal = ref(false)
const contentTypeModalTarget = ref<{ id: string; name: string; isCabinet: boolean } | null>(null)
const showApplyTemplateModal = ref(false)
const applyTemplateTarget = ref<{ id: string; name: string; isCabinet?: boolean } | null>(null)
const permissionModalTarget = ref<{ nodeType: 'Cabinet' | 'Folder' | 'Document'; nodeId: string; nodeName: string } | null>(null)
const showDeleteConfirm = ref(false)
const deleteTarget = ref<{ type: 'cabinet' | 'folder'; id: string; name: string } | null>(null)

// Context Menu
const showContextMenu = ref(false)
const contextMenuPosition = ref({ x: 0, y: 0 })
const contextMenuNode = ref<TreeNode | null>(null)
const contextMenuPermissionLevel = ref(0)

// Confirm Dialog State
const showConfirmDialog = ref(false)
const confirmDialogType = ref<ConfirmDialogType>('info')
const confirmDialogTitle = ref('')
const confirmDialogMessage = ref('')
const confirmDialogConfirmText = ref('OK')
const confirmDialogShowCancel = ref(false)
const confirmDialogCallback = ref<(() => void) | null>(null)

function showNotification(type: ConfirmDialogType, title: string, message: string) {
  confirmDialogType.value = type
  confirmDialogTitle.value = title
  confirmDialogMessage.value = message
  confirmDialogConfirmText.value = 'OK'
  confirmDialogShowCancel.value = false
  confirmDialogCallback.value = null
  showConfirmDialog.value = true
}

function showConfirmation(type: ConfirmDialogType, title: string, message: string, confirmText: string, callback: () => void) {
  confirmDialogType.value = type
  confirmDialogTitle.value = title
  confirmDialogMessage.value = message
  confirmDialogConfirmText.value = confirmText
  confirmDialogShowCancel.value = true
  confirmDialogCallback.value = callback
  showConfirmDialog.value = true
}

function handleConfirmDialogConfirm() {
  if (confirmDialogCallback.value) {
    confirmDialogCallback.value()
  }
  showConfirmDialog.value = false
}

// Additional Modals
const showFilingPlanModal = ref(false)
const showPermissionsModal = ref(false)
const showAuditModal = ref(false)
const showSettingsModal = ref(false)

// Filing Plan Data
const filingPlans = ref<FilingPlan[]>([])
const showNewFilingPlanModal = ref(false)
const newFilingPlan = ref({
  name: '',
  description: '',
  pattern: '',
  classificationId: '',
  documentTypeId: ''
})
const isLoadingFilingPlans = ref(false)

// Permissions Data
const permissions = ref<Permission[]>([])
const isLoadingPermissions = ref(false)

// Audit Trail Data
const auditLogs = ref<ActivityLog[]>([])
const isLoadingAudit = ref(false)

// View Mode
const viewMode = ref<'list' | 'grid'>('list')
const folderViewMode = ref<'list' | 'grid'>('grid')

// Folder Selection
const selectedFolderIds = ref<string[]>([])
const folderGridRef = ref<InstanceType<typeof FolderGrid> | null>(null)
const isBulkDeletingFolders = ref(false)

// Form data
const newCabinetName = ref('')
const newCabinetDescription = ref('')
const editCabinetData = ref({ id: '', name: '', description: '' })

const newFolderName = ref('')
const newFolderDescription = ref('')
const newFolderPrivacyLevelId = ref('')
const editFolderData = ref({ id: '', name: '', description: '', accessMode: 0, privacyLevelId: '' })

// Upload data
const uploadFiles = ref<File[]>([])
const uploadMetadata = ref({
  name: '',
  description: '',
  classificationId: '',
  importanceId: '',
  documentTypeId: ''
})
const isUploading = ref(false)
const isDragging = ref(false)

// Reference data
const classifications = ref<Classification[]>([])
const importances = ref<Importance[]>([])
const documentTypes = ref<DocumentType[]>([])
const privacyLevels = ref<PrivacyLevel[]>([])

// Privacy level modal
const showPrivacyLevelModal = ref(false)
const privacyLevelTarget = ref<{ id: string; name: string; currentPrivacyLevelId?: string }>({ id: '', name: '' })
const selectedPrivacyLevelId = ref('')

const privacyLevelOptions = computed(() => {
  const opts: { value: string; label: string }[] = [
    { value: '', label: 'None — visible to everyone' }
  ]
  for (const pl of privacyLevels.value.filter(p => p.isActive)) {
    opts.push({ value: pl.id, label: `${pl.name} (Level ${pl.level})` })
  }
  return opts
})

const breadcrumbs = computed(() => {
  const items: { name: string; type: string; index: number }[] = []
  if (store.currentCabinet) {
    items.push({ name: store.currentCabinet.name, type: 'cabinet', index: -1 })
  }
  store.folderPath.forEach((folder, i) => {
    items.push({ name: folder.name, type: 'folder', index: i })
  })
  return items
})

// Sync URL query params with current cabinet/folder selection
const isRestoringFromUrl = ref(false)

watch(
  () => ({ cabinet: store.currentCabinet?.id, folder: store.currentFolder?.id }),
  (newVal) => {
    if (isRestoringFromUrl.value) return
    const query: Record<string, string> = {}
    if (newVal.cabinet) query.cabinet = newVal.cabinet
    if (newVal.folder) query.folder = newVal.folder
    // Only replace if params actually changed
    if (route.query.cabinet !== query.cabinet || route.query.folder !== query.folder) {
      router.replace({ query })
    }
  }
)

onMounted(async () => {
  // Detect deep-link params early so the loading overlay shows immediately
  const cabinetId = route.query.cabinet as string | undefined
  const folderId = route.query.folder as string | undefined
  const hasDeepLink = !!(cabinetId || folderId)

  if (hasDeepLink) isRestoringFromUrl.value = true

  await Promise.all([
    store.loadCabinets(),
    loadReferenceData()
  ])

  // Handle deep-link query params (?cabinet=...&folder=...)
  if (folderId) {
    try {
      // If cabinetId is in URL, use it directly; otherwise fetch the folder to find it
      let resolvedCabinetId = cabinetId
      let folder: Folder | null = null

      if (!resolvedCabinetId) {
        const folderRes = await foldersApi.getById(folderId)
        folder = folderRes.data as Folder
        resolvedCabinetId = folder.cabinetId
      }

      const cabinet = store.cabinets.find(c => c.id === resolvedCabinetId)
      if (cabinet) {
        store.selectCabinet(cabinet)
        // Load tree, subfolders, and (if needed) folder details in parallel
        const [, subFoldersRes, folderRes] = await Promise.all([
          store.loadFolderTree(cabinet.id),
          foldersApi.getByParent(cabinet.id, folderId),
          !folder ? foldersApi.getById(folderId) : Promise.resolve(null)
        ])
        if (!folder) folder = folderRes!.data as Folder

        await store.selectFolder(folder)
        // Apply subfolders from the parallel fetch
        const sfData = subFoldersRes.data
        store.subFolders = Array.isArray(sfData) ? sfData : sfData.items ?? []
      }
    } catch {
      // Folder not found or no access — stay on default view
    } finally {
      isRestoringFromUrl.value = false
    }
  } else if (cabinetId) {
    try {
      const cabinet = store.cabinets.find(c => c.id === cabinetId)
      if (cabinet) {
        store.selectCabinet(cabinet)
        // Load tree and root subfolders in parallel
        const [, subFoldersRes] = await Promise.all([
          store.loadFolderTree(cabinet.id),
          foldersApi.getByParent(cabinet.id)
        ])
        const sfData = subFoldersRes.data
        store.subFolders = Array.isArray(sfData) ? sfData : sfData.items ?? []
      }
    } finally {
      isRestoringFromUrl.value = false
    }
  } else {
    // No query params — clear stale state so user sees a clean Explorer
    store.currentCabinet = null
    store.currentFolder = null
    store.folderPath = []
    store.documents = []
    store.subFolders = []
  }
})

async function loadReferenceData() {
  try {
    const [classRes, impRes, docTypeRes, plRes] = await Promise.all([
      referenceDataApi.getClassifications(),
      referenceDataApi.getImportances(),
      referenceDataApi.getDocumentTypes(),
      privacyLevelsApi.getAll()
    ])
    classifications.value = classRes.data
    importances.value = impRes.data
    documentTypes.value = docTypeRes.data
    privacyLevels.value = plRes.data
  } catch (err) {
  }
}

// Cabinet operations
async function handleCreateCabinet() {
  if (!newCabinetName.value.trim()) return
  try {
    await store.createCabinet({
      name: newCabinetName.value,
      description: newCabinetDescription.value
    })
    newCabinetName.value = ''
    newCabinetDescription.value = ''
    showNewCabinetModal.value = false
  } catch (err) {
  }
}

function openEditCabinet(cabinet: Cabinet) {
  editCabinetData.value = {
    id: cabinet.id,
    name: cabinet.name,
    description: cabinet.description || ''
  }
  showEditCabinetModal.value = true
}

async function handleUpdateCabinet() {
  if (!editCabinetData.value.name.trim()) return
  try {
    await cabinetsApi.update(editCabinetData.value.id, {
      name: editCabinetData.value.name,
      description: editCabinetData.value.description,
      breakInheritance: false
    })
    // Update tree node directly without reloading (preserves expansion state)
    store.updateNodeName(editCabinetData.value.id, editCabinetData.value.name)
    // Update currentCabinet if it's the one being renamed
    if (store.currentCabinet?.id === editCabinetData.value.id) {
      store.currentCabinet.name = editCabinetData.value.name
    }
    showEditCabinetModal.value = false
  } catch (err) {
  }
}

function confirmDeleteCabinet(cabinet: Cabinet) {
  deleteTarget.value = { type: 'cabinet', id: cabinet.id, name: cabinet.name }
  showDeleteConfirm.value = true
}

// Folder operations
async function handleCreateFolder() {
  if (!newFolderName.value.trim() || !store.currentCabinet) return
  try {
    await store.createFolder({
      cabinetId: store.currentCabinet.id,
      parentFolderId: store.currentFolder?.id,
      name: newFolderName.value,
      description: newFolderDescription.value,
      privacyLevelId: newFolderPrivacyLevelId.value || undefined
    })
    newFolderName.value = ''
    newFolderDescription.value = ''
    newFolderPrivacyLevelId.value = ''
    showNewFolderModal.value = false
    await store.loadFolderTree(store.currentCabinet.id)
  } catch (err) {
  }
}

function openEditFolder(folder: Folder) {
  editFolderData.value = {
    id: folder.id,
    name: folder.name,
    description: folder.description || '',
    accessMode: folder.accessMode ?? 0,
    privacyLevelId: folder.privacyLevelId || ''
  }
  showEditFolderModal.value = true
}

async function handleUpdateFolder() {
  if (!editFolderData.value.name.trim()) return
  try {
    await foldersApi.update(editFolderData.value.id, {
      name: editFolderData.value.name,
      description: editFolderData.value.description,
      breakInheritance: false,
      accessMode: editFolderData.value.accessMode,
      privacyLevelId: editFolderData.value.privacyLevelId || undefined
    })
    // Update tree node directly without reloading (preserves expansion state)
    store.updateNodeName(editFolderData.value.id, editFolderData.value.name)
    // Update currentFolder if it's the one being renamed
    if (store.currentFolder?.id === editFolderData.value.id) {
      store.currentFolder.name = editFolderData.value.name
    }
    showEditFolderModal.value = false
  } catch (err) {
  }
}

function confirmDeleteFolder(folder: Folder) {
  deleteTarget.value = { type: 'folder', id: folder.id, name: folder.name }
  showDeleteConfirm.value = true
}

async function handleDelete() {
  if (!deleteTarget.value) return
  try {
    if (deleteTarget.value.type === 'cabinet') {
      await cabinetsApi.delete(deleteTarget.value.id)
      await store.loadCabinets(true)
      if (store.currentCabinet?.id === deleteTarget.value.id) {
        store.currentCabinet = null
        store.currentFolder = null
        store.documents = []
      }
    } else {
      const deletedId = deleteTarget.value.id
      // Find the cabinet this folder belongs to (from tree or current state)
      const cabinetId = store.currentCabinet?.id || store.findCabinetForFolder(deletedId)
      await foldersApi.delete(deletedId)
      if (cabinetId) {
        await store.loadFolderTree(cabinetId)
        // If we deleted the current folder, navigate up; otherwise refresh subfolders
        if (store.currentFolder?.id === deletedId) {
          store.currentFolder = null
          store.documents = []
          await store.loadSubFolders(cabinetId, undefined)
        } else {
          await store.loadSubFolders(cabinetId, store.currentFolder?.id)
        }
      }
    }
    showDeleteConfirm.value = false
    deleteTarget.value = null
  } catch (err) {
  }
}

// Upload operations
function openUploadModal() {
  uploadFiles.value = []
  uploadMetadata.value = {
    name: '',
    description: '',
    classificationId: '',
    importanceId: '',
    documentTypeId: ''
  }
  showUploadModal.value = true
}

function handleDragOver(e: DragEvent) {
  e.preventDefault()
  isDragging.value = true
}

function handleDragLeave() {
  isDragging.value = false
}

function handleDrop(e: DragEvent) {
  e.preventDefault()
  isDragging.value = false
  if (e.dataTransfer?.files) {
    uploadFiles.value = Array.from(e.dataTransfer.files)
    if (uploadFiles.value.length === 1 && !uploadMetadata.value.name) {
      uploadMetadata.value.name = uploadFiles.value[0].name.replace(/\.[^/.]+$/, '')
    }
  }
}

function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    uploadFiles.value = Array.from(input.files)
    if (uploadFiles.value.length === 1 && !uploadMetadata.value.name) {
      uploadMetadata.value.name = uploadFiles.value[0].name.replace(/\.[^/.]+$/, '')
    }
  }
}

async function handleUpload() {
  if (!store.currentFolder || uploadFiles.value.length === 0) return
  isUploading.value = true

  try {
    for (const file of uploadFiles.value) {
      const formData = new FormData()
      formData.append('file', file)
      formData.append('folderId', store.currentFolder.id)
      formData.append('name', uploadMetadata.value.name || file.name.replace(/\.[^/.]+$/, ''))
      if (uploadMetadata.value.description) {
        formData.append('description', uploadMetadata.value.description)
      }
      if (uploadMetadata.value.classificationId) {
        formData.append('classificationId', uploadMetadata.value.classificationId)
      }
      if (uploadMetadata.value.importanceId) {
        formData.append('importanceId', uploadMetadata.value.importanceId)
      }
      if (uploadMetadata.value.documentTypeId) {
        formData.append('documentTypeId', uploadMetadata.value.documentTypeId)
      }

      await documentsApi.upload(formData)
    }

    showUploadModal.value = false
    await store.loadDocuments(store.currentFolder.id)
  } catch (err) {
  } finally {
    isUploading.value = false
  }
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

// Context Menu - Action code mapping for role-based permissions
const getActionCode = (menuId: string, isCabinet: boolean): string | null => {
  const prefix = isCabinet ? 'cabinet' : 'folder'
  const actionMap: Record<string, string> = {
    'open': `${prefix}.open`,
    'rename': `${prefix}.rename`,
    'add-folder': 'folder.create',
    'add-file': 'document.upload',
    'apply-template': 'folder.template.manage',
    'paste': 'folder.paste',
    'add-pattern': 'folder.pattern.add',
    'add-filing-plan': 'folder.filingplan.add',
    'settings': `${prefix}.settings`,
    'content-types': 'folder.contenttype.manage',
    'permissions': `${prefix}.permissions`,
    'delete': `${prefix}.delete`,
    'manage-workflow': `${prefix}.settings`,
    'audit': isCabinet ? 'audit.cabinet' : 'audit.folder',
    'dashboard': `${prefix}.dashboard`,
    'export': `${prefix}.export`
  }
  return actionMap[menuId] || null
}

// Check if user has role-based permission for an action
const hasRolePermission = (actionCode: string | null): boolean => {
  if (!actionCode) return true // No action code means no role restriction
  const hasIt = authStore.hasPermission(actionCode)
  if (!hasIt) {
  }
  return hasIt
}

const contextMenuItems = computed<MenuItem[]>(() => {
  if (!contextMenuNode.value) return []

  const isCabinet = contextMenuNode.value.type === 'cabinet'
  const hasClipboard = !!clipboard.value
  const pasteLabel = clipboard.value
    ? `Paste "${clipboard.value.document.name}" (${clipboard.value.action === 'cut' ? 'Move' : 'Copy'})`
    : 'Paste'

  // Check node-level permission levels
  const level = contextMenuPermissionLevel.value
  const canRead = (level & PermissionLevels.Read) === PermissionLevels.Read
  const canWrite = (level & PermissionLevels.Write) === PermissionLevels.Write
  const canDelete = (level & PermissionLevels.Delete) === PermissionLevels.Delete
  const canAdmin = (level & PermissionLevels.Admin) === PermissionLevels.Admin

  // Helper to check both node-level AND role-based permissions
  const canPerform = (menuId: string, nodePermission: boolean): boolean => {
    if (!nodePermission) return false
    const actionCode = getActionCode(menuId, isCabinet)
    return hasRolePermission(actionCode)
  }

  const items: MenuItem[] = []

  // Open action (Read + role permission)
  if (canPerform('open', canRead)) {
    items.push({ id: 'open', label: 'Open in new tab', icon: 'open' })
  }

  // Rename action (Write + role permission)
  if (canPerform('rename', canWrite)) {
    items.push({ id: 'rename', label: 'Rename', icon: 'rename' })
  }

  // Add actions (Write + role permissions)
  const addActionsAvailable = canPerform('add-folder', canWrite) ||
    canPerform('apply-template', canWrite) ||
    (!isCabinet && canPerform('add-file', canWrite))

  if (addActionsAvailable) {
    if (items.length > 0) items.push({ id: 'divider1', label: '', divider: true })
    if (canPerform('add-folder', canWrite)) {
      items.push({ id: 'add-folder', label: 'New folder', icon: 'folder-add' })
    }
    if (canPerform('apply-template', canWrite)) {
      items.push({ id: 'apply-template', label: 'New folder from template', icon: 'account_tree' })
    }
    if (!isCabinet) {
      if (canPerform('add-file', canWrite)) {
        items.push({ id: 'add-file', label: 'Upload file', icon: 'upload' })
      }
      if (hasClipboard && canPerform('paste', canWrite)) {
        items.push({ id: 'paste', label: pasteLabel, icon: 'paste' })
      }
    }
  }

  // Admin actions (patterns, filing plans, settings, content types, workflows)
  const adminActionsAvailable = (!isCabinet && (canPerform('add-pattern', canAdmin) || canPerform('add-filing-plan', canAdmin))) ||
    canPerform('settings', canAdmin) || canPerform('content-types', canAdmin) || canPerform('manage-workflow', canAdmin)

  if (adminActionsAvailable) {
    if (!isCabinet) {
      const patternFilingAvailable = canPerform('add-pattern', canAdmin) || canPerform('add-filing-plan', canAdmin)
      if (patternFilingAvailable && items.length > 0) {
        items.push({ id: 'divider2', label: '', divider: true })
      }
      if (canPerform('add-pattern', canAdmin)) {
        items.push({ id: 'add-pattern', label: 'Add pattern', icon: 'pattern' })
      }
      if (canPerform('add-filing-plan', canAdmin)) {
        items.push({ id: 'add-filing-plan', label: 'Add filing plan', icon: 'filing' })
      }
    }
    const settingsAvailable = canPerform('settings', canAdmin) || canPerform('content-types', canAdmin)
    if (settingsAvailable && items.length > 0) {
      items.push({ id: 'divider3', label: '', divider: true })
    }
    if (canPerform('settings', canAdmin)) {
      items.push({ id: 'settings', label: 'Settings', icon: 'settings' })
    }
    if (canPerform('content-types', canAdmin)) {
      items.push({ id: 'content-types', label: 'Manage Content Types', icon: 'category' })
    }
    if (canPerform('manage-workflow', canAdmin)) {
      items.push({ id: 'manage-workflow', label: 'Manage Workflow', icon: 'workflow' })
    }
    // Private folder toggle (folders only, admin permission)
    if (!isCabinet && canAdmin) {
      const isPrivate = contextMenuNode.value?.accessMode === 1
      items.push({
        id: 'toggle-private',
        label: isPrivate ? 'Disable Private Mode' : 'Enable Private Mode',
        icon: isPrivate ? 'lock-open' : 'lock'
      })
    }
    // Set privacy level (folders only, admin permission)
    if (!isCabinet && canAdmin && privacyLevels.value.length > 0) {
      items.push({
        id: 'set-privacy-level',
        label: 'Set Privacy Level',
        icon: 'shield'
      })
    }
  }

  // Permissions
  if (canPerform('permissions', canAdmin)) {
    if (items.length > 0) items.push({ id: 'divider5', label: '', divider: true })
    items.push({ id: 'permissions', label: 'Manage permissions', icon: 'permission' })
  }

  // Delete action
  if (canPerform('delete', canDelete)) {
    if (items.length > 0) items.push({ id: 'divider6', label: '', divider: true })
    items.push({ id: 'delete', label: 'Delete', icon: 'delete' })
  }

  // View actions (Read) and Audit (role permission only)
  const auditActionCode = getActionCode('audit', isCabinet)
  const hasAuditPermission = hasRolePermission(auditActionCode)
  const viewActionsAvailable = hasAuditPermission || canPerform('dashboard', canRead) || canPerform('export', canRead)
  if (viewActionsAvailable) {
    if (items.length > 0) items.push({ id: 'divider7', label: '', divider: true })
    if (hasAuditPermission) {
      items.push({ id: 'audit', label: 'Audit trail', icon: 'audit' })
    }
    if (canPerform('dashboard', canRead)) {
      items.push({ id: 'dashboard', label: 'Dashboard', icon: 'dashboard' })
    }
    if (canPerform('export', canRead)) {
      items.push({ id: 'export', label: 'Export', icon: 'export' })
    }
  }

  return items
})

async function handleTreeContextMenu(event: MouseEvent, node: TreeNode) {
  contextMenuNode.value = node
  contextMenuPosition.value = { x: event.clientX, y: event.clientY }

  // Fetch permission level for this node
  try {
    const nodeType = node.type === 'cabinet' ? 'Cabinet' : 'Folder'
    const response = await permissionsApi.getMyPermissionLevel(nodeType, node.id)
    contextMenuPermissionLevel.value = response.data.level || 0
  } catch (error) {
    contextMenuPermissionLevel.value = PermissionLevels.Read // Default to read-only
  }

  showContextMenu.value = true
}

function closeContextMenu() {
  showContextMenu.value = false
  contextMenuNode.value = null
}

async function handleContextMenuSelect(actionId: string) {
  if (!contextMenuNode.value) return

  const node = contextMenuNode.value
  const isCabinet = node.type === 'cabinet'

  switch (actionId) {
    case 'open':
      window.open(`/explorer?${isCabinet ? 'cabinet' : 'folder'}=${node.id}`, '_blank')
      break
    case 'rename':
      if (isCabinet) {
        editCabinetData.value = { id: node.id, name: node.name, description: '' }
        showEditCabinetModal.value = true
      } else {
        editFolderData.value = { id: node.id, name: node.name, description: '', accessMode: 0, privacyLevelId: '' }
        showEditFolderModal.value = true
      }
      break
    case 'add-folder':
      if (isCabinet) {
        store.selectCabinet({ id: node.id, name: node.name } as Cabinet)
        store.currentFolder = null
      } else {
        // Set the clicked folder as the parent for the new folder
        store.selectFolder({ id: node.id, name: node.name, parentFolderId: node.parentId } as Folder)
      }
      showNewFolderModal.value = true
      break
    case 'add-file':
      if (!isCabinet) {
        store.selectFolder({ id: node.id, name: node.name } as Folder)
        openUploadModal()
      }
      break
    case 'apply-template':
      applyTemplateTarget.value = { id: node.id, name: node.name, isCabinet }
      showApplyTemplateModal.value = true
      break
    case 'paste':
      if (!isCabinet && clipboard.value) {
        await handlePasteToFolder(node.id)
      }
      break
    case 'add-pattern':
    case 'add-filing-plan':
      if (!isCabinet) {
        await loadFilingPlans(node.id)
        showFilingPlanModal.value = true
      }
      break
    case 'settings':
      showSettingsModal.value = true
      break
    case 'content-types':
      contentTypeModalTarget.value = {
        id: node.id,
        name: node.name,
        isCabinet: isCabinet
      }
      showContentTypeModal.value = true
      break
    case 'manage-workflow':
      openWorkflowModal(node.id, node.name, isCabinet)
      break
    case 'permissions':
      permissionModalTarget.value = {
        nodeType: node.type === 'cabinet' ? 'Cabinet' : 'Folder',
        nodeId: node.id,
        nodeName: node.name
      }
      showPermissionsModal.value = true
      break
    case 'delete':
      deleteTarget.value = { type: isCabinet ? 'cabinet' : 'folder', id: node.id, name: node.name }
      showDeleteConfirm.value = true
      break
    case 'audit':
      await loadAuditTrail(node.type === 'cabinet' ? 'Cabinet' : 'Folder', node.id)
      showAuditModal.value = true
      break
    case 'dashboard':
      router.push('/dashboard')
      break
    case 'toggle-private':
      if (!isCabinet) {
        const newAccessMode = node.accessMode === 1 ? 0 : 1
        try {
          await foldersApi.update(node.id, {
            name: node.name,
            description: '',
            breakInheritance: false,
            accessMode: newAccessMode
          })
          // Update tree node accessMode in place
          node.accessMode = newAccessMode
          // Refresh folder data if it's the currently viewed folder
          if (store.currentFolder?.id === node.id) {
            store.currentFolder.accessMode = newAccessMode
          }
        } catch (err) {
          // silently fail
        }
      }
      break
    case 'set-privacy-level':
      if (!isCabinet) {
        privacyLevelTarget.value = { id: node.id, name: node.name }
        // Load current folder to get its privacy level
        try {
          const folderRes = await foldersApi.getById(node.id)
          privacyLevelTarget.value.currentPrivacyLevelId = folderRes.data.privacyLevelId
          selectedPrivacyLevelId.value = folderRes.data.privacyLevelId || ''
        } catch {
          selectedPrivacyLevelId.value = ''
        }
        showPrivacyLevelModal.value = true
      }
      break
    case 'export':
      // Export functionality - could download folder contents as zip
      break
  }

  closeContextMenu()
}

// Privacy Level Operations
async function savePrivacyLevel() {
  if (!privacyLevelTarget.value.id) return
  try {
    const folderRes = await foldersApi.getById(privacyLevelTarget.value.id)
    const folder = folderRes.data
    await foldersApi.update(privacyLevelTarget.value.id, {
      name: folder.name,
      description: folder.description || '',
      breakInheritance: folder.breakInheritance,
      accessMode: folder.accessMode ?? 0,
      privacyLevelId: selectedPrivacyLevelId.value || undefined
    })
    // Refresh tree and subfolder grid
    if (store.currentCabinet) {
      await store.loadFolderTree(store.currentCabinet.id)
    }
    if (store.currentFolder) {
      await store.loadSubFolders(store.currentFolder.cabinetId, store.currentFolder.id)
    }
    showPrivacyLevelModal.value = false
  } catch {
    // silently fail
  }
}

// Filing Plans Operations
async function loadFilingPlans(folderId: string) {
  isLoadingFilingPlans.value = true
  try {
    const response = await filingPlansApi.getByFolder(folderId)
    filingPlans.value = response.data
  } catch (err) {
  } finally {
    isLoadingFilingPlans.value = false
  }
}

async function createFilingPlan() {
  if (!contextMenuNode.value || !newFilingPlan.value.name) return
  try {
    await filingPlansApi.create({
      folderId: contextMenuNode.value.id,
      name: newFilingPlan.value.name,
      description: newFilingPlan.value.description || undefined,
      pattern: newFilingPlan.value.pattern || undefined,
      classificationId: newFilingPlan.value.classificationId || undefined,
      documentTypeId: newFilingPlan.value.documentTypeId || undefined
    })
    await loadFilingPlans(contextMenuNode.value.id)
    showNewFilingPlanModal.value = false
    newFilingPlan.value = { name: '', description: '', pattern: '', classificationId: '', documentTypeId: '' }
  } catch (err) {
  }
}

async function deleteFilingPlan(planId: string) {
  try {
    await filingPlansApi.delete(planId)
    if (contextMenuNode.value) {
      await loadFilingPlans(contextMenuNode.value.id)
    }
  } catch (err) {
  }
}

// Workflow Management
const showWorkflowModal = ref(false)
const workflowModalTarget = ref<{ id: string; name: string; isCabinet: boolean } | null>(null)
const allWorkflows = ref<ApprovalWorkflow[]>([])
const assignedWorkflowId = ref<string | null>(null)
const isLoadingWorkflows = ref(false)
const isSavingWorkflow = ref(false)

const workflowOptions = computed(() =>
  allWorkflows.value
    .filter(w => w.isActive)
    .map(wf => ({
      value: wf.id,
      label: wf.name,
      group: wf.triggerType === 'OnUpload' ? 'Auto-Trigger' : wf.triggerType === 'Manual' ? 'Manual' : 'Other'
    }))
)

const selectedWorkflow = computed(() =>
  allWorkflows.value.find(w => w.id === assignedWorkflowId.value)
)

async function openWorkflowModal(nodeId: string, nodeName: string, isCabinet: boolean) {
  workflowModalTarget.value = { id: nodeId, name: nodeName, isCabinet }
  isLoadingWorkflows.value = true
  showWorkflowModal.value = true
  try {
    const res = await approvalsApi.getWorkflows()
    const data = res.data
    allWorkflows.value = Array.isArray(data) ? data : data.items ?? []
    // Find if any workflow is already assigned to this folder
    const assigned = allWorkflows.value.find(w => w.folderId === nodeId)
    assignedWorkflowId.value = assigned?.id || null
  } catch {
    allWorkflows.value = []
    assignedWorkflowId.value = null
  } finally {
    isLoadingWorkflows.value = false
  }
}

async function saveWorkflowAssignment() {
  if (!workflowModalTarget.value) return
  isSavingWorkflow.value = true
  try {
    // If a workflow was previously assigned to this folder, clear it
    const previouslyAssigned = allWorkflows.value.find(w => w.folderId === workflowModalTarget.value!.id && w.id !== assignedWorkflowId.value)
    if (previouslyAssigned) {
      await approvalsApi.updateWorkflow(previouslyAssigned.id, {
        name: previouslyAssigned.name,
        description: previouslyAssigned.description || undefined,
        folderId: undefined,
        requiredApprovers: previouslyAssigned.requiredApprovers,
        isSequential: previouslyAssigned.isSequential,
        designerData: previouslyAssigned.designerData,
        triggerType: previouslyAssigned.triggerType,
        inheritToSubfolders: previouslyAssigned.inheritToSubfolders
      })
    }
    // Assign the selected workflow to this folder (or clear assignment)
    if (assignedWorkflowId.value) {
      const wf = allWorkflows.value.find(w => w.id === assignedWorkflowId.value)
      if (wf) {
        await approvalsApi.updateWorkflow(wf.id, {
          name: wf.name,
          description: wf.description || undefined,
          folderId: workflowModalTarget.value.id,
          requiredApprovers: wf.requiredApprovers,
          isSequential: wf.isSequential,
          designerData: wf.designerData,
          triggerType: wf.triggerType,
          inheritToSubfolders: wf.inheritToSubfolders
        })
      }
    }
    showWorkflowModal.value = false
  } catch {
    // silently fail
  } finally {
    isSavingWorkflow.value = false
  }
}

// Permissions Operations
async function loadPermissions(nodeType: string, nodeId: string) {
  isLoadingPermissions.value = true
  try {
    const response = await permissionsApi.getNodePermissions(nodeType, nodeId)
    permissions.value = response.data
  } catch (err) {
  } finally {
    isLoadingPermissions.value = false
  }
}

// Audit Trail Operations
async function loadAuditTrail(nodeType: string, nodeId: string) {
  isLoadingAudit.value = true
  try {
    const response = await activityLogsApi.getByNode(nodeType, nodeId)
    auditLogs.value = response.data.items
  } catch (err) {
  } finally {
    isLoadingAudit.value = false
  }
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleString()
}

function getPermissionLevelName(level: number): string {
  const levels: Record<number, string> = {
    1: 'Read',
    2: 'Write',
    4: 'Delete',
    8: 'Admin'
  }
  const names: string[] = []
  if (level & 8) names.push('Admin')
  else {
    if (level & 1) names.push('Read')
    if (level & 2) names.push('Write')
    if (level & 4) names.push('Delete')
  }
  return names.join(', ') || 'None'
}

const classificationOptions = computed(() => [
  { value: '', label: 'None' },
  ...classifications.value.map(c => ({ value: c.id, label: c.name }))
])

const importanceOptions = computed(() => [
  { value: '', label: 'None' },
  ...importances.value.map(i => ({ value: i.id, label: i.name }))
])

const documentTypeOptions = computed(() => [
  { value: '', label: 'None' },
  ...documentTypes.value.map(dt => ({ value: dt.id, label: dt.name }))
])

// Selection handling
function handleSelectionChange(ids: string[]) {
  selectedDocumentIds.value = ids
}

function clearSelection() {
  selectedDocumentIds.value = []
}

// Bulk operations
function handleBulkDelete() {
  if (selectedDocumentIds.value.length === 0) return

  showConfirmation(
    'danger',
    'Delete Documents',
    `Are you sure you want to delete ${selectedDocumentIds.value.length} document(s)? This action cannot be undone.`,
    'Delete',
    async () => {
      const result = await bulkDelete(selectedDocumentIds.value)
      if (result) {
        clearSelection()
        if (store.currentFolder) {
          await store.loadDocuments(store.currentFolder.id)
        }
      }
    }
  )
}

function openMoveModal() {
  showBulkMoveModal.value = true
}

async function handleBulkMove(targetFolderId: string) {
  if (selectedDocumentIds.value.length === 0) return

  // Validate content types for all selected documents
  const invalidDocs: string[] = []
  for (const docId of selectedDocumentIds.value) {
    const isValid = await validateContentTypeCompatibility(docId, targetFolderId)
    if (!isValid) {
      const doc = store.documents.find(d => d.id === docId)
      if (doc) invalidDocs.push(doc.name)
    }
  }

  if (invalidDocs.length > 0) {
    // Alert was already shown for the first invalid document
    return
  }

  const result = await bulkMove(selectedDocumentIds.value, targetFolderId)
  if (result) {
    clearSelection()
    showBulkMoveModal.value = false
    if (store.currentFolder) {
      await store.loadDocuments(store.currentFolder.id)
    }
  }
}

async function handleBulkDownload() {
  if (selectedDocumentIds.value.length === 0) return
  await bulkDownload(selectedDocumentIds.value)
}

// Folder selection and bulk operations
function handleFolderSelectionChange(ids: string[]) {
  selectedFolderIds.value = ids
}

function clearFolderSelection() {
  selectedFolderIds.value = []
  folderGridRef.value?.clearSelection()
}

function handleFolderDoubleClick(folder: Folder) {
  // Navigate into the folder
  handleSelectFolder(folder)
}

async function handleSelectCabinet(cabinet: Cabinet) {
  store.selectCabinet(cabinet)
  // Load subfolders at cabinet root level
  await store.loadSubFolders(cabinet.id, undefined)
  clearFolderSelection()
}

async function navigateBreadcrumb(item: { name: string; type: string; index: number }) {
  if (!store.currentCabinet) return
  if (item.type === 'cabinet') {
    // Go back to cabinet root
    store.navigateToBreadcrumb(-1)
    await store.loadSubFolders(store.currentCabinet.id, undefined)
  } else {
    // Go to a specific folder in the path
    store.navigateToBreadcrumb(item.index)
    if (store.currentFolder) {
      await store.loadSubFolders(store.currentCabinet.id, store.currentFolder.id)
    }
  }
  clearFolderSelection()
}

async function handleSelectFolder(folder: Folder) {
  await store.selectFolder(folder)
  // Load subfolders for this folder
  if (store.currentCabinet) {
    await store.loadSubFolders(store.currentCabinet.id, folder.id)
  }
  clearFolderSelection()
}

async function handleBulkDeleteFolders() {
  if (selectedFolderIds.value.length === 0) return

  showConfirmation(
    'danger',
    'Delete Folders',
    `Are you sure you want to delete ${selectedFolderIds.value.length} folder(s)? This will also delete all contents inside. This action cannot be undone.`,
    'Delete',
    async () => {
      isBulkDeletingFolders.value = true
      try {
        let successCount = 0
        let failCount = 0

        for (const folderId of selectedFolderIds.value) {
          try {
            await foldersApi.delete(folderId)
            successCount++
          } catch (err) {
            failCount++
          }
        }

        clearFolderSelection()

        // Refresh folder tree and subfolders
        if (store.currentCabinet) {
          await store.loadFolderTree(store.currentCabinet.id)
          await store.loadSubFolders(store.currentCabinet.id, store.currentFolder?.id)
        }

        if (failCount === 0) {
          showNotification('success', 'Folders Deleted', `Successfully deleted ${successCount} folder(s).`)
        } else {
          showNotification('warning', 'Partial Delete', `Deleted ${successCount} folder(s). ${failCount} failed (may contain documents or you lack permission).`)
        }
      } finally {
        isBulkDeletingFolders.value = false
      }
    }
  )
}

async function handleSingleDocumentMove(targetFolderId: string) {
  if (!moveDocument.value) return

  try {
    // Validate content type compatibility
    const isValid = await validateContentTypeCompatibility(moveDocument.value.id, targetFolderId)
    if (!isValid) {
      return // Notification is shown inside validateContentTypeCompatibility
    }

    const docName = moveDocument.value.name
    await documentsApi.move(moveDocument.value.id, targetFolderId)

    showMoveModal.value = false
    moveDocument.value = null

    if (store.currentFolder) {
      await store.loadDocuments(store.currentFolder.id)
    }

    showNotification('success', 'Document Moved', `Document "${docName}" has been moved successfully.`)
  } catch (err: any) {
    showNotification('danger', 'Move Failed', err.response?.data?.message || 'Failed to move document. Please try again.')
  }
}

// Document actions handling
function handleDocumentAction(action: string, document: Document) {
  switch (action) {
    case 'upload':
      openUploadModal()
      break
    case 'preview':
      previewDocument.value = document
      showPreviewModal.value = true
      break
    case 'download':
      downloadDocument(document)
      break
    case 'share':
      shareDocument.value = document
      showShareModal.value = true
      break
    case 'view-file':
    case 'view-properties':
      router.push(`/documents/${document.id}`)
      break
    case 'version-history':
      router.push(`/documents/${document.id}?tab=versions`)
      break
    case 'delete':
      showConfirmation(
        'danger',
        'Delete Document',
        `Are you sure you want to delete "${document.name}"? This action cannot be undone.`,
        'Delete',
        () => deleteDocument(document.id)
      )
      break
    case 'copy':
      clipboard.value = { action: 'copy', document }
      break
    case 'cut':
      clipboard.value = { action: 'cut', document }
      break
    case 'duplicate':
      handleDuplicate(document)
      break
    case 'move':
      moveDocument.value = document
      showMoveModal.value = true
      break
    case 'audit-trail':
      loadAuditTrail('Document', document.id)
      showAuditModal.value = true
      break
    case 'manage-permissions':
      permissionModalTarget.value = {
        nodeType: 'Document',
        nodeId: document.id,
        nodeName: document.name
      }
      showPermissionsModal.value = true
      break
    case 'manage-password':
      passwordDocument.value = document
      showPasswordDialog.value = true
      break
    case 'view-comments':
      commentsDocument.value = document
      showCommentsModal.value = true
      break
    case 'edit-comments':
      commentsDocument.value = document
      showCommentsModal.value = true
      break
    case 'view-links':
      linksDocument.value = document
      linksCanEdit.value = false
      showLinksModal.value = true
      break
    case 'link-files':
      linksDocument.value = document
      linksCanEdit.value = true
      showLinksModal.value = true
      break
    case 'view-attachments':
      if (document.hasPassword) {
        requestPasswordValidation(document, (password) => {
          attachmentsDocument.value = document
          attachmentsCanEdit.value = false
          attachmentsPassword.value = password
          showAttachmentsModal.value = true
        })
      } else {
        attachmentsDocument.value = document
        attachmentsCanEdit.value = false
        showAttachmentsModal.value = true
      }
      break
    case 'edit-attachments':
      if (document.hasPassword) {
        requestPasswordValidation(document, (password) => {
          attachmentsDocument.value = document
          attachmentsCanEdit.value = true
          attachmentsPassword.value = password
          showAttachmentsModal.value = true
        })
      } else {
        attachmentsDocument.value = document
        attachmentsCanEdit.value = true
        showAttachmentsModal.value = true
      }
      break
    case 'create-shortcut':
      shortcutDocument.value = document
      showCreateShortcutModal.value = true
      break
    case 'remove-shortcut':
      showConfirmation(
        'warning',
        'Remove Shortcut',
        `Remove this shortcut to "${document.name}"? The original document will not be affected.`,
        'Remove',
        async () => {
          if (!document.shortcutId) return
          try {
            await documentShortcutsApi.delete(document.shortcutId)
            if (store.currentFolder) {
              await store.loadDocuments(store.currentFolder.id)
            }
            showNotification('success', 'Shortcut Removed', `Shortcut to "${document.name}" has been removed.`)
          } catch (err: any) {
            showNotification('danger', 'Remove Failed', err.response?.data?.message || 'Failed to remove shortcut.')
          }
        }
      )
      break
    default:
  }
}

async function handleDuplicate(doc: Document) {
  if (!store.currentFolder) return
  try {
    await documentsApi.copy(doc.id, {
      targetFolderId: store.currentFolder.id,
      newName: `${doc.name} (Copy)`
    })
    await store.loadDocuments(store.currentFolder.id)
    showNotification('success', 'Document Duplicated', `Document "${doc.name}" has been duplicated successfully.`)
  } catch (err: any) {
    const errorMsg = err.response?.data?.message
      || err.response?.data?.title
      || err.response?.data?.errors?.join(', ')
      || JSON.stringify(err.response?.data)
      || 'Failed to duplicate document. Please try again.'
    showNotification('danger', 'Duplicate Failed', errorMsg)
  }
}

async function handlePasteToFolder(targetFolderId: string) {
  if (!clipboard.value) return

  try {
    // Validate content type compatibility
    const isValid = await validateContentTypeCompatibility(
      clipboard.value.document.id,
      targetFolderId
    )

    if (!isValid) {
      return // Notification is shown inside validateContentTypeCompatibility
    }

    const docName = clipboard.value.document.name
    const action = clipboard.value.action

    if (action === 'copy') {
      await documentsApi.copy(clipboard.value.document.id, {
        targetFolderId: targetFolderId
      })
      showNotification('success', 'Document Copied', `Document "${docName}" has been copied successfully.`)
    } else {
      await documentsApi.move(clipboard.value.document.id, targetFolderId)
      clipboard.value = null // Clear clipboard after cut
      showNotification('success', 'Document Moved', `Document "${docName}" has been moved successfully.`)
    }

    // Refresh the current folder view if we're viewing it
    if (store.currentFolder?.id === targetFolderId) {
      await store.loadDocuments(targetFolderId)
    }
  } catch (err: any) {
    showNotification('danger', 'Paste Failed', err.response?.data?.message || 'Failed to paste document. Please try again.')
  }
}

async function validateContentTypeCompatibility(documentId: string, targetFolderId: string): Promise<boolean> {
  try {
    // Get document's content type from metadata
    const metadataRes = await contentTypeDefinitionsApi.getDocumentMetadata(documentId)
    const documentMetadata = metadataRes.data || []

    // Get the content type ID from document metadata (if any)
    const documentContentTypeIds = new Set<string>()
    for (const meta of documentMetadata) {
      if (meta.contentTypeId) {
        documentContentTypeIds.add(meta.contentTypeId)
      }
    }

    // Get target folder's allowed content types
    const folderTypesRes = await contentTypeDefinitionsApi.getFolderContentTypes(targetFolderId)
    const folderContentTypes = folderTypesRes.data || []

    // If target folder has no content type restrictions, allow paste
    if (folderContentTypes.length === 0) {
      return true
    }

    // If document has no content type, check if folder requires one
    if (documentContentTypeIds.size === 0) {
      const hasRequired = folderContentTypes.some((ct: any) => ct.isRequired)
      if (hasRequired) {
        showNotification('warning', 'Content Type Required', 'Cannot paste: The target folder requires a content type, but the document has no content type assigned.')
        return false
      }
      return true
    }

    // Check if document's content type is allowed in the target folder
    const folderContentTypeIds = new Set(folderContentTypes.map((ct: any) => ct.contentTypeId))

    for (const docContentTypeId of documentContentTypeIds) {
      if (!folderContentTypeIds.has(docContentTypeId)) {
        const docTypeName = documentMetadata.find((m: any) => m.contentTypeId === docContentTypeId)?.contentTypeName || 'Unknown'
        const allowedTypes = folderContentTypes.map((ct: any) => ct.contentTypeName).join(', ') || 'None'
        showNotification('warning', 'Content Type Not Allowed', `Cannot paste: The document's content type "${docTypeName}" is not allowed in the target folder.\n\nAllowed content types: ${allowedTypes}`)
        return false
      }
    }

    return true
  } catch (err) {
    // If we can't validate, allow the operation and let the server handle it
    return true
  }
}

async function deleteDocument(docId: string) {
  try {
    await documentsApi.delete(docId)
    if (store.currentFolder) {
      await store.loadDocuments(store.currentFolder.id)
    }
  } catch (err) {
  }
}

async function downloadDocument(doc: Document) {
  try {
    const response = await documentsApi.download(doc.id)
    const blob = new Blob([response.data])
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = doc.name + (doc.extension || '')
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } catch (err) {
  }
}

// Handle template application result
async function handleTemplateApplied(result: ApplyTemplateResult) {
  // Close modal and reset state
  showApplyTemplateModal.value = false
  applyTemplateTarget.value = null

  // Show success notification
  if (result.success && result.foldersCreated > 0) {
    showNotification(
      'success',
      'Template Applied',
      `Successfully created ${result.foldersCreated} folder(s).`
    )
  } else if (result.errors && result.errors.length > 0) {
    showNotification(
      'warning',
      'Template Applied with Errors',
      `Created ${result.foldersCreated} folder(s). Errors: ${result.errors.join(', ')}`
    )
  }

  // Refresh the folder tree and subfolder grid to show newly created folders
  if (store.currentCabinet) {
    await store.loadFolderTree(store.currentCabinet.id)
    await store.loadSubFolders(store.currentCabinet.id, store.currentFolder?.id)
  }
}
</script>

<template>
  <div class="-m-6 flex h-[calc(100vh-4rem)]">
    <!-- Sidebar Tree -->
    <div
      class="flex-shrink-0 bg-white dark:bg-background-dark/50 border-r border-zinc-200 dark:border-border-dark flex flex-col"
      :style="{ width: `${treePanelWidth}px` }"
    >
      <div class="px-2 py-2 border-b border-zinc-200 dark:border-border-dark">
        <button
          @click="showNewCabinetModal = true"
          class="w-full py-2 px-3 bg-brand-gradient text-white text-sm rounded-lg font-medium shadow-md shadow-teal/20 flex items-center justify-center gap-2 hover:opacity-90 transition-opacity"
        >
          <span class="material-symbols-outlined text-lg">add_box</span>
          New Cabinet
        </button>
      </div>
      <div class="flex-1 overflow-y-auto">
        <TreeView
          :nodes="store.treeNodes"
          :selected-id="store.currentFolder?.id || store.currentCabinet?.id"
          @select-cabinet="handleSelectCabinet"
          @select-folder="handleSelectFolder"
          @expand-cabinet="store.loadFolderTree"
          @toggle-cabinet="store.toggleCabinetExpansion"
          @context-menu="handleTreeContextMenu"
        />
      </div>
    </div>

    <!-- Resizable Splitter -->
    <div
      @mousedown.stop="onSplitterMouseDown"
      class="w-1 flex-shrink-0 cursor-col-resize hover:w-1.5 hover:bg-teal/40 transition-all z-10 relative group"
      :class="isResizing ? 'w-1.5 bg-teal' : 'bg-zinc-200 dark:bg-border-dark'"
    >
      <!-- Grip dots - only visible on hover -->
      <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 flex flex-col gap-1 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none">
        <div class="w-1 h-1 rounded-full bg-teal"></div>
        <div class="w-1 h-1 rounded-full bg-teal"></div>
        <div class="w-1 h-1 rounded-full bg-teal"></div>
      </div>
    </div>

    <!-- Context Menu -->
    <ContextMenu
      v-if="showContextMenu"
      :items="contextMenuItems"
      :x="contextMenuPosition.x"
      :y="contextMenuPosition.y"
      @select="handleContextMenuSelect"
      @close="closeContextMenu"
    />

    <!-- Main Content -->
    <div class="flex-1 flex flex-col overflow-hidden">
      <!-- Header -->
      <div class="bg-white dark:bg-background-dark border-b border-zinc-200 dark:border-border-dark px-6 py-4">
        <!-- Title Row -->
        <div class="flex items-center justify-between mb-3">
          <h1 class="text-xl font-bold text-zinc-900 dark:text-zinc-100">Explorer</h1>
          <!-- Clipboard Indicator -->
          <div v-if="clipboard" class="flex items-center gap-2 px-3 py-1.5 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg">
            <span class="material-symbols-outlined text-amber-600 text-lg">{{ clipboard.action === 'cut' ? 'content_cut' : 'content_copy' }}</span>
            <span class="text-sm text-amber-700 dark:text-amber-400">
              {{ clipboard.action === 'cut' ? 'Cut' : 'Copied' }}: <strong>{{ clipboard.document.name }}</strong>
            </span>
            <button
              @click="clipboard = null"
              class="ml-2 p-0.5 text-amber-500 hover:text-amber-700 hover:bg-amber-100 rounded transition-colors"
              title="Clear clipboard"
            >
              <span class="material-symbols-outlined text-sm">close</span>
            </button>
          </div>
        </div>

        <!-- Breadcrumb & Actions Row -->
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <!-- Breadcrumb -->
          <div class="flex items-center gap-2 text-sm text-zinc-500 dark:text-zinc-400 min-w-0 flex-wrap">
            <button @click="store.currentCabinet = null; store.currentFolder = null; store.folderPath = []" class="hover:text-teal transition-colors flex items-center gap-1 flex-shrink-0">
              <span class="material-symbols-outlined text-lg">home</span>
            </button>
            <template v-for="(item, idx) in breadcrumbs" :key="idx">
              <span class="material-symbols-outlined text-sm text-zinc-400 flex-shrink-0">chevron_right</span>
              <button
                v-if="idx < breadcrumbs.length - 1"
                @click="navigateBreadcrumb(item)"
                class="hover:text-teal cursor-pointer transition-colors truncate max-w-[150px]"
                :title="item.name"
              >{{ item.name }}</button>
              <span
                v-else
                class="font-semibold text-zinc-900 dark:text-zinc-100 truncate max-w-[200px]"
                :title="item.name"
              >{{ item.name }}</span>
            </template>
          </div>

          <!-- Actions Toolbar -->
          <div class="flex items-center gap-3">
            <!-- Icon Button Group (Delete & Edit) -->
            <div v-if="store.currentCabinet" class="flex items-center bg-zinc-100 dark:bg-surface-dark rounded-lg p-1">
              <!-- Delete Button -->
              <button
                v-if="!store.currentFolder"
                @click="confirmDeleteCabinet(store.currentCabinet)"
                class="toolbar-btn group"
                title="Delete Cabinet"
              >
                <span class="material-symbols-outlined text-[20px] text-zinc-500 group-hover:text-red-500 transition-colors">delete</span>
              </button>
              <button
                v-if="store.currentFolder"
                @click="confirmDeleteFolder(store.currentFolder)"
                class="toolbar-btn group"
                title="Delete Folder"
              >
                <span class="material-symbols-outlined text-[20px] text-zinc-500 group-hover:text-red-500 transition-colors">delete</span>
              </button>

              <!-- Edit Button -->
              <button
                v-if="!store.currentFolder"
                @click="openEditCabinet(store.currentCabinet)"
                class="toolbar-btn group"
                title="Edit Properties"
              >
                <span class="material-symbols-outlined text-[20px] text-zinc-500 group-hover:text-teal transition-colors">settings</span>
              </button>
              <button
                v-if="store.currentFolder"
                @click="openEditFolder(store.currentFolder)"
                class="toolbar-btn group"
                title="Edit Properties"
              >
                <span class="material-symbols-outlined text-[20px] text-zinc-500 group-hover:text-teal transition-colors">settings</span>
              </button>
            </div>

            <!-- New Folder Button -->
            <button
              v-if="store.currentCabinet"
              @click="showNewFolderModal = true"
              class="flex items-center gap-2 px-3 py-2 text-zinc-600 dark:text-zinc-300 border border-zinc-300 dark:border-border-dark hover:border-teal hover:text-teal rounded-lg text-sm font-medium transition-colors"
            >
              <span class="material-symbols-outlined text-[20px]">create_new_folder</span>
              <span class="hidden sm:inline">New Folder</span>
            </button>

            <!-- Scan Button -->
            <button
              v-if="store.currentFolder"
              @click="showScanModal = true"
              class="flex items-center gap-2 px-3 py-2 text-zinc-600 dark:text-zinc-300 border border-zinc-300 dark:border-border-dark hover:border-teal hover:text-teal rounded-lg text-sm font-medium transition-colors"
            >
              <span class="material-symbols-outlined text-[20px]">document_scanner</span>
              <span class="hidden sm:inline">Scan</span>
            </button>

            <!-- Upload Button -->
            <button
              v-if="store.currentFolder"
              @click="openUploadModal"
              class="flex items-center gap-2 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg text-sm font-medium shadow-sm shadow-teal/25 transition-all"
            >
              <span class="material-symbols-outlined text-[20px]">upload</span>
              Upload
            </button>
          </div>
        </div>
      </div>

      <!-- Content Area -->
      <div class="flex-1 overflow-y-auto p-6">
        <!-- Loading overlay during deep-link URL restoration (prevents "dancing") -->
        <div v-if="isRestoringFromUrl" class="flex flex-col items-center justify-center h-full">
          <DmsLoader size="md" text="Loading..." />
        </div>
        <!-- Empty state when nothing selected -->
        <div v-else-if="!store.currentCabinet && !store.currentFolder" class="flex flex-col items-center justify-center h-full text-zinc-500">
          <span class="material-symbols-outlined text-7xl text-zinc-300 mb-4">folder_open</span>
          <p class="text-lg font-medium text-zinc-700 dark:text-zinc-300">Select a cabinet or folder to view contents</p>
          <p class="text-sm text-zinc-400 mt-1">Or create a new cabinet to get started</p>
        </div>

        <!-- Content when cabinet or folder is selected -->
        <div v-else class="space-y-6">
          <!-- Subfolders Section -->
          <div v-if="store.subFolders.length > 0 || store.isLoadingSubFolders">
            <!-- Subfolders Header -->
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-zinc-400">folder</span>
                <h3 class="text-sm font-semibold text-zinc-700 dark:text-zinc-300">Folders</h3>
                <span class="text-xs text-zinc-400">({{ store.subFolders.length }})</span>
              </div>
              <!-- View Mode Toggle -->
              <div class="flex items-center bg-zinc-100 dark:bg-surface-dark rounded-lg p-0.5">
                <button
                  @click="folderViewMode = 'grid'"
                  class="p-1.5 rounded-md transition-colors"
                  :class="folderViewMode === 'grid' ? 'bg-white dark:bg-border-dark shadow-sm text-teal' : 'text-zinc-400 hover:text-zinc-600'"
                  title="Grid view"
                >
                  <span class="material-symbols-outlined text-lg">grid_view</span>
                </button>
                <button
                  @click="folderViewMode = 'list'"
                  class="p-1.5 rounded-md transition-colors"
                  :class="folderViewMode === 'list' ? 'bg-white dark:bg-border-dark shadow-sm text-teal' : 'text-zinc-400 hover:text-zinc-600'"
                  title="List view"
                >
                  <span class="material-symbols-outlined text-lg">view_list</span>
                </button>
              </div>
            </div>

            <!-- Folder Grid/List -->
            <FolderGrid
              ref="folderGridRef"
              :folders="store.subFolders"
              :is-loading="store.isLoadingSubFolders"
              :view-mode="folderViewMode"
              :selectable="true"
              @folder-dblclick="handleFolderDoubleClick"
              @selection-change="handleFolderSelectionChange"
              @context-menu="(e, f) => handleTreeContextMenu(e, { id: f.id, name: f.name, type: 'folder', children: [], isExpanded: false, accessMode: f.accessMode })"
            />
          </div>

          <!-- Documents Section -->
          <div v-if="store.currentFolder">
            <div class="flex items-center gap-2 mb-3" v-if="store.subFolders.length > 0">
              <span class="material-symbols-outlined text-zinc-400">description</span>
              <h3 class="text-sm font-semibold text-zinc-700 dark:text-zinc-300">Documents</h3>
              <span class="text-xs text-zinc-400">({{ store.documents.length }})</span>
            </div>
            <FileList
              :documents="store.documents"
              :is-loading="store.isLoading"
              :selectable="true"
              @selection-change="handleSelectionChange"
              @document-action="handleDocumentAction"
            />
          </div>

          <!-- Message when viewing cabinet root (no documents) -->
          <div v-else-if="!store.currentFolder && store.subFolders.length === 0 && !store.isLoadingSubFolders" class="text-center py-12 text-zinc-400">
            <span class="material-symbols-outlined text-5xl mb-2 opacity-50">folder_off</span>
            <p class="text-sm">This cabinet is empty</p>
            <p class="text-xs mt-1">Create a folder to get started</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Selection Toolbar (Documents) -->
    <SelectionToolbar
      v-if="selectedFolderIds.length === 0"
      :selected-count="selectedDocumentIds.length"
      :is-processing="isBulkProcessing"
      @delete-selected="handleBulkDelete"
      @move-selected="openMoveModal"
      @download-selected="handleBulkDownload"
      @clear-selection="clearSelection"
    />

    <!-- Selection Toolbar (Folders) -->
    <Transition name="slide-up">
      <div
        v-if="selectedFolderIds.length > 0"
        class="fixed bottom-6 left-1/2 -translate-x-1/2 bg-zinc-900 text-white rounded-lg shadow-2xl px-6 py-4 flex items-center gap-6 z-50"
      >
        <!-- Selection Count -->
        <div class="flex items-center gap-2">
          <span class="material-symbols-outlined text-teal">check_circle</span>
          <span class="font-semibold">{{ selectedFolderIds.length }} folder{{ selectedFolderIds.length === 1 ? '' : 's' }} selected</span>
        </div>

        <!-- Divider -->
        <div class="w-px h-8 bg-zinc-700"></div>

        <!-- Actions -->
        <div class="flex items-center gap-1">
          <!-- Delete -->
          <button
            @click="handleBulkDeleteFolders"
            :disabled="isBulkDeletingFolders"
            class="flex items-center gap-2 px-4 py-2 hover:bg-red-600 rounded-lg transition-colors disabled:opacity-50"
            title="Delete selected folders"
          >
            <span class="material-symbols-outlined text-[20px]">delete</span>
            <span class="text-sm font-medium">Delete</span>
          </button>
        </div>

        <!-- Divider -->
        <div class="w-px h-8 bg-zinc-700"></div>

        <!-- Clear Selection -->
        <button
          @click="clearFolderSelection"
          :disabled="isBulkDeletingFolders"
          class="p-2 hover:bg-zinc-800 rounded-lg transition-colors disabled:opacity-50"
          title="Clear selection"
        >
          <span class="material-symbols-outlined">close</span>
        </button>

        <!-- Processing Indicator -->
        <div v-if="isBulkDeletingFolders" class="absolute inset-0 bg-zinc-900/80 rounded-lg flex items-center justify-center">
          <div class="flex items-center gap-3">
            <svg class="animate-spin w-5 h-5 text-teal" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            <span class="font-medium">Deleting...</span>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Document Viewer -->
    <DocumentViewer
      v-model="showPreviewModal"
      :document="previewDocument"
    />

    <!-- Confirm Dialog -->
    <UiConfirmDialog
      v-model="showConfirmDialog"
      :type="confirmDialogType"
      :title="confirmDialogTitle"
      :message="confirmDialogMessage"
      :confirm-text="confirmDialogConfirmText"
      :show-cancel="confirmDialogShowCancel"
      @confirm="handleConfirmDialogConfirm"
    />

    <!-- Single Document Move Modal -->
    <MoveDocumentsModal
      v-if="showMoveModal"
      v-model="showMoveModal"
      :document-count="1"
      @confirm="handleSingleDocumentMove"
    />

    <!-- Bulk Move Modal -->
    <MoveDocumentsModal
      v-if="showBulkMoveModal"
      v-model="showBulkMoveModal"
      :document-count="selectedDocumentIds.length"
      @confirm="handleBulkMove"
    />

    <!-- Create Shortcut Modal -->
    <CreateShortcutModal
      v-if="shortcutDocument"
      v-model="showCreateShortcutModal"
      :document="shortcutDocument"
      @created="shortcutDocument = null; showNotification('success', 'Shortcut Created', 'Document shortcut has been created successfully.')"
    />

    <!-- Share Document Modal -->
    <ShareDocumentModal
      v-if="showShareModal && shareDocument"
      :document="shareDocument"
      @close="showShareModal = false; shareDocument = null"
      @shared="showShareModal = false; shareDocument = null"
    />

    <!-- Document Password Dialog -->
    <DocumentPasswordDialog
      v-if="showPasswordDialog && passwordDocument"
      :document-id="passwordDocument.id"
      :document-name="passwordDocument.name"
      @close="showPasswordDialog = false; passwordDocument = null"
      @updated="(has: boolean) => { const doc = store.documents.find(d => d.id === passwordDocument?.id); if (doc) doc.hasPassword = has; showPasswordDialog = false; passwordDocument = null }"
    />

    <!-- Password Validation Dialog (for gating secured content) -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showPasswordValidation" class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50 p-8">
          <Transition
            enter-active-class="duration-200 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="duration-150 ease-in"
            leave-from-class="opacity-100 scale-100 translate-y-0"
            leave-to-class="opacity-0 scale-95 translate-y-4"
          >
            <div v-if="showPasswordValidation" class="w-full max-w-md">
              <form @submit.prevent="submitPasswordValidation" class="bg-white dark:bg-background-dark rounded-lg shadow-2xl p-8">
                <!-- Lock Icon -->
                <div class="flex justify-center mb-6">
                  <div class="w-20 h-20 rounded-lg bg-amber-500/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-5xl text-amber-500">lock</span>
                  </div>
                </div>

                <!-- Title -->
                <h2 class="text-xl font-bold text-center text-zinc-900 dark:text-white mb-2">
                  Password Protected
                </h2>
                <p class="text-sm text-center text-zinc-500 dark:text-zinc-400 mb-6">
                  This document requires a password to view
                </p>

                <!-- Hint -->
                <div v-if="passwordValidationHint" class="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
                  <div class="flex items-start gap-2">
                    <span class="material-symbols-outlined text-blue-500 text-lg mt-0.5">lightbulb</span>
                    <div>
                      <p class="text-xs font-medium text-blue-700 dark:text-blue-300 uppercase tracking-wide">Hint</p>
                      <p class="text-sm text-blue-600 dark:text-blue-400">{{ passwordValidationHint }}</p>
                    </div>
                  </div>
                </div>

                <!-- Password Input -->
                <div class="mb-4">
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">
                    Enter Password
                  </label>
                  <div class="relative">
                    <input
                      v-model="passwordValidationInput"
                      :type="showPasswordText ? 'text' : 'password'"
                      class="w-full px-4 py-3 pr-12 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
                      placeholder="Enter document password..."
                      autofocus
                    />
                    <button
                      type="button"
                      @click="showPasswordText = !showPasswordText"
                      class="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors"
                    >
                      <span class="material-symbols-outlined text-xl">{{ showPasswordText ? 'visibility_off' : 'visibility' }}</span>
                    </button>
                  </div>
                </div>

                <!-- Error Message -->
                <div v-if="passwordValidationError" class="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                  <div class="flex items-center gap-2">
                    <span class="material-symbols-outlined text-red-500 text-lg">error</span>
                    <p class="text-sm text-red-600 dark:text-red-400">{{ passwordValidationError }}</p>
                  </div>
                </div>

                <!-- Actions -->
                <div class="flex gap-3">
                  <button
                    type="button"
                    @click="closePasswordValidation"
                    class="flex-1 py-3 text-zinc-600 dark:text-zinc-400 font-medium rounded-lg hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    :disabled="!passwordValidationInput.trim() || passwordValidationLoading"
                    class="flex-1 py-3 bg-teal text-white font-medium rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                  >
                    <svg v-if="passwordValidationLoading" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                    </svg>
                    {{ passwordValidationLoading ? 'Verifying...' : 'Unlock' }}
                  </button>
                </div>
              </form>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- Attachments Modal -->
    <DocumentAttachmentsPanel
      v-if="attachmentsDocument"
      v-model="showAttachmentsModal"
      :document-id="attachmentsDocument.id"
      :document-name="attachmentsDocument.name"
      :can-edit="attachmentsCanEdit"
      :password="attachmentsPassword"
    />

    <!-- Comments Modal -->
    <UiModal
      v-model="showCommentsModal"
      size="lg"
      :show-close="false"
      @close="commentsDocument = null"
    >
      <template #header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-lg flex items-center justify-center">
              <span class="material-symbols-outlined text-white text-xl">chat</span>
            </div>
            <div>
              <h3 class="text-lg font-semibold text-white">Comments</h3>
              <p class="text-sm text-white/60">{{ commentsDocument?.name || 'Document' }}</p>
            </div>
          </div>
          <button
            type="button"
            class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors"
            @click="showCommentsModal = false; commentsDocument = null"
          >
            <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </template>
      <DocumentCommentsPanel
        v-if="commentsDocument"
        :document-id="commentsDocument.id"
        embedded
        @close="showCommentsModal = false; commentsDocument = null"
      />
    </UiModal>

    <!-- Links Modal -->
    <UiModal
      v-model="showLinksModal"
      size="lg"
      :show-close="false"
      @close="linksDocument = null"
    >
      <template #header>
        <div class="flex items-center justify-between w-full">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-lg flex items-center justify-center">
              <span class="material-symbols-outlined text-white text-xl">link</span>
            </div>
            <div>
              <h3 class="text-lg font-semibold text-white">Document Links</h3>
              <p class="text-sm text-white/60">{{ linksDocument?.name || 'Document' }}</p>
            </div>
          </div>
          <button
            type="button"
            class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors"
            @click="showLinksModal = false; linksDocument = null"
          >
            <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </template>
      <DocumentLinksPanel
        v-if="linksDocument"
        :document-id="linksDocument.id"
        :can-edit="linksCanEdit"
        embedded
        @close="showLinksModal = false; linksDocument = null"
      />
    </UiModal>

    <!-- New Cabinet Modal -->
    <UiModal v-model="showNewCabinetModal" title="Create New Cabinet" size="sm">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Name *</label>
          <input
            v-model="newCabinetName"
            type="text"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
            placeholder="Cabinet name"
            @keyup.enter="handleCreateCabinet"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
          <textarea
            v-model="newCabinetDescription"
            rows="3"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
            placeholder="Optional description"
          ></textarea>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showNewCabinetModal = false">Cancel</UiButton>
          <UiButton @click="handleCreateCabinet">Create</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Edit Cabinet Modal -->
    <UiModal v-model="showEditCabinetModal" title="Edit Cabinet" size="sm">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Name *</label>
          <input
            v-model="editCabinetData.name"
            type="text"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
          <textarea
            v-model="editCabinetData.description"
            rows="3"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
          ></textarea>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showEditCabinetModal = false">Cancel</UiButton>
          <UiButton @click="handleUpdateCabinet">Save</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- New Folder Modal -->
    <UiModal v-model="showNewFolderModal" title="Create New Folder" size="sm">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Name *</label>
          <input
            v-model="newFolderName"
            type="text"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
            placeholder="Folder name"
            @keyup.enter="handleCreateFolder"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
          <textarea
            v-model="newFolderDescription"
            rows="3"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
            placeholder="Optional description"
          ></textarea>
        </div>
        <div v-if="privacyLevels.length > 0">
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Privacy Level</label>
          <UiSelect
            v-model="newFolderPrivacyLevelId"
            :options="privacyLevelOptions"
            placeholder="None — visible to everyone"
            clearable
          />
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showNewFolderModal = false">Cancel</UiButton>
          <UiButton @click="handleCreateFolder">Create</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Edit Folder Modal -->
    <UiModal v-model="showEditFolderModal" title="Edit Folder" size="sm">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Name *</label>
          <input
            v-model="editFolderData.name"
            type="text"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
          <textarea
            v-model="editFolderData.description"
            rows="3"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
          ></textarea>
        </div>
        <div v-if="privacyLevels.length > 0">
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Privacy Level</label>
          <UiSelect
            v-model="editFolderData.privacyLevelId"
            :options="privacyLevelOptions"
            placeholder="None — visible to everyone"
            clearable
          />
        </div>
        <div class="flex items-center justify-between p-3 rounded-lg border border-zinc-200 dark:border-border-dark bg-zinc-50 dark:bg-surface-dark">
          <div class="flex items-center gap-3">
            <span class="material-symbols-outlined text-lg" :class="editFolderData.accessMode === 1 ? 'text-amber-500' : 'text-zinc-400'">
              {{ editFolderData.accessMode === 1 ? 'lock' : 'lock_open' }}
            </span>
            <div>
              <p class="text-sm font-medium text-zinc-700 dark:text-zinc-200">Private Folder</p>
              <p class="text-xs text-zinc-500 dark:text-zinc-400">Users only see their own documents. Admins see all.</p>
            </div>
          </div>
          <button
            type="button"
            @click="editFolderData.accessMode = editFolderData.accessMode === 1 ? 0 : 1"
            :class="editFolderData.accessMode === 1 ? 'bg-teal' : 'bg-zinc-300 dark:bg-zinc-600'"
            class="relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-teal/50"
          >
            <span
              :class="editFolderData.accessMode === 1 ? 'translate-x-5' : 'translate-x-0'"
              class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out mt-0.5 ml-0.5"
            />
          </button>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showEditFolderModal = false">Cancel</UiButton>
          <UiButton @click="handleUpdateFolder">Save</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Upload Modal -->
    <UploadDocumentModal
      v-if="showUploadModal && store.currentFolder"
      :folder-id="store.currentFolder.id"
      :folder-name="store.currentFolder.name"
      :folder-privacy-level-name="store.currentFolder.privacyLevelName"
      :folder-privacy-level-color="store.currentFolder.privacyLevelColor"
      @close="showUploadModal = false"
      @uploaded="store.loadDocuments(store.currentFolder!.id)"
    />

    <!-- Scan Document Modal -->
    <ScanDocumentModal
      v-if="showScanModal && store.currentFolder"
      :folder-id="store.currentFolder.id"
      :folder-name="store.currentFolder.name"
      @close="showScanModal = false"
      @uploaded="store.loadDocuments(store.currentFolder!.id)"
    />

    <!-- Folder Content Type Modal -->
    <FolderContentTypeModal
      v-if="showContentTypeModal && contentTypeModalTarget"
      :folder-id="contentTypeModalTarget.id"
      :folder-name="contentTypeModalTarget.name"
      :is-cabinet="contentTypeModalTarget.isCabinet"
      @close="showContentTypeModal = false; contentTypeModalTarget = null"
      @updated="() => {}"
    />

    <!-- Delete Confirmation Modal -->
    <UiModal v-model="showDeleteConfirm" :title="'Delete ' + (deleteTarget?.type || '')" size="sm" @close="deleteTarget = null">
      <div class="text-center">
        <div class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 dark:bg-red-900/30 rounded-full mb-4">
          <span class="material-symbols-outlined text-red-600">warning</span>
        </div>
        <p class="text-zinc-500 dark:text-zinc-400">
          Are you sure you want to delete "{{ deleteTarget?.name }}"? This action cannot be undone.
        </p>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showDeleteConfirm = false; deleteTarget = null">Cancel</UiButton>
          <UiButton variant="danger" @click="handleDelete">Delete</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Filing Plans Modal -->
    <UiModal v-model="showFilingPlanModal" title="Filing Plans" size="lg">
      <div>
        <UiButton class="mb-4" @click="showNewFilingPlanModal = true">
          <span class="flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            New Filing Plan
          </span>
        </UiButton>

        <div v-if="isLoadingFilingPlans" class="text-center py-8 text-zinc-500 dark:text-zinc-400">Loading...</div>
        <div v-else-if="filingPlans.length === 0" class="text-center py-8 text-zinc-500 dark:text-zinc-400">
          No filing plans configured
        </div>
        <div v-else class="space-y-2 max-h-96 overflow-y-auto">
          <div
            v-for="plan in filingPlans"
            :key="plan.id"
            class="flex items-center justify-between p-4 bg-zinc-50 dark:bg-surface-dark rounded-lg"
          >
            <div>
              <p class="font-medium text-zinc-900 dark:text-zinc-100">{{ plan.name }}</p>
              <p v-if="plan.description" class="text-sm text-zinc-500 dark:text-zinc-400">{{ plan.description }}</p>
              <div class="flex items-center gap-4 mt-1 text-xs text-zinc-400 dark:text-zinc-500">
                <span v-if="plan.pattern">Pattern: {{ plan.pattern }}</span>
                <span v-if="plan.classificationName">Classification: {{ plan.classificationName }}</span>
                <span v-if="plan.documentTypeName">Type: {{ plan.documentTypeName }}</span>
              </div>
            </div>
            <button
              @click="deleteFilingPlan(plan.id)"
              class="p-2 text-red-500 hover:text-red-700"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end">
          <UiButton variant="outline" @click="showFilingPlanModal = false">Close</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- New Filing Plan Modal -->
    <UiModal v-model="showNewFilingPlanModal" title="New Filing Plan" size="sm">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Name *</label>
          <input
            v-model="newFilingPlan.name"
            type="text"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
            placeholder="Plan name"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
          <textarea
            v-model="newFilingPlan.description"
            rows="2"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
            placeholder="Optional description"
          ></textarea>
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Pattern</label>
          <input
            v-model="newFilingPlan.pattern"
            type="text"
            class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"
            placeholder="e.g., {year}/{month}/{type}"
          />
        </div>
        <div class="grid grid-cols-2 gap-4">
          <UiSelect
            v-model="newFilingPlan.classificationId"
            :options="classificationOptions"
            label="Classification"
            placeholder="None"
          />
          <UiSelect
            v-model="newFilingPlan.documentTypeId"
            :options="documentTypeOptions"
            label="Document Type"
            placeholder="None"
          />
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showNewFilingPlanModal = false">Cancel</UiButton>
          <UiButton @click="createFilingPlan">Create</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Enterprise Permission Management Modal -->
    <PermissionManagementModal
      v-if="permissionModalTarget"
      :show="showPermissionsModal"
      :node-type="permissionModalTarget.nodeType"
      :node-id="permissionModalTarget.nodeId"
      :node-name="permissionModalTarget.nodeName"
      @close="showPermissionsModal = false; permissionModalTarget = null"
      @updated="() => {}"
    />

    <!-- Apply Folder Template Modal -->
    <ApplyTemplateModal
      v-if="showApplyTemplateModal && applyTemplateTarget"
      :folder-id="applyTemplateTarget.id"
      :folder-name="applyTemplateTarget.name"
      :is-cabinet="applyTemplateTarget.isCabinet"
      @close="showApplyTemplateModal = false; applyTemplateTarget = null"
      @applied="handleTemplateApplied"
    />

    <!-- Audit Trail Modal -->
    <UiModal v-model="showAuditModal" :title="'Audit Trail for ' + (contextMenuNode?.name || '')" size="xl">
      <div v-if="isLoadingAudit" class="text-center py-8 text-zinc-500 dark:text-zinc-400">Loading...</div>
      <div v-else-if="auditLogs.length === 0" class="text-center py-8 text-zinc-500 dark:text-zinc-400">
        No activity recorded
      </div>
      <div v-else class="max-h-96 overflow-y-auto">
        <div class="space-y-3">
          <div
            v-for="log in auditLogs"
            :key="log.id"
            class="flex items-start gap-3 p-3 bg-zinc-50 dark:bg-surface-dark rounded-lg"
          >
            <div class="flex-shrink-0 w-10 h-10 bg-blue-100 dark:bg-blue-900/30 rounded-full flex items-center justify-center">
              <svg class="w-5 h-5 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center justify-between">
                <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100">{{ log.action }}</p>
                <span class="text-xs text-zinc-400 dark:text-zinc-500">{{ formatDate(log.createdAt) }}</span>
              </div>
              <p class="text-sm text-zinc-500 dark:text-zinc-400">{{ log.userName || 'System' }}</p>
              <p v-if="log.details" class="text-xs text-zinc-400 dark:text-zinc-500 mt-1">{{ log.details }}</p>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end">
          <UiButton variant="outline" @click="showAuditModal = false">Close</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Settings Modal -->
    <UiModal v-model="showSettingsModal" :title="(contextMenuNode?.type === 'cabinet' ? 'Cabinet' : 'Folder') + ' Settings'" size="sm">
      <div class="space-y-4">
        <div class="flex items-center justify-between p-3 bg-zinc-50 dark:bg-surface-dark rounded-lg">
          <div>
            <p class="text-sm font-medium text-zinc-700 dark:text-zinc-200">Break Permission Inheritance</p>
            <p class="text-xs text-zinc-500 dark:text-zinc-400">Stop inheriting permissions from parent</p>
          </div>
          <UiButton variant="outline" size="sm">Break</UiButton>
        </div>
        <div class="flex items-center justify-between p-3 bg-zinc-50 dark:bg-surface-dark rounded-lg">
          <div>
            <p class="text-sm font-medium text-zinc-700 dark:text-zinc-200">Notifications</p>
            <p class="text-xs text-zinc-500 dark:text-zinc-400">Receive alerts for changes</p>
          </div>
          <UiButton variant="outline" size="sm">Configure</UiButton>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end">
          <UiButton variant="outline" @click="showSettingsModal = false">Close</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Privacy Level Modal -->
    <UiModal v-model="showPrivacyLevelModal" :title="'Set Privacy Level — ' + privacyLevelTarget.name" size="sm">
      <div class="space-y-4">
        <div class="flex items-start gap-3 bg-teal/5 dark:bg-teal/10 border border-teal/20 rounded-lg px-4 py-3">
          <span class="material-symbols-outlined text-teal text-lg mt-0.5">shield</span>
          <p class="text-sm text-zinc-600 dark:text-zinc-300">
            Set the minimum privacy clearance level required to access this folder and its documents. Users with a lower privacy level will not see this folder.
          </p>
        </div>

        <div>
          <label class="block text-xs font-semibold text-zinc-500 uppercase tracking-wider mb-2">Privacy Level</label>
          <UiSelect
            v-model="selectedPrivacyLevelId"
            :options="privacyLevelOptions"
            placeholder="None — visible to everyone"
            clearable
          />
        </div>

        <!-- Current level indicator -->
        <div v-if="selectedPrivacyLevelId" class="flex items-center gap-2 px-3 py-2 rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700">
          <span class="material-symbols-outlined text-amber-500 text-base">visibility_off</span>
          <span class="text-xs text-amber-700 dark:text-amber-400">
            Users with privacy level below <strong>{{ privacyLevels.find(p => p.id === selectedPrivacyLevelId)?.name }}</strong> will not see this folder
          </span>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-2">
          <UiButton variant="outline" @click="showPrivacyLevelModal = false">Cancel</UiButton>
          <UiButton @click="savePrivacyLevel">Save</UiButton>
        </div>
      </template>
    </UiModal>

    <!-- Workflow Assignment Modal -->
    <UiModal v-model="showWorkflowModal" :title="'Manage Workflow — ' + (workflowModalTarget?.name || '')" size="md" overflow-visible>
      <div v-if="isLoadingWorkflows" class="py-10">
        <DmsLoader size="md" text="Loading workflows..." />
      </div>
      <div v-else class="space-y-5">
        <div class="flex items-start gap-3 bg-teal/5 dark:bg-teal/10 border border-teal/20 rounded-lg px-4 py-3">
          <span class="material-symbols-outlined text-teal text-lg mt-0.5">info</span>
          <p class="text-sm text-zinc-600 dark:text-zinc-300">
            Assign a workflow to this {{ workflowModalTarget?.isCabinet ? 'cabinet' : 'folder' }}. Documents uploaded here will automatically trigger the selected workflow.
          </p>
        </div>

        <UiSelect
          v-model="assignedWorkflowId"
          :options="workflowOptions"
          label="Assigned Workflow"
          placeholder="None — no auto-trigger"
          searchable
          clearable
        />

        <!-- Selected workflow detail card -->
        <Transition
          enter-active-class="transition duration-200 ease-out"
          enter-from-class="opacity-0 -translate-y-2"
          enter-to-class="opacity-100 translate-y-0"
          leave-active-class="transition duration-150 ease-in"
          leave-from-class="opacity-100 translate-y-0"
          leave-to-class="opacity-0 -translate-y-2"
        >
          <div v-if="selectedWorkflow" class="rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden">
            <div class="bg-gradient-to-r from-navy/5 to-teal/5 dark:from-navy/20 dark:to-teal/20 px-4 py-3 flex items-center justify-between border-b border-zinc-200 dark:border-border-dark">
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-lg bg-teal/15 flex items-center justify-center">
                  <span class="material-symbols-outlined text-teal text-base">account_tree</span>
                </div>
                <span class="text-sm font-semibold text-zinc-800 dark:text-zinc-100">{{ selectedWorkflow.name }}</span>
              </div>
              <span
                class="inline-flex items-center gap-1 text-[11px] font-semibold px-2.5 py-1 rounded-full"
                :class="selectedWorkflow.triggerType === 'OnUpload'
                  ? 'bg-teal/15 text-teal'
                  : 'bg-zinc-100 dark:bg-zinc-700 text-zinc-500 dark:text-zinc-400'"
              >
                <span class="material-symbols-outlined text-xs">{{ selectedWorkflow.triggerType === 'OnUpload' ? 'bolt' : 'touch_app' }}</span>
                {{ selectedWorkflow.triggerType === 'OnUpload' ? 'Auto' : selectedWorkflow.triggerType }}
              </span>
            </div>
            <div class="px-4 py-3 grid grid-cols-2 gap-x-6 gap-y-2.5">
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-zinc-400 text-base">play_circle</span>
                <div>
                  <div class="text-[10px] font-semibold text-zinc-400 dark:text-zinc-500 uppercase tracking-wider">Trigger</div>
                  <div class="text-sm text-zinc-700 dark:text-zinc-200">{{ selectedWorkflow.triggerType === 'OnUpload' ? 'On File Upload' : selectedWorkflow.triggerType }}</div>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-zinc-400 text-base">{{ selectedWorkflow.inheritToSubfolders ? 'account_tree' : 'folder' }}</span>
                <div>
                  <div class="text-[10px] font-semibold text-zinc-400 dark:text-zinc-500 uppercase tracking-wider">Scope</div>
                  <div class="text-sm text-zinc-700 dark:text-zinc-200">{{ selectedWorkflow.inheritToSubfolders ? 'Includes subfolders' : 'This folder only' }}</div>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-zinc-400 text-base">format_list_numbered</span>
                <div>
                  <div class="text-[10px] font-semibold text-zinc-400 dark:text-zinc-500 uppercase tracking-wider">Steps</div>
                  <div class="text-sm text-zinc-700 dark:text-zinc-200">{{ selectedWorkflow.steps?.length || 0 }} {{ (selectedWorkflow.steps?.length || 0) === 1 ? 'step' : 'steps' }}</div>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-zinc-400 text-base">group</span>
                <div>
                  <div class="text-[10px] font-semibold text-zinc-400 dark:text-zinc-500 uppercase tracking-wider">Approvers</div>
                  <div class="text-sm text-zinc-700 dark:text-zinc-200">{{ selectedWorkflow.requiredApprovers }} required</div>
                </div>
              </div>
            </div>
          </div>
        </Transition>

        <div v-if="!allWorkflows.some(w => w.isActive)" class="flex items-center gap-2 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/30 rounded-lg px-4 py-3">
          <span class="material-symbols-outlined text-amber-500 text-base">warning</span>
          <p class="text-xs text-amber-700 dark:text-amber-400">
            No active workflows found. <a href="/admin/workflow-designer" class="font-medium underline hover:text-teal transition-colors">Create one in the Workflow Designer</a>.
          </p>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showWorkflowModal = false">Cancel</UiButton>
          <UiButton @click="saveWorkflowAssignment" :disabled="isSavingWorkflow">
            <span v-if="isSavingWorkflow" class="flex items-center gap-2">
              <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              Saving...
            </span>
            <span v-else>Save Assignment</span>
          </UiButton>
        </div>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
/* Toolbar Icon Buttons */
.toolbar-btn {
  @apply w-9 h-9 flex items-center justify-center rounded-md transition-colors;
}

.toolbar-btn:hover {
  @apply bg-white dark:bg-border-dark;
}

/* Selection Toolbar Transition */
.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.3s ease;
}

.slide-up-enter-from,
.slide-up-leave-to {
  opacity: 0;
  transform: translate(-50%, 20px);
}


</style>

