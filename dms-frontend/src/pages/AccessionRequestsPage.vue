<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { accessionsApi } from '@/api/client'
import type { AccessionRequest } from '@/types'

const router = useRouter()

// Data
const requests = ref<AccessionRequest[]>([])
const loading = ref(true)
const expandedId = ref<string | null>(null)
const actionLoading = ref<string | null>(null)
const actionError = ref('')

// Detail data for expanded request
const detailRequest = ref<AccessionRequest | null>(null)
const detailItems = ref<any[]>([])
const detailLoading = ref(false)

// Create Request modal
const showCreateModal = ref(false)
const createForm = ref({
  sourceStructureId: '',
  targetLocationId: '',
  description: '',
  notes: ''
})
const isCreating = ref(false)
const createError = ref('')

// Add Item modal
const showAddItemModal = ref(false)
const addItemForm = ref({
  documentId: '',
  title: '',
  description: ''
})
const isAddingItem = ref(false)
const addItemError = ref('')

// Reject modal
const showRejectModal = ref(false)
const rejectReason = ref('')
const rejectTargetId = ref('')
const isRejecting = ref(false)

// Notes modal for review/accept
const showNotesModal = ref(false)
const notesModalAction = ref<'review' | 'accept'>('review')
const notesModalNotes = ref('')
const notesModalTargetId = ref('')
const isSubmittingNotes = ref(false)

// Status badge classes
const statusBadge: Record<string, string> = {
  Draft: 'bg-zinc-100 text-zinc-700 dark:bg-zinc-700 dark:text-zinc-300',
  Submitted: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  UnderReview: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  Accepted: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  PartiallyAccepted: 'bg-teal/15 text-teal dark:bg-teal/20 dark:text-teal',
  Rejected: 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400',
  Transferred: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400'
}

// Stats
const stats = computed(() => ({
  total: requests.value.length,
  pending: requests.value.filter(r => r.status === 'Submitted' || r.status === 'UnderReview').length,
  accepted: requests.value.filter(r => r.status === 'Accepted' || r.status === 'PartiallyAccepted').length,
  transferred: requests.value.filter(r => r.status === 'Transferred').length
}))

// Load all requests
async function loadRequests() {
  loading.value = true
  try {
    const { data } = await accessionsApi.getAll()
    requests.value = data.data ?? data ?? []
  } catch {
    /* empty */
  }
  loading.value = false
}

// Load detail for a request
async function loadDetail(id: string) {
  detailLoading.value = true
  detailItems.value = []
  try {
    const { data } = await accessionsApi.getById(id)
    detailRequest.value = data
    detailItems.value = data.items ?? []
  } catch {
    /* empty */
  }
  detailLoading.value = false
}

// Toggle expanded request
function toggleExpand(id: string) {
  if (expandedId.value === id) {
    expandedId.value = null
    detailRequest.value = null
    detailItems.value = []
  } else {
    expandedId.value = id
    loadDetail(id)
  }
}

// Format date
function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
}

// Status icon
function getStatusIcon(status: string): string {
  switch (status) {
    case 'Draft': return 'edit_note'
    case 'Submitted': return 'send'
    case 'UnderReview': return 'rate_review'
    case 'Accepted': return 'check_circle'
    case 'PartiallyAccepted': return 'rule'
    case 'Rejected': return 'cancel'
    case 'Transferred': return 'move_to_inbox'
    default: return 'help'
  }
}

// ---- Actions ----

async function handleSubmit(id: string) {
  actionLoading.value = id
  actionError.value = ''
  try {
    await accessionsApi.submit(id)
    await loadRequests()
    if (expandedId.value === id) await loadDetail(id)
  } catch (err: any) {
    actionError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Action failed'
  }
  actionLoading.value = null
}

function openReviewNotes(id: string) {
  notesModalTargetId.value = id
  notesModalAction.value = 'review'
  notesModalNotes.value = ''
  showNotesModal.value = true
}

