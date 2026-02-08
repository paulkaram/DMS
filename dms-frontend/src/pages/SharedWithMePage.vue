<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import type { SharedDocument, Document } from '@/types'
import { sharesApi, documentsApi } from '@/api/client'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'

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
  // Navigate to document page in edit mode
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
  contextMenuPosition.value = { x: event.clientX, y: event.clientY }
  showContextMenu.value = true
}

function closeContextMenu() {
  showContextMenu.value = false
  contextMenuDocument.value = null
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
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
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-slate-100">Shared With Me</h1>
        <p class="text-slate-500 mt-1">Documents that others have shared with you</p>
      </div>
      <button
        @click="loadSharedWithMe"
        class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-xl font-medium text-sm transition-colors border border-slate-200 dark:border-slate-700"
      >
        <span class="material-symbols-outlined text-lg">refresh</span>
        Refresh
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-16">
      <div class="flex flex-col items-center gap-3">
        <div class="w-10 h-10 border-3 border-teal border-t-transparent rounded-full animate-spin"></div>
        <span class="text-sm text-slate-500 dark:text-slate-400">Loading shared documents...</span>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="sharedDocuments.length === 0" class="bg-white dark:bg-slate-800 rounded-xl shadow-sm border border-slate-200 dark:border-slate-700 p-16 text-center">
      <div class="w-16 h-16 mx-auto bg-[#1f2937] rounded-xl flex items-center justify-center mb-4">
        <span class="material-symbols-outlined text-3xl text-slate-500">folder_shared</span>
      </div>
      <p class="text-slate-700 dark:text-slate-300 font-medium">No shared documents</p>
      <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">Documents shared with you will appear here</p>
    </div>

    <!-- Shared Documents List -->
    <div v-else class="bg-white dark:bg-slate-900 rounded-lg border border-slate-200 dark:border-slate-700 overflow-hidden">
      <!-- Table Header -->
      <div class="bg-white dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 px-4 py-3">
        <div class="grid grid-cols-12 gap-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">
          <div class="col-span-4">Document</div>
          <div class="col-span-2">Shared By</div>
          <div class="col-span-2">Permission</div>
          <div class="col-span-2">Received</div>
          <div class="col-span-2 text-right">Actions</div>
        </div>
      </div>

      <!-- Items -->
      <div class="divide-y divide-slate-100 dark:divide-slate-800">
        <div
          v-for="doc in sharedDocuments"
          :key="doc.shareId"
          class="px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors group cursor-pointer"
          :class="{ 'opacity-60': isExpired(doc.expiresAt) }"
          @click="viewDocument(doc)"
          @contextmenu="openContextMenu($event, doc)"
        >
          <div class="grid grid-cols-12 gap-4 items-center">
            <!-- Document -->
            <div class="col-span-4 flex items-center gap-3 min-w-0">
              <div class="w-10 h-10 bg-[#1e3a5f] rounded-lg flex items-center justify-center flex-shrink-0">
                <span class="material-symbols-outlined text-lg text-white">description</span>
              </div>
              <div class="min-w-0">
                <div class="flex items-center gap-2">
                  <p class="text-sm font-medium text-slate-700 dark:text-slate-200 truncate group-hover:text-teal transition-colors">
                    {{ doc.documentName }}
                  </p>
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
                </div>
                <p class="text-xs text-slate-400 uppercase">{{ doc.extension?.replace('.', '') || 'File' }}</p>
              </div>
            </div>

            <!-- Shared By -->
            <div class="col-span-2 flex items-center gap-2">
              <div class="w-6 h-6 bg-slate-100 dark:bg-slate-700 rounded-full flex items-center justify-center">
                <span class="material-symbols-outlined text-xs text-slate-500">person</span>
              </div>
              <span class="text-sm text-slate-600 dark:text-slate-300 truncate">{{ doc.sharedByUserName || 'Unknown' }}</span>
            </div>

            <!-- Permission -->
            <div class="col-span-2">
              <div class="flex items-center gap-2">
                <span :class="[
                  'inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium',
                  doc.permissionLevel === 2
                    ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                    : 'bg-teal/10 text-teal'
                ]">
                  <span class="material-symbols-outlined text-xs">
                    {{ doc.permissionLevel === 2 ? 'edit' : 'visibility' }}
                  </span>
                  {{ getPermissionLabel(doc.permissionLevel) }}
                </span>
                <!-- Expiry Badge -->
                <span v-if="isExpired(doc.expiresAt)" class="px-1.5 py-0.5 rounded text-[10px] font-semibold uppercase bg-red-100 dark:bg-red-900/40 text-red-600 dark:text-red-400">
                  Expired
                </span>
                <span v-else-if="isExpiringSoon(doc.expiresAt)" class="px-1.5 py-0.5 rounded text-[10px] font-semibold uppercase bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400">
                  Expiring
                </span>
              </div>
            </div>

            <!-- Received Date -->
            <div class="col-span-2">
              <span class="text-sm text-slate-500 dark:text-slate-400">{{ getRelativeTime(doc.sharedAt) }}</span>
            </div>

            <!-- Actions -->
            <div class="col-span-2 flex items-center justify-end gap-0.5">
              <button
                @click.stop="previewDocument(doc)"
                :disabled="isExpired(doc.expiresAt)"
                class="action-btn p-1.5 rounded text-slate-400 hover:text-teal hover:bg-teal/10 transition-colors disabled:opacity-40"
                data-tooltip="Preview"
              >
                <span class="material-symbols-outlined text-lg">open_in_new</span>
              </button>
              <button
                @click.stop="downloadDocument(doc)"
                :disabled="isExpired(doc.expiresAt)"
                class="action-btn p-1.5 rounded text-slate-400 hover:text-teal hover:bg-teal/10 transition-colors disabled:opacity-40"
                data-tooltip="Download"
              >
                <span class="material-symbols-outlined text-lg">download</span>
              </button>
              <button
                v-if="doc.permissionLevel === 2"
                @click.stop="editDocument(doc)"
                :disabled="isExpired(doc.expiresAt)"
                class="action-btn p-1.5 rounded text-slate-400 hover:text-amber-500 hover:bg-amber-500/10 transition-colors disabled:opacity-40"
                data-tooltip="Edit"
              >
                <span class="material-symbols-outlined text-lg">edit</span>
              </button>
              <button
                @click.stop="openContextMenu($event, doc)"
                class="action-btn p-1.5 rounded text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                data-tooltip="More"
              >
                <span class="material-symbols-outlined text-lg">more_vert</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="bg-white dark:bg-slate-800 border-t border-slate-200 dark:border-slate-700 px-4 py-2.5 flex items-center justify-between">
        <span class="text-xs text-slate-500">
          <span class="font-medium text-slate-700 dark:text-slate-300">{{ sharedDocuments.length }}</span>
          shared {{ sharedDocuments.length === 1 ? 'document' : 'documents' }}
        </span>
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
          class="fixed z-50 min-w-[180px] bg-white dark:bg-slate-800 rounded-xl shadow-xl border border-slate-200 dark:border-slate-700 py-1.5 overflow-hidden"
          :style="{ left: contextMenuPosition.x + 'px', top: contextMenuPosition.y + 'px' }"
          @click.stop
        >
          <!-- Preview -->
          <button
            @click="previewDocument(contextMenuDocument); closeContextMenu()"
            :disabled="isExpired(contextMenuDocument.expiresAt)"
            class="w-full px-4 py-2.5 text-left text-sm text-slate-700 dark:text-slate-200 hover:bg-teal/10 hover:text-teal flex items-center gap-3 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <span class="material-symbols-outlined text-lg">open_in_new</span>
            Preview
          </button>

          <!-- Download -->
          <button
            @click="downloadDocument(contextMenuDocument); closeContextMenu()"
            :disabled="isExpired(contextMenuDocument.expiresAt)"
            class="w-full px-4 py-2.5 text-left text-sm text-slate-700 dark:text-slate-200 hover:bg-teal/10 hover:text-teal flex items-center gap-3 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <span class="material-symbols-outlined text-lg">download</span>
            Download
          </button>

          <!-- Edit (only if permission level is 2) -->
          <button
            v-if="contextMenuDocument.permissionLevel === 2"
            @click="editDocument(contextMenuDocument); closeContextMenu()"
            :disabled="isExpired(contextMenuDocument.expiresAt)"
            class="w-full px-4 py-2.5 text-left text-sm text-slate-700 dark:text-slate-200 hover:bg-amber-500/10 hover:text-amber-600 flex items-center gap-3 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
          >
            <span class="material-symbols-outlined text-lg">edit</span>
            Edit
          </button>

          <div class="my-1.5 border-t border-slate-200 dark:border-slate-700"></div>

          <!-- View Details -->
          <button
            @click="viewDocument(contextMenuDocument); closeContextMenu()"
            class="w-full px-4 py-2.5 text-left text-sm text-slate-700 dark:text-slate-200 hover:bg-teal/10 hover:text-teal flex items-center gap-3 transition-colors"
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
        class="fixed bottom-6 right-6 z-50 flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg"
        :class="toast.type === 'success'
          ? 'bg-emerald-600 text-white'
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

