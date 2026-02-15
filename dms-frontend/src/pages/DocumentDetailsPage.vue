<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { Document, DocumentVersion, WorkingCopy, VersionComparison } from '@/types'
import { CheckInType, DiffType } from '@/types'
import { documentsApi, permissionsApi, activityLogsApi, contentTypeDefinitionsApi, foldersApi, cabinetsApi, referenceDataApi, privacyLevelsApi } from '@/api/client'
import type { PrivacyLevel, Classification, Importance, DocumentType as DocType } from '@/types'
import { PermissionLevels } from '@/types'
import { UiSelect, UiDatePicker } from '@/components/ui'
import { PermissionManagementModal } from '@/components/permissions'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'
import InlineDocumentPreview from '@/components/documents/InlineDocumentPreview.vue'

const route = useRoute()
const router = useRouter()

const document = ref<Document | null>(null)
const versions = ref<DocumentVersion[]>([])
const activities = ref<any[]>([])
const metadata = ref<any[]>([])
const contentTypeInfo = ref<any>(null)
const isLoading = ref(true)
const error = ref<string | null>(null)

// Sidebar tab
const sidebarTab = ref<'info' | 'meta' | 'versions' | 'activity' | 'edit'>('info')

// Working Copy (draft state during checkout)
const workingCopy = ref<WorkingCopy | null>(null)

// Draft editing state
const draftName = ref('')
const draftDescription = ref('')
const draftMetadata = ref<{ fieldId: string; fieldName: string; value: string; numericValue?: number; dateValue?: string; fieldType: string }[]>([])
const draftFile = ref<File | null>(null)
const isSavingDraft = ref(false)
const isUploadingFile = ref(false)
const draftSaveMessage = ref<{ type: 'success' | 'error'; text: string } | null>(null)

// Draft classification, importance, document type, expiry, privacy
const draftClassificationId = ref<string | null>(null)
const draftImportanceId = ref<string | null>(null)
const draftDocumentTypeId = ref<string | null>(null)
const draftExpiryDate = ref<string>('')
const draftPrivacyLevelId = ref<string | null>(null)

// Reference data for dropdowns
const classifications = ref<Classification[]>([])
const importances = ref<Importance[]>([])
const documentTypes = ref<DocType[]>([])
const privacyLevels = ref<PrivacyLevel[]>([])

// File upload drag-and-drop
const isDragging = ref(false)
const fileInputRef = ref<HTMLInputElement | null>(null)

// User's permission level on this document
const myPermissionLevel = ref<number>(0)

// Folder privacy level
const folderPrivacyLevelName = ref<string | null>(null)
const folderPrivacyLevelColor = ref<string | null>(null)

// Folder breadcrumb for navigation context
const folderBreadcrumb = ref<{ id: string; name: string; cabinetId: string }[]>([])
const cabinetName = ref<string | null>(null)
const cabinetId = ref<string | null>(null)

// Computed permission checks
const canRead = computed(() => myPermissionLevel.value >= PermissionLevels.Read)
const canWrite = computed(() => myPermissionLevel.value >= PermissionLevels.Write)
const canDelete = computed(() => myPermissionLevel.value >= PermissionLevels.Delete)
const canAdmin = computed(() => myPermissionLevel.value >= PermissionLevels.Admin)

// Check if current user owns the checkout
const isMyCheckout = computed(() => {
  if (!document.value?.isCheckedOut) return false
  return true
})

// Is in edit mode (checked out by current user)
const isEditMode = computed(() => document.value?.isCheckedOut && isMyCheckout.value)

// Check-in modal
const showCheckinModal = ref(false)
const checkinFile = ref<File | null>(null)
const checkinComment = ref('')
const checkinChangeDescription = ref('')
const checkinType = ref<CheckInType>(CheckInType.Minor)
const keepCheckedOut = ref(false)
const isCheckingIn = ref(false)

// Version Comparison Modal
const showCompareModal = ref(false)
const compareSourceVersion = ref<string>('')
const compareTargetVersion = ref<string>('')
const versionComparison = ref<VersionComparison | null>(null)
const isComparing = ref(false)

// Restore Version Modal
const showRestoreModal = ref(false)
const restoreVersionId = ref<string>('')
const restoreComment = ref('')
const restoreContent = ref(true)
const restoreMetadata = ref(true)
const isRestoring = ref(false)

// Enterprise Permission Management Modal
const showPermissionModal = ref(false)

// Document Preview (full viewer modal)
const showPreview = ref(false)

onMounted(async () => {
  await loadDocument()
})

async function loadDocument() {
  const id = route.params.id as string
  isLoading.value = true
  try {
    const [docResponse, versionsResponse, metadataResponse] = await Promise.all([
      documentsApi.getById(id),
      documentsApi.getVersions(id),
      contentTypeDefinitionsApi.getDocumentMetadata(id)
    ])
    document.value = docResponse.data
    versions.value = versionsResponse.data
    metadata.value = metadataResponse.data || []

    await loadMyPermissionLevel(id)

    // Load folder info, privacy level, and build breadcrumb path
    if (document.value.folderId) {
      try {
        const folderRes = await foldersApi.getById(document.value.folderId)
        const folder = folderRes.data
        folderPrivacyLevelName.value = folder.privacyLevelName || null
        folderPrivacyLevelColor.value = folder.privacyLevelColor || null

        // Build breadcrumb by walking up parent chain
        const crumbs: { id: string; name: string; cabinetId: string }[] = [
          { id: folder.id, name: folder.name, cabinetId: folder.cabinetId }
        ]
        let parentId = folder.parentFolderId
        while (parentId) {
          try {
            const parentRes = await foldersApi.getById(parentId)
            const parent = parentRes.data
            crumbs.unshift({ id: parent.id, name: parent.name, cabinetId: parent.cabinetId })
            parentId = parent.parentFolderId
          } catch {
            break
          }
        }
        folderBreadcrumb.value = crumbs
        cabinetId.value = folder.cabinetId

        // Fetch cabinet name
        try {
          const cabRes = await cabinetsApi.getById(folder.cabinetId)
          cabinetName.value = cabRes.data.name
        } catch {
          // Cabinet access might be restricted
        }
      } catch {
        // Folder access might be restricted, ignore
      }
    }

    if (metadata.value.length > 0) {
      const contentTypeId = metadata.value[0]?.contentTypeId
      if (contentTypeId) {
        try {
          const ctResponse = await contentTypeDefinitionsApi.getById(contentTypeId)
          contentTypeInfo.value = ctResponse.data
        } catch (e) {
        }
      }
    }

    if (document.value.isCheckedOut) {
      await loadWorkingCopy()
    }
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to load document'
  } finally {
    isLoading.value = false
  }
}

async function loadWorkingCopy() {
  if (!document.value) return
  try {
    const response = await documentsApi.getWorkingCopy(document.value.id)
    workingCopy.value = response.data
  } catch (err: any) {
    workingCopy.value = null
  }
  initializeDraftValues()
}

function initializeDraftValues() {
  if (!document.value) return
  const wc = workingCopy.value

  draftName.value = wc?.draftName || document.value.name || ''
  draftDescription.value = wc?.draftDescription || document.value.description || ''
  draftClassificationId.value = wc?.draftClassificationId || document.value.classificationId || null
  draftImportanceId.value = wc?.draftImportanceId || document.value.importanceId || null
  draftDocumentTypeId.value = wc?.draftDocumentTypeId || document.value.documentTypeId || null
  draftExpiryDate.value = wc?.draftExpiryDateChanged ? (wc.draftExpiryDate ? formatDateForInput(wc.draftExpiryDate) : '') : (document.value.expiryDate ? formatDateForInput(document.value.expiryDate) : '')
  draftPrivacyLevelId.value = wc?.draftPrivacyLevelChanged ? (wc.draftPrivacyLevelId || null) : (document.value.privacyLevelId || null)

  if (wc?.draftMetadata && wc.draftMetadata.length > 0) {
    draftMetadata.value = wc.draftMetadata.map(m => {
      const fieldType = getFieldType(m.fieldId)
      let displayValue = m.value || ''
      if (isDateFieldType(fieldType) && m.dateValue) {
        displayValue = formatDateForInput(m.dateValue)
      } else if (isNumberFieldType(fieldType) && m.numericValue !== undefined && m.numericValue !== null) {
        displayValue = String(m.numericValue)
      }
      return {
        fieldId: m.fieldId,
        fieldName: m.fieldName,
        value: displayValue,
        numericValue: m.numericValue,
        dateValue: m.dateValue,
        fieldType: fieldType
      }
    })
  } else if (metadata.value.length > 0) {
    draftMetadata.value = metadata.value.map((m: any) => {
      const fieldType = getFieldType(m.fieldId)
      let displayValue = m.value || ''
      if (isDateFieldType(fieldType) && m.dateValue) {
        displayValue = formatDateForInput(m.dateValue)
      } else if (isNumberFieldType(fieldType) && m.numericValue !== undefined && m.numericValue !== null) {
        displayValue = String(m.numericValue)
      }
      return {
        fieldId: m.fieldId,
        fieldName: m.fieldName,
        value: displayValue,
        numericValue: m.numericValue,
        dateValue: m.dateValue ? formatDateForInput(m.dateValue) : undefined,
        fieldType: fieldType
      }
    })
  }
}

function getFieldType(fieldId: string): string {
  const fieldDefs = contentTypeInfo.value?.fields || []
  const field = fieldDefs.find((f: any) => f.id === fieldId)
  return field?.fieldType || 'Text'
}

function isTextFieldType(fieldType: string): boolean {
  const type = fieldType?.toLowerCase() || ''
  return type === 'text' || type === 'shorttext' || type === 'string'
}

function isTextAreaFieldType(fieldType: string): boolean {
  const type = fieldType?.toLowerCase() || ''
  return type === 'longtext' || type === 'textarea' || type === 'multilinetext'
}

function isNumberFieldType(fieldType: string): boolean {
  const type = fieldType?.toLowerCase() || ''
  return type === 'number' || type === 'decimal' || type === 'currency' || type === 'integer' || type === 'int'
}

function isDateFieldType(fieldType: string): boolean {
  const type = fieldType?.toLowerCase() || ''
  return type === 'date' || type === 'datetime' || type === 'datetimepicker'
}

function formatDateForInput(dateStr: string): string {
  try {
    return new Date(dateStr).toISOString().split('T')[0]
  } catch {
    return ''
  }
}

async function saveDraft() {
  if (!document.value) return
  isSavingDraft.value = true
  draftSaveMessage.value = null

  try {
    const metadataToSave = draftMetadata.value.map(m => {
      const result: {
        fieldId: string
        fieldName: string
        value?: string
        numericValue?: number
        dateValue?: string
      } = {
        fieldId: m.fieldId,
        fieldName: m.fieldName
      }

      if (isDateFieldType(m.fieldType)) {
        if (m.value) {
          result.dateValue = m.value
        }
      } else if (isNumberFieldType(m.fieldType)) {
        const num = parseFloat(m.value)
        if (!isNaN(num)) {
          result.numericValue = num
        }
      } else {
        result.value = m.value || undefined
      }

      return result
    })

    await documentsApi.saveWorkingCopy(document.value.id, {
      name: draftName.value,
      description: draftDescription.value,
      classificationId: draftClassificationId.value || undefined,
      importanceId: draftImportanceId.value || undefined,
      documentTypeId: draftDocumentTypeId.value || undefined,
      expiryDate: draftExpiryDate.value || null,
      expiryDateChanged: true,
      privacyLevelId: draftPrivacyLevelId.value || null,
      privacyLevelChanged: true,
      metadata: metadataToSave
    })

    draftSaveMessage.value = { type: 'success', text: 'Draft saved successfully' }
    setTimeout(() => { draftSaveMessage.value = null }, 3000)
  } catch (err: any) {
    draftSaveMessage.value = { type: 'error', text: err.response?.data?.errors?.[0] || 'Failed to save draft' }
  } finally {
    isSavingDraft.value = false
  }
}

