<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { documentsApi, contentTypeDefinitionsApi, referenceDataApi, usersApi } from '@/api/client'
import type { ContentTypeDefinition, ContentTypeField, EffectiveContentType, LookupItem, User } from '@/types'
import { UiCheckbox, UiDatePicker, UiSelect } from '@/components/ui'

interface Props {
  folderId: string
  folderName?: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  uploaded: []
}>()

// Upload state
const uploadFiles = ref<File[]>([])
const isDragging = ref(false)
const isUploading = ref(false)
const uploadProgress = ref(0)
const error = ref('')

// Wizard state for multi-file upload
type ConfigMode = 'same' | 'individual' | null
const configMode = ref<ConfigMode>(null)
const currentFileIndex = ref(0)
const applyToRemaining = ref(false)

// Per-file configuration storage
interface FileConfig {
  file: File
  name: string
  description: string
  contentTypeId: string | null
  fieldValues: Record<string, any>
  configured: boolean
}
const fileConfigs = ref<FileConfig[]>([])

// Content Type state - using effective content types with inheritance
const effectiveContentTypes = ref<EffectiveContentType[]>([])
const selectedContentTypeId = ref<string | null>(null)
const selectedContentType = ref<ContentTypeDefinition | null>(null)
const isLoadingContentTypes = ref(false)

// Metadata state
const basicMetadata = ref({
  name: '',
  description: ''
})

// Dynamic field values based on content type
const fieldValues = ref<Record<string, any>>({})

// Lookup data cache - stores fetched lookup items by lookupName
const lookupData = ref<Record<string, LookupItem[]>>({})
const usersList = ref<User[]>([])
const isLoadingLookups = ref(false)

// Computed
const hasRequiredContentType = computed(() => {
  return effectiveContentTypes.value.some(ct => ct.isRequired)
})

const contentTypeOptions = computed(() => {
  const options: { value: string | null; label: string; isDefault?: boolean; source?: string }[] = []

  // Only add "No Content Type" if not required
  if (!hasRequiredContentType.value) {
    options.push({ value: null, label: '-- No Content Type --' })
  }

  for (const ct of effectiveContentTypes.value) {
    let label = ct.name
    if (ct.category) label += ` (${ct.category})`
    if (ct.isDefault) label += ' [Default]'
    if (ct.isRequired) label += ' *'
    if (ct.source !== 'Direct') label += ` [${ct.source}${ct.sourceName ? `: ${ct.sourceName}` : ''}]`

    options.push({
      value: ct.contentTypeId,
      label,
      isDefault: ct.isDefault,
      source: ct.source
    })
  }
  return options
})

const groupedFields = computed(() => {
  if (!selectedContentType.value?.fields) return {}

  const groups: Record<string, ContentTypeField[]> = {}
  for (const field of selectedContentType.value.fields) {
    const group = field.groupName || 'General'
    if (!groups[group]) groups[group] = []
    groups[group].push(field)
  }
  return groups
})

const canUpload = computed(() => {
  if (uploadFiles.value.length === 0) return false
  if (hasRequiredContentType.value && !selectedContentTypeId.value) return false

  // Check required fields for selected content type
  if (selectedContentType.value?.fields) {
    for (const field of selectedContentType.value.fields) {
      if (field.isRequired) {
        const value = fieldValues.value[field.fieldName]
        if (!value && value !== 0 && value !== false) return false
      }
    }
  }

  return true
})

// Get selected effective content type for display info
const selectedEffectiveContentType = computed(() => {
  if (!selectedContentTypeId.value) return null
  return effectiveContentTypes.value.find(ct => ct.contentTypeId === selectedContentTypeId.value) || null
})

// Wizard computed properties
const isMultiFileUpload = computed(() => uploadFiles.value.length > 1)
const needsConfigModeSelection = computed(() => isMultiFileUpload.value && configMode.value === null)
const isWizardMode = computed(() => configMode.value === 'individual')
const currentFile = computed(() => isWizardMode.value ? uploadFiles.value[currentFileIndex.value] : null)
const totalWizardSteps = computed(() => uploadFiles.value.length)
const wizardProgress = computed(() => Math.round(((currentFileIndex.value + 1) / totalWizardSteps.value) * 100))

// Load content types assigned to folder
onMounted(async () => {
  await loadFolderContentTypes()
})

async function loadFolderContentTypes() {
  isLoadingContentTypes.value = true
  try {
    // Load effective content types (includes inheritance from parent folders and cabinet)
    const response = await contentTypeDefinitionsApi.getEffectiveContentTypes(props.folderId)
    effectiveContentTypes.value = response.data || []

    // Auto-select content type based on priority:
    // 1. Default content type (if any)
    // 2. If there's only one content type
    // 3. Required content type (if any)
    if (effectiveContentTypes.value.length > 0) {
      const defaultCt = effectiveContentTypes.value.find(ct => ct.isDefault)
      if (defaultCt) {
        selectedContentTypeId.value = defaultCt.contentTypeId
      } else if (effectiveContentTypes.value.length === 1) {
        selectedContentTypeId.value = effectiveContentTypes.value[0].contentTypeId
      } else {
        const requiredCt = effectiveContentTypes.value.find(ct => ct.isRequired)
        if (requiredCt) {
          selectedContentTypeId.value = requiredCt.contentTypeId
        }
      }
    }
  } catch (err) {
    // Fallback: allow upload without content type
    effectiveContentTypes.value = []
  } finally {
    isLoadingContentTypes.value = false
  }
}

// Watch for content type selection changes
watch(selectedContentTypeId, async (newId) => {
  if (!newId) {
    selectedContentType.value = null
    fieldValues.value = {}
    lookupData.value = {}
    return
  }

  try {
    // First check if the effective content type has fields already loaded
    const effectiveCt = effectiveContentTypes.value.find(ct => ct.contentTypeId === newId)
    let fields: ContentTypeField[] = []

    if (effectiveCt?.fields && effectiveCt.fields.length > 0) {
      // Use fields from effective content type (already loaded with inheritance)
      selectedContentType.value = {
        id: effectiveCt.contentTypeId,
        name: effectiveCt.name,
        description: effectiveCt.description,
        icon: effectiveCt.icon,
        color: effectiveCt.color,
        category: effectiveCt.category,
        isActive: true,
        allowOnFolders: true,
        allowOnDocuments: true,
        isRequired: effectiveCt.isRequired,
        sortOrder: effectiveCt.displayOrder,
        fields: effectiveCt.fields
      }
      fields = effectiveCt.fields
    } else {
      // Fallback: Load full content type details from API
      const response = await contentTypeDefinitionsApi.getById(newId)
      selectedContentType.value = response.data
      fields = response.data.fields || []
    }

    // Initialize field values with defaults
    fieldValues.value = {}
    for (const field of fields) {
      if (field.defaultValue) {
        fieldValues.value[field.fieldName] = parseDefaultValue(field)
      } else {
        fieldValues.value[field.fieldName] = getEmptyValue(field.fieldType)
      }
    }

    // Load lookup data for Lookup and User fields
    await loadLookupData(fields)
  } catch (err) {
  }
})

