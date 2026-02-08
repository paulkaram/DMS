<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { referenceDataApi } from '@/api/client'
import type { Classification, Importance, DocumentType } from '@/types'

const activeTab = ref<'classifications' | 'importances' | 'documentTypes'>('classifications')

const classifications = ref<Classification[]>([])
const importances = ref<Importance[]>([])
const documentTypes = ref<DocumentType[]>([])
const isLoading = ref(true)

// Modal state
const showModal = ref(false)
const modalMode = ref<'create' | 'edit'>('create')
const editingItem = ref<any>(null)
const isSaving = ref(false)
const error = ref('')

// Form data
const formData = ref({
  name: '',
  description: '',
  level: 1,
  color: '#3b82f6',
  language: 'en'
})

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [classResponse, impResponse, docTypeResponse] = await Promise.all([
      referenceDataApi.getClassifications(),
      referenceDataApi.getImportances(),
      referenceDataApi.getDocumentTypes()
    ])
    classifications.value = classResponse.data
    importances.value = impResponse.data
    documentTypes.value = docTypeResponse.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  modalMode.value = 'create'
  editingItem.value = null
  formData.value = {
    name: '',
    description: '',
    level: 1,
    color: '#3b82f6',
    language: 'en'
  }
  showModal.value = true
}

function openEditModal(item: any) {
  modalMode.value = 'edit'
  editingItem.value = item
  formData.value = {
    name: item.name,
    description: item.description || '',
    level: item.level || 1,
    color: item.color || '#3b82f6',
    language: item.language || 'en'
  }
  showModal.value = true
}

async function saveItem() {
  if (!formData.value.name.trim()) {
    error.value = 'Name is required'
    return
  }

  isSaving.value = true
  error.value = ''

  try {
    if (activeTab.value === 'classifications') {
      if (modalMode.value === 'create') {
        await referenceDataApi.createClassification({
          name: formData.value.name,
          description: formData.value.description,
          language: formData.value.language
        })
      } else {
        await referenceDataApi.updateClassification(editingItem.value.id, {
          name: formData.value.name,
          description: formData.value.description,
          language: formData.value.language,
          isActive: true
        })
      }
    } else if (activeTab.value === 'importances') {
      if (modalMode.value === 'create') {
        await referenceDataApi.createImportance({
          name: formData.value.name,
          level: formData.value.level,
          color: formData.value.color,
          language: formData.value.language
        })
      } else {
        await referenceDataApi.updateImportance(editingItem.value.id, {
          name: formData.value.name,
          level: formData.value.level,
          color: formData.value.color,
          language: formData.value.language,
          isActive: true
        })
      }
    } else {
      if (modalMode.value === 'create') {
        await referenceDataApi.createDocumentType({
          name: formData.value.name,
          description: formData.value.description,
          language: formData.value.language
        })
      } else {
        await referenceDataApi.updateDocumentType(editingItem.value.id, {
          name: formData.value.name,
          description: formData.value.description,
          language: formData.value.language,
          isActive: true
        })
      }
    }

    showModal.value = false
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to save'
  } finally {
    isSaving.value = false
  }
}

async function deleteItem(item: any) {
  if (!confirm(`Are you sure you want to delete "${item.name}"?`)) return

  try {
    if (activeTab.value === 'classifications') {
      await referenceDataApi.deleteClassification(item.id)
    } else if (activeTab.value === 'importances') {
      await referenceDataApi.deleteImportance(item.id)
    } else {
      await referenceDataApi.deleteDocumentType(item.id)
    }
    await loadData()
  } catch (err) {
  }
}

