<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { disposalApi } from '@/api/client'

const isLoading = ref(true)
const pendingDisposals = ref<any[]>([])
const upcomingDisposals = ref<any[]>([])
const certificates = ref<any[]>([])
const disposalRequests = ref<any[]>([])
const activeTab = ref<'requests' | 'pending' | 'upcoming' | 'certificates'>('requests')

// Request detail
const selectedRequest = ref<any>(null)
const loadingDetail = ref(false)

// Approval modal
const showApprovalModal = ref(false)
const approvalForm = ref({ decision: 'Approved', comments: '' })
const isSubmitting = ref(false)
const approvalRequestId = ref('')

onMounted(async () => {
  await loadAll()
})

async function loadAll() {
  isLoading.value = true
  try {
    const [pendRes, upRes, certsRes, reqsRes] = await Promise.allSettled([
      disposalApi.getPending(),
      disposalApi.getUpcoming(90),
      disposalApi.getCertificates(),
      disposalApi.getRequests()
    ])
    if (pendRes.status === 'fulfilled') pendingDisposals.value = pendRes.value.data || []
    if (upRes.status === 'fulfilled') upcomingDisposals.value = upRes.value.data || []
    if (certsRes.status === 'fulfilled') certificates.value = certsRes.value.data || []
    if (reqsRes.status === 'fulfilled') disposalRequests.value = reqsRes.value.data?.data ?? reqsRes.value.data ?? []
  } catch { /* silently fail */ }
  finally { isLoading.value = false }
}

async function selectRequest(req: any) {
  if (selectedRequest.value?.id === req.id) {
    selectedRequest.value = null
    return
  }
  loadingDetail.value = true
  try {
    const res = await disposalApi.getRequest(req.id)
    selectedRequest.value = res.data
  } catch { selectedRequest.value = null }
  finally { loadingDetail.value = false }
}

async function initiateDisposal(docId: string) {
  try {
    await disposalApi.initiate(docId, {
      reason: 'Retention policy expired',
      disposalMethod: 'HardDelete',
      requiresApproval: true
    })
    await loadAll()
  } catch { /* silently fail */ }
}

async function executeDisposal(docId: string) {
  if (!confirm('Are you sure you want to permanently dispose this document? This action cannot be undone.')) return
  try {
    await disposalApi.execute(docId, 'HardDelete')
    await loadAll()
  } catch { /* silently fail */ }
}

function openApprovalModal(requestId: string) {
  approvalRequestId.value = requestId
  approvalForm.value = { decision: 'Approved', comments: '' }
  showApprovalModal.value = true
}

async function submitApproval() {
  if (!approvalRequestId.value) return
  isSubmitting.value = true
  try {
    await disposalApi.submitApproval(approvalRequestId.value, {
      decision: approvalForm.value.decision,
      comments: approvalForm.value.comments || undefined
    })
    showApprovalModal.value = false
    // Refresh detail if viewing the same request
    if (selectedRequest.value?.id === approvalRequestId.value) {
      const res = await disposalApi.getRequest(approvalRequestId.value)
      selectedRequest.value = res.data
    }
    await loadAll()
  } catch { /* silently fail */ }
  finally { isSubmitting.value = false }
}

async function executeBatchDisposal(requestId: string) {
  if (!confirm('Execute this disposal request? All approved documents will be permanently disposed.')) return
  try {
    await disposalApi.executeBatch(requestId)
    selectedRequest.value = null
    await loadAll()
  } catch { /* silently fail */ }
}

async function processScheduled() {
  try {
    await disposalApi.processScheduled()
    await loadAll()
  } catch { /* silently fail */ }
}

function formatDate(d: string) {
  if (!d) return '-'
  return new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}

function getDaysUntil(d: string) {
  if (!d) return 0
  return Math.ceil((new Date(d).getTime() - Date.now()) / (1000 * 60 * 60 * 24))
}

