<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import type { ApprovalRequest, ApprovalWorkflow } from '@/types'
import { ApprovalStatus, ApprovalActionTypes } from '@/types'
import { approvalsApi } from '@/api/client'
import { DmsLoader, UiModal } from '@/components/ui'
import EmptyState from '@/components/common/EmptyState.vue'

const router = useRouter()
const activeTab = ref<'pending' | 'myRequests' | 'workflows'>('pending')

const pendingRequests = ref<ApprovalRequest[]>([])
const myRequests = ref<ApprovalRequest[]>([])
const workflows = ref<ApprovalWorkflow[]>([])
const isLoading = ref(true)
const isSubmitting = ref(false)

// Review modal
const showActionModal = ref(false)
const selectedRequest = ref<ApprovalRequest | null>(null)
const actionData = ref({ action: ApprovalActionTypes.Approved, comments: '' })

// Request detail expansion
const expandedRequestId = ref<string | null>(null)

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [pendingRes, myRes, workflowsRes] = await Promise.all([
      approvalsApi.getPendingRequests(),
      approvalsApi.getMyRequests(),
      approvalsApi.getWorkflows()
    ])
    pendingRequests.value = pendingRes.data
    myRequests.value = myRes.data
    workflows.value = workflowsRes.data
  } catch (err) {
    // silently fail
  } finally {
    isLoading.value = false
  }
}

function openActionModal(request: ApprovalRequest, presetAction?: number) {
  selectedRequest.value = request
  actionData.value = { action: presetAction ?? ApprovalActionTypes.Approved, comments: '' }
  showActionModal.value = true
}

async function submitAction() {
  if (!selectedRequest.value || isSubmitting.value) return
  isSubmitting.value = true
  try {
    await approvalsApi.submitAction(selectedRequest.value.id, actionData.value)
    showActionModal.value = false
    await loadData()
  } catch (err) {
    // silently fail
  } finally {
    isSubmitting.value = false
  }
}

async function cancelRequest(request: ApprovalRequest) {
  if (!confirm('Cancel this approval request? This cannot be undone.')) return
  try {
    await approvalsApi.cancelRequest(request.id)
    await loadData()
  } catch (err) {
    // silently fail
  }
}

async function resubmitRequest(request: ApprovalRequest) {
  if (!confirm('Resubmit this document for approval?')) return
  try {
    await approvalsApi.resubmitRequest(request.id)
    await loadData()
  } catch (err) {
    // silently fail
  }
}

function viewDocument(documentId: string) {
  router.push(`/documents/${documentId}`)
}

function toggleRequestExpand(requestId: string) {
  expandedRequestId.value = expandedRequestId.value === requestId ? null : requestId
}

// Computed stats
const pendingCount = computed(() => pendingRequests.value.length)
const myPendingCount = computed(() => myRequests.value.filter(r => r.status === ApprovalStatus.Pending).length)
const myApprovedCount = computed(() => myRequests.value.filter(r => r.status === ApprovalStatus.Approved).length)
const myRejectedCount = computed(() => myRequests.value.filter(r => r.status === ApprovalStatus.Rejected).length)
const myReturnedCount = computed(() => myRequests.value.filter(r => r.status === ApprovalStatus.ReturnedForRevision).length)
const activeWorkflowCount = computed(() => workflows.value.filter(w => w.isActive).length)

// Helpers
function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric', month: 'short', day: 'numeric'
  })
}

