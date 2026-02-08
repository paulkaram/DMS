<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useDocumentsStore } from '@/stores/documents'
import { useAuthStore } from '@/stores/auth'
import { referenceDataApi, cabinetsApi, foldersApi, documentsApi, folderLinksApi, filingPlansApi, permissionsApi, activityLogsApi, contentTypeDefinitionsApi } from '@/api/client'
import TreeView from '@/components/common/TreeView.vue'
import FileList from '@/components/common/FileList.vue'
import FolderGrid from '@/components/common/FolderGrid.vue'
import ContextMenu from '@/components/common/ContextMenu.vue'
import SelectionToolbar from '@/components/common/SelectionToolbar.vue'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'
import MoveDocumentsModal from '@/components/documents/MoveDocumentsModal.vue'
import ShareDocumentModal from '@/components/documents/ShareDocumentModal.vue'
import UploadDocumentModal from '@/components/documents/UploadDocumentModal.vue'
import ScanDocumentModal from '@/components/documents/ScanDocumentModal.vue'
import DocumentPasswordDialog from '@/components/documents/DocumentPasswordDialog.vue'
import FolderContentTypeModal from '@/components/folders/FolderContentTypeModal.vue'
import { PermissionManagementModal } from '@/components/permissions'
import ApplyTemplateModal from '@/components/templates/ApplyTemplateModal.vue'
import type { MenuItem } from '@/components/common/ContextMenu.vue'
import type { Classification, Importance, DocumentType, Cabinet, Folder, TreeNode, FolderLink, FilingPlan, Permission, ActivityLog, Document, ApplyTemplateResult } from '@/types'
import { PermissionLevels } from '@/types'
import { UiSelect, UiConfirmDialog } from '@/components/ui'
import type { ConfirmDialogType } from '@/components/ui/ConfirmDialog.vue'
import { useBulkOperations } from '@/composables/useBulkOperations'

const router = useRouter()
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
const showFolderLinksModal = ref(false)
const showViewLinksModal = ref(false)
const showFilingPlanModal = ref(false)
const showPermissionsModal = ref(false)
const showAuditModal = ref(false)
const showSettingsModal = ref(false)

// Folder Links Data
const folderLinks = ref<FolderLink[]>([])
const incomingLinks = ref<FolderLink[]>([])
const newLinkTargetId = ref('')
const isLoadingLinks = ref(false)

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
const editFolderData = ref({ id: '', name: '', description: '' })

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

onMounted(async () => {
  await Promise.all([
    store.loadCabinets(),
    loadReferenceData()
  ])
})

async function loadReferenceData() {
  try {
    const [classRes, impRes, docTypeRes] = await Promise.all([
      referenceDataApi.getClassifications(),
      referenceDataApi.getImportances(),
      referenceDataApi.getDocumentTypes()
    ])
    classifications.value = classRes.data
    importances.value = impRes.data
    documentTypes.value = docTypeRes.data
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
      description: newFolderDescription.value
    })
    newFolderName.value = ''
    newFolderDescription.value = ''
    showNewFolderModal.value = false
    await store.loadFolderTree(store.currentCabinet.id)
  } catch (err) {
  }
}

function openEditFolder(folder: Folder) {
  editFolderData.value = {
    id: folder.id,
    name: folder.name,
    description: folder.description || ''
  }
  showEditFolderModal.value = true
}

