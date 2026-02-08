<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import type { MySharedItem, Document } from '@/types'
import { sharesApi, documentsApi } from '@/api/client'
import Modal from '@/components/ui/Modal.vue'
import DatePicker from '@/components/ui/DatePicker.vue'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'
import DocumentIcon from '@/components/common/DocumentIcon.vue'

const router = useRouter()
const mySharedItems = ref<MySharedItem[]>([])
const isLoading = ref(true)

// Document viewer
const showViewer = ref(false)
const viewerDocument = ref<Document | null>(null)

// Extend date modal
const showExtendModal = ref(false)
const selectedItem = ref<MySharedItem | null>(null)
const newExpiryDate = ref<string | null>(null)
const isExtending = ref(false)

// Toast notification
const toast = ref<{ show: boolean; message: string; type: 'success' | 'error' }>({
  show: false,
  message: '',
  type: 'success'
})

onMounted(async () => {
  await loadMySharedItems()
})

async function loadMySharedItems() {
  isLoading.value = true
  try {
    const response = await sharesApi.getMySharedItems()
    mySharedItems.value = response.data
  } catch (err) {
    showToast('Failed to load shared items', 'error')
  } finally {
    isLoading.value = false
  }
}

function viewDocument(item: MySharedItem) {
  router.push(`/documents/${item.documentId}`)
}

async function previewDocument(item: MySharedItem) {
  try {
    // Fetch document details for the viewer
    const response = await documentsApi.getById(item.documentId)
    viewerDocument.value = response.data
    showViewer.value = true
  } catch (err) {
    showToast('Failed to load document preview', 'error')
  }
}

function copyShareUrl(item: MySharedItem) {
  const shareUrl = `${window.location.origin}/shared/${item.shareId}`
  navigator.clipboard.writeText(shareUrl)
    .then(() => {
      showToast('Share link copied to clipboard')
    })
    .catch(() => {
      showToast('Failed to copy link', 'error')
    })
}

function openExtendModal(item: MySharedItem) {
  selectedItem.value = item
  newExpiryDate.value = item.expiresAt || null
  showExtendModal.value = true
}

async function extendExpiry() {
  if (!selectedItem.value) return

  isExtending.value = true
  try {
    await sharesApi.update(selectedItem.value.shareId, {
      permissionLevel: selectedItem.value.permissionLevel,
      expiresAt: newExpiryDate.value || undefined
    })

    // Update local data
    const index = mySharedItems.value.findIndex(i => i.shareId === selectedItem.value!.shareId)
    if (index !== -1) {
      mySharedItems.value[index].expiresAt = newExpiryDate.value || undefined
    }

    showExtendModal.value = false
    showToast('Expiry date updated successfully')
  } catch (err) {
    showToast('Failed to update expiry date', 'error')
  } finally {
    isExtending.value = false
  }
}

async function revokeShare(item: MySharedItem) {
  if (!confirm(`Revoke sharing for "${item.documentName}" with ${item.sharedWithUserName}?`)) return

  try {
    await sharesApi.revoke(item.shareId)
    mySharedItems.value = mySharedItems.value.filter(i => i.shareId !== item.shareId)
    showToast('Share revoked successfully')
  } catch (err) {
    showToast('Failed to revoke share', 'error')
  }
}

