<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import type { Document, PreviewInfo, DocumentMetadata, ContentTypeDefinition } from '@/types'
import { documentsApi, documentPasswordsApi, contentTypeDefinitionsApi, permissionsApi, documentAnnotationsApi } from '@/api/client'
import VirtualizedPdfViewer from './VirtualizedPdfViewer.vue'
import AnnotationToolbar from './AnnotationToolbar.vue'
import SignaturePadModal from './SignaturePadModal.vue'
import { useAnnotations } from '@/composables/useAnnotations'

// Store validated document passwords in session (documentId -> true)
const validatedPasswords = new Map<string, boolean>()

const props = defineProps<{
  modelValue: boolean
  document: Document | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

// Refs
const viewerContainer = ref<HTMLElement | null>(null)
const pdfScrollContainer = ref<HTMLElement | null>(null)
const pdfViewerRef = ref<InstanceType<typeof VirtualizedPdfViewer> | null>(null)

// State
const previewInfo = ref<PreviewInfo | null>(null)
const isLoading = ref(false)
const error = ref<string | null>(null)
const blobUrl = ref<string | null>(null)
const textContent = ref<string>('')
const pdfSource = ref<string | null>(null)

// Password protection state
const requiresPassword = ref(false)
const passwordHint = ref<string | null>(null)
const enteredPassword = ref('')
const passwordError = ref('')
const isValidatingPassword = ref(false)
const showPassword = ref(false)

// PDF State
const totalPages = ref(1)
const currentPage = ref(1)

// View controls
const zoom = ref(100)
const rotation = ref(0)
const isDarkMode = ref(false)
const isFullscreen = ref(false)

// Metadata panel
const showMetadataPanel = ref(true)
const metadata = ref<DocumentMetadata[]>([])
const contentTypeInfo = ref<ContentTypeDefinition | null>(null)
const isLoadingMetadata = ref(false)

// Annotation system
const {
  isAnnotationMode,
  activeTool,
  toolSettings,
  hasUnsavedChanges: annotationUnsavedChanges,
  isSaving: isAnnotationSaving,
  canUndo,
  canRedo,
  annotationDataMap,
  loadAnnotations,
  saveAnnotations,
  discardChanges: discardAnnotationChanges,
  enterAnnotationMode,
  exitAnnotationMode,
  deleteSelected,
  setTool,
  undo: undoAnnotation,
  redo: redoAnnotation
} = useAnnotations()

const hasWritePermission = ref(false)
const hasAnnotations = ref(false)
const showSignatureModal = ref(false)
const annotationReadOnly = ref(false)

// Base PDF page width (A4 at ~96dpi is roughly 794px, we use 700 as comfortable default)
const BASE_PDF_WIDTH = 700

// Page navigation
function prevPage() {
  if (currentPage.value > 1) {
    currentPage.value--
    pdfViewerRef.value?.scrollToPage(currentPage.value)
  }
}

function nextPage() {
  if (currentPage.value < totalPages.value) {
    currentPage.value++
    pdfViewerRef.value?.scrollToPage(currentPage.value)
  }
}

function goToPage(page: number) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
    pdfViewerRef.value?.scrollToPage(page)
  }
}

// Handle PDF loaded event
function handlePdfLoaded(data: { numPages: number }) {
  totalPages.value = data.numPages
}

// Handle page change from virtualized viewer
function handlePageChange(page: number) {
  currentPage.value = page
}

// Keyboard shortcuts
function handleKeydown(e: KeyboardEvent) {
  if (!isOpen.value) return

  switch (e.key) {
    case 'Escape':
      if (isFullscreen.value) {
        exitFullscreen()
      } else {
        isOpen.value = false
      }
      break
    case '+':
    case '=':
      if (e.ctrlKey) {
        e.preventDefault()
        zoomIn()
      }
      break
    case '-':
      if (e.ctrlKey) {
        e.preventDefault()
        zoomOut()
      }
      break
    case '0':
      if (e.ctrlKey) {
        e.preventDefault()
        resetZoom()
      }
      break
    case 'ArrowLeft':
    case 'ArrowUp':
      if (!isAnnotationMode.value) {
        e.preventDefault()
        prevPage()
      }
      break
    case 'ArrowRight':
    case 'ArrowDown':
      if (!isAnnotationMode.value) {
        e.preventDefault()
        nextPage()
      }
      break
    case 'Home':
      e.preventDefault()
      goToPage(1)
      break
    case 'End':
      e.preventDefault()
      goToPage(totalPages.value)
      break
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
  cleanup()
})

watch(() => props.modelValue, async (newVal) => {
  if (newVal && props.document) {
    await loadPreview()
  } else {
    cleanup()
  }
})

function cleanup() {
  if (blobUrl.value) {
    URL.revokeObjectURL(blobUrl.value)
    blobUrl.value = null
  }
  if (pdfSource.value) {
    URL.revokeObjectURL(pdfSource.value)
    pdfSource.value = null
  }
  previewInfo.value = null
  textContent.value = ''
  error.value = null
  zoom.value = 100
  rotation.value = 0
  totalPages.value = 1
  currentPage.value = 1
  // Reset password state
  requiresPassword.value = false
  passwordHint.value = null
  enteredPassword.value = ''
  passwordError.value = ''
  isValidatingPassword.value = false
  showPassword.value = false
  // Reset metadata state
  metadata.value = []
  contentTypeInfo.value = null
  isLoadingMetadata.value = false
  // Reset annotation state
  exitAnnotationMode()
  hasWritePermission.value = false
  hasAnnotations.value = false
  annotationReadOnly.value = false
}

