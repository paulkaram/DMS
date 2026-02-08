<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { casesApi } from '@/api/client'
import type { Case } from '@/types'
import { UiButton, UiInput, UiTextArea, UiSelect, UiToggle, UiModal, UiDatePicker } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const cases = ref<Case[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<Case>>({
  caseNumber: '',
  title: '',
  description: '',
  status: 'Open',
  priority: 'Normal',
  assignedToUserId: undefined,
  folderId: undefined,
  dueDate: undefined,
  isActive: true
})

const statusOptions = [
  { value: 'Open', label: 'Open' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'OnHold', label: 'On Hold' },
  { value: 'Closed', label: 'Closed' },
  { value: 'Archived', label: 'Archived' }
]

const priorityOptions = [
  { value: 'Low', label: 'Low' },
  { value: 'Normal', label: 'Normal' },
  { value: 'High', label: 'High' },
  { value: 'Critical', label: 'Critical' }
]

const modalTitle = computed(() => isEditing.value ? 'Edit Case' : 'New Case')

onMounted(async () => {
  await loadCases()
})

async function loadCases() {
  isLoading.value = true
  try {
    const response = await casesApi.getAll()
    cases.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    caseNumber: '',
    title: '',
    description: '',
    status: 'Open',
    priority: 'Normal',
    assignedToUserId: undefined,
    folderId: undefined,
    dueDate: undefined,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(caseItem: Case) {
  formData.value = { ...caseItem }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await casesApi.update(formData.value.id, {
        caseNumber: formData.value.caseNumber!,
        title: formData.value.title!,
        description: formData.value.description,
        status: formData.value.status,
        priority: formData.value.priority,
        assignedToUserId: formData.value.assignedToUserId,
        folderId: formData.value.folderId,
        dueDate: formData.value.dueDate,
        closedDate: formData.value.closedDate,
        isActive: formData.value.isActive!
      })
    } else {
      await casesApi.create({
        caseNumber: formData.value.caseNumber!,
        title: formData.value.title!,
        description: formData.value.description,
        status: formData.value.status,
        priority: formData.value.priority,
        assignedToUserId: formData.value.assignedToUserId,
        folderId: formData.value.folderId,
        dueDate: formData.value.dueDate
      })
    }
    showModal.value = false
    await loadCases()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deleteCase(id: string) {
  if (!confirm('Are you sure you want to delete this case?')) return
  try {
    await casesApi.delete(id)
    await loadCases()
  } catch (error) {
  }
}

function getStatusColor(status: string) {
  switch (status) {
    case 'Open': return 'bg-blue-100 text-blue-700'
    case 'InProgress': return 'bg-yellow-100 text-yellow-700'
    case 'OnHold': return 'bg-orange-100 text-orange-700'
    case 'Closed': return 'bg-green-100 text-green-700'
    case 'Archived': return 'bg-gray-100 text-gray-500'
    default: return 'bg-gray-100 text-gray-500'
  }
}

function getPriorityColor(priority?: string) {
  switch (priority) {
    case 'Critical': return 'bg-red-100 text-red-700'
    case 'High': return 'bg-orange-100 text-orange-700'
    case 'Normal': return 'bg-blue-100 text-blue-700'
    case 'Low': return 'bg-gray-100 text-gray-500'
    default: return 'bg-gray-100 text-gray-500'
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Cases" icon="work" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Cases</h1>
          <p class="text-gray-500 mt-1">Manage document cases and workflows</p>
        </div>
        <UiButton @click="openCreateModal">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Case
        </UiButton>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">
          <svg class="animate-spin h-8 w-8 mx-auto text-teal" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          <p class="mt-2">Loading cases...</p>
        </div>
        <div v-else-if="cases.length === 0" class="p-8 text-center text-gray-500">
          <svg class="w-12 h-12 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
          </svg>
          <p class="text-lg font-medium">No cases configured</p>
          <p class="text-sm mt-1">Create a case to organize documents</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Case #</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Title</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Priority</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Due Date</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in cases" :key="c.id" class="border-t border-gray-100 hover:bg-gray-50 transition-colors">
              <td class="py-3 px-4 font-mono text-sm text-gray-900">{{ c.caseNumber }}</td>
              <td class="py-3 px-4">
                <p class="font-medium text-gray-900">{{ c.title }}</p>
                <p v-if="c.description" class="text-xs text-gray-500 truncate max-w-xs">{{ c.description }}</p>
              </td>
              <td class="py-3 px-4">
                <span :class="getStatusColor(c.status)" class="px-2.5 py-1 text-xs font-medium rounded-full">{{ c.status }}</span>
              </td>
              <td class="py-3 px-4">
                <span :class="getPriorityColor(c.priority)" class="px-2.5 py-1 text-xs font-medium rounded-full">{{ c.priority || '-' }}</span>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">
                {{ c.dueDate ? new Date(c.dueDate).toLocaleDateString() : '-' }}
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-1">
                  <UiButton variant="ghost" size="sm" icon-only @click="openEditModal(c)">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </UiButton>
                  <UiButton variant="ghost" size="sm" icon-only @click="deleteCase(c.id)">
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
        <div class="grid grid-cols-2 gap-4">
          <UiInput
            v-model="formData.caseNumber"
            label="Case Number"
            placeholder="CASE-001"
          />
          <UiSelect
            v-model="formData.status"
            label="Status"
            :options="statusOptions"
          />
        </div>

        <UiInput
          v-model="formData.title"
          label="Title"
          placeholder="Enter case title"
        />

        <UiTextArea
          v-model="formData.description"
          label="Description"
          placeholder="Enter description"
          :rows="3"
        />

        <div class="grid grid-cols-2 gap-4">
          <UiSelect
            v-model="formData.priority"
            label="Priority"
            :options="priorityOptions"
          />
          <UiDatePicker
            v-model="formData.dueDate"
            label="Due Date"
            placeholder="Select due date"
          />
        </div>

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