<style scoped>
.border-3 {
  border-width: 3px;
}

/* Modern Tooltip Styles */
.action-btn {
  position: relative;
}

.action-btn::before,
.action-btn::after {
  position: absolute;
  opacity: 0;
  visibility: hidden;
  transition: all 0.2s ease;
  pointer-events: none;
}

/* Tooltip text */
.action-btn::before {
  content: attr(data-tooltip);
  bottom: calc(100% + 8px);
  left: 50%;
  transform: translateX(-50%) translateY(4px);
  padding: 6px 12px;
  background: linear-gradient(135deg, #1e293b 0%, #334155 100%);
  color: white;
  font-size: 12px;
  font-weight: 500;
  white-space: nowrap;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15), 0 0 0 1px rgba(255, 255, 255, 0.05) inset;
  z-index: 50;
}

/* Tooltip arrow */
.action-btn::after {
  content: '';
  bottom: calc(100% + 2px);
  left: 50%;
  transform: translateX(-50%) translateY(4px);
  border: 6px solid transparent;
  border-top-color: #334155;
  z-index: 50;
}

/* Show on hover */
.action-btn:hover::before,
.action-btn:hover::after {
  opacity: 1;
  visibility: visible;
  transform: translateX(-50%) translateY(0);
}

/* Dark mode tooltip */
:deep(.dark) .action-btn::before {
  background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%);
  color: #1e293b;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3), 0 0 0 1px rgba(255, 255, 255, 0.1) inset;
}

:deep(.dark) .action-btn::after {
  border-top-color: #e2e8f0;
}

/* Disabled tooltip should still show */
.action-btn:disabled:hover::before,
.action-btn:disabled:hover::after {
  opacity: 1;
  visibility: visible;
}
</style>
