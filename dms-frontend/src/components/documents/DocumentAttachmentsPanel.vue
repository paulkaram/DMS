<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { documentAttachmentsApi } from '@/api/client'
import { UiButton, UiConfirmDialog } from '@/components/ui'
import VuePdfEmbed from 'vue-pdf-embed'
import 'vue-pdf-embed/dist/styles/annotationLayer.css'
import 'vue-pdf-embed/dist/styles/textLayer.css'

interface Attachment {
  id: string
  documentId: string
  fileName: string
  description?: string
  contentType?: string
  size: number
  createdBy: string
  createdByName?: string
  createdAt: string
}

const props = defineProps<{
  modelValue: boolean
  documentId: string
  documentName?: string
  canEdit?: boolean
  password?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

// Data
const attachments = ref<Attachment[]>([])
const isLoading = ref(true)
const isUploading = ref(false)
const uploadProgress = ref(0)
const showUploadForm = ref(false)
const description = ref('')
const fileInput = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)

// Delete confirmation
const showDeleteConfirm = ref(false)
const deleteTarget = ref<Attachment | null>(null)
const isDeleting = ref(false)

// Preview state
const previewAttachment = ref<Attachment | null>(null)
const previewUrl = ref<string | null>(null)
const isLoadingPreview = ref(false)

watch(isOpen, (open) => {
  if (open) {
    document.body.style.overflow = 'hidden'
    loadAttachments()
  } else {
    document.body.style.overflow = ''
    showUploadForm.value = false
    description.value = ''
  }
}, { immediate: true })

function close() {
  isOpen.value = false
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') {
    if (previewAttachment.value) {
      closePreview()
    } else if (isOpen.value && !showDeleteConfirm.value) {
      close()
    }
  }
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
  document.body.style.overflow = ''
  if (previewUrl.value) {
    window.URL.revokeObjectURL(previewUrl.value)
  }
})

