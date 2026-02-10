<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { purposesApi } from '@/api/client'
import type { Purpose } from '@/types'
import { UiButton, UiInput, UiTextArea, UiSelect, UiCheckbox, UiToggle, UiModal } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const purposes = ref<Purpose[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<Purpose>>({
  name: '',
  description: '',
  purposeType: 'General',
  requiresJustification: false,
  requiresApproval: false,
  isDefault: false,
  sortOrder: 0,
  isActive: true
})

const purposeTypeOptions = [
  { value: 'General', label: 'General' },
  { value: 'Access', label: 'Access' },
  { value: 'Download', label: 'Download' },
  { value: 'Print', label: 'Print' },
  { value: 'Share', label: 'Share' },
  { value: 'Export', label: 'Export' }
]

const modalTitle = computed(() => isEditing.value ? 'Edit Purpose' : 'New Purpose')

onMounted(async () => {
  await loadPurposes()
})

async function loadPurposes() {
  isLoading.value = true
  try {
    const response = await purposesApi.getAll()
    purposes.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    name: '',
    description: '',
    purposeType: 'General',
    requiresJustification: false,
    requiresApproval: false,
    isDefault: false,
    sortOrder: purposes.value.length,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(purpose: Purpose) {
  formData.value = { ...purpose }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await purposesApi.update(formData.value.id, {
        name: formData.value.name!,
        description: formData.value.description,
        purposeType: formData.value.purposeType!,
        requiresJustification: formData.value.requiresJustification,
        requiresApproval: formData.value.requiresApproval,
        isDefault: formData.value.isDefault,
        sortOrder: formData.value.sortOrder,
        isActive: formData.value.isActive!
      })
    } else {
      await purposesApi.create({
        name: formData.value.name!,
        description: formData.value.description,
        purposeType: formData.value.purposeType!,
        requiresJustification: formData.value.requiresJustification,
        requiresApproval: formData.value.requiresApproval,
        isDefault: formData.value.isDefault,
        sortOrder: formData.value.sortOrder
      })
    }
    showModal.value = false
    await loadPurposes()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deletePurpose(id: string) {
  if (!confirm('Are you sure you want to delete this purpose?')) return
  try {
    await purposesApi.delete(id)
    await loadPurposes()
  } catch (error) {
  }
}

function getTypeColor(type: string) {
  switch (type) {
    case 'General': return 'bg-gray-100 text-gray-700'
    case 'Access': return 'bg-blue-100 text-blue-700'
    case 'Download': return 'bg-green-100 text-green-700'
    case 'Print': return 'bg-orange-100 text-orange-700'
    case 'Share': return 'bg-purple-100 text-purple-700'
    case 'Export': return 'bg-cyan-100 text-cyan-700'
    default: return 'bg-gray-100 text-gray-500'
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Purposes" icon="flag" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Purposes</h1>
          <p class="text-gray-500 mt-1">Define document purpose classifications for compliance</p>
        </div>
        <UiButton @click="openCreateModal">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Purpose
        </UiButton>
      </div>

      <!-- Table -->
      <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">
          <svg class="animate-spin h-8 w-8 mx-auto text-teal" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          <p class="mt-2">Loading purposes...</p>
        </div>
        <div v-else-if="purposes.length === 0" class="p-8 text-center text-gray-500">
          <svg class="w-12 h-12 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
          <p class="text-lg font-medium">No purposes configured</p>
          <p class="text-sm mt-1">Create a purpose to track document access reasons</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Type</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Description</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Requirements</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="purpose in purposes" :key="purpose.id" class="border-t border-gray-100 hover:bg-gray-50 transition-colors">
              <td class="py-3 px-4">
                <span class="font-medium text-gray-900">{{ purpose.name }}</span>
                <span v-if="purpose.isDefault" class="ml-2 px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded-full font-medium">Default</span>
              </td>
              <td class="py-3 px-4">
                <span :class="getTypeColor(purpose.purposeType)" class="px-2.5 py-1 text-xs font-medium rounded-full">{{ purpose.purposeType }}</span>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500 max-w-xs truncate">{{ purpose.description || '-' }}</td>
              <td class="py-3 px-4">
                <div class="flex flex-wrap gap-1">
                  <span v-if="purpose.requiresJustification" class="px-2 py-0.5 text-xs bg-yellow-100 text-yellow-700 rounded-full font-medium">Justification</span>
                  <span v-if="purpose.requiresApproval" class="px-2 py-0.5 text-xs bg-red-100 text-red-700 rounded-full font-medium">Approval</span>
                  <span v-if="!purpose.requiresJustification && !purpose.requiresApproval" class="text-xs text-gray-400">None</span>
                </div>
              </td>
              <td class="py-3 px-4">
                <span :class="purpose.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2.5 py-1 text-xs font-medium rounded-full">
                  {{ purpose.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-1">
                  <UiButton variant="ghost" size="sm" icon-only @click="openEditModal(purpose)">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </UiButton>
                  <UiButton variant="ghost" size="sm" icon-only @click="deletePurpose(purpose.id)">
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

    <!-- Modal -->
    <UiModal v-model="showModal" :title="modalTitle" size="md">
      <div class="space-y-5">
        <UiInput
          v-model="formData.name"
          label="Name"
          placeholder="Enter purpose name"
        />

        <UiSelect
          v-model="formData.purposeType"
          label="Type"
          :options="purposeTypeOptions"
          placeholder="Select type"
        />

        <UiTextArea
          v-model="formData.description"
          label="Description"
          placeholder="Enter description"
          :rows="3"
        />

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-3">Requirements</label>
          <div class="space-y-3">
            <UiCheckbox
              v-model="formData.requiresJustification"
              label="Requires justification"
              description="Users must provide a reason when selecting this purpose"
              color="orange"
            />
            <UiCheckbox
              v-model="formData.requiresApproval"
              label="Requires approval"
              description="An administrator must approve the purpose selection"
              color="red"
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
