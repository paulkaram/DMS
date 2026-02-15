<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { dashboardApi, activityLogsApi, approvalsApi } from '@/api/client'
import type { DashboardStatistics, RecentDocument, ActivityLog, ExpiredDocument, ApprovalRequest, ApprovalWorkflow } from '@/types'
import { ApprovalStatus } from '@/types'
import DocumentIcon from '@/components/common/DocumentIcon.vue'

const router = useRouter()
const authStore = useAuthStore()

const stats = ref<DashboardStatistics>({
  totalCabinets: 0,
  totalFolders: 0,
  totalDocuments: 0,
  totalUsers: 0,
  documentsThisMonth: 0,
  documentsThisYear: 0,
  myCheckoutsCount: 0,
  pendingApprovalsCount: 0,
  expiredDocumentsCount: 0,
  expiringSoonCount: 0
})
const recentDocuments = ref<RecentDocument[]>([])
const recentActivity = ref<ActivityLog[]>([])
const myCheckouts = ref<any[]>([])
const expiredDocuments = ref<ExpiredDocument[]>([])
const pendingApprovals = ref<ApprovalRequest[]>([])
const activeWorkflows = ref<ApprovalWorkflow[]>([])
const isLoading = ref(true)

onMounted(async () => {
  await loadDashboardData()
})