async function handleUpdateFolder() {
  if (!editFolderData.value.name.trim()) return
  try {
    await foldersApi.update(editFolderData.value.id, {
      name: editFolderData.value.name,
      description: editFolderData.value.description,
      breakInheritance: false
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
      await store.loadCabinets()
      if (store.currentCabinet?.id === deleteTarget.value.id) {
        store.currentCabinet = null
        store.currentFolder = null
        store.documents = []
      }
    } else {
      await foldersApi.delete(deleteTarget.value.id)
      if (store.currentCabinet) {
        await store.loadFolderTree(store.currentCabinet.id)
      }
      if (store.currentFolder?.id === deleteTarget.value.id) {
        store.currentFolder = null
        store.documents = []
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
    'upload': 'folder.upload',
    'apply-template': 'folder.template.manage',
    'paste': 'folder.paste',
    'add-pattern': 'folder.pattern.add',
    'add-filing-plan': 'folder.filingplan.add',
    'settings': `${prefix}.settings`,
    'content-types': 'folder.contenttype.manage',
    'link-folders': 'folder.link.manage',
    'view-links': 'folder.link.view',
    'share': 'folder.share',
    'permissions': `${prefix}.permissions`,
    'delete': `${prefix}.delete`,
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
    (!isCabinet && (canPerform('add-file', canWrite) || canPerform('upload', canWrite)))

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
        items.push({ id: 'add-file', label: 'Add file', icon: 'file-add' })
      }
      if (canPerform('upload', canWrite)) {
        items.push({ id: 'upload', label: 'Upload', icon: 'upload' })
      }
      if (hasClipboard && canPerform('paste', canWrite)) {
        items.push({ id: 'paste', label: pasteLabel, icon: 'paste' })
      }
    }
  }

  // Admin actions (patterns, filing plans, settings, content types)
  const adminActionsAvailable = (!isCabinet && (canPerform('add-pattern', canAdmin) || canPerform('add-filing-plan', canAdmin))) ||
    canPerform('settings', canAdmin) || canPerform('content-types', canAdmin)

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
  }

  // Link actions
  if (!isCabinet) {
    const linkActionsAvailable = canPerform('link-folders', canWrite) || canPerform('view-links', canRead)
    if (linkActionsAvailable) {
      if (items.length > 0) items.push({ id: 'divider4', label: '', divider: true })
      if (canPerform('link-folders', canWrite)) {
        items.push({ id: 'link-folders', label: 'Link to other folders', icon: 'link' })
      }
      if (canPerform('view-links', canRead)) {
        items.push({ id: 'view-links', label: 'View link to other folders', icon: 'link-view' })
      }
    }
  }

  // Share and permissions
  const sharePermAvailable = (!isCabinet && canPerform('share', canRead)) || canPerform('permissions', canAdmin)
  if (sharePermAvailable) {
    if (items.length > 0) items.push({ id: 'divider5', label: '', divider: true })
    if (!isCabinet && canPerform('share', canRead)) {
      items.push({ id: 'share', label: 'Share', icon: 'share' })
    }
    if (canPerform('permissions', canAdmin)) {
      items.push({ id: 'permissions', label: 'Manage permissions', icon: 'permission' })
    }
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

  // Grid view is always available (UI preference, not a permission)
  items.push({ id: 'grid-view', label: 'Show as grid', icon: 'grid' })

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
        editFolderData.value = { id: node.id, name: node.name, description: '' }
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
    case 'upload':
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
    case 'link-folders':
      if (!isCabinet) {
        await loadFolderLinks(node.id)
        showFolderLinksModal.value = true
      }
      break
    case 'view-links':
      if (!isCabinet) {
        await loadIncomingLinks(node.id)
        showViewLinksModal.value = true
      }
      break
    case 'share':
      router.push(`/my-shared-items`)
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
    case 'grid-view':
      viewMode.value = viewMode.value === 'list' ? 'grid' : 'list'
      break
    case 'export':
      // Export functionality - could download folder contents as zip
      break
  }

  closeContextMenu()
}

// Folder Links Operations
async function loadFolderLinks(folderId: string) {
  isLoadingLinks.value = true
  try {
    const response = await folderLinksApi.getFromFolder(folderId)
    folderLinks.value = response.data
  } catch (err) {
  } finally {
    isLoadingLinks.value = false
  }
}

async function loadIncomingLinks(folderId: string) {
  isLoadingLinks.value = true
  try {
    const response = await folderLinksApi.getToFolder(folderId)
    incomingLinks.value = response.data
  } catch (err) {
  } finally {
    isLoadingLinks.value = false
  }
}

async function createFolderLink() {
  if (!contextMenuNode.value || !newLinkTargetId.value) return
  try {
    await folderLinksApi.create({
      sourceFolderId: contextMenuNode.value.id,
      targetFolderId: newLinkTargetId.value
    })
    await loadFolderLinks(contextMenuNode.value.id)
    newLinkTargetId.value = ''
  } catch (err) {
  }
}

