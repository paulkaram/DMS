<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { documentAttachmentsApi } from '@/api/client'

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
  documentId: string
  canEdit?: boolean
}>()

const emit = defineEmits<{
  close: []
}>()

const attachments = ref<Attachment[]>([])
const isLoading = ref(true)
const isUploading = ref(false)
const uploadProgress = ref(0)
const showUploadForm = ref(false)
const description = ref('')
const fileInput = ref<HTMLInputElement | null>(null)

onMounted(async () => {
  await loadAttachments()
})

async function loadAttachments() {
  isLoading.value = true
  try {
    const response = await documentAttachmentsApi.getByDocument(props.documentId)
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
    await documentAttachmentsApi.upload(props.documentId, file, description.value || undefined)
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

async function downloadAttachment(attachment: Attachment) {
  try {
    const response = await documentAttachmentsApi.download(props.documentId, attachment.id)
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

async function deleteAttachment(attachment: Attachment) {
  if (!confirm(`Are you sure you want to delete "${attachment.fileName}"?`)) return

  try {
    await documentAttachmentsApi.delete(props.documentId, attachment.id)
    await loadAttachments()
  } catch (error) {
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
  <div class="flex flex-col h-full bg-white dark:bg-slate-900">
    <!-- Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-800">
      <div>
        <h2 class="text-lg font-semibold text-slate-900 dark:text-slate-100">Attachments</h2>
        <p class="text-sm text-slate-500">{{ attachments.length }} files, {{ formatFileSize(totalSize) }} total</p>
      </div>
      <div class="flex items-center gap-2">
        <button
          v-if="canEdit"
          @click="showUploadForm = !showUploadForm"
          class="flex items-center gap-2 px-3 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors"
        >
          <span class="material-symbols-outlined text-lg">attach_file</span>
          Add
        </button>
        <button
          @click="emit('close')"
          class="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
        >
          <span class="material-symbols-outlined">close</span>
        </button>
      </div>
    </div>

    <!-- Upload Form -->
    <div v-if="showUploadForm" class="p-4 border-b border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50">
      <div class="space-y-3">
        <div>
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">File</label>
          <input
            ref="fileInput"
            type="file"
            @change="uploadFile"
            class="w-full text-sm text-slate-600 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-medium file:bg-teal file:text-white hover:file:bg-teal/90 file:cursor-pointer"
            :disabled="isUploading"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Description (optional)</label>
          <input
            v-model="description"
            type="text"
            class="w-full px-3 py-2 border border-slate-200 dark:border-slate-700 rounded-lg bg-white dark:bg-slate-900 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
            placeholder="Enter description..."
            :disabled="isUploading"
          />
        </div>
        <div v-if="isUploading" class="flex items-center gap-3">
          <div class="flex-1 h-2 bg-slate-200 dark:bg-slate-700 rounded-full overflow-hidden">
            <div
              class="h-full bg-teal transition-all duration-300"
              :style="{ width: `${uploadProgress}%` }"
            ></div>
          </div>
          <span class="text-sm text-slate-500">{{ uploadProgress }}%</span>
        </div>
      </div>
    </div>

    <!-- Attachments List -->
    <div class="flex-1 overflow-y-auto p-6">
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <div class="animate-spin w-8 h-8 border-3 border-teal border-t-transparent rounded-full"></div>
      </div>

      <div v-else-if="attachments.length === 0" class="text-center py-12">
        <span class="material-symbols-outlined text-4xl text-slate-300 dark:text-slate-600 mb-2">attach_file</span>
        <p class="text-slate-500 dark:text-slate-400">No attachments</p>
        <p class="text-sm text-slate-400 dark:text-slate-500">Add files to attach them to this document</p>
      </div>

      <div v-else class="space-y-2">
        <div
          v-for="attachment in attachments"
          :key="attachment.id"
          class="flex items-center gap-4 p-4 bg-slate-50 dark:bg-slate-800 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800/80 transition-colors group"
        >
          <!-- File Icon -->
          <div class="w-10 h-10 rounded-lg bg-teal/10 flex items-center justify-center text-teal flex-shrink-0">
            <span class="material-symbols-outlined">{{ getFileIcon(attachment.contentType) }}</span>
          </div>

          <!-- File Info -->
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-slate-900 dark:text-slate-100 truncate">
              {{ attachment.fileName }}
            </p>
            <div class="flex items-center gap-3 text-xs text-slate-500">
              <span>{{ formatFileSize(attachment.size) }}</span>
              <span>{{ formatDate(attachment.createdAt) }}</span>
              <span v-if="attachment.createdByName">by {{ attachment.createdByName }}</span>
            </div>
            <p v-if="attachment.description" class="text-xs text-slate-400 mt-1 truncate">
              {{ attachment.description }}
            </p>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button
              @click="downloadAttachment(attachment)"
              class="p-2 text-slate-400 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
              title="Download"
            >
              <span class="material-symbols-outlined text-lg">download</span>
            </button>
            <button
              v-if="canEdit"
              @click="deleteAttachment(attachment)"
              class="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
              title="Delete"
            >
              <span class="material-symbols-outlined text-lg">delete</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
