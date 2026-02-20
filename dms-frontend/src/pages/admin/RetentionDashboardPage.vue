<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { retentionDashboardApi } from '@/api/client'
import type { RetentionDashboard, RetentionAction, RetentionPolicySummary, BackgroundJob, UpcomingExpiration, ExpirationTimeline } from '@/types'

const dashboard = ref<RetentionDashboard | null>(null)
const isLoading = ref(true)
const lastUpdated = ref<Date | null>(null)
let refreshInterval: ReturnType<typeof setInterval> | null = null

const statCards = computed(() => {
  if (!dashboard.value) return []
  const d = dashboard.value
  return [
    { label: 'Active Retentions', value: d.activeRetentions, icon: 'schedule', color: 'text-teal', wave: '#00ae8c', sub: 'Under active policy' },
    { label: 'Pending Review', value: d.pendingReview, icon: 'rate_review', color: 'text-amber-400', wave: '#f59e0b', sub: 'Awaiting decision' },
    { label: 'Expiring ≤7d', value: d.expiringSoon7, icon: 'alarm', color: 'text-rose-400', wave: '#fb7185', sub: 'Urgent attention' },
    { label: 'Expiring ≤30d', value: d.expiringSoon30, icon: 'timelapse', color: 'text-orange-400', wave: '#fb923c', sub: 'Approaching deadline' },
    { label: 'On Hold', value: d.onHold, icon: 'gavel', color: 'text-indigo-400', wave: '#818cf8', sub: 'Legal hold active' },
    { label: 'Archived', value: d.archived, icon: 'inventory_2', color: 'text-blue-400', wave: '#60a5fa', sub: 'Preserved records' },
    { label: 'Disposed', value: d.disposed, icon: 'delete_forever', color: 'text-zinc-400', wave: '#a1a1aa', sub: 'Completed lifecycle' },
    { label: 'Awaiting Trigger', value: d.awaitingTrigger, icon: 'pending_actions', color: 'text-purple-400', wave: '#c084fc', sub: 'Event-based start' }
  ]
})

const timelineMax = computed(() => {
  if (!dashboard.value?.expirationTimeline?.length) return 1
  return Math.max(...dashboard.value.expirationTimeline.map(t => t.count), 1)
})

const timelineEntries = computed(() => {
  if (!dashboard.value?.expirationTimeline) return []
  const today = new Date()
  const entries: { date: Date; count: number; dayIndex: number }[] = []
  for (let i = 0; i < 90; i++) {
    const d = new Date(today)
    d.setDate(d.getDate() + i)
    const dateStr = d.toISOString().split('T')[0]
    const match = dashboard.value.expirationTimeline.find(t => t.date?.startsWith(dateStr))
    entries.push({ date: d, count: match?.count || 0, dayIndex: i })
  }
  return entries
})

async function loadDashboard() {
  isLoading.value = true
  try {
    const res = await retentionDashboardApi.getDashboard()
    dashboard.value = res.data
    lastUpdated.value = new Date()
  } catch (err) {
    console.error('Failed to load retention dashboard', err)
  } finally {
    isLoading.value = false
  }
}

function manualRefresh() {
  if (refreshInterval) clearInterval(refreshInterval)
  loadDashboard()
  refreshInterval = setInterval(loadDashboard, 60000)
}

function formatRelative(dateStr: string) {
  const ms = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(ms / 60000)
  if (mins < 1) return 'Just now'
  if (mins < 60) return `${mins}m ago`
  const hrs = Math.floor(mins / 60)
  if (hrs < 24) return `${hrs}h ago`
  return `${Math.floor(hrs / 24)}d ago`
}