async function loadPreview() {
  if (!props.document) return

  isLoading.value = true
  error.value = null

  try {
    // Check if document has password protection
    const hasPassword = await documentPasswordsApi.hasPassword(props.document.id)

    if (hasPassword.data && !validatedPasswords.has(props.document.id)) {
      // Document requires password and hasn't been validated yet
      isLoading.value = false
      requiresPassword.value = true
      // Get hint if available
      try {
        const hintResponse = await documentPasswordsApi.getHint(props.document.id)
        passwordHint.value = hintResponse.data || null
      } catch {
        passwordHint.value = null
      }
      return
    }

    await loadPreviewContent()
  } catch (err: any) {
    error.value = err.response?.data?.message || err.message || 'Failed to load preview'
    isLoading.value = false
  }
}

async function validateAndLoadPreview() {
  if (!props.document || !enteredPassword.value) return

  isValidatingPassword.value = true
  passwordError.value = ''

  try {
    const isValid = await documentPasswordsApi.validatePassword(props.document.id, enteredPassword.value)

    if (isValid.data) {
      // Password correct - mark as validated and load preview
      validatedPasswords.set(props.document.id, true)
      requiresPassword.value = false
      enteredPassword.value = ''
      isLoading.value = true
      await loadPreviewContent()
    } else {
      passwordError.value = 'Incorrect password. Please try again.'
    }
  } catch (err: any) {
    passwordError.value = err.response?.data?.message || 'Failed to validate password'
  } finally {
    isValidatingPassword.value = false
  }
}

