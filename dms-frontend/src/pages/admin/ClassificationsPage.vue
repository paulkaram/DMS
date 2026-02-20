<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { referenceDataApi, retentionPoliciesApi, privacyLevelsApi } from '@/api/client'
import type { Classification } from '@/types'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'
import UiSelect from '@/components/ui/Select.vue'
import ClassificationNode from '@/components/admin/ClassificationNode.vue'

const classifications = ref<Classification[]>([])
const flatClassifications = ref<Classification[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const errorMessage = ref('')
const expandedNodes = ref<Set<string>>(new Set())
const searchQuery = ref('')

// Modal state
const showModal = ref(false)
const isEditing = ref(false)
const showDeleteConfirm = ref(false)
const deleteTarget = ref<{ id: string; name: string } | null>(null)

const formData = ref({
  id: '',
  name: '',
  description: '',
  code: '',
  parentId: '' as string | undefined,
  confidentialityLevel: '',
  defaultRetentionPolicyId: '' as string | undefined,
  defaultPrivacyLevelId: '' as string | undefined,
  requiresDisposalApproval: true,
  sortOrder: 0,
  language: '',
  isActive: true
})

// Governance lookup data
const retentionPolicies = ref<{ value: string; label: string }[]>([])
const privacyLevels = ref<{ value: string; label: string }[]>([])

const confidentialityOptions = [
  { value: '', label: '-- None --' },
  { value: 'Public', label: 'Public' },
  { value: 'Internal', label: 'Internal' },
  { value: 'Confidential', label: 'Confidential' },
  { value: 'Secret', label: 'Secret' },
  { value: 'TopSecret', label: 'Top Secret' }
]

const parentOptions = computed(() => {
  const opts: { value: string; label: string }[] = [{ value: '', label: '-- Root Level --' }]
  function walk(nodes: Classification[], prefix: string) {
    for (const n of nodes) {
      // Don't allow setting self or descendants as parent
      if (isEditing.value && n.id === formData.value.id) continue
      const label = prefix ? `${prefix} > ${n.name}` : n.name
      opts.push({ value: n.id, label })
      if (n.children?.length) walk(n.children, label)
    }
  }
  walk(classifications.value, '')
  return opts
})

const filteredTree = computed(() => {
  if (!searchQuery.value.trim()) return classifications.value
  const q = searchQuery.value.toLowerCase()
  function filterNodes(nodes: Classification[]): Classification[] {
    const result: Classification[] = []
    for (const node of nodes) {
      const matchesSelf = node.name.toLowerCase().includes(q) ||
        node.code?.toLowerCase().includes(q) ||
        node.description?.toLowerCase().includes(q)
      const filteredChildren = node.children?.length ? filterNodes(node.children) : []
      if (matchesSelf || filteredChildren.length > 0) {
        result.push({ ...node, children: filteredChildren.length > 0 ? filteredChildren : node.children })
      }
    }
    return result
  }
  return filterNodes(classifications.value)
})

onMounted(async () => {
  await Promise.all([loadClassifications(), loadGovernanceLookups()])
})

async function loadGovernanceLookups() {
  try {
    const [rpRes, plRes] = await Promise.all([
      retentionPoliciesApi.getAll(),
      privacyLevelsApi.getAll()
    ])
    const rpData = Array.isArray(rpRes.data) ? rpRes.data : (rpRes.data?.data ?? [])
    const plData = Array.isArray(plRes.data) ? plRes.data : (plRes.data?.data ?? [])
    retentionPolicies.value = rpData.map((r: any) => ({ value: r.id, label: r.name }))
    privacyLevels.value = plData.map((p: any) => ({ value: p.id, label: p.name }))
  } catch (err) {
    console.error('Failed to load governance lookups:', err)
  }
}

async function loadClassifications() {
  isLoading.value = true
  try {
    const [treeRes, flatRes] = await Promise.all([
      referenceDataApi.getClassificationTree(),
      referenceDataApi.getClassifications()
    ])
    const treeData = Array.isArray(treeRes.data) ? treeRes.data : (treeRes.data?.data ?? [])
    const flatData = Array.isArray(flatRes.data) ? flatRes.data : (flatRes.data?.data ?? [])
    classifications.value = treeData
    flatClassifications.value = flatData
  } catch (err: any) {
    console.error('Failed to load classifications:', err)
  } finally {
    isLoading.value = false
  }
}

function toggleNode(id: string) {
  if (expandedNodes.value.has(id)) {
    expandedNodes.value.delete(id)
  } else {
    expandedNodes.value.add(id)
  }
}

function expandAll() {
  function walk(nodes: Classification[]) {
    for (const n of nodes) {
      if (n.children?.length) {
        expandedNodes.value.add(n.id)
        walk(n.children)
      }
    }
  }
  walk(classifications.value)
}

function collapseAll() {
  expandedNodes.value.clear()
}

function openCreateModal(parentId?: string) {
  formData.value = {
    id: '',
    name: '',
    description: '',
    code: '',
    parentId: parentId || undefined,
    confidentialityLevel: '',
    defaultRetentionPolicyId: undefined,
    defaultPrivacyLevelId: undefined,
    requiresDisposalApproval: true,
    sortOrder: 0,
    language: '',
    isActive: true
  }
  isEditing.value = false
  errorMessage.value = ''
  showModal.value = true
}

function openEditModal(c: Classification) {
  formData.value = {
    id: c.id,
    name: c.name,
    description: c.description || '',
    code: c.code || '',
    parentId: c.parentId || undefined,
    confidentialityLevel: c.confidentialityLevel || '',
    defaultRetentionPolicyId: c.defaultRetentionPolicyId || undefined,
    defaultPrivacyLevelId: c.defaultPrivacyLevelId || undefined,
    requiresDisposalApproval: c.requiresDisposalApproval,
    sortOrder: c.sortOrder,
    language: c.language || '',
    isActive: c.isActive
  }
  isEditing.value = true
  errorMessage.value = ''
  showModal.value = true
}

async function saveClassification() {
  if (!formData.value.name.trim()) return
  isSaving.value = true
  errorMessage.value = ''
  try {
    const payload = {
      name: formData.value.name,
      description: formData.value.description || undefined,
      code: formData.value.code || undefined,
      parentId: formData.value.parentId || undefined,
      confidentialityLevel: formData.value.confidentialityLevel || undefined,
      defaultRetentionPolicyId: formData.value.defaultRetentionPolicyId || undefined,
      defaultPrivacyLevelId: formData.value.defaultPrivacyLevelId || undefined,
      requiresDisposalApproval: formData.value.requiresDisposalApproval,
      sortOrder: formData.value.sortOrder,
      language: formData.value.language || undefined
    }
    if (isEditing.value) {
      await referenceDataApi.updateClassification(formData.value.id, {
        ...payload,
        isActive: formData.value.isActive
      })
    } else {
      await referenceDataApi.createClassification(payload)
    }
    showModal.value = false
    await loadClassifications()
  } catch (err: any) {
    const msg = err.response?.data?.message || err.response?.data || err.message || 'An error occurred while saving.'
    errorMessage.value = typeof msg === 'string' ? msg : 'An error occurred while saving.'
  } finally {
    isSaving.value = false
  }
}

function confirmDelete(c: Classification) {
  deleteTarget.value = { id: c.id, name: c.name }
  showDeleteConfirm.value = true
}

async function deleteClassification() {
  if (!deleteTarget.value) return
  try {
    await referenceDataApi.deleteClassification(deleteTarget.value.id)
    showDeleteConfirm.value = false
    deleteTarget.value = null
    await loadClassifications()
  } catch {
  }
}
</script>

<template>
  <div class="min-h-[calc(100vh-7rem)] p-4 bg-gray-100 dark:bg-zinc-950">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Classifications" icon="category" />

      <!-- Header Card -->
      <div class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-gray-200 dark:border-border-dark mt-3">
        <div class="px-5 py-4 border-b border-gray-100 dark:border-border-dark flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-lg bg-gradient-to-br from-teal to-teal/80 flex items-center justify-center shadow-sm">
              <span class="material-symbols-outlined text-white text-xl">category</span>
            </div>
            <div>
              <h1 class="text-lg font-semibold text-gray-900 dark:text-white">Classifications</h1>
              <p class="text-xs text-gray-500">Manage document classification hierarchy and governance defaults</p>
            </div>
          </div>
          <div class="flex items-center gap-2">
            <!-- Search -->
            <div class="relative">
              <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-lg">search</span>
              <input
                v-model="searchQuery"
                type="text"
                placeholder="Search..."
                class="pl-9 pr-3 py-2 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all w-48"
              />
            </div>
            <button
              @click="expandAll"
              class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-surface-dark rounded-lg transition-colors"
              title="Expand all"
            >
              <span class="material-symbols-outlined text-lg">unfold_more</span>
            </button>
            <button
              @click="collapseAll"
              class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-surface-dark rounded-lg transition-colors"
              title="Collapse all"
            >
              <span class="material-symbols-outlined text-lg">unfold_less</span>
            </button>
            <div class="w-px h-6 bg-gray-200 dark:bg-border-dark mx-1"></div>
            <button
              @click="openCreateModal()"
              class="flex items-center gap-1.5 px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors"
            >
              <span class="material-symbols-outlined text-lg">add</span>
              New Classification
            </button>
          </div>
        </div>

        <!-- Loading -->
        <div v-if="isLoading" class="flex items-center justify-center py-20">
          <span class="material-symbols-outlined animate-spin text-4xl text-teal">progress_activity</span>
        </div>

        <!-- Empty State -->
        <div v-else-if="classifications.length === 0" class="text-center py-20">
          <div class="w-16 h-16 mx-auto mb-4 rounded-full bg-teal/10 dark:bg-teal/10 flex items-center justify-center">
            <span class="material-symbols-outlined text-3xl text-teal">category</span>
          </div>
          <h3 class="text-lg font-medium text-gray-600 dark:text-gray-400 mb-1">No Classifications</h3>
          <p class="text-sm text-gray-400 mb-4">Create your first classification to organize documents</p>
          <button
            @click="openCreateModal()"
            class="inline-flex items-center gap-1.5 px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors"
          >
            <span class="material-symbols-outlined text-lg">add</span>
            Create Classification
          </button>
        </div>

        <!-- Tree View -->
        <div v-else class="p-4">
          <!-- Column headers -->
          <div class="flex items-center gap-4 px-4 py-2 text-xs font-semibold text-gray-400 uppercase tracking-wide border-b border-gray-100 dark:border-border-dark mb-2">
            <div class="flex-1">Name</div>
            <div class="w-24 text-center">Code</div>
            <div class="w-28 text-center">Confidentiality</div>
            <div class="w-20 text-center">Disposal</div>
            <div class="w-16 text-center">Status</div>
            <div class="w-28 text-right">Actions</div>
          </div>

          <!-- Recursive tree -->
          <div class="space-y-0.5">
            <ClassificationNode
              v-for="node in filteredTree"
              :key="node.id"
              :node="node"
              :depth="0"
              :expanded-nodes="expandedNodes"
              @toggle="toggleNode"
              @edit="openEditModal"
              @delete="confirmDelete"
              @add-child="openCreateModal"
            />
          </div>

          <div class="mt-3 px-4 py-2 text-xs text-gray-400">
            {{ flatClassifications.length }} classification{{ flatClassifications.length !== 1 ? 's' : '' }} total
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showModal" class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" @click.self="showModal = false">
          <Transition name="scale">
            <div v-if="showModal" class="bg-white dark:bg-background-dark rounded-xl shadow-2xl w-full max-w-lg max-h-[85vh] overflow-hidden flex flex-col">
              <!-- Modal Header -->
              <div class="bg-gradient-to-r from-navy to-primary px-6 py-4 relative overflow-hidden">
                <div class="absolute -right-4 -top-4 w-20 h-20 bg-white/5 rounded-full"></div>
                <div class="absolute -right-8 -bottom-8 w-28 h-28 bg-white/5 rounded-full"></div>
                <div class="relative flex items-center justify-between">
                  <div class="flex items-center gap-3">
                    <span class="material-symbols-outlined text-white/90 text-2xl">{{ isEditing ? 'edit' : 'add_circle' }}</span>
                    <h3 class="text-lg font-semibold text-white">{{ isEditing ? 'Edit Classification' : 'New Classification' }}</h3>
                  </div>
                  <button @click="showModal = false" class="p-1.5 text-white/70 hover:text-white hover:bg-white/10 rounded-lg transition-colors">
                    <span class="material-symbols-outlined text-xl">close</span>
                  </button>
                </div>
              </div>

              <!-- Modal Body -->
              <div class="flex-1 overflow-y-auto p-6 space-y-4">
                <!-- Error Message -->
                <div v-if="errorMessage" class="flex items-center gap-2 px-4 py-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-lg">
                  <span class="material-symbols-outlined text-red-500 text-lg flex-shrink-0">error</span>
                  <p class="text-sm text-red-600 dark:text-red-400">{{ errorMessage }}</p>
                  <button @click="errorMessage = ''" class="ml-auto p-0.5 text-red-400 hover:text-red-600 transition-colors flex-shrink-0">
                    <span class="material-symbols-outlined text-base">close</span>
                  </button>
                </div>

                <!-- Name & Code -->
                <div class="grid grid-cols-3 gap-4">
                  <div class="col-span-2">
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Name *</label>
                    <input
                      v-model="formData.name"
                      type="text"
                      class="w-full px-4 py-2.5 text-sm border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
                      placeholder="Classification name"
                    />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Code</label>
                    <input
                      v-model="formData.code"
                      type="text"
                      class="w-full px-4 py-2.5 text-sm border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
                      placeholder="e.g., FIN-001"
                    />
                  </div>
                </div>

                <!-- Description -->
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Description</label>
                  <textarea
                    v-model="formData.description"
                    rows="2"
                    class="w-full px-4 py-2.5 text-sm border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all resize-none"
                    placeholder="Optional description"
                  ></textarea>
                </div>

                <!-- Parent -->
                <div>
                  <UiSelect
                    v-model="formData.parentId"
                    :options="parentOptions"
                    label="Parent Classification"
                    placeholder="Root Level"
                    searchable
                    clearable
                  />
                </div>

                <!-- Governance Section -->
                <div class="border-t border-gray-100 dark:border-border-dark pt-4">
                  <h4 class="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">Governance Defaults</h4>

                  <div class="grid grid-cols-2 gap-4">
                    <!-- Confidentiality Level -->
                    <div>
                      <UiSelect
                        v-model="formData.confidentialityLevel"
                        :options="confidentialityOptions"
                        label="Confidentiality Level"
                        placeholder="None"
                        clearable
                      />
                    </div>

                    <!-- Sort Order -->
                    <div>
                      <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Sort Order</label>
                      <input
                        v-model.number="formData.sortOrder"
                        type="number"
                        min="0"
                        class="w-full px-4 py-2.5 text-sm border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
                      />
                    </div>
                  </div>

                  <div class="grid grid-cols-2 gap-4 mt-4">
                    <!-- Default Retention Policy -->
                    <div>
                      <UiSelect
                        v-model="formData.defaultRetentionPolicyId"
                        :options="retentionPolicies"
                        label="Default Retention Policy"
                        placeholder="None"
                        searchable
                        clearable
                      />
                    </div>

                    <!-- Default Privacy Level -->
                    <div>
                      <UiSelect
                        v-model="formData.defaultPrivacyLevelId"
                        :options="privacyLevels"
                        label="Default Privacy Level"
                        placeholder="None"
                        searchable
                        clearable
                      />
                    </div>
                  </div>

                  <!-- Requires Disposal Approval -->
                  <div class="mt-3">
                    <label class="flex items-center gap-3 cursor-pointer group">
                      <button
                        type="button"
                        @click="formData.requiresDisposalApproval = !formData.requiresDisposalApproval"
                        class="relative w-9 h-5 rounded-full transition-colors duration-200"
                        :class="formData.requiresDisposalApproval ? 'bg-teal' : 'bg-gray-300 dark:bg-zinc-600'"
                      >
                        <span
                          class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform duration-200"
                          :class="formData.requiresDisposalApproval ? 'translate-x-4' : 'translate-x-0'"
                        ></span>
                      </button>
                      <span class="text-sm text-gray-700 dark:text-gray-300 group-hover:text-gray-900 dark:group-hover:text-white transition-colors">Requires Disposal Approval</span>
                    </label>
                  </div>

                  <!-- Active toggle (edit only) -->
                  <div v-if="isEditing" class="mt-3">
                    <label class="flex items-center gap-3 cursor-pointer group">
                      <button
                        type="button"
                        @click="formData.isActive = !formData.isActive"
                        class="relative w-9 h-5 rounded-full transition-colors duration-200"
                        :class="formData.isActive ? 'bg-teal' : 'bg-gray-300 dark:bg-zinc-600'"
                      >
                        <span
                          class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform duration-200"
                          :class="formData.isActive ? 'translate-x-4' : 'translate-x-0'"
                        ></span>
                      </button>
                      <span class="text-sm text-gray-700 dark:text-gray-300 group-hover:text-gray-900 dark:group-hover:text-white transition-colors">Active</span>
                    </label>
                  </div>
                </div>
              </div>

              <!-- Modal Footer -->
              <div class="flex justify-end gap-3 px-6 py-4 border-t border-gray-100 dark:border-border-dark bg-gray-50 dark:bg-surface-dark/50">
                <button @click="showModal = false" class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 border border-gray-200 dark:border-border-dark rounded-lg hover:bg-gray-100 dark:hover:bg-surface-dark transition-colors">
                  Cancel
                </button>
                <button
                  @click="saveClassification"
                  :disabled="isSaving || !formData.name.trim()"
                  class="px-4 py-2 text-sm font-medium bg-teal text-white rounded-lg hover:bg-teal/90 disabled:opacity-50 transition-colors"
                >
                  {{ isSaving ? 'Saving...' : isEditing ? 'Update' : 'Create' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- Delete Confirmation -->
    <Teleport to="body">
      <Transition name="fade">
        <div v-if="showDeleteConfirm" class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" @click.self="showDeleteConfirm = false">
          <Transition name="scale">
            <div v-if="showDeleteConfirm" class="bg-white dark:bg-background-dark rounded-xl shadow-2xl w-full max-w-sm overflow-hidden">
              <div class="p-6 text-center">
                <div class="w-14 h-14 mx-auto mb-4 rounded-full bg-red-50 dark:bg-red-900/20 flex items-center justify-center">
                  <span class="material-symbols-outlined text-3xl text-red-500">delete_forever</span>
                </div>
                <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-2">Delete Classification</h3>
                <p class="text-sm text-gray-500 mb-1">
                  Are you sure you want to delete <strong class="text-gray-700 dark:text-gray-200">{{ deleteTarget?.name }}</strong>?
                </p>
                <p class="text-xs text-gray-400">This action cannot be undone.</p>
              </div>
              <div class="flex border-t border-gray-100 dark:border-border-dark">
                <button @click="showDeleteConfirm = false" class="flex-1 py-3 text-sm font-medium text-gray-600 hover:bg-gray-50 dark:hover:bg-surface-dark transition-colors">
                  Cancel
                </button>
                <button @click="deleteClassification" class="flex-1 py-3 text-sm font-medium text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 border-l border-gray-100 dark:border-border-dark transition-colors">
                  Delete
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
.scale-enter-active,
.scale-leave-active {
  transition: all 0.2s ease;
}
.scale-enter-from,
.scale-leave-to {
  transform: scale(0.95);
  opacity: 0;
}
</style>
