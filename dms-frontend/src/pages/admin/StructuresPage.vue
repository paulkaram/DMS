<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { structuresApi, usersApi } from '@/api/client'
import type { Structure, StructureTree, StructureMember, User } from '@/types'
import { UiToggle, UiSelect } from '@/components/ui'
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
const viewMode = ref<'tree' | 'list' | 'grid'>('tree')

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
  'bg-surface-dark-hover',
  'bg-primary/80',
  'bg-border-dark'
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

// Dropdown options for UiSelect
const typeOptions = structureTypes.map(t => ({ value: t, label: t }))

const parentOptions = computed(() => [
  { value: '', label: 'None (Root Level)' },
  ...availableParents.value.map(s => ({ value: s.id, label: `${s.name} (${s.type})` }))
])

const userOptions = computed(() =>
  availableUsers.value.map(u => ({
    value: u.id,
    label: `${u.displayName || u.username} (${u.email})`
  }))
)

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
    const usersData = usersRes.data
    users.value = Array.isArray(usersData) ? usersData : usersData.items ?? []

    // Try to load tree
    try {
      const treeRes = await structuresApi.getTree()
      // API returns a single virtual root node — unwrap its children
      const treeData = treeRes.data
      if (Array.isArray(treeData)) {
        structureTree.value = treeData
      } else if (treeData?.children) {
        structureTree.value = treeData.children
      } else {
        structureTree.value = []
      }
      // Auto-expand root nodes
      for (const node of structureTree.value) {
        expandedNodes.value.add(node.id)
      }
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

function expandAll() {
  function collectIds(nodes: StructureTree[]) {
    if (!Array.isArray(nodes)) return
    for (const node of nodes) {
      expandedNodes.value.add(node.id)
      if (node.children?.length) collectIds(node.children)
    }
  }
  collectIds(structureTree.value)
}

function collapseAll() {
  expandedNodes.value.clear()
}

function findStructureById(id: string): Structure | undefined {
  return structures.value.find(s => s.id === id)
}

interface FlatTreeNode {
  node: StructureTree
  depth: number
  isLast: boolean
  /** For each ancestor depth, whether that ancestor was the last child (controls connector line) */
  ancestorIsLast: boolean[]
}

const filteredTree = computed((): StructureTree[] => {
  if (!searchQuery.value && selectedTypeFilter.value === 'All') {
    return structureTree.value
  }
  const q = searchQuery.value.toLowerCase()

  function filterNodes(nodes: StructureTree[]): StructureTree[] {
    if (!Array.isArray(nodes)) return []
    const result: StructureTree[] = []
    for (const node of nodes) {
      const filteredChildren = filterNodes(node.children || [])
      const matchesType = selectedTypeFilter.value === 'All' || node.type === selectedTypeFilter.value
      const matchesSearch = !q ||
        node.name?.toLowerCase().includes(q) ||
        node.code?.toLowerCase().includes(q)
      if ((matchesType && matchesSearch) || filteredChildren.length > 0) {
        result.push({ ...node, children: filteredChildren })
      }
    }
    return result
  }
  return filterNodes(structureTree.value)
})

const flattenedTree = computed((): FlatTreeNode[] => {
  const result: FlatTreeNode[] = []
  function walk(nodes: StructureTree[], depth: number, ancestorIsLast: boolean[]) {
    if (!Array.isArray(nodes)) return
    nodes.forEach((node, i) => {
      const isLast = i === nodes.length - 1
      const children = node.children || []
      result.push({ node: { ...node, children }, depth, isLast, ancestorIsLast: [...ancestorIsLast] })
      if (expandedNodes.value.has(node.id) && children.length) {
        walk(children, depth + 1, [...ancestorIsLast, isLast])
      }
    })
  }
  walk(filteredTree.value, 0, [])
  return result
})

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
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Organizational Structures</h1>
        <p class="text-zinc-500 mt-1">Manage departments, divisions, and units</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- View Toggle -->
        <div class="flex items-center bg-zinc-100 dark:bg-surface-dark rounded-lg p-1">
          <button
            @click="viewMode = 'tree'"
            :class="[
              'p-1.5 rounded-md transition-colors',
              viewMode === 'tree'
                ? 'bg-white dark:bg-border-dark text-primary shadow-sm'
                : 'text-zinc-400 hover:text-zinc-600'
            ]"
            title="Tree view"
          >
            <span class="material-symbols-outlined text-lg">account_tree</span>
          </button>
          <button
            @click="viewMode = 'list'"
            :class="[
              'p-1.5 rounded-md transition-colors',
              viewMode === 'list'
                ? 'bg-white dark:bg-border-dark text-primary shadow-sm'
                : 'text-zinc-400 hover:text-zinc-600'
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
                ? 'bg-white dark:bg-border-dark text-primary shadow-sm'
                : 'text-zinc-400 hover:text-zinc-600'
            ]"
            title="Grid view"
          >
            <span class="material-symbols-outlined text-lg">grid_view</span>
          </button>
        </div>
        <!-- Expand/Collapse buttons for tree mode -->
        <div v-if="viewMode === 'tree'" class="flex items-center gap-1">
          <button
            @click="expandAll()"
            class="p-1.5 text-zinc-400 hover:text-primary hover:bg-zinc-100 dark:hover:bg-surface-dark rounded-lg transition-colors"
            title="Expand all"
          >
            <span class="material-symbols-outlined text-lg">unfold_more</span>
          </button>
          <button
            @click="collapseAll()"
            class="p-1.5 text-zinc-400 hover:text-primary hover:bg-zinc-100 dark:hover:bg-surface-dark rounded-lg transition-colors"
            title="Collapse all"
          >
            <span class="material-symbols-outlined text-lg">unfold_less</span>
          </button>
        </div>
        <!-- Search -->
        <div class="relative">
          <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400 text-lg">search</span>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search..."
            class="w-48 pl-10 pr-4 py-2 bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
          />
        </div>
        <button
          @click="openCreateModal()"
          class="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-navy to-teal hover:shadow-lg hover:shadow-teal/25 text-white rounded-lg font-medium text-sm transition-all"
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
          'relative overflow-hidden px-4 py-3 rounded-lg flex items-center justify-between transition-all text-white shadow-lg border',
          selectedTypeFilter === 'All'
            ? 'bg-[#0d1117] border-primary/40 ring-2 ring-primary/30'
            : 'bg-[#0d1117] border-white/5 hover:border-primary/20'
        ]"
      >
        <svg class="absolute right-0 top-0 h-full w-16 opacity-15" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="flex items-center gap-2 relative z-10">
          <span class="material-symbols-outlined text-primary text-lg">apartment</span>
          <span class="text-xs font-medium uppercase tracking-wide">All</span>
        </div>
        <span class="text-lg font-bold relative z-10">{{ structures.length }}</span>
      </button>
      <button
        v-for="type in structureTypes"
        :key="type"
        @click="selectedTypeFilter = type"
        :class="[
          'relative overflow-hidden px-4 py-3 rounded-lg flex items-center justify-between transition-all text-white shadow-lg border',
          selectedTypeFilter === type
            ? 'bg-[#0d1117] border-primary/40 ring-2 ring-primary/30'
            : 'bg-[#0d1117] border-white/5 hover:border-primary/20'
        ]"
      >
        <svg class="absolute right-0 top-0 h-full w-16 opacity-15" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="flex items-center gap-2 relative z-10">
          <span class="material-symbols-outlined text-primary text-lg">{{ getTypeIcon(type) }}</span>
          <span class="text-xs font-medium uppercase tracking-wide hidden xl:inline">{{ type }}</span>
        </div>
        <span class="text-lg font-bold relative z-10">{{ structures.filter(s => s.type === type).length }}</span>
      </button>
    </div>

    <!-- Content Card -->
    <div class="bg-white dark:bg-background-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark overflow-hidden">
      <div class="p-6">
      <!-- Messages -->
      <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
        <div v-if="error" class="mb-6 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-lg flex items-center gap-3">
          <span class="material-symbols-outlined text-red-500">error</span>
          <p class="text-red-600 dark:text-red-400 flex-1">{{ error }}</p>
          <button @click="error = null" class="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 rounded-lg">
            <span class="material-symbols-outlined text-red-400">close</span>
          </button>
        </div>
      </Transition>

      <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
        <div v-if="successMessage" class="mb-6 p-4 bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800/50 rounded-lg flex items-center gap-3">
          <span class="material-symbols-outlined text-emerald-500">check_circle</span>
          <p class="text-emerald-600 dark:text-emerald-400">{{ successMessage }}</p>
        </div>
      </Transition>

      <!-- Loading -->
      <div v-if="isLoading" class="space-y-3">
        <div v-for="i in 5" :key="i" class="h-14 bg-zinc-100 dark:bg-surface-dark rounded-lg animate-pulse"></div>
      </div>

      <!-- Tree View -->
      <div v-else-if="viewMode === 'tree'">
        <div v-if="filteredTree.length === 0" class="text-center py-16">
          <div class="w-20 h-20 rounded-lg bg-zinc-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
            <span class="material-symbols-outlined text-5xl text-zinc-400">account_tree</span>
          </div>
          <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">{{ searchQuery || selectedTypeFilter !== 'All' ? 'No Structures Match' : 'No Structures Found' }}</h3>
          <p class="text-zinc-500 dark:text-zinc-400 mt-1">{{ searchQuery || selectedTypeFilter !== 'All' ? 'Try different search or filter' : 'Create your first organizational structure' }}</p>
          <button
            v-if="!searchQuery && selectedTypeFilter === 'All'"
            @click="openCreateModal()"
            class="mt-4 px-5 py-2.5 bg-gradient-to-r from-navy to-teal text-white rounded-lg font-medium hover:shadow-lg hover:shadow-teal/25 transition-all"
          >
            Create Structure
          </button>
        </div>
        <div v-else>
          <div
            v-for="item in flattenedTree"
            :key="item.node.id"
            class="tree-node-row group flex items-center gap-2 py-1.5 rounded-lg hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors"
            :style="{ paddingLeft: `${item.depth * 24 + 8}px` }"
          >
            <!-- Connector lines -->
            <div
              v-for="d in item.depth"
              :key="'line-' + d"
              class="absolute tree-guide-line"
              :class="{ 'tree-guide-hidden': item.ancestorIsLast[d - 1] }"
              :style="{ left: `${(d - 1) * 24 + 20}px` }"
            ></div>

            <!-- Expand/collapse chevron -->
            <button
              v-if="item.node.children?.length"
              @click="toggleNode(item.node.id)"
              class="w-5 h-5 flex items-center justify-center flex-shrink-0 text-zinc-400 hover:text-primary transition-transform duration-200"
              :class="{ 'tree-chevron-open': expandedNodes.has(item.node.id) }"
            >
              <span class="material-symbols-outlined text-base">chevron_right</span>
            </button>
            <span v-else class="w-5 h-5 flex-shrink-0 flex items-center justify-center">
              <span class="w-1.5 h-1.5 rounded-full bg-zinc-300 dark:bg-surface-dark-hover"></span>
            </span>

            <!-- Type icon -->
            <div :class="[getTypeBadgeClass(item.node.type), 'w-7 h-7 rounded-lg flex items-center justify-center flex-shrink-0']">
              <span class="material-symbols-outlined text-sm">{{ getTypeIcon(item.node.type) }}</span>
            </div>

            <!-- Name -->
            <span class="font-medium text-sm text-zinc-900 dark:text-white truncate">{{ item.node.name }}</span>

            <!-- Code badge -->
            <code class="px-1.5 py-0.5 bg-zinc-100 dark:bg-surface-dark text-zinc-500 dark:text-zinc-400 text-[10px] rounded flex-shrink-0">{{ item.node.code }}</code>

            <!-- Type badge -->
            <span :class="['px-1.5 py-0.5 rounded-full text-[10px] font-medium flex-shrink-0 hidden sm:inline', getTypeBadgeClass(item.node.type)]">
              {{ item.node.type }}
            </span>

            <!-- Member count -->
            <span v-if="item.node.memberCount" class="flex items-center gap-0.5 text-xs text-zinc-400 flex-shrink-0">
              <span class="material-symbols-outlined text-xs">people</span>
              {{ item.node.memberCount }}
            </span>

            <!-- Spacer -->
            <span class="flex-1 min-w-0"></span>

            <!-- Action buttons (show on hover) -->
            <div class="flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0 pr-2">
              <button
                @click.stop="openCreateModal(item.node.id)"
                class="p-1 text-zinc-400 hover:text-primary hover:bg-primary/10 rounded transition-colors"
                title="Add child"
              >
                <span class="material-symbols-outlined text-base">add</span>
              </button>
              <button
                @click.stop="() => { const s = findStructureById(item.node.id); if (s) openEditModal(s) }"
                class="p-1 text-zinc-400 hover:text-primary hover:bg-primary/10 rounded transition-colors"
                title="Edit"
              >
                <span class="material-symbols-outlined text-base">edit</span>
              </button>
              <button
                @click.stop="() => { const s = findStructureById(item.node.id); if (s) openMembersModal(s) }"
                class="p-1 text-zinc-400 hover:text-primary hover:bg-primary/10 rounded transition-colors"
                title="Members"
              >
                <span class="material-symbols-outlined text-base">people</span>
              </button>
              <button
                @click.stop="() => { const s = findStructureById(item.node.id); if (s) confirmDelete(s) }"
                class="p-1 text-zinc-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded transition-colors"
                title="Delete"
              >
                <span class="material-symbols-outlined text-base">delete</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Empty State (list/grid) -->
      <div v-else-if="filteredStructures.length === 0" class="text-center py-16">
        <div class="w-20 h-20 rounded-lg bg-zinc-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-5xl text-zinc-400">domain_disabled</span>
        </div>
        <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">No Structures Found</h3>
        <p class="text-zinc-500 dark:text-zinc-400 mt-1">{{ searchQuery ? 'Try a different search term' : 'Create your first organizational structure' }}</p>
        <button
          v-if="!searchQuery"
          @click="openCreateModal()"
          class="mt-4 px-5 py-2.5 bg-gradient-to-r from-navy to-teal text-white rounded-lg font-medium hover:shadow-lg hover:shadow-teal/25 transition-all"
        >
          Create Structure
        </button>
      </div>

      <!-- List View -->
      <div v-else-if="viewMode === 'list'" class="overflow-x-auto">
        <table class="w-full">
          <thead>
            <tr class="border-b border-zinc-100 dark:border-border-dark">
              <th class="text-left py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Structure</th>
              <th class="text-left py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Code</th>
              <th class="text-left py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Type</th>
              <th class="text-left py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Parent</th>
              <th class="text-center py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Members</th>
              <th class="text-center py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Status</th>
              <th class="text-right py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(structure, index) in filteredStructures"
              :key="structure.id"
              class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors"
            >
              <td class="py-3 px-4">
                <div class="flex items-center gap-3">
                  <div :class="[getAvatarColor(index), 'w-9 h-9 rounded-lg flex items-center justify-center']">
                    <span class="material-symbols-outlined text-white text-lg">{{ getTypeIcon(structure.type) }}</span>
                  </div>
                  <div>
                    <span class="font-medium text-zinc-900 dark:text-white">{{ structure.name }}</span>
                    <p v-if="structure.nameAr" class="text-xs text-zinc-400">{{ structure.nameAr }}</p>
                  </div>
                </div>
              </td>
              <td class="py-3 px-4">
                <code class="px-2 py-1 bg-zinc-100 dark:bg-surface-dark text-zinc-600 dark:text-zinc-300 text-xs rounded">{{ structure.code }}</code>
              </td>
              <td class="py-3 px-4">
                <span :class="['px-2 py-1 rounded-full text-xs font-medium', getTypeBadgeClass(structure.type)]">
                  {{ structure.type }}
                </span>
              </td>
              <td class="py-3 px-4">
                <span class="text-sm text-zinc-500 dark:text-zinc-400">{{ getParentName(structure.parentId) || '-' }}</span>
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
                      : 'bg-zinc-100 dark:bg-border-dark text-zinc-600 dark:text-zinc-400'
                  ]"
                >
                  {{ structure.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4">
                <div class="flex items-center justify-end gap-1">
                  <button
                    @click="openCreateModal(structure.id)"
                    class="p-1.5 text-zinc-400 hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                    title="Add Child"
                  >
                    <span class="material-symbols-outlined text-lg">add</span>
                  </button>
                  <button
                    @click="openEditModal(structure)"
                    class="p-1.5 text-zinc-400 hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                    title="Edit"
                  >
                    <span class="material-symbols-outlined text-lg">edit</span>
                  </button>
                  <button
                    @click="confirmDelete(structure)"
                    class="p-1.5 text-zinc-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
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
      <div v-else-if="viewMode === 'grid'" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        <div
          v-for="(structure, index) in filteredStructures"
          :key="structure.id"
          class="bg-zinc-50 dark:bg-surface-dark rounded-lg p-4 border border-zinc-100 dark:border-border-dark hover:border-primary/30 hover:shadow-md transition-all group"
        >
          <div class="flex items-start justify-between mb-3">
            <div :class="[getAvatarColor(index), 'w-10 h-10 rounded-lg flex items-center justify-center']">
              <span class="material-symbols-outlined text-white">{{ getTypeIcon(structure.type) }}</span>
            </div>
            <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <button
                @click="openCreateModal(structure.id)"
                class="p-1 text-zinc-400 hover:text-primary hover:bg-white dark:hover:bg-border-dark rounded transition-colors"
                title="Add Child"
              >
                <span class="material-symbols-outlined text-sm">add</span>
              </button>
              <button
                @click="openEditModal(structure)"
                class="p-1 text-zinc-400 hover:text-primary hover:bg-white dark:hover:bg-border-dark rounded transition-colors"
                title="Edit"
              >
                <span class="material-symbols-outlined text-sm">edit</span>
              </button>
              <button
                @click="confirmDelete(structure)"
                class="p-1 text-zinc-400 hover:text-red-500 hover:bg-white dark:hover:bg-border-dark rounded transition-colors"
                title="Delete"
              >
                <span class="material-symbols-outlined text-sm">delete</span>
              </button>
            </div>
          </div>

          <h3 class="font-semibold text-zinc-900 dark:text-white text-sm mb-1">{{ structure.name }}</h3>
          <p v-if="structure.nameAr" class="text-xs text-zinc-400 mb-1">{{ structure.nameAr }}</p>
          <code class="px-2 py-0.5 bg-zinc-200 dark:bg-border-dark text-zinc-600 dark:text-zinc-300 text-xs rounded">{{ structure.code }}</code>

          <div class="flex items-center gap-2 mt-3 mb-3">
            <span :class="['px-2 py-0.5 rounded-full text-xs font-medium', getTypeBadgeClass(structure.type)]">
              {{ structure.type }}
            </span>
            <span
              :class="[
                'px-2 py-0.5 rounded-full text-xs font-medium',
                structure.isActive
                  ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                  : 'bg-zinc-200 dark:bg-surface-dark-hover text-zinc-600 dark:text-zinc-400'
              ]"
            >
              {{ structure.isActive ? 'Active' : 'Inactive' }}
            </span>
          </div>

          <div class="flex items-center justify-between pt-3 border-t border-zinc-200 dark:border-border-dark">
            <div class="flex items-center gap-3 text-xs text-zinc-500">
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
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">{{ editingStructure ? 'edit' : 'add_circle' }}</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">{{ modalTitle }}</h3>
                  <p class="text-sm text-white/70">{{ editingStructure ? 'Update structure details' : 'Define a new organizational unit' }}</p>
                </div>
              </div>
              <button
                @click="showStructureModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-lg transition-colors"
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
                    class="w-full px-4 py-3 border border-gray-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                  />
                </div>
                <div>
                  <UiSelect
                    v-model="structureForm.type"
                    :options="typeOptions"
                    label="Type *"
                    placeholder="Select type"
                  />
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Name (English) *</label>
                <input
                  v-model="structureForm.name"
                  type="text"
                  placeholder="e.g., Human Resources Department"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Name (Arabic)</label>
                <input
                  v-model="structureForm.nameAr"
                  type="text"
                  dir="rtl"
                  placeholder="قسم الموارد البشرية"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                />
              </div>

              <div>
                <UiSelect
                  v-model="structureForm.parentId"
                  :options="parentOptions"
                  label="Parent Structure"
                  placeholder="None (Root Level)"
                  searchable
                  clearable
                />
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-gray-100 dark:border-border-dark p-4 bg-gray-50 dark:bg-surface-dark/50 flex justify-end gap-3">
              <button
                @click="showStructureModal = false"
                class="px-5 py-2.5 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg font-medium transition-colors"
              >
                Cancel
              </button>
              <button
                @click="saveStructure"
                :disabled="!structureForm.code.trim() || !structureForm.name.trim() || isSaving"
                class="px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-lg font-medium hover:shadow-lg hover:shadow-primary/25 disabled:opacity-50 transition-all flex items-center gap-2"
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
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-lg overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">people</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">Structure Members</h3>
                  <p class="text-sm text-white/70">{{ selectedStructure.name }}</p>
                </div>
              </div>
              <button
                @click="showMembersModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-white">close</span>
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-5">
              <!-- Add Member Button -->
              <button
                @click="openAddMemberModal"
                class="w-full mb-4 px-4 py-3 border-2 border-dashed border-gray-200 dark:border-border-dark rounded-lg text-gray-500 dark:text-gray-400 hover:border-primary hover:text-primary transition-colors flex items-center justify-center gap-2"
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
                    class="flex items-center gap-3 p-3 bg-gray-50 dark:bg-surface-dark rounded-lg"
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
            <div class="border-t border-gray-100 dark:border-border-dark p-4 bg-gray-50 dark:bg-surface-dark/50">
              <button
                @click="showMembersModal = false"
                class="w-full px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-lg font-medium hover:shadow-lg transition-all"
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
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">person_add</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">Add Member</h3>
                  <p class="text-sm text-white/70">Add user to {{ selectedStructure?.name }}</p>
                </div>
              </div>
              <button
                @click="showAddMemberModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-white">close</span>
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-5">
              <div>
                <UiSelect
                  v-model="memberForm.userId"
                  :options="userOptions"
                  label="User *"
                  placeholder="Select a user..."
                  searchable
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Position</label>
                <input
                  v-model="memberForm.position"
                  type="text"
                  placeholder="e.g., Manager, Team Lead"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Start Date</label>
                <input
                  v-model="memberForm.startDate"
                  type="date"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                />
              </div>

              <div class="flex items-center gap-3 p-4 bg-amber-50 dark:bg-amber-900/20 rounded-lg">
                <UiToggle v-model="memberForm.isPrimary" />
                <div>
                  <div class="font-medium text-gray-900 dark:text-white">Primary Structure</div>
                  <div class="text-sm text-gray-500 dark:text-gray-400">Mark this as the user's primary organizational unit</div>
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-gray-100 dark:border-border-dark p-4 bg-gray-50 dark:bg-surface-dark/50 flex justify-end gap-3">
              <button
                @click="showAddMemberModal = false"
                class="px-5 py-2.5 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg font-medium transition-colors"
              >
                Cancel
              </button>
              <button
                @click="addMember"
                :disabled="!memberForm.userId || isSaving"
                class="px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-lg font-medium hover:shadow-lg hover:shadow-primary/25 disabled:opacity-50 transition-all flex items-center gap-2"
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
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-sm p-6 text-center">
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
                class="flex-1 px-4 py-2.5 border border-gray-200 dark:border-border-dark text-gray-700 dark:text-gray-300 rounded-lg font-medium hover:bg-gray-50 dark:hover:bg-surface-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="deleteStructure"
                :disabled="isSaving"
                class="flex-1 px-4 py-2.5 bg-red-600 text-white rounded-lg font-medium hover:bg-red-700 disabled:opacity-50 transition-colors"
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

<style scoped>
.tree-chevron-open {
  transform: rotate(90deg);
}

.tree-node-row {
  position: relative;
}

.tree-guide-line {
  top: 0;
  bottom: 0;
  width: 1px;
  background-color: rgb(228 228 231); /* zinc-200 */
}

:is(.dark) .tree-guide-line {
  background-color: theme('colors.border-dark');
}

.tree-guide-hidden {
  display: none;
}
</style>