const requestStatusColors: Record<string, string> = {
  Pending: 'bg-zinc-100 text-zinc-600 dark:bg-zinc-700/50 dark:text-zinc-300',
  PendingApproval: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  PartiallyApproved: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  Approved: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
  Rejected: 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400',
  Executed: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
  Cancelled: 'bg-zinc-100 text-zinc-500 dark:bg-zinc-700 dark:text-zinc-400'
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-4">
        <router-link to="/records" class="p-2 rounded-lg text-zinc-400 hover:text-zinc-700 dark:hover:text-white hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors">
          <span class="material-symbols-outlined">arrow_back</span>
        </router-link>
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Disposal Management</h1>
          <p class="text-zinc-500 text-sm mt-0.5">Multi-level disposal approval workflow and certificates</p>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <button @click="processScheduled" class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-surface-dark hover:bg-zinc-50 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark">
          <span class="material-symbols-outlined text-lg">play_arrow</span>
          Process Scheduled
        </button>
        <button @click="loadAll" class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-surface-dark hover:bg-zinc-50 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-lg font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark">
          <span class="material-symbols-outlined text-lg">refresh</span>
          Refresh
        </button>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-4 gap-4">
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden cursor-pointer" @click="activeTab = 'requests'">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-teal text-lg">assignment</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Requests</span>
          </div>
          <p class="text-2xl font-bold">{{ disposalRequests.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Disposal requests</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden cursor-pointer" @click="activeTab = 'pending'">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-rose-400 text-lg">pending</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Pending</span>
          </div>
          <p class="text-2xl font-bold">{{ pendingDisposals.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Awaiting disposal</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden cursor-pointer" @click="activeTab = 'upcoming'">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-amber-400 text-lg">schedule</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Upcoming</span>
          </div>
          <p class="text-2xl font-bold">{{ upcomingDisposals.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Next 90 days</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden cursor-pointer" @click="activeTab = 'certificates'">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-teal text-lg">verified</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Certificates</span>
          </div>
          <p class="text-2xl font-bold">{{ certificates.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Disposal records</p>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-12 gap-3">
      <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
      <span class="text-zinc-500">Loading disposal data...</span>
    </div>

    <!-- Content -->
    <div v-else class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden shadow-sm">
      <!-- Section tabs -->
      <div class="border-b border-zinc-200 dark:border-border-dark px-6 flex gap-1">
        <button
          v-for="tab in [
            { id: 'requests', label: 'Disposal Requests', icon: 'assignment' },
            { id: 'pending', label: 'Pending', icon: 'pending' },
            { id: 'upcoming', label: 'Upcoming', icon: 'schedule' },
            { id: 'certificates', label: 'Certificates', icon: 'verified' }
          ]"
          :key="tab.id"
          @click="activeTab = tab.id as any"
          class="px-4 py-3 flex items-center gap-2 text-sm font-medium transition-colors relative whitespace-nowrap"
          :class="activeTab === tab.id ? 'text-teal' : 'text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300'"
        >
          <span class="material-symbols-outlined text-lg">{{ tab.icon }}</span>
          {{ tab.label }}
          <div v-if="activeTab === tab.id" class="absolute bottom-0 left-2 right-2 h-0.5 bg-teal rounded-full"></div>
        </button>
      </div>

      <!-- Disposal Requests with Multi-Level Approval -->
      <div v-if="activeTab === 'requests'" class="p-6">
        <div v-if="disposalRequests.length === 0" class="text-center py-12 text-zinc-400">
          <span class="material-symbols-outlined text-4xl mb-2 block">assignment</span>
          <p class="text-sm">No disposal requests</p>
        </div>
        <div v-else class="space-y-3">
          <div v-for="req in disposalRequests" :key="req.id">
            <!-- Request card -->
            <div
              @click="selectRequest(req)"
              class="p-4 rounded-lg border cursor-pointer transition-all hover:border-teal/30"
              :class="selectedRequest?.id === req.id ? 'border-teal bg-teal/5 dark:bg-teal/10' : 'border-zinc-200 dark:border-border-dark'"
            >
              <div class="flex items-center justify-between mb-2">
                <div class="flex items-center gap-2">
                  <span class="material-symbols-outlined text-zinc-400 text-lg">{{ selectedRequest?.id === req.id ? 'expand_less' : 'expand_more' }}</span>
                  <span class="text-xs font-mono text-teal font-medium">{{ req.requestNumber }}</span>
                  <span class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full" :class="requestStatusColors[req.status] || requestStatusColors.Pending">{{ req.status }}</span>
                </div>
                <div class="flex items-center gap-3 text-[10px] text-zinc-400">
                  <span>{{ req.disposalMethod }}</span>
                  <span>{{ formatDate(req.requestedAt) }}</span>
                </div>
              </div>
              <div class="ml-8 flex items-center gap-4 text-xs text-zinc-500">
                <span v-if="req.reason">{{ req.reason }}</span>
                <span v-if="req.legalBasis" class="text-zinc-400">Legal: {{ req.legalBasis }}</span>
                <span class="ml-auto text-[10px]">Approval: {{ req.currentApprovalLevel || 0 }}/{{ req.requiredApprovalLevels || 1 }}</span>
              </div>
              <!-- Approval progress bar -->
              <div class="ml-8 mt-2 flex items-center gap-2">
                <div class="flex-1 bg-zinc-200 dark:bg-zinc-700 rounded-full h-1">
                  <div class="h-1 rounded-full transition-all" :class="req.status === 'Approved' ? 'bg-emerald-500' : req.status === 'Rejected' ? 'bg-rose-500' : 'bg-teal'" :style="{ width: `${req.requiredApprovalLevels > 0 ? ((req.currentApprovalLevel || 0) / req.requiredApprovalLevels) * 100 : 0}%` }"></div>
                </div>
              </div>
            </div>

            <!-- Request detail (expanded) -->
            <div v-if="selectedRequest?.id === req.id" class="ml-4 p-4 bg-zinc-50/50 dark:bg-surface-dark/30 rounded-lg border border-zinc-100 dark:border-border-dark/50">
              <div v-if="loadingDetail" class="flex items-center justify-center py-6 gap-2">
                <div class="w-5 h-5 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
                <span class="text-xs text-zinc-500">Loading details...</span>
              </div>
              <template v-else>
                <!-- Documents -->
                <div class="mb-4">
                  <h4 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-2">Documents ({{ selectedRequest.documents?.length || 0 }})</h4>
                  <div v-if="!selectedRequest.documents?.length" class="text-xs text-zinc-400">No documents attached</div>
                  <div v-else class="space-y-1">
                    <div v-for="doc in selectedRequest.documents" :key="doc.id" class="flex items-center gap-2 px-3 py-2 bg-white dark:bg-background-dark rounded border border-zinc-100 dark:border-border-dark">
                      <span class="material-symbols-outlined text-zinc-400 text-sm">description</span>
                      <span class="text-xs font-medium text-zinc-700 dark:text-zinc-200 flex-1">{{ doc.documentName || doc.documentId }}</span>
                      <span class="px-1.5 py-0.5 text-[9px] font-bold uppercase rounded-full"
                        :class="doc.status === 'Disposed' ? 'bg-purple-100 text-purple-700' : doc.status === 'Error' ? 'bg-rose-100 text-rose-700' : 'bg-zinc-100 text-zinc-500'">
                        {{ doc.status }}
                      </span>
                    </div>
                  </div>
                </div>

                <!-- Approval Chain -->
                <div class="mb-4">
                  <h4 class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest mb-2">Approval Chain ({{ selectedRequest.currentApprovalLevel || 0 }}/{{ selectedRequest.requiredApprovalLevels }})</h4>
                  <div v-if="!selectedRequest.approvals?.length" class="text-xs text-zinc-400">No approvals recorded yet</div>
                  <div v-else class="space-y-1.5">
                    <div v-for="approval in selectedRequest.approvals" :key="approval.id" class="flex items-center gap-3 px-3 py-2 bg-white dark:bg-background-dark rounded border border-zinc-100 dark:border-border-dark">
                      <div class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold"
                        :class="approval.decision === 'Approved' ? 'bg-emerald-100 text-emerald-700' : approval.decision === 'Rejected' ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-700'">
                        {{ approval.approvalLevel }}
                      </div>
                      <div class="flex-1 min-w-0">
                        <p class="text-xs font-medium text-zinc-700 dark:text-zinc-200">{{ approval.approverName || 'Unknown' }}</p>
                        <p v-if="approval.comments" class="text-[10px] text-zinc-400 italic truncate">{{ approval.comments }}</p>
                      </div>
                      <span class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full"
                        :class="approval.decision === 'Approved' ? 'bg-emerald-100 text-emerald-700' : approval.decision === 'Rejected' ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-700'">
                        {{ approval.decision }}
                      </span>
                      <span class="text-[10px] text-zinc-400">{{ formatDate(approval.decisionAt) }}</span>
                    </div>
                  </div>
                </div>

                <!-- Actions -->
                <div class="flex items-center gap-2">
                  <button
                    v-if="selectedRequest.status === 'PendingApproval' || selectedRequest.status === 'PartiallyApproved'"
                    @click="openApprovalModal(selectedRequest.id)"
                    class="px-3 py-1.5 text-xs font-medium text-white bg-teal hover:bg-teal/90 rounded-lg transition-colors"
                  >
                    Submit Approval
                  </button>
                  <button
                    v-if="selectedRequest.status === 'Approved'"
                    @click="executeBatchDisposal(selectedRequest.id)"
                    class="px-3 py-1.5 text-xs font-medium text-white bg-rose-600 hover:bg-rose-700 rounded-lg transition-colors"
                  >
                    Execute Disposal
                  </button>
                </div>
              </template>
            </div>
          </div>
        </div>
      </div>

      <!-- Pending Disposals -->
      <div v-if="activeTab === 'pending'" class="p-6">
        <div v-if="pendingDisposals.length === 0" class="text-center py-12 text-zinc-400">
          <span class="material-symbols-outlined text-4xl mb-2 block">check_circle</span>
          <p class="text-sm">No documents pending disposal</p>
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-left">
            <thead>
              <tr class="border-b border-zinc-200 dark:border-border-dark">
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Document</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Policy</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Expired</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Action</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Hold</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="doc in pendingDisposals" :key="doc.documentId" class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors">
                <td class="py-3 text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ doc.documentName }}</td>
                <td class="py-3 text-xs text-zinc-500">{{ doc.retentionPolicyName || '-' }}</td>
                <td class="py-3 text-xs text-rose-500">{{ formatDate(doc.retentionExpirationDate) }}</td>
                <td class="py-3 text-xs text-zinc-500">{{ doc.expirationAction || '-' }}</td>
                <td class="py-3">
                  <span v-if="doc.isOnLegalHold" class="px-2 py-0.5 text-[9px] font-bold uppercase bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 rounded-full">On Hold</span>
                  <span v-else class="text-[10px] text-zinc-400">-</span>
                </td>
                <td class="py-3 text-right">
                  <button
                    v-if="!doc.isOnLegalHold"
                    @click="executeDisposal(doc.documentId)"
                    class="px-3 py-1 text-xs font-medium text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-900/20 rounded transition-colors"
                  >
                    Execute
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Upcoming Disposals -->
      <div v-if="activeTab === 'upcoming'" class="p-6">
        <div v-if="upcomingDisposals.length === 0" class="text-center py-12 text-zinc-400">
          <span class="material-symbols-outlined text-4xl mb-2 block">event_available</span>
          <p class="text-sm">No upcoming disposals in the next 90 days</p>
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-left">
            <thead>
              <tr class="border-b border-zinc-200 dark:border-border-dark">
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Document</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Policy</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Expires</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Days Left</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="doc in upcomingDisposals" :key="doc.documentId" class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors">
                <td class="py-3 text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ doc.documentName }}</td>
                <td class="py-3 text-xs text-zinc-500">{{ doc.retentionPolicyName || '-' }}</td>
                <td class="py-3 text-xs text-amber-600">{{ formatDate(doc.retentionExpirationDate) }}</td>
                <td class="py-3">
                  <span class="text-xs font-medium" :class="(doc.daysUntilExpiration || getDaysUntil(doc.retentionExpirationDate)) <= 7 ? 'text-rose-500' : 'text-amber-600'">
                    {{ doc.daysUntilExpiration || getDaysUntil(doc.retentionExpirationDate) }}d
                  </span>
                </td>
                <td class="py-3 text-right">
                  <button @click="initiateDisposal(doc.documentId)" class="px-3 py-1 text-xs font-medium text-teal hover:bg-teal/10 rounded transition-colors">
                    Initiate
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Certificates -->
      <div v-if="activeTab === 'certificates'" class="p-6">
        <div v-if="certificates.length === 0" class="text-center py-12 text-zinc-400">
          <span class="material-symbols-outlined text-4xl mb-2 block">description</span>
          <p class="text-sm">No disposal certificates issued yet</p>
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-left">
            <thead>
              <tr class="border-b border-zinc-200 dark:border-border-dark">
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Certificate #</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Document</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Method</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Disposed</th>
                <th class="pb-3 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Disposed By</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="cert in certificates" :key="cert.id" class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors">
                <td class="py-3 text-xs font-mono text-teal">{{ cert.certificateNumber }}</td>
                <td class="py-3 text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ cert.documentName }}</td>
                <td class="py-3 text-xs text-zinc-500">{{ cert.disposalMethod }}</td>
                <td class="py-3 text-xs text-zinc-500">{{ formatDate(cert.disposedAt) }}</td>
                <td class="py-3 text-xs text-zinc-500">{{ cert.disposedByName || '-' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Approval Decision Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showApprovalModal" class="fixed inset-0 z-[100] flex items-center justify-center">
          <div class="absolute inset-0 bg-black/50" @click="showApprovalModal = false"></div>
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showApprovalModal" class="relative bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md overflow-hidden">
              <!-- Header -->
              <div class="px-6 py-4 bg-gradient-to-r from-[#0d1117] to-teal/80 relative overflow-hidden">
                <div class="absolute -right-4 -top-4 w-24 h-24 bg-white/5 rounded-full"></div>
                <div class="absolute -right-8 -bottom-8 w-32 h-32 bg-white/5 rounded-full"></div>
                <div class="relative flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-white/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-white">approval</span>
                  </div>
                  <div>
                    <h3 class="text-white font-bold">Submit Approval Decision</h3>
                    <p class="text-white/70 text-xs mt-0.5">Review and approve or reject this disposal request</p>
                  </div>
                </div>
              </div>

              <!-- Body -->
              <div class="p-6 space-y-4">
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-2">Decision</label>
                  <div class="grid grid-cols-3 gap-2">
                    <button
                      v-for="dec in [{ value: 'Approved', label: 'Approve', icon: 'check_circle', color: 'emerald' }, { value: 'Rejected', label: 'Reject', icon: 'cancel', color: 'rose' }, { value: 'ReturnedForReview', label: 'Return', icon: 'replay', color: 'amber' }]"
                      :key="dec.value"
                      @click="approvalForm.decision = dec.value"
                      :class="approvalForm.decision === dec.value ? `border-${dec.color}-500 bg-${dec.color}-50 dark:bg-${dec.color}-900/20 ring-1 ring-${dec.color}-300` : 'border-zinc-200 dark:border-zinc-600 hover:border-zinc-300'"
                      class="p-3 border rounded-lg text-center transition-all"
                    >
                      <span class="material-symbols-outlined text-xl" :class="approvalForm.decision === dec.value ? `text-${dec.color}-600` : 'text-zinc-400'">{{ dec.icon }}</span>
                      <p class="text-[10px] font-medium text-zinc-600 dark:text-zinc-300 mt-1">{{ dec.label }}</p>
                    </button>
                  </div>
                </div>
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Comments</label>
                  <textarea
                    v-model="approvalForm.comments"
                    rows="3"
                    :placeholder="approvalForm.decision === 'Rejected' ? 'Reason for rejection (required)...' : 'Optional comments...'"
                    class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none"
                  ></textarea>
                </div>
              </div>

              <!-- Footer -->
              <div class="px-6 py-4 bg-zinc-50 dark:bg-zinc-800/50 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3">
                <button @click="showApprovalModal = false" class="px-4 py-2 text-sm font-medium text-zinc-600 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-white transition-colors">Cancel</button>
                <button
                  @click="submitApproval"
                  :disabled="isSubmitting"
                  class="px-4 py-2 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-50 flex items-center gap-2"
                  :class="approvalForm.decision === 'Rejected' ? 'bg-rose-600 hover:bg-rose-700' : approvalForm.decision === 'ReturnedForReview' ? 'bg-amber-600 hover:bg-amber-700' : 'bg-teal hover:bg-teal/90'"
                >
                  <span v-if="isSubmitting" class="material-symbols-outlined text-sm animate-spin">refresh</span>
                  {{ isSubmitting ? 'Submitting...' : 'Submit Decision' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
