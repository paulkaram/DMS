<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { bookmarksApi } from '@/api/client'
import type { Bookmark } from '@/types'
import { UiButton, UiInput, UiTextArea, UiSelect, UiToggle, UiModal } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const bookmarks = ref<Bookmark[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<Bookmark>>({
  name: '',
  placeholder: '',
  description: '',
  defaultValue: '',
  dataType: 'Text',
  lookupName: '',
  isSystem: false,
  sortOrder: 0,
  isActive: true
})

const dataTypeOptions = [
  { value: 'Text', label: 'Text' },
  { value: 'Date', label: 'Date' },
  { value: 'Number', label: 'Number' },
  { value: 'User', label: 'User' },
  { value: 'Lookup', label: 'Lookup' }
]

const modalTitle = computed(() => isEditing.value ? 'Edit Bookmark' : 'New Bookmark')

onMounted(async () => {
  await loadBookmarks()
})

async function loadBookmarks() {
  isLoading.value = true
  try {
    const response = await bookmarksApi.getAll()
    bookmarks.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    name: '',
    placeholder: '',
    description: '',
    defaultValue: '',
    dataType: 'Text',
    lookupName: '',
    isSystem: false,
    sortOrder: bookmarks.value.length,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(bookmark: Bookmark) {
  formData.value = { ...bookmark }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await bookmarksApi.update(formData.value.id, {
        name: formData.value.name!,
        placeholder: formData.value.placeholder!,
        description: formData.value.description,
        defaultValue: formData.value.defaultValue,
        dataType: formData.value.dataType!,
        lookupName: formData.value.lookupName,
        isSystem: formData.value.isSystem,
        sortOrder: formData.value.sortOrder,
        isActive: formData.value.isActive!
      })
    } else {
      await bookmarksApi.create({
        name: formData.value.name!,
        placeholder: formData.value.placeholder!,
        description: formData.value.description,
        defaultValue: formData.value.defaultValue,
        dataType: formData.value.dataType!,
        lookupName: formData.value.lookupName,
        isSystem: formData.value.isSystem,
        sortOrder: formData.value.sortOrder
      })
    }
    showModal.value = false
    await loadBookmarks()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deleteBookmark(id: string) {
  if (!confirm('Are you sure you want to delete this bookmark?')) return
  try {
    await bookmarksApi.delete(id)
    await loadBookmarks()
  } catch (error) {
  }
}

function getDataTypeColor(type: string) {
  switch (type) {
    case 'Text': return 'bg-blue-100 text-blue-700'
    case 'Date': return 'bg-purple-100 text-purple-700'
    case 'Number': return 'bg-green-100 text-green-700'
    case 'User': return 'bg-orange-100 text-orange-700'
    case 'Lookup': return 'bg-cyan-100 text-cyan-700'
    default: return 'bg-gray-100 text-gray-500'
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Bookmarks" icon="bookmark" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Bookmarks</h1>
          <p class="text-gray-500 mt-1">Manage document bookmarks and replacements</p>
        </div>
        <UiButton @click="openCreateModal">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Bookmark
        </UiButton>
      </div>

      <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">
          <svg class="animate-spin h-8 w-8 mx-auto text-teal" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          <p class="mt-2">Loading bookmarks...</p>
        </div>
        <div v-else-if="bookmarks.length === 0" class="p-8 text-center text-gray-500">
          <svg class="w-12 h-12 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
          </svg>
          <p class="text-lg font-medium">No bookmarks configured</p>
          <p class="text-sm mt-1">Create a bookmark to use in document templates</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Placeholder</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Data Type</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Description</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="bookmark in bookmarks" :key="bookmark.id" class="border-t border-gray-100 hover:bg-gray-50 transition-colors">
              <td class="py-3 px-4 font-medium text-gray-900">
                {{ bookmark.name }}
                <span v-if="bookmark.isSystem" class="ml-2 px-2 py-0.5 text-xs bg-gray-100 text-gray-500 rounded-full">System</span>
              </td>
              <td class="py-3 px-4">
                <code class="px-2 py-1 bg-gray-100 text-sm rounded font-mono">{{ bookmark.placeholder }}</code>
              </td>
              <td class="py-3 px-4">
                <span :class="getDataTypeColor(bookmark.dataType)" class="px-2.5 py-1 text-xs font-medium rounded-full">{{ bookmark.dataType }}</span>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500 max-w-xs truncate">{{ bookmark.description || '-' }}</td>
              <td class="py-3 px-4">
                <span :class="bookmark.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2.5 py-1 text-xs font-medium rounded-full">
                  {{ bookmark.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-1">
                  <UiButton variant="ghost" size="sm" icon-only :disabled="bookmark.isSystem" @click="openEditModal(bookmark)">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </UiButton>
                  <UiButton variant="ghost" size="sm" icon-only :disabled="bookmark.isSystem" @click="deleteBookmark(bookmark.id)">
                    <svg class="w-4 h-4 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </UiButton>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <UiModal v-model="showModal" :title="modalTitle" size="md">
      <div class="space-y-5">
        <UiInput
          v-model="formData.name"
          label="Name"
          placeholder="Enter bookmark name"
        />

        <UiInput
          v-model="formData.placeholder"
          label="Placeholder"
          placeholder="{{FieldName}}"
          hint="Use this placeholder in document templates"
        />

        <UiSelect
          v-model="formData.dataType"
          label="Data Type"
          :options="dataTypeOptions"
        />

        <UiInput
          v-if="formData.dataType === 'Lookup'"
          v-model="formData.lookupName"
          label="Lookup Name"
          placeholder="Enter lookup name"
        />

        <UiInput
          v-model="formData.defaultValue"
          label="Default Value"
          placeholder="Enter default value"
        />

        <UiTextArea
          v-model="formData.description"
          label="Description"
          placeholder="Enter description"
          :rows="2"
        />

        <UiToggle
          v-model="formData.isActive"
          label="Active"
          color="green"
        />
      </div>

      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showModal = false">Cancel</UiButton>
          <UiButton :loading="isSaving" @click="handleSave">
            {{ isSaving ? 'Saving...' : 'Save' }}
          </UiButton>
        </div>
      </template>
    </UiModal>
  </div>
</template>