// Load lookup data from API for Lookup fields
async function loadLookupData(fields: ContentTypeField[]) {
  isLoadingLookups.value = true
  lookupData.value = {}

  try {
    const lookupPromises: Promise<void>[] = []
    const lookupNames = new Set<string>()
    let needsUsers = false

    for (const field of fields) {
      if (field.fieldType === 'Lookup' && field.lookupName && !lookupNames.has(field.lookupName)) {
        lookupNames.add(field.lookupName)
        lookupPromises.push(
          referenceDataApi.getLookupItems(field.lookupName)
            .then(res => {
              lookupData.value[field.lookupName!] = res.data || []
            })
            .catch(err => {
              lookupData.value[field.lookupName!] = []
            })
        )
      }
      if (field.fieldType === 'User') {
        needsUsers = true
      }
    }

    // Load users if needed
    if (needsUsers && usersList.value.length === 0) {
      lookupPromises.push(
        usersApi.getAll()
          .then(res => {
            usersList.value = res.data || []
          })
          .catch(err => {
            usersList.value = []
          })
      )
    }

    await Promise.all(lookupPromises)
  } finally {
    isLoadingLookups.value = false
  }
}

// Get lookup options for a field
function getLookupOptions(field: ContentTypeField): { value: string; label: string }[] {
  if (!field.lookupName || !lookupData.value[field.lookupName]) return []
  return lookupData.value[field.lookupName].map(item => ({
    value: item.value,
    label: item.displayText || item.value
  }))
}

