<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { systemHealthApi } from '@/api/client'
import type { SystemHealth, JobExecutionSummary } from '@/types'

const health = ref<SystemHealth | null>(null)
const loading = ref(true)

async function loadHealth() {
  loading.value = true
  try {
    const { data } = await systemHealthApi.getHealth()
    health.value = data.data ?? data
  } catch { /* empty */ }
  loading.value = false
}

onMounted(loadHealth)

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

function formatDuration(ms?: number): string {
  if (!ms) return '-'
  if (ms < 1000) return `${Math.round(ms)}ms`
  return `${(ms / 1000).toFixed(1)}s`
}

const storageUsageColor = computed(() => {
  if (!health.value) return 'text-gray-400'
  const pct = health.value.storage.usagePercent
  if (pct > 90) return 'text-red-600'
  if (pct > 70) return 'text-amber-600'
  return 'text-green-600'
})
</script>

<template>
  <div class="max-w-6xl mx-auto px-6 py-8">
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">System Health</h1>
        <p class="text-sm text-gray-500 mt-1">Monitor database, storage, search engine, and background jobs</p>
      </div>
      <button @click="loadHealth" class="px-4 py-2 text-sm bg-teal-600 text-white rounded-lg hover:bg-teal-700">
        <span class="material-symbols-outlined text-sm align-middle mr-1">refresh</span>
        Refresh
      </button>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <span class="material-symbols-outlined animate-spin text-4xl text-gray-400">progress_activity</span>
    </div>

    <template v-else-if="health">
      <!-- Health Cards -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        <!-- Database -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center" :class="health.database.isAvailable ? 'bg-green-100' : 'bg-red-100'">
              <span class="material-symbols-outlined" :class="health.database.isAvailable ? 'text-green-600' : 'text-red-600'">database</span>
            </div>
            <div>
              <div class="text-sm font-semibold text-gray-900">Database</div>
              <div class="text-xs" :class="health.database.isAvailable ? 'text-green-600' : 'text-red-600'">
                {{ health.database.isAvailable ? 'Connected' : 'Disconnected' }}
              </div>
            </div>
          </div>
          <div class="grid grid-cols-2 gap-2 text-sm">
            <div><span class="text-gray-500">Documents:</span> <span class="font-medium">{{ health.database.totalDocuments.toLocaleString() }}</span></div>
            <div><span class="text-gray-500">Users:</span> <span class="font-medium">{{ health.database.totalUsers.toLocaleString() }}</span></div>
            <div><span class="text-gray-500">Legal Holds:</span> <span class="font-medium">{{ health.database.activeLegalHolds }}</span></div>
          </div>
        </div>

        <!-- Storage -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center">
              <span class="material-symbols-outlined text-blue-600">hard_drive</span>
            </div>
            <div>
              <div class="text-sm font-semibold text-gray-900">Storage</div>
              <div class="text-xs" :class="storageUsageColor">{{ health.storage.usagePercent }}% used</div>
            </div>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2 mb-2">
            <div class="h-2 rounded-full transition-all"
              :class="{
                'bg-green-500': health.storage.usagePercent <= 70,
                'bg-amber-500': health.storage.usagePercent > 70 && health.storage.usagePercent <= 90,
                'bg-red-500': health.storage.usagePercent > 90
              }"
              :style="{ width: `${Math.min(health.storage.usagePercent, 100)}%` }"
            ></div>
          </div>
          <div class="text-xs text-gray-500">
            {{ formatBytes(health.storage.usedBytes) }} / {{ formatBytes(health.storage.totalBytes) }}
            ({{ formatBytes(health.storage.availableBytes) }} free)
          </div>
        </div>

        <!-- Overall Status -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center" :class="health.isHealthy ? 'bg-green-100' : 'bg-red-100'">
              <span class="material-symbols-outlined" :class="health.isHealthy ? 'text-green-600' : 'text-red-600'">
                {{ health.isHealthy ? 'check_circle' : 'error' }}
              </span>
            </div>
            <div>
              <div class="text-sm font-semibold text-gray-900">System Status</div>
              <div class="text-xs" :class="health.isHealthy ? 'text-green-600' : 'text-red-600'">
                {{ health.isHealthy ? 'All Systems Operational' : 'Issues Detected' }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Recent Job Executions -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200">
        <div class="p-4 border-b border-gray-100">
          <h3 class="text-sm font-semibold text-gray-700">Recent Background Job Executions</h3>
        </div>
        <div v-if="health.recentJobs.length === 0" class="p-8 text-center text-gray-400">No job executions recorded yet.</div>
        <table v-else class="w-full text-sm">
          <thead class="bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
            <tr>
              <th class="px-4 py-3">Job</th>
              <th class="px-4 py-3">Status</th>
              <th class="px-4 py-3">Started</th>
              <th class="px-4 py-3">Duration</th>
              <th class="px-4 py-3">Processed</th>
              <th class="px-4 py-3">Failed</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="job in health.recentJobs" :key="job.startedAt" class="hover:bg-gray-50">
              <td class="px-4 py-3 font-medium text-gray-900">{{ job.jobName }}</td>
              <td class="px-4 py-3">
                <span class="text-xs px-2 py-0.5 rounded-full"
                  :class="{
                    'bg-green-100 text-green-700': job.status === 'Completed',
                    'bg-red-100 text-red-700': job.status === 'Failed',
                    'bg-blue-100 text-blue-700': job.status === 'Running'
                  }"
                >{{ job.status }}</span>
              </td>
              <td class="px-4 py-3 text-gray-500">{{ new Date(job.startedAt).toLocaleString() }}</td>
              <td class="px-4 py-3 text-gray-500">{{ formatDuration(job.durationMs) }}</td>
              <td class="px-4 py-3">{{ job.itemsProcessed }}</td>
              <td class="px-4 py-3" :class="job.itemsFailed > 0 ? 'text-red-600' : ''">{{ job.itemsFailed }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
