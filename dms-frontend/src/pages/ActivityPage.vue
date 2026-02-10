<script setup lang="ts">
import { ref, computed, onMounted, watch, onUnmounted } from 'vue'
import type { ActivityLog } from '@/types'
import { activityLogsApi } from '@/api/client'
import DocumentIcon from '@/components/common/DocumentIcon.vue'

// State
const activities = ref<ActivityLog[]>([])
const isLoading = ref(true)
const searchQuery = ref('')
const selectedAction = ref<string>('All')
const selectedNodeType = ref<string>('All')

// Dropdown states
const showActionDropdown = ref(false)
const showTypeDropdown = ref(false)
const showPageSizeDropdown = ref(false)

// Pagination
const currentPage = ref(1)
const pageSize = ref(25)
const pageSizeOptions = [10, 25, 50, 100]

// Action types for filtering
const actionTypes = ['All', 'Created', 'Updated', 'Deleted', 'Viewed', 'Downloaded', 'CheckedOut', 'CheckedIn', 'Moved', 'Copied']
const nodeTypes = ['All', 'Cabinet', 'Folder', 'Document']

// Close dropdowns when clicking outside
function handleClickOutside(e: MouseEvent) {
  const target = e.target as HTMLElement
  if (!target.closest('.dropdown-action')) showActionDropdown.value = false
  if (!target.closest('.dropdown-type')) showTypeDropdown.value = false
  if (!target.closest('.dropdown-pagesize')) showPageSizeDropdown.value = false
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})

// Load data
onMounted(async () => {
  await loadActivities()
})

async function loadActivities() {
  isLoading.value = true
  try {
    const response = await activityLogsApi.getRecent(500) // Load more for client-side filtering
    activities.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

// Filtered activities
const filteredActivities = computed(() => {
  let result = activities.value

  // Filter by search
  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(a =>
      a.nodeName?.toLowerCase().includes(q) ||
      a.details?.toLowerCase().includes(q) ||
      a.userName?.toLowerCase().includes(q) ||
      a.action?.toLowerCase().includes(q)
    )
  }

  // Filter by action type
  if (selectedAction.value !== 'All') {
    result = result.filter(a => a.action === selectedAction.value)
  }

  // Filter by node type
  if (selectedNodeType.value !== 'All') {
    result = result.filter(a => a.nodeType === selectedNodeType.value)
  }

  return result
})

// Paginated activities
const paginatedActivities = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value
  const end = start + pageSize.value
  return filteredActivities.value.slice(start, end)
})

// Total pages
const totalPages = computed(() => Math.ceil(filteredActivities.value.length / pageSize.value))

// Stats
const stats = computed(() => ({
  total: activities.value.length,
  filtered: filteredActivities.value.length,
  created: activities.value.filter(a => a.action === 'Created').length,
  updated: activities.value.filter(a => a.action === 'Updated').length,
  deleted: activities.value.filter(a => a.action === 'Deleted').length
}))

// Reset to first page when filters change
watch([searchQuery, selectedAction, selectedNodeType, pageSize], () => {
  currentPage.value = 1
})

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined
  })
}

function formatFullDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString()
}

function getActionIcon(action: string): string {
  switch (action) {
    case 'Created': return 'add_circle'
    case 'Updated': return 'edit'
    case 'Deleted': return 'delete'
    case 'Viewed': return 'visibility'
    case 'Downloaded': return 'download'
    case 'CheckedOut': return 'lock'
    case 'CheckedIn': return 'lock_open'
    case 'Moved': return 'drive_file_move'
    case 'Copied': return 'content_copy'
    case 'Shared': return 'share'
    case 'Login': return 'login'
    case 'Logout': return 'logout'
    default: return 'history'
  }
}

function getActionColor(action: string): string {
  switch (action) {
    case 'Created': return 'bg-teal'
    case 'Updated': return 'bg-[#1a3a4a]'
    case 'Deleted': return 'bg-[#2d3748]'
    case 'Viewed': return 'bg-[#374151]'
    case 'Downloaded': return 'bg-teal/80'
    case 'CheckedOut': return 'bg-[#1e3a5f]'
    case 'CheckedIn': return 'bg-teal'
    case 'Moved': return 'bg-[#1a3a4a]'
    case 'Copied': return 'bg-[#1a3a4a]'
    case 'Shared': return 'bg-teal/70'
    case 'DiscardedCheckout': return 'bg-[#374151]'
    default: return 'bg-[#374151]'
  }
}

