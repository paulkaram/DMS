<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import type { SharedDocument, Document } from '@/types'
import { sharesApi, documentsApi } from '@/api/client'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'
import DocumentIcon from '@/components/common/DocumentIcon.vue'

const router = useRouter()
const sharedDocuments = ref<SharedDocument[]>([])
const isLoading = ref(true)

// Document viewer
const showViewer = ref(false)
const viewerDocument = ref<Document | null>(null)

// Context menu state
const showContextMenu = ref(false)
const contextMenuPosition = ref({ x: 0, y: 0 })
const contextMenuDocument = ref<SharedDocument | null>(null)

// Toast notification
const toast = ref<{ show: boolean; message: string; type: 'success' | 'error' }>({
  show: false,
  message: '',
  type: 'success'
})

onMounted(async () => {
  await loadSharedWithMe()
  document.addEventListener('click', closeContextMenu)
})

onUnmounted(() => {
  document.removeEventListener('click', closeContextMenu)
})

async function loadSharedWithMe() {
  isLoading.value = true
  try {
    const response = await sharesApi.getSharedWithMe()
    sharedDocuments.value = response.data
  } catch (err) {
    showToast('Failed to load shared documents', 'error')
  } finally {
    isLoading.value = false
  }
}

async function previewDocument(doc: SharedDocument) {
  try {
    const response = await documentsApi.getById(doc.documentId)
    viewerDocument.value = response.data
    showViewer.value = true
  } catch (err) {
    showToast('Failed to load document preview', 'error')
  }
}

function viewDocument(doc: SharedDocument) {
  router.push(`/documents/${doc.documentId}`)
}

async function downloadDocument(doc: SharedDocument) {
  try {
    const response = await documentsApi.download(doc.documentId)
    const blob = new Blob([response.data])
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = doc.documentName + (doc.extension || '')
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
    showToast('Download started')
  } catch (err) {
    showToast('Failed to download document', 'error')
  }
}

function editDocument(doc: SharedDocument) {
  router.push(`/documents/${doc.documentId}?edit=true`)
}

function showToast(message: string, type: 'success' | 'error' = 'success') {
  toast.value = { show: true, message, type }
  setTimeout(() => {
    toast.value.show = false
  }, 3000)
}

function openContextMenu(event: MouseEvent, doc: SharedDocument) {
  event.preventDefault()
  event.stopPropagation()
  contextMenuDocument.value = doc

  const menuWidth = 200
  const menuHeight = 200
  const x = Math.min(event.clientX, window.innerWidth - menuWidth)
  const y = Math.min(event.clientY, window.innerHeight - menuHeight)
  contextMenuPosition.value = { x, y }
  showContextMenu.value = true
}

