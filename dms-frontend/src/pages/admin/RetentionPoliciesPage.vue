<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { retentionPoliciesApi } from '@/api/client'
import type { RetentionPolicy, DocumentRetention } from '@/types'

const policies = ref<RetentionPolicy[]>([])
const expiringDocuments = ref<DocumentRetention[]>([])
const pendingReviews = ref<DocumentRetention[]>([])
const isLoading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const isSaving = ref(false)

const formData = ref({
  id: '',
  name: '',
  description: '',
  retentionDays: 365,
  expirationAction: 'Review',
  notifyBeforeExpiration: true,
  notificationDays: 30,
  requiresApproval: true,
  inheritToSubfolders: true,
  isLegalHold: false,
  isActive: true,
  retentionBasis: 'CreationDate',
  suspendDuringLegalHold: true,
  recalculateOnClassificationChange: true,
  disposalApprovalLevels: 1
})

const expirationActions = [
  { value: 'Review', label: 'Review', icon: 'visibility' },
  { value: 'Archive', label: 'Archive', icon: 'archive' },
  { value: 'Delete', label: 'Delete', icon: 'delete' },
  { value: 'Notify', label: 'Notify Only', icon: 'notifications' }
]

const retentionBases = [
  { value: 'CreationDate', label: 'Creation Date', desc: 'From when the document was created' },
  { value: 'LastModifiedDate', label: 'Last Modified', desc: 'From last modification' },
  { value: 'ClassificationDate', label: 'Classification Date', desc: 'From when classified' },
  { value: 'CustomEvent', label: 'Custom Event', desc: 'Triggered by specific event' }
]

const retentionPresets = [
  { label: '30d', days: 30 },
  { label: '90d', days: 90 },
  { label: '1y', days: 365 },
  { label: '3y', days: 1095 },
  { label: '7y', days: 2555 },
  { label: '∞', days: 0 }
]

onMounted(() => loadData())

async function loadData() {
  isLoading.value = true
  try {
    const [policiesRes, expiringRes, pendingRes] = await Promise.allSettled([
      retentionPoliciesApi.getAll(true),
      retentionPoliciesApi.getExpiringDocuments(30),
      retentionPoliciesApi.getPendingReview()
    ])
    if (policiesRes.status === 'fulfilled') policies.value = policiesRes.value.data || []
    if (expiringRes.status === 'fulfilled') expiringDocuments.value = expiringRes.value.data || []
    if (pendingRes.status === 'fulfilled') pendingReviews.value = pendingRes.value.data || []
  } catch { /* silently fail */ }
  finally { isLoading.value = false }
}

