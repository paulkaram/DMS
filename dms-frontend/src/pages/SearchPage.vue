<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { searchApi, referenceDataApi } from '@/api/client'
import type { Classification, DocumentType } from '@/types'

const route = useRoute()
const router = useRouter()

const searchQuery = ref('')
const results = ref<any[]>([])
const totalCount = ref(0)
const elapsedMs = ref(0)
const searchAfterToken = ref<string | null>(null)
const facets = ref<any[]>([])
const isLoading = ref(false)
const isLoadingMore = ref(false)
const hasSearched = ref(false)

// Filters
const filters = ref({
  classificationId: '',
  documentTypeId: '',
  state: '',
  extension: '',
  dateFrom: '',
  dateTo: '',
  sortBy: 'relevance',
  sortDescending: true
})

// Reference data for filter labels
const classifications = ref<Classification[]>([])
const documentTypes = ref<DocumentType[]>([])

// Document states for filter
const documentStates = ['Draft', 'Active', 'Record', 'Archived', 'OnHold', 'PendingDisposal', 'Disposed']

onMounted(async () => {
  await loadReferenceData()
  if (route.query.q) {
    searchQuery.value = route.query.q as string
    performSearch()
  }
})

watch(() => route.query.q, (newQuery) => {
  if (newQuery && newQuery !== searchQuery.value) {
    searchQuery.value = newQuery as string
    performSearch()
  }
})

async function loadReferenceData() {
  try {
    const [classRes, docTypeRes] = await Promise.all([
      referenceDataApi.getClassifications(),
      referenceDataApi.getDocumentTypes()
    ])
    classifications.value = classRes.data
    documentTypes.value = docTypeRes.data
  } catch { /* silently fail */ }
}

function buildRequest(loadMore = false) {
  const req: any = {
    query: searchQuery.value.trim(),
    page: 1,
    pageSize: 25,
    sortBy: filters.value.sortBy,
    sortDescending: filters.value.sortDescending
  }
  if (filters.value.classificationId) req.classificationId = filters.value.classificationId
  if (filters.value.documentTypeId) req.documentTypeId = filters.value.documentTypeId
  if (filters.value.state) req.state = filters.value.state
  if (filters.value.extension) req.extension = filters.value.extension
  if (filters.value.dateFrom) req.dateFrom = filters.value.dateFrom
  if (filters.value.dateTo) req.dateTo = filters.value.dateTo
  if (loadMore && searchAfterToken.value) req.searchAfterToken = searchAfterToken.value
  return req
}

async function performSearch() {
  isLoading.value = true
  hasSearched.value = true
  try {
    const res = await searchApi.searchDocuments(buildRequest())
    const data = res.data
    results.value = data.items || []
    totalCount.value = data.totalCount || 0
    elapsedMs.value = data.elapsedMs || 0
    searchAfterToken.value = data.searchAfterToken || null
    facets.value = data.facets || []
  } catch {
    results.value = []
    totalCount.value = 0
  } finally {
    isLoading.value = false
  }
}

async function loadMore() {
  if (!searchAfterToken.value || isLoadingMore.value) return
  isLoadingMore.value = true
  try {
    const res = await searchApi.searchDocuments(buildRequest(true))
    const data = res.data
    results.value.push(...(data.items || []))
    searchAfterToken.value = data.searchAfterToken || null
  } catch { /* silently fail */ }
  finally { isLoadingMore.value = false }
}

function handleSearch() {
  router.push({ name: 'search', query: { q: searchQuery.value } })
  performSearch()
}

function applyFacetFilter(field: string, value: string) {
  if (field === 'classificationName' || field === 'classification') {
    // Find classification ID by name
    const cls = classifications.value.find(c => c.name === value)
    filters.value.classificationId = cls ? cls.id : ''
  } else if (field === 'documentTypeName' || field === 'documentType') {
    const dt = documentTypes.value.find(d => d.name === value)
    filters.value.documentTypeId = dt ? dt.id : ''
  } else if (field === 'state') {
    filters.value.state = value
  } else if (field === 'extension') {
    filters.value.extension = value
  }
  performSearch()
}