function openAcceptNotes(id: string) {
  notesModalTargetId.value = id
  notesModalAction.value = 'accept'
  notesModalNotes.value = ''
  showNotesModal.value = true
}

async function submitNotesAction() {
  isSubmittingNotes.value = true
  actionError.value = ''
  try {
    if (notesModalAction.value === 'review') {
      await accessionsApi.review(notesModalTargetId.value, { notes: notesModalNotes.value || undefined })
    } else {
      await accessionsApi.accept(notesModalTargetId.value, { notes: notesModalNotes.value || undefined })
    }
    showNotesModal.value = false
    await loadRequests()
    if (expandedId.value === notesModalTargetId.value) await loadDetail(notesModalTargetId.value)
  } catch (err: any) {
    actionError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Action failed'
  }
  isSubmittingNotes.value = false
}

function openReject(id: string) {
  rejectTargetId.value = id
  rejectReason.value = ''
  showRejectModal.value = true
}

async function submitReject() {
  if (!rejectReason.value.trim()) return
  isRejecting.value = true
  actionError.value = ''
  try {
    await accessionsApi.reject(rejectTargetId.value, { reason: rejectReason.value })
    showRejectModal.value = false
    await loadRequests()
    if (expandedId.value === rejectTargetId.value) await loadDetail(rejectTargetId.value)
  } catch (err: any) {
    actionError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Reject failed'
  }
  isRejecting.value = false
}

async function handleTransfer(id: string) {
  actionLoading.value = id
  actionError.value = ''
  try {
    await accessionsApi.transfer(id)
    await loadRequests()
    if (expandedId.value === id) await loadDetail(id)
  } catch (err: any) {
    actionError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Transfer failed'
  }
  actionLoading.value = null
}

async function handleDelete(id: string) {
  if (!confirm('Are you sure you want to delete this accession request?')) return
  actionLoading.value = id
  actionError.value = ''
  try {
    await accessionsApi.reject(id, { reason: 'Deleted by user' })
    await loadRequests()
    if (expandedId.value === id) {
      expandedId.value = null
      detailRequest.value = null
    }
  } catch (err: any) {
    actionError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Delete failed'
  }
  actionLoading.value = null
}

// ---- Create Request ----

function openCreateModal() {
  createForm.value = { sourceStructureId: '', targetLocationId: '', description: '', notes: '' }
  createError.value = ''
  showCreateModal.value = true
}

async function submitCreate() {
  isCreating.value = true
  createError.value = ''
  try {
    await accessionsApi.create({
      sourceStructureId: createForm.value.sourceStructureId || undefined,
      targetLocationId: createForm.value.targetLocationId || undefined,
      notes: createForm.value.notes || undefined
    })
    showCreateModal.value = false
    await loadRequests()
  } catch (err: any) {
    createError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Failed to create request'
  }
  isCreating.value = false
}

// ---- Add/Remove Items ----

function openAddItemModal() {
  addItemForm.value = { documentId: '', title: '', description: '' }
  addItemError.value = ''
  showAddItemModal.value = true
}

async function submitAddItem() {
  if (!expandedId.value) return
  isAddingItem.value = true
  addItemError.value = ''
  try {
    await accessionsApi.addItem(expandedId.value, {
      documentId: addItemForm.value.documentId || undefined,
      title: addItemForm.value.title || undefined,
      description: addItemForm.value.description || undefined
    })
    showAddItemModal.value = false
    await loadDetail(expandedId.value)
    await loadRequests()
  } catch (err: any) {
    addItemError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Failed to add item'
  }
  isAddingItem.value = false
}

async function removeItem(itemId: string) {
  if (!expandedId.value) return
  if (!confirm('Remove this item from the accession request?')) return
  actionLoading.value = itemId
  try {
    await accessionsApi.removeItem(expandedId.value, itemId)
    await loadDetail(expandedId.value)
    await loadRequests()
  } catch (err: any) {
    actionError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Failed to remove item'
  }
  actionLoading.value = null
}