async function uploadDraftFile() {
  if (!document.value || !draftFile.value) return
  isUploadingFile.value = true
  draftSaveMessage.value = null

  try {
    await documentsApi.saveWorkingCopyContent(document.value.id, draftFile.value)
    draftSaveMessage.value = { type: 'success', text: 'File uploaded to draft' }
    await loadWorkingCopy()
    draftFile.value = null
    setTimeout(() => { draftSaveMessage.value = null }, 3000)
  } catch (err: any) {
    draftSaveMessage.value = { type: 'error', text: err.response?.data?.errors?.[0] || 'Failed to upload file' }
  } finally {
    isUploadingFile.value = false
  }
}

function handleDraftFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    draftFile.value = input.files[0]
  }
}

function triggerFileInput() {
  fileInputRef.value?.click()
}

function handleFileDrop(event: DragEvent) {
  isDragging.value = false
  const files = event.dataTransfer?.files
  if (files && files.length > 0) {
    draftFile.value = files[0]
  }
}

async function loadMyPermissionLevel(documentId: string) {
  const levels = [
    PermissionLevels.Admin,
    PermissionLevels.Delete,
    PermissionLevels.Write,
    PermissionLevels.Read
  ]

  for (const level of levels) {
    try {
      const response = await permissionsApi.checkPermission('Document', documentId, level)
      if (response.data === true || response.data?.hasPermission === true) {
        myPermissionLevel.value = level
        return
      }
    } catch {
    }
  }
  myPermissionLevel.value = PermissionLevels.Read
}

async function loadActivities() {
  if (!document.value) return
  try {
    const response = await activityLogsApi.getByNode('Document', document.value.id)
    activities.value = response.data.items
  } catch (err) {
  }
}

async function loadReferenceData() {
  try {
    const [classRes, impRes, dtRes, plRes] = await Promise.all([
      referenceDataApi.getClassifications(),
      referenceDataApi.getImportances(),
      referenceDataApi.getDocumentTypes(),
      privacyLevelsApi.getAll()
    ])
    classifications.value = classRes.data || []
    importances.value = impRes.data || []
    documentTypes.value = dtRes.data || []
    privacyLevels.value = (plRes.data || []).filter((p: PrivacyLevel) => p.isActive)
  } catch { /* silently fail */ }
}

function handleSidebarTabChange(tab: typeof sidebarTab.value) {
  sidebarTab.value = tab
  if (tab === 'activity' && activities.value.length === 0) {
    loadActivities()
  }
  if (tab === 'edit' && classifications.value.length === 0) {
    loadReferenceData()
  }
}

function openPermissions() {
  showPermissionModal.value = true
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString()
}

function formatDateShort(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
}

function formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('en-US', {
    hour: '2-digit',
    minute: '2-digit'
  })
}