function getActionBadgeClass(action: string): string {
  switch (action) {
    case 'Created': return 'bg-teal/15 text-teal'
    case 'Updated': return 'bg-zinc-200 dark:bg-border-dark text-zinc-700 dark:text-zinc-300'
    case 'Deleted': return 'bg-zinc-300 dark:bg-zinc-600 text-zinc-700 dark:text-zinc-300'
    case 'Viewed': return 'bg-zinc-100 dark:bg-border-dark text-zinc-600 dark:text-zinc-400'
    case 'Downloaded': return 'bg-teal/10 text-teal'
    case 'CheckedOut': return 'bg-navy/10 dark:bg-border-dark text-navy dark:text-zinc-300'
    case 'CheckedIn': return 'bg-teal/15 text-teal'
    case 'Moved': return 'bg-zinc-200 dark:bg-border-dark text-zinc-700 dark:text-zinc-300'
    case 'Copied': return 'bg-zinc-200 dark:bg-border-dark text-zinc-700 dark:text-zinc-300'
    case 'DiscardedCheckout': return 'bg-zinc-200 dark:bg-zinc-600 text-zinc-600 dark:text-zinc-400'
    default: return 'bg-zinc-100 dark:bg-border-dark text-zinc-600 dark:text-zinc-400'
  }
}

function getNodeTypeIcon(nodeType: string): string {
  switch (nodeType) {
    case 'Cabinet': return 'inventory_2'
    case 'Folder': return 'folder'
    case 'Document': return 'description'
    default: return 'article'
  }
}

function goToPage(page: number) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page
  }
}

