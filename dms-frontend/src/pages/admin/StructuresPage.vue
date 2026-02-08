<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { structuresApi, usersApi } from '@/api/client'
import type { Structure, StructureTree, StructureMember, User } from '@/types'
import { UiToggle } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

// State
const structures = ref<Structure[]>([])
const structureTree = ref<StructureTree[]>([])
const users = ref<User[]>([])
const searchQuery = ref('')
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const selectedTypeFilter = ref<string>('All')
const expandedNodes = ref<Set<string>>(new Set())
const viewMode = ref<'list' | 'grid'>('list')

// Modal state
const showStructureModal = ref(false)
const showMembersModal = ref(false)
const showDeleteConfirm = ref(false)
const showAddMemberModal = ref(false)
const editingStructure = ref<Structure | null>(null)
const selectedStructure = ref<Structure | null>(null)
const structureMembers = ref<StructureMember[]>([])
const isLoadingMembers = ref(false)

// Form state
const structureForm = ref({
  code: '',
  name: '',
  nameAr: '',
  type: 'Department' as string,
  parentId: '' as string | undefined
})

const memberForm = ref({
  userId: '',
  position: '',
  isPrimary: false,
  startDate: new Date().toISOString().split('T')[0]
})

// Structure types
const structureTypes = ['Ministry', 'Department', 'Division', 'Section', 'Unit']

// Alternating colors for avatars
const avatarColors = [
  'bg-primary',
  'bg-navy',
  'bg-slate-600',
  'bg-primary/80',
  'bg-slate-700'
]

function getAvatarColor(index: number) {
  return avatarColors[index % avatarColors.length]
}

// Computed
const filteredStructures = computed(() => {
  let result = structures.value

  if (selectedTypeFilter.value !== 'All') {
    result = result.filter(s => s.type === selectedTypeFilter.value)
  }

  if (searchQuery.value) {
    const q = searchQuery.value.toLowerCase()
    result = result.filter(s =>
      s.name?.toLowerCase().includes(q) ||
      s.nameAr?.toLowerCase().includes(q) ||
      s.code?.toLowerCase().includes(q)
    )
  }

  return result
})

const rootStructures = computed(() => filteredStructures.value.filter(s => !s.parentId))

const modalTitle = computed(() => editingStructure.value ? 'Edit Structure' : 'Create New Structure')

const availableParents = computed(() => {
  const editing = editingStructure.value
  if (!editing) return structures.value
  return structures.value.filter(s => s.id !== editing.id)
})

const availableUsers = computed(() => {
  const memberIds = structureMembers.value.map(m => m.userId)
  return users.value.filter(u => !memberIds.includes(u.id))
})

// Load data
onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  error.value = null
  try {
    const [structuresRes, usersRes] = await Promise.all([
      structuresApi.getAll(true),
      usersApi.getAll()
    ])
    structures.value = structuresRes.data
    users.value = usersRes.data

    // Try to load tree
    try {
      const treeRes = await structuresApi.getTree()
      structureTree.value = treeRes.data
    } catch {
      // Tree endpoint might not be available
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to load structures'
  } finally {
    isLoading.value = false
  }
}

function showSuccess(message: string) {
  successMessage.value = message
  setTimeout(() => { successMessage.value = null }, 3000)
}

// Tree operations
function toggleNode(id: string) {
  if (expandedNodes.value.has(id)) {
    expandedNodes.value.delete(id)
  } else {
    expandedNodes.value.add(id)
  }
}

function getChildren(parentId: string) {
  return filteredStructures.value.filter(s => s.parentId === parentId)
}

function hasChildren(id: string) {
  return filteredStructures.value.some(s => s.parentId === id)
}

// CRUD operations
function openCreateModal(parentId?: string) {
  editingStructure.value = null
  structureForm.value = {
    code: '',
    name: '',
    nameAr: '',
    type: 'Department',
    parentId: parentId
  }
  showStructureModal.value = true
}