onMounted(loadRequests)
</script>

<template>
  <div class="space-y-6">
    <!-- Back button + Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-4">
        <button
          @click="router.push('/archive')"
          class="p-2 rounded-lg border border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-400 hover:bg-zinc-50 dark:hover:bg-border-dark transition-colors"
        >
          <span class="material-symbols-outlined text-lg">arrow_back</span>
        </button>
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Accession Requests</h1>
          <p class="text-zinc-500 dark:text-zinc-400 mt-1">Manage record transfer requests to the archive</p>
        </div>
      </div>
      <button
        @click="openCreateModal"
        class="flex items-center gap-2 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors"
      >
        <span class="material-symbols-outlined text-lg">add</span>
        Create Request
      </button>
    </div>

    <!-- Stat Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <!-- Total Requests -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">inbox</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Total Requests</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.total }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">folder_open</span>
            All accessions
          </p>
        </div>
      </div>

      <!-- Pending Review -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">pending_actions</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Pending Review</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.pending }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">schedule</span>
            Awaiting action
          </p>
        </div>
      </div>

      <!-- Accepted -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">check_circle</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Accepted</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.accepted }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">verified</span>
            Ready for transfer
          </p>
        </div>
      </div>

      <!-- Transferred -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">move_to_inbox</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Transferred</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.transferred }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">done_all</span>
            Completed transfers
          </p>
        </div>
      </div>
    </div>

    <!-- Global Action Error -->
    <div v-if="actionError" class="p-3 bg-rose-50 dark:bg-rose-900/20 border border-rose-200 dark:border-rose-800 rounded-lg flex items-center gap-2">
      <span class="material-symbols-outlined text-rose-500 text-lg">error</span>
      <p class="text-sm text-rose-600 dark:text-rose-400 flex-1">{{ actionError }}</p>
      <button @click="actionError = ''" class="text-rose-400 hover:text-rose-600">
        <span class="material-symbols-outlined text-sm">close</span>
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-20">
      <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="requests.length === 0" class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark p-16 text-center">
      <div class="w-20 h-20 rounded-lg bg-zinc-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
        <span class="material-symbols-outlined text-5xl text-zinc-400">inbox</span>
      </div>
      <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">No Accession Requests</h3>
      <p class="text-zinc-500 dark:text-zinc-400 mt-1">Create your first accession request to transfer records to the archive.</p>
      <button
        @click="openCreateModal"
        class="mt-4 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors"
      >
        Create Request
      </button>
    </div>

    <!-- Requests List (Cards) -->
    <div v-else class="space-y-4">
      <div
        v-for="req in requests"
        :key="req.id"
        class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark overflow-hidden transition-all"
        :class="{ 'ring-2 ring-teal/30': expandedId === req.id }"
      >
        <!-- Card Header (clickable) -->
        <div
          @click="toggleExpand(req.id)"
          class="p-5 cursor-pointer hover:bg-zinc-50/50 dark:hover:bg-surface-dark/50 transition-colors"
        >
          <div class="flex items-center gap-4">
            <!-- Status Icon -->
            <div
              class="w-11 h-11 rounded-lg flex items-center justify-center flex-shrink-0"
              :class="{
                'bg-zinc-100 dark:bg-zinc-700': req.status === 'Draft',
                'bg-blue-100 dark:bg-blue-900/30': req.status === 'Submitted',
                'bg-amber-100 dark:bg-amber-900/30': req.status === 'UnderReview',
                'bg-emerald-100 dark:bg-emerald-900/30': req.status === 'Accepted',
                'bg-teal/15': req.status === 'PartiallyAccepted',
                'bg-rose-100 dark:bg-rose-900/30': req.status === 'Rejected',
                'bg-purple-100 dark:bg-purple-900/30': req.status === 'Transferred'
              }"
            >
              <span
                class="material-symbols-outlined text-xl"
                :class="{
                  'text-zinc-500 dark:text-zinc-400': req.status === 'Draft',
                  'text-blue-600 dark:text-blue-400': req.status === 'Submitted',
                  'text-amber-600 dark:text-amber-400': req.status === 'UnderReview',
                  'text-emerald-600 dark:text-emerald-400': req.status === 'Accepted',
                  'text-teal': req.status === 'PartiallyAccepted',
                  'text-rose-600 dark:text-rose-400': req.status === 'Rejected',
                  'text-purple-600 dark:text-purple-400': req.status === 'Transferred'
                }"
              >{{ getStatusIcon(req.status) }}</span>
            </div>

            <!-- Main Info -->
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-3 flex-wrap">
                <span class="font-mono text-sm font-semibold text-zinc-900 dark:text-white">{{ req.accessionNumber }}</span>
                <span
                  class="text-xs font-medium px-2.5 py-0.5 rounded-full"
                  :class="statusBadge[req.status] || 'bg-zinc-100 text-zinc-600'"
                >{{ req.status }}</span>
              </div>
              <div class="flex items-center gap-4 mt-1.5 text-sm text-zinc-500 dark:text-zinc-400">
                <span v-if="req.sourceStructureName" class="flex items-center gap-1">
                  <span class="material-symbols-outlined text-xs">account_tree</span>
                  {{ req.sourceStructureName }}
                </span>
                <span v-if="req.targetLocationName" class="flex items-center gap-1">
                  <span class="material-symbols-outlined text-xs">location_on</span>
                  {{ req.targetLocationName }}
                </span>
                <span class="flex items-center gap-1">
                  <span class="material-symbols-outlined text-xs">inventory_2</span>
                  {{ req.itemCount }} item{{ req.itemCount !== 1 ? 's' : '' }}
                </span>
              </div>
            </div>

            <!-- Right side: date + expand icon -->
            <div class="flex items-center gap-3 flex-shrink-0">
              <span class="text-sm text-zinc-400 dark:text-zinc-500">{{ formatDate(req.createdAt) }}</span>
              <span
                class="material-symbols-outlined text-zinc-400 transition-transform duration-200"
                :class="{ 'rotate-180': expandedId === req.id }"
              >expand_more</span>
            </div>
          </div>
        </div>

        <!-- Expanded Detail View -->
        <Transition
          enter-active-class="duration-300 ease-out"
          enter-from-class="opacity-0 max-h-0"
          enter-to-class="opacity-100 max-h-[2000px]"
          leave-active-class="duration-200 ease-in"
          leave-from-class="opacity-100 max-h-[2000px]"
          leave-to-class="opacity-0 max-h-0"
        >
          <div v-if="expandedId === req.id" class="border-t border-zinc-200 dark:border-border-dark overflow-hidden">
            <!-- Detail Loading -->
            <div v-if="detailLoading" class="p-8 flex items-center justify-center">
              <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
            </div>

            <div v-else class="p-5 space-y-5">
              <!-- Detail metadata -->
              <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div>
                  <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-1">Source Structure</p>
                  <p class="text-sm font-medium text-zinc-900 dark:text-white">{{ req.sourceStructureName || '-' }}</p>
                </div>
                <div>
                  <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-1">Target Location</p>
                  <p class="text-sm font-medium text-zinc-900 dark:text-white">{{ req.targetLocationName || '-' }}</p>
                </div>
                <div>
                  <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-1">Reviewed By</p>
                  <p class="text-sm font-medium text-zinc-900 dark:text-white">{{ detailRequest?.reviewedByName || req.reviewedByName || '-' }}</p>
                </div>
                <div>
                  <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-1">Accepted By</p>
                  <p class="text-sm font-medium text-zinc-900 dark:text-white">{{ detailRequest?.acceptedByName || req.acceptedByName || '-' }}</p>
                </div>
              </div>

              <!-- Review Notes / Rejection Reason -->
              <div v-if="req.reviewNotes || req.rejectionReason" class="p-3 bg-zinc-50 dark:bg-surface-dark rounded-lg border border-zinc-100 dark:border-border-dark">
                <p v-if="req.reviewNotes" class="text-sm text-zinc-600 dark:text-zinc-400">
                  <span class="font-medium text-zinc-700 dark:text-zinc-300">Review Notes:</span> {{ req.reviewNotes }}
                </p>
                <p v-if="req.rejectionReason" class="text-sm text-rose-600 dark:text-rose-400">
                  <span class="font-medium">Rejection Reason:</span> {{ req.rejectionReason }}
                </p>
              </div>

              <!-- Items List -->
              <div>
                <div class="flex items-center justify-between mb-3">
                  <h4 class="text-sm font-semibold text-zinc-700 dark:text-zinc-300 flex items-center gap-2">
                    <span class="material-symbols-outlined text-base">list</span>
                    Items ({{ detailItems.length }})
                  </h4>
                  <button
                    v-if="req.status === 'Draft'"
                    @click.stop="openAddItemModal"
                    class="flex items-center gap-1.5 px-3 py-1.5 bg-teal/10 hover:bg-teal/20 text-teal rounded-lg text-sm font-medium transition-colors"
                  >
                    <span class="material-symbols-outlined text-base">add</span>
                    Add Item
                  </button>
                </div>

                <div v-if="detailItems.length === 0" class="text-center py-8 text-zinc-400 dark:text-zinc-500">
                  <span class="material-symbols-outlined text-3xl mb-2 block">inventory_2</span>
                  <p class="text-sm">No items in this request</p>
                </div>

                <div v-else class="bg-white dark:bg-surface-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden">
                  <table class="w-full text-sm">
                    <thead>
                      <tr class="border-b border-zinc-100 dark:border-border-dark">
                        <th class="px-4 py-2.5 text-left text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Title / Document</th>
                        <th class="px-4 py-2.5 text-left text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Description</th>
                        <th class="px-4 py-2.5 text-right text-[10px] font-bold text-zinc-400 uppercase tracking-widest" v-if="req.status === 'Draft'">Actions</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-zinc-100 dark:divide-border-dark">
                      <tr v-for="item in detailItems" :key="item.id" class="hover:bg-zinc-50 dark:hover:bg-border-dark/50 transition-colors">
                        <td class="px-4 py-3 font-medium text-zinc-900 dark:text-white">
                          {{ item.title || item.documentTitle || item.documentId || '-' }}
                        </td>
                        <td class="px-4 py-3 text-zinc-500 dark:text-zinc-400">{{ item.description || '-' }}</td>
                        <td v-if="req.status === 'Draft'" class="px-4 py-3 text-right">
                          <button
                            @click.stop="removeItem(item.id)"
                            :disabled="actionLoading === item.id"
                            class="p-1.5 text-zinc-400 hover:text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-900/20 rounded-lg transition-colors disabled:opacity-50"
                          >
                            <span v-if="actionLoading === item.id" class="w-4 h-4 border-2 border-rose-400 border-t-transparent rounded-full animate-spin inline-block"></span>
                            <span v-else class="material-symbols-outlined text-base">delete</span>
                          </button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <!-- Action Buttons (based on status) -->
              <div class="flex items-center gap-3 pt-2 border-t border-zinc-100 dark:border-border-dark">
                <!-- Draft: Submit, Edit, Delete -->
                <template v-if="req.status === 'Draft'">
                  <button
                    @click.stop="handleSubmit(req.id)"
                    :disabled="actionLoading === req.id"
                    class="flex items-center gap-2 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                  >
                    <span v-if="actionLoading === req.id" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                    <span v-else class="material-symbols-outlined text-base">send</span>
                    Submit
                  </button>
                  <button
                    @click.stop
                    class="flex items-center gap-2 px-4 py-2 bg-zinc-100 dark:bg-surface-dark hover:bg-zinc-200 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark"
                  >
                    <span class="material-symbols-outlined text-base">edit</span>
                    Edit
                  </button>
                  <button
                    @click.stop="handleDelete(req.id)"
                    :disabled="actionLoading === req.id"
                    class="flex items-center gap-2 px-4 py-2 text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/20 rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                  >
                    <span class="material-symbols-outlined text-base">delete</span>
                    Delete
                  </button>
                </template>

                <!-- Submitted: Review (admin) -->
                <template v-if="req.status === 'Submitted'">
                  <button
                    @click.stop="openReviewNotes(req.id)"
                    class="flex items-center gap-2 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors"
                  >
                    <span class="material-symbols-outlined text-base">rate_review</span>
                    Review
                  </button>
                </template>

                <!-- UnderReview: Accept, Reject, Partially Accept -->
                <template v-if="req.status === 'UnderReview'">
                  <button
                    @click.stop="openAcceptNotes(req.id)"
                    class="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded-lg font-medium text-sm transition-colors"
                  >
                    <span class="material-symbols-outlined text-base">check_circle</span>
                    Accept
                  </button>
                  <button
                    @click.stop="openReject(req.id)"
                    class="flex items-center gap-2 px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-lg font-medium text-sm transition-colors"
                  >
                    <span class="material-symbols-outlined text-base">cancel</span>
                    Reject
                  </button>
                  <button
                    @click.stop="openAcceptNotes(req.id)"
                    class="flex items-center gap-2 px-4 py-2 bg-zinc-100 dark:bg-surface-dark hover:bg-zinc-200 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark"
                  >
                    <span class="material-symbols-outlined text-base">rule</span>
                    Partially Accept
                  </button>
                </template>

                <!-- Accepted: Transfer -->
                <template v-if="req.status === 'Accepted' || req.status === 'PartiallyAccepted'">
                  <button
                    @click.stop="handleTransfer(req.id)"
                    :disabled="actionLoading === req.id"
                    class="flex items-center gap-2 px-4 py-2 bg-purple-600 hover:bg-purple-700 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                  >
                    <span v-if="actionLoading === req.id" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                    <span v-else class="material-symbols-outlined text-base">move_to_inbox</span>
                    Transfer
                  </button>
                </template>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </div>

    <!-- ===== CREATE REQUEST MODAL ===== -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showCreateModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <Transition
            enter-active-class="duration-300 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="duration-200 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showCreateModal" class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-lg overflow-hidden ring-1 ring-black/5 dark:ring-white/10">
              <!-- Modal Header -->
              <div class="relative bg-gradient-to-r from-[#0d1117] via-[#0d1117]/95 to-teal/80 p-5 overflow-hidden">
                <div class="absolute top-0 right-0 w-48 h-48 bg-teal/15 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-16 w-20 h-20 bg-teal/10 rounded-full translate-y-1/2"></div>
                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-teal/25 backdrop-blur rounded-lg flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-xl">add_box</span>
                    </div>
                    <div>
                      <h3 class="text-lg font-bold text-white">Create Accession Request</h3>
                      <p class="text-sm text-white/60 mt-0.5">New record transfer request</p>
                    </div>
                  </div>
                  <button @click="showCreateModal = false" class="p-1.5 hover:bg-white/10 rounded-lg transition-colors">
                    <span class="material-symbols-outlined text-white/70">close</span>
                  </button>
                </div>
              </div>

              <!-- Modal Body -->
              <div class="p-6 space-y-4">
                <div v-if="createError" class="p-3 bg-rose-50 dark:bg-rose-900/20 border border-rose-200 dark:border-rose-800 rounded-lg">
                  <p class="text-sm text-rose-600 dark:text-rose-400">{{ createError }}</p>
                </div>

                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Source Structure ID</label>
                  <input
                    v-model="createForm.sourceStructureId"
                    type="text"
                    placeholder="Enter source structure ID"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Target Location ID</label>
                  <input
                    v-model="createForm.targetLocationId"
                    type="text"
                    placeholder="Enter target location ID"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Description</label>
                  <textarea
                    v-model="createForm.description"
                    rows="2"
                    placeholder="Brief description of the accession"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none"
                  ></textarea>
                </div>

                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Notes</label>
                  <textarea
                    v-model="createForm.notes"
                    rows="2"
                    placeholder="Additional notes (optional)"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none"
                  ></textarea>
                </div>
              </div>

              <!-- Modal Footer -->
              <div class="px-6 py-4 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3 bg-zinc-50 dark:bg-surface-dark">
                <button
                  @click="showCreateModal = false"
                  class="px-4 py-2 text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg font-medium text-sm transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="submitCreate"
                  :disabled="isCreating"
                  class="flex items-center gap-2 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                >
                  <span v-if="isCreating" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span v-else class="material-symbols-outlined text-base">add</span>
                  Create Request
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- ===== ADD ITEM MODAL ===== -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showAddItemModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <Transition
            enter-active-class="duration-300 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="duration-200 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showAddItemModal" class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-md overflow-hidden ring-1 ring-black/5 dark:ring-white/10">
              <!-- Modal Header -->
              <div class="relative bg-gradient-to-r from-[#0d1117] via-[#0d1117]/95 to-teal/80 p-5 overflow-hidden">
                <div class="absolute top-0 right-0 w-48 h-48 bg-teal/15 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-16 w-20 h-20 bg-teal/10 rounded-full translate-y-1/2"></div>
                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-teal/25 backdrop-blur rounded-lg flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-xl">playlist_add</span>
                    </div>
                    <div>
                      <h3 class="text-lg font-bold text-white">Add Item</h3>
                      <p class="text-sm text-white/60 mt-0.5">Add a record to this request</p>
                    </div>
                  </div>
                  <button @click="showAddItemModal = false" class="p-1.5 hover:bg-white/10 rounded-lg transition-colors">
                    <span class="material-symbols-outlined text-white/70">close</span>
                  </button>
                </div>
              </div>

              <!-- Modal Body -->
              <div class="p-6 space-y-4">
                <div v-if="addItemError" class="p-3 bg-rose-50 dark:bg-rose-900/20 border border-rose-200 dark:border-rose-800 rounded-lg">
                  <p class="text-sm text-rose-600 dark:text-rose-400">{{ addItemError }}</p>
                </div>

                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Document ID</label>
                  <input
                    v-model="addItemForm.documentId"
                    type="text"
                    placeholder="Enter document ID (optional)"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Title</label>
                  <input
                    v-model="addItemForm.title"
                    type="text"
                    placeholder="Item title"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
                  />
                </div>

                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Description</label>
                  <textarea
                    v-model="addItemForm.description"
                    rows="2"
                    placeholder="Item description (optional)"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none"
                  ></textarea>
                </div>
              </div>

              <!-- Modal Footer -->
              <div class="px-6 py-4 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3 bg-zinc-50 dark:bg-surface-dark">
                <button
                  @click="showAddItemModal = false"
                  class="px-4 py-2 text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg font-medium text-sm transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="submitAddItem"
                  :disabled="isAddingItem"
                  class="flex items-center gap-2 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                >
                  <span v-if="isAddingItem" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span v-else class="material-symbols-outlined text-base">add</span>
                  Add Item
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- ===== REJECT MODAL ===== -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showRejectModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <Transition
            enter-active-class="duration-300 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="duration-200 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showRejectModal" class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-md overflow-hidden ring-1 ring-black/5 dark:ring-white/10">
              <!-- Modal Header -->
              <div class="relative bg-gradient-to-r from-[#0d1117] via-[#0d1117]/95 to-rose-600/80 p-5 overflow-hidden">
                <div class="absolute top-0 right-0 w-48 h-48 bg-rose-500/15 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-16 w-20 h-20 bg-rose-500/10 rounded-full translate-y-1/2"></div>
                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-rose-500/25 backdrop-blur rounded-lg flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-xl">cancel</span>
                    </div>
                    <div>
                      <h3 class="text-lg font-bold text-white">Reject Request</h3>
                      <p class="text-sm text-white/60 mt-0.5">Provide a reason for rejection</p>
                    </div>
                  </div>
                  <button @click="showRejectModal = false" class="p-1.5 hover:bg-white/10 rounded-lg transition-colors">
                    <span class="material-symbols-outlined text-white/70">close</span>
                  </button>
                </div>
              </div>

              <!-- Modal Body -->
              <div class="p-6 space-y-4">
                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Rejection Reason <span class="text-rose-500">*</span></label>
                  <textarea
                    v-model="rejectReason"
                    rows="3"
                    placeholder="Explain why this request is being rejected..."
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-rose-500/50 focus:border-rose-500 outline-none resize-none"
                  ></textarea>
                </div>
              </div>

              <!-- Modal Footer -->
              <div class="px-6 py-4 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3 bg-zinc-50 dark:bg-surface-dark">
                <button
                  @click="showRejectModal = false"
                  class="px-4 py-2 text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg font-medium text-sm transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="submitReject"
                  :disabled="isRejecting || !rejectReason.trim()"
                  class="flex items-center gap-2 px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                >
                  <span v-if="isRejecting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span v-else class="material-symbols-outlined text-base">cancel</span>
                  Reject
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- ===== NOTES MODAL (Review / Accept) ===== -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="duration-200 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showNotesModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <Transition
            enter-active-class="duration-300 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="duration-200 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showNotesModal" class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-md overflow-hidden ring-1 ring-black/5 dark:ring-white/10">
              <!-- Modal Header -->
              <div class="relative bg-gradient-to-r from-[#0d1117] via-[#0d1117]/95 to-teal/80 p-5 overflow-hidden">
                <div class="absolute top-0 right-0 w-48 h-48 bg-teal/15 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-16 w-20 h-20 bg-teal/10 rounded-full translate-y-1/2"></div>
                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 bg-teal/25 backdrop-blur rounded-lg flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-xl">{{ notesModalAction === 'review' ? 'rate_review' : 'check_circle' }}</span>
                    </div>
                    <div>
                      <h3 class="text-lg font-bold text-white">{{ notesModalAction === 'review' ? 'Review Request' : 'Accept Request' }}</h3>
                      <p class="text-sm text-white/60 mt-0.5">{{ notesModalAction === 'review' ? 'Start the review process' : 'Approve this accession' }}</p>
                    </div>
                  </div>
                  <button @click="showNotesModal = false" class="p-1.5 hover:bg-white/10 rounded-lg transition-colors">
                    <span class="material-symbols-outlined text-white/70">close</span>
                  </button>
                </div>
              </div>

              <!-- Modal Body -->
              <div class="p-6 space-y-4">
                <div>
                  <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Notes (optional)</label>
                  <textarea
                    v-model="notesModalNotes"
                    rows="3"
                    :placeholder="notesModalAction === 'review' ? 'Add review notes...' : 'Add acceptance notes...'"
                    class="w-full px-3 py-2 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none"
                  ></textarea>
                </div>
              </div>

              <!-- Modal Footer -->
              <div class="px-6 py-4 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3 bg-zinc-50 dark:bg-surface-dark">
                <button
                  @click="showNotesModal = false"
                  class="px-4 py-2 text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg font-medium text-sm transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="submitNotesAction"
                  :disabled="isSubmittingNotes"
                  class="flex items-center gap-2 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-lg font-medium text-sm transition-colors disabled:opacity-50"
                >
                  <span v-if="isSubmittingNotes" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                  <span v-else class="material-symbols-outlined text-base">{{ notesModalAction === 'review' ? 'rate_review' : 'check_circle' }}</span>
                  {{ notesModalAction === 'review' ? 'Start Review' : 'Accept' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
