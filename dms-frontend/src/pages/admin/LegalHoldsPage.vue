<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { legalHoldsApi } from '@/api/client'
import type { LegalHold, LegalHoldDocument } from '@/types'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const holds = ref<LegalHold[]>([])
const isLoading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const activeFilter = ref<'all' | 'active' | 'released'>('active')

// Detail panel
const selectedHold = ref<LegalHold | null>(null)
const holdDocuments = ref<LegalHoldDocument[]>([])
const loadingDocuments = ref(false)

// Release modal
const showReleaseModal = ref(false)
const releaseTarget = ref<LegalHold | null>(null)
const releaseReason = ref('')
const isReleasing = ref(false)

const formData = ref({
  id: '',
  name: '',
  description: '',
  caseReference: '',
  requestedBy: '',
  effectiveFrom: '',
  effectiveUntil: '',
  notes: ''
})

onMounted(() => {
  loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const res = await legalHoldsApi.getAll(activeFilter.value === 'active')
    holds.value = res.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

const filteredHolds = computed(() => {
  if (activeFilter.value === 'all') return holds.value
  if (activeFilter.value === 'released') return holds.value.filter(h => h.status === 'Released')
  return holds.value.filter(h => h.status === 'Active')
})

const stats = computed(() => ({
  total: holds.value.length,
  active: holds.value.filter(h => h.status === 'Active').length,
  released: holds.value.filter(h => h.status === 'Released').length,
  documentsHeld: holds.value.filter(h => h.status === 'Active').reduce((sum, h) => sum + h.documentCount, 0)
}))

function openCreateModal() {
  formData.value = {
    id: '',
    name: '',
    description: '',
    caseReference: '',
    requestedBy: '',
    effectiveFrom: new Date().toISOString().split('T')[0],
    effectiveUntil: '',
    notes: ''
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(hold: LegalHold) {
  formData.value = {
    id: hold.id,
    name: hold.name,
    description: hold.description || '',
    caseReference: hold.caseReference || '',
    requestedBy: hold.requestedBy || '',
    effectiveFrom: hold.effectiveFrom ? hold.effectiveFrom.split('T')[0] : '',
    effectiveUntil: hold.effectiveUntil ? hold.effectiveUntil.split('T')[0] : '',
    notes: hold.notes || ''
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  try {
    if (isEditing.value) {
      await legalHoldsApi.update(formData.value.id, {
        name: formData.value.name,
        description: formData.value.description || undefined,
        caseReference: formData.value.caseReference || undefined,
        requestedBy: formData.value.requestedBy || undefined,
        effectiveUntil: formData.value.effectiveUntil || undefined,
        notes: formData.value.notes || undefined
      })
    } else {
      await legalHoldsApi.create({
        name: formData.value.name,
        description: formData.value.description || undefined,
        caseReference: formData.value.caseReference || undefined,
        requestedBy: formData.value.requestedBy || undefined,
        effectiveFrom: formData.value.effectiveFrom || undefined,
        effectiveUntil: formData.value.effectiveUntil || undefined,
        notes: formData.value.notes || undefined
      })
    }
    showModal.value = false
    await loadData()
  } catch (err) {
  }
}

async function selectHold(hold: LegalHold) {
  selectedHold.value = hold
  loadingDocuments.value = true
  try {
    const res = await legalHoldsApi.getDocuments(hold.id)
    holdDocuments.value = res.data
  } catch (err) {
    holdDocuments.value = []
  } finally {
    loadingDocuments.value = false
  }
}

function closeDetail() {
  selectedHold.value = null
  holdDocuments.value = []
}

async function removeDocumentFromHold(documentId: string) {
  if (!selectedHold.value) return
  if (!confirm('Remove this document from the legal hold?')) return
  try {
    await legalHoldsApi.removeDocument(selectedHold.value.id, documentId)
    holdDocuments.value = holdDocuments.value.filter(d => d.documentId !== documentId)
    // Update the count in the list
    const hold = holds.value.find(h => h.id === selectedHold.value!.id)
    if (hold) hold.documentCount--
  } catch (err) {
  }
}

function openReleaseModal(hold: LegalHold) {
  releaseTarget.value = hold
  releaseReason.value = ''
  showReleaseModal.value = true
}

async function confirmRelease() {
  if (!releaseTarget.value || !releaseReason.value.trim()) return
  isReleasing.value = true
  try {
    await legalHoldsApi.release(releaseTarget.value.id, releaseReason.value)
    showReleaseModal.value = false
    releaseTarget.value = null
    releaseReason.value = ''
    if (selectedHold.value) closeDetail()
    await loadData()
  } catch (err) {
  } finally {
    isReleasing.value = false
  }
}

function changeFilter(filter: 'all' | 'active' | 'released') {
  activeFilter.value = filter
  // Reload with correct activeOnly param
  if (filter === 'active') {
    loadData()
  } else {
    // Load all and filter client-side
    isLoading.value = true
    legalHoldsApi.getAll(false).then(res => {
      holds.value = res.data
    }).catch(() => {}).finally(() => {
      isLoading.value = false
    })
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString()
}

function getStatusClass(status: string): string {
  if (status === 'Active') return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'
  if (status === 'Released') return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
  return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400'
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-7xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Legal Holds" icon="gavel" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Legal Holds</h1>
          <p class="text-gray-500 dark:text-gray-400 mt-1">Case-based document preservation & litigation holds</p>
        </div>
        <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors">
          <span class="material-symbols-outlined text-lg">add</span>
          Create Legal Hold
        </button>
      </div>

      <!-- Stat Cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        <div class="bg-white dark:bg-surface-dark p-4 rounded-lg border border-gray-200 dark:border-border-dark">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Total Holds</p>
          <p class="text-2xl font-bold text-gray-900 dark:text-white mt-1">{{ stats.total }}</p>
        </div>
        <div class="bg-white dark:bg-surface-dark p-4 rounded-lg border border-amber-200 dark:border-amber-800/50">
          <p class="text-xs font-medium text-amber-600 dark:text-amber-400 uppercase tracking-wider">Active</p>
          <p class="text-2xl font-bold text-amber-700 dark:text-amber-400 mt-1">{{ stats.active }}</p>
        </div>
        <div class="bg-white dark:bg-surface-dark p-4 rounded-lg border border-green-200 dark:border-green-800/50">
          <p class="text-xs font-medium text-green-600 dark:text-green-400 uppercase tracking-wider">Released</p>
          <p class="text-2xl font-bold text-green-700 dark:text-green-400 mt-1">{{ stats.released }}</p>
        </div>
        <div class="bg-white dark:bg-surface-dark p-4 rounded-lg border border-blue-200 dark:border-blue-800/50">
          <p class="text-xs font-medium text-blue-600 dark:text-blue-400 uppercase tracking-wider">Documents Held</p>
          <p class="text-2xl font-bold text-blue-700 dark:text-blue-400 mt-1">{{ stats.documentsHeld }}</p>
        </div>
      </div>

      <!-- Filter Tabs -->
      <div class="border-b border-gray-200 dark:border-border-dark mb-6">
        <nav class="flex gap-6">
          <button
            @click="changeFilter('active')"
            :class="activeFilter === 'active' ? 'border-teal text-teal' : 'border-transparent text-gray-500 hover:text-gray-700 dark:hover:text-gray-300'"
            class="py-3 border-b-2 font-medium text-sm transition-colors flex items-center gap-2"
          >
            Active
            <span v-if="stats.active > 0" class="px-1.5 py-0.5 text-xs bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400 rounded-full">
              {{ stats.active }}
            </span>
          </button>
          <button
            @click="changeFilter('released')"
            :class="activeFilter === 'released' ? 'border-teal text-teal' : 'border-transparent text-gray-500 hover:text-gray-700 dark:hover:text-gray-300'"
            class="py-3 border-b-2 font-medium text-sm transition-colors"
          >
            Released
          </button>
          <button
            @click="changeFilter('all')"
            :class="activeFilter === 'all' ? 'border-teal text-teal' : 'border-transparent text-gray-500 hover:text-gray-700 dark:hover:text-gray-300'"
            class="py-3 border-b-2 font-medium text-sm transition-colors"
          >
            All
          </button>
        </nav>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="text-center py-12 text-gray-500 dark:text-gray-400">Loading...</div>

      <!-- Content: Table + Detail Panel -->
      <div v-else class="flex gap-6">
        <!-- Table -->
        <div :class="selectedHold ? 'w-1/2' : 'w-full'" class="transition-all">
          <div class="bg-white dark:bg-surface-dark rounded-lg shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden">
            <table class="w-full">
              <thead class="bg-gray-50 dark:bg-background-dark">
                <tr>
                  <th class="text-left py-3 px-4 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Hold</th>
                  <th class="text-left py-3 px-4 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Case Ref</th>
                  <th class="text-left py-3 px-4 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Status</th>
                  <th class="text-left py-3 px-4 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Dates</th>
                  <th class="text-center py-3 px-4 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Docs</th>
                  <th class="text-right py-3 px-4 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr v-if="filteredHolds.length === 0">
                  <td colspan="6" class="py-12 text-center text-gray-500 dark:text-gray-400">
                    <span class="material-symbols-outlined text-4xl text-gray-300 dark:text-gray-600 block mb-2">gavel</span>
                    No legal holds found
                  </td>
                </tr>
                <tr
                  v-for="hold in filteredHolds"
                  :key="hold.id"
                  @click="selectHold(hold)"
                  :class="selectedHold?.id === hold.id ? 'bg-teal/5 dark:bg-teal/10' : 'hover:bg-gray-50 dark:hover:bg-background-dark'"
                  class="border-t border-gray-100 dark:border-border-dark cursor-pointer transition-colors"
                >
                  <td class="py-3 px-4">
                    <p class="font-medium text-gray-900 dark:text-white text-sm">{{ hold.name }}</p>
                    <p class="text-xs text-gray-400">{{ hold.holdNumber }}</p>
                  </td>
                  <td class="py-3 px-4 text-sm text-gray-600 dark:text-gray-300">
                    {{ hold.caseReference || '—' }}
                  </td>
                  <td class="py-3 px-4">
                    <span :class="getStatusClass(hold.status)" class="px-2 py-1 text-xs rounded-full font-medium">
                      {{ hold.status }}
                    </span>
                  </td>
                  <td class="py-3 px-4 text-xs text-gray-500 dark:text-gray-400">
                    <div>{{ formatDate(hold.effectiveFrom) }}</div>
                    <div v-if="hold.effectiveUntil" class="text-gray-400">to {{ formatDate(hold.effectiveUntil) }}</div>
                  </td>
                  <td class="py-3 px-4 text-center">
                    <span class="inline-flex items-center justify-center w-7 h-7 rounded-full bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-xs font-bold">
                      {{ hold.documentCount }}
                    </span>
                  </td>
                  <td class="py-3 px-4 text-right" @click.stop>
                    <button
                      v-if="hold.status === 'Active'"
                      @click="openEditModal(hold)"
                      class="p-1.5 text-gray-400 hover:text-teal transition-colors mr-1"
                      title="Edit"
                    >
                      <span class="material-symbols-outlined text-lg">edit</span>
                    </button>
                    <button
                      v-if="hold.status === 'Active'"
                      @click="openReleaseModal(hold)"
                      class="p-1.5 text-gray-400 hover:text-amber-600 transition-colors"
                      title="Release Hold"
                    >
                      <span class="material-symbols-outlined text-lg">lock_open</span>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Detail Panel -->
        <div v-if="selectedHold" class="w-1/2">
          <div class="bg-white dark:bg-surface-dark rounded-lg shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden sticky top-6">
            <!-- Detail Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative">
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <span class="material-symbols-outlined text-2xl">gavel</span>
                    <div>
                      <h3 class="font-bold text-lg">{{ selectedHold.name }}</h3>
                      <p class="text-sm text-white/70">{{ selectedHold.holdNumber }}</p>
                    </div>
                  </div>
                  <button @click="closeDetail" class="p-1 hover:bg-white/20 rounded transition-colors">
                    <span class="material-symbols-outlined">close</span>
                  </button>
                </div>
              </div>
            </div>

            <!-- Detail Body -->
            <div class="p-5 space-y-4 max-h-[calc(100vh-280px)] overflow-y-auto">
              <!-- Status & Dates -->
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Status</p>
                  <span :class="getStatusClass(selectedHold.status)" class="inline-block px-2 py-1 text-xs rounded-full font-medium mt-1">
                    {{ selectedHold.status }}
                  </span>
                </div>
                <div>
                  <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Case Reference</p>
                  <p class="text-sm text-gray-900 dark:text-white mt-1">{{ selectedHold.caseReference || '—' }}</p>
                </div>
                <div>
                  <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Effective From</p>
                  <p class="text-sm text-gray-900 dark:text-white mt-1">{{ formatDate(selectedHold.effectiveFrom) }}</p>
                </div>
                <div>
                  <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Effective Until</p>
                  <p class="text-sm text-gray-900 dark:text-white mt-1">{{ selectedHold.effectiveUntil ? formatDate(selectedHold.effectiveUntil) : 'Indefinite' }}</p>
                </div>
                <div>
                  <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Requested By</p>
                  <p class="text-sm text-gray-900 dark:text-white mt-1">{{ selectedHold.requestedBy || '—' }}</p>
                </div>
                <div>
                  <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Applied By</p>
                  <p class="text-sm text-gray-900 dark:text-white mt-1">{{ selectedHold.appliedByName || '—' }}</p>
                </div>
              </div>

              <!-- Description -->
              <div v-if="selectedHold.description">
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase mb-1">Description</p>
                <p class="text-sm text-gray-700 dark:text-gray-300">{{ selectedHold.description }}</p>
              </div>

              <!-- Notes -->
              <div v-if="selectedHold.notes">
                <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase mb-1">Notes</p>
                <p class="text-sm text-gray-700 dark:text-gray-300">{{ selectedHold.notes }}</p>
              </div>

              <!-- Release Info -->
              <div v-if="selectedHold.status === 'Released'" class="bg-green-50 dark:bg-green-900/20 rounded-lg p-3 border border-green-200 dark:border-green-800/50">
                <p class="text-xs font-medium text-green-700 dark:text-green-400 uppercase mb-1">Released</p>
                <p class="text-sm text-green-800 dark:text-green-300">
                  {{ selectedHold.releasedByName }} on {{ selectedHold.releasedAt ? formatDate(selectedHold.releasedAt) : '' }}
                </p>
                <p v-if="selectedHold.releaseReason" class="text-xs text-green-600 dark:text-green-400 mt-1">
                  Reason: {{ selectedHold.releaseReason }}
                </p>
              </div>

              <!-- Documents -->
              <div>
                <div class="flex items-center justify-between mb-2">
                  <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">
                    Documents ({{ holdDocuments.length }})
                  </p>
                </div>

                <div v-if="loadingDocuments" class="text-center py-4 text-gray-400 text-sm">Loading documents...</div>
                <div v-else-if="holdDocuments.length === 0" class="text-center py-4 text-gray-400 text-sm">
                  No documents under this hold
                </div>
                <div v-else class="space-y-1">
                  <div
                    v-for="doc in holdDocuments"
                    :key="doc.id"
                    class="flex items-center justify-between p-2 rounded-lg hover:bg-gray-50 dark:hover:bg-background-dark group"
                  >
                    <div class="flex items-center gap-2 min-w-0">
                      <span class="material-symbols-outlined text-gray-400 text-lg">description</span>
                      <div class="min-w-0">
                        <router-link
                          :to="`/documents/${doc.documentId}`"
                          class="text-sm font-medium text-gray-900 dark:text-white hover:text-teal truncate block"
                        >
                          {{ doc.documentName }}
                        </router-link>
                        <p class="text-xs text-gray-400">Added {{ formatDate(doc.addedAt) }}</p>
                      </div>
                    </div>
                    <button
                      v-if="selectedHold.status === 'Active'"
                      @click="removeDocumentFromHold(doc.documentId)"
                      class="p-1 text-gray-300 hover:text-red-500 opacity-0 group-hover:opacity-100 transition-all"
                      title="Remove from hold"
                    >
                      <span class="material-symbols-outlined text-lg">close</span>
                    </button>
                  </div>
                </div>
              </div>

              <!-- Actions -->
              <div v-if="selectedHold.status === 'Active'" class="flex gap-2 pt-2 border-t border-gray-100 dark:border-border-dark">
                <button
                  @click="openEditModal(selectedHold)"
                  class="flex-1 flex items-center justify-center gap-2 px-3 py-2 text-sm border border-gray-200 dark:border-border-dark rounded-lg hover:bg-gray-50 dark:hover:bg-background-dark text-gray-700 dark:text-gray-300 transition-colors"
                >
                  <span class="material-symbols-outlined text-lg">edit</span>
                  Edit
                </button>
                <button
                  @click="openReleaseModal(selectedHold)"
                  class="flex-1 flex items-center justify-center gap-2 px-3 py-2 text-sm bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors"
                >
                  <span class="material-symbols-outlined text-lg">lock_open</span>
                  Release Hold
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showModal" class="fixed inset-0 bg-black/50 z-40" @click="showModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-hidden flex flex-col">
            <!-- Modal Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <span class="material-symbols-outlined text-2xl">gavel</span>
                  <h3 class="text-lg font-bold">{{ isEditing ? 'Edit Legal Hold' : 'Create Legal Hold' }}</h3>
                </div>
                <button @click="showModal = false" class="p-1 hover:bg-white/20 rounded transition-colors">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-4 overflow-y-auto">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Hold Name *</label>
                <input
                  v-model="formData.name"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="e.g., Smith v. Corp Litigation Hold"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Description</label>
                <textarea
                  v-model="formData.description"
                  rows="2"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Describe the purpose and scope of this hold"
                ></textarea>
              </div>

              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Case Reference</label>
                  <input
                    v-model="formData.caseReference"
                    type="text"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                    placeholder="e.g., CASE-2026-001"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Requested By</label>
                  <input
                    v-model="formData.requestedBy"
                    type="text"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                    placeholder="e.g., Legal Department"
                  />
                </div>
              </div>

              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Effective From</label>
                  <input
                    v-model="formData.effectiveFrom"
                    type="date"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Effective Until</label>
                  <input
                    v-model="formData.effectiveUntil"
                    type="date"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  />
                  <p class="text-xs text-gray-400 mt-1">Leave blank for indefinite hold</p>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes</label>
                <textarea
                  v-model="formData.notes"
                  rows="2"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Additional notes or instructions"
                ></textarea>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="px-6 py-4 border-t border-gray-200 dark:border-border-dark flex justify-end gap-3">
              <button
                @click="showModal = false"
                class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="handleSave"
                :disabled="!formData.name.trim()"
                class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {{ isEditing ? 'Save Changes' : 'Create Hold' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Release Modal -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showReleaseModal" class="fixed inset-0 bg-black/50 z-40" @click="showReleaseModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showReleaseModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showReleaseModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Release Header -->
            <div class="bg-gradient-to-r from-amber-600 to-amber-500 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="relative flex items-center gap-3">
                <span class="material-symbols-outlined text-2xl">lock_open</span>
                <h3 class="text-lg font-bold">Release Legal Hold</h3>
              </div>
            </div>

            <div class="p-6">
              <div class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/50 rounded-lg p-3 mb-4">
                <div class="flex items-start gap-2">
                  <span class="material-symbols-outlined text-amber-600 dark:text-amber-400 text-lg mt-0.5">warning</span>
                  <div>
                    <p class="text-sm font-medium text-amber-800 dark:text-amber-300">
                      Releasing "{{ releaseTarget?.name }}"
                    </p>
                    <p class="text-xs text-amber-600 dark:text-amber-400 mt-1">
                      This will remove preservation restrictions from {{ releaseTarget?.documentCount }} document(s). This action cannot be undone.
                    </p>
                  </div>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Reason for Release *</label>
                <textarea
                  v-model="releaseReason"
                  rows="3"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-amber-500/50 focus:border-amber-500"
                  placeholder="Provide the reason for releasing this legal hold..."
                ></textarea>
              </div>

              <div class="flex justify-end gap-3 mt-4">
                <button
                  @click="showReleaseModal = false"
                  class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="confirmRelease"
                  :disabled="!releaseReason.trim() || isReleasing"
                  class="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  <span v-if="isReleasing" class="material-symbols-outlined text-lg animate-spin">progress_activity</span>
                  Release Hold
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.backdrop-enter-active,
.backdrop-leave-active {
  transition: opacity 0.2s ease;
}
.backdrop-enter-from,
.backdrop-leave-to {
  opacity: 0;
}
.modal-enter-active,
.modal-leave-active {
  transition: all 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
  transform: scale(0.95);
}
</style>