function formatDuration(ms?: number) {
  if (!ms) return '-'
  if (ms < 1000) return `${Math.round(ms)}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

function formatTime(date: Date | null) {
  if (!date) return '-'
  return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}

function getActionBadgeClass(action: string) {
  const map: Record<string, string> = {
    Archive: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
    Delete: 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400',
    Review: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
    Notify: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400'
  }
  return map[action] || 'bg-zinc-100 text-zinc-600 dark:bg-zinc-700 dark:text-zinc-300'
}

function getJobStatusClass(status: string) {
  if (status === 'Completed') return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
  if (status === 'Running') return 'bg-teal/10 text-teal dark:bg-teal/20 dark:text-teal'
  if (status === 'Failed') return 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400'
  return 'bg-zinc-100 text-zinc-600 dark:bg-zinc-700 dark:text-zinc-300'
}

function getJobIcon(name: string) {
  if (name.includes('Retention')) return 'schedule'
  if (name.includes('Integrity')) return 'verified_user'
  if (name.includes('Checkout') || name.includes('Stale')) return 'lock_clock'
  if (name.includes('Disposal')) return 'delete_sweep'
  if (name.includes('Index') || name.includes('Search')) return 'search'
  return 'engineering'
}

function getActionIcon(action: string) {
  const map: Record<string, string> = {
    'Flagged for Review': 'rate_review',
    'Auto-Archived': 'inventory_2',
    'Auto-Deleted': 'delete_forever',
    'Placed on Hold': 'gavel',
    'Approved': 'check_circle'
  }
  return map[action] || 'swap_horiz'
}

function getActionColor(action: string) {
  const map: Record<string, string> = {
    'Flagged for Review': 'text-amber-500',
    'Auto-Archived': 'text-blue-500',
    'Auto-Deleted': 'text-rose-500',
    'Placed on Hold': 'text-indigo-500',
    'Approved': 'text-emerald-500'
  }
  return map[action] || 'text-zinc-400'
}

function getDaysLeftClass(days: number) {
  if (days <= 7) return 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300'
  if (days <= 30) return 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300'
  return 'bg-zinc-100 text-zinc-600 dark:bg-zinc-700 dark:text-zinc-300'
}

function getBarColor(count: number) {
  if (count === 0) return 'bg-zinc-200 dark:bg-zinc-800'
  if (count >= 5) return 'bg-rose-500'
  if (count >= 3) return 'bg-amber-500'
  return 'bg-teal'
}

onMounted(() => {
  loadDashboard()
  refreshInterval = setInterval(loadDashboard, 60000)
})

onUnmounted(() => {
  if (refreshInterval) clearInterval(refreshInterval)
})
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex items-center justify-between flex-wrap gap-4">
      <div class="flex items-center gap-4">
        <router-link to="/admin/compliance" class="p-2 rounded-lg text-zinc-400 hover:text-zinc-700 dark:hover:text-white hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors">
          <span class="material-symbols-outlined">arrow_back</span>
        </router-link>
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Retention Governance</h1>
          <p class="text-zinc-500 text-sm mt-0.5">Live retention monitoring, expiration tracking, and background job health</p>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <span class="text-[10px] text-zinc-400 font-mono tracking-wide">
          Last updated: {{ formatTime(lastUpdated) }}
        </span>
        <button
          @click="manualRefresh"
          :disabled="isLoading"
          class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-surface-dark hover:bg-zinc-50 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark disabled:opacity-50"
        >
          <span class="material-symbols-outlined text-lg" :class="{ 'animate-spin': isLoading }">refresh</span>
          Refresh
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading && !dashboard" class="p-16 flex flex-col items-center justify-center gap-3">
      <div class="w-8 h-8 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
      <span class="text-zinc-500 text-sm">Loading retention data...</span>
    </div>

    <template v-else-if="dashboard">
      <!-- Row 1: Stat Cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div
          v-for="(card, i) in statCards"
          :key="i"
          class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden"
        >
          <svg class="absolute right-0 top-0 h-full w-20 opacity-[0.15]" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" :fill="card.wave" />
          </svg>
          <div class="relative z-10">
            <div class="flex items-center justify-between mb-2">
              <span class="material-symbols-outlined text-lg" :class="card.color">{{ card.icon }}</span>
              <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">{{ card.label }}</span>
            </div>
            <p class="text-2xl font-bold tabular-nums">{{ card.value.toLocaleString() }}</p>
            <p class="text-[10px] text-zinc-500 mt-1">{{ card.sub }}</p>
          </div>
        </div>
      </div>

      <!-- Row 2: Policy Table + Jobs -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Retention by Policy -->
        <div class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
              <span class="material-symbols-outlined text-teal text-lg">policy</span>
              Retention by Policy
            </h3>
            <span class="text-[10px] text-zinc-400">{{ dashboard.retentionsByPolicy.length }} policies</span>
          </div>
          <div v-if="dashboard.retentionsByPolicy.length === 0" class="text-center py-8 text-zinc-400 text-sm">
            No retention policies configured
          </div>
          <div v-else class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-zinc-100 dark:border-border-dark">
                  <th class="text-left py-2 pr-3 text-[10px] font-bold text-zinc-400 uppercase tracking-wider">Policy</th>
                  <th class="text-left py-2 pr-3 text-[10px] font-bold text-zinc-400 uppercase tracking-wider">Action</th>
                  <th class="text-right py-2 pr-3 text-[10px] font-bold text-zinc-400 uppercase tracking-wider">Total</th>
                  <th class="text-right py-2 pr-3 text-[10px] font-bold text-zinc-400 uppercase tracking-wider">Active</th>
                  <th class="text-right py-2 pr-3 text-[10px] font-bold text-zinc-400 uppercase tracking-wider">Expired</th>
                  <th class="text-right py-2 text-[10px] font-bold text-zinc-400 uppercase tracking-wider">Hold</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="policy in dashboard.retentionsByPolicy"
                  :key="policy.policyId"
                  class="border-b border-zinc-50 dark:border-border-dark/50 last:border-0 hover:bg-zinc-50 dark:hover:bg-surface-dark transition-colors"
                >
                  <td class="py-2.5 pr-3">
                    <span class="font-medium text-zinc-700 dark:text-zinc-200 text-xs">{{ policy.policyName }}</span>
                  </td>
                  <td class="py-2.5 pr-3">
                    <span class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full" :class="getActionBadgeClass(policy.expirationAction)">
                      {{ policy.expirationAction }}
                    </span>
                  </td>
                  <td class="py-2.5 pr-3 text-right tabular-nums text-xs text-zinc-600 dark:text-zinc-300 font-semibold">{{ policy.totalDocuments }}</td>
                  <td class="py-2.5 pr-3 text-right tabular-nums text-xs text-teal font-semibold">{{ policy.activeCount }}</td>
                  <td class="py-2.5 pr-3 text-right tabular-nums text-xs text-amber-500 font-semibold">{{ policy.expiredCount }}</td>
                  <td class="py-2.5 text-right tabular-nums text-xs text-indigo-500 font-semibold">{{ policy.onHoldCount }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Background Jobs Health -->
        <div class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
              <span class="material-symbols-outlined text-teal text-lg">engineering</span>
              Background Jobs
            </h3>
            <router-link to="/admin/system-health" class="text-xs text-teal hover:text-teal/80 font-medium">View All</router-link>
          </div>
          <div v-if="dashboard.backgroundJobs.length === 0" class="text-center py-8 text-zinc-400 text-sm">
            No job executions recorded yet
          </div>
          <div v-else class="space-y-2.5 max-h-[340px] overflow-y-auto pr-1">
            <div
              v-for="job in dashboard.backgroundJobs.slice(0, 8)"
              :key="job.id"
              class="flex items-center gap-3 px-3 py-2.5 bg-zinc-50 dark:bg-surface-dark rounded-lg border border-zinc-100 dark:border-border-dark"
            >
              <div class="w-8 h-8 rounded-lg bg-teal/10 flex items-center justify-center flex-shrink-0">
                <span class="material-symbols-outlined text-teal text-base">{{ getJobIcon(job.jobName) }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-xs font-semibold text-zinc-700 dark:text-zinc-200 truncate">{{ job.jobName }}</p>
                <div class="flex items-center gap-2 mt-0.5">
                  <span class="text-[10px] text-zinc-400">{{ formatRelative(job.startedAt) }}</span>
                  <span v-if="job.durationMs" class="text-[10px] text-zinc-400">&middot; {{ formatDuration(job.durationMs) }}</span>
                  <span v-if="job.itemsProcessed > 0" class="text-[10px] text-zinc-400">&middot; {{ job.itemsProcessed }} processed</span>
                  <span v-if="job.itemsFailed > 0" class="text-[10px] text-rose-400">&middot; {{ job.itemsFailed }} failed</span>
                </div>
              </div>
              <span
                class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full flex-shrink-0"
                :class="getJobStatusClass(job.status)"
              >
                <span v-if="job.status === 'Running'" class="inline-block w-1.5 h-1.5 bg-teal rounded-full animate-pulse mr-1 align-middle"></span>
                {{ job.status }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Row 3: Upcoming Expirations + Recent Actions -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <!-- Upcoming Expirations -->
        <div class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
              <span class="material-symbols-outlined text-rose-400 text-lg">alarm</span>
              Upcoming Expirations
            </h3>
            <span class="text-[10px] text-zinc-400">Next 30 days</span>
          </div>
          <div v-if="dashboard.upcomingExpirations.length === 0" class="text-center py-8 text-zinc-400 text-sm">
            No documents expiring soon
          </div>
          <div v-else class="space-y-1.5">
            <div
              v-for="item in dashboard.upcomingExpirations.slice(0, 10)"
              :key="item.documentId"
              class="flex items-center gap-3 px-3 py-2 rounded-lg hover:bg-zinc-50 dark:hover:bg-surface-dark transition-colors"
            >
              <div class="flex-1 min-w-0">
                <router-link
                  :to="`/documents/${item.documentId}`"
                  class="text-xs font-medium text-zinc-700 dark:text-zinc-200 hover:text-teal transition-colors truncate block"
                >
                  {{ item.documentName }}
                </router-link>
                <p class="text-[10px] text-zinc-400 mt-0.5 truncate">{{ item.policyName }} &middot; {{ formatDate(item.expirationDate) }}</p>
              </div>
              <span
                class="px-2 py-0.5 text-[10px] font-bold rounded-full flex-shrink-0 tabular-nums"
                :class="getDaysLeftClass(item.daysRemaining)"
              >
                {{ item.daysRemaining }}d
              </span>
            </div>
          </div>
        </div>

        <!-- Recent Actions -->
        <div class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
              <span class="material-symbols-outlined text-teal text-lg">history</span>
              Recent Actions
            </h3>
          </div>
          <div v-if="dashboard.recentActions.length === 0" class="text-center py-8 text-zinc-400 text-sm">
            No retention actions recorded
          </div>
          <div v-else class="space-y-1">
            <div
              v-for="(action, i) in dashboard.recentActions.slice(0, 10)"
              :key="i"
              class="flex items-start gap-3 px-3 py-2.5 rounded-lg hover:bg-zinc-50 dark:hover:bg-surface-dark transition-colors"
            >
              <div class="w-7 h-7 rounded-full flex items-center justify-center flex-shrink-0 mt-0.5"
                :class="action.isSystemAction ? 'bg-zinc-100 dark:bg-zinc-800' : 'bg-teal/10'">
                <span v-if="action.isSystemAction" class="material-symbols-outlined text-zinc-400 text-sm">smart_toy</span>
                <span v-else class="material-symbols-outlined text-sm" :class="getActionColor(action.action)">{{ getActionIcon(action.action) }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-xs text-zinc-700 dark:text-zinc-200">
                  <span class="font-semibold">{{ action.documentName }}</span>
                  <span class="text-zinc-400"> — {{ action.action }}</span>
                </p>
                <p class="text-[10px] text-zinc-400 mt-0.5">{{ action.policyName }} &middot; {{ formatRelative(action.timestamp) }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Row 4: Expiration Timeline -->
      <div class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark p-6">
        <div class="flex items-center justify-between mb-5">
          <h3 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
            <span class="material-symbols-outlined text-teal text-lg">timeline</span>
            Expiration Timeline
          </h3>
          <span class="text-[10px] text-zinc-400">Next 90 days</span>
        </div>
        <div v-if="timelineEntries.every(t => t.count === 0)" class="text-center py-8 text-zinc-400 text-sm">
          No expirations scheduled in the next 90 days
        </div>
        <div v-else>
          <!-- Chart area -->
          <div class="flex items-end gap-px h-32 mb-2">
            <div
              v-for="entry in timelineEntries"
              :key="entry.dayIndex"
              class="flex-1 min-w-0 group relative"
            >
              <div
                class="w-full rounded-t-sm transition-all duration-200 group-hover:opacity-80"
                :class="[
                  getBarColor(entry.count),
                  entry.dayIndex % 7 === 6 ? 'mr-px' : ''
                ]"
                :style="{
                  height: entry.count > 0
                    ? `${Math.max(4, (entry.count / timelineMax) * 100)}%`
                    : '2px'
                }"
              ></div>
              <!-- Tooltip -->
              <div v-if="entry.count > 0" class="absolute bottom-full left-1/2 -translate-x-1/2 mb-1 px-2 py-1 bg-[#0d1117] text-white text-[9px] rounded shadow-lg opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none whitespace-nowrap z-10">
                {{ formatDate(entry.date.toISOString()) }}: {{ entry.count }}
              </div>
            </div>
          </div>
          <!-- Date labels -->
          <div class="flex gap-px">
            <div
              v-for="entry in timelineEntries"
              :key="'label-' + entry.dayIndex"
              class="flex-1 min-w-0"
            >
              <span
                v-if="entry.dayIndex % 14 === 0"
                class="text-[8px] text-zinc-400 block truncate"
              >
                {{ formatDate(entry.date.toISOString()) }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