function openEditModal(structure: Structure) {
  editingStructure.value = structure
  structureForm.value = {
    code: structure.code,
    name: structure.name,
    nameAr: structure.nameAr || '',
    type: structure.type,
    parentId: structure.parentId
  }
  showStructureModal.value = true
}

async function saveStructure() {
  if (!structureForm.value.code.trim() || !structureForm.value.name.trim()) return

  isSaving.value = true
  error.value = null
  try {
    const data = {
      code: structureForm.value.code,
      name: structureForm.value.name,
      nameAr: structureForm.value.nameAr || undefined,
      type: structureForm.value.type,
      parentId: structureForm.value.parentId || undefined
    }

    if (editingStructure.value) {
      await structuresApi.update(editingStructure.value.id, { ...data, isActive: true })
      showSuccess('Structure updated successfully')
    } else {
      await structuresApi.create(data)
      showSuccess('Structure created successfully')
    }
    showStructureModal.value = false
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to save structure'
  } finally {
    isSaving.value = false
  }
}

function confirmDelete(structure: Structure) {
  selectedStructure.value = structure
  showDeleteConfirm.value = true
}

async function deleteStructure() {
  if (!selectedStructure.value) return

  isSaving.value = true
  try {
    await structuresApi.delete(selectedStructure.value.id)
    showSuccess('Structure deleted successfully')
    showDeleteConfirm.value = false
    selectedStructure.value = null
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to delete structure'
  } finally {
    isSaving.value = false
  }
}

// Members management
async function openMembersModal(structure: Structure) {
  selectedStructure.value = structure
  showMembersModal.value = true
  isLoadingMembers.value = true
  try {
    const res = await structuresApi.getMembers(structure.id)
    structureMembers.value = res.data
  } catch (err) {
    structureMembers.value = []
  } finally {
    isLoadingMembers.value = false
  }
}

function openAddMemberModal() {
  memberForm.value = {
    userId: '',
    position: '',
    isPrimary: false,
    startDate: new Date().toISOString().split('T')[0]
  }
  showAddMemberModal.value = true
}

async function addMember() {
  if (!selectedStructure.value || !memberForm.value.userId) return

  isSaving.value = true
  try {
    await structuresApi.addMember(selectedStructure.value.id, {
      userId: memberForm.value.userId,
      position: memberForm.value.position || undefined,
      isPrimary: memberForm.value.isPrimary,
      startDate: memberForm.value.startDate
    })
    showSuccess('Member added')
    showAddMemberModal.value = false
    // Reload members
    const res = await structuresApi.getMembers(selectedStructure.value.id)
    structureMembers.value = res.data
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to add member'
  } finally {
    isSaving.value = false
  }
}

async function removeMember(userId: string) {
  if (!selectedStructure.value) return
  try {
    await structuresApi.removeMember(selectedStructure.value.id, userId)
    structureMembers.value = structureMembers.value.filter(m => m.userId !== userId)
    showSuccess('Member removed')
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to remove member'
  }
}

function getInitials(name: string) {
  return name?.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() || '??'
}

function getTypeIcon(type: string) {
  switch (type) {
    case 'Ministry': return 'account_balance'
    case 'Department': return 'domain'
    case 'Division': return 'business'
    case 'Section': return 'meeting_room'
    case 'Unit': return 'groups'
    default: return 'apartment'
  }
}

function getTypeBadgeClass(type: string) {
  switch (type) {
    case 'Ministry': return 'bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400'
    case 'Department': return 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400'
    case 'Division': return 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
    case 'Section': return 'bg-teal-100 dark:bg-teal-900/30 text-teal-700 dark:text-teal-400'
    case 'Unit': return 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400'
    default: return 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-400'
  }
}

function getParentName(parentId: string | undefined) {
  if (!parentId) return null
  const parent = structures.value.find(s => s.id === parentId)
  return parent?.name || null
}
</script>