function openCreateModal() {
  formData.value = {
    id: '', name: '', description: '', retentionDays: 365, expirationAction: 'Review',
    notifyBeforeExpiration: true, notificationDays: 30, requiresApproval: true,
    inheritToSubfolders: true, isLegalHold: false, isActive: true,
    retentionBasis: 'CreationDate', suspendDuringLegalHold: true,
    recalculateOnClassificationChange: true, disposalApprovalLevels: 1
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(policy: RetentionPolicy) {
  formData.value = {
    id: policy.id, name: policy.name, description: policy.description || '',
    retentionDays: policy.retentionDays, expirationAction: policy.expirationAction,
    notifyBeforeExpiration: policy.notifyBeforeExpiration, notificationDays: policy.notificationDays,
    requiresApproval: policy.requiresApproval, inheritToSubfolders: policy.inheritToSubfolders,
    isLegalHold: policy.isLegalHold, isActive: policy.isActive,
    retentionBasis: policy.retentionBasis || 'CreationDate',
    suspendDuringLegalHold: policy.suspendDuringLegalHold ?? true,
    recalculateOnClassificationChange: policy.recalculateOnClassificationChange ?? true,
    disposalApprovalLevels: policy.disposalApprovalLevels ?? 1
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    const data = {
      name: formData.value.name,
      description: formData.value.description || undefined,
      retentionDays: formData.value.retentionDays,
      expirationAction: formData.value.expirationAction,
      notifyBeforeExpiration: formData.value.notifyBeforeExpiration,
      notificationDays: formData.value.notificationDays,
      requiresApproval: formData.value.requiresApproval,
      inheritToSubfolders: formData.value.inheritToSubfolders,
      isLegalHold: formData.value.isLegalHold,
      retentionBasis: formData.value.retentionBasis,
      suspendDuringLegalHold: formData.value.suspendDuringLegalHold,
      recalculateOnClassificationChange: formData.value.recalculateOnClassificationChange,
      disposalApprovalLevels: formData.value.disposalApprovalLevels
    }
    if (isEditing.value) {
      await retentionPoliciesApi.update(formData.value.id, { ...data, isActive: formData.value.isActive })
    } else {
      await retentionPoliciesApi.create(data)
    }
    showModal.value = false
    await loadData()
  } catch { /* silently fail */ }
  finally { isSaving.value = false }
}

async function deletePolicy(id: string) {
  if (!confirm('Are you sure you want to delete this retention policy?')) return
  try {
    await retentionPoliciesApi.delete(id)
    await loadData()
  } catch { /* silently fail */ }
}

async function approveRetention(retentionId: string) {
  const notes = prompt('Enter approval notes (optional):')
  try {
    await retentionPoliciesApi.approveRetentionAction(retentionId, notes || undefined)
    await loadData()
  } catch { /* silently fail */ }
}

function formatRetentionPeriod(days: number): string {
  if (days === 0) return 'Permanent'
  if (days < 30) return `${days} days`
  if (days < 365) return `${Math.round(days / 30)} months`
  return `${(days / 365).toFixed(1).replace('.0', '')} years`
}

function formatDate(dateStr: string): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}

const actionColors: Record<string, string> = {
  Review: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  Archive: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  Delete: 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400',
  Notify: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
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
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Retention Policies</h1>
          <p class="text-zinc-500 text-sm mt-0.5">Define document lifecycle and compliance policies</p>
        </div>
      </div>
      <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors font-medium text-sm shadow-sm">
        <span class="material-symbols-outlined text-lg">add</span>
        New Policy
      </button>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-3 gap-4">
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-teal text-lg">schedule</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Policies</span>
          </div>
          <p class="text-2xl font-bold">{{ policies.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">{{ policies.filter(p => p.isActive).length }} active</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-amber-400 text-lg">warning</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Expiring</span>
          </div>
          <p class="text-2xl font-bold">{{ expiringDocuments.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Next 30 days</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-5 rounded-lg text-white shadow-xl border border-zinc-800/50 relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-20 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-2">
            <span class="material-symbols-outlined text-rose-400 text-lg">rate_review</span>
            <span class="text-[9px] font-bold text-zinc-500 uppercase tracking-widest">Pending</span>
          </div>
          <p class="text-2xl font-bold">{{ pendingReviews.length }}</p>
          <p class="text-[10px] text-zinc-500 mt-1">Awaiting review</p>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex items-center justify-center py-12 gap-3">
      <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
      <span class="text-zinc-500">Loading policies...</span>
    </div>

    <template v-else>
      <!-- Policies List -->
      <div class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden shadow-sm">
        <div class="px-5 py-3.5 border-b border-zinc-200 dark:border-border-dark flex items-center justify-between">
          <h2 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
            <span class="material-symbols-outlined text-lg text-teal">schedule</span>
            All Policies
          </h2>
        </div>
        <div v-if="policies.length === 0" class="p-12 text-center text-zinc-400">
          <span class="material-symbols-outlined text-4xl mb-2 block">schedule</span>
          <p class="text-sm">No retention policies defined</p>
        </div>
        <table v-else class="w-full text-left">
          <thead>
            <tr class="border-b border-zinc-200 dark:border-border-dark">
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Policy</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Retention</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Basis</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Action</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Approvals</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Flags</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Status</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="policy in policies" :key="policy.id" class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors">
              <td class="px-5 py-3">
                <div class="flex items-center gap-2">
                  <span class="text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ policy.name }}</span>
                  <span v-if="policy.isLegalHold" class="px-1.5 py-0.5 text-[9px] font-bold uppercase bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400 rounded-full">Hold</span>
                </div>
                <p v-if="policy.description" class="text-[10px] text-zinc-400 mt-0.5 line-clamp-1">{{ policy.description }}</p>
              </td>
              <td class="px-5 py-3 text-xs font-medium text-zinc-600 dark:text-zinc-300">{{ formatRetentionPeriod(policy.retentionDays) }}</td>
              <td class="px-5 py-3 text-[10px] text-zinc-500">{{ policy.retentionBasis || 'CreationDate' }}</td>
              <td class="px-5 py-3">
                <span class="px-2 py-0.5 text-[10px] font-bold rounded-full" :class="actionColors[policy.expirationAction] || 'bg-zinc-100 text-zinc-500'">
                  {{ policy.expirationAction }}
                </span>
              </td>
              <td class="px-5 py-3 text-xs text-zinc-500">{{ policy.disposalApprovalLevels || 1 }} level{{ (policy.disposalApprovalLevels || 1) > 1 ? 's' : '' }}</td>
              <td class="px-5 py-3">
                <div class="flex items-center gap-1 flex-wrap">
                  <span v-if="policy.suspendDuringLegalHold !== false" class="text-[9px] px-1.5 py-0.5 bg-zinc-100 dark:bg-zinc-700 text-zinc-500 dark:text-zinc-400 rounded" title="Suspends during legal hold">Hold</span>
                  <span v-if="policy.recalculateOnClassificationChange !== false" class="text-[9px] px-1.5 py-0.5 bg-zinc-100 dark:bg-zinc-700 text-zinc-500 dark:text-zinc-400 rounded" title="Recalculates on classification change">Recalc</span>
                  <span v-if="policy.notifyBeforeExpiration" class="text-[9px] px-1.5 py-0.5 bg-zinc-100 dark:bg-zinc-700 text-zinc-500 dark:text-zinc-400 rounded">{{ policy.notificationDays }}d</span>
                  <span v-if="policy.inheritToSubfolders" class="text-[9px] px-1.5 py-0.5 bg-zinc-100 dark:bg-zinc-700 text-zinc-500 dark:text-zinc-400 rounded">Inherit</span>
                </div>
              </td>
              <td class="px-5 py-3">
                <span class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full"
                  :class="policy.isActive ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400' : 'bg-zinc-100 text-zinc-500 dark:bg-zinc-700 dark:text-zinc-400'">
                  {{ policy.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="px-5 py-3 text-right">
                <button @click="openEditModal(policy)" class="p-1.5 text-zinc-400 hover:text-teal transition-colors" title="Edit">
                  <span class="material-symbols-outlined text-sm">edit</span>
                </button>
                <button @click="deletePolicy(policy.id)" class="p-1.5 text-zinc-400 hover:text-rose-600 transition-colors ml-1" title="Delete">
                  <span class="material-symbols-outlined text-sm">delete</span>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Expiring Documents -->
      <div v-if="expiringDocuments.length > 0" class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden shadow-sm">
        <div class="px-5 py-3.5 border-b border-zinc-200 dark:border-border-dark">
          <h2 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
            <span class="material-symbols-outlined text-lg text-amber-500">warning</span>
            Expiring Soon (30 days)
            <span class="ml-1 px-2 py-0.5 text-[10px] font-bold bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 rounded-full">{{ expiringDocuments.length }}</span>
          </h2>
        </div>
        <table class="w-full text-left">
          <thead>
            <tr class="border-b border-zinc-200 dark:border-border-dark">
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Document</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Policy</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Expires</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="doc in expiringDocuments" :key="doc.id" class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors">
              <td class="px-5 py-3 text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ doc.documentName }}</td>
              <td class="px-5 py-3 text-xs text-zinc-500">{{ doc.policyName }}</td>
              <td class="px-5 py-3 text-xs text-amber-600 font-medium">{{ doc.expirationDate ? formatDate(doc.expirationDate) : '-' }}</td>
              <td class="px-5 py-3">
                <span class="px-2 py-0.5 text-[9px] font-bold uppercase rounded-full bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">{{ doc.status }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pending Review -->
      <div v-if="pendingReviews.length > 0" class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden shadow-sm">
        <div class="px-5 py-3.5 border-b border-zinc-200 dark:border-border-dark">
          <h2 class="text-sm font-bold text-zinc-700 dark:text-zinc-200 flex items-center gap-2">
            <span class="material-symbols-outlined text-lg text-rose-500">rate_review</span>
            Pending Review
            <span class="ml-1 px-2 py-0.5 text-[10px] font-bold bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400 rounded-full">{{ pendingReviews.length }}</span>
          </h2>
        </div>
        <table class="w-full text-left">
          <thead>
            <tr class="border-b border-zinc-200 dark:border-border-dark">
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Document</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Policy</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Expired</th>
              <th class="px-5 pb-3 pt-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="doc in pendingReviews" :key="doc.id" class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors">
              <td class="px-5 py-3 text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ doc.documentName }}</td>
              <td class="px-5 py-3 text-xs text-zinc-500">{{ doc.policyName }}</td>
              <td class="px-5 py-3 text-xs text-rose-500 font-medium">{{ doc.expirationDate ? formatDate(doc.expirationDate) : '-' }}</td>
              <td class="px-5 py-3 text-right">
                <button @click="approveRetention(doc.id)" class="px-3 py-1.5 text-xs font-medium text-white bg-teal hover:bg-teal/90 rounded-lg transition-colors">
                  Approve
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>

    <!-- Create/Edit Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showModal" class="fixed inset-0 z-[100] flex items-center justify-center">
          <div class="absolute inset-0 bg-black/50" @click="showModal = false"></div>
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showModal" class="relative bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-lg overflow-hidden max-h-[90vh] flex flex-col">
              <!-- Header -->
              <div class="px-6 py-4 bg-gradient-to-r from-[#0d1117] to-teal/80 relative overflow-hidden shrink-0">
                <div class="absolute -right-4 -top-4 w-24 h-24 bg-white/5 rounded-full"></div>
                <div class="absolute -right-8 -bottom-8 w-32 h-32 bg-white/5 rounded-full"></div>
                <div class="relative flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-white/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-white">schedule</span>
                  </div>
                  <div>
                    <h3 class="text-white font-bold">{{ isEditing ? 'Edit Policy' : 'New Retention Policy' }}</h3>
                    <p class="text-white/70 text-xs mt-0.5">Define document lifecycle rules</p>
                  </div>
                </div>
              </div>

              <!-- Body -->
              <div class="p-6 space-y-4 overflow-y-auto">
                <!-- Name -->
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Policy Name *</label>
                  <input v-model="formData.name" type="text" class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none" placeholder="e.g., 7-Year Financial Records" />
                </div>

                <!-- Retention Period -->
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Retention Period</label>
                  <div class="flex gap-1.5 mb-2">
                    <button
                      v-for="preset in retentionPresets"
                      :key="preset.days"
                      @click="formData.retentionDays = preset.days"
                      :class="formData.retentionDays === preset.days ? 'bg-teal/10 border-teal text-teal' : 'border-zinc-200 dark:border-zinc-600 hover:border-zinc-300 text-zinc-600 dark:text-zinc-400'"
                      class="px-2.5 py-1 text-xs font-medium border rounded-full transition-colors"
                    >
                      {{ preset.label }}
                    </button>
                  </div>
                  <input v-model.number="formData.retentionDays" type="number" min="0" class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none" />
                  <p class="text-[10px] text-zinc-400 mt-1">Days (0 = permanent retention)</p>
                </div>

                <!-- Retention Basis -->
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Retention Basis</label>
                  <div class="grid grid-cols-2 gap-2">
                    <button
                      v-for="basis in retentionBases"
                      :key="basis.value"
                      @click="formData.retentionBasis = basis.value"
                      :class="formData.retentionBasis === basis.value ? 'border-teal bg-teal/5 ring-1 ring-teal/30' : 'border-zinc-200 dark:border-zinc-600 hover:border-zinc-300'"
                      class="p-2.5 border rounded-lg text-left transition-all"
                    >
                      <p class="text-xs font-medium text-zinc-700 dark:text-zinc-200">{{ basis.label }}</p>
                      <p class="text-[10px] text-zinc-400 mt-0.5">{{ basis.desc }}</p>
                    </button>
                  </div>
                </div>

                <!-- Expiration Action -->
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Expiration Action</label>
                  <div class="grid grid-cols-4 gap-2">
                    <button
                      v-for="action in expirationActions"
                      :key="action.value"
                      @click="formData.expirationAction = action.value"
                      :class="formData.expirationAction === action.value ? 'border-teal bg-teal/5 ring-1 ring-teal/30' : 'border-zinc-200 dark:border-zinc-600 hover:border-zinc-300'"
                      class="p-2 border rounded-lg text-center transition-all"
                    >
                      <span class="material-symbols-outlined text-lg" :class="formData.expirationAction === action.value ? 'text-teal' : 'text-zinc-400'">{{ action.icon }}</span>
                      <p class="text-[10px] font-medium text-zinc-600 dark:text-zinc-300 mt-0.5">{{ action.label }}</p>
                    </button>
                  </div>
                </div>

                <!-- Disposal Approval Levels -->
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Disposal Approval Levels</label>
                  <div class="flex items-center gap-3">
                    <input v-model.number="formData.disposalApprovalLevels" type="number" min="1" max="5" class="w-20 px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white text-center focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none" />
                    <p class="text-[10px] text-zinc-400">approval{{ formData.disposalApprovalLevels > 1 ? 's' : '' }} required before disposal</p>
                  </div>
                </div>

                <!-- Notification -->
                <div class="flex items-center gap-3">
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" v-model="formData.notifyBeforeExpiration" class="rounded border-zinc-300 text-teal focus:ring-teal" />
                    <span class="text-xs text-zinc-700 dark:text-zinc-300">Notify before expiration</span>
                  </label>
                  <input v-if="formData.notifyBeforeExpiration" v-model.number="formData.notificationDays" type="number" min="1" class="w-16 px-2 py-1 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded text-xs text-zinc-900 dark:text-white text-center" />
                  <span v-if="formData.notifyBeforeExpiration" class="text-[10px] text-zinc-400">days before</span>
                </div>

                <!-- Toggles -->
                <div class="space-y-2.5 pt-2 border-t border-zinc-100 dark:border-border-dark">
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" v-model="formData.requiresApproval" class="rounded border-zinc-300 text-teal focus:ring-teal" />
                    <span class="text-xs text-zinc-700 dark:text-zinc-300">Require approval before action</span>
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" v-model="formData.inheritToSubfolders" class="rounded border-zinc-300 text-teal focus:ring-teal" />
                    <span class="text-xs text-zinc-700 dark:text-zinc-300">Inherit to subfolders</span>
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" v-model="formData.suspendDuringLegalHold" class="rounded border-zinc-300 text-teal focus:ring-teal" />
                    <span class="text-xs text-zinc-700 dark:text-zinc-300">Suspend during legal hold</span>
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" v-model="formData.recalculateOnClassificationChange" class="rounded border-zinc-300 text-teal focus:ring-teal" />
                    <span class="text-xs text-zinc-700 dark:text-zinc-300">Recalculate on classification change</span>
                  </label>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" v-model="formData.isLegalHold" class="rounded border-zinc-300 text-purple-600 focus:ring-purple-500" />
                    <span class="text-xs text-zinc-700 dark:text-zinc-300">Legal hold (prevents deletion)</span>
                  </label>
                  <label v-if="isEditing" class="flex items-center gap-2 cursor-pointer">
                    <input type="checkbox" v-model="formData.isActive" class="rounded border-zinc-300 text-teal focus:ring-teal" />
                    <span class="text-xs text-zinc-700 dark:text-zinc-300">Active</span>
                  </label>
                </div>

                <!-- Description -->
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Description</label>
                  <textarea v-model="formData.description" rows="2" class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none" placeholder="Describe the purpose of this policy"></textarea>
                </div>
              </div>

              <!-- Footer -->
              <div class="px-6 py-4 bg-zinc-50 dark:bg-zinc-800/50 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3 shrink-0">
                <button @click="showModal = false" class="px-4 py-2 text-sm font-medium text-zinc-600 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-white transition-colors">Cancel</button>
                <button @click="handleSave" :disabled="isSaving || !formData.name" class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 flex items-center gap-2">
                  <span v-if="isSaving" class="material-symbols-outlined text-sm animate-spin">refresh</span>
                  {{ isSaving ? 'Saving...' : 'Save Policy' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
