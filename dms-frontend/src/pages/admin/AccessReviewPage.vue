<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { accessReviewApi } from '@/api/client'
import type { AccessReviewCampaign, StalePermission } from '@/types'

const campaigns = ref<AccessReviewCampaign[]>([])
const stalePermissions = ref<StalePermission[]>([])
const loading = ref(true)

// Campaign detail
const selectedCampaign = ref<AccessReviewCampaign | null>(null)
const campaignEntries = ref<any[]>([])
const loadingEntries = ref(false)

// Create campaign modal
const showCreateModal = ref(false)
const isCreating = ref(false)
const createForm = ref({ name: '', description: '', dueDate: '' })

async function loadData() {
  loading.value = true
  try {
    const [campRes, staleRes] = await Promise.allSettled([
      accessReviewApi.getCampaigns(),
      accessReviewApi.getStalePermissions(90)
    ])
    if (campRes.status === 'fulfilled') campaigns.value = campRes.value.data?.data ?? campRes.value.data ?? []
    if (staleRes.status === 'fulfilled') stalePermissions.value = staleRes.value.data?.data ?? staleRes.value.data ?? []
  } catch { /* silently fail */ }
  finally { loading.value = false }
}

onMounted(loadData)

async function selectCampaign(c: AccessReviewCampaign) {
  if (selectedCampaign.value?.id === c.id) {
    selectedCampaign.value = null
    campaignEntries.value = []
    return
  }
  selectedCampaign.value = c
  loadingEntries.value = true
  try {
    const res = await accessReviewApi.getEntries(c.id)
    campaignEntries.value = res.data?.data ?? res.data ?? []
  } catch { campaignEntries.value = [] }
  finally { loadingEntries.value = false }
}

async function createCampaign() {
  if (!createForm.value.name || !createForm.value.dueDate) return
  isCreating.value = true
  try {
    await accessReviewApi.createCampaign({
      name: createForm.value.name,
      description: createForm.value.description || undefined,
      dueDate: createForm.value.dueDate
    })
    showCreateModal.value = false
    createForm.value = { name: '', description: '', dueDate: '' }
    await loadData()
  } catch { /* silently fail */ }
  finally { isCreating.value = false }
}

async function submitDecision(entryId: string, decision: string) {
  const comments = decision === 'Revoke' ? prompt('Reason for revoking access:') : undefined
  try {
    await accessReviewApi.submitDecision(entryId, { decision, comments: comments || undefined })
    // Refresh entries
    if (selectedCampaign.value) {
      const res = await accessReviewApi.getEntries(selectedCampaign.value.id)
      campaignEntries.value = res.data?.data ?? res.data ?? []
    }
    await loadData()
  } catch { /* silently fail */ }
}

function progressPercent(c: AccessReviewCampaign): number {
  return c.totalEntries > 0 ? Math.round((c.completedEntries / c.totalEntries) * 100) : 0
}

function formatDate(d: string) {
  if (!d) return '-'
  return new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}

const statusColors: Record<string, string> = {
  Open: 'bg-zinc-100 text-zinc-600 dark:bg-zinc-700/50 dark:text-zinc-300',
  InProgress: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  Completed: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
}

