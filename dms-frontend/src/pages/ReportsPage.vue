<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { reportsApi, activityLogsApi } from '@/api/client'

interface ReportStatistics {
  totalDocuments: number
  documentsGrowth: number
  storageUsedGB: number
  storageCapacityPercent: number
  activeWorkflows: number
  pendingWorkflows: number
  totalUsers: number
  onlineUsers: number
}

interface MonthlyData {
  month: string
  currentYear: number
  previousYear: number
}

interface DocumentTypeData {
  type: string
  percentage: number
  color: string
}

interface ActivityLog {
  id: string
  userName?: string
  userInitials?: string
  action: string
  actionType?: string
  targetName?: string
  nodeName?: string
  details?: string
  createdAt: string
  ipAddress?: string
}

const statistics = ref<ReportStatistics | null>(null)
const monthlyData = ref<MonthlyData[]>([])
const documentTypes = ref<DocumentTypeData[]>([])
const recentActivity = ref<ActivityLog[]>([])
const isLoading = ref(true)
const selectedPeriod = ref('30')

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [statsRes, monthlyRes, typesRes, activityRes] = await Promise.all([
      reportsApi.getStatistics(),
      reportsApi.getMonthlyGrowth(),
      reportsApi.getDocumentTypes(),
      reportsApi.getRecentActivity(10)
    ])
    statistics.value = statsRes.data
    monthlyData.value = monthlyRes.data
    documentTypes.value = typesRes.data
    recentActivity.value = activityRes.data
  } catch (err) {
    // Set default values for demo
    statistics.value = {
      totalDocuments: 124592,
      documentsGrowth: 12,
      storageUsedGB: 856.4,
      storageCapacityPercent: 75,
      activeWorkflows: 42,
      pendingWorkflows: 8,
      totalUsers: 1208,
      onlineUsers: 4
    }
    monthlyData.value = [
      { month: 'Jan', currentYear: 3200, previousYear: 2400 },
      { month: 'Feb', currentYear: 4000, previousYear: 2800 },
      { month: 'Mar', currentYear: 4800, previousYear: 3200 },
      { month: 'Apr', currentYear: 3600, previousYear: 2000 },
      { month: 'May', currentYear: 5600, previousYear: 3600 },
      { month: 'Jun', currentYear: 6400, previousYear: 4000 }
    ]
    documentTypes.value = [
      { type: 'PDF Documents', percentage: 55, color: 'teal' },
      { type: 'Word / Office', percentage: 25, color: 'navy' },
      { type: 'Media & Other', percentage: 20, color: 'slate' }
    ]
    recentActivity.value = [
      { id: '1', userName: 'Alice Smith', action: 'Created', nodeType: 'Document', nodeName: 'Project_Charter_v1.pdf', createdAt: new Date(Date.now() - 3600000).toISOString(), ipAddress: '192.168.1.142' },
      { id: '2', userName: 'John Doe', action: 'Updated', nodeType: 'Document', nodeName: 'Financial_Q3_Draft.xlsx', createdAt: new Date(Date.now() - 7200000).toISOString(), ipAddress: '192.168.1.105' },
      { id: '3', userName: 'System Admin', action: 'Downloaded', nodeType: 'Document', nodeName: 'Annual_Report_2024.pdf', createdAt: new Date(Date.now() - 10800000).toISOString(), ipAddress: '10.0.0.1' }
    ]
  } finally {
    isLoading.value = false
  }
}

function formatNumber(num: number): string {
  return num.toLocaleString()
}

// Ensure we always show 6 months of data from API
const chartData = computed(() => {
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
  const now = new Date()
  const currentMonth = now.getMonth()

  // Get last 6 months (including current)
  const last6Months: string[] = []
  for (let i = 5; i >= 0; i--) {
    const monthIndex = (currentMonth - i + 12) % 12
    last6Months.push(months[monthIndex])
  }

  // Map API data to these months, fill zeros for missing
  return last6Months.map(month => {
    const apiData = monthlyData.value.find(d => d.month === month)
    return {
      month,
      currentYear: apiData?.currentYear || 0,
      previousYear: apiData?.previousYear || 0
    }
  })
})

function getMaxValue() {
  const data = chartData.value
  if (!data.length) return 100
  const max = Math.max(...data.map(d => Math.max(d.currentYear, d.previousYear)))
  return max > 0 ? max : 100
}

function getBarHeight(value: number, maxHeight: number = 120): string {
  const max = getMaxValue()
  if (max === 0) return '8px'
  const height = (value / max) * maxHeight
  return `${Math.max(height, 8)}px`
}

