<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { namingConventionsApi } from '@/api/client'
import type { NamingConvention } from '@/types'
import { UiSelect, UiCheckbox } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const conventions = ref<NamingConvention[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<NamingConvention>>({
  name: '',
  description: '',
  pattern: '',
  appliesTo: 'Document',
  folderId: undefined,
  documentTypeId: undefined,
  isRequired: false,
  autoGenerate: false,
  separator: '-',
  sortOrder: 0,
  isActive: true
})

const appliesToOptions = ['Document', 'Folder', 'Both']

onMounted(async () => {
  await loadConventions()
})

async function loadConventions() {
  isLoading.value = true
  try {
    const response = await namingConventionsApi.getAll()
    conventions.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    name: '',
    description: '',
    pattern: '',
    appliesTo: 'Document',
    folderId: undefined,
    documentTypeId: undefined,
    isRequired: false,
    autoGenerate: false,
    separator: '-',
    sortOrder: conventions.value.length,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(conv: NamingConvention) {
  formData.value = { ...conv }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await namingConventionsApi.update(formData.value.id, {
        name: formData.value.name!,
        description: formData.value.description,
        pattern: formData.value.pattern!,
        appliesTo: formData.value.appliesTo!,
        folderId: formData.value.folderId,
        documentTypeId: formData.value.documentTypeId,
        isRequired: formData.value.isRequired,
        autoGenerate: formData.value.autoGenerate,
        separator: formData.value.separator,
        sortOrder: formData.value.sortOrder,
        isActive: formData.value.isActive!
      })
    } else {
      await namingConventionsApi.create({
        name: formData.value.name!,
        description: formData.value.description,
        pattern: formData.value.pattern!,
        appliesTo: formData.value.appliesTo!,
        folderId: formData.value.folderId,
        documentTypeId: formData.value.documentTypeId,
        isRequired: formData.value.isRequired,
        autoGenerate: formData.value.autoGenerate,
        separator: formData.value.separator,
        sortOrder: formData.value.sortOrder
      })
    }
    showModal.value = false
    await loadConventions()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deleteConvention(id: string) {
  if (!confirm('Are you sure you want to delete this naming convention?')) return
  try {
    await namingConventionsApi.delete(id)
    await loadConventions()
  } catch (error) {
  }
}

const appliesToSelectOptions = computed(() =>
  appliesToOptions.map(opt => ({ value: opt, label: opt }))
)
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Naming Conventions" icon="text_format" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Naming Conventions</h1>
          <p class="text-gray-500 mt-1">Configure document naming rules and patterns</p>
        </div>
        <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Convention
        </button>
      </div>

      <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-4 mb-6">
        <h3 class="font-medium text-gray-900 mb-2">Available Variables</h3>
        <div class="flex flex-wrap gap-2">
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{date}</code>
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{year}</code>
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{month}</code>
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{day}</code>
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{seq}</code>
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{type}</code>
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{user}</code>
          <code class="px-2 py-1 bg-gray-100 text-xs rounded">{folder}</code>
        </div>
      </div>

      <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">Loading...</div>
        <div v-else-if="conventions.length === 0" class="p-8 text-center text-gray-500">
          <p class="text-lg font-medium">No naming conventions configured</p>
          <p class="text-sm mt-1">Create a convention to define naming rules</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Pattern</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Applies To</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Options</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="conv in conventions" :key="conv.id" class="border-t border-gray-100 hover:bg-gray-50">
              <td class="py-3 px-4">
                <span class="font-medium text-gray-900">{{ conv.name }}</span>
                <p v-if="conv.description" class="text-xs text-gray-500 mt-0.5">{{ conv.description }}</p>
              </td>
              <td class="py-3 px-4">
                <code class="px-2 py-1 bg-gray-100 text-sm rounded">{{ conv.pattern }}</code>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">
                {{ conv.appliesTo }}
                <span v-if="conv.folderName" class="text-xs"> ({{ conv.folderName }})</span>
              </td>
              <td class="py-3 px-4">
                <div class="flex flex-wrap gap-1">
                  <span v-if="conv.isRequired" class="px-2 py-0.5 text-xs bg-red-100 text-red-700 rounded">Required</span>
                  <span v-if="conv.autoGenerate" class="px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded">Auto</span>
                </div>
              </td>
              <td class="py-3 px-4">
                <span :class="conv.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2 py-1 text-xs rounded-full">
                  {{ conv.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <button @click="openEditModal(conv)" class="p-1 text-gray-400 hover:text-teal mr-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button @click="deleteConvention(conv.id)" class="p-1 text-gray-400 hover:text-red-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-lg mx-4 p-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Convention' : 'New Convention' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input v-model="formData.name" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Pattern</label>
            <input v-model="formData.pattern" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" placeholder="{type}-{date}-{seq}" />
          </div>
          <div class="grid grid-cols-2 gap-4">
            <UiSelect
              v-model="formData.appliesTo"
              :options="appliesToSelectOptions"
              label="Applies To"
              placeholder="Select target"
            />
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Separator</label>
              <input v-model="formData.separator" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" placeholder="-" />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea v-model="formData.description" rows="2" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal"></textarea>
          </div>
          <div class="flex flex-wrap gap-4">
            <UiCheckbox v-model="formData.isRequired" label="Required" />
            <UiCheckbox v-model="formData.autoGenerate" label="Auto-generate" />
            <UiCheckbox v-model="formData.isActive" label="Active" />
          </div>
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
          <button @click="handleSave" :disabled="isSaving" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 disabled:opacity-50">
            {{ isSaving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
