<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import type { MySharedItem, Document } from '@/types'
import { sharesApi, documentsApi } from '@/api/client'
import { UiModal, UiButton } from '@/components/ui'
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

// Stats
const viewOnlyCount = computed(() => mySharedItems.value.filter(i => i.permissionLevel === 1).length)
const editableCount = computed(() => mySharedItems.value.filter(i => i.permissionLevel === 2).length)
const expiredCount = computed(() => mySharedItems.value.filter(i => isExpired(i.expiresAt)).length)

// Filter
const selectedFilter = ref<string | null>(null)
const displayedItems = computed(() => {
  if (selectedFilter.value === null) return mySharedItems.value
  if (selectedFilter.value === 'view') return mySharedItems.value.filter(i => i.permissionLevel === 1)
  if (selectedFilter.value === 'edit') return mySharedItems.value.filter(i => i.permissionLevel === 2)
  if (selectedFilter.value === 'expired') return mySharedItems.value.filter(i => isExpired(i.expiresAt))
  return mySharedItems.value
})
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">My Shared Items</h1>
        <p class="text-zinc-500 mt-1">Documents you have shared with others</p>
      </div>
      <button
        @click="loadMySharedItems"
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
          <p class="text-[10px] text-teal mt-2 font-medium">Read-only shares</p>
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
          <p class="text-[10px] text-teal mt-2 font-medium">Edit permission shares</p>
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
              <span v-if="mySharedItems.length > 0" class="px-2 py-0.5 text-xs bg-teal/15 text-teal rounded-full">
                {{ mySharedItems.length }}
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
          <span class="material-symbols-outlined text-5xl text-zinc-400">share</span>
        </div>
        <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">No shared items</h3>
        <p class="text-zinc-500 mt-1">Documents you share with others will appear here</p>
      </div>

      <!-- Shared Items -->
      <div v-else class="p-6">
        <div class="space-y-2">
          <div
            v-for="(item, index) in displayedItems"
            :key="item.shareId"
            class="flex items-center gap-4 p-4 bg-zinc-50 dark:bg-surface-dark rounded-lg border border-zinc-100 dark:border-border-dark hover:border-teal/30 hover:shadow-md transition-all"
            :class="{ 'opacity-60': isExpired(item.expiresAt) }"
          >
            <!-- Document Icon -->
            <DocumentIcon :extension="item.extension" :index="index" size="lg" />

            <!-- Info -->
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <p class="font-medium text-zinc-900 dark:text-white truncate">{{ item.documentName }}</p>
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
              <div class="flex items-center gap-3 mt-1 text-sm text-zinc-500">
                <span :class="[
                  'px-2 py-0.5 rounded-full text-xs font-medium',
                  item.permissionLevel === 2
                    ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                    : 'bg-teal/15 text-teal'
                ]">
                  {{ getPermissionLabel(item.permissionLevel) }}
                </span>
                <span class="truncate flex items-center gap-1.5">
                  <span class="material-symbols-outlined text-sm text-zinc-400">person</span>
                  {{ item.sharedWithUserName }}
                </span>
              </div>
            </div>

            <!-- Expiry Info -->
            <div class="text-sm flex-shrink-0 text-right mr-4">
              <div v-if="item.expiresAt" :class="[
                isExpired(item.expiresAt)
                  ? 'text-red-500'
                  : isExpiringSoon(item.expiresAt)
                    ? 'text-amber-500'
                    : 'text-zinc-500 dark:text-zinc-400'
              ]">
                <div class="flex items-center gap-1 justify-end">
                  <span class="material-symbols-outlined text-sm">
                    {{ isExpired(item.expiresAt) ? 'error' : 'schedule' }}
                  </span>
                  <span>{{ formatDate(item.expiresAt) }}</span>
                </div>
                <span v-if="isExpired(item.expiresAt)" class="text-[10px] font-semibold uppercase">Expired</span>
                <span v-else-if="isExpiringSoon(item.expiresAt)" class="text-[10px] font-semibold uppercase">Expiring soon</span>
              </div>
              <span v-else class="text-xs text-zinc-400">No expiry</span>
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-1 flex-shrink-0">
              <div class="relative group">
                <button
                  @click="previewDocument(item)"
                  class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">open_in_new</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Preview
                </span>
              </div>
              <div class="relative group">
                <button
                  @click="viewDocument(item)"
                  class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">info</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Details
                </span>
              </div>
              <div class="relative group">
                <button
                  @click="copyShareUrl(item)"
                  class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">link</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Copy Link
                </span>
              </div>
              <div class="relative group">
                <button
                  @click="openExtendModal(item)"
                  class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">event</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Extend
                </span>
              </div>
              <div class="relative group">
                <button
                  @click="revokeShare(item)"
                  class="p-2 text-zinc-500 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">link_off</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Revoke
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Extend Date Modal -->
    <UiModal v-model="showExtendModal" size="sm" :overflow-visible="true">
      <template #header>
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-lg flex items-center justify-center">
            <span class="material-symbols-outlined text-white text-xl">event</span>
          </div>
          <div>
            <h3 class="text-lg font-semibold text-white">Extend Share Expiry</h3>
            <p class="text-sm text-white/70">{{ selectedItem?.documentName }}</p>
          </div>
        </div>
      </template>

      <div class="space-y-5">
        <p class="text-sm text-zinc-600 dark:text-zinc-400">
          Set a new expiry date for sharing with
          <span class="font-semibold text-zinc-900 dark:text-white">{{ selectedItem?.sharedWithUserName }}</span>
        </p>

        <DatePicker
          v-model="newExpiryDate"
          label="Expiry Date"
          placeholder="Select new expiry date"
          :min-date="minDate"
          :clearable="true"
        />

        <p class="text-xs text-zinc-500 dark:text-zinc-500">
          Leave empty to remove expiry (share will not expire)
        </p>
      </div>

      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showExtendModal = false">Cancel</UiButton>
          <UiButton @click="extendExpiry" :disabled="isExtending">
            <span v-if="isExtending" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            {{ isExtending ? 'Updating...' : 'Update Expiry' }}
          </UiButton>
        </div>
      </template>
    </UiModal>

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
