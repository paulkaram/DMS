<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import Modal from '@/components/ui/Modal.vue'
import type { Document, PreviewInfo } from '@/types'
import { documentsApi } from '@/api/client'

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

const previewInfo = ref<PreviewInfo | null>(null)
const isLoading = ref(false)
const error = ref<string | null>(null)
const blobUrl = ref<string | null>(null)

// Image zoom
const imageZoom = ref(100)

// Text content
const textContent = ref<string>('')

watch(() => props.modelValue, async (newVal) => {
  if (newVal && props.document) {
    await loadPreview()
  } else {
    // Cleanup
    if (blobUrl.value) {
      URL.revokeObjectURL(blobUrl.value)
      blobUrl.value = null
    }
    previewInfo.value = null
    textContent.value = ''
    error.value = null
    imageZoom.value = 100
  }
})

async function loadPreview() {
  if (!props.document) return

  isLoading.value = true
  error.value = null

  try {
    // Get preview info
    const infoResponse = await documentsApi.getPreviewInfo(props.document.id)
    previewInfo.value = infoResponse.data

    // For text files, use the content from the API
    if (previewInfo.value.type === 'Text' && previewInfo.value.textContent) {
      textContent.value = previewInfo.value.textContent
    }
    // For PDF and images, download the file
    else if (previewInfo.value.type === 'Pdf' || previewInfo.value.type === 'Image') {
      const response = await documentsApi.download(props.document.id)
      const blob = new Blob([response.data], { type: previewInfo.value.contentType })
      blobUrl.value = URL.createObjectURL(blob)
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to load preview'
  } finally {
    isLoading.value = false
  }
}

async function handleDownload() {
  if (!props.document) return

  try {
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

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

function getPreviewIcon(type: string): string {
  switch (type) {
    case 'Pdf': return 'picture_as_pdf'
    case 'Image': return 'image'
    case 'Text': return 'description'
    case 'Video': return 'movie'
    case 'Audio': return 'music_note'
    case 'Office': return 'article'
    default: return 'insert_drive_file'
  }
}

function zoomIn() {
  imageZoom.value = Math.min(200, imageZoom.value + 25)
}

function zoomOut() {
  imageZoom.value = Math.max(25, imageZoom.value - 25)
}

function resetZoom() {
  imageZoom.value = 100
}
</script>

<template>
  <Modal v-model="isOpen" size="xl">
    <template #header>
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-xl flex items-center justify-center">
          <span class="material-symbols-outlined text-white text-xl">description</span>
        </div>
        <h3 class="text-lg font-semibold text-white flex items-center gap-2">
          {{ document?.name || 'Document Preview' }}
          <!-- Password Protected Badge -->
          <div
            v-if="document?.hasPassword"
            class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-gradient-to-r from-violet-400/20 to-fuchsia-400/20 border border-violet-400/40 backdrop-blur-sm"
            title="Password protected"
          >
            <span
              class="material-symbols-outlined text-violet-300"
              style="font-size: 12px; font-variation-settings: 'FILL' 1;"
            >shield_lock</span>
            <span class="text-[9px] font-semibold text-violet-200 uppercase tracking-wide">Secured</span>
          </div>
        </h3>
      </div>
    </template>
    <div class="min-h-[500px] flex flex-col">
      <!-- Loading -->
      <div v-if="isLoading" class="flex-1 flex items-center justify-center">
        <div class="text-center">
          <svg class="animate-spin w-12 h-12 text-teal mx-auto mb-4" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
          </svg>
          <p class="text-slate-500">Loading preview...</p>
        </div>
      </div>

      <!-- Error -->
      <div v-else-if="error" class="flex-1 flex items-center justify-center">
        <div class="text-center">
          <span class="material-symbols-outlined text-5xl text-red-400 mb-4">error</span>
          <p class="text-red-600 mb-4">{{ error }}</p>
          <button
            @click="handleDownload"
            class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
          >
            Download Instead
          </button>
        </div>
      </div>

      <!-- Preview Content -->
      <div v-else-if="previewInfo" class="flex-1 flex flex-col">
        <!-- PDF Preview -->
        <template v-if="previewInfo.type === 'Pdf' && blobUrl">
          <div class="flex-1 bg-slate-100 rounded-lg overflow-hidden">
            <iframe
              :src="blobUrl"
              class="w-full h-full min-h-[500px]"
              frameborder="0"
            ></iframe>
          </div>
        </template>

        <!-- Image Preview -->
        <template v-else-if="previewInfo.type === 'Image' && blobUrl">
          <!-- Zoom Controls -->
          <div class="flex items-center justify-center gap-2 mb-4">
            <button
              @click="zoomOut"
              :disabled="imageZoom <= 25"
              class="p-2 rounded-lg hover:bg-slate-100 disabled:opacity-50 transition-colors"
            >
              <span class="material-symbols-outlined">remove</span>
            </button>
            <span class="text-sm font-medium w-16 text-center">{{ imageZoom }}%</span>
            <button
              @click="zoomIn"
              :disabled="imageZoom >= 200"
              class="p-2 rounded-lg hover:bg-slate-100 disabled:opacity-50 transition-colors"
            >
              <span class="material-symbols-outlined">add</span>
            </button>
            <button
              @click="resetZoom"
              class="p-2 rounded-lg hover:bg-slate-100 transition-colors ml-2"
              title="Reset zoom"
            >
              <span class="material-symbols-outlined">fit_screen</span>
            </button>
          </div>
          <div class="flex-1 bg-slate-100 rounded-lg overflow-auto flex items-center justify-center p-4">
            <img
              :src="blobUrl"
              :alt="document?.name"
              class="max-w-none transition-transform"
              :style="{ transform: `scale(${imageZoom / 100})` }"
            />
          </div>
        </template>

        <!-- Text Preview -->
        <template v-else-if="previewInfo.type === 'Text'">
          <div class="flex-1 bg-slate-900 rounded-lg overflow-auto">
            <pre class="p-4 text-sm text-slate-300 font-mono whitespace-pre-wrap">{{ textContent }}</pre>
          </div>
        </template>

        <!-- Video Preview -->
        <template v-else-if="previewInfo.type === 'Video' && blobUrl">
          <div class="flex-1 bg-black rounded-lg overflow-hidden flex items-center justify-center">
            <video
              :src="blobUrl"
              controls
              class="max-w-full max-h-full"
            ></video>
          </div>
        </template>

        <!-- Audio Preview -->
        <template v-else-if="previewInfo.type === 'Audio' && blobUrl">
          <div class="flex-1 flex items-center justify-center">
            <div class="text-center">
              <span class="material-symbols-outlined text-6xl text-teal mb-6">music_note</span>
              <audio :src="blobUrl" controls class="w-full max-w-md"></audio>
            </div>
          </div>
        </template>

        <!-- Unsupported / Office -->
        <template v-else>
          <div class="flex-1 flex items-center justify-center">
            <div class="text-center max-w-md">
              <span class="material-symbols-outlined text-6xl text-slate-300 mb-4">
                {{ getPreviewIcon(previewInfo.type) }}
              </span>
              <h3 class="text-lg font-semibold text-slate-700 mb-2">Preview not available</h3>
              <p class="text-slate-500 mb-6">
                {{ previewInfo.type === 'Office'
                  ? 'Office documents cannot be previewed in the browser. Please download to view.'
                  : 'This file type cannot be previewed. Please download to view.' }}
              </p>
              <button
                @click="handleDownload"
                class="px-6 py-3 bg-teal text-white rounded-xl font-medium hover:bg-teal/90 transition-colors inline-flex items-center gap-2"
              >
                <span class="material-symbols-outlined">download</span>
                Download File
              </button>
            </div>
          </div>
        </template>
      </div>
    </div>

    <!-- Footer -->
    <template #footer>
      <div class="flex items-center justify-between w-full">
        <div v-if="previewInfo" class="text-sm text-slate-500">
          {{ previewInfo.fileName }} · {{ formatFileSize(previewInfo.fileSize) }}
        </div>
        <div class="flex items-center gap-3">
          <button
            @click="handleDownload"
            class="px-4 py-2 border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors inline-flex items-center gap-2"
          >
            <span class="material-symbols-outlined text-[18px]">download</span>
            Download
          </button>
          <button
            @click="isOpen = false"
            class="px-4 py-2 bg-slate-100 rounded-lg hover:bg-slate-200 transition-colors"
          >
            Close
          </button>
        </div>
      </div>
    </template>
  </Modal>
</template>