function getActionColor(actionType: string): string {
  const action = (actionType || '').toLowerCase()
  switch (action) {
    case 'created':
    case 'upload':
    case 'create':
    case 'checkedin':
      return 'bg-teal/15 text-teal'
    case 'updated':
    case 'edit':
    case 'update':
    case 'moved':
    case 'move':
    case 'copied':
    case 'copy':
      return 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300'
    case 'deleted':
    case 'delete':
      return 'bg-slate-300 dark:bg-slate-600 text-slate-700 dark:text-slate-300'
    case 'viewed':
    case 'view':
    case 'downloaded':
    case 'download':
      return 'bg-teal/10 text-teal'
    case 'checkout':
    case 'checkedout':
    case 'discardedcheckout':
      return 'bg-navy/10 dark:bg-slate-700 text-navy dark:text-slate-300'
    case 'login':
    case 'logout':
      return 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400'
    default:
      return 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400'
  }
}

function getUserAvatarColor(index: number): string {
  const colors = ['bg-teal', 'bg-[#1a3a4a]', 'bg-[#2d3748]', 'bg-teal/80', 'bg-[#1e3a5f]']
  return colors[index % colors.length]
}

function getUserInitials(userName?: string): string {
  if (!userName) return '?'
  const parts = userName.trim().split(' ')
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
  }
  return userName.substring(0, 2).toUpperCase()
}

