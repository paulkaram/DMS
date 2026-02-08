<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { contentTypesApi } from '@/api/client'
import type { ContentType } from '@/types'
import { UiCheckbox } from '@/components/ui'

const contentTypes = ref<ContentType[]>([])
const isLoading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)

const formData = ref<{
  id?: string
  extension: string
  mimeType: string
  displayName: string
  icon: string
  allowPreview: boolean
  allowThumbnail: boolean
  maxFileSizeMB: number
  isActive: boolean
}>({
  extension: '',
  mimeType: '',
  displayName: '',
  icon: '',
  allowPreview: true,
  allowThumbnail: true,
  maxFileSizeMB: 100,
  isActive: true
})

const showDeleteConfirm = ref(false)
const deleteTarget = ref<ContentType | null>(null)

onMounted(() => {
  loadContentTypes()
})

async function loadContentTypes() {
  isLoading.value = true
  try {
    const response = await contentTypesApi.getAll()
    contentTypes.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    extension: '',
    mimeType: '',
    displayName: '',
    icon: '',
    allowPreview: true,
    allowThumbnail: true,
    maxFileSizeMB: 100,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(ct: ContentType) {
  formData.value = {
    id: ct.id,
    extension: ct.extension,
    mimeType: ct.mimeType,
    displayName: ct.displayName || '',
    icon: ct.icon || '',
    allowPreview: ct.allowPreview,
    allowThumbnail: ct.allowThumbnail,
    maxFileSizeMB: ct.maxFileSizeMB,
    isActive: ct.isActive
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSubmit() {
  if (!formData.value.extension || !formData.value.mimeType) return

  try {
    if (isEditing.value && formData.value.id) {
      await contentTypesApi.update(formData.value.id, {
        extension: formData.value.extension,
        mimeType: formData.value.mimeType,
        displayName: formData.value.displayName || undefined,
        icon: formData.value.icon || undefined,
        allowPreview: formData.value.allowPreview,
        allowThumbnail: formData.value.allowThumbnail,
        maxFileSizeMB: formData.value.maxFileSizeMB,
        isActive: formData.value.isActive
      })
    } else {
      await contentTypesApi.create({
        extension: formData.value.extension,
        mimeType: formData.value.mimeType,
        displayName: formData.value.displayName || undefined,
        icon: formData.value.icon || undefined,
        allowPreview: formData.value.allowPreview,
        allowThumbnail: formData.value.allowThumbnail,
        maxFileSizeMB: formData.value.maxFileSizeMB
      })
    }
    showModal.value = false
    await loadContentTypes()
  } catch (err) {
  }
}

function confirmDelete(ct: ContentType) {
  deleteTarget.value = ct
  showDeleteConfirm.value = true
}

async function handleDelete() {
  if (!deleteTarget.value) return
  try {
    await contentTypesApi.delete(deleteTarget.value.id)
    showDeleteConfirm.value = false
    deleteTarget.value = null
    await loadContentTypes()
  } catch (err) {
  }
}

function getFileIcon(extension: string): string {
  const icons: Record<string, string> = {
    pdf: 'M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z',
    doc: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    docx: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    xls: 'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    xlsx: 'M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
    jpg: 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z',
    png: 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z',
    zip: 'M5 8h14M5 8a2 2 0 110-4h14a2 2 0 110 4M5 8v10a2 2 0 002 2h10a2 2 0 002-2V8m-9 4h4',
    txt: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z'
  }
  return icons[extension.toLowerCase()] || 'M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z'
}
</script>

<template>
  <div class="space-y-6">
    <div class="max-w-6xl mx-auto space-y-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-slate-900 dark:text-slate-100">Content Types</h1>
          <p class="text-slate-500 mt-1">Manage file types and their settings</p>
        </div>
        <button
          @click="openCreateModal"
          class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Add Content Type
        </button>
      </div>

      <!-- Content Types Table -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">
          <svg class="animate-spin h-8 w-8 mx-auto text-blue-600" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p class="mt-2">Loading content types...</p>
        </div>

        <div v-else-if="contentTypes.length === 0" class="p-8 text-center text-gray-500">
          <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
          </svg>
          <p class="text-lg font-medium">No content types configured</p>
          <p class="text-sm mt-1">Add a content type to get started</p>
        </div>

        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Extension</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">MIME Type</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Display Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Max Size</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Features</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="ct in contentTypes"
              :key="ct.id"
              class="border-t border-gray-100 hover:bg-gray-50"
            >
              <td class="py-3 px-4">
                <div class="flex items-center gap-2">
                  <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getFileIcon(ct.extension)" />
                  </svg>
                  <span class="font-medium text-gray-900">.{{ ct.extension }}</span>
                </div>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ ct.mimeType }}</td>
              <td class="py-3 px-4 text-sm text-gray-700">{{ ct.displayName || '-' }}</td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ ct.maxFileSizeMB }} MB</td>
              <td class="py-3 px-4">
                <div class="flex items-center gap-2">
                  <span
                    v-if="ct.allowPreview"
                    class="px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded"
                  >
                    Preview
                  </span>
                  <span
                    v-if="ct.allowThumbnail"
                    class="px-2 py-0.5 text-xs bg-purple-100 text-purple-700 rounded"
                  >
                    Thumbnail
                  </span>
                </div>
              </td>
              <td class="py-3 px-4">
                <span
                  :class="ct.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'"
                  class="px-2 py-1 text-xs rounded-full"
                >
                  {{ ct.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-2">
                  <button
                    @click="openEditModal(ct)"
                    class="p-1 text-gray-400 hover:text-teal"
                    title="Edit"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button
                    @click="confirmDelete(ct)"
                    class="p-1 text-gray-400 hover:text-red-600"
                    title="Delete"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4">
        <div class="p-6">
          <h3 class="text-lg font-semibold text-gray-900 mb-4">
            {{ isEditing ? 'Edit Content Type' : 'Add Content Type' }}
          </h3>

          <div class="space-y-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Extension *</label>
                <input
                  v-model="formData.extension"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
                  placeholder="pdf, docx, jpg..."
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">MIME Type *</label>
                <input
                  v-model="formData.mimeType"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
                  placeholder="application/pdf"
                />
              </div>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Display Name</label>
                <input
                  v-model="formData.displayName"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
                  placeholder="PDF Document"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1">Max File Size (MB)</label>
                <input
                  v-model.number="formData.maxFileSizeMB"
                  type="number"
                  min="1"
                  class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
                />
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Icon (SVG path)</label>
              <input
                v-model="formData.icon"
                type="text"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
                placeholder="Optional SVG path data"
              />
            </div>

            <div class="flex items-center gap-6">
              <UiCheckbox v-model="formData.allowPreview" label="Allow Preview" />
              <UiCheckbox v-model="formData.allowThumbnail" label="Allow Thumbnail" />
              <UiCheckbox v-if="isEditing" v-model="formData.isActive" label="Active" />
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3">
            <button
              @click="showModal = false"
              class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              @click="handleSubmit"
              class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90"
            >
              {{ isEditing ? 'Save Changes' : 'Create' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteConfirm" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-sm mx-4">
        <div class="p-6">
          <div class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 rounded-full mb-4">
            <svg class="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <h3 class="text-lg font-semibold text-gray-900 text-center mb-2">Delete Content Type</h3>
          <p class="text-gray-500 text-center mb-6">
            Are you sure you want to delete the ".{{ deleteTarget?.extension }}" content type?
          </p>
          <div class="flex gap-3">
            <button
              @click="showDeleteConfirm = false; deleteTarget = null"
              class="flex-1 px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50"
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
  </div>
</template>
