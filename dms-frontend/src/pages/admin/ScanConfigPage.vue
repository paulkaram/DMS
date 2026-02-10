<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { scanConfigsApi } from '@/api/client'
import type { ScanConfig } from '@/types'
import { UiButton, UiInput, UiTextArea, UiSelect, UiCheckbox, UiToggle, UiModal } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const configs = ref<ScanConfig[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<ScanConfig>>({
  name: '',
  description: '',
  resolution: 300,
  colorMode: 'Color',
  outputFormat: 'PDF',
  enableOCR: true,
  ocrLanguage: 'eng',
  autoDeskew: true,
  autoCrop: true,
  removeBlankPages: false,
  compressionQuality: 85,
  targetFolderId: undefined,
  isDefault: false,
  isActive: true
})

const colorModeOptions = [
  { value: 'Color', label: 'Full Color' },
  { value: 'Grayscale', label: 'Grayscale' },
  { value: 'BlackWhite', label: 'Black & White' }
]

const outputFormatOptions = [
  { value: 'PDF', label: 'PDF' },
  { value: 'TIFF', label: 'TIFF' },
  { value: 'PNG', label: 'PNG' },
  { value: 'JPEG', label: 'JPEG' }
]

const resolutionOptions = [
  { value: 150, label: '150 DPI (Draft)' },
  { value: 200, label: '200 DPI (Standard)' },
  { value: 300, label: '300 DPI (High Quality)' },
  { value: 600, label: '600 DPI (Archival)' }
]

const ocrLanguageOptions = [
  { value: 'eng', label: 'English' },
  { value: 'ara', label: 'Arabic' },
  { value: 'fra', label: 'French' },
  { value: 'deu', label: 'German' },
  { value: 'spa', label: 'Spanish' }
]

const modalTitle = computed(() => isEditing.value ? 'Edit Scan Config' : 'New Scan Config')

onMounted(async () => {
  await loadConfigs()
})

async function loadConfigs() {
  isLoading.value = true
  try {
    const response = await scanConfigsApi.getAll()
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
    resolution: 300,
    colorMode: 'Color',
    outputFormat: 'PDF',
    enableOCR: true,
    ocrLanguage: 'eng',
    autoDeskew: true,
    autoCrop: true,
    removeBlankPages: false,
    compressionQuality: 85,
    targetFolderId: undefined,
    isDefault: false,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(config: ScanConfig) {
  formData.value = { ...config }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await scanConfigsApi.update(formData.value.id, {
        name: formData.value.name!,
        description: formData.value.description,
        resolution: formData.value.resolution,
        colorMode: formData.value.colorMode,
        outputFormat: formData.value.outputFormat,
        enableOCR: formData.value.enableOCR,
        ocrLanguage: formData.value.ocrLanguage,
        autoDeskew: formData.value.autoDeskew,
        autoCrop: formData.value.autoCrop,
        removeBlankPages: formData.value.removeBlankPages,
        compressionQuality: formData.value.compressionQuality,
        targetFolderId: formData.value.targetFolderId,
        isDefault: formData.value.isDefault,
        isActive: formData.value.isActive!
      })
    } else {
      await scanConfigsApi.create({
        name: formData.value.name!,
        description: formData.value.description,
        resolution: formData.value.resolution,
        colorMode: formData.value.colorMode,
        outputFormat: formData.value.outputFormat,
        enableOCR: formData.value.enableOCR,
        ocrLanguage: formData.value.ocrLanguage,
        autoDeskew: formData.value.autoDeskew,
        autoCrop: formData.value.autoCrop,
        removeBlankPages: formData.value.removeBlankPages,
        compressionQuality: formData.value.compressionQuality,
        targetFolderId: formData.value.targetFolderId,
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
  if (!confirm('Are you sure you want to delete this scan configuration?')) return
  try {
    await scanConfigsApi.delete(id)
    await loadConfigs()
  } catch (error) {
  }
}

async function setDefault(id: string) {
  try {
    await scanConfigsApi.setDefault(id)
    await loadConfigs()
  } catch (error) {
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Scan Configuration" icon="scanner" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Scan Configuration</h1>
          <p class="text-gray-500 mt-1">Configure document scanning and OCR settings</p>
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
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
          </svg>
          <p class="text-lg font-medium">No scan configurations</p>
          <p class="text-sm mt-1">Create a configuration to define scan settings</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Resolution</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Format</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Features</th>
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
                <span class="text-sm text-gray-700">{{ config.resolution }} DPI</span>
                <span class="text-xs text-gray-400 ml-1">({{ config.colorMode }})</span>
              </td>
              <td class="py-3 px-4">
                <span class="px-2.5 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-full">{{ config.outputFormat }}</span>
              </td>
              <td class="py-3 px-4">
                <div class="flex flex-wrap gap-1">
                  <span v-if="config.enableOCR" class="px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded-full font-medium">OCR</span>
                  <span v-if="config.autoDeskew" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded-full">Deskew</span>
                  <span v-if="config.autoCrop" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded-full">Crop</span>
                  <span v-if="config.removeBlankPages" class="px-2 py-0.5 text-xs bg-gray-100 text-gray-600 rounded-full">No Blanks</span>
                </div>
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

        <div class="border-t border-gray-200 pt-4">
          <h4 class="text-sm font-semibold text-gray-900 mb-4">Scan Quality</h4>
          <div class="grid grid-cols-2 gap-4">
            <UiSelect
              v-model="formData.resolution"
              label="Resolution"
              :options="resolutionOptions"
            />
            <UiSelect
              v-model="formData.colorMode"
              label="Color Mode"
              :options="colorModeOptions"
            />
            <UiSelect
              v-model="formData.outputFormat"
              label="Output Format"
              :options="outputFormatOptions"
            />
            <UiInput
              v-model.number="formData.compressionQuality"
              type="number"
              label="Compression Quality"
              placeholder="85"
              hint="1-100"
            />
          </div>
        </div>

        <div class="border-t border-gray-200 pt-4">
          <h4 class="text-sm font-semibold text-gray-900 mb-4">OCR Settings</h4>
          <div class="space-y-4">
            <UiToggle
              v-model="formData.enableOCR"
              label="Enable OCR"
              description="Extract text from scanned images"
              color="blue"
            />
            <UiSelect
              v-if="formData.enableOCR"
              v-model="formData.ocrLanguage"
              label="OCR Language"
              :options="ocrLanguageOptions"
            />
          </div>
        </div>

        <div class="border-t border-gray-200 pt-4">
          <h4 class="text-sm font-semibold text-gray-900 mb-4">Image Processing</h4>
          <div class="space-y-3">
            <UiCheckbox
              v-model="formData.autoDeskew"
              label="Auto-deskew"
              description="Straighten tilted scans automatically"
            />
            <UiCheckbox
              v-model="formData.autoCrop"
              label="Auto-crop"
              description="Remove empty borders from scans"
            />
            <UiCheckbox
              v-model="formData.removeBlankPages"
              label="Remove blank pages"
              description="Automatically skip blank pages"
            />
          </div>
        </div>

        <div class="flex items-center gap-6 border-t border-gray-200 pt-4">
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
