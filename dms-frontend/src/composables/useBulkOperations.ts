import { ref } from 'vue'
import { documentsApi } from '@/api/client'
import type { BulkOperationResult } from '@/types'

export function useBulkOperations() {
  const isProcessing = ref(false)
  const lastResult = ref<BulkOperationResult | null>(null)
  const error = ref<string | null>(null)

  async function bulkDelete(documentIds: string[]): Promise<BulkOperationResult | null> {
    isProcessing.value = true
    error.value = null

    try {
      const response = await documentsApi.bulkDelete(documentIds)
      lastResult.value = response.data
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to delete documents'
      return null
    } finally {
      isProcessing.value = false
    }
  }

  async function bulkMove(documentIds: string[], targetFolderId: string): Promise<BulkOperationResult | null> {
    isProcessing.value = true
    error.value = null

    try {
      const response = await documentsApi.bulkMove(documentIds, targetFolderId)
      lastResult.value = response.data
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to move documents'
      return null
    } finally {
      isProcessing.value = false
    }
  }

  async function bulkDownload(documentIds: string[]): Promise<boolean> {
    isProcessing.value = true
    error.value = null

    try {
      const response = await documentsApi.bulkDownload(documentIds)

      // Create download link
      const blob = new Blob([response.data], { type: 'application/zip' })
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `documents-${new Date().toISOString().slice(0, 10)}.zip`
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)

      return true
    } catch (err: any) {
      error.value = err.response?.data?.message || 'Failed to download documents'
      return false
    } finally {
      isProcessing.value = false
    }
  }

  return {
    isProcessing,
    lastResult,
    error,
    bulkDelete,
    bulkMove,
    bulkDownload
  }
}
