<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { retentionPoliciesApi } from '@/api/client'
import type { RetentionPolicy, DocumentRetention } from '@/types'
import { UiSelect, UiCheckbox } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const policies = ref<RetentionPolicy[]>([])
const expiringDocuments = ref<DocumentRetention[]>([])
const pendingReviews = ref<DocumentRetention[]>([])
const isLoading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const activeTab = ref<'policies' | 'expiring' | 'pending'>('policies')

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
  isActive: true
})

const expirationActions = [
  { value: 'Review', label: 'Review', description: 'Requires manual review before action' },
  { value: 'Archive', label: 'Archive', description: 'Move to archive storage' },
  { value: 'Delete', label: 'Delete', description: 'Permanently delete (if approved)' },
  { value: 'Notify', label: 'Notify Only', description: 'Only send notification' }
]

const retentionPresets = [
  { label: '30 Days', days: 30 },
  { label: '90 Days', days: 90 },
  { label: '1 Year', days: 365 },
  { label: '3 Years', days: 1095 },
  { label: '7 Years', days: 2555 },
  { label: 'Permanent', days: 0 }
]

onMounted(() => {
  loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [policiesRes, expiringRes, pendingRes] = await Promise.all([
      retentionPoliciesApi.getAll(true),
      retentionPoliciesApi.getExpiringDocuments(30),
      retentionPoliciesApi.getPendingReview()
    ])
    policies.value = policiesRes.data
    expiringDocuments.value = expiringRes.data
    pendingReviews.value = pendingRes.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
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
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(policy: RetentionPolicy) {
  formData.value = {
    id: policy.id,
    name: policy.name,
    description: policy.description || '',
    retentionDays: policy.retentionDays,
    expirationAction: policy.expirationAction,
    notifyBeforeExpiration: policy.notifyBeforeExpiration,
    notificationDays: policy.notificationDays,
    requiresApproval: policy.requiresApproval,
    inheritToSubfolders: policy.inheritToSubfolders,
    isLegalHold: policy.isLegalHold,
    isActive: policy.isActive
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
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
      isLegalHold: formData.value.isLegalHold
    }

    if (isEditing.value) {
      await retentionPoliciesApi.update(formData.value.id, { ...data, isActive: formData.value.isActive })
    } else {
      await retentionPoliciesApi.create(data)
    }
    showModal.value = false
    await loadData()
  } catch (err) {
  }
}

async function deletePolicy(id: string) {
  if (!confirm('Are you sure you want to delete this retention policy?')) return
  try {
    await retentionPoliciesApi.delete(id)
    await loadData()
  } catch (err) {
  }
}

async function approveRetention(retentionId: string) {
  const notes = prompt('Enter approval notes (optional):')
  try {
    await retentionPoliciesApi.approveRetentionAction(retentionId, notes || undefined)
    await loadData()
  } catch (err) {
  }
}

function formatRetentionPeriod(days: number): string {
  if (days === 0) return 'Permanent'
  if (days < 30) return `${days} days`
  if (days < 365) return `${Math.round(days / 30)} months`
  return `${Math.round(days / 365)} years`
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString()
}

function getActionColor(action: string): string {
  const colors: Record<string, string> = {
    Review: 'bg-amber-100 text-amber-700',
    Archive: 'bg-blue-100 text-blue-700',
    Delete: 'bg-red-100 text-red-700',
    Notify: 'bg-green-100 text-green-700'
  }
  return colors[action] || 'bg-gray-100 text-gray-700'
}

function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    Active: 'bg-green-100 text-green-700',
    PendingReview: 'bg-amber-100 text-amber-700',
    Approved: 'bg-blue-100 text-blue-700',
    Archived: 'bg-gray-100 text-gray-500',
    Deleted: 'bg-red-100 text-red-700',
    OnHold: 'bg-purple-100 text-purple-700'
  }
  return colors[status] || 'bg-gray-100 text-gray-700'
}

