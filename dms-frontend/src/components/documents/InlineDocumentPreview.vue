<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import type { Document, PreviewInfo } from '@/types'
import { documentsApi, documentPasswordsApi } from '@/api/client'
import VirtualizedPdfViewer from './VirtualizedPdfViewer.vue'

// Store validated document passwords in session (documentId -> true)
const validatedPasswords = new Map<string, boolean>()

const props = defineProps<{
  document: Document | null
}>()

const emit = defineEmits<{
  'open-full-viewer': []
}>()

// Refs
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

function handlePdfLoaded(data: { numPages: number }) {
  totalPages.value = data.numPages
}

function handlePageChange(page: number) {
  currentPage.value = page
}

function handlePdfError(_err: any) {
  error.value = 'Failed to render PDF. Please try downloading the file instead.'
}

// Zoom controls
function zoomIn() {
  zoom.value = Math.min(300, zoom.value + 25)
}

function zoomOut() {
  zoom.value = Math.max(50, zoom.value - 25)
}

// Rotation (for images)
function rotate() {
  rotation.value = (rotation.value + 90) % 360
}

// Cleanup
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
  requiresPassword.value = false
  passwordHint.value = null
  enteredPassword.value = ''
  passwordError.value = ''
  isValidatingPassword.value = false
  showPassword.value = false
}