function closeContextMenu() {
  showContextMenu.value = false
  contextMenuDocument.value = null
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

function getPermissionLabel(level: number): string {
  switch (level) {
    case 1: return 'View'
    case 2: return 'Edit'
    default: return 'Unknown'
  }
}

function isExpiringSoon(dateStr?: string): boolean {
  if (!dateStr) return false
  const expiry = new Date(dateStr)
  const now = new Date()
  const diff = expiry.getTime() - now.getTime()
  const days = diff / (1000 * 60 * 60 * 24)
  return days <= 7 && days > 0
}

function isExpired(dateStr?: string): boolean {
  if (!dateStr) return false
  return new Date(dateStr) < new Date()
}

function getRelativeTime(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diff = now.getTime() - date.getTime()
  const days = Math.floor(diff / (1000 * 60 * 60 * 24))

  if (days === 0) return 'Today'
  if (days === 1) return 'Yesterday'
  if (days < 7) return `${days} days ago`
  if (days < 30) return `${Math.floor(days / 7)} weeks ago`
  return formatDate(dateStr)
}

// Stats
const viewOnlyCount = computed(() => sharedDocuments.value.filter(d => d.permissionLevel === 1).length)
const editableCount = computed(() => sharedDocuments.value.filter(d => d.permissionLevel === 2).length)
const expiredCount = computed(() => sharedDocuments.value.filter(d => isExpired(d.expiresAt)).length)

// Filter
const selectedFilter = ref<string | null>(null)
const displayedItems = computed(() => {
  if (selectedFilter.value === null) return sharedDocuments.value
  if (selectedFilter.value === 'view') return sharedDocuments.value.filter(d => d.permissionLevel === 1)
  if (selectedFilter.value === 'edit') return sharedDocuments.value.filter(d => d.permissionLevel === 2)
  if (selectedFilter.value === 'expired') return sharedDocuments.value.filter(d => isExpired(d.expiresAt))
  return sharedDocuments.value
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Shared With Me</h1>
        <p class="text-zinc-500 mt-1">Documents that others have shared with you</p>
      </div>
      <button
        @click="loadSharedWithMe"
        class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-surface-dark hover:bg-zinc-50 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark"
      >
        <span class="material-symbols-outlined text-lg">refresh</span>
        Refresh
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">visibility</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">View Only</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ viewOnlyCount }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Read-only documents</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">edit</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Editable</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ editableCount }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Edit permission documents</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">timer_off</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Expired</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ expiredCount }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Expired shares</p>
        </div>
      </div>
    </div>

    <!-- Filter Tabs + Content -->
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
              <span v-if="sharedDocuments.length > 0" class="px-2 py-0.5 text-xs bg-teal/15 text-teal rounded-full">
                {{ sharedDocuments.length }}
              </span>
            </span>
          </button>
          <button
            @click="selectedFilter = 'view'"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              selectedFilter === 'view'
                ? 'border-teal text-teal'
                : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">visibility</span>
              View Only
              <span v-if="viewOnlyCount > 0" class="px-2 py-0.5 text-xs bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-300 rounded-full">
                {{ viewOnlyCount }}
              </span>
            </span>
          </button>
          <button
            @click="selectedFilter = 'edit'"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              selectedFilter === 'edit'
                ? 'border-teal text-teal'
                : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">edit</span>
              Editable
              <span v-if="editableCount > 0" class="px-2 py-0.5 text-xs bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-300 rounded-full">
                {{ editableCount }}
              </span>
            </span>
          </button>
          <button
            @click="selectedFilter = 'expired'"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              selectedFilter === 'expired'
                ? 'border-teal text-teal'
                : 'border-transparent text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">timer_off</span>
              Expired
              <span v-if="expiredCount > 0" class="px-2 py-0.5 text-xs bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-300 rounded-full">
                {{ expiredCount }}
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
          <span class="material-symbols-outlined text-5xl text-zinc-400">folder_shared</span>
        </div>
        <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">No shared documents</h3>
        <p class="text-zinc-500 mt-1">Documents shared with you will appear here</p>
      </div>

      <!-- Shared Documents -->
      <div v-else class="p-6">
        <div class="space-y-2">
          <div
            v-for="(doc, index) in displayedItems"
            :key="doc.shareId"
            class="flex items-center gap-4 p-4 bg-zinc-50 dark:bg-surface-dark rounded-lg border border-zinc-100 dark:border-border-dark hover:border-teal/30 hover:shadow-md transition-all cursor-pointer"
            :class="{ 'opacity-60': isExpired(doc.expiresAt) }"
            @click="viewDocument(doc)"
            @contextmenu="openContextMenu($event, doc)"
          >
            <!-- Document Icon -->
            <DocumentIcon :extension="doc.extension" :index="index" size="lg" />

            <!-- Info -->
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <p class="font-medium text-zinc-900 dark:text-white truncate">{{ doc.documentName }}</p>
                <!-- Password Protected Badge -->
                <div
                  v-if="doc.hasPassword"
                  class="inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full bg-gradient-to-r from-violet-500/15 to-fuchsia-500/15 border border-violet-300/30 dark:border-violet-500/30 shrink-0"
                  title="Password protected"
                >
                  <span
                    class="material-symbols-outlined text-violet-500 dark:text-violet-400"
                    style="font-size: 12px; font-variation-settings: 'FILL' 1;"
                  >shield_lock</span>
                  <span class="text-[9px] font-semibold text-violet-600 dark:text-violet-400 uppercase tracking-wide pr-0.5">Secured</span>
                </div>
                <!-- Expiry Badges -->
                <span v-if="isExpired(doc.expiresAt)" class="px-1.5 py-0.5 rounded-full text-[10px] font-semibold uppercase bg-red-100 dark:bg-red-900/40 text-red-600 dark:text-red-400 shrink-0">
                  Expired
                </span>
                <span v-else-if="isExpiringSoon(doc.expiresAt)" class="px-1.5 py-0.5 rounded-full text-[10px] font-semibold uppercase bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 shrink-0">
                  Expiring
                </span>
              </div>
              <div class="flex items-center gap-3 mt-1 text-sm text-zinc-500">
                <span :class="[
                  'px-2 py-0.5 rounded-full text-xs font-medium',
                  doc.permissionLevel === 2
                    ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                    : 'bg-teal/15 text-teal'
                ]">
                  {{ getPermissionLabel(doc.permissionLevel) }}
                </span>
                <span class="truncate flex items-center gap-1.5">
                  <span class="material-symbols-outlined text-sm text-zinc-400">person</span>
                  {{ doc.sharedByUserName || 'Unknown' }}
                </span>
                <span class="text-zinc-400">{{ getRelativeTime(doc.sharedAt) }}</span>
              </div>
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-1 flex-shrink-0">
              <div class="relative group">
                <button
                  @click.stop="previewDocument(doc)"
                  :disabled="isExpired(doc.expiresAt)"
                  class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors disabled:opacity-40"
                >
                  <span class="material-symbols-outlined text-xl">open_in_new</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Preview
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="downloadDocument(doc)"
                  :disabled="isExpired(doc.expiresAt)"
                  class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors disabled:opacity-40"
                >
                  <span class="material-symbols-outlined text-xl">download</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Download
                </span>
              </div>
              <div v-if="doc.permissionLevel === 2" class="relative group">
                <button
                  @click.stop="editDocument(doc)"
                  :disabled="isExpired(doc.expiresAt)"
                  class="p-2 text-zinc-500 hover:text-amber-500 hover:bg-amber-500/10 rounded-lg transition-colors disabled:opacity-40"
                >
                  <span class="material-symbols-outlined text-xl">edit</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Edit
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="openContextMenu($event, doc)"
                  class="p-2 text-zinc-500 hover:text-zinc-600 dark:hover:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">more_vert</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  More
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Context Menu -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-100 ease-out"
        enter-from-class="transform scale-95 opacity-0"
        enter-to-class="transform scale-100 opacity-100"
        leave-active-class="transition duration-75 ease-in"
        leave-from-class="transform scale-100 opacity-100"
        leave-to-class="transform scale-95 opacity-0"
      >
        <div
          v-if="showContextMenu && contextMenuDocument"
          class="fixed z-50 min-w-[180px] bg-white dark:bg-surface-dark rounded-lg shadow-xl border border-zinc-200 dark:border-border-dark py-1.5 overflow-hidden"
          :style="{ left: contextMenuPosition.x + 'px', top: contextMenuPosition.y + 'px' }"
          @click.stop
        >
          <button
            @click="previewDocument(contextMenuDocument); closeContextMenu()"
            :disabled="isExpired(contextMenuDocument.expiresAt)"
            class="w-full px-4 py-2.5 text-left text-sm text-zinc-700 dark:text-zinc-200 hover:bg-teal/10 hover:text-teal flex items-center gap-3 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <span class="material-symbols-outlined text-lg">open_in_new</span>
            Preview
          </button>
          <button
            @click="downloadDocument(contextMenuDocument); closeContextMenu()"
            :disabled="isExpired(contextMenuDocument.expiresAt)"
            class="w-full px-4 py-2.5 text-left text-sm text-zinc-700 dark:text-zinc-200 hover:bg-teal/10 hover:text-teal flex items-center gap-3 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <span class="material-symbols-outlined text-lg">download</span>
            Download
          </button>
          <button
            v-if="contextMenuDocument.permissionLevel === 2"
            @click="editDocument(contextMenuDocument); closeContextMenu()"
            :disabled="isExpired(contextMenuDocument.expiresAt)"
            class="w-full px-4 py-2.5 text-left text-sm text-zinc-700 dark:text-zinc-200 hover:bg-amber-500/10 hover:text-amber-600 flex items-center gap-3 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <span class="material-symbols-outlined text-lg">edit</span>
            Edit
          </button>
          <div class="my-1.5 border-t border-zinc-200 dark:border-border-dark"></div>
          <button
            @click="viewDocument(contextMenuDocument); closeContextMenu()"
            class="w-full px-4 py-2.5 text-left text-sm text-zinc-700 dark:text-zinc-200 hover:bg-teal/10 hover:text-teal flex items-center gap-3 transition-colors"
          >
            <span class="material-symbols-outlined text-lg">info</span>
            View Details
          </button>
        </div>
      </Transition>
    </Teleport>

    <!-- Toast Notification -->
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="transform translate-y-2 opacity-0"
      enter-to-class="transform translate-y-0 opacity-100"
      leave-active-class="transition duration-150 ease-in"
      leave-from-class="transform translate-y-0 opacity-100"
      leave-to-class="transform translate-y-2 opacity-0"
    >
      <div
        v-if="toast.show"
        class="fixed bottom-6 right-6 z-50 flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg"
        :class="toast.type === 'success'
          ? 'bg-teal text-white'
          : 'bg-red-600 text-white'"
      >
        <span class="material-symbols-outlined text-xl">
          {{ toast.type === 'success' ? 'check_circle' : 'error' }}
        </span>
        <span class="text-sm font-medium">{{ toast.message }}</span>
      </div>
    </Transition>

    <!-- Document Viewer -->
    <DocumentViewer
      v-model="showViewer"
      :document="viewerDocument"
    />
  </div>
</template>