const expirationActionOptions = computed(() =>
  expirationActions.map(action => ({
    value: action.value,
    label: `${action.label} - ${action.description}`
  }))
)
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Retention Policies" icon="schedule" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Retention Policies</h1>
          <p class="text-gray-500 mt-1">Define document lifecycle and compliance policies</p>
        </div>
        <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Policy
        </button>
      </div>

      <!-- Tabs -->
      <div class="border-b border-gray-200 mb-6">
        <nav class="flex gap-6">
          <button
            @click="activeTab = 'policies'"
            :class="activeTab === 'policies' ? 'border-teal text-teal' : 'border-transparent text-gray-500 hover:text-gray-700'"
            class="py-3 border-b-2 font-medium"
          >
            Policies
          </button>
          <button
            @click="activeTab = 'expiring'"
            :class="activeTab === 'expiring' ? 'border-teal text-teal' : 'border-transparent text-gray-500 hover:text-gray-700'"
            class="py-3 border-b-2 font-medium flex items-center gap-2"
          >
            Expiring Soon
            <span v-if="expiringDocuments.length > 0" class="px-2 py-0.5 text-xs bg-amber-100 text-amber-700 rounded-full">
              {{ expiringDocuments.length }}
            </span>
          </button>
          <button
            @click="activeTab = 'pending'"
            :class="activeTab === 'pending' ? 'border-teal text-teal' : 'border-transparent text-gray-500 hover:text-gray-700'"
            class="py-3 border-b-2 font-medium flex items-center gap-2"
          >
            Pending Review
            <span v-if="pendingReviews.length > 0" class="px-2 py-0.5 text-xs bg-red-100 text-red-700 rounded-full">
              {{ pendingReviews.length }}
            </span>
          </button>
        </nav>
      </div>

      <div v-if="isLoading" class="text-center py-12 text-gray-500">Loading...</div>

      <!-- Policies Tab -->
      <div v-else-if="activeTab === 'policies'" class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Policy Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Retention</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Action</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Options</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="policies.length === 0">
              <td colspan="6" class="py-12 text-center text-gray-500">No retention policies defined</td>
            </tr>
            <tr v-for="policy in policies" :key="policy.id" class="border-t border-gray-100 hover:bg-gray-50">
              <td class="py-3 px-4">
                <div class="flex items-center gap-2">
                  <p class="font-medium text-gray-900">{{ policy.name }}</p>
                  <span v-if="policy.isLegalHold" class="px-2 py-0.5 text-xs bg-purple-100 text-purple-700 rounded-full">Legal Hold</span>
                </div>
                <p v-if="policy.description" class="text-xs text-gray-500">{{ policy.description }}</p>
              </td>
              <td class="py-3 px-4 text-sm">{{ formatRetentionPeriod(policy.retentionDays) }}</td>
              <td class="py-3 px-4">
                <span :class="getActionColor(policy.expirationAction)" class="px-2 py-1 text-xs rounded-full">
                  {{ policy.expirationAction }}
                </span>
              </td>
              <td class="py-3 px-4 text-xs text-gray-500">
                <div class="space-y-0.5">
                  <div v-if="policy.notifyBeforeExpiration">Notify {{ policy.notificationDays }}d before</div>
                  <div v-if="policy.requiresApproval">Requires approval</div>
                  <div v-if="policy.inheritToSubfolders">Inherits to subfolders</div>
                </div>
              </td>
              <td class="py-3 px-4">
                <span :class="policy.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2 py-1 text-xs rounded-full">
                  {{ policy.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <button @click="openEditModal(policy)" class="p-1 text-gray-400 hover:text-teal mr-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button @click="deletePolicy(policy.id)" class="p-1 text-gray-400 hover:text-red-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Expiring Soon Tab -->
      <div v-else-if="activeTab === 'expiring'" class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Document</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Policy</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Expires</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="expiringDocuments.length === 0">
              <td colspan="4" class="py-12 text-center text-gray-500">No documents expiring soon</td>
            </tr>
            <tr v-for="doc in expiringDocuments" :key="doc.id" class="border-t border-gray-100 hover:bg-gray-50">
              <td class="py-3 px-4 font-medium text-gray-900">{{ doc.documentName }}</td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ doc.policyName }}</td>
              <td class="py-3 px-4 text-sm text-amber-600 font-medium">{{ doc.expirationDate ? formatDate(doc.expirationDate) : 'N/A' }}</td>
              <td class="py-3 px-4">
                <span :class="getStatusColor(doc.status)" class="px-2 py-1 text-xs rounded-full">{{ doc.status }}</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pending Review Tab -->
      <div v-else-if="activeTab === 'pending'" class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Document</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Policy</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Expired</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="pendingReviews.length === 0">
              <td colspan="4" class="py-12 text-center text-gray-500">No documents pending review</td>
            </tr>
            <tr v-for="doc in pendingReviews" :key="doc.id" class="border-t border-gray-100 hover:bg-gray-50">
              <td class="py-3 px-4 font-medium text-gray-900">{{ doc.documentName }}</td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ doc.policyName }}</td>
              <td class="py-3 px-4 text-sm text-red-600">{{ doc.expirationDate ? formatDate(doc.expirationDate) : 'N/A' }}</td>
              <td class="py-3 px-4 text-right">
                <button @click="approveRetention(doc.id)" class="px-3 py-1 text-sm bg-green-600 text-white rounded hover:bg-green-700">
                  Approve
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg mx-4 p-6 max-h-[90vh] overflow-y-auto">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Policy' : 'New Retention Policy' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Policy Name *</label>
            <input v-model="formData.name" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg" placeholder="e.g., 7-Year Financial Records" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Retention Period</label>
            <div class="flex gap-2 mb-2">
              <button
                v-for="preset in retentionPresets"
                :key="preset.days"
                @click="formData.retentionDays = preset.days"
                :class="formData.retentionDays === preset.days ? 'bg-teal/10 border-teal text-teal' : 'border-gray-200 hover:border-gray-300'"
                class="px-3 py-1 text-sm border rounded-full"
              >
                {{ preset.label }}
              </button>
            </div>
            <input v-model.number="formData.retentionDays" type="number" min="0" class="w-full px-4 py-2 border border-gray-300 rounded-lg" />
            <p class="text-xs text-gray-500 mt-1">Days (0 = permanent retention)</p>
          </div>

          <UiSelect
            v-model="formData.expirationAction"
            :options="expirationActionOptions"
            label="Expiration Action"
            placeholder="Select action"
          />

          <div class="grid grid-cols-2 gap-4">
            <UiCheckbox v-model="formData.notifyBeforeExpiration" label="Notify before expiration" />
            <div v-if="formData.notifyBeforeExpiration">
              <input v-model.number="formData.notificationDays" type="number" min="1" class="w-full px-3 py-1 border border-gray-300 rounded-lg text-sm" />
              <p class="text-xs text-gray-500">days before</p>
            </div>
          </div>

          <div class="space-y-2">
            <UiCheckbox v-model="formData.requiresApproval" label="Require approval before action" />
            <UiCheckbox v-model="formData.inheritToSubfolders" label="Inherit to subfolders" />
            <UiCheckbox v-model="formData.isLegalHold" label="Legal hold (prevents deletion)" color="purple" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea v-model="formData.description" rows="2" class="w-full px-4 py-2 border border-gray-300 rounded-lg" placeholder="Describe the purpose of this policy"></textarea>
          </div>

          <UiCheckbox v-if="isEditing" v-model="formData.isActive" label="Active" />
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
          <button @click="handleSave" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">Save</button>
        </div>
      </div>
    </div>
  </div>
</template>