function clearFilters() {
  searchQuery.value = ''
  selectedAction.value = 'All'
  selectedNodeType.value = 'All'
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Activity Log</h1>
        <p class="text-zinc-500 mt-1">Track all system activities and changes</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- Search -->
        <div class="relative">
          <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400 text-lg">search</span>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search activities..."
            class="w-64 pl-10 pr-4 py-2 bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
          />
        </div>
        <button
          @click="loadActivities"
          class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-surface-dark hover:bg-zinc-50 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark"
        >
          <span class="material-symbols-outlined text-lg">refresh</span>
          Refresh
        </button>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">history</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Total</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.total }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">schedule</span>
            All activities
          </p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">add_circle</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Created</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.created }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">trending_up</span>
            New items
          </p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">edit</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Updated</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.updated }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">sync</span>
            Modifications
          </p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">delete</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Deleted</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.deleted }}</p>
          <p class="text-[10px] text-teal mt-4 font-medium">Removed items</p>
        </div>
      </div>
    </div>

    <!-- Filters Card -->
    <div class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark p-4">
      <div class="flex items-center justify-between">
        <!-- Filters -->
        <div class="flex items-center gap-4">
          <!-- Action Filter -->
          <div class="flex items-center gap-2">
            <span class="text-sm text-zinc-500 dark:text-zinc-400">Action:</span>
            <div class="relative dropdown-action">
              <button
                @click.stop="showActionDropdown = !showActionDropdown"
                class="flex items-center gap-2 px-4 py-2 bg-zinc-100 dark:bg-surface-dark hover:bg-zinc-200 dark:hover:bg-border-dark rounded-lg text-sm font-medium text-zinc-700 dark:text-zinc-300 transition-colors min-w-[120px] justify-between"
              >
                <span class="flex items-center gap-2">
                  <span v-if="selectedAction !== 'All'" class="material-symbols-outlined text-sm" :class="getActionColor(selectedAction).replace('bg-', 'text-')">{{ getActionIcon(selectedAction) }}</span>
                  {{ selectedAction }}
                </span>
                <span class="material-symbols-outlined text-lg transition-transform" :class="{ 'rotate-180': showActionDropdown }">expand_more</span>
              </button>
              <Transition
                enter-active-class="transition duration-150 ease-out"
                enter-from-class="opacity-0 scale-95 -translate-y-1"
                enter-to-class="opacity-100 scale-100 translate-y-0"
                leave-active-class="transition duration-100 ease-in"
                leave-from-class="opacity-100 scale-100 translate-y-0"
                leave-to-class="opacity-0 scale-95 -translate-y-1"
              >
                <div v-if="showActionDropdown" class="absolute top-full left-0 mt-2 w-48 bg-white dark:bg-surface-dark rounded-lg shadow-xl border border-zinc-200 dark:border-border-dark py-2 z-50 max-h-64 overflow-y-auto">
                  <button
                    v-for="action in actionTypes"
                    :key="action"
                    @click="selectedAction = action; showActionDropdown = false"
                    class="w-full px-4 py-2 text-left text-sm hover:bg-zinc-100 dark:hover:bg-border-dark flex items-center gap-3 transition-colors"
                    :class="selectedAction === action ? 'bg-teal/10 text-teal font-medium' : 'text-zinc-700 dark:text-zinc-300'"
                  >
                    <span v-if="action !== 'All'" class="material-symbols-outlined text-sm" :class="getActionColor(action).replace('bg-', 'text-')">{{ getActionIcon(action) }}</span>
                    <span v-else class="material-symbols-outlined text-sm text-zinc-400">filter_list</span>
                    {{ action }}
                    <span v-if="selectedAction === action" class="material-symbols-outlined text-sm ml-auto">check</span>
                  </button>
                </div>
              </Transition>
            </div>
          </div>

          <!-- Node Type Filter -->
          <div class="flex items-center gap-2">
            <span class="text-sm text-zinc-500 dark:text-zinc-400">Type:</span>
            <div class="relative dropdown-type">
              <button
                @click.stop="showTypeDropdown = !showTypeDropdown"
                class="flex items-center gap-2 px-4 py-2 bg-zinc-100 dark:bg-surface-dark hover:bg-zinc-200 dark:hover:bg-border-dark rounded-lg text-sm font-medium text-zinc-700 dark:text-zinc-300 transition-colors min-w-[120px] justify-between"
              >
                <span class="flex items-center gap-2">
                  <span class="material-symbols-outlined text-sm text-zinc-500">{{ selectedNodeType === 'All' ? 'category' : getNodeTypeIcon(selectedNodeType) }}</span>
                  {{ selectedNodeType }}
                </span>
                <span class="material-symbols-outlined text-lg transition-transform" :class="{ 'rotate-180': showTypeDropdown }">expand_more</span>
              </button>
              <Transition
                enter-active-class="transition duration-150 ease-out"
                enter-from-class="opacity-0 scale-95 -translate-y-1"
                enter-to-class="opacity-100 scale-100 translate-y-0"
                leave-active-class="transition duration-100 ease-in"
                leave-from-class="opacity-100 scale-100 translate-y-0"
                leave-to-class="opacity-0 scale-95 -translate-y-1"
              >
                <div v-if="showTypeDropdown" class="absolute top-full left-0 mt-2 w-44 bg-white dark:bg-surface-dark rounded-lg shadow-xl border border-zinc-200 dark:border-border-dark py-2 z-50">
                  <button
                    v-for="type in nodeTypes"
                    :key="type"
                    @click="selectedNodeType = type; showTypeDropdown = false"
                    class="w-full px-4 py-2 text-left text-sm hover:bg-zinc-100 dark:hover:bg-border-dark flex items-center gap-3 transition-colors"
                    :class="selectedNodeType === type ? 'bg-teal/10 text-teal font-medium' : 'text-zinc-700 dark:text-zinc-300'"
                  >
                    <span class="material-symbols-outlined text-sm" :class="selectedNodeType === type ? 'text-teal' : 'text-zinc-500'">{{ type === 'All' ? 'category' : getNodeTypeIcon(type) }}</span>
                    {{ type }}
                    <span v-if="selectedNodeType === type" class="material-symbols-outlined text-sm ml-auto">check</span>
                  </button>
                </div>
              </Transition>
            </div>
          </div>

          <!-- Clear Filters -->
          <button
            v-if="searchQuery || selectedAction !== 'All' || selectedNodeType !== 'All'"
            @click="clearFilters"
            class="text-sm text-teal hover:text-teal/80 font-medium flex items-center gap-1"
          >
            <span class="material-symbols-outlined text-sm">close</span>
            Clear filters
          </button>
        </div>

        <!-- Stats -->
        <div class="flex items-center gap-4 text-sm">
          <span class="text-zinc-500 dark:text-zinc-400">
            Showing <span class="font-semibold text-zinc-900 dark:text-white">{{ filteredActivities.length }}</span> of {{ stats.total }} activities
          </span>
        </div>
      </div>
    </div>

    <!-- Content Card -->
    <div class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark overflow-hidden">
      <div class="p-6">
      <!-- Loading -->
      <div v-if="isLoading" class="space-y-3">
        <div v-for="i in 10" :key="i" class="bg-zinc-50 dark:bg-surface-dark rounded-lg p-4 animate-pulse">
          <div class="flex items-center gap-4">
            <div class="w-10 h-10 bg-zinc-200 dark:bg-border-dark rounded-full"></div>
            <div class="flex-1">
              <div class="h-4 bg-zinc-200 dark:bg-border-dark rounded w-1/4 mb-2"></div>
              <div class="h-3 bg-zinc-200 dark:bg-border-dark rounded w-1/2"></div>
            </div>
            <div class="h-3 bg-zinc-200 dark:bg-border-dark rounded w-20"></div>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else-if="filteredActivities.length === 0" class="text-center py-16">
        <div class="w-20 h-20 rounded-lg bg-zinc-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-5xl text-zinc-400">history_toggle_off</span>
        </div>
        <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">No Activities Found</h3>
        <p class="text-zinc-500 dark:text-zinc-400 mt-1">
          {{ searchQuery || selectedAction !== 'All' || selectedNodeType !== 'All' ? 'Try adjusting your filters' : 'No activity recorded yet' }}
        </p>
        <button
          v-if="searchQuery || selectedAction !== 'All' || selectedNodeType !== 'All'"
          @click="clearFilters"
          class="mt-4 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors"
        >
          Clear Filters
        </button>
      </div>

      <!-- Activity List -->
      <div v-else class="space-y-2">
        <div
          v-for="(activity, index) in paginatedActivities"
          :key="activity.id"
          class="bg-zinc-50 dark:bg-surface-dark rounded-lg border border-zinc-100 dark:border-border-dark p-4 hover:shadow-md hover:border-teal/30 transition-all"
        >
          <div class="flex items-start gap-4">
            <!-- Action Icon -->
            <DocumentIcon :icon="getActionIcon(activity.action)" :index="index" size="md" />

            <!-- Content -->
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 flex-wrap">
                <span :class="['px-2 py-0.5 text-xs font-medium rounded-full', getActionBadgeClass(activity.action)]">
                  {{ activity.action }}
                </span>
                <span class="flex items-center gap-1 text-sm text-zinc-500 dark:text-zinc-400">
                  <span class="material-symbols-outlined text-sm">{{ getNodeTypeIcon(activity.nodeType || '') }}</span>
                  {{ activity.nodeType }}
                </span>
                <span v-if="activity.userName" class="text-sm text-zinc-500 dark:text-zinc-400">
                  by <span class="font-medium text-zinc-700 dark:text-zinc-300">{{ activity.userName }}</span>
                </span>
              </div>

              <p class="mt-1 font-medium text-zinc-900 dark:text-white truncate">{{ activity.nodeName || 'Unknown' }}</p>

              <p v-if="activity.details" class="mt-1 text-sm text-zinc-500 dark:text-zinc-400 line-clamp-1">
                {{ activity.details }}
              </p>
            </div>

            <!-- Time -->
            <div class="text-right flex-shrink-0">
              <div class="text-sm font-medium text-zinc-600 dark:text-zinc-300" :title="formatFullDate(activity.createdAt)">
                {{ formatDate(activity.createdAt) }}
              </div>
              <div v-if="activity.ipAddress" class="text-xs text-zinc-400 dark:text-zinc-500 mt-1">
                {{ activity.ipAddress }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Pagination -->
      <div v-if="!isLoading && filteredActivities.length > 0" class="mt-6 flex items-center justify-between">
        <!-- Page Size -->
        <div class="flex items-center gap-2">
          <span class="text-sm text-zinc-500 dark:text-zinc-400">Show</span>
          <div class="relative dropdown-pagesize">
            <button
              @click.stop="showPageSizeDropdown = !showPageSizeDropdown"
              class="flex items-center gap-2 px-3 py-1.5 bg-zinc-50 dark:bg-surface-dark hover:bg-zinc-100 dark:hover:bg-border-dark border border-zinc-200 dark:border-border-dark rounded-lg text-sm font-medium text-zinc-700 dark:text-zinc-300 transition-colors min-w-[70px] justify-between"
            >
              {{ pageSize }}
              <span class="material-symbols-outlined text-base transition-transform" :class="{ 'rotate-180': showPageSizeDropdown }">expand_more</span>
            </button>
            <Transition
              enter-active-class="transition duration-150 ease-out"
              enter-from-class="opacity-0 scale-95 translate-y-1"
              enter-to-class="opacity-100 scale-100 translate-y-0"
              leave-active-class="transition duration-100 ease-in"
              leave-from-class="opacity-100 scale-100 translate-y-0"
              leave-to-class="opacity-0 scale-95 translate-y-1"
            >
              <div v-if="showPageSizeDropdown" class="absolute bottom-full left-0 mb-2 w-24 bg-white dark:bg-surface-dark rounded-lg shadow-xl border border-zinc-200 dark:border-border-dark py-2 z-50">
                <button
                  v-for="size in pageSizeOptions"
                  :key="size"
                  @click="pageSize = size; showPageSizeDropdown = false"
                  class="w-full px-4 py-2 text-left text-sm hover:bg-zinc-100 dark:hover:bg-border-dark flex items-center justify-between transition-colors"
                  :class="pageSize === size ? 'bg-teal/10 text-teal font-medium' : 'text-zinc-700 dark:text-zinc-300'"
                >
                  {{ size }}
                  <span v-if="pageSize === size" class="material-symbols-outlined text-sm">check</span>
                </button>
              </div>
            </Transition>
          </div>
          <span class="text-sm text-zinc-500 dark:text-zinc-400">per page</span>
        </div>

        <!-- Page Navigation -->
        <div class="flex items-center gap-2">
          <button
            @click="goToPage(1)"
            :disabled="currentPage === 1"
            class="p-2 rounded-lg border border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-400 hover:bg-zinc-50 dark:hover:bg-border-dark disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span class="material-symbols-outlined text-lg">first_page</span>
          </button>
          <button
            @click="goToPage(currentPage - 1)"
            :disabled="currentPage === 1"
            class="p-2 rounded-lg border border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-400 hover:bg-zinc-50 dark:hover:bg-border-dark disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span class="material-symbols-outlined text-lg">chevron_left</span>
          </button>

          <div class="flex items-center gap-1">
            <template v-for="page in Math.min(totalPages, 5)" :key="page">
              <button
                v-if="totalPages <= 5 || (page <= 3 || page > totalPages - 2)"
                @click="goToPage(totalPages <= 5 ? page : (page <= 3 ? page : totalPages - (5 - page)))"
                :class="[
                  'w-10 h-10 rounded-lg font-medium text-sm transition-colors',
                  currentPage === (totalPages <= 5 ? page : (page <= 3 ? page : totalPages - (5 - page)))
                    ? 'bg-teal text-white'
                    : 'border border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-400 hover:bg-zinc-50 dark:hover:bg-border-dark'
                ]"
              >
                {{ totalPages <= 5 ? page : (page <= 3 ? page : totalPages - (5 - page)) }}
              </button>
            </template>
          </div>

          <button
            @click="goToPage(currentPage + 1)"
            :disabled="currentPage === totalPages"
            class="p-2 rounded-lg border border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-400 hover:bg-zinc-50 dark:hover:bg-border-dark disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span class="material-symbols-outlined text-lg">chevron_right</span>
          </button>
          <button
            @click="goToPage(totalPages)"
            :disabled="currentPage === totalPages"
            class="p-2 rounded-lg border border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-400 hover:bg-zinc-50 dark:hover:bg-border-dark disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <span class="material-symbols-outlined text-lg">last_page</span>
          </button>
        </div>

        <!-- Page Info -->
        <div class="text-sm text-zinc-500 dark:text-zinc-400">
          Page {{ currentPage }} of {{ totalPages }}
        </div>
      </div>
      </div>
    </div>
  </div>
</template>