// Get user options for User field type
function getUserOptions(): { value: string; label: string }[] {
  return usersList.value.map(user => ({
    value: user.id,
    label: user.displayName || `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.username
  }))
}

function parseDefaultValue(field: ContentTypeField): any {
  if (!field.defaultValue) return getEmptyValue(field.fieldType)

  switch (field.fieldType) {
    case 'Number':
    case 'Decimal':
      return parseFloat(field.defaultValue) || 0
    case 'Boolean':
      return field.defaultValue.toLowerCase() === 'true'
    case 'Date':
    case 'DateTime':
      return field.defaultValue
    default:
      return field.defaultValue
  }
}

function getEmptyValue(fieldType: string): any {
  switch (fieldType) {
    case 'Number':
    case 'Decimal':
      return null
    case 'Boolean':
      return false
    case 'MultiSelect':
      return []
    default:
      return ''
  }
}

// Drag and drop handlers
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
    if (uploadFiles.value.length === 1 && !basicMetadata.value.name) {
      basicMetadata.value.name = uploadFiles.value[0].name.replace(/\.[^/.]+$/, '')
    }
  }
}

function handleFileSelect(event: Event) {
  const input = event.target as HTMLInputElement
  if (input.files && input.files.length > 0) {
    uploadFiles.value = Array.from(input.files)
    if (uploadFiles.value.length === 1 && !basicMetadata.value.name) {
      basicMetadata.value.name = uploadFiles.value[0].name.replace(/\.[^/.]+$/, '')
    }
  }
}

function removeFile(index: number) {
  uploadFiles.value.splice(index, 1)
  // Reset config mode if we go from multi to single file
  if (uploadFiles.value.length <= 1) {
    configMode.value = null
    currentFileIndex.value = 0
    fileConfigs.value = []
  }
}

// Wizard navigation functions
function selectConfigMode(mode: ConfigMode) {
  configMode.value = mode
  if (mode === 'individual') {
    initializeFileConfigs()
    currentFileIndex.value = 0
    loadConfigForCurrentFile()
  }
}

function initializeFileConfigs() {
  fileConfigs.value = uploadFiles.value.map(file => ({
    file,
    name: file.name.replace(/\.[^/.]+$/, ''),
    description: '',
    contentTypeId: null,
    fieldValues: {},
    configured: false
  }))

  // Set default content type for all if available
  const defaultCt = effectiveContentTypes.value.find(ct => ct.isDefault)
  if (defaultCt) {
    fileConfigs.value.forEach(fc => {
      fc.contentTypeId = defaultCt.contentTypeId
    })
  }
}

function loadConfigForCurrentFile() {
  const config = fileConfigs.value[currentFileIndex.value]
  if (config) {
    basicMetadata.value.name = config.name
    basicMetadata.value.description = config.description
    selectedContentTypeId.value = config.contentTypeId
    // Field values will be loaded via the watcher on selectedContentTypeId
    // We need to restore them after content type loads
    setTimeout(() => {
      if (Object.keys(config.fieldValues).length > 0) {
        fieldValues.value = { ...config.fieldValues }
      }
    }, 100)
  }
}

function saveCurrentFileConfig() {
  if (currentFileIndex.value >= 0 && currentFileIndex.value < fileConfigs.value.length) {
    fileConfigs.value[currentFileIndex.value] = {
      ...fileConfigs.value[currentFileIndex.value],
      name: basicMetadata.value.name,
      description: basicMetadata.value.description,
      contentTypeId: selectedContentTypeId.value,
      fieldValues: { ...fieldValues.value },
      configured: true
    }
  }
}

function wizardNext() {
  saveCurrentFileConfig()

  // Apply to remaining if checked
  if (applyToRemaining.value) {
    const currentConfig = fileConfigs.value[currentFileIndex.value]
    for (let i = currentFileIndex.value + 1; i < fileConfigs.value.length; i++) {
      fileConfigs.value[i] = {
        ...fileConfigs.value[i],
        contentTypeId: currentConfig.contentTypeId,
        fieldValues: { ...currentConfig.fieldValues },
        configured: true
      }
    }
    // Move to upload
    handleUpload()
    return
  }

  // If not on last step, move to next file
  if (currentFileIndex.value < uploadFiles.value.length - 1) {
    currentFileIndex.value++
    loadConfigForCurrentFile()
  } else {
    // Last step - start upload
    handleUpload()
  }
}

function wizardBack() {
  saveCurrentFileConfig()
  if (currentFileIndex.value > 0) {
    currentFileIndex.value--
    loadConfigForCurrentFile()
  }
}

function wizardSkip() {
  // Mark as configured with defaults
  fileConfigs.value[currentFileIndex.value].configured = true
  if (currentFileIndex.value < uploadFiles.value.length - 1) {
    currentFileIndex.value++
    loadConfigForCurrentFile()
  } else {
    // Last file, proceed to upload
    handleUpload()
  }
}

function goBackToModeSelection() {
  configMode.value = null
  currentFileIndex.value = 0
  fileConfigs.value = []
}

const canProceedWizardStep = computed(() => {
  if (!hasRequiredContentType.value) return true
  if (!selectedContentTypeId.value) return false

  // Check required fields
  if (selectedContentType.value?.fields) {
    for (const field of selectedContentType.value.fields) {
      if (field.isRequired) {
        const value = fieldValues.value[field.fieldName]
        if (!value && value !== 0 && value !== false) return false
      }
    }
  }
  return true
})

const isLastWizardStep = computed(() => currentFileIndex.value === uploadFiles.value.length - 1)

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

// Upload handler
async function handleUpload() {
  // For wizard mode, save current config first
  if (isWizardMode.value) {
    saveCurrentFileConfig()
  }

  if (!isWizardMode.value && !canUpload.value) return

  isUploading.value = true
  error.value = ''
  uploadProgress.value = 0

  try {
    for (let i = 0; i < uploadFiles.value.length; i++) {
      const file = uploadFiles.value[i]

      // Get config for this file (wizard mode) or use shared config
      const config = isWizardMode.value
        ? fileConfigs.value[i]
        : {
            name: basicMetadata.value.name || file.name.replace(/\.[^/.]+$/, ''),
            description: basicMetadata.value.description,
            contentTypeId: selectedContentTypeId.value,
            fieldValues: fieldValues.value
          }

      // Create form data for upload
      const formData = new FormData()
      formData.append('file', file)
      formData.append('folderId', props.folderId)
      formData.append('name', config.name || file.name.replace(/\.[^/.]+$/, ''))
      if (config.description) {
        formData.append('description', config.description)
      }
      if (config.contentTypeId) {
        formData.append('contentTypeId', config.contentTypeId)
      }

      // Upload the document
      const uploadResponse = await documentsApi.uploadWithProgress(formData, (progress) => {
        uploadProgress.value = Math.round((i / uploadFiles.value.length) * 100 + (progress / uploadFiles.value.length))
      })

      const documentId = uploadResponse.data?.id || uploadResponse.data

      // Save metadata if content type is selected
      if (config.contentTypeId && documentId && Object.keys(config.fieldValues).length > 0) {
        // Get content type fields for this config
        const effectiveCt = effectiveContentTypes.value.find(ct => ct.contentTypeId === config.contentTypeId)
        const fields = effectiveCt?.fields || selectedContentType.value?.fields || []

        const metadata = fields.map(field => ({
          fieldId: field.id,
          fieldName: field.fieldName,
          value: getStringValue(config.fieldValues[field.fieldName], field.fieldType),
          numericValue: getNumericValue(config.fieldValues[field.fieldName], field.fieldType),
          dateValue: getDateValue(config.fieldValues[field.fieldName], field.fieldType)
        })).filter(m => m.value || m.numericValue || m.dateValue) || []

        if (metadata.length > 0) {
          await contentTypeDefinitionsApi.saveDocumentMetadata(
            documentId,
            config.contentTypeId,
            metadata
          )
        }
      }
    }

    uploadProgress.value = 100
    emit('uploaded')
    emit('close')
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Upload failed. Please try again.'
  } finally {
    isUploading.value = false
  }
}

function getStringValue(value: any, fieldType: string): string | undefined {
  if (value === null || value === undefined || value === '') return undefined
  if (['Number', 'Decimal', 'Date', 'DateTime'].includes(fieldType)) return undefined
  if (fieldType === 'MultiSelect' && Array.isArray(value)) return value.join(',')
  if (fieldType === 'Boolean') return value ? 'true' : 'false'
  return String(value)
}

function getNumericValue(value: any, fieldType: string): number | undefined {
  if (!['Number', 'Decimal'].includes(fieldType)) return undefined
  if (value === null || value === undefined || value === '') return undefined
  const num = parseFloat(value)
  return isNaN(num) ? undefined : num
}

function getDateValue(value: any, fieldType: string): string | undefined {
  if (!['Date', 'DateTime'].includes(fieldType)) return undefined
  if (!value) return undefined
  return value
}

// Parse dropdown/multiselect options
function getFieldOptions(field: ContentTypeField): { value: string; label: string }[] {
  if (!field.options) return []
  try {
    return JSON.parse(field.options)
  } catch {
    return []
  }
}

// Map legacy/invalid icon names to valid Material Symbols names
function getMaterialIcon(iconName?: string): string {
  if (!iconName) return 'description'

  const iconMap: Record<string, string> = {
    'document': 'description',
    'file': 'description',
    'doc': 'description',
    'pdf': 'picture_as_pdf',
    'image': 'image',
    'photo': 'photo',
    'video': 'videocam',
    'audio': 'audio_file',
    'spreadsheet': 'table_chart',
    'presentation': 'slideshow',
    'archive': 'folder_zip',
    'code': 'code',
    'text': 'article',
  }

  return iconMap[iconName.toLowerCase()] || iconName
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4" @click.self="emit('close')">
        <Transition
          enter-active-class="duration-300 ease-out"
          enter-from-class="opacity-0 scale-95 translate-y-4"
          enter-to-class="opacity-100 scale-100 translate-y-0"
          leave-active-class="duration-200 ease-in"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div class="bg-white dark:bg-zinc-900 rounded-2xl shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col ring-1 ring-black/5 dark:ring-white/10">
            <!-- Header with brand gradient -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-6 overflow-hidden">
              <!-- Decorative elements -->
              <div class="absolute top-0 right-0 w-64 h-64 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
              <div class="absolute bottom-0 left-0 w-32 h-32 bg-primary/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>

              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-4">
                  <div class="w-12 h-12 bg-primary/30 backdrop-blur rounded-xl flex items-center justify-center">
                    <span class="material-symbols-outlined text-white text-2xl">cloud_upload</span>
                  </div>
                  <div>
                    <h3 class="text-xl font-bold text-white">
                      {{ isWizardMode ? `Configure File ${currentFileIndex + 1} of ${totalWizardSteps}` : 'Upload Document' }}
                    </h3>
                    <p v-if="folderName && !isWizardMode" class="text-sm text-white/80 flex items-center gap-1 mt-0.5">
                      <span class="material-symbols-outlined text-sm">folder</span>
                      {{ folderName }}
                    </p>
                    <p v-if="isWizardMode && currentFile" class="text-sm text-white/80 flex items-center gap-1 mt-0.5">
                      <span class="material-symbols-outlined text-sm">description</span>
                      {{ currentFile.name }}
                    </p>
                  </div>
                </div>
                <div class="flex items-center gap-3">
                  <!-- Wizard Progress Dots -->
                  <div v-if="isWizardMode" class="flex items-center gap-1.5 mr-2">
                    <div
                      v-for="(_, idx) in uploadFiles"
                      :key="idx"
                      :class="[
                        'w-2.5 h-2.5 rounded-full transition-all duration-300',
                        idx < currentFileIndex ? 'bg-white' :
                        idx === currentFileIndex ? 'bg-white scale-125 ring-2 ring-white/50' :
                        'bg-white/30'
                      ]"
                    ></div>
                  </div>
                  <button
                    @click="emit('close')"
                    class="w-10 h-10 flex items-center justify-center rounded-xl bg-white/10 hover:bg-white/20 transition-colors"
                  >
                    <span class="material-symbols-outlined text-white">close</span>
                  </button>
                </div>
              </div>

              <!-- Wizard Progress Bar -->
              <div v-if="isWizardMode" class="relative mt-4">
                <div class="h-1 bg-white/20 rounded-full overflow-hidden">
                  <div
                    class="h-full bg-white transition-all duration-500"
                    :style="{ width: wizardProgress + '%' }"
                  ></div>
                </div>
              </div>
            </div>

            <!-- Content -->
            <div class="flex-1 overflow-y-auto">
              <!-- Error Alert -->
              <Transition
                enter-active-class="duration-300 ease-out"
                enter-from-class="opacity-0 -translate-y-2"
                enter-to-class="opacity-100 translate-y-0"
              >
                <div v-if="error" class="mx-6 mt-4 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-xl flex items-start gap-3">
                  <span class="material-symbols-outlined text-red-500 mt-0.5">error</span>
                  <div>
                    <p class="text-sm font-medium text-red-700 dark:text-red-400">Upload Failed</p>
                    <p class="text-sm text-red-600 dark:text-red-400/80 mt-0.5">{{ error }}</p>
                  </div>
                </div>
              </Transition>

              <!-- Configuration Mode Selection (Multi-file only) -->
              <Transition
                enter-active-class="duration-300 ease-out"
                enter-from-class="opacity-0 translate-y-4"
                enter-to-class="opacity-100 translate-y-0"
                leave-active-class="duration-200 ease-in"
                leave-from-class="opacity-100 translate-y-0"
                leave-to-class="opacity-0 -translate-y-4"
              >
                <div v-if="needsConfigModeSelection" class="p-8">
                  <div class="max-w-2xl mx-auto">
                    <div class="text-center mb-8">
                      <div class="w-16 h-16 bg-gradient-to-br from-navy to-primary rounded-2xl flex items-center justify-center mx-auto mb-4 shadow-lg shadow-primary/25">
                        <span class="material-symbols-outlined text-white text-3xl">settings</span>
                      </div>
                      <h4 class="text-xl font-bold text-gray-900 dark:text-white">Configure {{ uploadFiles.length }} Files</h4>
                      <p class="text-gray-500 mt-2">How would you like to set up content types for your files?</p>
                    </div>

                    <!-- Options -->
                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <!-- Same for All -->
                      <button
                        @click="selectConfigMode('same')"
                        class="group relative p-6 bg-white dark:bg-zinc-800 border-2 border-gray-200 dark:border-gray-700 rounded-2xl text-left hover:border-teal hover:shadow-lg hover:shadow-teal/10 transition-all duration-300"
                      >
                        <div class="absolute top-4 right-4">
                          <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-teal/10 text-teal">
                            Recommended
                          </span>
                        </div>
                        <div class="w-12 h-12 bg-teal/10 rounded-xl flex items-center justify-center mb-4 group-hover:bg-teal/20 transition-colors">
                          <span class="material-symbols-outlined text-teal text-2xl">content_copy</span>
                        </div>
                        <h5 class="text-lg font-semibold text-gray-900 dark:text-white mb-2">Apply Same to All</h5>
                        <p class="text-sm text-gray-500">
                          Use the same content type and metadata for all {{ uploadFiles.length }} files. Fastest option for similar documents.
                        </p>
                      </button>

                      <!-- Configure Each -->
                      <button
                        @click="selectConfigMode('individual')"
                        class="group p-6 bg-white dark:bg-zinc-800 border-2 border-gray-200 dark:border-gray-700 rounded-2xl text-left hover:border-primary hover:shadow-lg hover:shadow-primary/10 transition-all duration-300"
                      >
                        <div class="w-12 h-12 bg-primary/10 rounded-xl flex items-center justify-center mb-4 group-hover:bg-primary/20 transition-colors">
                          <span class="material-symbols-outlined text-primary text-2xl">tune</span>
                        </div>
                        <h5 class="text-lg font-semibold text-gray-900 dark:text-white mb-2">Configure Individually</h5>
                        <p class="text-sm text-gray-500">
                          Set up each file separately with its own content type and metadata. Best for mixed document types.
                        </p>
                      </button>
                    </div>

                    <!-- File Preview -->
                    <div class="mt-8 p-4 bg-gray-50 dark:bg-zinc-800/50 rounded-xl">
                      <p class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
                        <span class="material-symbols-outlined text-sm">folder_open</span>
                        Files to upload:
                      </p>
                      <div class="flex flex-wrap gap-2 max-h-32 overflow-y-auto">
                        <div
                          v-for="(file, idx) in uploadFiles"
                          :key="idx"
                          class="inline-flex items-center gap-2 px-3 py-1.5 bg-white dark:bg-zinc-700 rounded-lg border border-gray-200 dark:border-gray-600 text-sm"
                        >
                          <span class="material-symbols-outlined text-teal text-sm">description</span>
                          <span class="text-gray-700 dark:text-gray-300 truncate max-w-[150px]">{{ file.name }}</span>
                          <span class="text-xs text-gray-400">{{ formatSize(file.size) }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </Transition>

              <!-- Two Column Layout (shown when not in mode selection) -->
              <div v-if="!needsConfigModeSelection" class="grid grid-cols-1 lg:grid-cols-2 divide-y lg:divide-y-0 lg:divide-x divide-gray-200 dark:divide-gray-700/50">
                <!-- Section 1: File Upload (or Current File in Wizard) -->
                <div class="p-6">
                  <div class="flex items-center gap-3 mb-5">
                    <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-navy to-primary flex items-center justify-center text-white text-sm font-bold shadow-lg shadow-primary/25">
                      1
                    </div>
                    <div>
                      <h4 class="text-base font-semibold text-gray-900 dark:text-white">
                        {{ isWizardMode ? 'Current File' : 'Select Files' }}
                      </h4>
                      <p class="text-xs text-gray-500">
                        {{ isWizardMode ? `File ${currentFileIndex + 1} of ${totalWizardSteps}` : 'Choose files to upload' }}
                      </p>
                    </div>
                  </div>

                  <!-- Wizard Mode: Current File Display -->
                  <div v-if="isWizardMode && currentFile" class="mb-5">
                    <div class="p-4 bg-gradient-to-r from-teal/10 to-primary/10 rounded-xl border border-teal/20">
                      <div class="flex items-center gap-4">
                        <div class="w-14 h-14 bg-white dark:bg-zinc-800 rounded-xl flex items-center justify-center shadow-sm">
                          <span class="material-symbols-outlined text-teal text-3xl">description</span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <p class="text-lg font-semibold text-gray-900 dark:text-white truncate">{{ currentFile.name }}</p>
                          <p class="text-sm text-gray-500">{{ formatSize(currentFile.size) }}</p>
                        </div>
                        <div class="text-right">
                          <span class="inline-flex items-center gap-1 px-3 py-1 bg-white dark:bg-zinc-800 rounded-lg text-sm font-medium text-gray-700 dark:text-gray-300 shadow-sm">
                            <span class="material-symbols-outlined text-sm text-teal">check_circle</span>
                            {{ currentFileIndex + 1 }} / {{ totalWizardSteps }}
                          </span>
                        </div>
                      </div>
                    </div>

                    <!-- Back to mode selection -->
                    <button
                      @click="goBackToModeSelection"
                      class="mt-3 text-sm text-gray-500 hover:text-primary flex items-center gap-1 transition-colors"
                    >
                      <span class="material-symbols-outlined text-sm">arrow_back</span>
                      Change configuration mode
                    </button>
                  </div>

                  <!-- Drop Zone (hidden in wizard mode) -->
                  <div
                    v-if="!isWizardMode"
                    @dragover="handleDragOver"
                    @dragleave="handleDragLeave"
                    @drop="handleDrop"
                    :class="[
                      'relative border-2 border-dashed rounded-2xl p-8 text-center transition-all duration-300 group',
                      isDragging
                        ? 'border-teal bg-teal/5 scale-[1.02]'
                        : 'border-gray-300 dark:border-gray-700 hover:border-teal hover:bg-gray-50 dark:hover:bg-zinc-800/50'
                    ]"
                  >
                    <div class="flex flex-col items-center">
                      <div :class="[
                        'w-16 h-16 rounded-2xl flex items-center justify-center mb-4 transition-all duration-300',
                        isDragging
                          ? 'bg-teal/20 scale-110'
                          : 'bg-gray-100 dark:bg-zinc-800 group-hover:bg-teal/10 group-hover:scale-105'
                      ]">
                        <span :class="[
                          'material-symbols-outlined text-4xl transition-colors',
                          isDragging ? 'text-teal' : 'text-gray-400 group-hover:text-teal'
                        ]">
                          {{ isDragging ? 'file_download' : 'cloud_upload' }}
                        </span>
                      </div>
                      <p class="text-gray-600 dark:text-gray-400 mb-3">
                        <span class="font-medium">Drop files here</span> or click to browse
                      </p>
                      <label class="inline-flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-xl hover:shadow-lg hover:shadow-primary/25 hover:-translate-y-0.5 transition-all duration-200 cursor-pointer font-medium">
                        <span class="material-symbols-outlined text-lg">folder_open</span>
                        Browse Files
                        <input type="file" multiple class="hidden" @change="handleFileSelect" />
                      </label>
                    </div>
                  </div>

                  <!-- Selected Files (hidden in wizard mode) -->
                  <Transition
                    enter-active-class="duration-300 ease-out"
                    enter-from-class="opacity-0 translate-y-2"
                    enter-to-class="opacity-100 translate-y-0"
                  >
                    <div v-if="uploadFiles.length > 0 && !isWizardMode" class="mt-4">
                      <div class="flex items-center justify-between mb-2">
                        <p class="text-sm font-medium text-gray-700 dark:text-gray-300">
                          Selected ({{ uploadFiles.length }})
                        </p>
                        <span class="text-xs text-gray-500">
                          {{ formatSize(uploadFiles.reduce((acc, f) => acc + f.size, 0)) }} total
                        </span>
                      </div>
                      <div class="space-y-2 max-h-32 overflow-y-auto scrollbar-thin">
                        <TransitionGroup
                          enter-active-class="duration-200 ease-out"
                          enter-from-class="opacity-0 translate-x-2"
                          enter-to-class="opacity-100 translate-x-0"
                          leave-active-class="duration-150 ease-in"
                          leave-from-class="opacity-100"
                          leave-to-class="opacity-0 -translate-x-2"
                        >
                          <div
                            v-for="(file, index) in uploadFiles"
                            :key="file.name + index"
                            class="flex items-center gap-3 p-3 bg-gray-50 dark:bg-zinc-800/50 rounded-xl border border-gray-100 dark:border-gray-800 group/file hover:border-teal/30 transition-colors"
                          >
                            <div class="w-10 h-10 rounded-lg bg-teal/10 flex items-center justify-center flex-shrink-0">
                              <span class="material-symbols-outlined text-teal">description</span>
                            </div>
                            <div class="flex-1 min-w-0">
                              <p class="text-sm font-medium text-gray-700 dark:text-gray-300 truncate">{{ file.name }}</p>
                              <p class="text-xs text-gray-500">{{ formatSize(file.size) }}</p>
                            </div>
                            <button
                              @click="removeFile(index)"
                              class="w-8 h-8 rounded-lg flex items-center justify-center text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors opacity-0 group-hover/file:opacity-100"
                            >
                              <span class="material-symbols-outlined text-lg">close</span>
                            </button>
                          </div>
                        </TransitionGroup>
                      </div>

                      <!-- Change configuration mode link (only in 'same' mode with multiple files) -->
                      <button
                        v-if="configMode === 'same' && isMultiFileUpload"
                        @click="goBackToModeSelection"
                        class="mt-3 text-sm text-gray-500 hover:text-primary flex items-center gap-1 transition-colors"
                      >
                        <span class="material-symbols-outlined text-sm">arrow_back</span>
                        Change configuration mode
                      </button>
                    </div>
                  </Transition>

                  <!-- Basic Metadata -->
                  <div class="mt-5 space-y-4">
                    <div>
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Document Name</label>
                      <div class="relative">
                        <span class="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-gray-400 text-lg">badge</span>
                        <input
                          v-model="basicMetadata.name"
                          type="text"
                          class="w-full pl-10 pr-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-teal/20 focus:border-teal transition-all"
                          placeholder="Uses filename if empty"
                        />
                      </div>
                    </div>
                    <div>
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Description</label>
                      <div class="relative">
                        <span class="absolute left-3 top-3 material-symbols-outlined text-gray-400 text-lg">notes</span>
                        <textarea
                          v-model="basicMetadata.description"
                          rows="2"
                          class="w-full pl-10 pr-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-teal/20 focus:border-teal transition-all resize-none"
                          placeholder="Add a description..."
                        ></textarea>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Section 2: Content Type Metadata -->
                <div class="p-6">
                  <div class="flex items-center gap-3 mb-5">
                    <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-primary to-primary/70 flex items-center justify-center text-white text-sm font-bold shadow-lg shadow-primary/25">
                      2
                    </div>
                    <div class="flex-1">
                      <h4 class="text-base font-semibold text-gray-900 dark:text-white">Content Type & Metadata</h4>
                      <p class="text-xs text-gray-500">Define document properties</p>
                    </div>
                    <span
                      v-if="hasRequiredContentType"
                      class="px-2 py-1 text-xs font-medium bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 rounded-lg"
                    >
                      Required
                    </span>
                  </div>

                  <!-- Content Type Selection -->
                  <div v-if="effectiveContentTypes.length > 0 || isLoadingContentTypes" class="mb-5">
                    <div v-if="isLoadingContentTypes" class="flex flex-col items-center justify-center py-12">
                      <div class="w-12 h-12 rounded-xl bg-primary/10 dark:bg-primary/20 flex items-center justify-center mb-3">
                        <span class="material-symbols-outlined animate-spin text-primary dark:text-primary text-2xl">progress_activity</span>
                      </div>
                      <p class="text-sm text-gray-500">Loading content types...</p>
                    </div>
                    <div v-else>
                      <UiSelect
                        v-model="selectedContentTypeId"
                        :options="contentTypeOptions"
                        label="Select Content Type"
                        placeholder="-- Select Content Type --"
                        :clearable="!hasRequiredContentType"
                        searchable
                      />

                      <!-- Content Type Info Card -->
                      <Transition
                        enter-active-class="duration-300 ease-out"
                        enter-from-class="opacity-0 translate-y-2"
                        enter-to-class="opacity-100 translate-y-0"
                      >
                        <div
                          v-if="selectedEffectiveContentType"
                          class="mt-2 px-3 py-2 rounded-lg border transition-colors"
                          :style="{
                            borderColor: selectedEffectiveContentType.color || '#00ae8c',
                            backgroundColor: (selectedEffectiveContentType.color || '#00ae8c') + '08'
                          }"
                        >
                          <div class="flex items-center gap-2">
                            <div
                              class="w-7 h-7 rounded-md flex items-center justify-center flex-shrink-0"
                              :style="{ backgroundColor: selectedEffectiveContentType.color || '#00ae8c' }"
                            >
                              <span class="material-symbols-outlined text-white text-base">
                                {{ getMaterialIcon(selectedEffectiveContentType.icon) }}
                              </span>
                            </div>
                            <div class="flex-1 min-w-0 overflow-hidden">
                              <div class="flex items-center gap-2">
                                <p class="text-sm font-medium text-gray-900 dark:text-white truncate" :title="selectedEffectiveContentType.name">
                                  {{ selectedEffectiveContentType.name }}
                                </p>
                                <span
                                  v-if="selectedEffectiveContentType.isDefault"
                                  class="inline-flex items-center px-1.5 py-0.5 text-[10px] font-medium bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded whitespace-nowrap"
                                >
                                  Default
                                </span>
                              </div>
                              <p v-if="selectedEffectiveContentType.category" class="text-xs text-gray-500 truncate">
                                {{ selectedEffectiveContentType.category }}
                              </p>
                            </div>
                          </div>
                        </div>
                      </Transition>

                      <p v-if="!hasRequiredContentType && !selectedContentTypeId" class="text-xs text-gray-500 mt-2 flex items-center gap-1">
                        <span class="material-symbols-outlined text-xs">info</span>
                        Optional - select to add custom metadata fields
                      </p>
                    </div>
                  </div>

                  <!-- No content types message -->
                  <div v-else class="py-12 text-center">
                    <div class="w-16 h-16 rounded-2xl bg-gray-100 dark:bg-zinc-800 flex items-center justify-center mx-auto mb-4">
                      <span class="material-symbols-outlined text-4xl text-gray-400">category</span>
                    </div>
                    <p class="text-sm font-medium text-gray-700 dark:text-gray-300">No Content Types Available</p>
                    <p class="text-xs text-gray-500 mt-1">Documents can still be uploaded without metadata</p>
                  </div>

                  <!-- Dynamic Metadata Fields -->
                  <Transition
                    enter-active-class="duration-300 ease-out"
                    enter-from-class="opacity-0"
                    enter-to-class="opacity-100"
                  >
                    <div v-if="selectedContentType && selectedContentType.fields && selectedContentType.fields.length > 0">
                      <div class="border-t border-gray-200 dark:border-gray-700/50 pt-5">
                        <div class="flex items-center gap-2 mb-4">
                          <span class="material-symbols-outlined text-primary">edit_note</span>
                          <h5 class="text-sm font-semibold text-gray-900 dark:text-white">Metadata Fields</h5>
                          <span class="text-xs text-gray-500">({{ selectedContentType.fields.length }} fields)</span>
                        </div>

                        <div class="space-y-5 max-h-[300px] overflow-y-auto pr-2 scrollbar-thin">
                          <div v-for="(fields, groupName) in groupedFields" :key="groupName">
                            <p v-if="Object.keys(groupedFields).length > 1" class="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3 flex items-center gap-2">
                              <span class="material-symbols-outlined text-xs">folder_special</span>
                              {{ groupName }}
                            </p>
                            <div class="grid grid-cols-1 gap-4">
                              <div
                                v-for="field in fields"
                                :key="field.id"
                                class="group"
                              >
                                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5 flex items-center gap-1">
                                  {{ field.displayName }}
                                  <span v-if="field.isRequired" class="text-red-500 text-xs">*</span>
                                </label>

                                <!-- Text -->
                                <input
                                  v-if="field.fieldType === 'Text'"
                                  v-model="fieldValues[field.fieldName]"
                                  type="text"
                                  :placeholder="field.description || 'Enter text...'"
                                  :readonly="field.isReadOnly"
                                  class="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                                />

                                <!-- TextArea -->
                                <textarea
                                  v-else-if="field.fieldType === 'TextArea'"
                                  v-model="fieldValues[field.fieldName]"
                                  rows="2"
                                  :placeholder="field.description || 'Enter text...'"
                                  :readonly="field.isReadOnly"
                                  class="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all resize-none"
                                ></textarea>

                                <!-- Number -->
                                <input
                                  v-else-if="field.fieldType === 'Number'"
                                  v-model.number="fieldValues[field.fieldName]"
                                  type="number"
                                  :placeholder="field.description || 'Enter number...'"
                                  :readonly="field.isReadOnly"
                                  class="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                                />

                                <!-- Decimal -->
                                <input
                                  v-else-if="field.fieldType === 'Decimal'"
                                  v-model.number="fieldValues[field.fieldName]"
                                  type="number"
                                  step="0.01"
                                  :placeholder="field.description || 'Enter decimal...'"
                                  :readonly="field.isReadOnly"
                                  class="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                                />

                                <!-- Date -->
                                <UiDatePicker
                                  v-else-if="field.fieldType === 'Date'"
                                  v-model="fieldValues[field.fieldName]"
                                  :placeholder="field.description || 'Select date...'"
                                  :disabled="field.isReadOnly"
                                  :clearable="!field.isRequired"
                                />

                                <!-- DateTime -->
                                <UiDatePicker
                                  v-else-if="field.fieldType === 'DateTime'"
                                  v-model="fieldValues[field.fieldName]"
                                  :placeholder="field.description || 'Select date and time...'"
                                  :disabled="field.isReadOnly"
                                  :clearable="!field.isRequired"
                                />

                                <!-- Boolean -->
                                <div v-else-if="field.fieldType === 'Boolean'" class="mt-1">
                                  <label class="inline-flex items-center gap-3 cursor-pointer group/check">
                                    <div class="relative">
                                      <input
                                        v-model="fieldValues[field.fieldName]"
                                        type="checkbox"
                                        :disabled="field.isReadOnly"
                                        class="sr-only peer"
                                      />
                                      <div class="w-10 h-6 bg-gray-200 dark:bg-zinc-700 rounded-full peer-checked:bg-primary transition-colors"></div>
                                      <div class="absolute left-1 top-1 w-4 h-4 bg-white rounded-full shadow peer-checked:translate-x-4 transition-transform"></div>
                                    </div>
                                    <span class="text-sm text-gray-600 dark:text-gray-400">{{ field.description || 'Enable' }}</span>
                                  </label>
                                </div>

                                <!-- Dropdown -->
                                <UiSelect
                                  v-else-if="field.fieldType === 'Dropdown'"
                                  v-model="fieldValues[field.fieldName]"
                                  :options="getFieldOptions(field)"
                                  :placeholder="field.description || 'Select option...'"
                                  :disabled="field.isReadOnly"
                                  :clearable="!field.isRequired"
                                />

                                <!-- MultiSelect -->
                                <select
                                  v-else-if="field.fieldType === 'MultiSelect'"
                                  v-model="fieldValues[field.fieldName]"
                                  multiple
                                  :disabled="field.isReadOnly"
                                  class="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all h-24"
                                >
                                  <option v-for="opt in getFieldOptions(field)" :key="opt.value" :value="opt.value">
                                    {{ opt.label }}
                                  </option>
                                </select>

                                <!-- Lookup -->
                                <div v-else-if="field.fieldType === 'Lookup'" class="relative">
                                  <UiSelect
                                    v-model="fieldValues[field.fieldName]"
                                    :options="getLookupOptions(field)"
                                    :placeholder="field.description || 'Select option...'"
                                    :disabled="field.isReadOnly || isLoadingLookups"
                                    :clearable="!field.isRequired"
                                    searchable
                                  />
                                  <span v-if="isLoadingLookups" class="absolute right-10 top-1/2 -translate-y-1/2 material-symbols-outlined text-primary animate-spin text-sm z-10">progress_activity</span>
                                </div>

                                <!-- User -->
                                <div v-else-if="field.fieldType === 'User'" class="relative">
                                  <UiSelect
                                    v-model="fieldValues[field.fieldName]"
                                    :options="getUserOptions()"
                                    placeholder="Select User..."
                                    :disabled="field.isReadOnly || isLoadingLookups"
                                    :clearable="!field.isRequired"
                                    searchable
                                  />
                                  <span v-if="isLoadingLookups" class="absolute right-10 top-1/2 -translate-y-1/2 material-symbols-outlined text-primary animate-spin text-sm z-10">progress_activity</span>
                                </div>

                                <!-- Default to text input -->
                                <input
                                  v-else
                                  v-model="fieldValues[field.fieldName]"
                                  type="text"
                                  :placeholder="field.description || 'Enter value...'"
                                  :readonly="field.isReadOnly"
                                  class="w-full px-4 py-2.5 border border-gray-300 dark:border-gray-700 rounded-xl bg-white dark:bg-zinc-800 text-gray-900 dark:text-white text-sm focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                                />

                                <p v-if="field.description && !['Boolean', 'Text', 'TextArea'].includes(field.fieldType)" class="text-xs text-gray-500 mt-1">
                                  {{ field.description }}
                                </p>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </Transition>
                </div>
              </div>
            </div>

            <!-- Footer with Upload Progress and Actions -->
            <div class="border-t border-gray-200 dark:border-gray-700/50 p-6 bg-gray-50 dark:bg-zinc-800/50">
              <!-- Upload Progress -->
              <Transition
                enter-active-class="duration-300 ease-out"
                enter-from-class="opacity-0 -translate-y-2"
                enter-to-class="opacity-100 translate-y-0"
              >
                <div v-if="isUploading" class="mb-4">
                  <div class="flex items-center justify-between text-sm mb-2">
                    <span class="text-gray-600 dark:text-gray-400 flex items-center gap-2">
                      <span class="material-symbols-outlined animate-spin text-teal">progress_activity</span>
                      Uploading {{ uploadFiles.length }} file{{ uploadFiles.length > 1 ? 's' : '' }}...
                    </span>
                    <span class="font-semibold text-teal">{{ uploadProgress }}%</span>
                  </div>
                  <div class="h-2 bg-gray-200 dark:bg-zinc-700 rounded-full overflow-hidden">
                    <div
                      class="h-full bg-gradient-to-r from-primary/80 to-primary transition-all duration-300 rounded-full"
                      :style="{ width: uploadProgress + '%' }"
                    ></div>
                  </div>
                </div>
              </Transition>

              <!-- Wizard Mode: Apply to remaining checkbox (not on last file) -->
              <div v-if="isWizardMode && !isUploading && !isLastWizardStep" class="mb-4 pb-4 border-b border-gray-200 dark:border-gray-700">
                <label class="inline-flex items-center gap-3 cursor-pointer group">
                  <input
                    v-model="applyToRemaining"
                    type="checkbox"
                    class="w-4 h-4 rounded border-gray-300 text-teal focus:ring-teal/20"
                  />
                  <span class="text-sm text-gray-600 dark:text-gray-400 group-hover:text-gray-900 dark:group-hover:text-gray-200 transition-colors">
                    Apply this content type to remaining {{ totalWizardSteps - currentFileIndex - 1 }} file(s)
                  </span>
                </label>
              </div>

              <!-- Actions -->
              <div class="flex items-center justify-between">
                <div class="text-sm text-gray-500">
                  <!-- Mode Selection Screen - No Status -->
                  <span v-if="needsConfigModeSelection" class="flex items-center gap-1">
                    <span class="material-symbols-outlined text-sm text-primary">info</span>
                    Select how to configure your files
                  </span>
                  <!-- Wizard Mode Status -->
                  <span v-else-if="isWizardMode" class="flex items-center gap-1">
                    <span class="material-symbols-outlined text-sm text-teal">description</span>
                    Configuring: {{ currentFile?.name }}
                  </span>
                  <!-- Normal Mode Status -->
                  <span v-else-if="uploadFiles.length > 0" class="flex items-center gap-1">
                    <span class="material-symbols-outlined text-sm">check_circle</span>
                    {{ uploadFiles.length }} file{{ uploadFiles.length > 1 ? 's' : '' }} selected
                  </span>
                </div>

                <!-- Action Buttons -->
                <div class="flex items-center gap-3">
                  <!-- Mode Selection: Only Cancel -->
                  <template v-if="needsConfigModeSelection">
                    <button
                      @click="emit('close')"
                      class="px-5 py-2.5 border border-gray-300 dark:border-gray-600 rounded-xl hover:bg-gray-100 dark:hover:bg-zinc-700 text-gray-700 dark:text-gray-300 font-medium transition-all"
                    >
                      Cancel
                    </button>
                  </template>

                  <!-- Wizard Mode Navigation -->
                  <template v-else-if="isWizardMode">
                    <button
                      @click="wizardBack"
                      :disabled="currentFileIndex === 0 || isUploading"
                      class="px-4 py-2.5 border border-gray-300 dark:border-gray-600 rounded-xl hover:bg-gray-100 dark:hover:bg-zinc-700 text-gray-700 dark:text-gray-300 font-medium transition-all disabled:opacity-50 flex items-center gap-1.5"
                    >
                      <span class="material-symbols-outlined text-lg">arrow_back</span>
                      Back
                    </button>

                    <button
                      @click="wizardSkip"
                      :disabled="isUploading"
                      class="px-4 py-2.5 border border-gray-300 dark:border-gray-600 rounded-xl hover:bg-gray-100 dark:hover:bg-zinc-700 text-gray-700 dark:text-gray-300 font-medium transition-all disabled:opacity-50"
                    >
                      Skip
                    </button>

                    <button
                      v-if="!isLastWizardStep && !applyToRemaining"
                      @click="wizardNext"
                      :disabled="!canProceedWizardStep || isUploading"
                      class="px-6 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-xl hover:shadow-lg hover:shadow-primary/25 hover:-translate-y-0.5 disabled:opacity-50 disabled:hover:shadow-none disabled:hover:translate-y-0 font-medium transition-all flex items-center gap-2"
                    >
                      Next
                      <span class="material-symbols-outlined text-lg">arrow_forward</span>
                    </button>

                    <button
                      v-else
                      @click="wizardNext"
                      :disabled="!canProceedWizardStep || isUploading"
                      class="px-6 py-2.5 bg-gradient-to-r from-teal to-primary text-white rounded-xl hover:shadow-lg hover:shadow-teal/25 hover:-translate-y-0.5 disabled:opacity-50 disabled:hover:shadow-none disabled:hover:translate-y-0 font-medium transition-all flex items-center gap-2"
                    >
                      <span v-if="isUploading" class="material-symbols-outlined animate-spin text-lg">progress_activity</span>
                      <span v-else class="material-symbols-outlined text-lg">cloud_upload</span>
                      {{ isUploading ? 'Uploading...' : (applyToRemaining ? 'Apply & Upload All' : 'Upload All') }}
                    </button>
                  </template>

                  <!-- Normal Mode -->
                  <template v-else>
                    <button
                      @click="emit('close')"
                      :disabled="isUploading"
                      class="px-5 py-2.5 border border-gray-300 dark:border-gray-600 rounded-xl hover:bg-gray-100 dark:hover:bg-zinc-700 text-gray-700 dark:text-gray-300 font-medium transition-all disabled:opacity-50"
                    >
                      Cancel
                    </button>
                    <button
                      @click="handleUpload"
                      :disabled="!canUpload || isUploading"
                      class="px-6 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-xl hover:shadow-lg hover:shadow-primary/25 hover:-translate-y-0.5 disabled:opacity-50 disabled:hover:shadow-none disabled:hover:translate-y-0 font-medium transition-all flex items-center gap-2"
                    >
                      <span v-if="isUploading" class="material-symbols-outlined animate-spin text-lg">progress_activity</span>
                      <span v-else class="material-symbols-outlined text-lg">cloud_upload</span>
                      {{ isUploading ? 'Uploading...' : 'Upload' }}
                    </button>
                  </template>
                </div>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.scrollbar-thin::-webkit-scrollbar {
  width: 6px;
}
.scrollbar-thin::-webkit-scrollbar-track {
  background: transparent;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: #d1d5db;
  border-radius: 3px;
}
.scrollbar-thin::-webkit-scrollbar-thumb:hover {
  background: #9ca3af;
}
.dark .scrollbar-thin::-webkit-scrollbar-thumb {
  background: #4b5563;
}
.dark .scrollbar-thin::-webkit-scrollbar-thumb:hover {
  background: #6b7280;
}
</style>