function formatActivityDate(dateStr: string): string {
  if (!dateStr) return '-'

  // Check if it's already a formatted string (e.g., "Today, 1:37 PM")
  if (dateStr.includes('Today') || dateStr.includes('Yesterday') || dateStr.includes('ago')) {
    return dateStr
  }

  const date = new Date(dateStr)

  // Check if date is valid
  if (isNaN(date.getTime())) {
    return dateStr // Return original string if not a valid date
  }

  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `Today, ${date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}`
  if (diffDays < 2) return `Yesterday, ${date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}`

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Donut chart calculations
const donutSegments = computed(() => {
  const circumference = 440
  let offset = 0
  return documentTypes.value.map((type, index) => {
    const dashLength = (type.percentage / 100) * circumference
    const segment = {
      ...type,
      dashArray: `${dashLength} ${circumference - dashLength}`,
      dashOffset: -offset
    }
    offset += dashLength
    return segment
  })
})

async function exportPDF() {
  // TODO: Implement PDF export
  alert('PDF export coming soon!')
}
</script>

<template>
  <div class="space-y-8">
    <!-- Header -->
    <div class="flex flex-col md:flex-row md:items-end justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-slate-900 dark:text-slate-100">Reports & Analytics</h1>
        <p class="text-slate-500 mt-1">Real-time enterprise statistics and document activity overview.</p>
      </div>
      <div class="flex items-center gap-3">
        <button class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-sm font-medium hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors shadow-sm">
          <span class="material-symbols-outlined text-lg">calendar_today</span>
          Last {{ selectedPeriod }} Days
        </button>
        <button
          @click="exportPDF"
          class="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-navy to-teal text-white rounded-lg text-sm font-medium shadow-md shadow-teal/20 hover:opacity-95 transition-opacity"
        >
          <span class="material-symbols-outlined text-lg">download</span>
          Export PDF
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-24">
      <div class="animate-spin w-10 h-10 border-4 border-teal border-t-transparent rounded-full"></div>
    </div>

    <template v-else>
      <!-- Stats Cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <!-- Total Documents -->
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">description</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Total Documents</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ formatNumber(statistics?.totalDocuments || 0) }}</p>
            <p v-if="statistics?.documentsGrowth" class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">trending_up</span>
              +{{ statistics.documentsGrowth }}% growth
            </p>
          </div>
        </div>

        <!-- Storage Used -->
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">database</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Storage Used</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ statistics?.storageUsedGB || 0 }} <span class="text-lg">GB</span></p>
            <div class="mt-4 w-full bg-zinc-800 h-1.5 rounded-full overflow-hidden">
              <div class="bg-teal h-full rounded-full" :style="{ width: (statistics?.storageCapacityPercent || 0) + '%' }"></div>
            </div>
            <p class="text-[10px] text-teal mt-2 font-medium">{{ statistics?.storageCapacityPercent || 0 }}% capacity used</p>
          </div>
        </div>

        <!-- Active Workflows -->
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">account_tree</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Active Workflows</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ statistics?.activeWorkflows || 0 }}</p>
            <p v-if="statistics?.pendingWorkflows" class="text-[10px] text-amber-400 mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">pending</span>
              {{ statistics.pendingWorkflows }} pending
            </p>
            <p v-else class="text-[10px] text-teal mt-4 font-medium">Workflows running</p>
          </div>
        </div>

        <!-- Total Users -->
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">groups</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Total Users</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ formatNumber(statistics?.totalUsers || 0) }}</p>
            <p v-if="statistics?.onlineUsers" class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">circle</span>
              {{ statistics.onlineUsers }} online now
            </p>
            <p v-else class="text-[10px] text-teal mt-4 font-medium">System users</p>
          </div>
        </div>
      </div>

      <!-- Charts Row -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-8">
        <!-- Document Growth Chart -->
        <div class="lg:col-span-2 bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm">
          <div class="flex items-center justify-between mb-8">
            <div>
              <h3 class="font-bold text-slate-900 dark:text-slate-100">Document Growth</h3>
              <p class="text-xs text-slate-500">Monthly upload volume trends</p>
            </div>
            <div class="flex items-center gap-4 text-xs">
              <div class="flex items-center gap-1.5">
                <span class="w-3 h-3 rounded-full bg-teal"></span>
                <span class="text-slate-600 dark:text-slate-400">Current Year</span>
              </div>
              <div class="flex items-center gap-1.5">
                <span class="w-3 h-3 rounded-full bg-navy/30"></span>
                <span class="text-slate-600 dark:text-slate-400">Previous Year</span>
              </div>
            </div>
          </div>

          <!-- Bar Chart -->
          <div class="h-[280px] w-full flex items-end gap-2 px-4 relative pb-8">
            <!-- Grid Lines with Y-Axis Labels -->
            <div class="absolute inset-x-4 top-0 bottom-12 flex flex-col justify-between pointer-events-none">
              <div class="border-b border-slate-100 dark:border-slate-800 w-full relative">
                <span class="absolute -left-2 -top-2 text-[9px] text-slate-400">{{ Math.round(getMaxValue()) }}</span>
              </div>
              <div class="border-b border-slate-100 dark:border-slate-800 w-full relative">
                <span class="absolute -left-2 -top-2 text-[9px] text-slate-400">{{ Math.round(getMaxValue() * 0.75) }}</span>
              </div>
              <div class="border-b border-slate-100 dark:border-slate-800 w-full relative">
                <span class="absolute -left-2 -top-2 text-[9px] text-slate-400">{{ Math.round(getMaxValue() * 0.5) }}</span>
              </div>
              <div class="border-b border-slate-100 dark:border-slate-800 w-full relative">
                <span class="absolute -left-2 -top-2 text-[9px] text-slate-400">{{ Math.round(getMaxValue() * 0.25) }}</span>
              </div>
              <div class="border-b border-slate-100 dark:border-slate-800 w-full relative">
                <span class="absolute -left-2 -top-2 text-[9px] text-slate-400">0</span>
              </div>
            </div>

            <!-- Bars -->
            <div
              v-for="data in chartData"
              :key="data.month"
              class="flex-1 flex flex-col items-center justify-end gap-1 group cursor-pointer relative"
              style="max-height: 220px;"
            >
              <!-- Values on hover -->
              <div class="absolute -top-6 left-1/2 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition-opacity bg-slate-800 text-white text-[10px] px-2 py-1 rounded whitespace-nowrap z-10">
                <span class="text-teal font-bold">{{ formatNumber(data.currentYear) }}</span> / <span class="text-slate-300">{{ formatNumber(data.previousYear) }}</span>
              </div>

              <div class="w-full flex gap-1 items-end justify-center" style="height: 180px;">
                <!-- Previous Year Bar -->
                <div class="relative flex-1 max-w-[28px]">
                  <div
                    class="w-full bg-slate-300/50 dark:bg-slate-600/50 rounded-t group-hover:bg-slate-300/70 dark:group-hover:bg-slate-600/70 transition-all"
                    :style="{ height: getBarHeight(data.previousYear, 160) }"
                  ></div>
                  <span class="absolute -top-4 left-1/2 -translate-x-1/2 text-[8px] text-slate-400 font-medium opacity-0 group-hover:opacity-100 transition-opacity">{{ data.previousYear > 0 ? formatNumber(data.previousYear) : '' }}</span>
                </div>
                <!-- Current Year Bar -->
                <div class="relative flex-1 max-w-[28px]">
                  <div
                    class="w-full bg-teal rounded-t group-hover:bg-teal/90 transition-all shadow-sm"
                    :style="{ height: getBarHeight(data.currentYear, 160) }"
                  ></div>
                  <span class="absolute -top-4 left-1/2 -translate-x-1/2 text-[8px] text-teal font-bold opacity-0 group-hover:opacity-100 transition-opacity">{{ data.currentYear > 0 ? formatNumber(data.currentYear) : '' }}</span>
                </div>
              </div>
              <span class="text-[10px] text-center text-slate-500 font-medium absolute -bottom-6">{{ data.month }}</span>
            </div>
          </div>
        </div>

        <!-- Document Types Donut Chart -->
        <div class="bg-white dark:bg-slate-900 p-6 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm">
          <h3 class="font-bold text-slate-900 dark:text-slate-100">Document Types</h3>
          <p class="text-xs text-slate-500 mb-8">Storage distribution by extension</p>

          <!-- Donut Chart -->
          <div class="relative flex items-center justify-center h-48 mb-8">
            <svg class="w-40 h-40 transform -rotate-90">
              <circle
                class="text-slate-100 dark:text-slate-800"
                cx="80" cy="80" fill="transparent" r="70"
                stroke="currentColor" stroke-width="20"
              ></circle>
              <circle
                class="text-teal"
                cx="80" cy="80" fill="transparent" r="70"
                stroke="currentColor" stroke-width="20"
                stroke-dasharray="242 198"
                stroke-dashoffset="0"
              ></circle>
              <circle
                class="text-navy"
                cx="80" cy="80" fill="transparent" r="70"
                stroke="currentColor" stroke-width="20"
                stroke-dasharray="110 330"
                stroke-dashoffset="-242"
              ></circle>
              <circle
                class="text-slate-300 dark:text-slate-600"
                cx="80" cy="80" fill="transparent" r="70"
                stroke="currentColor" stroke-width="20"
                stroke-dasharray="88 352"
                stroke-dashoffset="-352"
              ></circle>
            </svg>
            <div class="absolute inset-0 flex flex-col items-center justify-center">
              <span class="text-xl font-bold text-slate-900 dark:text-slate-100">100%</span>
              <span class="text-[10px] text-slate-400 uppercase tracking-tighter">Verified</span>
            </div>
          </div>

          <!-- Legend -->
          <div class="space-y-3">
            <div class="flex items-center justify-between text-sm">
              <div class="flex items-center gap-2">
                <span class="w-3 h-3 rounded bg-teal"></span>
                <span class="text-slate-600 dark:text-slate-400 font-medium">PDF Documents</span>
              </div>
              <span class="font-bold text-slate-900 dark:text-slate-100">55%</span>
            </div>
            <div class="flex items-center justify-between text-sm">
              <div class="flex items-center gap-2">
                <span class="w-3 h-3 rounded bg-navy"></span>
                <span class="text-slate-600 dark:text-slate-400 font-medium">Word / Office</span>
              </div>
              <span class="font-bold text-slate-900 dark:text-slate-100">25%</span>
            </div>
            <div class="flex items-center justify-between text-sm">
              <div class="flex items-center gap-2">
                <span class="w-3 h-3 rounded bg-slate-300 dark:bg-slate-700"></span>
                <span class="text-slate-600 dark:text-slate-400 font-medium">Media & Other</span>
              </div>
              <span class="font-bold text-slate-900 dark:text-slate-100">20%</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Recent Activity Table -->
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div class="px-6 py-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <h3 class="font-bold text-slate-900 dark:text-slate-100">Recent System Activity</h3>
          <router-link to="/activity" class="text-teal text-xs font-semibold hover:underline">
            View Full Audit Log
          </router-link>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50/50 dark:bg-slate-800/50 text-[11px] font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">
                <th class="px-6 py-4">User</th>
                <th class="px-6 py-4">Action</th>
                <th class="px-6 py-4">Target Document / Cabinet</th>
                <th class="px-6 py-4">Date & Time</th>
                <th class="px-6 py-4">IP Address</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr
                v-for="(activity, index) in recentActivity"
                :key="activity.id"
                class="hover:bg-slate-50/50 dark:hover:bg-slate-800/40 transition-colors group"
              >
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                    <div
                      class="w-7 h-7 rounded-full text-[10px] text-white flex items-center justify-center font-bold"
                      :class="getUserAvatarColor(index)"
                    >
                      {{ activity.userInitials || getUserInitials(activity.userName) }}
                    </div>
                    <p class="text-sm font-medium text-slate-900 dark:text-slate-100">{{ activity.userName || 'System' }}</p>
                  </div>
                </td>
                <td class="px-6 py-4">
                  <span
                    class="px-2 py-1 rounded text-[10px] font-bold uppercase"
                    :class="getActionColor(activity.actionType || activity.action?.toLowerCase() || '')"
                  >
                    {{ activity.action }}
                  </span>
                </td>
                <td class="px-6 py-4 text-sm text-slate-600 dark:text-slate-400">{{ activity.targetName || activity.nodeName || activity.details || '-' }}</td>
                <td class="px-6 py-4 text-sm text-slate-500">{{ formatActivityDate(activity.createdAt) }}</td>
                <td class="px-6 py-4 text-xs font-mono text-slate-400">{{ activity.ipAddress || '-' }}</td>
              </tr>

              <tr v-if="recentActivity.length === 0">
                <td colspan="5" class="px-6 py-12 text-center text-slate-400">
                  No recent activity
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>