function showToast(message: string, type: 'success' | 'error' = 'success') {
  toast.value = { show: true, message, type }
  setTimeout(() => {
    toast.value.show = false
  }, 3000)
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

const minDate = computed(() => {
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  return tomorrow.toISOString().split('T')[0]
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-slate-100">My Shared Items</h1>
        <p class="text-slate-500 mt-1">Documents you have shared with others</p>
      </div>
      <button
        @click="loadMySharedItems"
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
        <span class="text-sm text-slate-500 dark:text-slate-400">Loading shared items...</span>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="mySharedItems.length === 0" class="bg-white dark:bg-slate-800 rounded-xl shadow-sm border border-slate-200 dark:border-slate-700 p-16 text-center">
      <div class="w-16 h-16 mx-auto bg-[#1f2937] rounded-xl flex items-center justify-center mb-4">
        <span class="material-symbols-outlined text-3xl text-slate-500">share</span>
      </div>
      <p class="text-slate-700 dark:text-slate-300 font-medium">No shared items</p>
      <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">Documents you share with others will appear here</p>
    </div>

    <!-- Shared Items List -->
    <div v-else class="bg-white dark:bg-slate-900 rounded-lg border border-slate-200 dark:border-slate-700 overflow-hidden">
      <!-- Table Header -->
      <div class="bg-white dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 px-4 py-3">
        <div class="grid grid-cols-12 gap-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">
          <div class="col-span-4">Document</div>
          <div class="col-span-2">Shared With</div>
          <div class="col-span-2">Permission</div>
          <div class="col-span-2">Expires</div>
          <div class="col-span-2 text-right">Actions</div>
        </div>
      </div>

      <!-- Items -->
      <div class="divide-y divide-slate-100 dark:divide-slate-800">
        <div
          v-for="item in mySharedItems"
          :key="item.shareId"
          class="px-4 py-3 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors group"
        >
          <div class="grid grid-cols-12 gap-4 items-center">
            <!-- Document -->
            <div class="col-span-4 flex items-center gap-3 min-w-0">
              <DocumentIcon :extension="item.extension" size="md" />
              <div class="min-w-0">
                <div class="flex items-center gap-2">
                  <p class="text-sm font-medium text-slate-700 dark:text-slate-200 truncate group-hover:text-teal transition-colors">
                    {{ item.documentName }}
                  </p>
                  <!-- Password Protected Badge -->
                  <div
                    v-if="item.hasPassword"
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
                <p class="text-xs text-slate-400 uppercase">{{ item.extension?.replace('.', '') || 'File' }}</p>
              </div>
            </div>

            <!-- Shared With -->
            <div class="col-span-2 flex items-center gap-2">
              <div class="w-6 h-6 bg-slate-100 dark:bg-slate-700 rounded-full flex items-center justify-center">
                <span class="material-symbols-outlined text-xs text-slate-500">person</span>
              </div>
              <span class="text-sm text-slate-600 dark:text-slate-300 truncate">{{ item.sharedWithUserName }}</span>
            </div>

            <!-- Permission -->
            <div class="col-span-2">
              <span :class="[
                'inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium',
                item.permissionLevel === 2
                  ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                  : 'bg-teal/10 text-teal'
              ]">
                <span class="material-symbols-outlined text-xs">
                  {{ item.permissionLevel === 2 ? 'edit' : 'visibility' }}
                </span>
                {{ getPermissionLabel(item.permissionLevel) }}
              </span>
            </div>

            <!-- Expires -->
            <div class="col-span-2">
              <div v-if="item.expiresAt" class="flex items-center gap-1 text-sm" :class="[
                isExpired(item.expiresAt)
                  ? 'text-red-500'
                  : isExpiringSoon(item.expiresAt)
                    ? 'text-amber-500'
                    : 'text-slate-500 dark:text-slate-400'
              ]">
                <span class="material-symbols-outlined text-sm">
                  {{ isExpired(item.expiresAt) ? 'error' : 'schedule' }}
                </span>
                <span>{{ formatDate(item.expiresAt) }}</span>
              </div>
              <span v-else class="text-sm text-slate-400">No expiry</span>
            </div>

            <!-- Actions -->
            <div class="col-span-2 flex items-center justify-end gap-0.5">
              <button
                @click="previewDocument(item)"
                class="tooltip-btn p-1.5 rounded text-slate-400 hover:text-teal hover:bg-teal/10 transition-colors"
                data-tooltip="Preview"
              >
                <span class="material-symbols-outlined text-lg">open_in_new</span>
              </button>
              <button
                @click="viewDocument(item)"
                class="tooltip-btn p-1.5 rounded text-slate-400 hover:text-teal hover:bg-teal/10 transition-colors"
                data-tooltip="Details"
              >
                <span class="material-symbols-outlined text-lg">info</span>
              </button>
              <button
                @click="copyShareUrl(item)"
                class="tooltip-btn p-1.5 rounded text-slate-400 hover:text-teal hover:bg-teal/10 transition-colors"
                data-tooltip="Copy Link"
              >
                <span class="material-symbols-outlined text-lg">link</span>
              </button>
              <button
                @click="openExtendModal(item)"
                class="tooltip-btn p-1.5 rounded text-slate-400 hover:text-teal hover:bg-teal/10 transition-colors"
                data-tooltip="Extend"
              >
                <span class="material-symbols-outlined text-lg">event</span>
              </button>
              <button
                @click="revokeShare(item)"
                class="tooltip-btn p-1.5 rounded text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                data-tooltip="Revoke"
              >
                <span class="material-symbols-outlined text-lg">link_off</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="bg-white dark:bg-slate-800 border-t border-slate-200 dark:border-slate-700 px-4 py-2.5 flex items-center justify-between">
        <span class="text-xs text-slate-500">
          <span class="font-medium text-slate-700 dark:text-slate-300">{{ mySharedItems.length }}</span>
          shared {{ mySharedItems.length === 1 ? 'item' : 'items' }}
        </span>
      </div>
    </div>

    <!-- Extend Date Modal -->
    <Modal v-model="showExtendModal" size="sm" :overflow-visible="true">
      <template #header>
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-xl flex items-center justify-center">
            <span class="material-symbols-outlined text-white text-xl">event</span>
          </div>
          <div>
            <h3 class="text-lg font-semibold text-white">Extend Share Expiry</h3>
            <p class="text-sm text-white/70">{{ selectedItem?.documentName }}</p>
          </div>
        </div>
      </template>

      <div class="space-y-5">
        <p class="text-sm text-gray-600 dark:text-slate-400">
          Set a new expiry date for sharing with
          <span class="font-semibold text-gray-900 dark:text-white">{{ selectedItem?.sharedWithUserName }}</span>
        </p>

        <DatePicker
          v-model="newExpiryDate"
          label="Expiry Date"
          placeholder="Select new expiry date"
          :min-date="minDate"
          :clearable="true"
        />

        <p class="text-xs text-gray-500 dark:text-slate-500">
          Leave empty to remove expiry (share will not expire)
        </p>
      </div>

      <template #footer>
        <div class="flex justify-end gap-3">
          <button
            @click="showExtendModal = false"
            class="px-4 py-2.5 text-sm font-medium text-gray-600 dark:text-slate-400 hover:bg-gray-100 dark:hover:bg-slate-700 rounded-xl transition-colors"
          >
            Cancel
          </button>
          <button
            @click="extendExpiry"
            :disabled="isExtending"
            class="px-5 py-2.5 text-sm font-medium text-white bg-gradient-to-r from-navy to-primary hover:shadow-lg hover:shadow-primary/25 rounded-xl transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            <span v-if="isExtending" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <span>{{ isExtending ? 'Updating...' : 'Update Expiry' }}</span>
          </button>
        </div>
      </template>
    </Modal>

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

<style scoped>
.border-3 {
  border-width: 3px;
}

/* Tooltip Styles */
.tooltip-btn {
  position: relative;
}

.tooltip-btn::before {
  content: attr(data-tooltip);
  position: absolute;
  bottom: calc(100% + 6px);
  left: 50%;
  transform: translateX(-50%) translateY(4px);
  padding: 4px 8px;
  background: #1f2937;
  color: white;
  font-size: 11px;
  font-weight: 500;
  white-space: nowrap;
  border-radius: 4px;
  opacity: 0;
  visibility: hidden;
  transition: all 0.15s ease;
  pointer-events: none;
  z-index: 50;
}

.tooltip-btn::after {
  content: '';
  position: absolute;
  bottom: calc(100% + 2px);
  left: 50%;
  transform: translateX(-50%) translateY(4px);
  border: 4px solid transparent;
  border-top-color: #1f2937;
  opacity: 0;
  visibility: hidden;
  transition: all 0.15s ease;
  pointer-events: none;
  z-index: 50;
}

.tooltip-btn:hover::before,
.tooltip-btn:hover::after {
  opacity: 1;
  visibility: visible;
  transform: translateX(-50%) translateY(0);
}
</style>