function openCreateModal() {
  createForm.value = { name: '', description: '', dueDate: '' }
  showCreateModal.value = true
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-4">
        <router-link to="/records" class="p-2 rounded-lg text-zinc-400 hover:text-zinc-700 dark:hover:text-white hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors">
          <span class="material-symbols-outlined">arrow_back</span>
        </router-link>
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Access Review</h1>
          <p class="text-zinc-500 text-sm mt-0.5">Periodic review of user permissions and stale access</p>
        </div>
      </div>
      <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors font-medium text-sm shadow-sm">
        <span class="material-symbols-outlined text-lg">add</span>
        New Campaign
      </button>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-3 gap-4">
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-teal text-lg">campaign</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Campaigns</span>
          </div>
          <p class="text-2xl font-bold">{{ campaigns.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">{{ campaigns.filter(c => c.status === 'InProgress').length }} in progress</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-amber-400 text-lg">warning</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Stale</span>
          </div>
          <p class="text-2xl font-bold">{{ stalePermissions.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Inactive &gt; 90 days</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-emerald-400 text-lg">check_circle</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Completed</span>
          </div>
          <p class="text-2xl font-bold">{{ campaigns.filter(c => c.status === 'Completed').length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Campaigns finished</p>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center py-12 gap-3">
      <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
      <span class="text-zinc-500">Loading access review data...</span>
    </div>

    <template v-else>
      <!-- Campaigns List -->
      <div class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden shadow-sm">
        <div class="px-5 py-3.5 border-b border-zinc-200 dark:border-border-dark">
          <h2 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
            <span class="material-symbols-outlined text-lg text-teal">campaign</span>
            Review Campaigns
          </h2>
        </div>
        <div v-if="campaigns.length === 0" class="p-12 text-center text-zinc-400">
          <span class="material-symbols-outlined text-4xl mb-2 block">campaign</span>
          <p class="text-sm">No campaigns created yet</p>
          <p class="text-xs mt-1">Create a campaign to start reviewing user permissions</p>
        </div>
        <div v-else class="divide-y divide-zinc-100 dark:divide-border-dark/50">
          <div v-for="c in campaigns" :key="c.id">
            <!-- Campaign row -->
            <div
              @click="selectCampaign(c)"
              class="px-5 py-4 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 cursor-pointer transition-colors"
              :class="selectedCampaign?.id === c.id ? 'bg-teal/5 dark:bg-teal/10' : ''"
            >
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-center gap-2">
                  <span class="material-symbols-outlined text-zinc-400 text-lg">{{ selectedCampaign?.id === c.id ? 'expand_less' : 'expand_more' }}</span>
                  <h3 class="text-sm font-semibold text-zinc-800 dark:text-white">{{ c.name }}</h3>
                  <span class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full" :class="statusColors[c.status] || statusColors.Open">{{ c.status }}</span>
                </div>
                <span class="text-xs text-zinc-400">Due: {{ formatDate(c.dueDate) }}</span>
              </div>
              <p v-if="c.description" class="text-xs text-zinc-500 mb-2 ml-8">{{ c.description }}</p>
              <div class="ml-8 flex items-center gap-3">
                <div class="flex-1 bg-zinc-200 dark:bg-zinc-700 rounded-full h-1.5">
                  <div class="h-1.5 rounded-full bg-teal transition-all" :style="{ width: `${progressPercent(c)}%` }"></div>
                </div>
                <span class="text-[10px] font-medium text-zinc-500">{{ c.completedEntries }}/{{ c.totalEntries }} ({{ progressPercent(c) }}%)</span>
              </div>
            </div>

            <!-- Campaign detail/entries -->
            <div v-if="selectedCampaign?.id === c.id" class="px-5 pb-4 bg-zinc-50/50 dark:bg-surface-dark/30">
              <div v-if="loadingEntries" class="flex items-center justify-center py-6 gap-2">
                <div class="w-5 h-5 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
                <span class="text-xs text-zinc-500">Loading entries...</span>
              </div>
              <div v-else-if="campaignEntries.length === 0" class="py-6 text-center text-zinc-400 text-xs">
                No entries in this campaign
              </div>
              <table v-else class="w-full text-left mt-2">
                <thead>
                  <tr class="border-b border-zinc-200 dark:border-border-dark">
                    <th class="pb-2 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">User</th>
                    <th class="pb-2 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Resource</th>
                    <th class="pb-2 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Permission</th>
                    <th class="pb-2 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Decision</th>
                    <th class="pb-2 text-[10px] font-bold text-zinc-400 uppercase tracking-widest text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="entry in campaignEntries" :key="entry.id" class="border-b border-zinc-100 dark:border-border-dark/30">
                    <td class="py-2 text-xs font-medium text-zinc-700 dark:text-zinc-200">{{ entry.userName || entry.userId }}</td>
                    <td class="py-2 text-xs text-zinc-500">{{ entry.resourceName || entry.nodeId }}</td>
                    <td class="py-2 text-xs text-zinc-500">{{ entry.permissionLevel }}</td>
                    <td class="py-2">
                      <span v-if="entry.decision" class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full"
                        :class="entry.decision === 'Approve' ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400' : 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400'">
                        {{ entry.decision }}
                      </span>
                      <span v-else class="text-[10px] text-zinc-400">Pending</span>
                    </td>
                    <td class="py-2 text-right">
                      <template v-if="!entry.decision">
                        <button @click.stop="submitDecision(entry.id, 'Approve')" class="px-2.5 py-1 text-[10px] font-medium text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-900/20 rounded transition-colors">
                          Approve
                        </button>
                        <button @click.stop="submitDecision(entry.id, 'Revoke')" class="px-2.5 py-1 text-[10px] font-medium text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/20 rounded transition-colors ml-1">
                          Revoke
                        </button>
                      </template>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- Stale Permissions -->
      <div v-if="stalePermissions.length > 0" class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden shadow-sm">
        <div class="px-5 py-3.5 border-b border-zinc-200 dark:border-border-dark">
          <h2 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
            <span class="material-symbols-outlined text-lg text-amber-500">warning</span>
            Stale Permissions
            <span class="ml-1 px-2 py-0.5 text-[10px] font-bold bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 rounded-full">{{ stalePermissions.length }}</span>
          </h2>
        </div>
        <table class="w-full text-left">
          <thead>
            <tr class="border-b border-zinc-200 dark:border-border-dark">
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">User</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Node Type</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Permission</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Last Login</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Inactive</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="sp in stalePermissions" :key="`${sp.userId}-${sp.nodeId}`" class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors">
              <td class="px-5 py-3 text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ sp.userName || sp.userId }}</td>
              <td class="px-5 py-3 text-xs text-zinc-500">{{ sp.nodeType }}</td>
              <td class="px-5 py-3 text-xs text-zinc-500">{{ sp.permissionLevel }}</td>
              <td class="px-5 py-3 text-xs text-zinc-500">{{ sp.lastLoginAt ? formatDate(sp.lastLoginAt) : 'Never' }}</td>
              <td class="px-5 py-3">
                <span class="text-xs font-medium" :class="sp.inactiveDays > 180 ? 'text-rose-500' : 'text-amber-600'">
                  {{ sp.inactiveDays === -1 ? 'Never' : `${sp.inactiveDays}d` }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- Create Campaign Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showCreateModal" class="fixed inset-0 z-[100] flex items-center justify-center">
          <div class="absolute inset-0 bg-black/50" @click="showCreateModal = false"></div>
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showCreateModal" class="relative bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md overflow-hidden">
              <!-- Header -->
              <div class="px-6 py-4 bg-gradient-to-r from-[#0d1117] to-teal/80 relative overflow-hidden">
                <div class="absolute -right-4 -top-4 w-24 h-24 bg-white/5 rounded-full"></div>
                <div class="absolute -right-8 -bottom-8 w-32 h-32 bg-white/5 rounded-full"></div>
                <div class="relative flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-white/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-white">campaign</span>
                  </div>
                  <div>
                    <h3 class="text-white font-bold">New Review Campaign</h3>
                    <p class="text-white/70 text-xs mt-0.5">Schedule a periodic access review</p>
                  </div>
                </div>
              </div>

              <!-- Body -->
              <div class="p-6 space-y-4">
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Campaign Name *</label>
                  <input v-model="createForm.name" type="text" class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none" placeholder="Q1 2026 Access Review" />
                </div>
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Due Date *</label>
                  <input v-model="createForm.dueDate" type="date" class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none" />
                </div>
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Description</label>
                  <textarea v-model="createForm.description" rows="2" class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none" placeholder="Optional description..."></textarea>
                </div>
              </div>

              <!-- Footer -->
              <div class="px-6 py-4 bg-zinc-50 dark:bg-zinc-800/50 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3">
                <button @click="showCreateModal = false" class="px-4 py-2 text-sm font-medium text-zinc-600 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-white transition-colors">Cancel</button>
                <button @click="createCampaign" :disabled="isCreating || !createForm.name || !createForm.dueDate" class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 flex items-center gap-2">
                  <span v-if="isCreating" class="material-symbols-outlined text-sm animate-spin">refresh</span>
                  {{ isCreating ? 'Creating...' : 'Create Campaign' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