async function loadAttachments() {
  isLoading.value = true
  try {
    const response = await documentAttachmentsApi.getByDocument(props.documentId, props.password)
    attachments.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

async function uploadFile(event: Event) {
  const target = event.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  isUploading.value = true
  uploadProgress.value = 0

  try {
    await documentAttachmentsApi.upload(props.documentId, file, description.value || undefined, props.password)
    description.value = ''
    showUploadForm.value = false
    if (fileInput.value) fileInput.value.value = ''
    await loadAttachments()
  } catch (error) {
  } finally {
    isUploading.value = false
    uploadProgress.value = 0
  }
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
  if (!props.canEdit) return
  const file = e.dataTransfer?.files?.[0]
  if (!file) return

  // Trigger upload directly
  isUploading.value = true
  uploadProgress.value = 0
  showUploadForm.value = true

  documentAttachmentsApi.upload(props.documentId, file, undefined, props.password)
    .then(() => loadAttachments())
    .finally(() => {
      isUploading.value = false
      uploadProgress.value = 0
    })
}

async function downloadAttachment(attachment: Attachment) {
  try {
    const response = await documentAttachmentsApi.download(props.documentId, attachment.id, props.password)
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', attachment.fileName)
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (error) {
  }
}

function isPreviewable(contentType?: string): boolean {
  if (!contentType) return false
  return contentType.startsWith('image/') ||
    contentType === 'application/pdf' ||
    contentType.startsWith('video/') ||
    contentType.startsWith('audio/') ||
    contentType.startsWith('text/')
}

function getPreviewType(contentType?: string): 'image' | 'pdf' | 'video' | 'audio' | 'text' | null {
  if (!contentType) return null
  if (contentType.startsWith('image/')) return 'image'
  if (contentType === 'application/pdf') return 'pdf'
  if (contentType.startsWith('video/')) return 'video'
  if (contentType.startsWith('audio/')) return 'audio'
  if (contentType.startsWith('text/')) return 'text'
  return null
}

async function openPreview(attachment: Attachment) {
  previewAttachment.value = attachment
  isLoadingPreview.value = true
  try {
    const response = await documentAttachmentsApi.download(props.documentId, attachment.id, props.password)
    const blob = new Blob([response.data], { type: attachment.contentType || 'application/octet-stream' })
    if (previewUrl.value) {
      window.URL.revokeObjectURL(previewUrl.value)
    }
    previewUrl.value = window.URL.createObjectURL(blob)
  } catch (error) {
    previewAttachment.value = null
  } finally {
    isLoadingPreview.value = false
  }
}

function closePreview() {
  previewAttachment.value = null
  if (previewUrl.value) {
    window.URL.revokeObjectURL(previewUrl.value)
    previewUrl.value = null
  }
}

function confirmDelete(attachment: Attachment) {
  deleteTarget.value = attachment
  showDeleteConfirm.value = true
}

async function handleDeleteConfirmed() {
  if (!deleteTarget.value) return
  isDeleting.value = true
  try {
    await documentAttachmentsApi.delete(props.documentId, deleteTarget.value.id, props.password)
    showDeleteConfirm.value = false
    deleteTarget.value = null
    await loadAttachments()
  } catch (error) {
  } finally {
    isDeleting.value = false
  }
}

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString()
}

function getFileIcon(contentType?: string): string {
  if (!contentType) return 'description'
  if (contentType.startsWith('image/')) return 'image'
  if (contentType.startsWith('video/')) return 'movie'
  if (contentType.startsWith('audio/')) return 'audio_file'
  if (contentType.includes('pdf')) return 'picture_as_pdf'
  if (contentType.includes('spreadsheet') || contentType.includes('excel')) return 'table_chart'
  if (contentType.includes('presentation') || contentType.includes('powerpoint')) return 'slideshow'
  if (contentType.includes('document') || contentType.includes('word')) return 'article'
  if (contentType.includes('zip') || contentType.includes('archive')) return 'folder_zip'
  return 'description'
}

const totalSize = computed(() => {
  return attachments.value.reduce((sum, a) => sum + a.size, 0)
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isOpen"
        class="fixed inset-0 z-50 overflow-y-auto"
      >
        <!-- Overlay -->
        <div
          class="fixed inset-0 bg-black/50 backdrop-blur-sm"
          @click="close"
        />

        <!-- Modal Container -->
        <div class="flex min-h-full items-center justify-center p-4">
          <Transition
            enter-active-class="duration-200 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="duration-150 ease-in"
            leave-from-class="opacity-100 scale-100 translate-y-0"
            leave-to-class="opacity-0 scale-95 translate-y-4"
          >
            <div
              v-if="isOpen"
              class="relative w-full max-w-2xl bg-white dark:bg-background-dark rounded-2xl shadow-2xl ring-1 ring-black/5 dark:ring-white/10 overflow-hidden"
              @click.stop
              @dragover="handleDragOver"
              @dragleave="handleDragLeave"
              @drop="handleDrop"
            >
              <!-- Header with brand gradient -->
              <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary px-6 py-5 overflow-hidden rounded-t-2xl">
                <!-- Decorative elements -->
                <div class="absolute top-0 right-0 w-32 h-32 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-0 w-20 h-20 bg-primary/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>

                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-xl flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-xl">attach_file</span>
                    </div>
                    <div>
                      <h3 class="text-lg font-semibold text-white">Attachments</h3>
                      <p class="text-sm text-white/60">{{ documentName || 'Document' }}</p>
                    </div>
                  </div>
                  <button
                    type="button"
                    class="w-9 h-9 flex items-center justify-center rounded-xl bg-white/10 hover:bg-white/20 transition-colors"
                    @click="close"
                  >
                    <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Drag overlay -->
              <div
                v-if="isDragging && canEdit"
                class="absolute inset-0 z-10 bg-teal/10 border-2 border-dashed border-teal rounded-2xl flex items-center justify-center"
              >
                <div class="text-center">
                  <span class="material-symbols-outlined text-4xl text-teal mb-2">upload_file</span>
                  <p class="text-sm font-medium text-teal">Drop file to attach</p>
                </div>
              </div>

              <!-- Toolbar -->
              <div class="flex items-center justify-between px-6 py-3 border-b border-zinc-200 dark:border-border-dark bg-zinc-50 dark:bg-surface-dark/30">
                <p class="text-sm text-zinc-500 dark:text-zinc-400">
                  {{ attachments.length }} file{{ attachments.length !== 1 ? 's' : '' }}<span v-if="attachments.length > 0">, {{ formatFileSize(totalSize) }} total</span>
                </p>
                <UiButton
                  v-if="canEdit"
                  size="sm"
                  @click="showUploadForm = !showUploadForm"
                >
                  <span class="flex items-center gap-1.5">
                    <span class="material-symbols-outlined text-lg">add</span>
                    Add Attachment
                  </span>
                </UiButton>
              </div>

              <!-- Upload Form -->
              <Transition
                enter-active-class="duration-200 ease-out"
                enter-from-class="opacity-0 -translate-y-2"
                enter-to-class="opacity-100 translate-y-0"
                leave-active-class="duration-150 ease-in"
                leave-from-class="opacity-100 translate-y-0"
                leave-to-class="opacity-0 -translate-y-2"
              >
                <div v-if="showUploadForm" class="px-6 py-4 border-b border-zinc-200 dark:border-border-dark bg-zinc-50/50 dark:bg-surface-dark/20">
                  <div class="space-y-3">
                    <div>
                      <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">File</label>
                      <input
                        ref="fileInput"
                        type="file"
                        @change="uploadFile"
                        class="w-full text-sm text-zinc-600 dark:text-zinc-400
                               file:mr-4 file:py-2 file:px-4 file:rounded-xl file:border-0
                               file:text-sm file:font-medium file:bg-teal file:text-white
                               hover:file:bg-teal/90 file:cursor-pointer file:transition-colors"
                        :disabled="isUploading"
                      />
                    </div>
                    <div>
                      <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Description (optional)</label>
                      <input
                        v-model="description"
                        type="text"
                        class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-xl bg-white dark:bg-surface-dark text-sm text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-teal/50 focus:border-teal transition-colors"
                        placeholder="Enter description..."
                        :disabled="isUploading"
                      />
                    </div>
                    <div v-if="isUploading" class="flex items-center gap-3">
                      <div class="flex-1 h-2 bg-zinc-200 dark:bg-border-dark rounded-full overflow-hidden">
                        <div
                          class="h-full bg-teal rounded-full transition-all duration-300"
                          :style="{ width: `${uploadProgress}%` }"
                        ></div>
                      </div>
                      <span class="text-sm text-zinc-500 tabular-nums">{{ uploadProgress }}%</span>
                    </div>
                    <div class="flex justify-end">
                      <UiButton variant="outline" size="sm" @click="showUploadForm = false; description = ''">
                        Cancel
                      </UiButton>
                    </div>
                  </div>
                </div>
              </Transition>

              <!-- Body -->
              <div class="px-6 py-5 max-h-[50vh] overflow-y-auto">
                <!-- Loading -->
                <div v-if="isLoading" class="flex items-center justify-center py-12">
                  <svg class="animate-spin w-8 h-8 text-teal" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                  </svg>
                </div>

                <!-- Empty State -->
                <div v-else-if="attachments.length === 0" class="text-center py-12">
                  <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-zinc-100 dark:bg-surface-dark flex items-center justify-center">
                    <span class="material-symbols-outlined text-3xl text-zinc-400 dark:text-zinc-500">attach_file</span>
                  </div>
                  <p class="text-sm font-medium text-zinc-700 dark:text-zinc-300">No attachments yet</p>
                  <p class="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
                    {{ canEdit ? 'Click "Add Attachment" or drag & drop a file' : 'No files have been attached to this document' }}
                  </p>
                </div>

                <!-- Attachments List -->
                <div v-else class="space-y-2">
                  <div
                    v-for="attachment in attachments"
                    :key="attachment.id"
                    class="flex items-center gap-4 p-4 bg-zinc-50 dark:bg-surface-dark rounded-xl hover:bg-zinc-100 dark:hover:bg-surface-dark/80 transition-colors group"
                  >
                    <!-- File Icon -->
                    <div class="w-10 h-10 rounded-xl bg-teal/10 flex items-center justify-center text-teal flex-shrink-0">
                      <span class="material-symbols-outlined">{{ getFileIcon(attachment.contentType) }}</span>
                    </div>

                    <!-- File Info -->
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100 truncate">
                        {{ attachment.fileName }}
                      </p>
                      <div class="flex items-center gap-3 text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">
                        <span>{{ formatFileSize(attachment.size) }}</span>
                        <span>{{ formatDate(attachment.createdAt) }}</span>
                        <span v-if="attachment.createdByName">by {{ attachment.createdByName }}</span>
                      </div>
                      <p v-if="attachment.description" class="text-xs text-zinc-400 dark:text-zinc-500 mt-1 truncate">
                        {{ attachment.description }}
                      </p>
                    </div>

                    <!-- Actions -->
                    <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                      <button
                        v-if="isPreviewable(attachment.contentType)"
                        @click="openPreview(attachment)"
                        class="p-2 text-zinc-400 hover:text-teal hover:bg-teal/10 rounded-xl transition-colors"
                        title="Preview"
                      >
                        <span class="material-symbols-outlined text-lg">visibility</span>
                      </button>
                      <button
                        @click="downloadAttachment(attachment)"
                        class="p-2 text-zinc-400 hover:text-teal hover:bg-teal/10 rounded-xl transition-colors"
                        title="Download"
                      >
                        <span class="material-symbols-outlined text-lg">download</span>
                      </button>
                      <button
                        v-if="canEdit"
                        @click="confirmDelete(attachment)"
                        class="p-2 text-zinc-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-colors"
                        title="Delete"
                      >
                        <span class="material-symbols-outlined text-lg">delete</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Footer -->
              <div class="px-6 py-4 bg-gray-50 dark:bg-surface-dark/50 border-t border-gray-200 dark:border-gray-700/50 rounded-b-2xl">
                <div class="flex justify-end">
                  <UiButton variant="outline" @click="close">Close</UiButton>
                </div>
              </div>
            </div>
          </Transition>
        </div>
      </div>
    </Transition>
  </Teleport>

  <!-- Delete Confirmation Dialog -->
  <UiConfirmDialog
    v-model="showDeleteConfirm"
    type="danger"
    title="Delete Attachment"
    :message="`Are you sure you want to delete &quot;${deleteTarget?.fileName || ''}&quot;? This action cannot be undone.`"
    confirm-text="Delete"
    :loading="isDeleting"
    @confirm="handleDeleteConfirmed"
    @cancel="deleteTarget = null"
  />

  <!-- Attachment Preview Overlay -->
  <Teleport to="body">
    <Transition
      enter-active-class="duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="previewAttachment" class="fixed inset-0 z-[60] flex flex-col">
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/80 backdrop-blur-sm" @click="closePreview" />

        <!-- Preview Header -->
        <div class="relative z-10 flex items-center justify-between px-6 py-3 bg-black/60">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-8 h-8 rounded-lg bg-teal/20 flex items-center justify-center flex-shrink-0">
              <span class="material-symbols-outlined text-teal text-lg">{{ getFileIcon(previewAttachment.contentType) }}</span>
            </div>
            <div class="min-w-0">
              <p class="text-sm font-medium text-white truncate">{{ previewAttachment.fileName }}</p>
              <p class="text-xs text-white/50">{{ formatFileSize(previewAttachment.size) }}</p>
            </div>
          </div>
          <div class="flex items-center gap-2">
            <button
              @click="downloadAttachment(previewAttachment)"
              class="p-2 text-white/60 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
              title="Download"
            >
              <span class="material-symbols-outlined">download</span>
            </button>
            <button
              @click="closePreview"
              class="p-2 text-white/60 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
              title="Close preview"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
        </div>

        <!-- Preview Content -->
        <div class="relative z-10 flex-1 flex items-center justify-center p-6 overflow-auto">
          <!-- Loading -->
          <div v-if="isLoadingPreview" class="text-center">
            <svg class="animate-spin w-10 h-10 text-teal mx-auto mb-3" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
            </svg>
            <p class="text-sm text-white/60">Loading preview...</p>
          </div>

          <!-- Image Preview -->
          <img
            v-else-if="previewUrl && getPreviewType(previewAttachment.contentType) === 'image'"
            :src="previewUrl"
            :alt="previewAttachment.fileName"
            class="max-w-full max-h-full object-contain rounded-lg shadow-2xl"
          />

          <!-- PDF Preview -->
          <div
            v-else-if="previewUrl && getPreviewType(previewAttachment.contentType) === 'pdf'"
            class="w-full max-w-4xl h-full overflow-auto rounded-lg shadow-2xl bg-[#e2e8f0]"
          >
            <div class="flex flex-col items-center gap-4 py-4">
              <VuePdfEmbed
                :source="previewUrl"
                :width="800"
                :annotation-layer="false"
                :text-layer="true"
                class="pdf-preview-embed"
              />
            </div>
          </div>

          <!-- Video Preview -->
          <video
            v-else-if="previewUrl && getPreviewType(previewAttachment.contentType) === 'video'"
            :src="previewUrl"
            controls
            class="max-w-full max-h-full rounded-lg shadow-2xl"
          ></video>

          <!-- Audio Preview -->
          <div
            v-else-if="previewUrl && getPreviewType(previewAttachment.contentType) === 'audio'"
            class="w-full max-w-lg bg-white dark:bg-zinc-800 rounded-2xl p-8 shadow-2xl text-center"
          >
            <div class="w-20 h-20 mx-auto mb-4 rounded-2xl bg-teal/10 flex items-center justify-center">
              <span class="material-symbols-outlined text-4xl text-teal">audio_file</span>
            </div>
            <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100 mb-4">{{ previewAttachment.fileName }}</p>
            <audio :src="previewUrl" controls class="w-full"></audio>
          </div>

          <!-- Text Preview -->
          <iframe
            v-else-if="previewUrl && getPreviewType(previewAttachment.contentType) === 'text'"
            :src="previewUrl"
            class="w-full max-w-4xl h-full rounded-lg shadow-2xl bg-white"
          ></iframe>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.pdf-preview-embed :deep(.vue-pdf-embed__page) {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  margin-bottom: 16px;
}
</style>
