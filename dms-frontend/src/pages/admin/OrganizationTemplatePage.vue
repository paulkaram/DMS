<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { organizationTemplatesApi } from '@/api/client'
import type { OrganizationTemplate } from '@/types'
import { UiCheckbox } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const templates = ref<OrganizationTemplate[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<OrganizationTemplate>>({
  name: '',
  description: '',
  structure: '[]',
  defaultPermissions: '',
  includeContentTypes: false,
  isDefault: false,
  isActive: true
})

onMounted(async () => {
  await loadTemplates()
})

async function loadTemplates() {
  isLoading.value = true
  try {
    const response = await organizationTemplatesApi.getAll()
    templates.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    name: '',
    description: '',
    structure: '[\n  {"name": "Inbox", "children": []},\n  {"name": "Documents", "children": []},\n  {"name": "Archive", "children": []}\n]',
    defaultPermissions: '',
    includeContentTypes: false,
    isDefault: false,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(template: OrganizationTemplate) {
  formData.value = { ...template }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await organizationTemplatesApi.update(formData.value.id, {
        name: formData.value.name!,
        description: formData.value.description,
        structure: formData.value.structure!,
        defaultPermissions: formData.value.defaultPermissions,
        includeContentTypes: formData.value.includeContentTypes,
        isDefault: formData.value.isDefault,
        isActive: formData.value.isActive!
      })
    } else {
      await organizationTemplatesApi.create({
        name: formData.value.name!,
        description: formData.value.description,
        structure: formData.value.structure!,
        defaultPermissions: formData.value.defaultPermissions,
        includeContentTypes: formData.value.includeContentTypes,
        isDefault: formData.value.isDefault
      })
    }
    showModal.value = false
    await loadTemplates()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deleteTemplate(id: string) {
  if (!confirm('Are you sure you want to delete this template?')) return
  try {
    await organizationTemplatesApi.delete(id)
    await loadTemplates()
  } catch (error) {
  }
}

async function setDefault(id: string) {
  try {
    await organizationTemplatesApi.setDefault(id)
    await loadTemplates()
  } catch (error) {
  }
}

function formatStructure(structure: string): string {
  try {
    const parsed = JSON.parse(structure)
    return formatFolders(parsed, 0)
  } catch {
    return structure
  }
}

function formatFolders(folders: any[], indent: number): string {
  return folders.map(f => {
    const prefix = '  '.repeat(indent) + '/'
    const childStr = f.children?.length ? '\n' + formatFolders(f.children, indent + 1) : ''
    return prefix + f.name + childStr
  }).join('\n')
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Organization Templates" icon="account_tree" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Organization Templates</h1>
          <p class="text-gray-500 mt-1">Define standard folder structures for quick setup</p>
        </div>
        <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Template
        </button>
      </div>

      <div v-if="isLoading" class="p-8 text-center text-gray-500">Loading...</div>
      <div v-else-if="templates.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-200 p-8 text-center text-gray-500">
        <p class="text-lg font-medium">No organization templates</p>
        <p class="text-sm mt-1">Create a template to define standard folder structures</p>
      </div>
      <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div
          v-for="template in templates"
          :key="template.id"
          class="bg-white rounded-xl shadow-sm border border-gray-200 p-6"
        >
          <div class="flex items-start justify-between mb-4">
            <div>
              <h3 class="font-semibold text-gray-900">{{ template.name }}</h3>
              <p v-if="template.description" class="text-sm text-gray-500 mt-1">{{ template.description }}</p>
            </div>
            <div class="flex items-center gap-2">
              <span v-if="template.isDefault" class="px-2 py-1 text-xs bg-blue-100 text-blue-700 rounded-full">Default</span>
              <span :class="template.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2 py-1 text-xs rounded-full">
                {{ template.isActive ? 'Active' : 'Inactive' }}
              </span>
            </div>
          </div>

          <div class="bg-gray-50 rounded-lg p-4 mb-4">
            <p class="text-xs font-medium text-gray-500 mb-2">Folder Structure:</p>
            <pre class="text-sm text-gray-700 whitespace-pre-wrap">{{ formatStructure(template.structure) }}</pre>
          </div>

          <div class="flex items-center gap-2 text-xs text-gray-500 mb-4">
            <span v-if="template.includeContentTypes" class="px-2 py-0.5 bg-gray-100 text-gray-600 rounded">Content Types</span>
            <span v-if="template.defaultPermissions" class="px-2 py-0.5 bg-gray-100 text-gray-600 rounded">Permissions</span>
          </div>

          <div class="flex items-center justify-between">
            <span class="text-xs text-gray-400">Created {{ new Date(template.createdAt).toLocaleDateString() }}</span>
            <div class="flex items-center gap-2">
              <button v-if="!template.isDefault" @click="setDefault(template.id)" class="p-1 text-gray-400 hover:text-yellow-600" title="Set as Default">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                </svg>
              </button>
              <button @click="openEditModal(template)" class="p-1 text-gray-400 hover:text-teal">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button @click="deleteTemplate(template.id)" class="p-1 text-gray-400 hover:text-red-600">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4 p-6 max-h-[90vh] overflow-y-auto">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Template' : 'New Template' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input v-model="formData.name" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <input v-model="formData.description" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Folder Structure (JSON)</label>
            <textarea v-model="formData.structure" rows="8" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal font-mono text-sm"></textarea>
            <p class="text-xs text-gray-500 mt-1">JSON array: [{"name": "Folder", "children": [...]}]</p>
          </div>
          <div class="flex flex-wrap gap-4">
            <UiCheckbox v-model="formData.includeContentTypes" label="Include content types" />
            <UiCheckbox v-model="formData.isDefault" label="Default" />
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