function getRelativeTime(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins} min ago`
  if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`
  if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`
  return formatDateShort(dateStr)
}

function getActivityIcon(action: string): string {
  const actionLower = action.toLowerCase()
  if (actionLower.includes('create')) return 'add_circle'
  if (actionLower.includes('upload')) return 'upload'
  if (actionLower.includes('download')) return 'download'
  if (actionLower.includes('edit') || actionLower.includes('update')) return 'edit'
  if (actionLower.includes('delete')) return 'delete'
  if (actionLower.includes('checkout') || actionLower === 'checkedout') return 'lock'
  if (actionLower.includes('checkin') || actionLower === 'checkedin') return 'lock_open'
  if (actionLower.includes('discard')) return 'lock_reset'
  if (actionLower.includes('share')) return 'share'
  if (actionLower.includes('version') || actionLower.includes('restore')) return 'history'
  if (actionLower.includes('view')) return 'visibility'
  return 'info'
}

function formatActionLabel(action: string): string {
  const actionMap: Record<string, string> = {
    'CheckedOut': 'Checkout file',
    'CheckedIn': 'Check in file',
    'DiscardedCheckout': 'Discard checkout',
    'Created': 'Upload',
    'Updated': 'Update properties',
    'Deleted': 'Delete',
    'Moved': 'Move',
    'Copied': 'Copy',
    'Downloaded': 'Download',
    'Viewed': 'View properties',
    'Shared': 'Share',
    'VersionRestored': 'Restore version',
    'DraftSaved': 'Save draft',
    'MetadataUpdated': 'Update metadata'
  }
  return actionMap[action] || action.replace(/([A-Z])/g, ' $1').trim()
}

async function handleCheckout() {
  if (!document.value) return
  try {
    await documentsApi.checkout(document.value.id)
    document.value.isCheckedOut = true
    await loadWorkingCopy()
    loadReferenceData()
    sidebarTab.value = 'edit'
  } catch (err: any) {
    alert(err.response?.data?.errors?.[0] || 'Checkout failed')
  }
}

async function handleDiscardCheckout() {
  if (!document.value || !confirm('Are you sure you want to discard this checkout? All draft changes will be lost.')) return
  try {
    await documentsApi.discardCheckout(document.value.id)
    document.value.isCheckedOut = false
    document.value.checkedOutBy = undefined
    workingCopy.value = null
    sidebarTab.value = 'info'
  } catch (err: any) {
    alert(err.response?.data?.errors?.[0] || 'Discard checkout failed')
  }
}

function openCheckinModal() {
  checkinFile.value = null
  checkinComment.value = ''
  checkinChangeDescription.value = ''
  checkinType.value = CheckInType.Minor
  keepCheckedOut.value = false
  showCheckinModal.value = true
}

async function handleCheckin() {
  if (!document.value) return
  isCheckingIn.value = true
  try {
    const metadataToSave = draftMetadata.value.map(m => {
      const result: {
        fieldId: string
        fieldName: string
        value?: string
        numericValue?: number
        dateValue?: string
      } = {
        fieldId: m.fieldId,
        fieldName: m.fieldName
      }

      if (isDateFieldType(m.fieldType)) {
        if (m.value) {
          result.dateValue = m.value
        }
      } else if (isNumberFieldType(m.fieldType)) {
        const num = parseFloat(m.value)
        if (!isNaN(num)) {
          result.numericValue = num
        }
      } else {
        result.value = m.value || undefined
      }

      return result
    })

    await documentsApi.saveWorkingCopy(document.value.id, {
      name: draftName.value,
      description: draftDescription.value,
      classificationId: draftClassificationId.value || undefined,
      importanceId: draftImportanceId.value || undefined,
      documentTypeId: draftDocumentTypeId.value || undefined,
      expiryDate: draftExpiryDate.value || null,
      expiryDateChanged: true,
      privacyLevelId: draftPrivacyLevelId.value || null,
      privacyLevelChanged: true,
      metadata: metadataToSave
    })

    const fileToCheckin = checkinFile.value || draftFile.value || undefined

    await documentsApi.checkin(document.value.id, {
      file: fileToCheckin,
      comment: checkinComment.value || undefined,
      checkInType: checkinType.value,
      keepCheckedOut: keepCheckedOut.value,
      changeDescription: checkinChangeDescription.value || undefined
    })
    showCheckinModal.value = false
    checkinFile.value = null
    draftFile.value = null
    checkinComment.value = ''
    checkinChangeDescription.value = ''
    sidebarTab.value = 'info'
    await loadDocument()
  } catch (err: any) {
    alert(err.response?.data?.errors?.[0] || 'Check-in failed')
  } finally {
    isCheckingIn.value = false
  }
}

function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    checkinFile.value = input.files[0]
  }
}

async function handleDownload(version?: number) {
  if (!document.value) return
  try {
    const response = await documentsApi.download(document.value.id, version)
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = window.document.createElement('a')
    link.href = url
    link.setAttribute('download', document.value.name + (document.value.extension || ''))
    window.document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (err) {
  }
}

async function handleDelete() {
  if (!document.value || !confirm('Are you sure you want to delete this document?')) return
  try {
    await documentsApi.delete(document.value.id)
    router.push('/explorer')
  } catch (err: any) {
    alert(err.response?.data?.errors?.[0] || 'Delete failed')
  }
}

// Version Comparison
function openCompareModal() {
  if (versions.value.length < 2) {
    alert('Need at least 2 versions to compare')
    return
  }
  compareSourceVersion.value = versions.value[1]?.id || ''
  compareTargetVersion.value = versions.value[0]?.id || ''
  versionComparison.value = null
  showCompareModal.value = true
}

async function compareVersions() {
  if (!document.value || !compareSourceVersion.value || !compareTargetVersion.value) return
  isComparing.value = true
  try {
    const response = await documentsApi.compareVersions(
      document.value.id,
      compareSourceVersion.value,
      compareTargetVersion.value
    )
    versionComparison.value = response.data
  } catch (err: any) {
    alert(err.response?.data?.errors?.[0] || 'Failed to compare versions')
  } finally {
    isComparing.value = false
  }
}

async function quickCompareWithCurrent(sourceVersionId: string) {
  if (!document.value || versions.value.length < 2) return
  const currentVersion = versions.value.find(v => v.versionNumber === document.value?.currentVersion)
  if (!currentVersion) return

  compareSourceVersion.value = sourceVersionId
  compareTargetVersion.value = currentVersion.id
  versionComparison.value = null
  showCompareModal.value = true
  await compareVersions()
}

const restoreVersionInfo = computed(() => {
  if (!restoreVersionId.value) return null
  return versions.value.find(v => v.id === restoreVersionId.value)
})

const restoreComparison = ref<VersionComparison | null>(null)
const isLoadingRestorePreview = ref(false)

async function openRestoreModal(versionId: string) {
  restoreVersionId.value = versionId
  restoreComment.value = ''
  restoreContent.value = true
  restoreMetadata.value = true
  restoreComparison.value = null
  showRestoreModal.value = true
  await loadRestorePreview(versionId)
}

async function loadRestorePreview(versionId: string) {
  if (!document.value) return
  const currentVersion = versions.value.find(v => v.versionNumber === document.value?.currentVersion)
  if (!currentVersion) return

  isLoadingRestorePreview.value = true
  try {
    const response = await documentsApi.compareVersions(
      document.value.id,
      currentVersion.id,
      versionId
    )
    restoreComparison.value = response.data
  } catch (err) {
  } finally {
    isLoadingRestorePreview.value = false
  }
}

async function handleRestore() {
  if (!document.value || !restoreVersionId.value) return
  isRestoring.value = true
  try {
    await documentsApi.restoreVersion(document.value.id, restoreVersionId.value, {
      comment: restoreComment.value || undefined,
      restoreContent: restoreContent.value,
      restoreMetadata: restoreMetadata.value
    })
    showRestoreModal.value = false
    await loadDocument()
  } catch (err: any) {
    alert(err.response?.data?.errors?.[0] || 'Failed to restore version')
  } finally {
    isRestoring.value = false
  }
}

function getDiffTypeLabel(diffType: DiffType): string {
  switch (diffType) {
    case DiffType.Added: return 'Added'
    case DiffType.Removed: return 'Removed'
    case DiffType.Modified: return 'Modified'
    default: return 'Unchanged'
  }
}

function getDiffTypeClass(diffType: DiffType): string {
  switch (diffType) {
    case DiffType.Added: return 'bg-teal/10 text-teal'
    case DiffType.Removed: return 'bg-red-100 text-red-700'
    case DiffType.Modified: return 'bg-amber-100 text-amber-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}

function getVersionLabel(version: DocumentVersion): string {
  return version.versionLabel || `${version.majorVersion || 1}.${version.minorVersion || 0}`
}

function goBack() {
  if (cabinetId.value && document.value?.folderId) {
    router.push({ path: '/explorer', query: { cabinet: cabinetId.value, folder: document.value.folderId } })
  } else if (cabinetId.value) {
    router.push({ path: '/explorer', query: { cabinet: cabinetId.value } })
  } else {
    router.push('/explorer')
  }
}

function navigateToBreadcrumbFolder(folderId: string) {
  if (cabinetId.value) {
    router.push({ path: '/explorer', query: { cabinet: cabinetId.value, folder: folderId } })
  }
}

function navigateToCabinet() {
  if (cabinetId.value) {
    router.push({ path: '/explorer', query: { cabinet: cabinetId.value } })
  }
}

const metadataFields = computed(() => {
  if (!metadata.value || metadata.value.length === 0) return []
  const fieldDefs = contentTypeInfo.value?.fields || []
  return metadata.value.map((m: any) => {
    const fieldDef = fieldDefs.find((f: any) => f.id === m.fieldId)
    return {
      id: m.id,
      fieldName: m.fieldName,
      displayName: fieldDef?.displayName || m.fieldName,
      value: m.value || m.numericValue || (m.dateValue ? formatDate(m.dateValue) : '-'),
      fieldType: fieldDef?.fieldType || 'Text'
    }
  })
})

const versionSelectOptions = computed(() =>
  versions.value.map(v => ({
    value: v.id,
    label: `v${getVersionLabel(v)} - ${formatDate(v.createdAt)}`
  }))
)

// Auto-switch to edit tab on checkout, info tab on checkin
watch(isEditMode, (newVal) => {
  if (newVal) {
    sidebarTab.value = 'edit'
  }
})
</script>

<template>
  <div class="space-y-0">
    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-20">
      <div class="flex flex-col items-center gap-3">
        <div class="w-10 h-10 border-3 border-teal border-t-transparent rounded-full animate-spin"></div>
        <span class="text-sm text-zinc-500">Loading document...</span>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 p-4 rounded-lg">
      {{ error }}
    </div>

    <div v-else-if="document" class="h-[calc(100vh-128px)] flex flex-col">
      <!-- 1. Slim Header (Dark) -->
      <div class="shrink-0 bg-[#111318] dark:bg-[#0d1117] rounded-t-lg px-5 py-3">
        <div class="flex items-center justify-between">
          <!-- Left: Back + Document Info -->
          <div class="flex items-center gap-3 min-w-0">
            <button @click="goBack" class="p-1.5 rounded-lg text-zinc-400 hover:text-white hover:bg-white/10 transition-colors">
              <span class="material-symbols-outlined text-xl">arrow_back</span>
            </button>
            <div class="w-10 h-10 rounded-lg bg-teal/20 flex items-center justify-center flex-shrink-0">
              <span class="material-symbols-outlined text-teal text-xl">description</span>
            </div>
            <div class="min-w-0">
              <h1 class="text-sm font-bold text-white truncate flex items-center gap-2">
                {{ document.name }}{{ document.extension }}
                <div
                  v-if="document.hasPassword"
                  class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-gradient-to-r from-violet-400/20 to-fuchsia-400/20 border border-violet-400/40"
                  title="Password protected"
                >
                  <span class="material-symbols-outlined text-violet-300" style="font-size: 11px; font-variation-settings: 'FILL' 1;">shield_lock</span>
                  <span class="text-[8px] font-semibold text-violet-200 uppercase tracking-wide">Secured</span>
                </div>
              </h1>
              <div class="flex items-center gap-2 mt-0.5">
                <span class="text-zinc-500 text-xs">{{ document.extension?.replace('.', '').toUpperCase() || 'FILE' }} &middot; {{ formatSize(document.size) }}</span>
                <span
                  :class="isEditMode ? 'bg-teal/20 text-teal border-teal/30' : document.isCheckedOut ? 'bg-amber-500/20 text-amber-400 border-amber-500/30' : 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30'"
                  class="px-2 py-0.5 text-[9px] font-bold uppercase tracking-wider rounded-full border"
                >
                  {{ isEditMode ? 'Editing' : document.isCheckedOut ? 'Checked Out' : 'Available' }}
                </span>
                <span v-if="isEditMode && workingCopy?.hasDraftFile" class="px-2 py-0.5 text-[9px] font-bold uppercase tracking-wider rounded-full bg-teal/15 text-teal border border-teal/30">
                  Draft file
                </span>
                <span class="px-2 py-0.5 text-[9px] font-bold uppercase tracking-wider rounded-full bg-blue-500/20 text-blue-400 border border-blue-500/30">
                  v{{ document.currentMajorVersion || 1 }}.{{ document.currentMinorVersion || 0 }}
                </span>
              </div>
              <!-- Folder breadcrumb path -->
              <div v-if="cabinetName || folderBreadcrumb.length > 0" class="flex items-center gap-1 mt-1 text-[11px] text-zinc-500">
                <span class="material-symbols-outlined" style="font-size: 13px;">folder_open</span>
                <button v-if="cabinetName" @click="navigateToCabinet" class="hover:text-teal transition-colors">{{ cabinetName }}</button>
                <template v-for="(crumb, idx) in folderBreadcrumb" :key="crumb.id">
                  <span class="text-zinc-600">/</span>
                  <button @click="navigateToBreadcrumbFolder(crumb.id)" class="hover:text-teal transition-colors truncate max-w-[120px]" :title="crumb.name">{{ crumb.name }}</button>
                </template>
              </div>
            </div>
          </div>

          <!-- Right: Action Buttons -->
          <div class="flex items-center gap-1.5 flex-shrink-0">
            <!-- Edit mode actions -->
            <template v-if="isEditMode">
              <button
                @click="saveDraft"
                :disabled="isSavingDraft"
                class="px-3 py-1.5 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors flex items-center gap-1.5 text-xs font-medium disabled:opacity-50"
              >
                <span v-if="isSavingDraft" class="material-symbols-outlined animate-spin text-sm">refresh</span>
                <span v-else class="material-symbols-outlined text-sm">save</span>
                {{ isSavingDraft ? 'Saving...' : 'Save Draft' }}
              </button>
              <button
                @click="openCheckinModal"
                class="px-3 py-1.5 bg-blue-600 text-white rounded-lg hover:bg-blue-500 transition-colors flex items-center gap-1.5 text-xs font-medium"
              >
                <span class="material-symbols-outlined text-sm">check_circle</span>
                Check In
              </button>
              <button
                @click="handleDiscardCheckout"
                class="px-3 py-1.5 text-zinc-400 hover:text-white hover:bg-white/10 border border-zinc-600 rounded-lg transition-colors text-xs font-medium flex items-center gap-1.5"
              >
                <span class="material-symbols-outlined text-sm">undo</span>
                Discard
              </button>
              <div class="w-px h-5 bg-zinc-700 mx-0.5"></div>
            </template>
            <!-- Normal mode actions -->
            <button
              v-if="canWrite && !document.isCheckedOut"
              @click="handleCheckout"
              class="px-3 py-1.5 bg-zinc-700 hover:bg-zinc-600 text-white rounded-lg transition-colors flex items-center gap-1.5 text-xs font-medium border border-zinc-600"
            >
              <span class="material-symbols-outlined text-base">lock</span>
              Check Out
            </button>
            <span
              v-else-if="document.isCheckedOut && !isMyCheckout"
              class="px-2.5 py-1.5 text-[10px] text-amber-400 bg-amber-500/10 border border-amber-500/20 rounded-lg"
            >
              Checked out by another user
            </span>
            <button
              v-if="canRead"
              @click="handleDownload()"
              class="px-3 py-1.5 bg-teal hover:bg-teal/90 text-white rounded-lg transition-colors flex items-center gap-1.5 text-xs font-medium"
            >
              <span class="material-symbols-outlined text-base">download</span>
              Download
            </button>
            <button
              v-if="canRead"
              @click="showPreview = true"
              class="p-1.5 text-zinc-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
              title="Open full viewer"
            >
              <span class="material-symbols-outlined text-lg">open_in_new</span>
            </button>
            <button
              v-if="canDelete && !document.isCheckedOut"
              @click="handleDelete"
              class="p-1.5 text-zinc-500 hover:text-red-400 hover:bg-red-500/10 rounded-lg transition-colors"
              title="Delete document"
            >
              <span class="material-symbols-outlined text-lg">delete</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Draft save message (conditional, overlays top of content) -->
      <div v-if="draftSaveMessage" class="shrink-0 px-5 py-1.5 bg-[#111318] dark:bg-[#0d1117] border-t border-zinc-800">
        <div
          :class="draftSaveMessage.type === 'success' ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30' : 'bg-red-500/15 text-red-400 border border-red-500/30'"
          class="px-3 py-1.5 rounded-lg text-xs flex items-center gap-1.5"
        >
          <span class="material-symbols-outlined text-sm">{{ draftSaveMessage.type === 'success' ? 'check_circle' : 'error' }}</span>
          {{ draftSaveMessage.text }}
        </div>
      </div>

      <!-- 3. Main Content (flex row, fills viewport) -->
      <div class="flex-1 flex overflow-hidden border border-zinc-200 dark:border-border-dark border-t-0 rounded-b-lg">
        <!-- Left: Inline Preview (50%) -->
        <div class="w-1/2 shrink-0 min-w-0 relative bg-zinc-50 dark:bg-zinc-900/50">
          <InlineDocumentPreview :document="document" @open-full-viewer="showPreview = true" />
        </div>

        <!-- Right: Details Panel (dominant) -->
        <aside class="flex-1 border-l border-zinc-200 dark:border-border-dark flex flex-col bg-white dark:bg-background-dark">
          <!-- Tab navigation with labels -->
          <div class="shrink-0 border-b border-zinc-200 dark:border-border-dark px-4 flex gap-1">
            <button
              @click="handleSidebarTabChange('info')"
              class="px-3 py-2.5 flex items-center gap-1.5 transition-colors relative text-xs font-medium"
              :class="sidebarTab === 'info' ? 'text-teal' : 'text-zinc-400 hover:text-zinc-600'"
            >
              <span class="material-symbols-outlined text-base">info</span>
              Details
              <div v-if="sidebarTab === 'info'" class="absolute bottom-0 left-1 right-1 h-0.5 bg-teal rounded-full"></div>
            </button>
            <button
              @click="handleSidebarTabChange('meta')"
              class="px-3 py-2.5 flex items-center gap-1.5 transition-colors relative text-xs font-medium"
              :class="sidebarTab === 'meta' ? 'text-teal' : 'text-zinc-400 hover:text-zinc-600'"
            >
              <span class="material-symbols-outlined text-base">category</span>
              Metadata
              <div v-if="sidebarTab === 'meta'" class="absolute bottom-0 left-1 right-1 h-0.5 bg-teal rounded-full"></div>
            </button>
            <button
              @click="handleSidebarTabChange('versions')"
              class="px-3 py-2.5 flex items-center gap-1.5 transition-colors relative text-xs font-medium"
              :class="sidebarTab === 'versions' ? 'text-teal' : 'text-zinc-400 hover:text-zinc-600'"
            >
              <span class="material-symbols-outlined text-base">history</span>
              Versions
              <span class="ml-0.5 px-1.5 py-0 text-[9px] font-bold rounded-full bg-zinc-100 dark:bg-border-dark text-zinc-500">{{ versions.length }}</span>
              <div v-if="sidebarTab === 'versions'" class="absolute bottom-0 left-1 right-1 h-0.5 bg-teal rounded-full"></div>
            </button>
            <button
              v-if="canWrite"
              @click="handleSidebarTabChange('activity')"
              class="px-3 py-2.5 flex items-center gap-1.5 transition-colors relative text-xs font-medium"
              :class="sidebarTab === 'activity' ? 'text-teal' : 'text-zinc-400 hover:text-zinc-600'"
            >
              <span class="material-symbols-outlined text-base">timeline</span>
              Activity
              <div v-if="sidebarTab === 'activity'" class="absolute bottom-0 left-1 right-1 h-0.5 bg-teal rounded-full"></div>
            </button>
            <button
              v-if="isEditMode"
              @click="handleSidebarTabChange('edit')"
              class="px-3 py-2.5 flex items-center gap-1.5 transition-colors relative text-xs font-medium"
              :class="sidebarTab === 'edit' ? 'text-teal' : 'text-zinc-400 hover:text-zinc-600'"
            >
              <span class="material-symbols-outlined text-base">edit_document</span>
              Edit
              <div v-if="sidebarTab === 'edit'" class="absolute bottom-0 left-1 right-1 h-0.5 bg-teal rounded-full"></div>
            </button>
          </div>

          <!-- Scrollable tab content -->
          <div class="flex-1 overflow-y-auto">
            <!-- ============ INFO TAB ============ -->
            <div v-if="sidebarTab === 'info'" class="p-4 space-y-4">
              <!-- Document Properties -->
              <div>
                <h3 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-3">Document Properties</h3>
                <dl class="space-y-3">
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">File Type</dt>
                    <dd class="text-sm font-medium text-zinc-800 dark:text-white">{{ document.extension?.replace('.', '').toUpperCase() || '-' }}</dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Content Type</dt>
                    <dd class="text-sm font-medium text-zinc-800 dark:text-white">{{ document.contentTypeName || document.contentType || '-' }}</dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Size</dt>
                    <dd class="text-sm font-medium text-zinc-800 dark:text-white">{{ formatSize(document.size) }}</dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Version</dt>
                    <dd class="text-sm font-medium text-zinc-800 dark:text-white">
                      {{ document.currentMajorVersion || 1 }}.{{ document.currentMinorVersion || 0 }}
                      <span class="text-[9px] text-zinc-400 ml-1">(rev {{ document.currentVersion }})</span>
                    </dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Status</dt>
                    <dd>
                      <span
                        :class="document.isCheckedOut ? 'bg-amber-100 text-amber-700' : 'bg-emerald-100 text-emerald-700'"
                        class="px-2 py-0.5 text-xs font-medium rounded"
                      >
                        {{ document.isCheckedOut ? 'Checked Out' : 'Available' }}
                      </span>
                    </dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Created</dt>
                    <dd class="text-sm font-medium text-zinc-800 dark:text-white text-right">
                      <div>{{ formatDateShort(document.createdAt) }}</div>
                      <div class="text-[10px] text-zinc-400">{{ document.createdByName || 'System' }}</div>
                    </dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Modified</dt>
                    <dd class="text-sm font-medium text-zinc-800 dark:text-white text-right">
                      <div>{{ document.modifiedAt ? getRelativeTime(document.modifiedAt) : '-' }}</div>
                    </dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Permission</dt>
                    <dd>
                      <span
                        class="px-2 py-0.5 text-xs font-medium rounded flex items-center gap-1"
                        :class="{
                          'bg-purple-100 text-purple-700': canAdmin,
                          'bg-red-100 text-red-700': canDelete && !canAdmin,
                          'bg-teal/10 text-teal': canWrite && !canDelete,
                          'bg-zinc-100 text-zinc-600': canRead && !canWrite
                        }"
                      >
                        <span class="material-symbols-outlined text-xs">
                          {{ canAdmin ? 'shield_person' : canDelete ? 'delete' : canWrite ? 'edit' : 'visibility' }}
                        </span>
                        {{ canAdmin ? 'Admin' : canDelete ? 'Delete' : canWrite ? 'Write' : 'Read' }}
                      </span>
                    </dd>
                  </div>
                  <div class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Expiry Date</dt>
                    <dd>
                      <span
                        v-if="document.expiryDate"
                        class="px-2 py-0.5 text-xs font-medium rounded-full inline-flex items-center gap-1"
                        :class="{
                          'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400': new Date(document.expiryDate) <= new Date(),
                          'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-400': new Date(document.expiryDate) > new Date() && new Date(document.expiryDate) <= new Date(Date.now() + 7 * 86400000),
                          'bg-zinc-100 text-zinc-600 dark:bg-zinc-700 dark:text-zinc-300': new Date(document.expiryDate) > new Date(Date.now() + 7 * 86400000)
                        }"
                      >
                        <span class="material-symbols-outlined text-xs">
                          {{ new Date(document.expiryDate) <= new Date() ? 'event_busy' : 'event' }}
                        </span>
                        {{ new Date(document.expiryDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) }}
                        <template v-if="new Date(document.expiryDate) <= new Date()"> &middot; Expired</template>
                        <template v-else-if="new Date(document.expiryDate) <= new Date(Date.now() + 7 * 86400000)"> &middot; Expiring Soon</template>
                      </span>
                      <span v-else class="text-sm text-zinc-400">-</span>
                    </dd>
                  </div>
                  <div v-if="document.privacyLevelName || folderPrivacyLevelName" class="flex items-center justify-between">
                    <dt class="text-xs text-zinc-500">Privacy</dt>
                    <dd class="flex items-center gap-1.5">
                      <span
                        v-if="document.privacyLevelName"
                        class="px-2 py-0.5 text-xs font-semibold rounded-full flex items-center gap-1"
                        :style="{
                          backgroundColor: (document.privacyLevelColor || '#6b7280') + '18',
                          color: document.privacyLevelColor || '#6b7280'
                        }"
                      >
                        <span class="material-symbols-outlined text-xs">shield</span>
                        {{ document.privacyLevelName }}
                      </span>
                      <span
                        v-else-if="folderPrivacyLevelName"
                        class="px-2 py-0.5 text-xs font-semibold rounded-full flex items-center gap-1"
                        :style="{
                          backgroundColor: (folderPrivacyLevelColor || '#6b7280') + '18',
                          color: folderPrivacyLevelColor || '#6b7280'
                        }"
                      >
                        <span class="material-symbols-outlined text-xs">shield</span>
                        {{ folderPrivacyLevelName }}
                        <span class="text-[8px] opacity-60">(folder)</span>
                      </span>
                    </dd>
                  </div>
                </dl>
              </div>

              <!-- Description -->
              <div class="pt-2 border-t border-zinc-100 dark:border-border-dark">
                <h3 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-2">Description</h3>
                <p class="text-sm text-zinc-600 dark:text-zinc-400 leading-relaxed">
                  {{ document.description || 'No description.' }}
                </p>
              </div>

              <!-- Quick Actions -->
              <div class="pt-2 border-t border-zinc-100 dark:border-border-dark">
                <h3 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-2">Quick Actions</h3>
                <div class="space-y-1">
                  <button
                    @click="handleDownload()"
                    class="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-zinc-600 dark:text-zinc-300 hover:bg-teal/10 hover:text-teal transition-all text-sm"
                  >
                    <span class="material-symbols-outlined text-lg">download</span>
                    Download File
                  </button>
                  <button
                    @click="showPreview = true"
                    class="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-zinc-600 dark:text-zinc-300 hover:bg-teal/10 hover:text-teal transition-all text-sm"
                  >
                    <span class="material-symbols-outlined text-lg">open_in_new</span>
                    Open Full Viewer
                  </button>
                  <button
                    v-if="canWrite && !document.isCheckedOut"
                    @click="handleCheckout"
                    class="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-zinc-600 dark:text-zinc-300 hover:bg-teal/10 hover:text-teal transition-all text-sm"
                  >
                    <span class="material-symbols-outlined text-lg">lock</span>
                    Check Out for Editing
                  </button>
                  <button
                    v-if="canAdmin"
                    @click="openPermissions"
                    class="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-zinc-600 dark:text-zinc-300 hover:bg-teal/10 hover:text-teal transition-all text-sm"
                  >
                    <span class="material-symbols-outlined text-lg">shield_person</span>
                    Manage Permissions
                  </button>
                </div>
              </div>
            </div>

            <!-- ============ META TAB ============ -->
            <div v-else-if="sidebarTab === 'meta'" class="p-4 space-y-4">
              <template v-if="metadataFields.length > 0">
                <div class="flex items-center gap-2 mb-1">
                  <h3 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">{{ contentTypeInfo?.name || 'Metadata' }}</h3>
                </div>
                <div class="space-y-2">
                  <div
                    v-for="field in metadataFields"
                    :key="field.id"
                    class="p-3 bg-zinc-50 dark:bg-surface-dark rounded-lg"
                  >
                    <dt class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-1">{{ field.displayName }}</dt>
                    <dd class="text-sm font-medium text-zinc-800 dark:text-white break-words">{{ field.value || '-' }}</dd>
                  </div>
                </div>
              </template>
              <div v-else class="py-8 text-center">
                <div class="w-14 h-14 mx-auto mb-3 bg-zinc-100 dark:bg-surface-dark rounded-full flex items-center justify-center">
                  <span class="material-symbols-outlined text-2xl text-zinc-300">category</span>
                </div>
                <p class="text-sm text-zinc-400">No metadata associated with this document.</p>
              </div>
            </div>

            <!-- ============ VERSIONS TAB ============ -->
            <div v-else-if="sidebarTab === 'versions'" class="p-4 space-y-3">
              <!-- Current version highlight -->
              <div class="p-3 bg-teal/5 border border-teal/20 rounded-lg flex items-center gap-3">
                <div class="w-10 h-10 rounded-lg bg-teal/20 flex items-center justify-center flex-shrink-0">
                  <span class="material-symbols-outlined text-teal text-lg">verified</span>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-[10px] font-bold text-teal uppercase tracking-wider">Current</p>
                  <p class="text-lg font-bold text-zinc-900 dark:text-white">v{{ document.currentMajorVersion || 1 }}.{{ document.currentMinorVersion || 0 }}</p>
                </div>
                <span class="text-xs text-zinc-400">{{ versions.length }} total</span>
              </div>

              <!-- Version cards -->
              <div
                v-for="version in versions"
                :key="version.id"
                class="p-3 rounded-lg border transition-all hover:border-teal/30"
                :class="version.versionNumber === document.currentVersion
                  ? 'border-teal/30 bg-teal/5'
                  : 'border-zinc-200 dark:border-border-dark bg-white dark:bg-background-dark'"
              >
                <div class="flex items-start gap-3">
                  <!-- Version Badge -->
                  <div
                    class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0"
                    :class="version.versionType === 'Major'
                      ? 'bg-gradient-to-br from-purple-500 to-purple-700'
                      : 'bg-gradient-to-br from-zinc-500 to-zinc-700'"
                  >
                    <span class="text-white font-bold text-xs">{{ getVersionLabel(version) }}</span>
                  </div>
                  <!-- Info -->
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-1.5 mb-0.5">
                      <span class="text-sm font-bold text-zinc-900 dark:text-white">v{{ getVersionLabel(version) }}</span>
                      <span
                        :class="version.versionType === 'Major'
                          ? 'bg-purple-100 text-purple-700'
                          : 'bg-zinc-100 text-zinc-600'"
                        class="px-1.5 py-0 rounded text-[9px] font-bold uppercase"
                      >
                        {{ version.versionType || 'Minor' }}
                      </span>
                      <span
                        v-if="version.versionNumber === document.currentVersion"
                        class="px-1.5 py-0 rounded text-[9px] font-bold uppercase bg-teal/10 text-teal"
                      >
                        Current
                      </span>
                    </div>
                    <p class="text-xs text-zinc-500">{{ formatDateShort(version.createdAt) }} &middot; {{ formatSize(version.size) }}</p>
                    <p v-if="version.comment || version.changeDescription" class="text-xs text-zinc-600 dark:text-zinc-300 mt-1 line-clamp-2">
                      {{ version.comment || version.changeDescription }}
                    </p>
                    <!-- Change badges -->
                    <div class="flex items-center gap-1 mt-1.5">
                      <span
                        v-if="version.isContentChanged"
                        class="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-bold uppercase rounded bg-blue-100 text-blue-600"
                      >
                        <span class="material-symbols-outlined" style="font-size: 10px;">description</span>
                        Content
                      </span>
                      <span
                        v-if="version.isMetadataChanged"
                        class="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[9px] font-bold uppercase rounded bg-emerald-100 text-emerald-600"
                      >
                        <span class="material-symbols-outlined" style="font-size: 10px;">category</span>
                        Meta
                      </span>
                    </div>
                  </div>
                </div>
                <!-- Actions -->
                <div class="flex items-center gap-1 mt-2 pt-2 border-t border-zinc-100 dark:border-border-dark">
                  <button
                    @click="handleDownload(version.versionNumber)"
                    class="flex-1 py-1.5 text-xs text-zinc-500 hover:text-teal hover:bg-teal/5 rounded-lg transition-colors flex items-center justify-center gap-1"
                  >
                    <span class="material-symbols-outlined text-sm">download</span>
                    Download
                  </button>
                  <button
                    v-if="version.versionNumber !== document.currentVersion && versions.length > 1"
                    @click="quickCompareWithCurrent(version.id)"
                    class="flex-1 py-1.5 text-xs text-zinc-500 hover:text-purple-500 hover:bg-purple-50 rounded-lg transition-colors flex items-center justify-center gap-1"
                  >
                    <span class="material-symbols-outlined text-sm">compare</span>
                    Compare
                  </button>
                  <button
                    v-if="canWrite && !document.isCheckedOut && version.versionNumber !== document.currentVersion"
                    @click="openRestoreModal(version.id)"
                    class="flex-1 py-1.5 text-xs text-zinc-500 hover:text-amber-500 hover:bg-amber-50 rounded-lg transition-colors flex items-center justify-center gap-1"
                  >
                    <span class="material-symbols-outlined text-sm">settings_backup_restore</span>
                    Restore
                  </button>
                </div>
              </div>

              <!-- Empty state -->
              <div v-if="versions.length === 0" class="py-8 text-center">
                <div class="w-14 h-14 mx-auto mb-3 bg-zinc-100 dark:bg-surface-dark rounded-full flex items-center justify-center">
                  <span class="material-symbols-outlined text-2xl text-zinc-300">history</span>
                </div>
                <p class="text-sm text-zinc-400">No version history available.</p>
              </div>

              <!-- Compare action -->
              <div v-if="versions.length > 1" class="pt-2">
                <button
                  @click="openCompareModal"
                  class="w-full py-2 text-xs font-medium text-zinc-500 hover:text-teal bg-zinc-50 dark:bg-surface-dark hover:bg-teal/5 rounded-lg transition-colors flex items-center justify-center gap-1.5"
                >
                  <span class="material-symbols-outlined text-sm">compare_arrows</span>
                  Compare Any Two Versions
                </button>
              </div>
            </div>

            <!-- ============ ACTIVITY TAB ============ -->
            <div v-else-if="sidebarTab === 'activity'" class="p-4">
              <div v-if="activities.length === 0" class="py-8 text-center">
                <div class="w-14 h-14 mx-auto mb-3 bg-zinc-100 dark:bg-surface-dark rounded-full flex items-center justify-center">
                  <span class="material-symbols-outlined text-2xl text-zinc-300">timeline</span>
                </div>
                <p class="text-sm text-zinc-400">No activity recorded.</p>
              </div>
              <div v-else class="space-y-1.5">
                <div
                  v-for="activity in activities"
                  :key="activity.id"
                  class="flex items-start gap-3 p-3 rounded-lg hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors"
                >
                  <div class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                    :class="{
                      'bg-blue-100': activity.action === 'CheckedOut',
                      'bg-green-100': activity.action === 'CheckedIn',
                      'bg-amber-100': activity.action === 'DiscardedCheckout',
                      'bg-purple-100': activity.action === 'Created',
                      'bg-teal/10': !['CheckedOut', 'CheckedIn', 'DiscardedCheckout', 'Created'].includes(activity.action)
                    }">
                    <span class="material-symbols-outlined text-base"
                      :class="{
                        'text-blue-600': activity.action === 'CheckedOut',
                        'text-green-600': activity.action === 'CheckedIn',
                        'text-amber-600': activity.action === 'DiscardedCheckout',
                        'text-purple-600': activity.action === 'Created',
                        'text-teal': !['CheckedOut', 'CheckedIn', 'DiscardedCheckout', 'Created'].includes(activity.action)
                      }">{{ getActivityIcon(activity.action) }}</span>
                  </div>
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-semibold text-zinc-800 dark:text-white">{{ formatActionLabel(activity.action) }}</p>
                    <p class="text-xs text-zinc-500 mt-0.5">{{ activity.userName || 'Unknown user' }}</p>
                    <p v-if="activity.details" class="text-xs text-zinc-400 mt-0.5">{{ activity.details }}</p>
                  </div>
                  <div class="text-right flex-shrink-0">
                    <p class="text-[10px] font-medium text-zinc-500">{{ formatTime(activity.createdAt) }}</p>
                    <p class="text-[10px] text-zinc-400">{{ formatDateShort(activity.createdAt) }}</p>
                  </div>
                </div>
              </div>
            </div>

            <!-- ============ EDIT TAB ============ -->
            <div v-else-if="sidebarTab === 'edit' && isEditMode" class="p-5 space-y-5">
              <!-- File Replacement (compact) -->
              <div>
                <h3 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-2">File</h3>
                <div class="p-3 bg-zinc-50 dark:bg-surface-dark rounded-lg">
                  <div class="flex items-center gap-2.5">
                    <div class="w-8 h-8 rounded-lg bg-zinc-200 dark:bg-border-dark flex items-center justify-center flex-shrink-0">
                      <span class="material-symbols-outlined text-zinc-500 text-base">description</span>
                    </div>
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-medium text-zinc-800 dark:text-white truncate">{{ document.name }}{{ document.extension }}</p>
                      <p class="text-[11px] text-zinc-500">{{ formatSize(document.size) }}</p>
                    </div>
                  </div>
                  <div v-if="workingCopy?.hasDraftFile" class="mt-2 pt-2 border-t border-zinc-200 dark:border-border-dark flex items-center gap-2.5">
                    <div class="w-8 h-8 rounded-lg bg-teal/20 flex items-center justify-center flex-shrink-0">
                      <span class="material-symbols-outlined text-teal text-base">draft</span>
                    </div>
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-medium text-teal truncate">{{ workingCopy.draftFileName }}</p>
                      <p class="text-[11px] text-zinc-500">Draft &middot; {{ formatSize(workingCopy.draftSize || 0) }}</p>
                    </div>
                    <span class="material-symbols-outlined text-teal text-base">check_circle</span>
                  </div>
                </div>
                <!-- Drop zone -->
                <div
                  class="mt-2 relative border-2 border-dashed rounded-lg p-4 text-center transition-all cursor-pointer hover:border-teal hover:bg-teal/5"
                  :class="isDragging ? 'border-teal bg-teal/10' : 'border-zinc-300 dark:border-border-dark'"
                  @dragenter.prevent="isDragging = true"
                  @dragover.prevent="isDragging = true"
                  @dragleave.prevent="isDragging = false"
                  @drop.prevent="handleFileDrop"
                  @click="triggerFileInput"
                >
                  <input ref="fileInputRef" type="file" @change="handleDraftFileSelect" class="hidden" />
                  <div class="flex items-center justify-center gap-2">
                    <span class="material-symbols-outlined text-lg" :class="isDragging ? 'text-teal' : 'text-zinc-400'">cloud_upload</span>
                    <p class="text-xs text-zinc-500"><span class="text-teal font-medium">Click to upload</span> or drag and drop</p>
                  </div>
                </div>
                <!-- Selected file -->
                <div v-if="draftFile" class="mt-2 p-2.5 bg-teal/5 border border-teal/20 rounded-lg">
                  <div class="flex items-center gap-2">
                    <span class="material-symbols-outlined text-teal text-base">insert_drive_file</span>
                    <span class="text-sm font-medium text-zinc-800 dark:text-white truncate flex-1">{{ draftFile.name }}</span>
                    <button @click.stop="draftFile = null" class="p-0.5 text-zinc-400 hover:text-red-500 rounded transition-colors">
                      <span class="material-symbols-outlined text-sm">close</span>
                    </button>
                  </div>
                  <button
                    @click.stop="uploadDraftFile"
                    :disabled="isUploadingFile"
                    class="mt-2 w-full py-1.5 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors flex items-center justify-center gap-1.5 disabled:opacity-50 text-xs font-medium"
                  >
                    <span v-if="isUploadingFile" class="material-symbols-outlined animate-spin text-sm">refresh</span>
                    <span v-else class="material-symbols-outlined text-sm">cloud_upload</span>
                    {{ isUploadingFile ? 'Uploading...' : 'Upload to Draft' }}
                  </button>
                </div>
              </div>

              <!-- Document Properties -->
              <div class="pt-3 border-t border-zinc-100 dark:border-border-dark">
                <h3 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-3">Properties</h3>
                <div class="grid grid-cols-2 gap-3">
                  <!-- Name (full width) -->
                  <div class="col-span-2">
                    <label class="block text-xs font-medium text-zinc-500 mb-1">Name</label>
                    <input
                      v-model="draftName"
                      type="text"
                      class="w-full px-3 py-2 text-sm border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-800 dark:text-white"
                    />
                  </div>
                  <!-- Description (full width) -->
                  <div class="col-span-2">
                    <label class="block text-xs font-medium text-zinc-500 mb-1">Description</label>
                    <textarea
                      v-model="draftDescription"
                      rows="2"
                      class="w-full px-3 py-2 text-sm border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-800 dark:text-white"
                      placeholder="Enter a description..."
                    ></textarea>
                  </div>
                  <!-- Classification -->
                  <div>
                    <label class="block text-xs font-medium text-zinc-500 mb-1">Classification</label>
                    <UiSelect
                      v-model="draftClassificationId"
                      :options="[{ value: '', label: 'None' }, ...classifications.map(c => ({ value: c.id, label: c.name }))]"
                      size="md"
                      placeholder="Select..."
                    />
                  </div>
                  <!-- Importance -->
                  <div>
                    <label class="block text-xs font-medium text-zinc-500 mb-1">Importance</label>
                    <UiSelect
                      v-model="draftImportanceId"
                      :options="[{ value: '', label: 'None' }, ...importances.map(i => ({ value: i.id, label: i.name }))]"
                      size="md"
                      placeholder="Select..."
                    />
                  </div>
                  <!-- Document Type -->
                  <div>
                    <label class="block text-xs font-medium text-zinc-500 mb-1">Document Type</label>
                    <UiSelect
                      v-model="draftDocumentTypeId"
                      :options="[{ value: '', label: 'None' }, ...documentTypes.map(d => ({ value: d.id, label: d.name }))]"
                      size="md"
                      placeholder="Select..."
                    />
                  </div>
                  <!-- Expiry Date -->
                  <div>
                    <label class="block text-xs font-medium text-zinc-500 mb-1">Expiry Date</label>
                    <UiDatePicker
                      v-model="draftExpiryDate"
                      placeholder="No expiry"
                      size="md"
                      clearable
                    />
                  </div>
                  <!-- Privacy Level (full width) -->
                  <div class="col-span-2">
                    <label class="block text-xs font-medium text-zinc-500 mb-1">Privacy Level</label>
                    <div class="flex flex-wrap gap-1.5">
                      <button
                        @click="draftPrivacyLevelId = null"
                        class="px-3 py-1.5 text-xs font-medium rounded-lg border transition-all"
                        :class="!draftPrivacyLevelId ? 'border-teal bg-teal/10 text-teal' : 'border-zinc-200 dark:border-border-dark text-zinc-500 hover:border-zinc-300'"
                      >
                        None
                      </button>
                      <button
                        v-for="pl in privacyLevels"
                        :key="pl.id"
                        @click="draftPrivacyLevelId = pl.id"
                        class="px-3 py-1.5 text-xs font-medium rounded-lg border transition-all flex items-center gap-1.5"
                        :class="draftPrivacyLevelId === pl.id
                          ? 'border-current bg-current/10'
                          : 'border-zinc-200 dark:border-border-dark text-zinc-500 hover:border-zinc-300'"
                        :style="draftPrivacyLevelId === pl.id ? { color: pl.color || '#6b7280' } : {}"
                      >
                        <span class="w-2 h-2 rounded-full" :style="{ backgroundColor: pl.color || '#6b7280' }"></span>
                        {{ pl.name }}
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Content Type Metadata Fields -->
              <div v-if="draftMetadata.length > 0" class="pt-3 border-t border-zinc-100 dark:border-border-dark">
                <h3 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-3">{{ contentTypeInfo?.name || 'Metadata' }}</h3>
                <div class="grid grid-cols-2 gap-3">
                  <div
                    v-for="(field, index) in draftMetadata"
                    :key="field.fieldId"
                    :class="isTextAreaFieldType(field.fieldType) ? 'col-span-2' : ''"
                  >
                    <label class="block text-xs font-medium text-zinc-500 mb-1">
                      {{ metadataFields.find(f => f.id === field.fieldId)?.displayName || field.fieldName }}
                    </label>
                    <input
                      v-if="isTextFieldType(field.fieldType)"
                      v-model="draftMetadata[index].value"
                      type="text"
                      class="w-full px-3 py-2 text-sm border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-800 dark:text-white"
                    />
                    <textarea
                      v-else-if="isTextAreaFieldType(field.fieldType)"
                      v-model="draftMetadata[index].value"
                      rows="2"
                      class="w-full px-3 py-2 text-sm border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-800 dark:text-white"
                    ></textarea>
                    <input
                      v-else-if="isNumberFieldType(field.fieldType)"
                      v-model="draftMetadata[index].value"
                      type="number"
                      step="any"
                      class="w-full px-3 py-2 text-sm border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-800 dark:text-white"
                    />
                    <UiDatePicker
                      v-else-if="isDateFieldType(field.fieldType)"
                      v-model="draftMetadata[index].value"
                      :placeholder="'Select ' + (metadataFields.find(f => f.id === field.fieldId)?.displayName || field.fieldName)"
                      size="md"
                      clearable
                    />
                    <input
                      v-else
                      v-model="draftMetadata[index].value"
                      type="text"
                      class="w-full px-3 py-2 text-sm border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 bg-white dark:bg-surface-dark text-zinc-800 dark:text-white"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </aside>
      </div>
    </div>

    <!-- ============ MODALS (unchanged) ============ -->

    <!-- Check-in Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showCheckinModal" class="fixed inset-0 z-50 overflow-y-auto">
          <div class="fixed inset-0 bg-black/50 backdrop-blur-sm" @click="showCheckinModal = false" />
          <div class="flex min-h-full items-center justify-center p-4">
            <Transition
              enter-active-class="duration-200 ease-out"
              enter-from-class="opacity-0 scale-95 translate-y-4"
              enter-to-class="opacity-100 scale-100 translate-y-0"
              leave-active-class="duration-150 ease-in"
              leave-from-class="opacity-100 scale-100 translate-y-0"
              leave-to-class="opacity-0 scale-95 translate-y-4"
            >
              <div v-if="showCheckinModal" class="relative w-full max-w-md bg-white dark:bg-background-dark rounded-lg shadow-2xl overflow-hidden" @click.stop>
                <div class="relative bg-gradient-to-r from-navy via-navy/95 to-teal p-5 overflow-hidden">
                  <div class="absolute top-0 right-0 w-32 h-32 bg-teal/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                  <div class="absolute bottom-0 left-0 w-20 h-20 bg-teal/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>
                  <div class="relative flex items-center justify-between">
                    <div class="flex items-center gap-3">
                      <div class="w-10 h-10 bg-teal/30 backdrop-blur rounded-lg flex items-center justify-center">
                        <span class="material-symbols-outlined text-white text-xl">check_circle</span>
                      </div>
                      <div>
                        <h3 class="text-lg font-bold text-white">Check In Document</h3>
                        <p class="text-sm text-white/70">Create a new version</p>
                      </div>
                    </div>
                    <button type="button" @click="showCheckinModal = false" class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors">
                      <span class="material-symbols-outlined text-white text-lg">close</span>
                    </button>
                  </div>
                </div>

                <div class="px-6 py-4 space-y-4">
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-2">Version Type</label>
                    <div class="grid grid-cols-3 gap-2">
                      <button type="button" @click="checkinType = CheckInType.Minor"
                        :class="['p-3 rounded-lg border-2 text-center transition-all', checkinType === CheckInType.Minor ? 'border-teal bg-teal/5' : 'border-zinc-200 dark:border-border-dark hover:border-zinc-300']">
                        <span class="material-symbols-outlined text-xl mb-1" :class="checkinType === CheckInType.Minor ? 'text-teal' : 'text-zinc-400'">edit_note</span>
                        <p class="text-xs font-semibold" :class="checkinType === CheckInType.Minor ? 'text-teal' : 'text-zinc-700 dark:text-zinc-300'">Draft</p>
                        <p class="text-[10px] text-zinc-400 mt-0.5">v{{ document?.currentMajorVersion || 1 }}.{{ (document?.currentMinorVersion || 0) + 1 }}</p>
                      </button>
                      <button type="button" @click="checkinType = CheckInType.Major"
                        :class="['p-3 rounded-lg border-2 text-center transition-all', checkinType === CheckInType.Major ? 'border-purple-500 bg-purple-50 dark:bg-purple-900/20' : 'border-zinc-200 dark:border-border-dark hover:border-zinc-300']">
                        <span class="material-symbols-outlined text-xl mb-1" :class="checkinType === CheckInType.Major ? 'text-purple-600' : 'text-zinc-400'">publish</span>
                        <p class="text-xs font-semibold" :class="checkinType === CheckInType.Major ? 'text-purple-600' : 'text-zinc-700 dark:text-zinc-300'">Publish</p>
                        <p class="text-[10px] text-zinc-400 mt-0.5">v{{ (document?.currentMajorVersion || 1) + 1 }}.0</p>
                      </button>
                      <button type="button" @click="checkinType = CheckInType.Overwrite"
                        :class="['p-3 rounded-lg border-2 text-center transition-all', checkinType === CheckInType.Overwrite ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20' : 'border-zinc-200 dark:border-border-dark hover:border-zinc-300']">
                        <span class="material-symbols-outlined text-xl mb-1" :class="checkinType === CheckInType.Overwrite ? 'text-amber-600' : 'text-zinc-400'">sync</span>
                        <p class="text-xs font-semibold" :class="checkinType === CheckInType.Overwrite ? 'text-amber-600' : 'text-zinc-700 dark:text-zinc-300'">Overwrite</p>
                        <p class="text-[10px] text-zinc-400 mt-0.5">Replace</p>
                      </button>
                    </div>
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-2">Replace File</label>
                    <div v-if="checkinFile" class="p-3 bg-teal/5 border border-teal/20 rounded-lg">
                      <div class="flex items-center gap-3">
                        <div class="w-8 h-8 rounded-lg bg-teal/20 flex items-center justify-center flex-shrink-0">
                          <span class="material-symbols-outlined text-teal text-lg">insert_drive_file</span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <p class="text-sm font-medium text-zinc-900 dark:text-white truncate">{{ checkinFile.name }}</p>
                          <p class="text-xs text-zinc-500">{{ formatSize(checkinFile.size) }}</p>
                        </div>
                        <button @click="checkinFile = null" class="p-1 text-zinc-400 hover:text-red-500 rounded transition-colors">
                          <span class="material-symbols-outlined text-lg">close</span>
                        </button>
                      </div>
                    </div>
                    <div v-else-if="draftFile || workingCopy?.hasDraftFile" class="p-3 bg-teal/5 border border-teal/20 rounded-lg">
                      <div class="flex items-center gap-3">
                        <div class="w-8 h-8 rounded-lg bg-teal/20 flex items-center justify-center flex-shrink-0">
                          <span class="material-symbols-outlined text-teal text-lg">swap_horiz</span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <p class="text-xs font-medium text-teal">Draft replacement</p>
                          <p class="text-sm text-zinc-700 dark:text-zinc-300 truncate">{{ draftFile?.name || workingCopy?.draftFileName }}</p>
                        </div>
                        <span class="material-symbols-outlined text-teal text-lg">check_circle</span>
                      </div>
                    </div>
                    <label v-else class="flex items-center gap-3 p-3 border border-dashed border-zinc-300 dark:border-border-dark rounded-lg cursor-pointer hover:border-teal hover:bg-teal/5 transition-all">
                      <input type="file" @change="handleFileSelect" class="hidden" />
                      <div class="w-8 h-8 rounded-lg bg-zinc-100 dark:bg-surface-dark flex items-center justify-center flex-shrink-0">
                        <span class="material-symbols-outlined text-zinc-400 text-lg">upload_file</span>
                      </div>
                      <div class="flex-1">
                        <p class="text-sm text-zinc-500">Click to select a replacement file</p>
                      </div>
                    </label>
                  </div>

                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1">Version Comment</label>
                    <textarea v-model="checkinComment" rows="2" class="w-full px-3 py-2 text-sm border border-gray-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal bg-white dark:bg-surface-dark text-gray-900 dark:text-white" placeholder="Brief description of changes..."></textarea>
                  </div>

                  <label class="flex items-center gap-3 p-3 bg-gray-50 dark:bg-surface-dark rounded-lg cursor-pointer">
                    <input type="checkbox" v-model="keepCheckedOut" class="w-4 h-4 text-teal rounded focus:ring-teal border-gray-300 dark:border-border-dark" />
                    <div>
                      <span class="text-sm font-medium text-gray-700 dark:text-zinc-300">Keep checked out</span>
                      <p class="text-xs text-gray-500 dark:text-zinc-400">Continue editing after check-in</p>
                    </div>
                  </label>
                </div>

                <div class="flex justify-end gap-3 px-6 py-4 bg-gray-50 dark:bg-surface-dark/50 border-t border-gray-200 dark:border-border-dark/50">
                  <button type="button" @click="showCheckinModal = false" class="px-5 py-2.5 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-100 dark:hover:bg-border-dark text-gray-700 dark:text-gray-300 font-medium transition-all">Cancel</button>
                  <button type="button" @click="handleCheckin" :disabled="isCheckingIn" class="px-6 py-2.5 bg-gradient-to-r from-navy to-teal text-white rounded-lg hover:shadow-lg hover:shadow-teal/25 hover:-translate-y-0.5 disabled:opacity-50 disabled:hover:shadow-none disabled:hover:translate-y-0 font-medium transition-all flex items-center gap-2">
                    <span v-if="isCheckingIn" class="material-symbols-outlined animate-spin text-base">progress_activity</span>
                    <span v-else class="material-symbols-outlined text-base">check_circle</span>
                    {{ isCheckingIn ? 'Checking in...' : 'Check In' }}
                  </button>
                </div>
              </div>
            </Transition>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Version Comparison Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showCompareModal" class="fixed inset-0 z-50 overflow-y-auto">
          <div class="fixed inset-0 bg-black/60 backdrop-blur-sm" @click="showCompareModal = false" />
          <div class="flex min-h-full items-center justify-center p-4">
            <div class="relative w-full max-w-4xl bg-white dark:bg-background-dark rounded-lg shadow-2xl overflow-hidden" @click.stop>
              <div class="relative bg-gradient-to-r from-navy via-navy/95 to-teal p-5 overflow-hidden">
                <div class="absolute top-0 right-0 w-40 h-40 bg-teal/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-0 w-24 h-24 bg-teal/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>
                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-11 h-11 bg-teal/30 backdrop-blur rounded-lg flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-xl">compare</span>
                    </div>
                    <div>
                      <h3 class="text-lg font-bold text-white">Version Comparison</h3>
                      <p class="text-sm text-white/70">View differences between versions</p>
                    </div>
                  </div>
                  <button @click="showCompareModal = false" class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors">
                    <span class="material-symbols-outlined text-white text-lg">close</span>
                  </button>
                </div>
                <div class="mt-5 grid grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-bold text-white/60 uppercase tracking-wider mb-2">Source (Older)</label>
                    <select v-model="compareSourceVersion" class="w-full px-4 py-2.5 bg-white/10 border border-white/20 rounded-lg text-white text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal">
                      <option v-for="v in versions" :key="v.id" :value="v.id" class="text-zinc-900">v{{ getVersionLabel(v) }} - {{ formatDateShort(v.createdAt) }}</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs font-bold text-white/60 uppercase tracking-wider mb-2">Target (Newer)</label>
                    <select v-model="compareTargetVersion" class="w-full px-4 py-2.5 bg-white/10 border border-white/20 rounded-lg text-white text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal">
                      <option v-for="v in versions" :key="v.id" :value="v.id" class="text-zinc-900">v{{ getVersionLabel(v) }} - {{ formatDateShort(v.createdAt) }}</option>
                    </select>
                  </div>
                </div>
                <button @click="compareVersions" :disabled="isComparing || !compareSourceVersion || !compareTargetVersion" class="mt-4 w-full px-4 py-2.5 bg-white/20 hover:bg-white/30 text-white rounded-lg font-medium transition-colors disabled:opacity-50 flex items-center justify-center gap-2">
                  <span v-if="isComparing" class="material-symbols-outlined animate-spin text-lg">progress_activity</span>
                  <span class="material-symbols-outlined text-lg" v-else>compare_arrows</span>
                  {{ isComparing ? 'Comparing...' : 'Compare Versions' }}
                </button>
              </div>

              <div class="max-h-[60vh] overflow-y-auto">
                <div v-if="isComparing" class="p-12 text-center">
                  <div class="w-12 h-12 border-4 border-teal border-t-transparent rounded-full animate-spin mx-auto"></div>
                  <p class="text-sm text-zinc-500 mt-4">Analyzing versions...</p>
                </div>

                <div v-else-if="versionComparison" class="p-6 space-y-6">
                  <div class="grid grid-cols-2 gap-4">
                    <div class="p-4 bg-zinc-50 dark:bg-surface-dark rounded-lg border-2 border-zinc-200 dark:border-border-dark">
                      <div class="flex items-center gap-3 mb-3">
                        <div class="w-10 h-10 rounded-lg bg-[#0d1117] flex items-center justify-center">
                          <span class="text-white font-bold text-sm">{{ versionComparison.sourceVersion.versionLabel }}</span>
                        </div>
                        <div>
                          <p class="text-xs font-bold text-zinc-400 uppercase tracking-wider">Source</p>
                          <p class="text-sm font-semibold text-zinc-900 dark:text-white">Version {{ versionComparison.sourceVersion.versionLabel }}</p>
                        </div>
                      </div>
                      <p class="text-xs text-zinc-500">{{ formatDate(versionComparison.sourceVersion.createdAt) }}</p>
                      <p class="text-xs text-zinc-500 mt-1">{{ formatSize(versionComparison.sourceVersion.size) }}</p>
                    </div>
                    <div class="p-4 bg-teal/5 dark:bg-teal/10 rounded-lg border-2 border-teal/30 dark:border-teal/40">
                      <div class="flex items-center gap-3 mb-3">
                        <div class="w-10 h-10 rounded-lg bg-teal flex items-center justify-center">
                          <span class="text-white font-bold text-sm">{{ versionComparison.targetVersion.versionLabel }}</span>
                        </div>
                        <div>
                          <p class="text-xs font-bold text-teal uppercase tracking-wider">Target</p>
                          <p class="text-sm font-semibold text-zinc-900 dark:text-white">Version {{ versionComparison.targetVersion.versionLabel }}</p>
                        </div>
                      </div>
                      <p class="text-xs text-zinc-500">{{ formatDate(versionComparison.targetVersion.createdAt) }}</p>
                      <p class="text-xs text-zinc-500 mt-1">{{ formatSize(versionComparison.targetVersion.size) }}</p>
                    </div>
                  </div>

                  <div class="grid grid-cols-3 gap-3">
                    <div class="p-4 rounded-lg border-2 transition-colors" :class="versionComparison.contentChanged ? 'bg-teal/5 dark:bg-teal/10 border-teal/30 dark:border-teal/40' : 'bg-zinc-50 dark:bg-surface-dark border-zinc-200 dark:border-border-dark'">
                      <div class="flex items-center gap-2 mb-2">
                        <span class="material-symbols-outlined text-lg" :class="versionComparison.contentChanged ? 'text-teal' : 'text-zinc-400'">description</span>
                        <span class="text-xs font-bold uppercase tracking-wider" :class="versionComparison.contentChanged ? 'text-teal' : 'text-zinc-400'">Content</span>
                      </div>
                      <p class="text-sm font-semibold" :class="versionComparison.contentChanged ? 'text-teal' : 'text-zinc-500'">{{ versionComparison.contentChanged ? 'Changed' : 'Unchanged' }}</p>
                      <p v-if="versionComparison.sizeDifference !== 0" class="text-xs text-zinc-500 mt-1">{{ versionComparison.sizeDifference > 0 ? '+' : '' }}{{ formatSize(Math.abs(versionComparison.sizeDifference)) }}</p>
                    </div>
                    <div class="p-4 rounded-lg border-2 transition-colors" :class="versionComparison.metadataChanged ? 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-800' : 'bg-zinc-50 dark:bg-surface-dark border-zinc-200 dark:border-border-dark'">
                      <div class="flex items-center gap-2 mb-2">
                        <span class="material-symbols-outlined text-lg" :class="versionComparison.metadataChanged ? 'text-emerald-600 dark:text-emerald-400' : 'text-zinc-400'">category</span>
                        <span class="text-xs font-bold uppercase tracking-wider" :class="versionComparison.metadataChanged ? 'text-emerald-600 dark:text-emerald-400' : 'text-zinc-400'">Metadata</span>
                      </div>
                      <p class="text-sm font-semibold" :class="versionComparison.metadataChanged ? 'text-emerald-700 dark:text-emerald-300' : 'text-zinc-500'">{{ versionComparison.metadataChanged ? 'Changed' : 'Unchanged' }}</p>
                      <p v-if="versionComparison.metadataDifferences.length > 0" class="text-xs text-zinc-500 mt-1">{{ versionComparison.metadataDifferences.length }} field{{ versionComparison.metadataDifferences.length > 1 ? 's' : '' }}</p>
                    </div>
                    <div class="p-4 rounded-lg border-2 bg-zinc-50 dark:bg-surface-dark border-zinc-200 dark:border-border-dark">
                      <div class="flex items-center gap-2 mb-2">
                        <span class="material-symbols-outlined text-lg text-zinc-400">analytics</span>
                        <span class="text-xs font-bold uppercase tracking-wider text-zinc-400">Summary</span>
                      </div>
                      <p class="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{{ (versionComparison.contentChanged ? 1 : 0) + versionComparison.metadataDifferences.length }} change{{ ((versionComparison.contentChanged ? 1 : 0) + versionComparison.metadataDifferences.length) !== 1 ? 's' : '' }}</p>
                    </div>
                  </div>

                  <div v-if="versionComparison.metadataDifferences.length > 0" class="bg-white dark:bg-surface-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden">
                    <div class="px-4 py-3 bg-zinc-50 dark:bg-background-dark border-b border-zinc-200 dark:border-border-dark">
                      <h4 class="text-sm font-bold text-zinc-900 dark:text-white flex items-center gap-2">
                        <span class="material-symbols-outlined text-lg text-emerald-500">difference</span>
                        Metadata Differences
                      </h4>
                    </div>
                    <div class="divide-y divide-zinc-100 dark:divide-zinc-700">
                      <div v-for="diff in versionComparison.metadataDifferences" :key="diff.fieldId" class="p-4 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors"
                        :class="{ 'bg-teal/5 dark:bg-teal/10': diff.diffType === DiffType.Added, 'bg-red-50/50 dark:bg-red-900/10': diff.diffType === DiffType.Removed, 'bg-amber-50/50 dark:bg-amber-900/10': diff.diffType === DiffType.Modified }">
                        <div class="flex items-start justify-between gap-4">
                          <div class="flex-1 min-w-0">
                            <div class="flex items-center gap-2 mb-2">
                              <span class="text-sm font-semibold text-zinc-900 dark:text-white">{{ diff.displayName }}</span>
                              <span :class="{ 'bg-teal/10 text-teal': diff.diffType === DiffType.Added, 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400': diff.diffType === DiffType.Removed, 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400': diff.diffType === DiffType.Modified, 'bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-400': diff.diffType === DiffType.Unchanged }" class="px-2 py-0.5 rounded text-[10px] font-bold uppercase">{{ getDiffTypeLabel(diff.diffType) }}</span>
                            </div>
                            <div class="grid grid-cols-2 gap-4">
                              <div class="p-3 rounded-lg overflow-hidden" :class="diff.diffType === DiffType.Removed || diff.diffType === DiffType.Modified ? 'bg-red-100/70 dark:bg-red-900/20 border border-red-200 dark:border-red-800' : 'bg-zinc-100 dark:bg-background-dark'">
                                <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-wider mb-1">Previous</p>
                                <p class="text-sm break-words" :class="diff.oldValue ? (diff.diffType === DiffType.Removed || diff.diffType === DiffType.Modified ? 'text-red-700 dark:text-red-400 line-through' : 'text-zinc-600 dark:text-zinc-400') : 'text-zinc-400 italic'">{{ diff.oldValue || '(empty)' }}</p>
                              </div>
                              <div class="p-3 rounded-lg overflow-hidden" :class="diff.diffType === DiffType.Added || diff.diffType === DiffType.Modified ? 'bg-teal/5 dark:bg-teal/10 border border-teal/30' : 'bg-zinc-100 dark:bg-background-dark'">
                                <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-wider mb-1">New</p>
                                <p class="text-sm break-words" :class="diff.newValue ? (diff.diffType === DiffType.Added || diff.diffType === DiffType.Modified ? 'text-teal font-medium' : 'text-zinc-600 dark:text-zinc-400') : 'text-zinc-400 italic'">{{ diff.newValue || '(empty)' }}</p>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div v-else-if="!versionComparison.contentChanged" class="text-center py-8">
                    <div class="w-16 h-16 bg-zinc-100 dark:bg-surface-dark rounded-full flex items-center justify-center mx-auto mb-4">
                      <span class="material-symbols-outlined text-3xl text-zinc-400">check_circle</span>
                    </div>
                    <p class="text-zinc-500">These versions are identical</p>
                  </div>
                </div>

                <div v-else class="p-12 text-center">
                  <div class="w-16 h-16 bg-zinc-100 dark:bg-surface-dark rounded-full flex items-center justify-center mx-auto mb-4">
                    <span class="material-symbols-outlined text-3xl text-zinc-400">compare</span>
                  </div>
                  <p class="text-zinc-500">Select versions and click Compare to see differences</p>
                </div>
              </div>

              <div class="px-6 py-4 bg-zinc-50 dark:bg-surface-dark/50 border-t border-zinc-200 dark:border-border-dark flex justify-end">
                <button @click="showCompareModal = false" class="px-5 py-2.5 bg-zinc-200 dark:bg-border-dark hover:bg-zinc-300 dark:hover:bg-zinc-600 text-zinc-700 dark:text-zinc-300 rounded-lg font-medium transition-colors">Close</button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Restore Version Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showRestoreModal" class="fixed inset-0 z-50 overflow-y-auto">
          <div class="fixed inset-0 bg-black/60 backdrop-blur-sm" @click="showRestoreModal = false" />
          <div class="flex min-h-full items-center justify-center p-4">
            <div class="relative w-full max-w-2xl bg-white dark:bg-background-dark rounded-lg shadow-2xl overflow-hidden" @click.stop>
              <div class="relative bg-gradient-to-r from-amber-600 via-amber-500 to-orange-500 p-5 overflow-hidden">
                <div class="absolute top-0 right-0 w-40 h-40 bg-white/10 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-0 w-24 h-24 bg-white/5 rounded-full translate-y-1/2 -translate-x-1/2"></div>
                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-11 h-11 bg-white/20 backdrop-blur rounded-lg flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-xl">settings_backup_restore</span>
                    </div>
                    <div>
                      <h3 class="text-lg font-bold text-white">Restore Version</h3>
                      <p class="text-sm text-white/80">Roll back to a previous state</p>
                    </div>
                  </div>
                  <button @click="showRestoreModal = false" class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors">
                    <span class="material-symbols-outlined text-white text-lg">close</span>
                  </button>
                </div>
              </div>

              <div class="p-6 max-h-[60vh] overflow-y-auto">
                <div v-if="restoreVersionInfo" class="mb-6 p-4 bg-amber-50 dark:bg-amber-900/20 rounded-lg border border-amber-200 dark:border-amber-800">
                  <div class="flex items-center gap-4">
                    <div class="w-14 h-14 rounded-lg bg-gradient-to-br from-amber-500 to-orange-500 flex items-center justify-center flex-shrink-0">
                      <span class="text-white font-bold text-lg">{{ getVersionLabel(restoreVersionInfo) }}</span>
                    </div>
                    <div>
                      <p class="text-xs font-bold text-amber-600 dark:text-amber-400 uppercase tracking-wider">Restoring To</p>
                      <p class="text-lg font-bold text-zinc-900 dark:text-white">Version {{ getVersionLabel(restoreVersionInfo) }}</p>
                      <p class="text-sm text-zinc-500">{{ formatDate(restoreVersionInfo.createdAt) }} &middot; {{ formatSize(restoreVersionInfo.size) }}</p>
                    </div>
                  </div>
                </div>

                <div v-if="isLoadingRestorePreview" class="mb-6 p-8 text-center">
                  <div class="w-10 h-10 border-4 border-amber-500 border-t-transparent rounded-full animate-spin mx-auto"></div>
                  <p class="text-sm text-zinc-500 mt-3">Loading preview...</p>
                </div>

                <div v-else-if="restoreComparison" class="mb-6">
                  <h4 class="text-sm font-bold text-zinc-900 dark:text-white mb-3 flex items-center gap-2">
                    <span class="material-symbols-outlined text-lg text-amber-500">preview</span>
                    Changes Preview
                  </h4>
                  <div class="grid grid-cols-2 gap-3 mb-4">
                    <div class="p-3 rounded-lg border-2" :class="restoreComparison.contentChanged ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-800' : 'bg-zinc-50 dark:bg-surface-dark border-zinc-200 dark:border-border-dark'">
                      <div class="flex items-center gap-2">
                        <span class="material-symbols-outlined text-lg" :class="restoreComparison.contentChanged ? 'text-blue-600' : 'text-zinc-400'">description</span>
                        <div>
                          <p class="text-xs font-bold uppercase tracking-wider" :class="restoreComparison.contentChanged ? 'text-blue-600' : 'text-zinc-400'">File Content</p>
                          <p class="text-sm font-semibold" :class="restoreComparison.contentChanged ? 'text-blue-700 dark:text-blue-300' : 'text-zinc-500'">{{ restoreComparison.contentChanged ? 'Will be restored' : 'No change' }}</p>
                        </div>
                      </div>
                    </div>
                    <div class="p-3 rounded-lg border-2" :class="restoreComparison.metadataChanged ? 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-800' : 'bg-zinc-50 dark:bg-surface-dark border-zinc-200 dark:border-border-dark'">
                      <div class="flex items-center gap-2">
                        <span class="material-symbols-outlined text-lg" :class="restoreComparison.metadataChanged ? 'text-emerald-600' : 'text-zinc-400'">category</span>
                        <div>
                          <p class="text-xs font-bold uppercase tracking-wider" :class="restoreComparison.metadataChanged ? 'text-emerald-600' : 'text-zinc-400'">Metadata</p>
                          <p class="text-sm font-semibold" :class="restoreComparison.metadataChanged ? 'text-emerald-700 dark:text-emerald-300' : 'text-zinc-500'">{{ restoreComparison.metadataChanged ? `${restoreComparison.metadataDifferences.length} field(s) will change` : 'No change' }}</p>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div v-if="restoreComparison.metadataDifferences.length > 0" class="bg-zinc-50 dark:bg-surface-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden">
                    <div class="px-4 py-2 bg-zinc-100 dark:bg-background-dark border-b border-zinc-200 dark:border-border-dark">
                      <p class="text-xs font-bold text-zinc-500 uppercase tracking-wider">Metadata Changes</p>
                    </div>
                    <div class="divide-y divide-zinc-200 dark:divide-zinc-700 max-h-40 overflow-y-auto">
                      <div v-for="diff in restoreComparison.metadataDifferences" :key="diff.fieldId" class="px-4 py-2.5">
                        <div class="flex items-center justify-between gap-2 mb-1">
                          <span class="text-sm font-medium text-zinc-700 dark:text-zinc-300 truncate">{{ diff.displayName }}</span>
                          <span class="material-symbols-outlined text-zinc-400 text-sm flex-shrink-0">arrow_forward</span>
                        </div>
                        <div class="grid grid-cols-2 gap-2 text-xs">
                          <span class="text-red-500 line-through break-words">{{ diff.oldValue || '(empty)' }}</span>
                          <span class="text-green-600 font-medium break-words">{{ diff.newValue || '(empty)' }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div class="space-y-3 mb-6">
                  <h4 class="text-sm font-bold text-zinc-900 dark:text-white">What to Restore</h4>
                  <label class="flex items-center gap-4 p-4 rounded-lg border-2 cursor-pointer transition-all" :class="restoreContent ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-300 dark:border-blue-700' : 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark hover:border-zinc-300'">
                    <input type="checkbox" v-model="restoreContent" class="w-5 h-5 text-blue-600 rounded focus:ring-blue-500 border-zinc-300" />
                    <div class="flex-1">
                      <div class="flex items-center gap-2">
                        <span class="material-symbols-outlined text-lg" :class="restoreContent ? 'text-blue-600' : 'text-zinc-400'">description</span>
                        <span class="font-semibold text-zinc-900 dark:text-white">File Content</span>
                      </div>
                      <p class="text-sm text-zinc-500 mt-0.5">Replace current file with the selected version's file</p>
                    </div>
                  </label>
                  <label class="flex items-center gap-4 p-4 rounded-lg border-2 cursor-pointer transition-all" :class="restoreMetadata ? 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-300 dark:border-emerald-700' : 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark hover:border-zinc-300'">
                    <input type="checkbox" v-model="restoreMetadata" class="w-5 h-5 text-emerald-600 rounded focus:ring-emerald-500 border-zinc-300" />
                    <div class="flex-1">
                      <div class="flex items-center gap-2">
                        <span class="material-symbols-outlined text-lg" :class="restoreMetadata ? 'text-emerald-600' : 'text-zinc-400'">category</span>
                        <span class="font-semibold text-zinc-900 dark:text-white">Metadata</span>
                      </div>
                      <p class="text-sm text-zinc-500 mt-0.5">Replace current metadata fields with the selected version's values</p>
                    </div>
                  </label>
                </div>

                <div>
                  <label class="block text-sm font-bold text-zinc-900 dark:text-white mb-2">Restore Comment <span class="font-normal text-zinc-400">(optional)</span></label>
                  <textarea v-model="restoreComment" rows="2" class="w-full px-4 py-3 border border-zinc-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-amber-500/50 focus:border-amber-500 bg-white dark:bg-surface-dark text-zinc-900 dark:text-white text-sm resize-none" placeholder="Reason for restoring this version..."></textarea>
                </div>

                <div class="mt-4 p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg flex items-start gap-3">
                  <span class="material-symbols-outlined text-amber-600 dark:text-amber-400 text-lg flex-shrink-0">warning</span>
                  <p class="text-sm text-amber-800 dark:text-amber-300">This will create a new major version. The current version will remain in version history and can be restored later.</p>
                </div>
              </div>

              <div class="flex justify-end gap-3 px-6 py-4 bg-zinc-50 dark:bg-surface-dark/50 border-t border-zinc-200 dark:border-border-dark">
                <button @click="showRestoreModal = false" class="px-5 py-2.5 border border-zinc-300 dark:border-border-dark rounded-lg hover:bg-zinc-100 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 font-medium transition-colors">Cancel</button>
                <button @click="handleRestore" :disabled="isRestoring || (!restoreContent && !restoreMetadata)" class="px-6 py-2.5 bg-gradient-to-r from-amber-500 to-orange-500 text-white rounded-lg hover:shadow-lg hover:shadow-amber-500/25 hover:-translate-y-0.5 disabled:opacity-50 disabled:hover:shadow-none disabled:hover:translate-y-0 font-medium transition-all flex items-center gap-2">
                  <span v-if="isRestoring" class="material-symbols-outlined animate-spin text-base">progress_activity</span>
                  <span v-else class="material-symbols-outlined text-base">settings_backup_restore</span>
                  {{ isRestoring ? 'Restoring...' : 'Restore Version' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Enterprise Permission Management Modal -->
    <PermissionManagementModal
      v-if="document"
      :show="showPermissionModal"
      node-type="Document"
      :node-id="document.id"
      :node-name="document.name"
      @close="showPermissionModal = false"
      @updated="loadMyPermissionLevel(document.id)"
    />

    <!-- Document Full Viewer (Modal) -->
    <DocumentViewer
      v-if="document"
      v-model="showPreview"
      :document="document"
    />
  </div>
</template>

<style scoped>
.border-3 {
  border-width: 3px;
}
</style>
