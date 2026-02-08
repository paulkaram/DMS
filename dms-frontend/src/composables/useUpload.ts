import { ref, computed } from 'vue'
import { documentsApi } from '@/api/client'
import type { UploadItem } from '@/types'

export function useUpload() {
  const uploadQueue = ref<UploadItem[]>([])
  const isUploading = ref(false)

  const pendingCount = computed(() =>
    uploadQueue.value.filter(item => item.status === 'pending').length
  )

  const completedCount = computed(() =>
    uploadQueue.value.filter(item => item.status === 'completed').length
  )

  const errorCount = computed(() =>
    uploadQueue.value.filter(item => item.status === 'error').length
  )

  const totalProgress = computed(() => {
    if (uploadQueue.value.length === 0) return 0
    const sum = uploadQueue.value.reduce((acc, item) => acc + item.progress, 0)
    return Math.round(sum / uploadQueue.value.length)
  })

  function addFiles(files: File[]) {
    const newItems: UploadItem[] = files.map(file => ({
      id: crypto.randomUUID(),
      file,
      status: 'pending',
      progress: 0
    }))
    uploadQueue.value.push(...newItems)
    return newItems
  }

  function removeFile(id: string) {
    const index = uploadQueue.value.findIndex(item => item.id === id)
    if (index !== -1) {
      uploadQueue.value.splice(index, 1)
    }
  }

  function clearCompleted() {
    uploadQueue.value = uploadQueue.value.filter(item => item.status !== 'completed')
  }

  function clearAll() {
    uploadQueue.value = []
  }

  async function uploadFile(
    item: UploadItem,
    folderId: string,
    metadata: {
      name?: string
      description?: string
      classificationId?: string
      importanceId?: string
      documentTypeId?: string
    } = {}
  ): Promise<boolean> {
    const queueItem = uploadQueue.value.find(i => i.id === item.id)
    if (!queueItem) return false

    queueItem.status = 'uploading'
    queueItem.progress = 0

    try {
      const formData = new FormData()
      formData.append('file', item.file)
      formData.append('folderId', folderId)
      formData.append('name', metadata.name || item.file.name.replace(/\.[^/.]+$/, ''))

      if (metadata.description) formData.append('description', metadata.description)
      if (metadata.classificationId) formData.append('classificationId', metadata.classificationId)
      if (metadata.importanceId) formData.append('importanceId', metadata.importanceId)
      if (metadata.documentTypeId) formData.append('documentTypeId', metadata.documentTypeId)

      const response = await documentsApi.uploadWithProgress(formData, (progress) => {
        queueItem.progress = progress
      })

      queueItem.status = 'completed'
      queueItem.progress = 100
      queueItem.documentId = response.data.id
      return true
    } catch (err: any) {
      queueItem.status = 'error'
      queueItem.error = err.response?.data?.message || 'Upload failed'
      return false
    }
  }

  async function uploadAll(
    folderId: string,
    metadata: {
      description?: string
      classificationId?: string
      importanceId?: string
      documentTypeId?: string
    } = {}
  ): Promise<{ success: number; failed: number }> {
    isUploading.value = true
    let success = 0
    let failed = 0

    const pendingItems = uploadQueue.value.filter(item => item.status === 'pending')

    for (const item of pendingItems) {
      const result = await uploadFile(item, folderId, metadata)
      if (result) {
        success++
      } else {
        failed++
      }
    }

    isUploading.value = false
    return { success, failed }
  }

  async function retryFailed(folderId: string, metadata: any = {}): Promise<void> {
    const failedItems = uploadQueue.value.filter(item => item.status === 'error')
    for (const item of failedItems) {
      item.status = 'pending'
      item.error = undefined
      item.progress = 0
    }
    await uploadAll(folderId, metadata)
  }

  return {
    uploadQueue,
    isUploading,
    pendingCount,
    completedCount,
    errorCount,
    totalProgress,
    addFiles,
    removeFile,
    clearCompleted,
    clearAll,
    uploadFile,
    uploadAll,
    retryFailed
  }
}