function formatDateTime(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit'
  })
}

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime()
  const hours = Math.floor(diff / 3600000)
  if (hours < 1) return 'Just now'
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d ago`
  return formatDate(dateStr)
}

function getDueUrgency(request: ApprovalRequest): 'overdue' | 'soon' | 'normal' | null {
  if (!request.dueDate) return null
  const diff = new Date(request.dueDate).getTime() - Date.now()
  if (diff < 0) return 'overdue'
  if (diff < 86400000 * 2) return 'soon' // within 2 days
  return 'normal'
}

function getStatusConfig(status: number) {
  switch (status) {
    case ApprovalStatus.Pending: return {
      label: 'Pending', icon: 'hourglass_top',
      bg: 'bg-indigo-50 dark:bg-indigo-900/30 border-indigo-200 dark:border-indigo-700',
      text: 'text-indigo-600 dark:text-indigo-400', dot: 'bg-indigo-500'
    }
    case ApprovalStatus.Approved: return {
      label: 'Approved', icon: 'check_circle',
      bg: 'bg-emerald-50 dark:bg-emerald-900/30 border-emerald-200 dark:border-emerald-700',
      text: 'text-emerald-600 dark:text-emerald-400', dot: 'bg-emerald-500'
    }
    case ApprovalStatus.Rejected: return {
      label: 'Rejected', icon: 'cancel',
      bg: 'bg-rose-50 dark:bg-rose-900/30 border-rose-200 dark:border-rose-700',
      text: 'text-rose-600 dark:text-rose-400', dot: 'bg-rose-500'
    }
    case ApprovalStatus.Cancelled: return {
      label: 'Cancelled', icon: 'block',
      bg: 'bg-zinc-50 dark:bg-zinc-800/30 border-zinc-200 dark:border-zinc-700',
      text: 'text-zinc-500 dark:text-zinc-400', dot: 'bg-zinc-400'
    }
    case ApprovalStatus.ReturnedForRevision: return {
      label: 'Revision Required', icon: 'undo',
      bg: 'bg-amber-50 dark:bg-amber-900/30 border-amber-200 dark:border-amber-700',
      text: 'text-amber-600 dark:text-amber-400', dot: 'bg-amber-500'
    }
    default: return {
      label: 'Unknown', icon: 'help',
      bg: 'bg-zinc-50 border-zinc-200', text: 'text-zinc-500', dot: 'bg-zinc-400'
    }
  }
}

function getActionConfig(action: number) {
  switch (action) {
    case ApprovalActionTypes.Approved: return {
      label: 'Approved', icon: 'check_circle', color: 'text-emerald-600 dark:text-emerald-400'
    }
    case ApprovalActionTypes.Rejected: return {
      label: 'Rejected', icon: 'cancel', color: 'text-rose-600 dark:text-rose-400'
    }
    case ApprovalActionTypes.ReturnedForRevision: return {
      label: 'Returned for revision', icon: 'undo', color: 'text-amber-600 dark:text-amber-400'
    }
    default: return { label: 'Unknown', icon: 'help', color: 'text-zinc-500' }
  }
}

function getTriggerConfig(triggerType: string) {
  switch (triggerType) {
    case 'OnUpload': return { label: 'On Upload', icon: 'upload_file', color: 'text-teal' }
    case 'OnStatusChange': return { label: 'On Status Change', icon: 'sync', color: 'text-blue-500' }
    case 'Scheduled': return { label: 'Scheduled', icon: 'schedule', color: 'text-purple-500' }
    default: return { label: 'Manual', icon: 'touch_app', color: 'text-zinc-500' }
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100 flex items-center gap-3">
          <div class="w-10 h-10 bg-gradient-to-br from-teal to-teal/70 rounded-xl flex items-center justify-center shadow-lg shadow-teal/20">
            <span class="material-symbols-outlined text-white text-xl" style="font-variation-settings: 'FILL' 1;">approval</span>
          </div>
          Approvals
        </h1>
        <p class="text-zinc-500 mt-1 ml-[52px]">Manage approval tasks, track requests, and review workflows</p>
      </div>
      <button
        @click="loadData"
        :disabled="isLoading"
        class="p-2.5 rounded-lg bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark hover:bg-zinc-50 dark:hover:bg-zinc-700 transition-colors shadow-sm"
        title="Refresh"
      >
        <span class="material-symbols-outlined text-zinc-500" :class="{ 'animate-spin': isLoading }">refresh</span>
      </button>
    </div>

    <!-- Stats Grid -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <!-- Awaiting Review -->
      <button @click="activeTab = 'pending'" class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden text-left group hover:border-teal/30 transition-all">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">pending_actions</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Awaiting Review</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ pendingCount }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">schedule</span>
            Tasks assigned to you
          </p>
        </div>
      </button>

      <!-- My Requests -->
      <button @click="activeTab = 'myRequests'" class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden text-left group hover:border-teal/30 transition-all">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">send</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">My Requests</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ myRequests.length }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">hourglass_top</span>
            {{ myPendingCount }} pending
          </p>
        </div>
      </button>

      <!-- Approved -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">check_circle</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Approved</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ myApprovedCount }}</p>
          <p v-if="myReturnedCount > 0" class="text-[10px] text-amber-400 mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">undo</span>
            {{ myReturnedCount }} need revision
          </p>
          <p v-else-if="myRejectedCount > 0" class="text-[10px] text-rose-400 mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">cancel</span>
            {{ myRejectedCount }} rejected
          </p>
          <p v-else class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">trending_up</span>
            Documents approved
          </p>
        </div>
      </div>

      <!-- Active Workflows -->
      <button @click="activeTab = 'workflows'" class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden text-left group hover:border-teal/30 transition-all">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">account_tree</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Active Workflows</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ activeWorkflowCount }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">bolt</span>
            {{ workflows.length }} total configured
          </p>
        </div>
      </button>
    </div>

    <!-- Tab Navigation -->
    <div class="bg-white dark:bg-surface-dark rounded-xl border border-zinc-200 dark:border-border-dark shadow-sm">
      <div class="border-b border-zinc-200 dark:border-border-dark px-1">
        <nav class="flex gap-1 p-1">
          <button
            v-for="tab in [
              { key: 'pending', label: 'Pending Approval', icon: 'pending_actions', count: pendingCount },
              { key: 'myRequests', label: 'My Requests', icon: 'send', count: myRequests.length },
              { key: 'workflows', label: 'Workflows', icon: 'account_tree', count: null }
            ] as const"
            :key="tab.key"
            @click="activeTab = tab.key"
            class="flex items-center gap-2 px-4 py-2.5 text-sm font-medium rounded-lg transition-all"
            :class="activeTab === tab.key
              ? 'bg-teal/10 text-teal'
              : 'text-zinc-500 hover:text-zinc-700 dark:hover:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-800'"
          >
            <span class="material-symbols-outlined text-lg" :style="activeTab === tab.key ? 'font-variation-settings: \'FILL\' 1;' : ''">{{ tab.icon }}</span>
            {{ tab.label }}
            <span
              v-if="tab.count && tab.count > 0"
              class="ml-1 px-2 py-0.5 text-[10px] font-bold rounded-full"
              :class="activeTab === tab.key
                ? 'bg-teal/20 text-teal'
                : 'bg-zinc-100 dark:bg-zinc-700 text-zinc-600 dark:text-zinc-300'"
            >
              {{ tab.count }}
            </span>
          </button>
        </nav>
      </div>

      <!-- Tab Content -->
      <div class="p-5">
        <!-- Loading -->
        <div v-if="isLoading" class="py-12">
          <DmsLoader size="lg" text="Loading approvals..." />
        </div>

        <div v-else>
          <!-- ===== PENDING APPROVAL TAB ===== -->
          <div v-if="activeTab === 'pending'">
            <EmptyState
              v-if="pendingRequests.length === 0"
              icon="task_alt"
              title="All caught up!"
              description="No documents are waiting for your approval right now"
            />

            <div v-else class="space-y-4">
              <TransitionGroup name="list" tag="div" class="space-y-4">
                <div
                  v-for="request in pendingRequests"
                  :key="request.id"
                  class="group relative bg-white dark:bg-background-dark rounded-xl border border-zinc-200 dark:border-border-dark hover:border-teal/30 dark:hover:border-teal/30 transition-all hover:shadow-lg hover:shadow-teal/5"
                >
                  <!-- Urgency indicator bar -->
                  <div
                    v-if="getDueUrgency(request)"
                    class="absolute top-0 left-0 right-0 h-0.5 rounded-t-xl"
                    :class="{
                      'bg-rose-500': getDueUrgency(request) === 'overdue',
                      'bg-amber-500': getDueUrgency(request) === 'soon',
                      'bg-teal': getDueUrgency(request) === 'normal'
                    }"
                  ></div>

                  <div class="p-5">
                    <div class="flex items-start gap-4">
                      <!-- Document icon -->
                      <div class="flex-shrink-0 w-12 h-12 bg-gradient-to-br from-indigo-500 to-indigo-600 rounded-xl flex items-center justify-center shadow-lg shadow-indigo-500/20">
                        <span class="material-symbols-outlined text-white text-xl">description</span>
                      </div>

                      <!-- Content -->
                      <div class="flex-1 min-w-0">
                        <div class="flex items-start justify-between gap-4">
                          <div class="min-w-0">
                            <h3 class="font-semibold text-zinc-900 dark:text-zinc-100 truncate">{{ request.documentName }}</h3>
                            <div class="flex items-center gap-3 mt-1 text-sm text-zinc-500">
                              <span class="flex items-center gap-1">
                                <span class="material-symbols-outlined text-sm">person</span>
                                {{ request.requestedByName }}
                              </span>
                              <span class="flex items-center gap-1">
                                <span class="material-symbols-outlined text-sm">schedule</span>
                                {{ timeAgo(request.createdAt) }}
                              </span>
                              <span v-if="request.workflowName" class="flex items-center gap-1">
                                <span class="material-symbols-outlined text-sm">account_tree</span>
                                {{ request.workflowName }}
                              </span>
                            </div>
                          </div>

                          <!-- Due date badge -->
                          <div v-if="request.dueDate" class="flex-shrink-0">
                            <span
                              class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-medium border"
                              :class="{
                                'bg-rose-50 text-rose-600 border-rose-200': getDueUrgency(request) === 'overdue',
                                'bg-amber-50 text-amber-600 border-amber-200': getDueUrgency(request) === 'soon',
                                'bg-zinc-50 text-zinc-600 border-zinc-200': getDueUrgency(request) === 'normal'
                              }"
                            >
                              <span class="material-symbols-outlined text-sm">event</span>
                              Due {{ formatDate(request.dueDate) }}
                            </span>
                          </div>
                        </div>

                        <!-- Comments -->
                        <p v-if="request.comments" class="mt-2.5 text-sm text-zinc-600 dark:text-zinc-400 bg-zinc-50 dark:bg-zinc-800/50 rounded-lg px-3 py-2 border-l-2 border-indigo-300">
                          {{ request.comments }}
                        </p>

                        <!-- Action buttons -->
                        <div class="flex items-center gap-2 mt-4">
                          <button
                            @click="openActionModal(request, ApprovalActionTypes.Approved)"
                            :disabled="isSubmitting"
                            class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium text-white bg-emerald-600 rounded-lg hover:bg-emerald-700 shadow-sm shadow-emerald-600/20 transition-all disabled:opacity-50"
                          >
                            <span class="material-symbols-outlined text-base">check</span>
                            Approve
                          </button>
                          <button
                            @click="openActionModal(request, ApprovalActionTypes.Rejected)"
                            :disabled="isSubmitting"
                            class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium text-rose-700 bg-rose-50 border border-rose-200 rounded-lg hover:bg-rose-100 transition-all disabled:opacity-50"
                          >
                            <span class="material-symbols-outlined text-base">close</span>
                            Reject
                          </button>
                          <button
                            @click="openActionModal(request, ApprovalActionTypes.ReturnedForRevision)"
                            class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium text-zinc-600 dark:text-zinc-300 bg-zinc-100 dark:bg-zinc-700 border border-zinc-200 dark:border-zinc-600 rounded-lg hover:bg-zinc-200 dark:hover:bg-zinc-600 transition-all"
                          >
                            <span class="material-symbols-outlined text-base">undo</span>
                            Return
                          </button>
                          <div class="flex-1"></div>
                          <button
                            @click="viewDocument(request.documentId)"
                            class="inline-flex items-center gap-1.5 px-3 py-2 text-sm text-teal hover:text-teal/80 hover:bg-teal/5 rounded-lg transition-all"
                          >
                            <span class="material-symbols-outlined text-base">open_in_new</span>
                            View Document
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </TransitionGroup>
            </div>
          </div>

          <!-- ===== MY REQUESTS TAB ===== -->
          <div v-if="activeTab === 'myRequests'">
            <EmptyState
              v-if="myRequests.length === 0"
              icon="send"
              title="No approval requests"
              description="Documents you submit for approval will appear here"
            />

            <div v-else class="overflow-hidden rounded-lg border border-zinc-200 dark:border-border-dark">
              <table class="w-full text-left border-collapse">
                <thead>
                  <tr class="bg-[#0d1117] text-xs font-semibold text-zinc-300 uppercase tracking-wider">
                    <th class="px-5 py-4">Document</th>
                    <th class="px-5 py-4">Workflow</th>
                    <th class="px-5 py-4">Submitted</th>
                    <th class="px-5 py-4">Status</th>
                    <th class="px-5 py-4 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <template v-for="request in myRequests" :key="request.id">
                    <tr
                      class="border-t border-zinc-100 dark:border-border-dark hover:bg-zinc-50/50 dark:hover:bg-zinc-800/30 transition-colors cursor-pointer"
                      @click="toggleRequestExpand(request.id)"
                    >
                      <td class="px-5 py-4">
                        <div class="flex items-center gap-3">
                          <div class="w-8 h-8 bg-zinc-100 dark:bg-zinc-700 rounded-lg flex items-center justify-center">
                            <span class="material-symbols-outlined text-zinc-500 text-base">description</span>
                          </div>
                          <button
                            @click.stop="viewDocument(request.documentId)"
                            class="text-sm font-medium text-teal hover:text-teal/80 transition-colors"
                          >
                            {{ request.documentName }}
                          </button>
                        </div>
                      </td>
                      <td class="px-5 py-4">
                        <span class="text-sm text-zinc-600 dark:text-zinc-400">{{ request.workflowName || 'Standard' }}</span>
                      </td>
                      <td class="px-5 py-4">
                        <span class="text-sm text-zinc-500">{{ formatDate(request.createdAt) }}</span>
                      </td>
                      <td class="px-5 py-4">
                        <span
                          class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-[10px] font-semibold uppercase tracking-wider border"
                          :class="[getStatusConfig(request.status).bg, getStatusConfig(request.status).text]"
                        >
                          <span class="material-symbols-outlined text-xs" style="font-variation-settings: 'FILL' 1;">{{ getStatusConfig(request.status).icon }}</span>
                          {{ getStatusConfig(request.status).label }}
                        </span>
                      </td>
                      <td class="px-5 py-4 text-right">
                        <div class="flex items-center justify-end gap-2">
                          <button
                            v-if="request.status === ApprovalStatus.ReturnedForRevision || request.status === ApprovalStatus.Rejected"
                            @click.stop="resubmitRequest(request)"
                            class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-teal hover:bg-teal/10 rounded-lg transition-colors"
                          >
                            <span class="material-symbols-outlined text-sm">replay</span>
                            Resubmit
                          </button>
                          <button
                            v-if="request.status === ApprovalStatus.Pending"
                            @click.stop="cancelRequest(request)"
                            class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/20 rounded-lg transition-colors"
                          >
                            <span class="material-symbols-outlined text-sm">block</span>
                            Cancel
                          </button>
                          <span
                            class="material-symbols-outlined text-zinc-400 text-lg transition-transform"
                            :class="{ 'rotate-180': expandedRequestId === request.id }"
                          >expand_more</span>
                        </div>
                      </td>
                    </tr>

                    <!-- Expanded detail row -->
                    <tr v-if="expandedRequestId === request.id">
                      <td colspan="5" class="px-5 pb-5 bg-zinc-50/50 dark:bg-zinc-800/20">
                        <div class="pt-2 pl-11">
                          <!-- Revision/Rejection reason banner -->
                          <div
                            v-if="(request.status === ApprovalStatus.ReturnedForRevision || request.status === ApprovalStatus.Rejected) && request.actions?.length"
                            class="mb-4 p-4 rounded-lg border flex items-start gap-3"
                            :class="request.status === ApprovalStatus.ReturnedForRevision
                              ? 'bg-amber-50 dark:bg-amber-900/20 border-amber-200 dark:border-amber-700'
                              : 'bg-rose-50 dark:bg-rose-900/20 border-rose-200 dark:border-rose-700'"
                          >
                            <span
                              class="material-symbols-outlined text-xl mt-0.5"
                              :class="request.status === ApprovalStatus.ReturnedForRevision ? 'text-amber-500' : 'text-rose-500'"
                              style="font-variation-settings: 'FILL' 1;"
                            >{{ request.status === ApprovalStatus.ReturnedForRevision ? 'undo' : 'cancel' }}</span>
                            <div class="flex-1">
                              <p class="text-sm font-semibold" :class="request.status === ApprovalStatus.ReturnedForRevision ? 'text-amber-800 dark:text-amber-300' : 'text-rose-800 dark:text-rose-300'">
                                {{ request.status === ApprovalStatus.ReturnedForRevision ? 'Returned for Revision' : 'Rejected' }}
                                by {{ request.actions[request.actions.length - 1].approverName || 'Unknown' }}
                              </p>
                              <p v-if="request.actions[request.actions.length - 1].comments" class="text-sm mt-1" :class="request.status === ApprovalStatus.ReturnedForRevision ? 'text-amber-700 dark:text-amber-400' : 'text-rose-700 dark:text-rose-400'">
                                "{{ request.actions[request.actions.length - 1].comments }}"
                              </p>
                              <p class="text-xs mt-1.5" :class="request.status === ApprovalStatus.ReturnedForRevision ? 'text-amber-500' : 'text-rose-500'">
                                {{ formatDateTime(request.actions[request.actions.length - 1].actionDate) }}
                              </p>
                            </div>
                          </div>

                          <!-- Request info -->
                          <div class="flex items-start gap-6 mb-4">
                            <div v-if="request.comments" class="flex-1">
                              <p class="text-xs font-medium text-zinc-500 uppercase mb-1">Comments</p>
                              <p class="text-sm text-zinc-700 dark:text-zinc-300">{{ request.comments }}</p>
                            </div>
                            <div v-if="request.dueDate">
                              <p class="text-xs font-medium text-zinc-500 uppercase mb-1">Due Date</p>
                              <p class="text-sm text-zinc-700 dark:text-zinc-300">{{ formatDate(request.dueDate) }}</p>
                            </div>
                            <div v-if="request.completedAt">
                              <p class="text-xs font-medium text-zinc-500 uppercase mb-1">Completed</p>
                              <p class="text-sm text-zinc-700 dark:text-zinc-300">{{ formatDateTime(request.completedAt) }}</p>
                            </div>
                          </div>

                          <!-- Approval actions timeline -->
                          <div v-if="request.actions && request.actions.length > 0">
                            <p class="text-xs font-medium text-zinc-500 uppercase mb-3">Approval History</p>
                            <div class="space-y-3">
                              <div
                                v-for="action in request.actions"
                                :key="action.id"
                                class="flex items-start gap-3"
                              >
                                <div class="flex-shrink-0 mt-0.5">
                                  <span
                                    class="material-symbols-outlined text-lg"
                                    :class="getActionConfig(action.action).color"
                                    style="font-variation-settings: 'FILL' 1;"
                                  >{{ getActionConfig(action.action).icon }}</span>
                                </div>
                                <div class="flex-1 min-w-0">
                                  <p class="text-sm text-zinc-800 dark:text-zinc-200">
                                    <span class="font-medium">{{ action.approverName || 'Unknown' }}</span>
                                    <span class="text-zinc-500"> {{ getActionConfig(action.action).label.toLowerCase() }} this document</span>
                                  </p>
                                  <p v-if="action.comments" class="text-sm text-zinc-500 mt-0.5">"{{ action.comments }}"</p>
                                  <p class="text-xs text-zinc-400 mt-0.5">{{ formatDateTime(action.actionDate) }}</p>
                                </div>
                              </div>
                            </div>
                          </div>

                          <div v-else-if="request.status === ApprovalStatus.Pending" class="text-sm text-zinc-400 italic">
                            No actions taken yet — awaiting reviewer response
                          </div>
                        </div>
                      </td>
                    </tr>
                  </template>
                </tbody>
              </table>
            </div>
          </div>

          <!-- ===== WORKFLOWS TAB ===== -->
          <div v-if="activeTab === 'workflows'">
            <EmptyState
              v-if="workflows.length === 0"
              icon="account_tree"
              title="No workflows configured"
              description="Approval workflows are set up by administrators in the Workflow Designer"
            />

            <div v-else class="grid grid-cols-1 lg:grid-cols-2 gap-4">
              <div
                v-for="workflow in workflows"
                :key="workflow.id"
                class="bg-white dark:bg-background-dark rounded-xl border border-zinc-200 dark:border-border-dark overflow-hidden hover:border-purple-200 dark:hover:border-purple-800 transition-all hover:shadow-lg hover:shadow-purple-500/5"
              >
                <!-- Workflow header -->
                <div class="relative bg-gradient-to-r from-[#0d1117] to-[#0d1117]/95 px-5 py-4 overflow-hidden">
                  <div class="absolute top-0 right-0 w-24 h-24 bg-purple-500/10 rounded-full -translate-y-8 translate-x-8"></div>
                  <div class="relative flex items-center justify-between">
                    <div class="flex items-center gap-3">
                      <div class="w-9 h-9 bg-purple-500/20 backdrop-blur rounded-lg flex items-center justify-center">
                        <span class="material-symbols-outlined text-purple-400 text-lg">account_tree</span>
                      </div>
                      <div>
                        <h3 class="font-semibold text-white text-sm">{{ workflow.name }}</h3>
                        <p v-if="workflow.folderName" class="text-xs text-zinc-400 flex items-center gap-1 mt-0.5">
                          <span class="material-symbols-outlined text-xs">folder</span>
                          {{ workflow.folderName }}
                          <span v-if="workflow.inheritToSubfolders" class="text-purple-400 ml-1">(+ subfolders)</span>
                        </p>
                      </div>
                    </div>
                    <span
                      class="px-2 py-0.5 rounded-full text-[10px] font-semibold uppercase"
                      :class="workflow.isActive ? 'bg-emerald-500/20 text-emerald-400' : 'bg-zinc-500/20 text-zinc-400'"
                    >{{ workflow.isActive ? 'Active' : 'Inactive' }}</span>
                  </div>
                </div>

                <!-- Workflow body -->
                <div class="p-5 space-y-4">
                  <p v-if="workflow.description" class="text-sm text-zinc-600 dark:text-zinc-400">{{ workflow.description }}</p>

                  <!-- Info badges -->
                  <div class="flex flex-wrap gap-2">
                    <span class="inline-flex items-center gap-1 px-2.5 py-1 bg-zinc-100 dark:bg-zinc-800 rounded-lg text-xs font-medium text-zinc-600 dark:text-zinc-400">
                      <span class="material-symbols-outlined text-sm" :class="getTriggerConfig(workflow.triggerType).color">{{ getTriggerConfig(workflow.triggerType).icon }}</span>
                      {{ getTriggerConfig(workflow.triggerType).label }}
                    </span>
                    <span class="inline-flex items-center gap-1 px-2.5 py-1 bg-zinc-100 dark:bg-zinc-800 rounded-lg text-xs font-medium text-zinc-600 dark:text-zinc-400">
                      <span class="material-symbols-outlined text-sm text-indigo-500">{{ workflow.isSequential ? 'linear_scale' : 'call_split' }}</span>
                      {{ workflow.isSequential ? 'Sequential' : 'Parallel' }}
                    </span>
                    <span class="inline-flex items-center gap-1 px-2.5 py-1 bg-zinc-100 dark:bg-zinc-800 rounded-lg text-xs font-medium text-zinc-600 dark:text-zinc-400">
                      <span class="material-symbols-outlined text-sm text-teal">group</span>
                      {{ workflow.requiredApprovers }} required
                    </span>
                  </div>

                  <!-- Steps visualization -->
                  <div v-if="workflow.steps && workflow.steps.length > 0">
                    <p class="text-xs font-medium text-zinc-500 uppercase tracking-wider mb-2">Approval Steps</p>
                    <div class="flex items-center gap-1 flex-wrap">
                      <template v-for="(step, idx) in workflow.steps" :key="step.id">
                        <div class="flex items-center gap-2 px-3 py-2 bg-zinc-50 dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700">
                          <span class="flex-shrink-0 w-5 h-5 rounded-full bg-purple-100 dark:bg-purple-900/30 text-purple-600 dark:text-purple-400 text-[10px] font-bold flex items-center justify-center">
                            {{ step.stepOrder }}
                          </span>
                          <div class="min-w-0">
                            <p class="text-xs font-medium text-zinc-700 dark:text-zinc-300 truncate">
                              {{ step.approverUserName || step.approverRoleName || step.approverStructureName || 'Unassigned' }}
                            </p>
                            <p v-if="step.approverRoleName && !step.approverUserName" class="text-[10px] text-zinc-400">Role</p>
                            <p v-if="step.approverStructureName" class="text-[10px] text-zinc-400">
                              {{ step.assignToManager ? 'Manager' : 'Department' }}
                            </p>
                            <div v-if="step.statusName" class="flex items-center gap-1 mt-0.5">
                              <span class="w-1.5 h-1.5 rounded-full flex-shrink-0" :style="{ backgroundColor: step.statusColor || '#6366f1' }"></span>
                              <span class="text-[10px] font-medium" :style="{ color: step.statusColor || '#6366f1' }">{{ step.statusName }}</span>
                            </div>
                          </div>
                          <span
                            v-if="step.isRequired"
                            class="flex-shrink-0 w-4 h-4 rounded-full bg-rose-100 dark:bg-rose-900/30 text-rose-500 text-[8px] font-bold flex items-center justify-center"
                            title="Required"
                          >!</span>
                        </div>
                        <span
                          v-if="idx < workflow.steps.length - 1"
                          class="material-symbols-outlined text-zinc-300 dark:text-zinc-600 text-base"
                        >{{ workflow.isSequential ? 'arrow_forward' : 'commit' }}</span>
                      </template>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Review Action Modal -->
    <UiModal v-model="showActionModal" size="md">
      <template #header>
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 bg-indigo-500/30 backdrop-blur rounded-lg flex items-center justify-center">
            <span class="material-symbols-outlined text-white text-xl">rate_review</span>
          </div>
          <div>
            <h3 class="text-lg font-semibold text-white">Review Document</h3>
            <p class="text-sm text-zinc-300">{{ selectedRequest?.documentName }}</p>
          </div>
        </div>
      </template>

      <form v-if="selectedRequest" @submit.prevent="submitAction" class="space-y-5">
        <!-- Request info -->
        <div class="flex items-center gap-3 p-3 bg-zinc-50 dark:bg-zinc-800/50 rounded-lg">
          <span class="material-symbols-outlined text-zinc-400">person</span>
          <div class="text-sm">
            <span class="text-zinc-500">Requested by</span>
            <span class="font-medium text-zinc-800 dark:text-zinc-200 ml-1">{{ selectedRequest.requestedByName }}</span>
            <span class="text-zinc-400 ml-2">{{ timeAgo(selectedRequest.createdAt) }}</span>
          </div>
        </div>

        <!-- Decision cards -->
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-3">Your Decision</label>
          <div class="grid grid-cols-3 gap-3">
            <button
              type="button"
              @click="actionData.action = ApprovalActionTypes.Approved"
              class="relative flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition-all"
              :class="actionData.action === ApprovalActionTypes.Approved
                ? 'border-emerald-500 bg-emerald-50 dark:bg-emerald-900/20'
                : 'border-zinc-200 dark:border-zinc-700 hover:border-emerald-300'"
            >
              <span
                class="material-symbols-outlined text-2xl"
                :class="actionData.action === ApprovalActionTypes.Approved ? 'text-emerald-600' : 'text-zinc-400'"
                style="font-variation-settings: 'FILL' 1;"
              >check_circle</span>
              <span class="text-sm font-medium" :class="actionData.action === ApprovalActionTypes.Approved ? 'text-emerald-700 dark:text-emerald-400' : 'text-zinc-600 dark:text-zinc-400'">Approve</span>
              <div v-if="actionData.action === ApprovalActionTypes.Approved" class="absolute top-2 right-2 w-2 h-2 bg-emerald-500 rounded-full"></div>
            </button>

            <button
              type="button"
              @click="actionData.action = ApprovalActionTypes.Rejected"
              class="relative flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition-all"
              :class="actionData.action === ApprovalActionTypes.Rejected
                ? 'border-rose-500 bg-rose-50 dark:bg-rose-900/20'
                : 'border-zinc-200 dark:border-zinc-700 hover:border-rose-300'"
            >
              <span
                class="material-symbols-outlined text-2xl"
                :class="actionData.action === ApprovalActionTypes.Rejected ? 'text-rose-600' : 'text-zinc-400'"
                style="font-variation-settings: 'FILL' 1;"
              >cancel</span>
              <span class="text-sm font-medium" :class="actionData.action === ApprovalActionTypes.Rejected ? 'text-rose-700 dark:text-rose-400' : 'text-zinc-600 dark:text-zinc-400'">Reject</span>
              <div v-if="actionData.action === ApprovalActionTypes.Rejected" class="absolute top-2 right-2 w-2 h-2 bg-rose-500 rounded-full"></div>
            </button>

            <button
              type="button"
              @click="actionData.action = ApprovalActionTypes.ReturnedForRevision"
              class="relative flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition-all"
              :class="actionData.action === ApprovalActionTypes.ReturnedForRevision
                ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20'
                : 'border-zinc-200 dark:border-zinc-700 hover:border-amber-300'"
            >
              <span
                class="material-symbols-outlined text-2xl"
                :class="actionData.action === ApprovalActionTypes.ReturnedForRevision ? 'text-amber-600' : 'text-zinc-400'"
                style="font-variation-settings: 'FILL' 1;"
              >undo</span>
              <span class="text-sm font-medium" :class="actionData.action === ApprovalActionTypes.ReturnedForRevision ? 'text-amber-700 dark:text-amber-400' : 'text-zinc-600 dark:text-zinc-400'">Return</span>
              <div v-if="actionData.action === ApprovalActionTypes.ReturnedForRevision" class="absolute top-2 right-2 w-2 h-2 bg-amber-500 rounded-full"></div>
            </button>
          </div>
        </div>

        <!-- Comments -->
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">
            Comments
            <span class="text-rose-500 ml-0.5">*</span>
          </label>
          <textarea
            v-model="actionData.comments"
            rows="3"
            :placeholder="actionData.action === ApprovalActionTypes.Approved ? 'Add your approval comments...' : 'Please provide a reason...'"
            class="w-full px-4 py-3 bg-white dark:bg-zinc-800 border border-zinc-200 dark:border-zinc-700 rounded-lg text-sm text-zinc-800 dark:text-zinc-200 placeholder-zinc-400 focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all resize-none"
          ></textarea>
        </div>
      </form>

      <template #footer>
        <div class="flex items-center justify-between">
          <button
            @click="viewDocument(selectedRequest!.documentId)"
            class="inline-flex items-center gap-1.5 text-sm text-teal hover:text-teal/80 transition-colors"
          >
            <span class="material-symbols-outlined text-base">open_in_new</span>
            View Document
          </button>
          <div class="flex items-center gap-3">
            <button
              @click="showActionModal = false"
              class="px-4 py-2 text-sm font-medium text-zinc-600 dark:text-zinc-300 bg-zinc-100 dark:bg-zinc-700 rounded-lg hover:bg-zinc-200 dark:hover:bg-zinc-600 transition-colors"
            >
              Cancel
            </button>
            <button
              @click="submitAction"
              :disabled="isSubmitting || !actionData.comments.trim()"
              class="inline-flex items-center gap-2 px-5 py-2 text-sm font-medium text-white rounded-lg shadow-sm transition-all disabled:opacity-50 disabled:cursor-not-allowed"
              :class="{
                'bg-emerald-600 hover:bg-emerald-700 shadow-emerald-600/20': actionData.action === ApprovalActionTypes.Approved,
                'bg-rose-600 hover:bg-rose-700 shadow-rose-600/20': actionData.action === ApprovalActionTypes.Rejected,
                'bg-amber-600 hover:bg-amber-700 shadow-amber-600/20': actionData.action === ApprovalActionTypes.ReturnedForRevision
              }"
            >
              <DmsLoader v-if="isSubmitting" size="xs" />
              <span v-else class="material-symbols-outlined text-base" style="font-variation-settings: 'FILL' 1;">
                {{ actionData.action === ApprovalActionTypes.Approved ? 'check_circle' : actionData.action === ApprovalActionTypes.Rejected ? 'cancel' : 'undo' }}
              </span>
              {{ actionData.action === ApprovalActionTypes.Approved ? 'Approve' : actionData.action === ApprovalActionTypes.Rejected ? 'Reject' : 'Return for Revision' }}
            </button>
          </div>
        </div>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.list-enter-active,
.list-leave-active {
  transition: all 0.3s ease;
}
.list-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}
.list-leave-to {
  opacity: 0;
  transform: translateX(30px);
}
</style>