function getTabTitle() {
  if (activeTab.value === 'classifications') return 'Classification'
  if (activeTab.value === 'importances') return 'Importance Level'
  return 'Document Type'
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Settings</h1>
        <p class="text-zinc-500 mt-1">Manage reference data and system settings</p>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-2 mb-6 border-b border-gray-200">
      <button
        @click="activeTab = 'classifications'"
        :class="[
          'px-4 py-3 font-medium text-sm border-b-2 transition-colors',
          activeTab === 'classifications'
            ? 'border-teal text-teal'
            : 'border-transparent text-gray-500 hover:text-gray-700'
        ]"
      >
        Classifications
      </button>
      <button
        @click="activeTab = 'importances'"
        :class="[
          'px-4 py-3 font-medium text-sm border-b-2 transition-colors',
          activeTab === 'importances'
            ? 'border-teal text-teal'
            : 'border-transparent text-gray-500 hover:text-gray-700'
        ]"
      >
        Importance Levels
      </button>
      <button
        @click="activeTab = 'documentTypes'"
        :class="[
          'px-4 py-3 font-medium text-sm border-b-2 transition-colors',
          activeTab === 'documentTypes'
            ? 'border-teal text-teal'
            : 'border-transparent text-gray-500 hover:text-gray-700'
        ]"
      >
        Document Types
      </button>
    </div>

    <!-- Add Button -->
    <div class="mb-4">
      <button
        @click="openCreateModal"
        class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Add {{ getTabTitle() }}
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12">
      <div class="flex items-center justify-center gap-3">
        <svg class="animate-spin w-6 h-6 text-blue-600" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
        </svg>
        <span class="text-gray-600">Loading...</span>
      </div>
    </div>

    <!-- Classifications Tab -->
    <div v-else-if="activeTab === 'classifications'" class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <table class="w-full">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200">
          <tr v-for="item in classifications" :key="item.id" class="hover:bg-gray-50">
            <td class="px-6 py-4 whitespace-nowrap font-medium text-gray-900">{{ item.name }}</td>
            <td class="px-6 py-4 text-gray-500">{{ item.description || '-' }}</td>
            <td class="px-6 py-4 text-right space-x-2">
              <button @click="openEditModal(item)" class="text-teal hover:text-teal/80">Edit</button>
              <button @click="deleteItem(item)" class="text-red-600 hover:text-red-800">Delete</button>
            </td>
          </tr>
          <tr v-if="classifications.length === 0">
            <td colspan="3" class="px-6 py-12 text-center text-gray-500">No classifications found</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Importances Tab -->
    <div v-else-if="activeTab === 'importances'" class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <table class="w-full">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Level</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Color</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200">
          <tr v-for="item in importances" :key="item.id" class="hover:bg-gray-50">
            <td class="px-6 py-4 whitespace-nowrap font-medium text-gray-900">{{ item.name }}</td>
            <td class="px-6 py-4 text-gray-500">{{ item.level }}</td>
            <td class="px-6 py-4">
              <div class="flex items-center gap-2">
                <div class="w-6 h-6 rounded" :style="{ backgroundColor: item.color || '#3b82f6' }"></div>
                <span class="text-gray-500 text-sm">{{ item.color || '#3b82f6' }}</span>
              </div>
            </td>
            <td class="px-6 py-4 text-right space-x-2">
              <button @click="openEditModal(item)" class="text-teal hover:text-teal/80">Edit</button>
              <button @click="deleteItem(item)" class="text-red-600 hover:text-red-800">Delete</button>
            </td>
          </tr>
          <tr v-if="importances.length === 0">
            <td colspan="4" class="px-6 py-12 text-center text-gray-500">No importance levels found</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Document Types Tab -->
    <div v-else class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
      <table class="w-full">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200">
          <tr v-for="item in documentTypes" :key="item.id" class="hover:bg-gray-50">
            <td class="px-6 py-4 whitespace-nowrap font-medium text-gray-900">{{ item.name }}</td>
            <td class="px-6 py-4 text-gray-500">{{ item.description || '-' }}</td>
            <td class="px-6 py-4 text-right space-x-2">
              <button @click="openEditModal(item)" class="text-teal hover:text-teal/80">Edit</button>
              <button @click="deleteItem(item)" class="text-red-600 hover:text-red-800">Delete</button>
            </td>
          </tr>
          <tr v-if="documentTypes.length === 0">
            <td colspan="3" class="px-6 py-12 text-center text-gray-500">No document types found</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-6">
            <h3 class="text-lg font-semibold text-gray-900">
              {{ modalMode === 'create' ? 'Create' : 'Edit' }} {{ getTabTitle() }}
            </h3>
            <button @click="showModal = false" class="text-gray-400 hover:text-gray-600">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
              <input
                v-model="formData.name"
                type="text"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-transparent"
                placeholder="Enter name"
              />
            </div>

            <div v-if="activeTab !== 'importances'">
              <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <textarea
                v-model="formData.description"
                rows="3"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-transparent"
                placeholder="Enter description"
              ></textarea>
            </div>

            <div v-if="activeTab === 'importances'">
              <label class="block text-sm font-medium text-gray-700 mb-1">Level *</label>
              <input
                v-model.number="formData.level"
                type="number"
                min="1"
                max="10"
                class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-transparent"
              />
            </div>

            <div v-if="activeTab === 'importances'">
              <label class="block text-sm font-medium text-gray-700 mb-1">Color</label>
              <div class="flex items-center gap-3">
                <input
                  v-model="formData.color"
                  type="color"
                  class="w-12 h-10 border border-gray-300 rounded-lg cursor-pointer"
                />
                <input
                  v-model="formData.color"
                  type="text"
                  class="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-transparent"
                  placeholder="#3b82f6"
                />
              </div>
            </div>

            <div v-if="error" class="bg-red-50 border border-red-200 rounded-lg p-3 text-red-600 text-sm">
              {{ error }}
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3">
            <button
              @click="showModal = false"
              class="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              @click="saveItem"
              :disabled="isSaving"
              class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50"
            >
              {{ isSaving ? 'Saving...' : 'Save' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
