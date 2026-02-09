<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import type { Document, Classification, Importance, DocumentType } from '@/types'
import { documentsApi, referenceDataApi } from '@/api/client'
import { UiSelect, UiDatePicker } from '@/components/ui'

const route = useRoute()
const router = useRouter()

const searchQuery = ref('')
const results = ref<Document[]>([])
const isLoading = ref(false)
const showFilters = ref(false)

// Filters
const filters = ref({
  classificationId: '',
  importanceId: '',
  documentTypeId: '',
  dateFrom: '',
  dateTo: ''
})

// Reference data
const classifications = ref<Classification[]>([])
const importances = ref<Importance[]>([])
const documentTypes = ref<DocumentType[]>([])

onMounted(async () => {
  await loadReferenceData()
  if (route.query.q) {
    searchQuery.value = route.query.q as string
    performSearch()
  }
})

watch(() => route.query.q, (newQuery) => {
  if (newQuery) {
    searchQuery.value = newQuery as string
    performSearch()
  }
})

async function loadReferenceData() {
  try {
    const [classRes, impRes, docTypeRes] = await Promise.all([
      referenceDataApi.getClassifications(),
      referenceDataApi.getImportances(),
      referenceDataApi.getDocumentTypes()
    ])
    classifications.value = classRes.data
    importances.value = impRes.data
    documentTypes.value = docTypeRes.data
  } catch (err) {
  }
}

