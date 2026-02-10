<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import type { RecycleBinItem } from '@/types'
import { recycleBinApi } from '@/api/client'
import DocumentIcon from '@/components/common/DocumentIcon.vue'

const recycleBinItems = ref<RecycleBinItem[]>([])
const isLoading = ref(true)
const selectedFilter = ref<number | null>(null)

onMounted(async () => {
  await loadRecycleBin()
})

async function loadRecycleBin() {
  isLoading.value = true
  try {
    const response = await recycleBinApi.getMyRecycleBin()
    recycleBinItems.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

async function restoreItem(item: RecycleBinItem) {
  try {
    await recycleBinApi.restore(item.id)
    recycleBinItems.value = recycleBinItems.value.filter(i => i.id !== item.id)
  } catch (err) {
  }
}

async function permanentlyDelete(item: RecycleBinItem) {
  if (!confirm(`Permanently delete "${item.nodeName}"? This cannot be undone.`)) return

  try {
    await recycleBinApi.permanentlyDelete(item.id)
    recycleBinItems.value = recycleBinItems.value.filter(i => i.id !== item.id)
  } catch (err) {
  }
}

async function emptyRecycleBin() {
  if (!confirm('Empty the entire recycle bin? This cannot be undone.')) return

  try {
    await recycleBinApi.empty()
    recycleBinItems.value = []
  } catch (err) {
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function getNodeTypeName(nodeType: number): string {
  switch (nodeType) {
    case 1: return 'Cabinet'
    case 2: return 'Folder'
    case 3: return 'Document'
    default: return 'Unknown'
  }
}

function getNodeIcon(nodeType: number): string {
  switch (nodeType) {
    case 1: return 'inventory_2'
    case 2: return 'folder'
    case 3: return 'description'
    default: return 'help'
  }
}

function getExtension(name: string): string {
  const parts = name.split('.')
  return parts.length > 1 ? parts.pop()! : ''
}

const displayedItems = computed(() => {
  if (selectedFilter.value === null) return recycleBinItems.value
  return recycleBinItems.value.filter(i => i.nodeType === selectedFilter.value)
})

// Stats for header cards
const documentCount = computed(() => recycleBinItems.value.filter(i => i.nodeType === 3).length)
const folderCount = computed(() => recycleBinItems.value.filter(i => i.nodeType === 2).length)
const cabinetCount = computed(() => recycleBinItems.value.filter(i => i.nodeType === 1).length)
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Recycle Bin</h1>
        <p class="text-zinc-500 mt-1">Deleted items (auto-purged after 30 days)</p>
      </div>
      <div class="flex items-center gap-3">
        <button
          @click="loadRecycleBin"
          class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-surface-dark hover:bg-zinc-50 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark"
        >
          <span class="material-symbols-outlined text-lg">refresh</span>
          Refresh
        </button>
        <button
          v-if="recycleBinItems.length > 0"
          @click="emptyRecycleBin"
          class="flex items-center gap-2 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors font-medium text-sm"
        >
          <span class="material-symbols-outlined text-lg">delete_forever</span>
          Empty Recycle Bin
        </button>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden border border-zinc-800/50">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">description</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Documents</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ documentCount }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Deleted documents</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden border border-zinc-800/50">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">folder</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Folders</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ folderCount }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Deleted folders</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden border border-zinc-800/50">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">inventory_2</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Cabinets</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ cabinetCount }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Deleted cabinets</p>
        </div>
      </div>
    </div>

    <!-- Filter Tabs -->
    <div class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark overflow-hidden">
      <div class="border-b border-zinc-200 dark:border-border-dark">
        <nav class="flex">
          <button
            @click="selectedFilter = null"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              selectedFilter === null
                ? 'border-teal text-teal'
                : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">all_inbox</span>
              All Items
              <span v-if="recycleBinItems.length > 0" class="px-2 py-0.5 text-xs bg-teal/15 text-teal rounded-full">
                {{ recycleBinItems.length }}
              </span>
            </span>
          </button>
          <button
            @click="selectedFilter = 3"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              selectedFilter === 3
                ? 'border-teal text-teal'
                : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">description</span>
              Documents
              <span v-if="documentCount > 0" class="px-2 py-0.5 text-xs bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-300 rounded-full">
                {{ documentCount }}
              </span>
            </span>
          </button>
          <button
            @click="selectedFilter = 2"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              selectedFilter === 2
                ? 'border-teal text-teal'
                : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">folder</span>
              Folders
              <span v-if="folderCount > 0" class="px-2 py-0.5 text-xs bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-300 rounded-full">
                {{ folderCount }}
              </span>
            </span>
          </button>
          <button
            @click="selectedFilter = 1"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              selectedFilter === 1
                ? 'border-teal text-teal'
                : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">inventory_2</span>
              Cabinets
              <span v-if="cabinetCount > 0" class="px-2 py-0.5 text-xs bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-300 rounded-full">
                {{ cabinetCount }}
              </span>
            </span>
          </button>
        </nav>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <div class="animate-spin w-8 h-8 border-4 border-teal border-t-transparent rounded-full"></div>
      </div>

      <!-- Empty State -->
      <div v-else-if="displayedItems.length === 0" class="text-center py-12">
        <div class="w-20 h-20 rounded-lg bg-zinc-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-5xl text-zinc-400">delete_outline</span>
        </div>
        <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">Recycle bin is empty</h3>
        <p class="text-zinc-500 mt-1">Deleted items will appear here</p>
      </div>

      <!-- Recycle Bin Items -->
      <div v-else class="p-6">
        <div class="space-y-2">
          <div
            v-for="(item, index) in displayedItems"
            :key="item.id"
            class="flex items-center gap-4 p-4 bg-zinc-50 dark:bg-surface-dark rounded-lg border border-zinc-100 dark:border-border-dark hover:border-teal/30 hover:shadow-md transition-all"
          >
            <!-- Icon -->
            <DocumentIcon
              v-if="item.nodeType === 3"
              :extension="getExtension(item.nodeName)"
              :index="index"
              size="lg"
            />
            <DocumentIcon
              v-else
              :icon="getNodeIcon(item.nodeType)"
              :index="index"
              size="lg"
            />

            <!-- Info -->
            <div class="flex-1 min-w-0">
              <p class="font-medium text-zinc-900 dark:text-white truncate">{{ item.nodeName }}</p>
              <div class="flex items-center gap-3 mt-1 text-sm text-zinc-500">
                <span :class="[
                  'px-2 py-0.5 rounded-full text-xs font-medium',
                  item.nodeType === 1 ? 'bg-teal/15 text-teal' :
                  item.nodeType === 2 ? 'bg-zinc-200 dark:bg-border-dark text-zinc-600 dark:text-zinc-300' :
                  'bg-teal/15 text-teal'
                ]">
                  {{ getNodeTypeName(item.nodeType) }}
                </span>
                <span class="truncate">{{ item.originalPath || 'Unknown location' }}</span>
              </div>
            </div>

            <!-- Delete Info -->
            <div class="text-sm text-zinc-500 dark:text-zinc-400 flex-shrink-0 text-right mr-4">
              <div>{{ formatDate(item.deletedAt) }}</div>
              <div class="text-xs text-zinc-400">by {{ item.deletedByUserName || 'Unknown' }}</div>
            </div>

            <!-- Expiry Warning -->
            <div v-if="item.expiresAt" class="text-sm flex-shrink-0 mr-4">
              <div class="flex items-center gap-1 text-amber-600 dark:text-amber-400">
                <span class="material-symbols-outlined text-sm">schedule</span>
                <span class="text-xs">Expires {{ formatDate(item.expiresAt) }}</span>
              </div>
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-1 flex-shrink-0">
              <div class="relative group">
                <button
                  @click="restoreItem(item)"
                  class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">restore</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Restore
                </span>
              </div>
              <div class="relative group">
                <button
                  @click="permanentlyDelete(item)"
                  class="p-2 text-zinc-500 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">delete_forever</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Delete Permanently
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