<template>
  <div class="space-y-6">
    <!-- Breadcrumb -->
    <AdminBreadcrumb current-page="Structures" icon="apartment" />

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-slate-100">Organizational Structures</h1>
        <p class="text-slate-500 mt-1">Manage departments, divisions, and units</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- View Toggle -->
        <div class="flex items-center bg-slate-100 dark:bg-slate-800 rounded-lg p-1">
          <button
            @click="viewMode = 'list'"
            :class="[
              'p-1.5 rounded-md transition-colors',
              viewMode === 'list'
                ? 'bg-white dark:bg-slate-700 text-primary shadow-sm'
                : 'text-slate-400 hover:text-slate-600'
            ]"
            title="List view"
          >
            <span class="material-symbols-outlined text-lg">view_list</span>
          </button>
          <button
            @click="viewMode = 'grid'"
            :class="[
              'p-1.5 rounded-md transition-colors',
              viewMode === 'grid'
                ? 'bg-white dark:bg-slate-700 text-primary shadow-sm'
                : 'text-slate-400 hover:text-slate-600'
            ]"
            title="Grid view"
          >
            <span class="material-symbols-outlined text-lg">grid_view</span>
          </button>
        </div>
        <!-- Search -->
        <div class="relative">
          <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-lg">search</span>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search..."
            class="w-48 pl-10 pr-4 py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white placeholder-slate-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
          />
        </div>
        <button
          @click="openCreateModal()"
          class="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-navy to-teal hover:shadow-lg hover:shadow-teal/25 text-white rounded-xl font-medium text-sm transition-all"
        >
          <span class="material-symbols-outlined text-lg">add</span>
          Create
        </button>
      </div>
    </div>

    <!-- Compact Stats Cards -->
    <div class="grid grid-cols-6 gap-3">
      <button
        @click="selectedTypeFilter = 'All'"
        :class="[
          'px-4 py-3 rounded-xl flex items-center justify-between transition-all',
          selectedTypeFilter === 'All'
            ? 'bg-navy text-white ring-2 ring-primary/50'
            : 'bg-slate-800 text-white hover:bg-slate-700'
        ]"
      >
        <div class="flex items-center gap-2">
          <span class="material-symbols-outlined text-primary text-lg">apartment</span>
          <span class="text-xs font-medium uppercase tracking-wide">All</span>
        </div>
        <span class="text-lg font-bold">{{ structures.length }}</span>
      </button>
      <button
        v-for="type in structureTypes"
        :key="type"
        @click="selectedTypeFilter = type"
        :class="[
          'px-4 py-3 rounded-xl flex items-center justify-between transition-all',
          selectedTypeFilter === type
            ? 'bg-navy text-white ring-2 ring-primary/50'
            : 'bg-slate-700 text-white hover:bg-slate-600'
        ]"
      >
        <div class="flex items-center gap-2">
          <span class="material-symbols-outlined text-primary text-lg">{{ getTypeIcon(type) }}</span>
          <span class="text-xs font-medium uppercase tracking-wide hidden xl:inline">{{ type }}</span>
        </div>
        <span class="text-lg font-bold">{{ structures.filter(s => s.type === type).length }}</span>
      </button>
    </div>

    <!-- Content Card -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
      <div class="p-6">
      <!-- Messages -->
      <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
        <div v-if="error" class="mb-6 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-2xl flex items-center gap-3">
          <span class="material-symbols-outlined text-red-500">error</span>
          <p class="text-red-600 dark:text-red-400 flex-1">{{ error }}</p>
          <button @click="error = null" class="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 rounded-lg">
            <span class="material-symbols-outlined text-red-400">close</span>
          </button>
        </div>
      </Transition>

      <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
        <div v-if="successMessage" class="mb-6 p-4 bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800/50 rounded-2xl flex items-center gap-3">
          <span class="material-symbols-outlined text-emerald-500">check_circle</span>
          <p class="text-emerald-600 dark:text-emerald-400">{{ successMessage }}</p>
        </div>
      </Transition>

      <!-- Loading -->
      <div v-if="isLoading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse"></div>
      </div>

      <!-- Empty State -->
      <div v-else-if="filteredStructures.length === 0" class="text-center py-16">
        <div class="w-20 h-20 rounded-2xl bg-slate-100 dark:bg-slate-800 flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-5xl text-slate-400">domain_disabled</span>
        </div>
        <h3 class="text-lg font-semibold text-slate-700 dark:text-slate-300">No Structures Found</h3>
        <p class="text-slate-500 dark:text-slate-400 mt-1">{{ searchQuery ? 'Try a different search term' : 'Create your first organizational structure' }}</p>
        <button
          v-if="!searchQuery"
          @click="openCreateModal()"
          class="mt-4 px-5 py-2.5 bg-gradient-to-r from-navy to-teal text-white rounded-xl font-medium hover:shadow-lg hover:shadow-teal/25 transition-all"
        >
          Create Structure
        </button>
      </div>

      <!-- List View -->
      <div v-else-if="viewMode === 'list'" class="overflow-x-auto">
        <table class="w-full">
          <thead>
            <tr class="border-b border-slate-100 dark:border-slate-800">
              <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Structure</th>
              <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Code</th>
              <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Type</th>
              <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Parent</th>
              <th class="text-center py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Members</th>
              <th class="text-center py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Status</th>
              <th class="text-right py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(structure, index) in filteredStructures"
              :key="structure.id"
              class="border-b border-slate-50 dark:border-slate-800/50 hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors"
            >
              <td class="py-3 px-4">
                <div class="flex items-center gap-3">
                  <div :class="[getAvatarColor(index), 'w-9 h-9 rounded-lg flex items-center justify-center']">
                    <span class="material-symbols-outlined text-white text-lg">{{ getTypeIcon(structure.type) }}</span>
                  </div>
                  <div>
                    <span class="font-medium text-slate-900 dark:text-white">{{ structure.name }}</span>
                    <p v-if="structure.nameAr" class="text-xs text-slate-400">{{ structure.nameAr }}</p>
                  </div>
                </div>
              </td>
              <td class="py-3 px-4">
                <code class="px-2 py-1 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 text-xs rounded">{{ structure.code }}</code>
              </td>
              <td class="py-3 px-4">
                <span :class="['px-2 py-1 rounded-full text-xs font-medium', getTypeBadgeClass(structure.type)]">
                  {{ structure.type }}
                </span>
              </td>
              <td class="py-3 px-4">
                <span class="text-sm text-slate-500 dark:text-slate-400">{{ getParentName(structure.parentId) || '-' }}</span>
              </td>
              <td class="py-3 px-4 text-center">
                <button
                  @click="openMembersModal(structure)"
                  class="inline-flex items-center gap-1 px-2 py-1 text-sm text-primary hover:bg-primary/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-sm">people</span>
                  {{ structure.memberCount || 0 }}
                </button>
              </td>
              <td class="py-3 px-4 text-center">
                <span
                  :class="[
                    'px-2 py-1 rounded-full text-xs font-medium',
                    structure.isActive
                      ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                      : 'bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400'
                  ]"
                >
                  {{ structure.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4">
                <div class="flex items-center justify-end gap-1">
                  <button
                    @click="openCreateModal(structure.id)"
                    class="p-1.5 text-slate-400 hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                    title="Add Child"
                  >
                    <span class="material-symbols-outlined text-lg">add</span>
                  </button>
                  <button
                    @click="openEditModal(structure)"
                    class="p-1.5 text-slate-400 hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                    title="Edit"
                  >
                    <span class="material-symbols-outlined text-lg">edit</span>
                  </button>
                  <button
                    @click="confirmDelete(structure)"
                    class="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                    title="Delete"
                  >
                    <span class="material-symbols-outlined text-lg">delete</span>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Grid View -->
      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        <div
          v-for="(structure, index) in filteredStructures"
          :key="structure.id"
          class="bg-slate-50 dark:bg-slate-800 rounded-xl p-4 border border-slate-100 dark:border-slate-700 hover:border-primary/30 hover:shadow-md transition-all group"
        >
          <div class="flex items-start justify-between mb-3">
            <div :class="[getAvatarColor(index), 'w-10 h-10 rounded-lg flex items-center justify-center']">
              <span class="material-symbols-outlined text-white">{{ getTypeIcon(structure.type) }}</span>
            </div>
            <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <button
                @click="openCreateModal(structure.id)"
                class="p-1 text-slate-400 hover:text-primary hover:bg-white dark:hover:bg-slate-700 rounded transition-colors"
                title="Add Child"
              >
                <span class="material-symbols-outlined text-sm">add</span>
              </button>
              <button
                @click="openEditModal(structure)"
                class="p-1 text-slate-400 hover:text-primary hover:bg-white dark:hover:bg-slate-700 rounded transition-colors"
                title="Edit"
              >
                <span class="material-symbols-outlined text-sm">edit</span>
              </button>
              <button
                @click="confirmDelete(structure)"
                class="p-1 text-slate-400 hover:text-red-500 hover:bg-white dark:hover:bg-slate-700 rounded transition-colors"
                title="Delete"
              >
                <span class="material-symbols-outlined text-sm">delete</span>
              </button>
            </div>
          </div>

          <h3 class="font-semibold text-slate-900 dark:text-white text-sm mb-1">{{ structure.name }}</h3>
          <p v-if="structure.nameAr" class="text-xs text-slate-400 mb-1">{{ structure.nameAr }}</p>
          <code class="px-2 py-0.5 bg-slate-200 dark:bg-slate-700 text-slate-600 dark:text-slate-300 text-xs rounded">{{ structure.code }}</code>

          <div class="flex items-center gap-2 mt-3 mb-3">
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', getTypeBadgeClass(structure.type)]">
              {{ structure.type }}
            </span>
            <span
              :class="[
                'px-2 py-0.5 rounded-full text-xs font-medium',
                structure.isActive
                  ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                  : 'bg-slate-200 dark:bg-slate-600 text-slate-600 dark:text-slate-400'
              ]"
            >
              {{ structure.isActive ? 'Active' : 'Inactive' }}
            </span>
          </div>

          <div class="flex items-center justify-between pt-3 border-t border-slate-200 dark:border-slate-700">
            <div class="flex items-center gap-3 text-xs text-slate-500">
              <span class="flex items-center gap-1">
                <span class="material-symbols-outlined text-xs">people</span>
                {{ structure.memberCount || 0 }}
              </span>
              <span class="flex items-center gap-1">
                <span class="material-symbols-outlined text-xs">subdirectory_arrow_right</span>
                {{ structure.childCount || 0 }}
              </span>
            </div>
            <button
              @click="openMembersModal(structure)"
              class="text-xs text-primary hover:text-primary/80 font-medium flex items-center gap-1"
            >
              <span class="material-symbols-outlined text-xs">manage_accounts</span>
              Members
            </button>
          </div>
        </div>
      </div>
      </div>
    </div>

    <!-- Create/Edit Structure Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        leave-active-class="duration-200 ease-in"
        leave-to-class="opacity-0"
      >
        <div v-if="showStructureModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-xl flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">{{ editingStructure ? 'edit' : 'add_circle' }}</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">{{ modalTitle }}</h3>
                  <p class="text-sm text-white/70">{{ editingStructure ? 'Update structure details' : 'Define a new organizational unit' }}</p>
                </div>
              </div>
              <button
                @click="showStructureModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-xl transition-colors"
              >
                <span class="material-symbols-outlined text-white">close</span>
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-5">
              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Code *</label>
                  <input
                    v-model="structureForm.code"
                    type="text"
                    placeholder="e.g., HR-001"
                    class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Type *</label>
                  <select
                    v-model="structureForm.type"
                    class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                  >
                    <option v-for="type in structureTypes" :key="type" :value="type">{{ type }}</option>
                  </select>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Name (English) *</label>
                <input
                  v-model="structureForm.name"
                  type="text"
                  placeholder="e.g., Human Resources Department"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Name (Arabic)</label>
                <input
                  v-model="structureForm.nameAr"
                  type="text"
                  dir="rtl"
                  placeholder="قسم الموارد البشرية"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Parent Structure</label>
                <select
                  v-model="structureForm.parentId"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                >
                  <option value="">None (Root Level)</option>
                  <option v-for="s in availableParents" :key="s.id" :value="s.id">
                    {{ s.name }} ({{ s.type }})
                  </option>
                </select>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-gray-100 dark:border-gray-700 p-4 bg-gray-50 dark:bg-slate-800/50 flex justify-end gap-3">
              <button
                @click="showStructureModal = false"
                class="px-5 py-2.5 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-slate-700 rounded-xl font-medium transition-colors"
              >
                Cancel
              </button>
              <button
                @click="saveStructure"
                :disabled="!structureForm.code.trim() || !structureForm.name.trim() || isSaving"
                class="px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-xl font-medium hover:shadow-lg hover:shadow-primary/25 disabled:opacity-50 transition-all flex items-center gap-2"
              >
                <span v-if="isSaving" class="material-symbols-outlined animate-spin text-sm">progress_activity</span>
                {{ isSaving ? 'Saving...' : (editingStructure ? 'Update' : 'Create') }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Members Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        leave-active-class="duration-200 ease-in"
        leave-to-class="opacity-0"
      >
        <div v-if="showMembersModal && selectedStructure" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-xl flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">people</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">Structure Members</h3>
                  <p class="text-sm text-white/70">{{ selectedStructure.name }}</p>
                </div>
              </div>
              <button
                @click="showMembersModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-xl transition-colors"
              >
                <span class="material-symbols-outlined text-white">close</span>
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-5">
              <!-- Add Member Button -->
              <button
                @click="openAddMemberModal"
                class="w-full mb-4 px-4 py-3 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-xl text-gray-500 dark:text-gray-400 hover:border-primary hover:text-primary transition-colors flex items-center justify-center gap-2"
              >
                <span class="material-symbols-outlined">person_add</span>
                Add Member
              </button>

              <div class="max-h-80 overflow-y-auto">
                <div v-if="isLoadingMembers" class="flex items-center justify-center py-8">
                  <span class="material-symbols-outlined animate-spin text-primary text-3xl">progress_activity</span>
                </div>

                <div v-else-if="structureMembers.length === 0" class="text-center py-8">
                  <span class="material-symbols-outlined text-4xl text-gray-300 dark:text-gray-600">person_off</span>
                  <p class="text-gray-500 dark:text-gray-400 mt-2">No members in this structure</p>
                </div>

                <div v-else class="space-y-2">
                  <div
                    v-for="(member, index) in structureMembers"
                    :key="member.userId"
                    class="flex items-center gap-3 p-3 bg-gray-50 dark:bg-slate-800 rounded-xl"
                  >
                    <div :class="[getAvatarColor(index), 'w-10 h-10 rounded-full flex items-center justify-center text-white font-medium']">
                      {{ getInitials(member.userDisplayName || member.userName || '') }}
                    </div>
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2">
                        <span class="font-medium text-gray-900 dark:text-white truncate">{{ member.userDisplayName || member.userName }}</span>
                        <span
                          v-if="member.isPrimary"
                          class="px-2 py-0.5 bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 text-xs rounded-full font-medium"
                        >
                          Primary
                        </span>
                      </div>
                      <div class="text-sm text-gray-500 dark:text-gray-400">{{ member.position || 'No position' }}</div>
                    </div>
                    <button
                      @click="removeMember(member.userId)"
                      class="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                      title="Remove from structure"
                    >
                      <span class="material-symbols-outlined">person_remove</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-gray-100 dark:border-gray-700 p-4 bg-gray-50 dark:bg-slate-800/50">
              <button
                @click="showMembersModal = false"
                class="w-full px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-xl font-medium hover:shadow-lg transition-all"
              >
                Done
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Add Member Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        leave-active-class="duration-200 ease-in"
        leave-to-class="opacity-0"
      >
        <div v-if="showAddMemberModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-xl flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">person_add</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">Add Member</h3>
                  <p class="text-sm text-white/70">Add user to {{ selectedStructure?.name }}</p>
                </div>
              </div>
              <button
                @click="showAddMemberModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-xl transition-colors"
              >
                <span class="material-symbols-outlined text-white">close</span>
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-5">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">User *</label>
                <select
                  v-model="memberForm.userId"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                >
                  <option value="">Select a user...</option>
                  <option v-for="user in availableUsers" :key="user.id" :value="user.id">
                    {{ user.displayName || user.username }} ({{ user.email }})
                  </option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Position</label>
                <input
                  v-model="memberForm.position"
                  type="text"
                  placeholder="e.g., Manager, Team Lead"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Start Date</label>
                <input
                  v-model="memberForm.startDate"
                  type="date"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-xl focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
                />
              </div>

              <div class="flex items-center gap-3 p-4 bg-amber-50 dark:bg-amber-900/20 rounded-xl">
                <UiToggle v-model="memberForm.isPrimary" />
                <div>
                  <div class="font-medium text-gray-900 dark:text-white">Primary Structure</div>
                  <div class="text-sm text-gray-500 dark:text-gray-400">Mark this as the user's primary organizational unit</div>
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-gray-100 dark:border-gray-700 p-4 bg-gray-50 dark:bg-slate-800/50 flex justify-end gap-3">
              <button
                @click="showAddMemberModal = false"
                class="px-5 py-2.5 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-slate-700 rounded-xl font-medium transition-colors"
              >
                Cancel
              </button>
              <button
                @click="addMember"
                :disabled="!memberForm.userId || isSaving"
                class="px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-xl font-medium hover:shadow-lg hover:shadow-primary/25 disabled:opacity-50 transition-all flex items-center gap-2"
              >
                <span v-if="isSaving" class="material-symbols-outlined animate-spin text-sm">progress_activity</span>
                {{ isSaving ? 'Adding...' : 'Add Member' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- Delete Confirmation -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        leave-active-class="duration-200 ease-in"
        leave-to-class="opacity-0"
      >
        <div v-if="showDeleteConfirm && selectedStructure" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-sm p-6 text-center">
            <div class="w-16 h-16 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center mx-auto mb-4">
              <span class="material-symbols-outlined text-red-600 dark:text-red-400 text-3xl">warning</span>
            </div>
            <h3 class="text-lg font-bold text-gray-900 dark:text-white">Delete Structure?</h3>
            <p class="text-gray-500 dark:text-gray-400 mt-2">
              Are you sure you want to delete "{{ selectedStructure.name }}"? This will also remove all member associations.
            </p>
            <div class="flex gap-3 mt-6">
              <button
                @click="showDeleteConfirm = false"
                class="flex-1 px-4 py-2.5 border border-gray-200 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-xl font-medium hover:bg-gray-50 dark:hover:bg-slate-800 transition-colors"
              >
                Cancel
              </button>
              <button
                @click="deleteStructure"
                :disabled="isSaving"
                class="flex-1 px-4 py-2.5 bg-red-600 text-white rounded-xl font-medium hover:bg-red-700 disabled:opacity-50 transition-colors"
              >
                {{ isSaving ? 'Deleting...' : 'Delete' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