async function loadPreviewContent() {
  if (!props.document) return

  try {
    // Load preview info first
    const infoResponse = await documentsApi.getPreviewInfo(props.document.id)
    previewInfo.value = infoResponse.data

    // Load metadata in background (non-blocking)
    loadMetadata()

    // Check annotation permissions and load read-only annotations
    checkAnnotationPermissions().then(async () => {
      if (hasAnnotations.value && previewInfo.value?.type === 'Pdf' && props.document) {
        await loadAnnotations(props.document.id)
        annotationReadOnly.value = true
      }
    })

    // Handle text files with content from API
    if (previewInfo.value.type === 'Text' && previewInfo.value.textContent) {
      textContent.value = previewInfo.value.textContent
    }
    // Handle PDF with vue-pdf-embed
    else if (previewInfo.value.type === 'Pdf') {
      const response = await documentsApi.download(props.document.id)
      const blob = response.data as Blob
      pdfSource.value = URL.createObjectURL(blob)
    }
    // Handle other files that need to be downloaded (Image, Video, Audio)
    else if (['Image', 'Video', 'Audio'].includes(previewInfo.value.type)) {
      const response = await documentsApi.download(props.document.id)
      const blob = response.data as Blob
      blobUrl.value = URL.createObjectURL(blob)
    }
    // Handle text files that need to be downloaded
    else if (previewInfo.value.type === 'Text' && !previewInfo.value.textContent) {
      const response = await documentsApi.download(props.document.id)
      const blob = response.data as Blob
      textContent.value = await blob.text()
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || err.message || 'Failed to load preview'
  } finally {
    isLoading.value = false
  }
}

// Handle PDF rendering errors
function handlePdfError(err: any) {
  error.value = 'Failed to render PDF. Please try downloading the file instead.'
}

// Zoom controls (for images)
function zoomIn() {
  zoom.value = Math.min(300, zoom.value + 25)
}

function zoomOut() {
  zoom.value = Math.max(50, zoom.value - 25)
}

function resetZoom() {
  zoom.value = 100
}

// Rotation (for images)
function rotate() {
  rotation.value = (rotation.value + 90) % 360
}

// Fullscreen
function toggleFullscreen() {
  if (isFullscreen.value) {
    exitFullscreen()
  } else {
    enterFullscreen()
  }
}

function enterFullscreen() {
  const elem = viewerContainer.value
  if (elem?.requestFullscreen) {
    elem.requestFullscreen()
    isFullscreen.value = true
  }
}

function exitFullscreen() {
  if (document.exitFullscreen) {
    document.exitFullscreen()
    isFullscreen.value = false
  }
}

// Download
async function handleDownload() {
  if (!props.document) return

  try {
    // Check if document has password and hasn't been validated
    const hasPassword = await documentPasswordsApi.hasPassword(props.document.id)
    if (hasPassword.data && !validatedPasswords.has(props.document.id)) {
      // Show password prompt
      requiresPassword.value = true
      try {
        const hintResponse = await documentPasswordsApi.getHint(props.document.id)
        passwordHint.value = hintResponse.data || null
      } catch {
        passwordHint.value = null
      }
      return
    }

    const response = await documentsApi.download(props.document.id)
    const blob = new Blob([response.data])
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = props.document.name + (props.document.extension || '')
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } catch (err) {
  }
}

// Print
function handlePrint() {
  window.print()
}

// Helpers
function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

function getFileIcon(type: string): string {
  const icons: Record<string, string> = {
    'Pdf': 'picture_as_pdf',
    'Image': 'image',
    'Text': 'description',
    'Video': 'movie',
    'Audio': 'music_note',
    'Office': 'article'
  }
  return icons[type] || 'insert_drive_file'
}

function getFileIconBg(type: string): string {
  const colors: Record<string, string> = {
    'Pdf': 'bg-red-100 dark:bg-red-900/30',
    'Image': 'bg-purple-100 dark:bg-purple-900/30',
    'Text': 'bg-slate-100 dark:bg-slate-700',
    'Video': 'bg-pink-100 dark:bg-pink-900/30',
    'Audio': 'bg-indigo-100 dark:bg-indigo-900/30',
    'Office': 'bg-blue-100 dark:bg-blue-900/30'
  }
  return colors[type] || 'bg-slate-100 dark:bg-slate-700'
}

function getFileIconColor(type: string): string {
  const colors: Record<string, string> = {
    'Pdf': 'text-red-600',
    'Image': 'text-purple-600',
    'Text': 'text-slate-600',
    'Video': 'text-pink-600',
    'Audio': 'text-indigo-600',
    'Office': 'text-blue-600'
  }
  return colors[type] || 'text-slate-500'
}

// Metadata functions
async function loadMetadata() {
  if (!props.document || isLoadingMetadata.value) return

  isLoadingMetadata.value = true
  try {
    const metadataResponse = await contentTypeDefinitionsApi.getDocumentMetadata(props.document.id)
    const metadataData = metadataResponse.data || []

    // Load content type definition to get field definitions
    if (metadataData.length > 0) {
      const contentTypeId = metadataData[0]?.contentTypeId
      if (contentTypeId) {
        const ctResponse = await contentTypeDefinitionsApi.getById(contentTypeId)
        contentTypeInfo.value = ctResponse.data
      }
    }

    // Set metadata after content type is loaded
    metadata.value = metadataData
  } catch (err) {
    // Silently fail - metadata is optional
    metadata.value = []
    contentTypeInfo.value = null
  } finally {
    isLoadingMetadata.value = false
  }
}

// Computed: Group metadata fields by group name
const groupedMetadata = computed(() => {
  if (!metadata.value.length || !contentTypeInfo.value?.fields) return {}

  const groups: Record<string, Array<{
    fieldName: string
    displayName: string
    value: string
    fieldType: string
    groupName: string
  }>> = {}

  for (const m of metadata.value) {
    const fieldDef = contentTypeInfo.value.fields.find(f => f.id === m.fieldId)
    if (!fieldDef) continue

    const groupName = fieldDef.groupName || 'General'
    if (!groups[groupName]) groups[groupName] = []

    // Get display value based on field type
    let displayValue = ''
    if (fieldDef.fieldType === 'Date' || fieldDef.fieldType === 'DateTime') {
      displayValue = m.dateValue ? formatMetadataDate(m.dateValue, fieldDef.fieldType === 'DateTime') : '-'
    } else if (fieldDef.fieldType === 'Number' || fieldDef.fieldType === 'Decimal') {
      displayValue = m.numericValue !== undefined && m.numericValue !== null ? String(m.numericValue) : '-'
    } else if (fieldDef.fieldType === 'Boolean') {
      displayValue = m.value === 'true' ? 'Yes' : 'No'
    } else {
      displayValue = m.value || '-'
    }

    groups[groupName].push({
      fieldName: m.fieldName,
      displayName: fieldDef.displayName || m.fieldName,
      value: displayValue,
      fieldType: fieldDef.fieldType,
      groupName
    })
  }

  return groups
})

const hasMetadata = computed(() => metadata.value.length > 0)

function formatMetadataDate(dateStr: string, includeTime = false): string {
  const date = new Date(dateStr)
  if (includeTime) {
    return date.toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  }
  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

function getFieldTypeIcon(fieldType: string): string {
  const icons: Record<string, string> = {
    'Text': 'text_fields',
    'TextArea': 'notes',
    'Number': 'numbers',
    'Decimal': 'decimal_increase',
    'Date': 'calendar_today',
    'DateTime': 'schedule',
    'Boolean': 'toggle_on',
    'Dropdown': 'arrow_drop_down_circle',
    'MultiSelect': 'checklist',
    'User': 'person',
    'Lookup': 'search'
  }
  return icons[fieldType] || 'label'
}

function toggleMetadataPanel() {
  showMetadataPanel.value = !showMetadataPanel.value
}

// Annotation functions
async function checkAnnotationPermissions() {
  if (!props.document) return
  try {
    // Check write permission
    const permResponse = await permissionsApi.checkPermission('Document', props.document.id, 2)
    hasWritePermission.value = !!permResponse.data
  } catch {
    hasWritePermission.value = false
  }

  try {
    // Check if annotations exist
    const countResponse = await documentAnnotationsApi.getCount(props.document.id)
    hasAnnotations.value = (countResponse.data || 0) > 0
  } catch {
    hasAnnotations.value = false
  }
}

async function handleEnterAnnotationMode() {
  if (!props.document) return
  enterAnnotationMode()
  annotationReadOnly.value = false
  await loadAnnotations(props.document.id)
}

async function handleSaveAnnotations() {
  if (!props.document) return
  try {
    await saveAnnotations()
    hasAnnotations.value = true
  } catch {
    // Error handled in composable
  }
}

function handleDiscardAnnotations() {
  discardAnnotationChanges()
}

async function handleExitAnnotationMode() {
  if (annotationUnsavedChanges.value) {
    if (!confirm('You have unsaved annotation changes. Discard them?')) return
  }
  exitAnnotationMode()
  // Reload annotations for read-only display
  if (props.document && hasAnnotations.value) {
    await loadAnnotations(props.document.id)
    annotationReadOnly.value = true
  }
}

function handleAnnotationToolChange(tool: any) {
  setTool(tool)
}

function handleToolSettingsChange(settings: any) {
  Object.assign(toolSettings, settings)
}

function handleOpenSignature() {
  showSignatureModal.value = true
}

function handleSignatureSelect(dataUrl: string) {
  // Insert onto the current page's annotation layer
  const pageNum = currentPage.value
  const layer = pdfViewerRef.value?.getAnnotationLayer(pageNum)
  if (layer) {
    layer.placeSignature(dataUrl)
  }
  setTool('select')
}

function handleAnnotationUndo() {
  undoAnnotation()
}

function handleAnnotationRedo() {
  redoAnnotation()
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
      <div
        v-if="isOpen"
        ref="viewerContainer"
        class="fixed inset-0 z-50 flex flex-col"
        :class="isDarkMode ? 'bg-slate-900' : 'bg-[#e2e8f0]'"
      >
        <!-- Header Toolbar - Light with Cyan Accents -->
        <header
          class="flex items-center justify-between px-4 py-2.5 border-b shrink-0 bg-white border-slate-200 shadow-sm"
        >
          <!-- Left: Back & Document Info -->
          <div class="flex items-center gap-3 min-w-0">
            <button
              @click="isOpen = false"
              class="p-2 rounded-lg transition-colors text-slate-500 hover:text-teal hover:bg-teal/10"
            >
              <span class="material-symbols-outlined">close</span>
            </button>

            <div v-if="previewInfo" class="flex items-center gap-3 min-w-0">
              <div class="w-9 h-9 rounded-lg flex items-center justify-center shrink-0 bg-[#0d1117]">
                <span class="material-symbols-outlined text-lg text-teal">
                  {{ getFileIcon(previewInfo.type) }}
                </span>
              </div>
              <div class="min-w-0">
                <h1 class="font-semibold text-sm truncate text-slate-800 flex items-center gap-1.5">
                  <span class="truncate">{{ document?.name }}{{ document?.extension }}</span>
                  <!-- Password Protected Badge -->
                  <div
                    v-if="document?.hasPassword"
                    class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-gradient-to-r from-violet-500/15 to-fuchsia-500/15 border border-violet-300/30 shrink-0"
                    title="Password protected"
                  >
                    <span
                      class="material-symbols-outlined text-violet-500"
                      style="font-size: 11px; font-variation-settings: 'FILL' 1;"
                    >shield_lock</span>
                    <span class="text-[8px] font-semibold text-violet-600 uppercase tracking-wide pr-0.5">Secured</span>
                  </div>
                </h1>
                <p class="text-xs text-slate-400">
                  {{ formatFileSize(previewInfo.fileSize) }}
                  <span v-if="totalPages > 1" class="text-teal font-medium"> · {{ totalPages }} pages</span>
                </p>
              </div>
            </div>
          </div>

          <!-- Center: Page Navigation (PDF only) -->
          <div
            v-if="previewInfo?.type === 'Pdf' && totalPages > 1"
            class="flex items-center gap-1 bg-slate-50 rounded-xl px-2 py-1 border border-slate-200"
          >
            <button
              @click="prevPage"
              :disabled="currentPage <= 1"
              class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-slate-500 hover:text-teal hover:bg-teal/10"
            >
              <span class="material-symbols-outlined text-xl">chevron_left</span>
            </button>

            <div class="flex items-center gap-1.5 px-2">
              <input
                v-model.number="currentPage"
                type="number"
                min="1"
                :max="totalPages"
                @change="goToPage(currentPage)"
                class="w-12 px-2 py-1 text-center text-sm rounded-lg border border-slate-200 bg-white text-slate-800 font-medium focus:border-teal focus:ring-1 focus:ring-teal/20 outline-none"
              />
              <span class="text-slate-400">/</span>
              <span class="text-slate-600 font-medium">{{ totalPages }}</span>
            </div>

            <button
              @click="nextPage"
              :disabled="currentPage >= totalPages"
              class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-slate-500 hover:text-teal hover:bg-teal/10"
            >
              <span class="material-symbols-outlined text-xl">chevron_right</span>
            </button>
          </div>

          <!-- Right: View Controls & Actions -->
          <div class="flex items-center gap-1">
            <!-- Zoom Controls -->
            <div
              v-if="previewInfo?.type === 'Pdf' || previewInfo?.type === 'Image'"
              class="flex items-center gap-0.5 px-1.5 py-1 rounded-xl mr-2 bg-slate-50 border border-slate-200"
            >
              <button
                @click="zoomOut"
                :disabled="zoom <= 50"
                class="p-1 rounded-lg transition-colors disabled:opacity-30 text-slate-500 hover:text-teal hover:bg-teal/10"
              >
                <span class="material-symbols-outlined text-lg">remove</span>
              </button>

              <span class="w-12 text-center text-xs font-bold text-teal">
                {{ zoom }}%
              </span>

              <button
                @click="zoomIn"
                :disabled="zoom >= 300"
                class="p-1 rounded-lg transition-colors disabled:opacity-30 text-slate-500 hover:text-teal hover:bg-teal/10"
              >
                <span class="material-symbols-outlined text-lg">add</span>
              </button>
            </div>

            <!-- Rotate (Image only) -->
            <button
              v-if="previewInfo?.type === 'Image'"
              @click="rotate"
              class="p-2 rounded-lg transition-colors text-slate-500 hover:text-teal hover:bg-teal/10"
              title="Rotate"
            >
              <span class="material-symbols-outlined">rotate_right</span>
            </button>

            <div class="w-px h-6 mx-1 bg-slate-200"></div>

            <!-- Theme Toggle -->
            <button
              @click="isDarkMode = !isDarkMode"
              class="p-2 rounded-lg transition-colors text-slate-500 hover:text-teal hover:bg-teal/10"
              title="Toggle theme"
            >
              <span class="material-symbols-outlined">{{ isDarkMode ? 'light_mode' : 'dark_mode' }}</span>
            </button>

            <!-- Fullscreen -->
            <button
              @click="toggleFullscreen"
              class="p-2 rounded-lg transition-colors text-slate-500 hover:text-teal hover:bg-teal/10"
              title="Fullscreen"
            >
              <span class="material-symbols-outlined">{{ isFullscreen ? 'fullscreen_exit' : 'fullscreen' }}</span>
            </button>

            <div class="w-px h-6 mx-1 bg-slate-200"></div>

            <!-- Print -->
            <button
              @click="handlePrint"
              class="p-2 rounded-lg transition-colors text-slate-500 hover:text-teal hover:bg-teal/10"
              title="Print"
            >
              <span class="material-symbols-outlined">print</span>
            </button>

            <!-- Annotate (PDF only, Write permission required) -->
            <button
              v-if="previewInfo?.type === 'Pdf' && hasWritePermission && !isAnnotationMode"
              @click="handleEnterAnnotationMode"
              class="p-2 rounded-lg transition-colors text-amber-500 hover:bg-amber-50"
              title="Annotate PDF"
            >
              <span class="material-symbols-outlined">edit_note</span>
            </button>

            <!-- Download -->
            <button
              @click="handleDownload"
              class="p-2 rounded-lg transition-colors text-teal hover:bg-teal/10"
              title="Download"
            >
              <span class="material-symbols-outlined">download</span>
            </button>

            <div class="w-px h-6 mx-1 bg-slate-200"></div>

            <!-- Metadata Panel Toggle -->
            <button
              v-if="hasMetadata"
              @click="toggleMetadataPanel"
              class="p-2 rounded-lg transition-colors"
              :class="showMetadataPanel ? 'text-teal bg-teal/10' : 'text-slate-500 hover:text-teal hover:bg-teal/10'"
              title="Toggle metadata panel"
            >
              <span class="material-symbols-outlined">info</span>
            </button>

          </div>
        </header>

        <!-- Annotation Toolbar -->
        <AnnotationToolbar
          v-if="isAnnotationMode"
          :active-tool="activeTool"
          :tool-settings="toolSettings"
          :is-saving="isAnnotationSaving"
          :has-unsaved-changes="annotationUnsavedChanges"
          :can-undo="canUndo"
          :can-redo="canRedo"
          @update:active-tool="handleAnnotationToolChange"
          @update:tool-settings="handleToolSettingsChange"
          @save="handleSaveAnnotations"
          @discard="handleDiscardAnnotations"
          @close="handleExitAnnotationMode"
          @undo="handleAnnotationUndo"
          @redo="handleAnnotationRedo"
          @delete-selected="deleteSelected"
          @open-signature="handleOpenSignature"
        />

        <!-- Signature Pad Modal -->
        <SignaturePadModal
          v-model="showSignatureModal"
          @select-signature="handleSignatureSelect"
        />

        <!-- Main Content -->
        <main class="flex-1 overflow-hidden relative" :class="isDarkMode ? 'bg-slate-800' : 'bg-[#e2e8f0]'">
          <!-- Password Required -->
          <div v-if="requiresPassword" class="absolute inset-0 flex items-center justify-center p-8">
            <div class="w-full max-w-md">
              <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl p-8">
                <!-- Lock Icon -->
                <div class="flex justify-center mb-6">
                  <div class="w-20 h-20 rounded-2xl bg-amber-500/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-5xl text-amber-500">lock</span>
                  </div>
                </div>

                <!-- Title -->
                <h2 class="text-xl font-bold text-center text-slate-900 dark:text-white mb-2">
                  Password Protected
                </h2>
                <p class="text-sm text-center text-slate-500 dark:text-slate-400 mb-6">
                  This document requires a password to view
                </p>

                <!-- Hint -->
                <div v-if="passwordHint" class="mb-4 p-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl">
                  <div class="flex items-start gap-2">
                    <span class="material-symbols-outlined text-blue-500 text-lg mt-0.5">lightbulb</span>
                    <div>
                      <p class="text-xs font-medium text-blue-700 dark:text-blue-300 uppercase tracking-wide">Hint</p>
                      <p class="text-sm text-blue-600 dark:text-blue-400">{{ passwordHint }}</p>
                    </div>
                  </div>
                </div>

                <!-- Password Input -->
                <div class="mb-4">
                  <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1.5">
                    Enter Password
                  </label>
                  <div class="relative">
                    <input
                      v-model="enteredPassword"
                      :type="showPassword ? 'text' : 'password'"
                      class="w-full px-4 py-3 pr-12 border border-slate-200 dark:border-slate-700 rounded-xl bg-white dark:bg-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
                      placeholder="Enter document password..."
                      @keyup.enter="validateAndLoadPreview"
                      autofocus
                    />
                    <button
                      type="button"
                      @click="showPassword = !showPassword"
                      class="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 transition-colors"
                    >
                      <span class="material-symbols-outlined text-xl">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
                    </button>
                  </div>
                </div>

                <!-- Error Message -->
                <div v-if="passwordError" class="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl">
                  <div class="flex items-center gap-2">
                    <span class="material-symbols-outlined text-red-500 text-lg">error</span>
                    <p class="text-sm text-red-600 dark:text-red-400">{{ passwordError }}</p>
                  </div>
                </div>

                <!-- Actions -->
                <div class="flex gap-3">
                  <button
                    @click="isOpen = false"
                    class="flex-1 py-3 text-slate-600 dark:text-slate-400 font-medium rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    @click="validateAndLoadPreview"
                    :disabled="!enteredPassword || isValidatingPassword"
                    class="flex-1 py-3 bg-teal text-white font-medium rounded-xl hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                  >
                    <svg v-if="isValidatingPassword" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                    </svg>
                    {{ isValidatingPassword ? 'Verifying...' : 'Unlock' }}
                  </button>
                </div>
              </div>
            </div>
          </div>

          <!-- Loading - Document Skeleton -->
          <div v-if="isLoading" class="absolute inset-0 flex items-center justify-center p-8">
            <div class="w-full max-w-lg">
              <!-- Document Skeleton Card -->
              <div class="bg-white rounded-lg shadow-xl p-8 space-y-6 animate-pulse">
                <!-- Title placeholder -->
                <div class="flex justify-center">
                  <div class="h-4 bg-slate-200 rounded-full w-3/5"></div>
                </div>

                <!-- Text lines -->
                <div class="space-y-3">
                  <div class="h-3 bg-slate-200 rounded-full w-full"></div>
                  <div class="h-3 bg-slate-200 rounded-full w-11/12"></div>
                  <div class="h-3 bg-slate-200 rounded-full w-4/5"></div>
                </div>

                <!-- Image placeholder -->
                <div class="flex justify-center py-4">
                  <div class="w-full h-32 bg-slate-100 rounded-lg flex items-center justify-center border-2 border-dashed border-slate-200">
                    <span class="material-symbols-outlined text-4xl text-slate-300">image</span>
                  </div>
                </div>

                <!-- More text lines -->
                <div class="space-y-3">
                  <div class="h-3 bg-slate-200 rounded-full w-full"></div>
                  <div class="h-3 bg-slate-200 rounded-full w-3/4"></div>
                </div>

                <!-- Bottom spacer -->
                <div class="pt-8"></div>
              </div>

              <!-- Loading text -->
              <p class="text-center mt-6 text-sm" :class="isDarkMode ? 'text-slate-400' : 'text-slate-500'">
                Loading document...
              </p>
            </div>
          </div>

          <!-- Error -->
          <div v-else-if="error" class="absolute inset-0 flex items-center justify-center">
            <div class="text-center max-w-sm px-4">
              <div
                class="w-16 h-16 mx-auto mb-4 rounded-2xl flex items-center justify-center"
                :class="isDarkMode ? 'bg-red-900/30' : 'bg-red-100'"
              >
                <span class="material-symbols-outlined text-3xl text-red-500">error</span>
              </div>
              <h2 class="text-lg font-semibold mb-2" :class="isDarkMode ? 'text-white' : 'text-slate-900'">
                Unable to preview
              </h2>
              <p class="text-sm mb-6" :class="isDarkMode ? 'text-slate-400' : 'text-slate-500'">
                {{ error }}
              </p>
              <button
                @click="handleDownload"
                class="px-5 py-2.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors inline-flex items-center gap-2"
              >
                <span class="material-symbols-outlined text-lg">download</span>
                Download file
              </button>
            </div>
          </div>

          <!-- Content -->
          <div v-else-if="previewInfo" class="h-full overflow-hidden flex flex-row">
            <!-- Document Content Area (flex-1 to share space with metadata panel) -->
            <div class="flex-1 min-w-0 h-full overflow-hidden">
            <!-- PDF Preview (Virtualized) -->
            <div
              v-if="previewInfo.type === 'Pdf' && pdfSource"
              ref="pdfScrollContainer"
              class="h-full w-full"
              :class="isDarkMode ? 'bg-slate-800' : 'bg-[#e2e8f0]'"
            >
              <VirtualizedPdfViewer
                ref="pdfViewerRef"
                :source="pdfSource"
                :width="BASE_PDF_WIDTH"
                :zoom="zoom"
                :rotation="rotation"
                :is-dark-mode="isDarkMode"
                :is-annotation-mode="isAnnotationMode"
                :active-tool="activeTool"
                :tool-settings="toolSettings"
                :annotation-data-map="annotationDataMap"
                :annotation-read-only="annotationReadOnly"
                @loaded="handlePdfLoaded"
                @page-change="handlePageChange"
                @error="handlePdfError"
              />
            </div>

            <!-- Image Preview -->
            <div
              v-else-if="previewInfo.type === 'Image' && blobUrl"
              class="h-full flex items-center justify-center p-8 overflow-auto"
              :class="isDarkMode ? 'bg-slate-800' : 'bg-[#e2e8f0]'"
            >
              <img
                :src="blobUrl"
                :alt="document?.name"
                class="max-w-none shadow-2xl rounded-lg transition-transform duration-200"
                :style="{
                  transform: `scale(${zoom / 100}) rotate(${rotation}deg)`,
                }"
              />
            </div>

            <!-- Text Preview -->
            <div
              v-else-if="previewInfo.type === 'Text'"
              class="h-full overflow-auto p-6"
              :class="isDarkMode ? 'bg-slate-800' : 'bg-[#e2e8f0]'"
            >
              <div class="max-w-4xl mx-auto">
                <pre
                  class="p-6 rounded-xl font-mono text-sm whitespace-pre-wrap leading-relaxed shadow-sm"
                  :class="isDarkMode
                    ? 'bg-slate-900 text-slate-300 border border-slate-700'
                    : 'bg-white text-slate-700 border border-slate-200'"
                >{{ textContent }}</pre>
              </div>
            </div>

            <!-- Video Preview -->
            <div
              v-else-if="previewInfo.type === 'Video' && blobUrl"
              class="h-full flex items-center justify-center bg-black"
            >
              <video :src="blobUrl" controls class="max-w-full max-h-full"></video>
            </div>

            <!-- Audio Preview -->
            <div
              v-else-if="previewInfo.type === 'Audio' && blobUrl"
              class="h-full flex items-center justify-center"
              :class="isDarkMode ? 'bg-slate-800' : 'bg-[#e2e8f0]'"
            >
              <div class="text-center">
                <div class="w-28 h-28 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-lg">
                  <span class="material-symbols-outlined text-5xl text-white">music_note</span>
                </div>
                <h3 class="font-medium mb-4" :class="isDarkMode ? 'text-white' : 'text-slate-900'">
                  {{ document?.name }}{{ document?.extension }}
                </h3>
                <audio :src="blobUrl" controls class="w-72"></audio>
              </div>
            </div>

            <!-- Unsupported -->
            <div
              v-else
              class="h-full flex items-center justify-center"
              :class="isDarkMode ? 'bg-slate-800' : 'bg-[#e2e8f0]'"
            >
              <div class="text-center max-w-sm px-4">
                <div
                  class="w-20 h-20 mx-auto mb-4 rounded-2xl flex items-center justify-center"
                  :class="getFileIconBg(previewInfo.type)"
                >
                  <span class="material-symbols-outlined text-4xl" :class="getFileIconColor(previewInfo.type)">
                    {{ getFileIcon(previewInfo.type) }}
                  </span>
                </div>
                <h2 class="text-lg font-semibold mb-2" :class="isDarkMode ? 'text-white' : 'text-slate-900'">
                  Preview not available
                </h2>
                <p class="text-sm mb-6" :class="isDarkMode ? 'text-slate-400' : 'text-slate-500'">
                  This file type cannot be previewed in the browser.
                </p>
                <button
                  @click="handleDownload"
                  class="px-5 py-2.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors inline-flex items-center gap-2"
                >
                  <span class="material-symbols-outlined text-lg">download</span>
                  Download file
                </button>
              </div>
            </div>
            </div><!-- end Document Content Area -->

          <!-- Metadata Panel -->
          <Transition
            enter-active-class="transition-all duration-300 ease-out"
            enter-from-class="w-0 opacity-0"
            enter-to-class="opacity-100"
            leave-active-class="transition-all duration-200 ease-in"
            leave-from-class="opacity-100"
            leave-to-class="w-0 opacity-0"
          >
          <aside
            v-if="showMetadataPanel && hasMetadata && !isLoading && !requiresPassword && !error"
              class="w-80 lg:w-96 shrink-0 overflow-hidden flex flex-col border-l"
              :class="isDarkMode ? 'bg-slate-900 border-slate-700' : 'bg-white border-slate-200'"
            >
              <!-- Panel Header -->
              <div
                class="px-4 py-3 border-b shrink-0"
                :class="isDarkMode ? 'border-slate-700 bg-slate-800' : 'border-slate-200 bg-slate-50'"
              >
                <div class="flex items-center justify-between gap-2">
                  <div class="flex items-center gap-2.5 min-w-0">
                    <div
                      class="w-8 h-8 rounded-lg flex items-center justify-center shrink-0"
                      :style="{
                        backgroundColor: contentTypeInfo?.color ? contentTypeInfo.color + '20' : '#00ae8c20'
                      }"
                    >
                      <span
                        class="material-symbols-outlined text-base"
                        :style="{ color: contentTypeInfo?.color || '#00ae8c' }"
                      >category</span>
                    </div>
                    <div class="min-w-0">
                      <h3
                        class="font-semibold text-sm truncate"
                        :class="isDarkMode ? 'text-white' : 'text-slate-900'"
                      >
                        {{ contentTypeInfo?.name || 'Document Metadata' }}
                      </h3>
                      <p class="text-xs text-slate-500 truncate">
                        {{ metadata.length }} field{{ metadata.length !== 1 ? 's' : '' }}
                      </p>
                    </div>
                  </div>
                  <button
                    @click="toggleMetadataPanel"
                    class="p-1.5 rounded-lg transition-colors shrink-0"
                    :class="isDarkMode ? 'text-slate-400 hover:text-slate-200 hover:bg-slate-700' : 'text-slate-400 hover:text-slate-600 hover:bg-slate-100'"
                  >
                    <span class="material-symbols-outlined text-lg">close</span>
                  </button>
                </div>
              </div>

              <!-- Panel Content -->
              <div class="flex-1 overflow-y-auto p-5">
                <!-- Metadata Groups -->
                <div class="space-y-6">
                  <div
                    v-for="(fields, groupName) in groupedMetadata"
                    :key="groupName"
                    class="space-y-3"
                  >
                    <!-- Group Header -->
                    <div
                      v-if="Object.keys(groupedMetadata).length > 1"
                      class="flex items-center gap-2 pb-2 border-b"
                      :class="isDarkMode ? 'border-slate-700' : 'border-slate-200'"
                    >
                      <span
                        class="material-symbols-outlined text-sm"
                        :class="isDarkMode ? 'text-slate-500' : 'text-slate-400'"
                      >folder</span>
                      <span
                        class="text-xs font-semibold uppercase tracking-wider"
                        :class="isDarkMode ? 'text-slate-400' : 'text-slate-500'"
                      >
                        {{ groupName }}
                      </span>
                    </div>

                    <!-- Fields -->
                    <div class="space-y-3">
                      <div
                        v-for="field in fields"
                        :key="field.fieldName"
                        class="group"
                      >
                        <div
                          class="p-3 rounded-xl transition-colors"
                          :class="isDarkMode
                            ? 'bg-slate-800 hover:bg-slate-750 border border-slate-700'
                            : 'bg-slate-50 hover:bg-slate-100 border border-slate-100'"
                        >
                          <!-- Field Label -->
                          <div class="flex items-center gap-2 mb-1.5">
                            <span
                              class="material-symbols-outlined text-xs"
                              :class="isDarkMode ? 'text-slate-500' : 'text-slate-400'"
                            >{{ getFieldTypeIcon(field.fieldType) }}</span>
                            <span
                              class="text-xs font-medium uppercase tracking-wide"
                              :class="isDarkMode ? 'text-slate-400' : 'text-slate-500'"
                            >
                              {{ field.displayName }}
                            </span>
                          </div>

                          <!-- Field Value -->
                          <div
                            class="text-sm font-medium pl-5"
                            :class="[
                              field.value === '-'
                                ? (isDarkMode ? 'text-slate-600' : 'text-slate-300')
                                : (isDarkMode ? 'text-slate-200' : 'text-slate-700'),
                              field.fieldType === 'Boolean' && field.value === 'Yes' ? 'text-emerald-500' : '',
                              field.fieldType === 'Boolean' && field.value === 'No' ? 'text-slate-400' : ''
                            ]"
                          >
                            <!-- Boolean with icon -->
                            <template v-if="field.fieldType === 'Boolean'">
                              <span class="inline-flex items-center gap-1.5">
                                <span
                                  class="material-symbols-outlined text-base"
                                  :class="field.value === 'Yes' ? 'text-emerald-500' : 'text-slate-400'"
                                  style="font-variation-settings: 'FILL' 1;"
                                >
                                  {{ field.value === 'Yes' ? 'check_circle' : 'cancel' }}
                                </span>
                                {{ field.value }}
                              </span>
                            </template>
                            <!-- Date with icon -->
                            <template v-else-if="field.fieldType === 'Date' || field.fieldType === 'DateTime'">
                              <span class="inline-flex items-center gap-1.5">
                                <span
                                  class="material-symbols-outlined text-base"
                                  :class="isDarkMode ? 'text-slate-500' : 'text-slate-400'"
                                >calendar_today</span>
                                {{ field.value }}
                              </span>
                            </template>
                            <!-- Regular value -->
                            <template v-else>
                              {{ field.value }}
                            </template>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Loading Metadata -->
                <div v-if="isLoadingMetadata" class="flex items-center justify-center py-8">
                  <div class="flex flex-col items-center gap-2">
                    <span class="material-symbols-outlined animate-spin text-2xl text-teal">progress_activity</span>
                    <span class="text-xs text-slate-500">Loading metadata...</span>
                  </div>
                </div>
              </div>

              <!-- Panel Footer -->
              <div
                class="px-5 py-3 border-t shrink-0"
                :class="isDarkMode ? 'border-slate-700 bg-slate-800' : 'border-slate-200 bg-slate-50'"
              >
                <div class="flex items-center justify-between">
                  <span class="text-xs text-slate-500">
                    Content Type: <span class="font-medium" :class="isDarkMode ? 'text-slate-300' : 'text-slate-700'">{{ contentTypeInfo?.name }}</span>
                  </span>
                  <div
                    class="w-2 h-2 rounded-full"
                    :style="{ backgroundColor: contentTypeInfo?.color || '#00ae8c' }"
                  ></div>
                </div>
              </div>
            </aside>
          </Transition>
          </div><!-- end Content flex-row -->
        </main>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
/* PDF Viewer Container */
.pdf-viewer-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-bottom: 32px;
}

/* vue-pdf-embed styling */
.pdf-embed {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

/* Style each PDF page canvas */
:deep(.pdf-embed canvas) {
  background: white;
  border-radius: 4px;
  box-shadow:
    0 1px 3px rgba(0, 0, 0, 0.08),
    0 4px 6px rgba(0, 0, 0, 0.06),
    0 10px 20px rgba(0, 0, 0, 0.05),
    0 20px 40px rgba(0, 0, 0, 0.04);
}

/* Dark mode shadows */
.dark :deep(.pdf-embed canvas) {
  box-shadow:
    0 2px 4px rgba(0, 0, 0, 0.3),
    0 4px 8px rgba(0, 0, 0, 0.3),
    0 8px 16px rgba(0, 0, 0, 0.2),
    0 16px 32px rgba(0, 0, 0, 0.2);
}

/* Text layer styling (for selectable text) */
:deep(.vue-pdf-embed__page) {
  margin-bottom: 16px;
}

:deep(.vue-pdf-embed__page:last-child) {
  margin-bottom: 0;
}

@media print {
  header, footer {
    display: none !important;
  }
}
</style>