async function performSearch() {
  isLoading.value = true
  try {
    const params: any = {}
    if (searchQuery.value.trim()) {
      params.search = searchQuery.value
    }
    if (filters.value.classificationId) {
      params.classificationId = filters.value.classificationId
    }
    if (filters.value.documentTypeId) {
      params.documentTypeId = filters.value.documentTypeId
    }

    const response = await documentsApi.search(params)
    results.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function handleSearch() {
  router.push({ name: 'search', query: { q: searchQuery.value } })
  performSearch()
}

function clearFilters() {
  filters.value = {
    classificationId: '',
    importanceId: '',
    documentTypeId: '',
    dateFrom: '',
    dateTo: ''
  }
  performSearch()
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

function viewDocument(doc: Document) {
  router.push(`/documents/${doc.id}`)
}

function getClassificationName(id?: string) {
  if (!id) return '-'
  const c = classifications.value.find(x => x.id === id)
  return c?.name || '-'
}

function getImportanceName(id?: string) {
  if (!id) return '-'
  const i = importances.value.find(x => x.id === id)
  return i?.name || '-'
}

function getDocumentTypeName(id?: string) {
  if (!id) return '-'
  const dt = documentTypes.value.find(x => x.id === id)
  return dt?.name || '-'
}

const classificationOptions = computed(() => [
  { value: '', label: 'All' },
  ...classifications.value.map(c => ({ value: c.id, label: c.name }))
])

const importanceOptions = computed(() => [
  { value: '', label: 'All' },
  ...importances.value.map(i => ({ value: i.id, label: i.name }))
])

const documentTypeOptions = computed(() => [
  { value: '', label: 'All' },
  ...documentTypes.value.map(dt => ({ value: dt.id, label: dt.name }))
])
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Search Documents</h1>
      <p class="text-gray-500 dark:text-zinc-400 mt-1">Find documents across all cabinets and folders</p>
    </div>

    <!-- Search Box -->
    <div class="bg-white dark:bg-background-dark rounded-xl shadow-sm border border-gray-200 dark:border-border-dark p-5 mb-6">
      <div class="flex gap-4">
        <div class="flex-1 relative">
          <svg class="absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400 dark:text-zinc-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search documents by name, description..."
            class="w-full pl-12 pr-4 py-3 border border-gray-200 dark:border-border-dark rounded-xl focus:ring-2 focus:ring-teal/30 focus:border-teal bg-white dark:bg-surface-dark text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-zinc-500 transition-all"
            @keyup.enter="handleSearch"
          />
        </div>
        <button
          @click="showFilters = !showFilters"
          :class="[
            'px-4 py-2 border rounded-xl flex items-center gap-2 transition-all font-medium',
            showFilters ? 'border-teal bg-teal/10 text-teal' : 'border-gray-200 dark:border-border-dark text-gray-600 dark:text-zinc-300 hover:bg-gray-50 dark:hover:bg-surface-dark hover:border-teal/50'
          ]"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
          </svg>
          Filters
        </button>
        <button
          @click="handleSearch"
          class="px-6 py-2 bg-teal text-white rounded-xl hover:bg-teal/90 transition-colors font-medium shadow-sm hover:shadow-md"
        >
          Search
        </button>
      </div>

      <!-- Filters Panel -->
      <div v-if="showFilters" class="mt-4 pt-4 border-t border-gray-200 dark:border-border-dark">
        <div class="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-4">
          <UiSelect
            v-model="filters.classificationId"
            :options="classificationOptions"
            label="Classification"
            placeholder="All"
            @update:model-value="performSearch"
          />
          <UiSelect
            v-model="filters.importanceId"
            :options="importanceOptions"
            label="Importance"
            placeholder="All"
            @update:model-value="performSearch"
          />
          <UiSelect
            v-model="filters.documentTypeId"
            :options="documentTypeOptions"
            label="Document Type"
            placeholder="All"
            @update:model-value="performSearch"
          />
          <UiDatePicker
            v-model="filters.dateFrom"
            label="Date From"
            placeholder="Select date"
          />
          <UiDatePicker
            v-model="filters.dateTo"
            label="Date To"
            placeholder="Select date"
          />
        </div>
        <div class="mt-4 flex justify-end">
          <button @click="clearFilters" class="text-sm text-teal hover:text-teal/80">
            Clear all filters
          </button>
        </div>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <svg class="w-8 h-8 animate-spin text-teal" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
      </svg>
    </div>

    <!-- No Results -->
    <div v-else-if="results.length === 0 && (searchQuery || Object.values(filters).some(v => v))" class="bg-white dark:bg-background-dark rounded-xl shadow-sm border border-gray-200 dark:border-border-dark p-12 text-center">
      <div class="w-16 h-16 mx-auto bg-gray-100 dark:bg-surface-dark rounded-full flex items-center justify-center mb-4">
        <svg class="w-8 h-8 text-gray-400 dark:text-zinc-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </div>
      <p class="text-gray-700 dark:text-zinc-300 text-lg font-medium">No documents found</p>
      <p class="text-gray-500 dark:text-zinc-500 mt-1">Try adjusting your search or filters</p>
    </div>

    <!-- Initial State -->
    <div v-else-if="results.length === 0" class="bg-white dark:bg-background-dark rounded-xl shadow-sm border border-gray-200 dark:border-border-dark p-12 text-center">
      <div class="w-16 h-16 mx-auto bg-teal/10 rounded-full flex items-center justify-center mb-4">
        <svg class="w-8 h-8 text-teal" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
      </div>
      <p class="text-gray-700 dark:text-zinc-300 text-lg font-medium">Start searching</p>
      <p class="text-gray-500 dark:text-zinc-500 mt-1">Enter a search term or use filters to find documents</p>
    </div>

    <!-- Results -->
    <div v-else>
      <div class="mb-4 flex items-center justify-between">
        <p class="text-sm text-gray-500 dark:text-zinc-400">Found <span class="font-semibold text-teal">{{ results.length }}</span> document(s)</p>
      </div>

      <div class="bg-white dark:bg-background-dark rounded-xl shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden">
        <table class="w-full">
          <thead class="bg-gray-50 dark:bg-surface-dark/50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Document</th>
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Classification</th>
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Type</th>
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Size</th>
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Modified</th>
              <th class="px-6 py-3 text-left text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Status</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-zinc-800">
            <tr
              v-for="doc in results"
              :key="doc.id"
              @click="viewDocument(doc)"
              class="hover:bg-teal/5 dark:hover:bg-teal/10 cursor-pointer transition-colors"
            >
              <td class="px-6 py-4">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 bg-teal/10 rounded-lg flex items-center justify-center">
                    <svg class="w-5 h-5 text-teal" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                    </svg>
                  </div>
                  <div>
                    <p class="font-medium text-gray-900 dark:text-white">{{ doc.name }}</p>
                    <p class="text-sm text-gray-500 dark:text-zinc-500">{{ doc.extension || 'Unknown' }}</p>
                  </div>
                </div>
              </td>
              <td class="px-6 py-4 text-sm text-gray-600 dark:text-zinc-400">
                {{ getClassificationName(doc.classificationId) }}
              </td>
              <td class="px-6 py-4 text-sm text-gray-600 dark:text-zinc-400">
                {{ getDocumentTypeName(doc.documentTypeId) }}
              </td>
              <td class="px-6 py-4 text-sm text-gray-600 dark:text-zinc-400">
                {{ formatSize(doc.size) }}
              </td>
              <td class="px-6 py-4 text-sm text-gray-600 dark:text-zinc-400">
                {{ formatDate(doc.modifiedAt || doc.createdAt) }}
              </td>
              <td class="px-6 py-4">
                <span
                  :class="[
                    'px-2.5 py-1 rounded-full text-xs font-medium',
                    doc.isCheckedOut ? 'bg-amber-100 dark:bg-amber-500/20 text-amber-700 dark:text-amber-400' : 'bg-teal/10 text-teal'
                  ]"
                >
                  {{ doc.isCheckedOut ? 'Checked Out' : 'Available' }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
