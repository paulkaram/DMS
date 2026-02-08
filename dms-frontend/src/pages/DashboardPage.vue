<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { dashboardApi, activityLogsApi } from '@/api/client'
import type { DashboardStatistics, RecentDocument, ActivityLog } from '@/types'
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
  myCheckoutsCount: 0
})
const recentDocuments = ref<RecentDocument[]>([])
const recentActivity = ref<ActivityLog[]>([])
const myCheckouts = ref<any[]>([])
const isLoading = ref(true)

onMounted(async () => {
  await loadDashboardData()
})

async function loadDashboardData() {
  isLoading.value = true
  try {
    const [statsResponse, recentDocsResponse, activityResponse, checkoutsResponse] = await Promise.all([
      dashboardApi.getStatistics(),
      dashboardApi.getRecentDocuments(5),
      activityLogsApi.getMyActivity(0, 10),
      dashboardApi.getMyCheckouts()
    ])

    stats.value = statsResponse.data
    recentDocuments.value = recentDocsResponse.data
    recentActivity.value = activityResponse.data
    myCheckouts.value = checkoutsResponse.data
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

function formatFileSize(bytes: number) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

function getActivityStyle(action: string) {
  const actionLower = action.toLowerCase()

  if (actionLower.includes('create') || actionLower.includes('upload')) {
    return {
      bg: 'bg-emerald-500/10',
      icon: 'text-emerald-400',
      iconName: 'add_circle',
      badge: 'bg-emerald-500/20 text-emerald-400'
    }
  }
  if (actionLower.includes('update') || actionLower.includes('edit') || actionLower.includes('modify')) {
    return {
      bg: 'bg-blue-500/10',
      icon: 'text-blue-400',
      iconName: 'edit',
      badge: 'bg-blue-500/20 text-blue-400'
    }
  }
  if (actionLower.includes('delete') || actionLower.includes('remove')) {
    return {
      bg: 'bg-red-500/10',
      icon: 'text-red-400',
      iconName: 'delete',
      badge: 'bg-red-500/20 text-red-400'
    }
  }
  if (actionLower.includes('checkout') || actionLower.includes('lock')) {
    return {
      bg: 'bg-amber-500/10',
      icon: 'text-amber-400',
      iconName: 'lock',
      badge: 'bg-amber-500/20 text-amber-400'
    }
  }
  if (actionLower.includes('checkin') || actionLower.includes('unlock')) {
    return {
      bg: 'bg-teal/10',
      icon: 'text-teal',
      iconName: 'lock_open',
      badge: 'bg-teal/20 text-teal'
    }
  }
  if (actionLower.includes('discard')) {
    return {
      bg: 'bg-slate-500/10',
      icon: 'text-slate-400',
      iconName: 'undo',
      badge: 'bg-slate-500/20 text-slate-400'
    }
  }
  if (actionLower.includes('download') || actionLower.includes('view')) {
    return {
      bg: 'bg-purple-500/10',
      icon: 'text-purple-400',
      iconName: 'download',
      badge: 'bg-purple-500/20 text-purple-400'
    }
  }
  if (actionLower.includes('move') || actionLower.includes('copy')) {
    return {
      bg: 'bg-indigo-500/10',
      icon: 'text-indigo-400',
      iconName: 'drive_file_move',
      badge: 'bg-indigo-500/20 text-indigo-400'
    }
  }

  // Default
  return {
    bg: 'bg-teal/10',
    icon: 'text-teal',
    iconName: 'info',
    badge: 'bg-teal/20 text-teal'
  }
}

function getActivityStyleLight(action: string) {
  const actionLower = action.toLowerCase()

  if (actionLower.includes('create') || actionLower.includes('upload')) {
    return {
      bg: 'bg-emerald-50 dark:bg-emerald-900/20',
      icon: 'text-emerald-500',
      iconName: 'add_circle',
      text: 'text-emerald-600'
    }
  }
  if (actionLower.includes('update') || actionLower.includes('edit') || actionLower.includes('modify')) {
    return {
      bg: 'bg-blue-50 dark:bg-blue-900/20',
      icon: 'text-blue-500',
      iconName: 'edit',
      text: 'text-blue-600'
    }
  }
  if (actionLower.includes('delete') || actionLower.includes('remove')) {
    return {
      bg: 'bg-red-50 dark:bg-red-900/20',
      icon: 'text-red-500',
      iconName: 'delete',
      text: 'text-red-600'
    }
  }
  if (actionLower.includes('checkout') || actionLower.includes('lock')) {
    return {
      bg: 'bg-amber-50 dark:bg-amber-900/20',
      icon: 'text-amber-500',
      iconName: 'lock',
      text: 'text-amber-600'
    }
  }
  if (actionLower.includes('checkin') || actionLower.includes('unlock')) {
    return {
      bg: 'bg-teal/10',
      icon: 'text-teal',
      iconName: 'lock_open',
      text: 'text-teal'
    }
  }
  if (actionLower.includes('discard')) {
    return {
      bg: 'bg-slate-100 dark:bg-slate-800',
      icon: 'text-slate-500',
      iconName: 'undo',
      text: 'text-slate-600'
    }
  }
  if (actionLower.includes('download') || actionLower.includes('view')) {
    return {
      bg: 'bg-purple-50 dark:bg-purple-900/20',
      icon: 'text-purple-500',
      iconName: 'download',
      text: 'text-purple-600'
    }
  }
  if (actionLower.includes('move') || actionLower.includes('copy')) {
    return {
      bg: 'bg-indigo-50 dark:bg-indigo-900/20',
      icon: 'text-indigo-500',
      iconName: 'drive_file_move',
      text: 'text-indigo-600'
    }
  }

  // Default - teal/cyan accent
  return {
    bg: 'bg-teal/10',
    icon: 'text-teal',
    iconName: 'info',
    text: 'text-teal'
  }
}

// Activity action icons
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

// Activity badge classes
function getActivityBadgeClass(action: string): string {
  const actionLower = action.toLowerCase()
  if (actionLower.includes('create') || actionLower.includes('upload') || actionLower.includes('checkin')) {
    return 'bg-teal/15 text-teal'
  }
  if (actionLower.includes('update') || actionLower.includes('edit')) {
    return 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300'
  }
  if (actionLower.includes('delete') || actionLower.includes('remove')) {
    return 'bg-slate-300 dark:bg-slate-600 text-slate-700 dark:text-slate-300'
  }
  if (actionLower.includes('checkout') || actionLower.includes('discard')) {
    return 'bg-[#1e3a5f]/20 text-[#1e3a5f] dark:bg-slate-700 dark:text-slate-300'
  }
  if (actionLower.includes('download') || actionLower.includes('view')) {
    return 'bg-teal/10 text-teal'
  }
  return 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400'
}
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-slate-900 dark:text-slate-100">
        Welcome back, {{ authStore.user?.displayName || authStore.user?.username }}
      </h1>
      <p class="text-slate-500 mt-1">Here's what's happening with your documents</p>
    </div>

    <!-- Stats Grid -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <!-- Total Documents Card -->
      <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
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
      <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
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
      <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
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
      <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
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

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Quick Actions -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="p-6 border-b border-slate-100 dark:border-slate-800">
          <h3 class="text-sm font-bold text-slate-800 dark:text-white uppercase tracking-wider">Quick Actions</h3>
        </div>
        <div class="p-4 space-y-2">
          <button
            @click="navigateToExplorer"
            class="w-full flex items-center gap-4 px-4 py-3 rounded-xl text-slate-600 dark:text-slate-300 hover:bg-teal/10 hover:text-teal transition-all group"
          >
            <span class="material-symbols-outlined text-xl text-teal">explore</span>
            <span class="text-sm font-semibold">Browse Documents</span>
          </button>
          <button
            @click="router.push('/search')"
            class="w-full flex items-center gap-4 px-4 py-3 rounded-xl text-slate-600 dark:text-slate-300 hover:bg-teal/10 hover:text-teal transition-all group"
          >
            <span class="material-symbols-outlined text-xl text-teal">search</span>
            <span class="text-sm font-semibold">Search Documents</span>
          </button>
          <button
            @click="router.push('/activity')"
            class="w-full flex items-center gap-4 px-4 py-3 rounded-xl text-slate-600 dark:text-slate-300 hover:bg-teal/10 hover:text-teal transition-all group"
          >
            <span class="material-symbols-outlined text-xl text-teal">history</span>
            <span class="text-sm font-semibold">View Activity</span>
          </button>
        </div>
      </div>

      <!-- Recent Documents -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="p-6 border-b border-slate-100 dark:border-slate-800 flex justify-between items-center">
          <h3 class="text-sm font-bold text-slate-800 dark:text-white uppercase tracking-wider">Recent Documents</h3>
          <button @click="navigateToExplorer" class="text-[10px] font-bold text-teal uppercase hover:underline tracking-wider">View All</button>
        </div>
        <div v-if="isLoading" class="p-4 space-y-3">
          <div v-for="i in 3" :key="i" class="animate-pulse flex items-center gap-3 p-2">
            <div class="w-10 h-10 bg-slate-200 dark:bg-slate-700 rounded-lg"></div>
            <div class="flex-1">
              <div class="h-4 bg-slate-200 dark:bg-slate-700 rounded w-3/4 mb-2"></div>
              <div class="h-3 bg-slate-200 dark:bg-slate-700 rounded w-1/2"></div>
            </div>
          </div>
        </div>
        <div v-else-if="recentDocuments.length === 0" class="p-8 text-center">
          <div class="w-16 h-16 bg-slate-50 dark:bg-slate-800 rounded-full flex items-center justify-center mx-auto mb-3">
            <span class="material-symbols-outlined text-3xl text-slate-300 dark:text-slate-600">description</span>
          </div>
          <p class="text-slate-400 font-medium">No recent documents</p>
          <p class="text-[10px] text-slate-400 uppercase tracking-widest mt-1">Upload files to see them here</p>
        </div>
        <div v-else class="divide-y divide-slate-100 dark:divide-slate-800">
          <div
            v-for="(doc, index) in recentDocuments"
            :key="doc.id"
            @click="navigateToDocument(doc.id)"
            class="p-4 hover:bg-slate-50 dark:hover:bg-slate-800/50 cursor-pointer transition-all group flex items-center gap-4"
          >
            <DocumentIcon :extension="doc.extension" :index="index" size="md" />
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-slate-800 dark:text-slate-200 truncate group-hover:text-teal transition-colors">{{ doc.name }}</p>
              <p class="text-[10px] text-slate-400 mt-0.5">{{ formatDate(doc.createdAt) }}</p>
            </div>
            <span class="material-symbols-outlined text-slate-300 group-hover:text-teal transition-colors">
              chevron_right
            </span>
          </div>
        </div>
      </div>

      <!-- My Checkouts -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden h-full flex flex-col">
        <div class="p-6 border-b border-slate-100 dark:border-slate-800">
          <h3 class="text-sm font-bold text-slate-800 dark:text-white uppercase tracking-wider">My Checked Out Files</h3>
        </div>
        <div v-if="isLoading" class="p-4 space-y-3 flex-1">
          <div v-for="i in 3" :key="i" class="animate-pulse flex items-center gap-3 p-2">
            <div class="w-10 h-10 bg-slate-200 dark:bg-slate-700 rounded-lg"></div>
            <div class="flex-1">
              <div class="h-4 bg-slate-200 dark:bg-slate-700 rounded w-3/4 mb-2"></div>
              <div class="h-3 bg-slate-200 dark:bg-slate-700 rounded w-1/2"></div>
            </div>
          </div>
        </div>
        <div v-else-if="myCheckouts.length === 0" class="flex-1 flex flex-col items-center justify-center p-8 text-center">
          <div class="w-20 h-20 bg-slate-50 dark:bg-slate-800 rounded-full flex items-center justify-center mb-4">
            <span class="material-symbols-outlined text-4xl text-slate-300 dark:text-slate-700">check_circle</span>
          </div>
          <p class="text-slate-400 font-medium">No checked out files</p>
          <p class="text-[10px] text-slate-400 uppercase tracking-widest mt-1">You're all caught up!</p>
        </div>
        <div v-else class="divide-y divide-slate-100 dark:divide-slate-800 flex-1">
          <div
            v-for="(doc, index) in myCheckouts"
            :key="doc.id"
            @click="navigateToDocument(doc.id)"
            class="p-4 hover:bg-slate-50 dark:hover:bg-slate-800/50 cursor-pointer transition-all group flex items-center gap-4"
          >
            <div class="w-10 h-10 bg-[#1e3a5f] rounded-full flex items-center justify-center flex-shrink-0">
              <span class="material-symbols-outlined text-white">edit_document</span>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-slate-800 dark:text-slate-200 truncate">{{ doc.name }}</p>
              <p class="text-[10px] text-teal mt-0.5">Checked out {{ formatDate(doc.checkedOutAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Recent Activity -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
      <div class="p-6 border-b border-slate-100 dark:border-slate-800 flex justify-between items-center">
        <h3 class="text-sm font-bold text-slate-800 dark:text-white uppercase tracking-wider">Recent Activity</h3>
        <button @click="router.push('/activity')" class="text-[10px] font-bold text-teal uppercase hover:underline tracking-wider">View All</button>
      </div>

      <div v-if="isLoading" class="p-6 space-y-4">
        <div v-for="i in 5" :key="i" class="animate-pulse flex items-center gap-4">
          <div class="w-10 h-10 bg-slate-200 dark:bg-slate-700 rounded-lg"></div>
          <div class="flex-1">
            <div class="h-4 bg-slate-200 dark:bg-slate-700 rounded w-1/2 mb-2"></div>
            <div class="h-3 bg-slate-200 dark:bg-slate-700 rounded w-1/4"></div>
          </div>
        </div>
      </div>

      <div v-else-if="recentActivity.length === 0" class="p-12 text-center">
        <div class="w-20 h-20 bg-slate-50 dark:bg-slate-800 rounded-full flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-4xl text-slate-300 dark:text-slate-700">history</span>
        </div>
        <p class="text-slate-400 font-medium">No recent activity</p>
        <p class="text-[10px] text-slate-400 uppercase tracking-widest mt-1">Start working to see activity here</p>
      </div>

      <div v-else class="divide-y divide-slate-100 dark:divide-slate-800">
        <div
          v-for="(activity, index) in recentActivity"
          :key="activity.id"
          class="px-6 py-4 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-all cursor-pointer group flex items-center gap-4"
        >
          <!-- Icon -->
          <DocumentIcon :icon="getActivityIcon(activity.action)" :index="index" size="md" />

          <!-- Content -->
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
              <span
                class="px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider rounded-full"
                :class="getActivityBadgeClass(activity.action)"
              >
                {{ activity.action }}
              </span>
              <span v-if="activity.nodeName" class="text-sm text-slate-700 dark:text-slate-200 font-medium truncate group-hover:text-teal transition-colors">
                {{ activity.nodeName }}
              </span>
            </div>
            <p class="text-[10px] text-slate-400 mt-1">{{ formatDate(activity.createdAt) }}</p>
          </div>

          <!-- Arrow indicator -->
          <span class="material-symbols-outlined text-slate-300 group-hover:text-teal group-hover:translate-x-1 transition-all">
            chevron_right
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