function clearFilters() {
  filters.value = {
    classificationId: '',
    documentTypeId: '',
    state: '',
    extension: '',
    dateFrom: '',
    dateTo: '',
    sortBy: 'relevance',
    sortDescending: true
  }
  performSearch()
}

const hasActiveFilters = computed(() =>
  filters.value.classificationId || filters.value.documentTypeId ||
  filters.value.state || filters.value.extension ||
  filters.value.dateFrom || filters.value.dateTo
)

function formatSize(bytes: number): string {
  if (!bytes || bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

function formatDate(dateStr: string): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}

function viewDocument(item: any) {
  router.push(`/documents/${item.id}`)
}

const stateColors: Record<string, string> = {
  Draft: 'bg-zinc-100 text-zinc-600 dark:bg-zinc-700/50 dark:text-zinc-300',
  Active: 'bg-teal/10 text-teal',
  Record: 'bg-blue-50 text-blue-600 dark:bg-blue-900/20 dark:text-blue-400',
  Archived: 'bg-amber-50 text-amber-600 dark:bg-amber-900/20 dark:text-amber-400',
  Disposed: 'bg-rose-50 text-rose-600 dark:bg-rose-900/20 dark:text-rose-400',
  OnHold: 'bg-purple-50 text-purple-600 dark:bg-purple-900/20 dark:text-purple-400',
  PendingDisposal: 'bg-orange-50 text-orange-600 dark:bg-orange-900/20 dark:text-orange-400'
}

const sortOptions = [
  { value: 'relevance', label: 'Relevance' },
  { value: 'name', label: 'Name' },
  { value: 'date', label: 'Date' },
  { value: 'size', label: 'Size' }
]
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Search Documents</h1>
      <p class="text-zinc-500 text-sm mt-0.5">Full-text search across all cabinets and folders</p>
    </div>

    <!-- Search Box -->
    <div class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark p-5">
      <div class="flex gap-3">
        <div class="flex-1 relative">
          <span class="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400 text-xl">search</span>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search by name, content, OCR text..."
            class="w-full pl-12 pr-4 py-3 border border-zinc-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/30 focus:border-teal bg-white dark:bg-surface-dark text-zinc-900 dark:text-white placeholder-zinc-400 dark:placeholder-zinc-500 transition-all text-sm"
            @keyup.enter="handleSearch"
          />
        </div>
        <button
          @click="handleSearch"
          class="px-6 py-2.5 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors font-medium text-sm shadow-sm"
        >
          Search
        </button>
      </div>

      <!-- Quick filters row -->
      <div v-if="hasSearched" class="mt-3 flex items-center gap-2 flex-wrap">
        <!-- Sort -->
        <div class="flex items-center gap-1.5">
          <span class="text-[10px] font-medium text-zinc-400 uppercase">Sort:</span>
          <select
            v-model="filters.sortBy"
            @change="performSearch"
            class="text-xs bg-zinc-50 dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded px-2 py-1 text-zinc-700 dark:text-zinc-300"
          >
            <option v-for="opt in sortOptions" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
          </select>
          <button
            @click="filters.sortDescending = !filters.sortDescending; performSearch()"
            class="p-1 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 transition-colors"
            :title="filters.sortDescending ? 'Descending' : 'Ascending'"
          >
            <span class="material-symbols-outlined text-sm">{{ filters.sortDescending ? 'arrow_downward' : 'arrow_upward' }}</span>
          </button>
        </div>

        <span class="text-zinc-200 dark:text-zinc-700">|</span>

        <!-- State filter -->
        <select
          v-model="filters.state"
          @change="performSearch"
          class="text-xs bg-zinc-50 dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded px-2 py-1 text-zinc-700 dark:text-zinc-300"
        >
          <option value="">All States</option>
          <option v-for="s in documentStates" :key="s" :value="s">{{ s }}</option>
        </select>

        <!-- Date range -->
        <input
          v-model="filters.dateFrom"
          type="date"
          @change="performSearch"
          class="text-xs bg-zinc-50 dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded px-2 py-1 text-zinc-700 dark:text-zinc-300"
          placeholder="From"
        />
        <span class="text-zinc-400 text-xs">to</span>
        <input
          v-model="filters.dateTo"
          type="date"
          @change="performSearch"
          class="text-xs bg-zinc-50 dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded px-2 py-1 text-zinc-700 dark:text-zinc-300"
          placeholder="To"
        />

        <!-- Clear -->
        <button
          v-if="hasActiveFilters"
          @click="clearFilters"
          class="text-xs text-teal hover:text-teal/80 font-medium ml-auto"
        >
          Clear filters
        </button>
      </div>
    </div>

    <!-- Results area -->
    <div v-if="hasSearched" class="flex gap-6">
      <!-- Facet Sidebar -->
      <aside v-if="facets.length > 0" class="w-56 shrink-0 space-y-4">
        <div v-for="facetGroup in facets" :key="facetGroup.field" class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden">
          <h3 class="px-3 py-2.5 text-[10px] font-bold text-zinc-400 uppercase tracking-widest bg-zinc-50 dark:bg-surface-dark/50 border-b border-zinc-200 dark:border-border-dark">
            {{ facetGroup.field === 'classificationName' ? 'Classification' : facetGroup.field === 'documentTypeName' ? 'Document Type' : facetGroup.field === 'extension' ? 'File Type' : facetGroup.field }}
          </h3>
          <div class="p-2 space-y-0.5">
            <button
              v-for="fv in facetGroup.values.slice(0, 8)"
              :key="fv.value"
              @click="applyFacetFilter(facetGroup.field, fv.value)"
              class="w-full flex items-center justify-between px-2.5 py-1.5 rounded text-xs hover:bg-teal/5 dark:hover:bg-teal/10 transition-colors group"
            >
              <span class="text-zinc-600 dark:text-zinc-300 group-hover:text-teal truncate">{{ fv.label || fv.value }}</span>
              <span class="text-[10px] font-medium text-zinc-400 bg-zinc-100 dark:bg-zinc-700 px-1.5 py-0.5 rounded-full">{{ fv.count }}</span>
            </button>
          </div>
        </div>
      </aside>

      <!-- Results Column -->
      <div class="flex-1 min-w-0">
        <!-- Loading -->
        <div v-if="isLoading" class="flex items-center justify-center py-16 gap-3">
          <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
          <span class="text-zinc-500 text-sm">Searching...</span>
        </div>

        <!-- No Results -->
        <div v-else-if="results.length === 0" class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-12 text-center">
          <span class="material-symbols-outlined text-zinc-300 text-5xl mb-3 block">search_off</span>
          <p class="text-zinc-700 dark:text-zinc-300 font-medium">No documents found</p>
          <p class="text-zinc-500 text-sm mt-1">Try adjusting your search terms or filters</p>
        </div>

        <!-- Results List -->
        <template v-else>
          <!-- Stats bar -->
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs text-zinc-500">
              Found <span class="font-semibold text-teal">{{ totalCount.toLocaleString() }}</span> result(s)
              <span v-if="elapsedMs" class="text-zinc-400">&middot; {{ elapsedMs.toFixed(0) }}ms</span>
            </p>
          </div>

          <!-- Result cards -->
          <div class="space-y-2">
            <div
              v-for="item in results"
              :key="item.id"
              @click="viewDocument(item)"
              class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-4 hover:border-teal/50 hover:shadow-sm cursor-pointer transition-all group"
            >
              <div class="flex items-start gap-3">
                <!-- Icon -->
                <div class="w-10 h-10 rounded-lg bg-teal/10 flex items-center justify-center shrink-0 group-hover:bg-teal/20 transition-colors">
                  <span class="material-symbols-outlined text-teal text-xl">description</span>
                </div>

                <!-- Content -->
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 mb-0.5">
                    <h3 class="text-sm font-semibold text-zinc-900 dark:text-white truncate group-hover:text-teal transition-colors">{{ item.name }}</h3>
                    <span v-if="item.state" class="px-1.5 py-0.5 text-[9px] font-bold uppercase rounded-full shrink-0" :class="stateColors[item.state] || 'bg-zinc-100 text-zinc-500'">
                      {{ item.state }}
                    </span>
                    <span v-if="item.score" class="text-[9px] text-zinc-400 ml-auto shrink-0">Score: {{ item.score.toFixed(2) }}</span>
                  </div>

                  <!-- Highlights -->
                  <div v-if="item.highlights && item.highlights.length" class="mb-1.5">
                    <p v-for="(hl, idx) in item.highlights.slice(0, 2)" :key="idx" class="text-xs text-zinc-500 dark:text-zinc-400 leading-relaxed" v-html="hl"></p>
                  </div>
                  <p v-else-if="item.description" class="text-xs text-zinc-500 dark:text-zinc-400 line-clamp-1 mb-1.5">{{ item.description }}</p>

                  <!-- Meta row -->
                  <div class="flex items-center gap-3 text-[10px] text-zinc-400 flex-wrap">
                    <span v-if="item.extension" class="inline-flex items-center gap-0.5">
                      <span class="material-symbols-outlined" style="font-size: 11px;">insert_drive_file</span>
                      {{ item.extension.toUpperCase() }}
                    </span>
                    <span v-if="item.size" class="inline-flex items-center gap-0.5">
                      <span class="material-symbols-outlined" style="font-size: 11px;">straighten</span>
                      {{ formatSize(item.size) }}
                    </span>
                    <span v-if="item.classificationName" class="inline-flex items-center gap-0.5">
                      <span class="material-symbols-outlined" style="font-size: 11px;">label</span>
                      {{ item.classificationName }}
                    </span>
                    <span v-if="item.documentTypeName" class="inline-flex items-center gap-0.5">
                      <span class="material-symbols-outlined" style="font-size: 11px;">category</span>
                      {{ item.documentTypeName }}
                    </span>
                    <span v-if="item.folderPath" class="inline-flex items-center gap-0.5 truncate max-w-[200px]">
                      <span class="material-symbols-outlined" style="font-size: 11px;">folder</span>
                      {{ item.folderPath }}
                    </span>
                    <span class="inline-flex items-center gap-0.5">
                      <span class="material-symbols-outlined" style="font-size: 11px;">calendar_today</span>
                      {{ formatDate(item.createdAt) }}
                    </span>
                    <span v-if="item.createdByName">by {{ item.createdByName }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Load More -->
          <div v-if="searchAfterToken" class="mt-4 text-center">
            <button
              @click="loadMore"
              :disabled="isLoadingMore"
              class="px-6 py-2.5 bg-zinc-100 dark:bg-surface-dark hover:bg-zinc-200 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg text-sm font-medium transition-colors disabled:opacity-50"
            >
              <span v-if="isLoadingMore" class="inline-flex items-center gap-2">
                <span class="material-symbols-outlined text-sm animate-spin">refresh</span>
                Loading...
              </span>
              <span v-else>Load more results</span>
            </button>
          </div>
        </template>
      </div>
    </div>

    <!-- Initial State (no search yet) -->
    <div v-if="!hasSearched" class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-16 text-center">
      <div class="w-16 h-16 mx-auto bg-teal/10 rounded-full flex items-center justify-center mb-4">
        <span class="material-symbols-outlined text-teal text-3xl">search</span>
      </div>
      <p class="text-zinc-700 dark:text-zinc-300 font-medium text-lg">Start searching</p>
      <p class="text-zinc-500 text-sm mt-1">Enter a search term to find documents by name, content, or metadata</p>
    </div>
  </div>
</template>
