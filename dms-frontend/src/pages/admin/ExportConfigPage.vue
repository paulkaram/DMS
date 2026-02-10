<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { exportConfigsApi } from '@/api/client'
import type { ExportConfig } from '@/types'
import { UiButton, UiInput, UiTextArea, UiSelect, UiCheckbox, UiToggle, UiModal } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const configs = ref<ExportConfig[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<ExportConfig>>({
  name: '',
  description: '',
  exportFormat: 'PDF',
  includeMetadata: true,
  includeVersions: false,
  includeAuditTrail: false,
  flattenFolders: false,
  namingPattern: '',
  watermarkText: '',
  maxFileSizeMB: undefined,
  isDefault: false,
  isActive: true
})

const exportFormatOptions = [
  { value: 'PDF', label: 'PDF' },
  { value: 'ZIP', label: 'ZIP Archive' },
  { value: 'CSV', label: 'CSV' },
  { value: 'XML', label: 'XML' }
]

const modalTitle = computed(() => isEditing.value ? 'Edit Export Config' : 'New Export Config')

onMounted(async () => {
  await loadConfigs()
})

async function loadConfigs() {
  isLoading.value = true
  try {
    const response = await exportConfigsApi.getAll()
    configs.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    name: '',
    description: '',
    exportFormat: 'PDF',
    includeMetadata: true,
    includeVersions: false,
    includeAuditTrail: false,
    flattenFolders: false,
    namingPattern: '',
    watermarkText: '',
    maxFileSizeMB: undefined,
    isDefault: false,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(config: ExportConfig) {
  formData.value = { ...config }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await exportConfigsApi.update(formData.value.id, {
        name: formData.value.name!,
        description: formData.value.description,
        exportFormat: formData.value.exportFormat!,
        includeMetadata: formData.value.includeMetadata,
        includeVersions: formData.value.includeVersions,
        includeAuditTrail: formData.value.includeAuditTrail,
        flattenFolders: formData.value.flattenFolders,
        namingPattern: formData.value.namingPattern,
        watermarkText: formData.value.watermarkText,
        maxFileSizeMB: formData.value.maxFileSizeMB,
        isDefault: formData.value.isDefault,
        isActive: formData.value.isActive!
      })
    } else {
      await exportConfigsApi.create({
        name: formData.value.name!,
        description: formData.value.description,
        exportFormat: formData.value.exportFormat!,
        includeMetadata: formData.value.includeMetadata,
        includeVersions: formData.value.includeVersions,
        includeAuditTrail: formData.value.includeAuditTrail,
        flattenFolders: formData.value.flattenFolders,
        namingPattern: formData.value.namingPattern,
        watermarkText: formData.value.watermarkText,
        maxFileSizeMB: formData.value.maxFileSizeMB,
        isDefault: formData.value.isDefault
      })
    }
    showModal.value = false
    await loadConfigs()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deleteConfig(id: string) {
  if (!confirm('Are you sure you want to delete this export configuration?')) return
  try {
    await exportConfigsApi.delete(id)
    await loadConfigs()
  } catch (error) {
  }
}

async function setDefault(id: string) {
  try {
    await exportConfigsApi.setDefault(id)
    await loadConfigs()
  } catch (error) {
  }
}

function getFormatColor(format: string) {
  switch (format) {
    case 'PDF': return 'bg-red-100 text-red-700'
    case 'ZIP': return 'bg-purple-100 text-purple-700'
    case 'CSV': return 'bg-green-100 text-green-700'
    case 'XML': return 'bg-blue-100 text-blue-700'
    default: return 'bg-gray-100 text-gray-700'
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Export Configuration" icon="export_notes" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Export Configuration</h1>
          <p class="text-gray-500 mt-1">Configure document export settings and profiles</p>
        </div>
        <UiButton @click="openCreateModal">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Config
        </UiButton>
      </div>

      <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">
          <svg class="animate-spin h-8 w-8 mx-auto text-teal" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          <p class="mt-2">Loading configurations...</p>
        </div>
        <div v-else-if="configs.length === 0" class="p-8 text-center text-gray-500">
          <svg class="w-12 h-12 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          <p class="text-lg font-medium">No export configurations</p>
          <p class="text-sm mt-1">Create a configuration to define export settings</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Format</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Options</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Max Size</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="config in configs" :key="config.id" class="border-t border-gray-100 hover:bg-gray-50 transition-colors">
              <td class="py-3 px-4">
                <span class="font-medium text-gray-900">{{ config.name }}</span>
                <span v-if="config.isDefault" class="ml-2 px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded-full font-medium">Default</span>
                <p v-if="config.description" class="text-xs text-gray-500 mt-0.5">{{ config.description }}</p>
              </td>
              <td class="py-3 px-4">
                <span :class="getFormatColor(config.exportFormat)" class="px-2.5 py-1 text-xs font-medium rounded-full">{{ config.exportFormat }}</span>
              </td>
              <td class="py-3 px-4">
                <div class="flex flex-wrap gap-1">
                  <span v-if="config.includeMetadata" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded-full">Metadata</span>
                  <span v-if="config.includeVersions" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded-full">Versions</span>
                  <span v-if="config.includeAuditTrail" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded-full">Audit</span>
                  <span v-if="config.flattenFolders" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded-full">Flatten</span>
                </div>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">
                {{ config.maxFileSizeMB ? `${config.maxFileSizeMB} MB` : 'Unlimited' }}
              </td>
              <td class="py-3 px-4">
                <span :class="config.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2.5 py-1 text-xs font-medium rounded-full">
                  {{ config.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-1">
                  <UiButton v-if="!config.isDefault" variant="ghost" size="sm" icon-only @click="setDefault(config.id)" title="Set as Default">
                    <svg class="w-4 h-4 text-yellow-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z" />
                    </svg>
                  </UiButton>
                  <UiButton variant="ghost" size="sm" icon-only @click="openEditModal(config)">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </UiButton>
                  <UiButton variant="ghost" size="sm" icon-only @click="deleteConfig(config.id)">
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

    <UiModal v-model="showModal" :title="modalTitle" size="lg">
      <div class="space-y-5">
        <UiInput
          v-model="formData.name"
          label="Name"
          placeholder="Enter configuration name"
        />

        <UiTextArea
          v-model="formData.description"
          label="Description"
          placeholder="Enter description"
          :rows="2"
        />

        <div class="grid grid-cols-2 gap-4">
          <UiSelect
            v-model="formData.exportFormat"
            label="Export Format"
            :options="exportFormatOptions"
          />
          <UiInput
            v-model.number="formData.maxFileSizeMB"
            type="number"
            label="Max File Size (MB)"
            placeholder="Unlimited"
          />
        </div>

        <UiInput
          v-model="formData.namingPattern"
          label="Naming Pattern"
          placeholder="{DocumentName}_{Date}"
          hint="Variables: {DocumentName}, {Date}, {User}, {Id}"
        />

        <UiInput
          v-model="formData.watermarkText"
          label="Watermark Text"
          placeholder="Optional watermark"
        />

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-3">Export Options</label>
          <div class="space-y-3">
            <UiCheckbox
              v-model="formData.includeMetadata"
              label="Include metadata"
              description="Export document properties and attributes"
            />
            <UiCheckbox
              v-model="formData.includeVersions"
              label="Include version history"
              description="Export all previous versions of documents"
            />
            <UiCheckbox
              v-model="formData.includeAuditTrail"
              label="Include audit trail"
              description="Export activity history for documents"
            />
            <UiCheckbox
              v-model="formData.flattenFolders"
              label="Flatten folder structure"
              description="Export all files to a single folder"
            />
          </div>
        </div>

        <div class="flex items-center gap-6 pt-2">
          <UiToggle
            v-model="formData.isDefault"
            label="Default"
            color="blue"
          />
          <UiToggle
            v-model="formData.isActive"
            label="Active"
            color="green"
          />
        </div>
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