async function loadDashboardData() {
  isLoading.value = true
  try {
    const [statsResponse, recentDocsResponse, activityResponse, checkoutsResponse, expiredResponse, pendingResponse, myRequestsResponse, workflowsResponse] = await Promise.all([
      dashboardApi.getStatistics(),
      dashboardApi.getRecentDocuments(5),
      activityLogsApi.getMyActivity(1, 10),
      dashboardApi.getMyCheckouts(),
      dashboardApi.getExpiredDocuments(5),
      approvalsApi.getPendingRequests(5).catch(() => ({ data: [] })),
      approvalsApi.getMyRequests().catch(() => ({ data: [] })),
      approvalsApi.getWorkflows().catch(() => ({ data: [] }))
    ])

    stats.value = statsResponse.data
    recentDocuments.value = recentDocsResponse.data
    recentActivity.value = activityResponse.data.items
    myCheckouts.value = checkoutsResponse.data
    expiredDocuments.value = expiredResponse.data

    // Show pending requests assigned to me, or fall back to requests I submitted that are still pending
    const pending = pendingResponse.data as ApprovalRequest[]
    if (pending.length > 0) {
      pendingApprovals.value = pending
    } else {
      const myReqs = (myRequestsResponse.data as ApprovalRequest[]).filter(r => r.status === ApprovalStatus.Pending)
      pendingApprovals.value = myReqs.slice(0, 5)
    }

    activeWorkflows.value = (workflowsResponse.data as ApprovalWorkflow[]).filter(w => w.isActive && w.folderId)
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function navigateToExplorer() {
  router.push('/explorer')
}

function navigateToDocument(id: string) {
  router.push(`/documents/${id}`)
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function formatShortDate(dateString: string) {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
}

function getExpiryInfo(expiryDate: string) {
  const now = new Date()
  const expiry = new Date(expiryDate)
  const diffMs = expiry.getTime() - now.getTime()
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays <= 0) {
    return { label: 'Expired', class: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400' }
  }
  if (diffDays === 1) {
    return { label: 'Expires tomorrow', class: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400' }
  }
  return { label: `Expires in ${diffDays} days`, class: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400' }
}

const hasExpiryItems = computed(() => expiredDocuments.value.length > 0)
const hasPendingApprovals = computed(() => pendingApprovals.value.length > 0)

function getActivityIcon(action: string): string {
  const actionLower = action.toLowerCase()
  if (actionLower.includes('create') || actionLower.includes('upload')) return 'add_circle'
  if (actionLower.includes('update') || actionLower.includes('edit')) return 'edit'
  if (actionLower.includes('delete') || actionLower.includes('remove')) return 'delete'
  if (actionLower.includes('checkout') || actionLower.includes('lock')) return 'lock'
  if (actionLower.includes('checkin') || actionLower.includes('unlock')) return 'lock_open'
  if (actionLower.includes('discard')) return 'undo'
  if (actionLower.includes('download')) return 'download'
  if (actionLower.includes('view')) return 'visibility'
  if (actionLower.includes('move')) return 'drive_file_move'
  if (actionLower.includes('copy')) return 'content_copy'
  return 'history'
}

function getActivityBadgeClass(action: string): string {
  const actionLower = action.toLowerCase()
  if (actionLower.includes('create') || actionLower.includes('upload') || actionLower.includes('checkin')) {
    return 'bg-teal/15 text-teal'
  }
  if (actionLower.includes('update') || actionLower.includes('edit')) {
    return 'bg-zinc-200 dark:bg-border-dark text-zinc-700 dark:text-zinc-300'
  }
  if (actionLower.includes('delete') || actionLower.includes('remove')) {
    return 'bg-zinc-300 dark:bg-zinc-600 text-zinc-700 dark:text-zinc-300'
  }
  if (actionLower.includes('checkout') || actionLower.includes('discard')) {
    return 'bg-[#1e3a5f]/20 text-[#1e3a5f] dark:bg-border-dark dark:text-zinc-300'
  }
  if (actionLower.includes('download') || actionLower.includes('view')) {
    return 'bg-teal/10 text-teal'
  }
  return 'bg-zinc-100 dark:bg-border-dark text-zinc-600 dark:text-zinc-400'
}

function navigateToActivity(activity: ActivityLog) {
  if (!activity.nodeId) return
  const type = activity.nodeType?.toLowerCase()
  if (type === 'document') {
    router.push(`/documents/${activity.nodeId}`)
  } else if (type === 'folder' || type === 'cabinet') {
    router.push(`/explorer?folderId=${activity.nodeId}`)
  } else {
    router.push('/activity')
  }
}

const quickActions = [
  { label: 'Browse Documents', description: 'Explore cabinets and folders', icon: 'explore', route: '/explorer' },
  { label: 'Search Documents', description: 'Find files quickly', icon: 'search', route: '/search' },
  { label: 'My Approvals', description: 'Review pending requests', icon: 'task_alt', route: '/approvals' },
  { label: 'Activity Log', description: 'View recent actions', icon: 'history', route: '/activity' }
]
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">
        Welcome back, {{ authStore.user?.displayName || authStore.user?.username }}
      </h1>
      <p class="text-zinc-500 mt-1">Here's what's happening with your documents</p>
    </div>

    <!-- Stats Grid (unchanged) -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <!-- Total Documents Card -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">cloud_done</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Total Documents</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.totalDocuments }}</p>
          <div class="mt-4 w-full bg-zinc-800 h-1.5 rounded-full overflow-hidden">
            <div class="bg-teal h-full rounded-full" :style="{ width: Math.min(stats.documentsThisMonth * 10, 100) + '%' }"></div>
          </div>
          <p class="text-[10px] text-teal mt-2 font-medium">{{ stats.documentsThisMonth }} documents this month</p>
        </div>
      </div>

      <!-- Cabinets Card -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">inventory_2</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Cabinets</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.totalCabinets }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">trending_up</span>
            {{ stats.totalFolders }} folders total
          </p>
        </div>
      </div>

      <!-- Active Users Card -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">groups</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Active Users</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.totalUsers }}</p>
          <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
            <span class="material-symbols-outlined text-xs">bolt</span>
            Collaborating now
          </p>
        </div>
      </div>

      <!-- My Checkouts Card -->
      <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">edit_document</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">My Checkouts</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ stats.myCheckoutsCount }}</p>
          <p class="text-[10px] text-teal mt-4 font-medium">Files being edited</p>
        </div>
      </div>
    </div>

    <!-- Middle Section: 2-column layout -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Left Column (spans 2) -->
      <div class="lg:col-span-2 space-y-6">
        <!-- Quick Actions - 2x2 icon card grid -->
        <div class="bg-white dark:bg-zinc-900 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-800 overflow-hidden">
          <div class="p-5 border-b border-zinc-100 dark:border-zinc-800">
            <h3 class="text-[10px] font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-widest">Quick Actions</h3>
          </div>
          <div class="p-4">
            <div class="grid grid-cols-2 gap-3">
              <button
                v-for="action in quickActions"
                :key="action.route"
                @click="router.push(action.route)"
                class="flex items-center gap-3.5 p-4 rounded-xl border border-zinc-200 dark:border-zinc-800 hover:border-teal/40 hover:bg-teal/5 hover:-translate-y-0.5 transition-all duration-200 group text-left"
              >
                <div class="w-11 h-11 rounded-xl bg-teal/10 flex items-center justify-center flex-shrink-0 transition-transform group-hover:scale-110">
                  <span class="material-symbols-outlined text-xl text-teal">{{ action.icon }}</span>
                </div>
                <div class="min-w-0">
                  <p class="text-sm font-semibold text-zinc-800 dark:text-zinc-200 group-hover:text-teal transition-colors">{{ action.label }}</p>
                  <p class="text-[11px] text-zinc-400 dark:text-zinc-500 mt-0.5">{{ action.description }}</p>
                </div>
              </button>
            </div>
          </div>
        </div>

        <!-- Pending Approvals Widget (always visible) -->
        <div class="bg-white dark:bg-zinc-900 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-800 overflow-hidden">
          <div class="p-5 border-b border-zinc-100 dark:border-zinc-800 flex justify-between items-center">
            <div class="flex items-center gap-2.5">
              <h3 class="text-[10px] font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-widest">Pending Approvals</h3>
              <span
                v-if="pendingApprovals.length > 0"
                class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-teal/15 text-teal"
              >
                {{ pendingApprovals.length }}
              </span>
            </div>
            <button @click="router.push('/approvals')" class="text-[10px] font-bold text-teal uppercase hover:underline tracking-wider">View All</button>
          </div>
          <div v-if="pendingApprovals.length === 0" class="p-8 text-center">
            <div class="w-14 h-14 bg-teal/10 rounded-full flex items-center justify-center mx-auto mb-3">
              <span class="material-symbols-outlined text-2xl text-teal">task_alt</span>
            </div>
            <p class="text-zinc-500 font-medium text-sm">No pending approvals</p>
            <p class="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-widest mt-1">You're all caught up!</p>
          </div>
          <div v-else class="divide-y divide-zinc-100 dark:divide-zinc-800">
            <div
              v-for="approval in pendingApprovals"
              :key="approval.id"
              @click="navigateToDocument(approval.documentId)"
              class="p-4 hover:bg-teal/5 cursor-pointer transition-all group flex items-center gap-4"
            >
              <div class="w-10 h-10 rounded-full bg-teal/10 flex items-center justify-center flex-shrink-0">
                <span class="material-symbols-outlined text-teal">pending_actions</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-semibold text-zinc-800 dark:text-zinc-200 truncate group-hover:text-teal transition-colors">{{ approval.documentName }}</p>
                <p class="text-[11px] text-zinc-400 dark:text-zinc-500 mt-0.5">
                  {{ approval.workflowName }} &middot; Submitted {{ formatShortDate(approval.createdAt) }}
                </p>
              </div>
              <span class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-teal/15 text-teal">Pending</span>
            </div>
          </div>
        </div>

        <!-- Active Workflows on Folders -->
        <div class="bg-white dark:bg-zinc-900 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-800 overflow-hidden">
          <div class="p-5 border-b border-zinc-100 dark:border-zinc-800 flex justify-between items-center">
            <div class="flex items-center gap-2.5">
              <h3 class="text-[10px] font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-widest">Active Workflows</h3>
              <span
                v-if="activeWorkflows.length > 0"
                class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-teal/15 text-teal"
              >
                {{ activeWorkflows.length }}
              </span>
            </div>
            <button @click="router.push('/admin/workflow-designer')" class="text-[10px] font-bold text-teal uppercase hover:underline tracking-wider">Manage</button>
          </div>
          <div v-if="activeWorkflows.length === 0" class="p-8 text-center">
            <div class="w-14 h-14 bg-teal/10 rounded-full flex items-center justify-center mx-auto mb-3">
              <span class="material-symbols-outlined text-2xl text-teal">account_tree</span>
            </div>
            <p class="text-zinc-500 font-medium text-sm">No active workflows</p>
            <p class="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-widest mt-1">Assign workflows to folders to see them here</p>
          </div>
          <div v-else class="divide-y divide-zinc-100 dark:divide-zinc-800">
            <div
              v-for="workflow in activeWorkflows"
              :key="workflow.id"
              @click="router.push('/admin/workflow-designer')"
              class="p-4 hover:bg-teal/5 cursor-pointer transition-all group flex items-center gap-4"
            >
              <div class="w-10 h-10 rounded-xl bg-teal/10 flex items-center justify-center flex-shrink-0">
                <span class="material-symbols-outlined text-teal">account_tree</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-semibold text-zinc-800 dark:text-zinc-200 truncate group-hover:text-teal transition-colors">{{ workflow.name }}</p>
                <div class="flex items-center gap-2 mt-0.5">
                  <span class="text-[11px] text-zinc-400 dark:text-zinc-500 flex items-center gap-1">
                    <span class="material-symbols-outlined text-xs text-teal">folder</span>
                    {{ workflow.folderName || 'Unknown folder' }}
                  </span>
                  <span v-if="workflow.inheritToSubfolders" class="text-[10px] text-teal/60 flex items-center gap-0.5">
                    <span class="material-symbols-outlined text-[10px]">subdirectory_arrow_right</span>
                    Inherited
                  </span>
                </div>
              </div>
              <div class="flex flex-col items-end gap-1 flex-shrink-0">
                <span
                  class="px-2 py-0.5 text-[10px] font-bold rounded-full"
                  :class="workflow.triggerType === 'OnUpload' ? 'bg-teal/15 text-teal' : 'bg-zinc-100 dark:bg-zinc-800 text-zinc-500 dark:text-zinc-400'"
                >
                  {{ workflow.triggerType === 'OnUpload' ? 'Auto-trigger' : 'Manual' }}
                </span>
                <span class="text-[10px] text-zinc-400 dark:text-zinc-500">
                  {{ workflow.isSequential ? 'Sequential' : 'Parallel' }} &middot; {{ workflow.requiredApprovers }} approver{{ workflow.requiredApprovers !== 1 ? 's' : '' }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right Column -->
      <div class="space-y-6">
        <!-- Expired & Expiring Documents Widget -->
        <div class="bg-white dark:bg-zinc-900 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-800 overflow-hidden h-full flex flex-col">
          <div class="p-5 border-b border-zinc-100 dark:border-zinc-800">
            <div class="flex items-center gap-2 flex-wrap">
              <h3 class="text-[10px] font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-widest">Expiry Alerts</h3>
              <span
                v-if="stats.expiredDocumentsCount > 0"
                class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-red-50 text-red-600 dark:bg-red-500/15 dark:text-red-400"
              >
                {{ stats.expiredDocumentsCount }} expired
              </span>
              <span
                v-if="stats.expiringSoonCount > 0"
                class="px-2 py-0.5 text-[10px] font-bold rounded-full bg-amber-50 text-amber-600 dark:bg-amber-500/15 dark:text-amber-400"
              >
                {{ stats.expiringSoonCount }} soon
              </span>
            </div>
          </div>

          <div v-if="isLoading" class="p-4 space-y-3 flex-1">
            <div v-for="i in 3" :key="i" class="animate-pulse flex items-center gap-3 p-2">
              <div class="w-10 h-10 bg-zinc-100 dark:bg-zinc-800 rounded-lg"></div>
              <div class="flex-1">
                <div class="h-4 bg-zinc-100 dark:bg-zinc-800 rounded w-3/4 mb-2"></div>
                <div class="h-3 bg-zinc-100 dark:bg-zinc-800 rounded w-1/2"></div>
              </div>
            </div>
          </div>

          <div v-else-if="!hasExpiryItems" class="flex-1 flex flex-col items-center justify-center p-8 text-center">
            <div class="w-16 h-16 bg-teal/10 rounded-full flex items-center justify-center mb-3">
              <span class="material-symbols-outlined text-3xl text-teal">verified</span>
            </div>
            <p class="text-zinc-500 font-medium">No expiry alerts</p>
            <p class="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-widest mt-1">All documents are current</p>
          </div>

          <div v-else class="divide-y divide-zinc-100 dark:divide-zinc-800 flex-1">
            <div
              v-for="doc in expiredDocuments"
              :key="doc.id"
              @click="navigateToDocument(doc.id)"
              class="p-4 hover:bg-teal/5 cursor-pointer transition-all group flex items-center gap-3"
            >
              <div
                class="w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0"
                :class="getExpiryInfo(doc.expiryDate).label === 'Expired' ? 'bg-red-50 dark:bg-red-500/15' : 'bg-amber-50 dark:bg-amber-500/15'"
              >
                <span
                  class="material-symbols-outlined text-lg"
                  :class="getExpiryInfo(doc.expiryDate).label === 'Expired' ? 'text-red-500 dark:text-red-400' : 'text-amber-500 dark:text-amber-400'"
                >
                  {{ getExpiryInfo(doc.expiryDate).label === 'Expired' ? 'event_busy' : 'schedule' }}
                </span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-zinc-800 dark:text-zinc-200 truncate group-hover:text-teal transition-colors">{{ doc.name }}</p>
                <p class="text-[11px] text-zinc-400 dark:text-zinc-500 mt-0.5">{{ formatShortDate(doc.expiryDate) }}</p>
              </div>
              <span
                class="px-2 py-0.5 text-[10px] font-bold rounded-full flex-shrink-0"
                :class="getExpiryInfo(doc.expiryDate).label === 'Expired' ? 'bg-red-50 text-red-600 dark:bg-red-500/15 dark:text-red-400' : 'bg-amber-50 text-amber-600 dark:bg-amber-500/15 dark:text-amber-400'"
              >
                {{ getExpiryInfo(doc.expiryDate).label }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Bottom Section: Recent Docs + My Checkouts (full-width 2-col) -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Recent Documents -->
      <div class="bg-white dark:bg-zinc-900 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-800 overflow-hidden">
        <div class="p-5 border-b border-zinc-100 dark:border-zinc-800 flex justify-between items-center">
          <h3 class="text-[10px] font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-widest">Recent Documents</h3>
          <button @click="navigateToExplorer" class="text-[10px] font-bold text-teal uppercase hover:underline tracking-wider">View All</button>
        </div>
        <div v-if="isLoading" class="p-4 space-y-3">
          <div v-for="i in 3" :key="i" class="animate-pulse flex items-center gap-3 p-2">
            <div class="w-10 h-10 bg-zinc-100 dark:bg-zinc-800 rounded-lg"></div>
            <div class="flex-1">
              <div class="h-4 bg-zinc-100 dark:bg-zinc-800 rounded w-3/4 mb-2"></div>
              <div class="h-3 bg-zinc-100 dark:bg-zinc-800 rounded w-1/2"></div>
            </div>
          </div>
        </div>
        <div v-else-if="recentDocuments.length === 0" class="p-8 text-center">
          <div class="w-16 h-16 bg-zinc-100 dark:bg-zinc-800 rounded-full flex items-center justify-center mx-auto mb-3">
            <span class="material-symbols-outlined text-3xl text-zinc-400 dark:text-zinc-600">description</span>
          </div>
          <p class="text-zinc-500 font-medium">No recent documents</p>
          <p class="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-widest mt-1">Upload files to see them here</p>
        </div>
        <div v-else class="divide-y divide-zinc-100 dark:divide-zinc-800">
          <div
            v-for="(doc, index) in recentDocuments"
            :key="doc.id"
            @click="navigateToDocument(doc.id)"
            class="p-4 hover:bg-teal/5 cursor-pointer transition-all group flex items-center gap-4"
          >
            <DocumentIcon :extension="doc.extension" :index="index" size="md" />
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-zinc-800 dark:text-zinc-200 truncate group-hover:text-teal transition-colors">{{ doc.name }}</p>
              <p class="text-[10px] text-zinc-400 dark:text-zinc-500 mt-0.5">{{ formatDate(doc.createdAt) }}</p>
            </div>
            <span class="material-symbols-outlined text-zinc-300 dark:text-zinc-600 group-hover:text-teal transition-colors">
              chevron_right
            </span>
          </div>
        </div>
      </div>

      <!-- My Checkouts -->
      <div class="bg-white dark:bg-zinc-900 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-800 overflow-hidden h-full flex flex-col">
        <div class="p-5 border-b border-zinc-100 dark:border-zinc-800">
          <h3 class="text-[10px] font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-widest">My Checked Out Files</h3>
        </div>
        <div v-if="isLoading" class="p-4 space-y-3 flex-1">
          <div v-for="i in 3" :key="i" class="animate-pulse flex items-center gap-3 p-2">
            <div class="w-10 h-10 bg-zinc-100 dark:bg-zinc-800 rounded-lg"></div>
            <div class="flex-1">
              <div class="h-4 bg-zinc-100 dark:bg-zinc-800 rounded w-3/4 mb-2"></div>
              <div class="h-3 bg-zinc-100 dark:bg-zinc-800 rounded w-1/2"></div>
            </div>
          </div>
        </div>
        <div v-else-if="myCheckouts.length === 0" class="flex-1 flex flex-col items-center justify-center p-8 text-center">
          <div class="w-20 h-20 bg-zinc-100 dark:bg-zinc-800 rounded-full flex items-center justify-center mb-4">
            <span class="material-symbols-outlined text-4xl text-teal">check_circle</span>
          </div>
          <p class="text-zinc-500 font-medium">No checked out files</p>
          <p class="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-widest mt-1">You're all caught up!</p>
        </div>
        <div v-else class="divide-y divide-zinc-100 dark:divide-zinc-800 flex-1">
          <div
            v-for="(doc, index) in myCheckouts"
            :key="doc.id"
            @click="navigateToDocument(doc.id)"
            class="p-4 hover:bg-teal/5 cursor-pointer transition-all group flex items-center gap-4"
          >
            <div class="w-10 h-10 bg-teal/10 rounded-full flex items-center justify-center flex-shrink-0">
              <span class="material-symbols-outlined text-teal">edit_document</span>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-zinc-800 dark:text-zinc-200 truncate group-hover:text-teal transition-colors">{{ doc.name }}</p>
              <p class="text-[10px] text-teal mt-0.5">Checked out {{ formatDate(doc.checkedOutAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Recent Activity -->
    <div class="bg-white dark:bg-zinc-900 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-800 overflow-hidden">
      <div class="p-5 border-b border-zinc-100 dark:border-zinc-800 flex justify-between items-center">
        <h3 class="text-[10px] font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-widest">Recent Activity</h3>
        <button @click="router.push('/activity')" class="text-[10px] font-bold text-teal uppercase hover:underline tracking-wider">View All</button>
      </div>

      <div v-if="isLoading" class="p-6 space-y-4">
        <div v-for="i in 5" :key="i" class="animate-pulse flex items-center gap-4">
          <div class="w-10 h-10 bg-zinc-100 dark:bg-zinc-800 rounded-lg"></div>
          <div class="flex-1">
            <div class="h-4 bg-zinc-100 dark:bg-zinc-800 rounded w-1/2 mb-2"></div>
            <div class="h-3 bg-zinc-100 dark:bg-zinc-800 rounded w-1/4"></div>
          </div>
        </div>
      </div>

      <div v-else-if="recentActivity.length === 0" class="p-12 text-center">
        <div class="w-20 h-20 bg-zinc-100 dark:bg-zinc-800 rounded-full flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-4xl text-zinc-400 dark:text-zinc-600">history</span>
        </div>
        <p class="text-zinc-500 font-medium">No recent activity</p>
        <p class="text-[10px] text-zinc-400 dark:text-zinc-600 uppercase tracking-widest mt-1">Start working to see activity here</p>
      </div>

      <div v-else class="divide-y divide-zinc-100 dark:divide-zinc-800">
        <div
          v-for="(activity, index) in recentActivity"
          :key="activity.id"
          class="px-6 py-4 hover:bg-teal/5 transition-all cursor-pointer group flex items-center gap-4"
          @click="navigateToActivity(activity)"
        >
          <DocumentIcon :icon="getActivityIcon(activity.action)" :index="index" size="md" />
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
              <span class="px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider rounded-full bg-teal/15 text-teal">
                {{ activity.action }}
              </span>
              <span v-if="activity.nodeName" class="text-sm text-zinc-700 dark:text-zinc-300 font-medium truncate group-hover:text-teal transition-colors">
                {{ activity.nodeName }}
              </span>
            </div>
            <p class="text-[10px] text-zinc-400 dark:text-zinc-500 mt-1">{{ formatDate(activity.createdAt) }}</p>
          </div>
          <span class="material-symbols-outlined text-zinc-300 dark:text-zinc-600 group-hover:text-teal group-hover:translate-x-1 transition-all">
            chevron_right
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
