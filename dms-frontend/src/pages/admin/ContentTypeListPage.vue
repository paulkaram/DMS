<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { contentTypeDefinitionsApi } from '@/api/client'
import type { ContentTypeDefinition } from '@/types'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const router = useRouter()
const contentTypes = ref<ContentTypeDefinition[]>([])
const isLoading = ref(false)
const showDeleteConfirm = ref(false)
const deleteTarget = ref<ContentTypeDefinition | null>(null)

onMounted(() => {
  loadContentTypes()
})

async function loadContentTypes() {
  isLoading.value = true
  try {
    const response = await contentTypeDefinitionsApi.getAll(true)
    contentTypes.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function editContentType(id: string) {
  router.push(`/admin/content-type-builder/${id}`)
}

function createContentType() {
  router.push('/admin/content-type-builder/new')
}

function confirmDelete(ct: ContentTypeDefinition) {
  deleteTarget.value = ct
  showDeleteConfirm.value = true
}

async function handleDelete() {
  if (!deleteTarget.value) return

  try {
    await contentTypeDefinitionsApi.delete(deleteTarget.value.id)
    await loadContentTypes()
  } catch (err) {
  } finally {
    showDeleteConfirm.value = false
    deleteTarget.value = null
  }
}

async function setAsSystemDefault(ct: ContentTypeDefinition, event: Event) {
  event.stopPropagation()
  try {
    if (ct.isSystemDefault) {
      // Clear system default
      await contentTypeDefinitionsApi.clearSystemDefault()
    } else {
      // Set as system default
      await contentTypeDefinitionsApi.setSystemDefault(ct.id)
    }
    await loadContentTypes()
  } catch (err) {
    console.error('Failed to update system default:', err)
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Content Types" icon="category" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Content Types</h1>
          <p class="text-gray-500 mt-1">Define document schemas with custom metadata fields</p>
        </div>
        <button
          @click="createContentType"
          class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Content Type
        </button>
      </div>

      <!-- Info Box -->
      <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
        <div class="flex items-start gap-3">
          <svg class="w-5 h-5 text-blue-600 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <div>
            <h3 class="font-medium text-blue-900">What are Content Types?</h3>
            <p class="text-sm text-blue-700 mt-1">
              Content Types define custom metadata fields for documents. You can assign content types to folders
              (all documents in that folder inherit the fields) or to individual documents.
              Use the form builder to create custom fields like text, numbers, dates, dropdowns, and more.
            </p>
            <p class="text-sm text-blue-700 mt-2">
              <strong>System Default:</strong> Click the star icon to set a content type as the system default.
              This content type will be automatically used when uploading documents to folders that don't have a specific content type assigned.
            </p>
          </div>
        </div>
      </div>

      <!-- Content Types List -->
      <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">
          <svg class="animate-spin h-8 w-8 mx-auto text-blue-600" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <p class="mt-2">Loading content types...</p>
        </div>

        <div v-else-if="contentTypes.length === 0" class="p-12 text-center">
          <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <h3 class="text-lg font-medium text-gray-900 mb-2">No content types defined</h3>
          <p class="text-gray-500 mb-4">Create your first content type to start defining custom metadata fields.</p>
          <button
            @click="createContentType"
            class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90"
          >
            Create Content Type
          </button>
        </div>

        <div v-else class="divide-y divide-gray-100">
          <div
            v-for="ct in contentTypes"
            :key="ct.id"
            class="p-4 hover:bg-gray-50 cursor-pointer"
            @click="editContentType(ct.id)"
          >
            <div class="flex items-center gap-4">
              <div
                class="w-12 h-12 rounded-lg flex items-center justify-center"
                :style="{ backgroundColor: (ct.color || '#3B82F6') + '20' }"
              >
                <svg class="w-6 h-6" :style="{ color: ct.color || '#3B82F6' }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <div class="flex-1">
                <div class="flex items-center gap-2">
                  <h3 class="font-semibold text-gray-900">{{ ct.name }}</h3>
                  <span v-if="ct.isSystemDefault" class="px-2 py-0.5 text-xs bg-gradient-to-r from-teal to-primary text-white rounded-full font-medium">System Default</span>
                  <span v-if="!ct.isActive" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-500 rounded-full">Inactive</span>
                </div>
                <p v-if="ct.description" class="text-sm text-gray-500 mt-0.5">{{ ct.description }}</p>
                <div class="flex items-center gap-3 mt-2 text-xs text-gray-400">
                  <span v-if="ct.allowOnFolders" class="flex items-center gap-1">
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
                    </svg>
                    Folders
                  </span>
                  <span v-if="ct.allowOnDocuments" class="flex items-center gap-1">
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                    Documents
                  </span>
                  <span v-if="ct.isRequired" class="flex items-center gap-1 text-amber-500">
                    <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                    </svg>
                    Required
                  </span>
                </div>
              </div>
              <div class="flex items-center gap-2" @click.stop>
                <button
                  @click="setAsSystemDefault(ct, $event)"
                  class="p-2 rounded-lg transition-colors"
                  :class="ct.isSystemDefault
                    ? 'text-teal bg-teal/10 hover:bg-teal/20'
                    : 'text-gray-400 hover:text-teal hover:bg-teal/10'"
                  :title="ct.isSystemDefault ? 'Remove as system default' : 'Set as system default'"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                  </svg>
                </button>
                <button
                  @click="editContentType(ct.id)"
                  class="p-2 text-gray-400 hover:text-teal hover:bg-teal/10 rounded-lg"
                  title="Edit"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button
                  @click="confirmDelete(ct)"
                  class="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                  title="Delete"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Delete Confirmation Modal -->
    <div v-if="showDeleteConfirm" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-sm mx-4 p-6">
        <div class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 rounded-full mb-4">
          <svg class="w-6 h-6 text-red-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <h3 class="text-lg font-semibold text-gray-900 text-center mb-2">Delete Content Type</h3>
        <p class="text-gray-500 text-center mb-6">
          Are you sure you want to delete "{{ deleteTarget?.name }}"? This will also remove all associated metadata from documents.
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
</template>