async function deleteFolderLink(linkId: string) {
  try {
    await folderLinksApi.delete(linkId)
    if (contextMenuNode.value) {
      await loadFolderLinks(contextMenuNode.value.id)
    }
  } catch (err) {
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
    auditLogs.value = response.data
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
    case 'start-workflow':
      router.push(`/approvals/new?documentId=${document.id}`)
      break
    case 'route':
      router.push(`/approvals/new?documentId=${document.id}`)
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

  // Refresh the folder tree to show newly created folders
  if (store.currentCabinet) {
    await store.loadFolderTree(store.currentCabinet.id)
  }
}
</script>

<template>
  <div class="-m-6 flex h-[calc(100vh-4rem)]">
    <!-- Sidebar Tree -->
    <div
      class="flex-shrink-0 bg-white dark:bg-slate-900/50 border-r border-slate-200 dark:border-slate-800 flex flex-col"
      :style="{ width: `${treePanelWidth}px` }"
    >
      <div class="px-2 py-2 border-b border-slate-200 dark:border-slate-800">
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
      :class="isResizing ? 'w-1.5 bg-teal' : 'bg-slate-200 dark:bg-slate-700'"
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
      <div class="bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-700 px-6 py-4">
        <!-- Title Row -->
        <div class="flex items-center justify-between mb-3">
          <h1 class="text-xl font-bold text-slate-900 dark:text-slate-100">Explorer</h1>
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
          <div class="flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400 min-w-0 flex-wrap">
            <button @click="store.currentCabinet = null; store.currentFolder = null; store.folderPath = []" class="hover:text-teal transition-colors flex items-center gap-1 flex-shrink-0">
              <span class="material-symbols-outlined text-lg">home</span>
            </button>
            <template v-for="(item, idx) in breadcrumbs" :key="idx">
              <span class="material-symbols-outlined text-sm text-slate-400 flex-shrink-0">chevron_right</span>
              <button
                v-if="idx < breadcrumbs.length - 1"
                @click="navigateBreadcrumb(item)"
                class="hover:text-teal cursor-pointer transition-colors truncate max-w-[150px]"
                :title="item.name"
              >{{ item.name }}</button>
              <span
                v-else
                class="font-semibold text-slate-900 dark:text-slate-100 truncate max-w-[200px]"
                :title="item.name"
              >{{ item.name }}</span>
            </template>
          </div>

          <!-- Actions Toolbar -->
          <div class="flex items-center gap-3">
            <!-- Icon Button Group (Delete & Edit) -->
            <div v-if="store.currentCabinet" class="flex items-center bg-slate-100 dark:bg-slate-800 rounded-lg p-1">
              <!-- Delete Button -->
              <button
                v-if="!store.currentFolder"
                @click="confirmDeleteCabinet(store.currentCabinet)"
                class="toolbar-btn group"
                title="Delete Cabinet"
              >
                <span class="material-symbols-outlined text-[20px] text-slate-500 group-hover:text-red-500 transition-colors">delete</span>
              </button>
              <button
                v-if="store.currentFolder"
                @click="confirmDeleteFolder(store.currentFolder)"
                class="toolbar-btn group"
                title="Delete Folder"
              >
                <span class="material-symbols-outlined text-[20px] text-slate-500 group-hover:text-red-500 transition-colors">delete</span>
              </button>

              <!-- Edit Button -->
              <button
                v-if="!store.currentFolder"
                @click="openEditCabinet(store.currentCabinet)"
                class="toolbar-btn group"
                title="Edit Properties"
              >
                <span class="material-symbols-outlined text-[20px] text-slate-500 group-hover:text-teal transition-colors">settings</span>
              </button>
              <button
                v-if="store.currentFolder"
                @click="openEditFolder(store.currentFolder)"
                class="toolbar-btn group"
                title="Edit Properties"
              >
                <span class="material-symbols-outlined text-[20px] text-slate-500 group-hover:text-teal transition-colors">settings</span>
              </button>
            </div>

            <!-- New Folder Button -->
            <button
              v-if="store.currentCabinet"
              @click="showNewFolderModal = true"
              class="flex items-center gap-2 px-3 py-2 text-slate-600 dark:text-slate-300 border border-slate-300 dark:border-slate-600 hover:border-teal hover:text-teal rounded-lg text-sm font-medium transition-colors"
            >
              <span class="material-symbols-outlined text-[20px]">create_new_folder</span>
              <span class="hidden sm:inline">New Folder</span>
            </button>

            <!-- Scan Button -->
            <button
              v-if="store.currentFolder"
              @click="showScanModal = true"
              class="flex items-center gap-2 px-3 py-2 text-slate-600 dark:text-slate-300 border border-slate-300 dark:border-slate-600 hover:border-teal hover:text-teal rounded-lg text-sm font-medium transition-colors"
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
        <!-- Empty state when nothing selected -->
        <div v-if="!store.currentCabinet && !store.currentFolder" class="flex flex-col items-center justify-center h-full text-slate-500">
          <span class="material-symbols-outlined text-7xl text-slate-300 mb-4">folder_open</span>
          <p class="text-lg font-medium text-slate-700 dark:text-slate-300">Select a cabinet or folder to view contents</p>
          <p class="text-sm text-slate-400 mt-1">Or create a new cabinet to get started</p>
        </div>

        <!-- Content when cabinet or folder is selected -->
        <div v-else class="space-y-6">
          <!-- Subfolders Section -->
          <div v-if="store.subFolders.length > 0 || store.isLoadingSubFolders">
            <!-- Subfolders Header -->
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-slate-400">folder</span>
                <h3 class="text-sm font-semibold text-slate-700 dark:text-slate-300">Folders</h3>
                <span class="text-xs text-slate-400">({{ store.subFolders.length }})</span>
              </div>
              <!-- View Mode Toggle -->
              <div class="flex items-center bg-slate-100 dark:bg-slate-800 rounded-lg p-0.5">
                <button
                  @click="folderViewMode = 'grid'"
                  class="p-1.5 rounded-md transition-colors"
                  :class="folderViewMode === 'grid' ? 'bg-white dark:bg-slate-700 shadow-sm text-teal' : 'text-slate-400 hover:text-slate-600'"
                  title="Grid view"
                >
                  <span class="material-symbols-outlined text-lg">grid_view</span>
                </button>
                <button
                  @click="folderViewMode = 'list'"
                  class="p-1.5 rounded-md transition-colors"
                  :class="folderViewMode === 'list' ? 'bg-white dark:bg-slate-700 shadow-sm text-teal' : 'text-slate-400 hover:text-slate-600'"
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
              @context-menu="(e, f) => handleTreeContextMenu(e, { id: f.id, name: f.name, type: 'folder', children: [], isExpanded: false })"
            />
          </div>

          <!-- Documents Section -->
          <div v-if="store.currentFolder">
            <div class="flex items-center gap-2 mb-3" v-if="store.subFolders.length > 0">
              <span class="material-symbols-outlined text-slate-400">description</span>
              <h3 class="text-sm font-semibold text-slate-700 dark:text-slate-300">Documents</h3>
              <span class="text-xs text-slate-400">({{ store.documents.length }})</span>
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
          <div v-else-if="!store.currentFolder && store.subFolders.length === 0 && !store.isLoadingSubFolders" class="text-center py-12 text-slate-400">
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
        class="fixed bottom-6 left-1/2 -translate-x-1/2 bg-slate-900 text-white rounded-2xl shadow-2xl px-6 py-4 flex items-center gap-6 z-50"
      >
        <!-- Selection Count -->
        <div class="flex items-center gap-2">
          <span class="material-symbols-outlined text-teal">check_circle</span>
          <span class="font-semibold">{{ selectedFolderIds.length }} folder{{ selectedFolderIds.length === 1 ? '' : 's' }} selected</span>
        </div>

        <!-- Divider -->
        <div class="w-px h-8 bg-slate-700"></div>

        <!-- Actions -->
        <div class="flex items-center gap-1">
          <!-- Delete -->
          <button
            @click="handleBulkDeleteFolders"
            :disabled="isBulkDeletingFolders"
            class="flex items-center gap-2 px-4 py-2 hover:bg-red-600 rounded-xl transition-colors disabled:opacity-50"
            title="Delete selected folders"
          >
            <span class="material-symbols-outlined text-[20px]">delete</span>
            <span class="text-sm font-medium">Delete</span>
          </button>
        </div>

        <!-- Divider -->
        <div class="w-px h-8 bg-slate-700"></div>

        <!-- Clear Selection -->
        <button
          @click="clearFolderSelection"
          :disabled="isBulkDeletingFolders"
          class="p-2 hover:bg-slate-800 rounded-xl transition-colors disabled:opacity-50"
          title="Clear selection"
        >
          <span class="material-symbols-outlined">close</span>
        </button>

        <!-- Processing Indicator -->
        <div v-if="isBulkDeletingFolders" class="absolute inset-0 bg-slate-900/80 rounded-2xl flex items-center justify-center">
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
      v-model="showMoveModal"
      :document-count="1"
      @confirm="handleSingleDocumentMove"
    />

    <!-- Bulk Move Modal -->
    <MoveDocumentsModal
      v-model="showBulkMoveModal"
      :document-count="selectedDocumentIds.length"
      @confirm="handleBulkMove"
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
      @updated="showPasswordDialog = false; passwordDocument = null"
    />

    <!-- New Cabinet Modal -->
    <div v-if="showNewCabinetModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl w-full max-w-md mx-4 border border-slate-200 dark:border-slate-800">
        <div class="p-6">
          <h3 class="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">Create New Cabinet</h3>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Name *</label>
              <input
                v-model="newCabinetName"
                type="text"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
                placeholder="Cabinet name"
                @keyup.enter="handleCreateCabinet"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Description</label>
              <textarea
                v-model="newCabinetDescription"
                rows="3"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
                placeholder="Optional description"
              ></textarea>
            </div>
          </div>
          <div class="mt-6 flex justify-end gap-3">
            <button @click="showNewCabinetModal = false" class="px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300">
              Cancel
            </button>
            <button @click="handleCreateCabinet" class="px-4 py-2 bg-brand-gradient text-white rounded-lg hover:opacity-90">
              Create
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Cabinet Modal -->
    <div v-if="showEditCabinetModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl w-full max-w-md mx-4 border border-slate-200 dark:border-slate-800">
        <div class="p-6">
          <h3 class="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">Edit Cabinet</h3>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Name *</label>
              <input
                v-model="editCabinetData.name"
                type="text"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Description</label>
              <textarea
                v-model="editCabinetData.description"
                rows="3"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
              ></textarea>
            </div>
          </div>
          <div class="mt-6 flex justify-end gap-3">
            <button @click="showEditCabinetModal = false" class="px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300">
              Cancel
            </button>
            <button @click="handleUpdateCabinet" class="px-4 py-2 bg-brand-gradient text-white rounded-lg hover:opacity-90">
              Save
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- New Folder Modal -->
    <div v-if="showNewFolderModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl w-full max-w-md mx-4 border border-slate-200 dark:border-slate-800">
        <div class="p-6">
          <h3 class="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">Create New Folder</h3>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Name *</label>
              <input
                v-model="newFolderName"
                type="text"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
                placeholder="Folder name"
                @keyup.enter="handleCreateFolder"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Description</label>
              <textarea
                v-model="newFolderDescription"
                rows="3"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
                placeholder="Optional description"
              ></textarea>
            </div>
          </div>
          <div class="mt-6 flex justify-end gap-3">
            <button @click="showNewFolderModal = false" class="px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300">
              Cancel
            </button>
            <button @click="handleCreateFolder" class="px-4 py-2 bg-brand-gradient text-white rounded-lg hover:opacity-90">
              Create
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Edit Folder Modal -->
    <div v-if="showEditFolderModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl w-full max-w-md mx-4 border border-slate-200 dark:border-slate-800">
        <div class="p-6">
          <h3 class="text-lg font-semibold text-slate-900 dark:text-slate-100 mb-4">Edit Folder</h3>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Name *</label>
              <input
                v-model="editFolderData.name"
                type="text"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Description</label>
              <textarea
                v-model="editFolderData.description"
                rows="3"
                class="w-full px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-slate-800 text-slate-900 dark:text-slate-100"
              ></textarea>
            </div>
          </div>
          <div class="mt-6 flex justify-end gap-3">
            <button @click="showEditFolderModal = false" class="px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300">
              Cancel
            </button>
            <button @click="handleUpdateFolder" class="px-4 py-2 bg-brand-gradient text-white rounded-lg hover:opacity-90">
              Save
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Upload Modal -->
    <UploadDocumentModal
      v-if="showUploadModal && store.currentFolder"
      :folder-id="store.currentFolder.id"
      :folder-name="store.currentFolder.name"
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
    <div v-if="showDeleteConfirm" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl w-full max-w-sm mx-4 border border-slate-200 dark:border-slate-800">
        <div class="p-6">
          <div class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 dark:bg-red-900/30 rounded-full mb-4">
            <span class="material-symbols-outlined text-red-600">warning</span>
          </div>
          <h3 class="text-lg font-semibold text-slate-900 dark:text-slate-100 text-center mb-2">Delete {{ deleteTarget?.type }}</h3>
          <p class="text-slate-500 text-center mb-6">
            Are you sure you want to delete "{{ deleteTarget?.name }}"? This action cannot be undone.
          </p>
          <div class="flex gap-3">
            <button
              @click="showDeleteConfirm = false; deleteTarget = null"
              class="flex-1 px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300"
            >
              Cancel
            </button>
            <button
              @click="handleDelete"
              class="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700"
            >
              Delete
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Folder Links Modal -->
    <div v-if="showFolderLinksModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-gray-900">Link to Other Folders</h3>
            <button @click="showFolderLinksModal = false" class="text-gray-400 hover:text-gray-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Add New Link -->
          <div class="flex gap-2 mb-4">
            <input
              v-model="newLinkTargetId"
              type="text"
              class="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
              placeholder="Target folder ID"
            />
            <button
              @click="createFolderLink"
              class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90"
            >
              Add Link
            </button>
          </div>

          <!-- Existing Links -->
          <div v-if="isLoadingLinks" class="text-center py-8 text-gray-500">Loading...</div>
          <div v-else-if="folderLinks.length === 0" class="text-center py-8 text-gray-500">
            No links configured
          </div>
          <div v-else class="space-y-2 max-h-64 overflow-y-auto">
            <div
              v-for="link in folderLinks"
              :key="link.id"
              class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
            >
              <div>
                <p class="text-sm font-medium text-gray-700">{{ link.targetFolderName || link.targetFolderId }}</p>
                <p v-if="link.targetFolderPath" class="text-xs text-gray-500">{{ link.targetFolderPath }}</p>
              </div>
              <button
                @click="deleteFolderLink(link.id)"
                class="p-1 text-red-500 hover:text-red-700"
              >
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- View Incoming Links Modal -->
    <div v-if="showViewLinksModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-gray-900">Links to This Folder</h3>
            <button @click="showViewLinksModal = false" class="text-gray-400 hover:text-gray-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div v-if="isLoadingLinks" class="text-center py-8 text-gray-500">Loading...</div>
          <div v-else-if="incomingLinks.length === 0" class="text-center py-8 text-gray-500">
            No folders link to this one
          </div>
          <div v-else class="space-y-2 max-h-64 overflow-y-auto">
            <div
              v-for="link in incomingLinks"
              :key="link.id"
              class="p-3 bg-gray-50 rounded-lg"
            >
              <p class="text-sm font-medium text-gray-700">{{ link.sourceFolderName || link.sourceFolderId }}</p>
              <p class="text-xs text-gray-500">Linked on {{ formatDate(link.createdAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Filing Plans Modal -->
    <div v-if="showFilingPlanModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-gray-900">Filing Plans</h3>
            <button @click="showFilingPlanModal = false" class="text-gray-400 hover:text-gray-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <button
            @click="showNewFilingPlanModal = true"
            class="mb-4 flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            New Filing Plan
          </button>

          <div v-if="isLoadingFilingPlans" class="text-center py-8 text-gray-500">Loading...</div>
          <div v-else-if="filingPlans.length === 0" class="text-center py-8 text-gray-500">
            No filing plans configured
          </div>
          <div v-else class="space-y-2 max-h-96 overflow-y-auto">
            <div
              v-for="plan in filingPlans"
              :key="plan.id"
              class="flex items-center justify-between p-4 bg-gray-50 rounded-lg"
            >
              <div>
                <p class="font-medium text-gray-900">{{ plan.name }}</p>
                <p v-if="plan.description" class="text-sm text-gray-500">{{ plan.description }}</p>
                <div class="flex items-center gap-4 mt-1 text-xs text-gray-400">
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
      </div>
    </div>

    <!-- New Filing Plan Modal -->
    <div v-if="showNewFilingPlanModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4">
        <div class="p-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">New Filing Plan</h3>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
              <input
                v-model="newFilingPlan.name"
                type="text"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
                placeholder="Plan name"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <textarea
                v-model="newFilingPlan.description"
                rows="2"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
                placeholder="Optional description"
              ></textarea>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Pattern</label>
              <input
                v-model="newFilingPlan.pattern"
                type="text"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
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
          <div class="mt-6 flex justify-end gap-3">
            <button @click="showNewFilingPlanModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">
              Cancel
            </button>
            <button @click="createFilingPlan" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
              Create
            </button>
          </div>
        </div>
      </div>
    </div>

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
    <div v-if="showAuditModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-3xl mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-gray-900">
              Audit Trail for {{ contextMenuNode?.name }}
            </h3>
            <button @click="showAuditModal = false" class="text-gray-400 hover:text-gray-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div v-if="isLoadingAudit" class="text-center py-8 text-gray-500">Loading...</div>
          <div v-else-if="auditLogs.length === 0" class="text-center py-8 text-gray-500">
            No activity recorded
          </div>
          <div v-else class="max-h-96 overflow-y-auto">
            <div class="space-y-3">
              <div
                v-for="log in auditLogs"
                :key="log.id"
                class="flex items-start gap-3 p-3 bg-gray-50 rounded-lg"
              >
                <div class="flex-shrink-0 w-10 h-10 bg-blue-100 rounded-full flex items-center justify-center">
                  <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
                  </svg>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center justify-between">
                    <p class="text-sm font-medium text-gray-900">{{ log.action }}</p>
                    <span class="text-xs text-gray-400">{{ formatDate(log.createdAt) }}</span>
                  </div>
                  <p class="text-sm text-gray-500">{{ log.userName || 'System' }}</p>
                  <p v-if="log.details" class="text-xs text-gray-400 mt-1">{{ log.details }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Settings Modal -->
    <div v-if="showSettingsModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold text-gray-900">
              {{ contextMenuNode?.type === 'cabinet' ? 'Cabinet' : 'Folder' }} Settings
            </h3>
            <button @click="showSettingsModal = false" class="text-gray-400 hover:text-gray-600">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="space-y-4">
            <div class="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
              <div>
                <p class="text-sm font-medium text-gray-700">Break Permission Inheritance</p>
                <p class="text-xs text-gray-500">Stop inheriting permissions from parent</p>
              </div>
              <button class="px-3 py-1 text-sm border border-gray-300 rounded-lg hover:bg-gray-100">
                Break
              </button>
            </div>
            <div class="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
              <div>
                <p class="text-sm font-medium text-gray-700">Notifications</p>
                <p class="text-xs text-gray-500">Receive alerts for changes</p>
              </div>
              <button class="px-3 py-1 text-sm border border-gray-300 rounded-lg hover:bg-gray-100">
                Configure
              </button>
            </div>
          </div>

          <div class="mt-6 flex justify-end">
            <button @click="showSettingsModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Toolbar Icon Buttons */
.toolbar-btn {
  @apply w-9 h-9 flex items-center justify-center rounded-md transition-colors;
}

.toolbar-btn:hover {
  @apply bg-white dark:bg-slate-700;
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