// Load preview
async function loadPreview() {
  if (!props.document) return

  isLoading.value = true
  error.value = null

  try {
    const hasPassword = await documentPasswordsApi.hasPassword(props.document.id)

    if (hasPassword.data && !validatedPasswords.has(props.document.id)) {
      isLoading.value = false
      requiresPassword.value = true
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
    const infoResponse = await documentsApi.getPreviewInfo(props.document.id)
    previewInfo.value = infoResponse.data

    if (previewInfo.value.type === 'Text' && previewInfo.value.textContent) {
      textContent.value = previewInfo.value.textContent
    } else if (previewInfo.value.type === 'Pdf') {
      const response = await documentsApi.download(props.document.id)
      const blob = response.data as Blob
      pdfSource.value = URL.createObjectURL(blob)
    } else if (['Image', 'Video', 'Audio'].includes(previewInfo.value.type)) {
      const response = await documentsApi.download(props.document.id)
      const blob = response.data as Blob
      blobUrl.value = URL.createObjectURL(blob)
    } else if (previewInfo.value.type === 'Text' && !previewInfo.value.textContent) {
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

// Download
async function handleDownload() {
  if (!props.document) return
  try {
    const response = await documentsApi.download(props.document.id)
    const blob = new Blob([response.data])
    const url = URL.createObjectURL(blob)
    const a = window.document.createElement('a')
    a.href = url
    a.download = props.document.name + (props.document.extension || '')
    window.document.body.appendChild(a)
    a.click()
    window.document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } catch {
    // silent
  }
}

// Watch document changes
watch(() => props.document?.id, (newId, oldId) => {
  if (newId !== oldId) {
    cleanup()
    if (newId) {
      loadPreview()
    }
  }
}, { immediate: true })

onUnmounted(() => {
  cleanup()
})
</script>

<template>
  <div class="relative w-full h-full flex flex-col">
    <!-- Password Required -->
    <div v-if="requiresPassword" class="absolute inset-0 flex items-center justify-center p-8 bg-zinc-100 z-10">
      <div class="w-full max-w-md">
        <div class="bg-white rounded-lg shadow-2xl p-8">
          <div class="flex justify-center mb-6">
            <div class="w-20 h-20 rounded-lg bg-amber-500/10 flex items-center justify-center">
              <span class="material-symbols-outlined text-5xl text-amber-500">lock</span>
            </div>
          </div>
          <h2 class="text-xl font-bold text-center text-zinc-900 mb-2">Password Protected</h2>
          <p class="text-sm text-center text-zinc-500 mb-6">This document requires a password to view</p>

          <div v-if="passwordHint" class="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
            <div class="flex items-start gap-2">
              <span class="material-symbols-outlined text-blue-500 text-lg mt-0.5">lightbulb</span>
              <div>
                <p class="text-xs font-medium text-blue-700 uppercase tracking-wide">Hint</p>
                <p class="text-sm text-blue-600">{{ passwordHint }}</p>
              </div>
            </div>
          </div>

          <div class="mb-4">
            <label class="block text-sm font-medium text-zinc-700 mb-1.5">Enter Password</label>
            <div class="relative">
              <input
                v-model="enteredPassword"
                :type="showPassword ? 'text' : 'password'"
                class="w-full px-4 py-3 pr-12 border border-zinc-200 rounded-lg bg-white text-zinc-900 focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
                placeholder="Enter document password..."
                @keyup.enter="validateAndLoadPreview"
                autofocus
              />
              <button
                type="button"
                @click="showPassword = !showPassword"
                class="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-zinc-400 hover:text-zinc-600 transition-colors"
              >
                <span class="material-symbols-outlined text-xl">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
              </button>
            </div>
          </div>

          <div v-if="passwordError" class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
            <div class="flex items-center gap-2">
              <span class="material-symbols-outlined text-red-500 text-lg">error</span>
              <p class="text-sm text-red-600">{{ passwordError }}</p>
            </div>
          </div>

          <button
            @click="validateAndLoadPreview"
            :disabled="!enteredPassword || isValidatingPassword"
            class="w-full py-3 bg-teal text-white font-medium rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
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

    <!-- Loading -->
    <div v-if="isLoading" class="absolute inset-0 flex items-center justify-center bg-zinc-100 z-10">
      <div class="flex flex-col items-center gap-3">
        <div class="w-10 h-10 border-3 border-teal border-t-transparent rounded-full animate-spin"></div>
        <span class="text-sm text-zinc-500">Loading preview...</span>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="absolute inset-0 flex items-center justify-center bg-zinc-100">
      <div class="text-center max-w-sm px-4">
        <div class="w-16 h-16 mx-auto mb-4 rounded-lg bg-red-100 flex items-center justify-center">
          <span class="material-symbols-outlined text-3xl text-red-500">error</span>
        </div>
        <h2 class="text-lg font-semibold text-zinc-900 mb-2">Unable to preview</h2>
        <p class="text-sm text-zinc-500 mb-6">{{ error }}</p>
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
    <template v-else-if="previewInfo">
      <!-- PDF Preview -->
      <div
        v-if="previewInfo.type === 'Pdf' && pdfSource"
        class="flex-1 h-full w-full bg-zinc-200"
      >
        <VirtualizedPdfViewer
          ref="pdfViewerRef"
          :source="pdfSource"
          :width="BASE_PDF_WIDTH"
          :zoom="zoom"
          :rotation="rotation"
          :is-dark-mode="false"
          @loaded="handlePdfLoaded"
          @page-change="handlePageChange"
          @error="handlePdfError"
        />
      </div>

      <!-- Image Preview -->
      <div
        v-else-if="previewInfo.type === 'Image' && blobUrl"
        class="flex-1 h-full flex items-center justify-center p-8 overflow-auto bg-zinc-100"
      >
        <img
          :src="blobUrl"
          :alt="document?.name"
          class="max-w-none shadow-2xl rounded-lg transition-transform duration-200"
          :style="{ transform: `scale(${zoom / 100}) rotate(${rotation}deg)` }"
        />
      </div>

      <!-- Text Preview -->
      <div
        v-else-if="previewInfo.type === 'Text'"
        class="flex-1 h-full overflow-auto p-6 bg-zinc-100"
      >
        <div class="max-w-4xl mx-auto">
          <pre class="p-6 rounded-lg font-mono text-sm whitespace-pre-wrap leading-relaxed shadow-sm bg-white text-zinc-700 border border-zinc-200">{{ textContent }}</pre>
        </div>
      </div>

      <!-- Video Preview -->
      <div
        v-else-if="previewInfo.type === 'Video' && blobUrl"
        class="flex-1 h-full flex items-center justify-center bg-black"
      >
        <video :src="blobUrl" controls class="max-w-full max-h-full"></video>
      </div>

      <!-- Audio Preview -->
      <div
        v-else-if="previewInfo.type === 'Audio' && blobUrl"
        class="flex-1 h-full flex items-center justify-center bg-zinc-100"
      >
        <div class="text-center">
          <div class="w-28 h-28 mx-auto mb-6 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center shadow-lg">
            <span class="material-symbols-outlined text-5xl text-white">music_note</span>
          </div>
          <h3 class="font-medium text-zinc-900 mb-4">{{ document?.name }}{{ document?.extension }}</h3>
          <audio :src="blobUrl" controls class="w-72"></audio>
        </div>
      </div>

      <!-- Unsupported -->
      <div v-else class="flex-1 h-full flex items-center justify-center bg-zinc-100">
        <div class="text-center max-w-sm px-4">
          <div class="w-20 h-20 mx-auto mb-4 rounded-lg bg-zinc-200 flex items-center justify-center">
            <span class="material-symbols-outlined text-4xl text-zinc-400">insert_drive_file</span>
          </div>
          <h2 class="text-lg font-semibold text-zinc-900 mb-2">Preview not available</h2>
          <p class="text-sm text-zinc-500 mb-6">This file type cannot be previewed in the browser.</p>
          <button
            @click="handleDownload"
            class="px-5 py-2.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors inline-flex items-center gap-2"
          >
            <span class="material-symbols-outlined text-lg">download</span>
            Download file
          </button>
        </div>
      </div>
    </template>

    <!-- Empty state when no document -->
    <div v-else class="flex-1 h-full flex items-center justify-center bg-zinc-100">
      <div class="text-center">
        <div class="w-20 h-20 mx-auto mb-4 rounded-lg bg-zinc-200 flex items-center justify-center">
          <span class="material-symbols-outlined text-4xl text-zinc-300">description</span>
        </div>
        <p class="text-sm text-zinc-400">No document selected</p>
      </div>
    </div>

    <!-- Floating Mini-Toolbar -->
    <div
      v-if="previewInfo && !isLoading && !requiresPassword && !error"
      class="absolute bottom-4 left-1/2 -translate-x-1/2 z-20 flex items-center gap-1 px-3 py-2 rounded-lg bg-white/80 backdrop-blur-xl border border-zinc-200/80 shadow-lg shadow-black/5"
    >
      <!-- PDF controls: page navigation -->
      <template v-if="previewInfo.type === 'Pdf' && totalPages > 1">
        <button
          @click="prevPage"
          :disabled="currentPage <= 1"
          class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-zinc-600 hover:text-teal hover:bg-teal/10"
        >
          <span class="material-symbols-outlined text-lg">chevron_left</span>
        </button>
        <span class="text-xs font-semibold text-zinc-700 min-w-[4rem] text-center tabular-nums">
          {{ currentPage }} / {{ totalPages }}
        </span>
        <button
          @click="nextPage"
          :disabled="currentPage >= totalPages"
          class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-zinc-600 hover:text-teal hover:bg-teal/10"
        >
          <span class="material-symbols-outlined text-lg">chevron_right</span>
        </button>
        <div class="w-px h-5 bg-zinc-300 mx-1"></div>
      </template>

      <!-- Zoom controls (PDF + Image) -->
      <template v-if="previewInfo.type === 'Pdf' || previewInfo.type === 'Image'">
        <button
          @click="zoomOut"
          :disabled="zoom <= 50"
          class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-zinc-600 hover:text-teal hover:bg-teal/10"
        >
          <span class="material-symbols-outlined text-lg">remove</span>
        </button>
        <span class="text-xs font-bold text-teal min-w-[3rem] text-center tabular-nums">{{ zoom }}%</span>
        <button
          @click="zoomIn"
          :disabled="zoom >= 300"
          class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-zinc-600 hover:text-teal hover:bg-teal/10"
        >
          <span class="material-symbols-outlined text-lg">add</span>
        </button>
      </template>

      <!-- Rotate (Image only) -->
      <template v-if="previewInfo.type === 'Image'">
        <div class="w-px h-5 bg-zinc-300 mx-1"></div>
        <button
          @click="rotate"
          class="p-1.5 rounded-lg transition-colors text-zinc-600 hover:text-teal hover:bg-teal/10"
          title="Rotate"
        >
          <span class="material-symbols-outlined text-lg">rotate_right</span>
        </button>
      </template>

      <!-- Separator before open full viewer -->
      <div v-if="previewInfo.type === 'Pdf' || previewInfo.type === 'Image'" class="w-px h-5 bg-zinc-300 mx-1"></div>

      <!-- Open Full Viewer -->
      <button
        @click="emit('open-full-viewer')"
        class="p-1.5 rounded-lg transition-colors text-zinc-600 hover:text-teal hover:bg-teal/10"
        title="Open full viewer"
      >
        <span class="material-symbols-outlined text-lg">open_in_full</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.border-3 {
  border-width: 3px;
}
</style>
