<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { filingPlansApi, referenceDataApi } from '@/api/client'
import type { FilingPlan, Classification, DocumentType } from '@/types'
import { UiSelect, UiCheckbox } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const filingPlans = ref<FilingPlan[]>([])
const classifications = ref<Classification[]>([])
const documentTypes = ref<DocumentType[]>([])
const isLoading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)

const formData = ref({
  id: '',
  folderId: '',
  name: '',
  description: '',
  pattern: '',
  classificationId: '',
  documentTypeId: '',
  isActive: true
})

onMounted(async () => {
  await loadReferenceData()
})

async function loadReferenceData() {
  try {
    const [classRes, docTypeRes] = await Promise.all([
      referenceDataApi.getClassifications(),
      referenceDataApi.getDocumentTypes()
    ])
    classifications.value = classRes.data
    documentTypes.value = docTypeRes.data
  } catch (err) {
  }
}

function openCreateModal() {
  formData.value = { id: '', folderId: '', name: '', description: '', pattern: '', classificationId: '', documentTypeId: '', isActive: true }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(plan: FilingPlan) {
  formData.value = {
    id: plan.id,
    folderId: plan.folderId,
    name: plan.name,
    description: plan.description || '',
    pattern: plan.pattern || '',
    classificationId: plan.classificationId || '',
    documentTypeId: plan.documentTypeId || '',
    isActive: plan.isActive
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  try {
    if (isEditing.value) {
      await filingPlansApi.update(formData.value.id, {
        folderId: formData.value.folderId,
        name: formData.value.name,
        description: formData.value.description || undefined,
        pattern: formData.value.pattern || undefined,
        classificationId: formData.value.classificationId || undefined,
        documentTypeId: formData.value.documentTypeId || undefined,
        isActive: formData.value.isActive
      })
    } else {
      await filingPlansApi.create({
        folderId: formData.value.folderId,
        name: formData.value.name,
        description: formData.value.description || undefined,
        pattern: formData.value.pattern || undefined,
        classificationId: formData.value.classificationId || undefined,
        documentTypeId: formData.value.documentTypeId || undefined
      })
    }
    showModal.value = false
  } catch (err) {
  }
}

async function deletePlan(id: string) {
  if (!confirm('Are you sure you want to delete this filing plan?')) return
  try {
    await filingPlansApi.delete(id)
    filingPlans.value = filingPlans.value.filter(p => p.id !== id)
  } catch (err) {
  }
}

const classificationOptions = computed(() => [
  { value: '', label: 'None' },
  ...classifications.value.map(c => ({ value: c.id, label: c.name }))
])

const documentTypeOptions = computed(() => [
  { value: '', label: 'None' },
  ...documentTypes.value.map(dt => ({ value: dt.id, label: dt.name }))
])
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Filing Plans" icon="inbox" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Filing Plans</h1>
          <p class="text-gray-500 mt-1">Configure automatic document filing rules</p>
        </div>
        <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Filing Plan
        </button>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">Loading...</div>
        <div v-else-if="filingPlans.length === 0" class="p-8 text-center text-gray-500">
          <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10" />
          </svg>
          <p class="text-lg font-medium">No filing plans configured</p>
          <p class="text-sm mt-1">Filing plans are configured per folder via the context menu</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Folder</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Pattern</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Classification</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="plan in filingPlans" :key="plan.id" class="border-t border-gray-100 hover:bg-gray-50">
              <td class="py-3 px-4">
                <p class="font-medium text-gray-900">{{ plan.name }}</p>
                <p v-if="plan.description" class="text-xs text-gray-500">{{ plan.description }}</p>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ plan.folderName || '-' }}</td>
              <td class="py-3 px-4">
                <code v-if="plan.pattern" class="px-2 py-1 bg-gray-100 text-xs rounded">{{ plan.pattern }}</code>
                <span v-else class="text-gray-400">-</span>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ plan.classificationName || '-' }}</td>
              <td class="py-3 px-4">
                <span :class="plan.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2 py-1 text-xs rounded-full">
                  {{ plan.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <button @click="openEditModal(plan)" class="p-1 text-gray-400 hover:text-teal mr-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button @click="deletePlan(plan.id)" class="p-1 text-gray-400 hover:text-red-600">
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
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4 p-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Filing Plan' : 'New Filing Plan' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
            <input v-model="formData.name" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea v-model="formData.description" rows="2" class="w-full px-4 py-2 border border-gray-300 rounded-lg"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Pattern</label>
            <input v-model="formData.pattern" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg" placeholder="{Year}/{Month}/{Type}" />
          </div>
          <div class="grid grid-cols-2 gap-4">
            <UiSelect
              v-model="formData.classificationId"
              :options="classificationOptions"
              label="Classification"
              placeholder="None"
            />
            <UiSelect
              v-model="formData.documentTypeId"
              :options="documentTypeOptions"
              label="Document Type"
              placeholder="None"
            />
          </div>
          <UiCheckbox v-if="isEditing" v-model="formData.isActive" label="Active" />
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
          <button @click="handleSave" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">Save</button>
        </div>
      </div>
    </div>
  </div>
</template>
