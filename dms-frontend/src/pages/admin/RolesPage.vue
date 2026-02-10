<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { rolesApi, usersApi } from '@/api/client'
import type { Role, User } from '@/types'
import { UiCheckbox, UiToggle } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

// State
const roles = ref<Role[]>([])
const users = ref<User[]>([])
const searchQuery = ref('')
const isLoading = ref(true)
const isSaving = ref(false)
const error = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const viewMode = ref<'list' | 'grid'>('list')

// Modal state
const showRoleModal = ref(false)
const showMembersModal = ref(false)
const showDeleteConfirm = ref(false)
const editingRole = ref<Role | null>(null)
const selectedRole = ref<Role | null>(null)
const roleMembers = ref<User[]>([])
const isLoadingMembers = ref(false)

// Form state
const roleForm = ref({
  name: '',
  description: ''
})

// Alternating colors for avatars
const avatarColors = [
  'bg-primary',
  'bg-navy',
  'bg-zinc-600',
  'bg-primary/80',
  'bg-zinc-700'
]

function getAvatarColor(index: number) {
  return avatarColors[index % avatarColors.length]
}

// Computed
const filteredRoles = computed(() => {
  if (!searchQuery.value) return roles.value
  const q = searchQuery.value.toLowerCase()
  return roles.value.filter(r =>
    r.name?.toLowerCase().includes(q) ||
    r.description?.toLowerCase().includes(q)
  )
})

const modalTitle = computed(() => editingRole.value ? 'Edit Role' : 'Create New Role')

// Load data
onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  error.value = null
  try {
    const [rolesRes, usersRes] = await Promise.all([
      rolesApi.getAll(),
      usersApi.getAll()
    ])
    roles.value = rolesRes.data
    users.value = usersRes.data
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to load roles'
  } finally {
    isLoading.value = false
  }
}

function showSuccess(message: string) {
  successMessage.value = message
  setTimeout(() => { successMessage.value = null }, 3000)
}

// CRUD operations
function openCreateModal() {
  editingRole.value = null
  roleForm.value = { name: '', description: '' }
  showRoleModal.value = true
}

function openEditModal(role: Role) {
  editingRole.value = role
  roleForm.value = {
    name: role.name,
    description: role.description || ''
  }
  showRoleModal.value = true
}

async function saveRole() {
  if (!roleForm.value.name.trim()) return

  isSaving.value = true
  error.value = null
  try {
    if (editingRole.value) {
      await rolesApi.update(editingRole.value.id, roleForm.value)
      showSuccess('Role updated successfully')
    } else {
      await rolesApi.create(roleForm.value)
      showSuccess('Role created successfully')
    }
    showRoleModal.value = false
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to save role'
  } finally {
    isSaving.value = false
  }
}

function confirmDelete(role: Role) {
  selectedRole.value = role
  showDeleteConfirm.value = true
}

async function deleteRole() {
  if (!selectedRole.value) return

  isSaving.value = true
  try {
    await rolesApi.delete(selectedRole.value.id)
    showSuccess('Role deleted successfully')
    showDeleteConfirm.value = false
    selectedRole.value = null
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to delete role'
  } finally {
    isSaving.value = false
  }
}

// Members management
async function openMembersModal(role: Role) {
  selectedRole.value = role
  showMembersModal.value = true
  isLoadingMembers.value = true
  try {
    const res = await rolesApi.getMembers(role.id)
    roleMembers.value = res.data
  } catch (err) {
    roleMembers.value = []
  } finally {
    isLoadingMembers.value = false
  }
}

async function removeMember(userId: string) {
  if (!selectedRole.value) return
  try {
    await rolesApi.removeMember(selectedRole.value.id, userId)
    roleMembers.value = roleMembers.value.filter(m => m.id !== userId)
    showSuccess('Member removed')
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to remove member'
  }
}

function getInitials(name: string) {
  return name?.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase() || '??'
}
</script>

<template>
  <div class="space-y-6">
    <!-- Breadcrumb -->
    <AdminBreadcrumb current-page="Roles" icon="badge" />

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Roles Management</h1>
        <p class="text-zinc-500 mt-1">Define roles and manage memberships</p>
      </div>
      <div class="flex items-center gap-3">
        <!-- View Toggle -->
        <div class="flex items-center bg-zinc-100 dark:bg-surface-dark rounded-lg p-1">
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
        <!-- Search -->
        <div class="relative">
          <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400 text-lg">search</span>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search roles..."
            class="w-64 pl-10 pr-4 py-2 bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none"
          />
        </div>
        <button
          @click="openCreateModal"
          class="flex items-center gap-2 px-4 py-2 bg-gradient-to-r from-navy to-teal hover:shadow-lg hover:shadow-teal/25 text-white rounded-lg font-medium text-sm transition-all"
        >
          <span class="material-symbols-outlined text-lg">add</span>
          Create Role
        </button>
      </div>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="relative overflow-hidden bg-[#0d1117] rounded-lg p-4 flex items-center gap-4 shadow-lg border border-white/5">
        <svg class="absolute right-0 top-0 h-full w-24 opacity-15" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="w-12 h-12 rounded-lg bg-teal/20 flex items-center justify-center relative z-10">
          <span class="material-symbols-outlined text-teal text-2xl">shield_person</span>
        </div>
        <div class="relative z-10">
          <p class="text-3xl font-bold text-white">{{ roles.length }}</p>
          <p class="text-xs text-zinc-400 uppercase tracking-wide">Total Roles</p>
        </div>
      </div>
      <div class="relative overflow-hidden bg-[#0d1117] rounded-lg p-4 flex items-center gap-4 shadow-lg border border-white/5">
        <svg class="absolute right-0 top-0 h-full w-24 opacity-15" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="w-12 h-12 rounded-lg bg-emerald-500/20 flex items-center justify-center relative z-10">
          <span class="material-symbols-outlined text-emerald-400 text-2xl">check_circle</span>
        </div>
        <div class="relative z-10">
          <p class="text-3xl font-bold text-white">{{ roles.filter(r => r.isActive !== false).length }}</p>
          <p class="text-xs text-zinc-400 uppercase tracking-wide">Active Roles</p>
        </div>
      </div>
      <div class="relative overflow-hidden bg-[#0d1117] rounded-lg p-4 flex items-center gap-4 shadow-lg border border-white/5">
        <svg class="absolute right-0 top-0 h-full w-24 opacity-15" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
        <div class="w-12 h-12 rounded-lg bg-blue-500/20 flex items-center justify-center relative z-10">
          <span class="material-symbols-outlined text-blue-400 text-2xl">groups</span>
        </div>
        <div class="relative z-10">
          <p class="text-3xl font-bold text-white">{{ users.length }}</p>
          <p class="text-xs text-zinc-400 uppercase tracking-wide">Total Users</p>
        </div>
      </div>
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

      <!-- Empty State -->
      <div v-else-if="filteredRoles.length === 0" class="text-center py-16">
        <div class="w-20 h-20 rounded-lg bg-zinc-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-5xl text-zinc-400">group_off</span>
        </div>
        <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">No Roles Found</h3>
        <p class="text-zinc-500 dark:text-zinc-400 mt-1">{{ searchQuery ? 'Try a different search term' : 'Create your first role to get started' }}</p>
        <button
          v-if="!searchQuery"
          @click="openCreateModal"
          class="mt-4 px-5 py-2.5 bg-gradient-to-r from-navy to-teal text-white rounded-lg font-medium hover:shadow-lg hover:shadow-teal/25 transition-all"
        >
          Create Role
        </button>
      </div>

      <!-- List View -->
      <div v-else-if="viewMode === 'list'" class="overflow-x-auto">
        <table class="w-full">
          <thead>
            <tr class="border-b border-zinc-100 dark:border-border-dark">
              <th class="text-left py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Role</th>
              <th class="text-left py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Description</th>
              <th class="text-center py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Members</th>
              <th class="text-center py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Status</th>
              <th class="text-right py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="(role, index) in filteredRoles"
              :key="role.id"
              class="border-b border-zinc-50 dark:border-border-dark/50 hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors"
            >
              <td class="py-3 px-4">
                <div class="flex items-center gap-3">
                  <div :class="[getAvatarColor(index), 'w-9 h-9 rounded-lg flex items-center justify-center']">
                    <span class="material-symbols-outlined text-white text-lg">badge</span>
                  </div>
                  <span class="font-medium text-zinc-900 dark:text-white">{{ role.name }}</span>
                </div>
              </td>
              <td class="py-3 px-4">
                <span class="text-sm text-zinc-500 dark:text-zinc-400 line-clamp-1">{{ role.description || 'No description' }}</span>
              </td>
              <td class="py-3 px-4 text-center">
                <button
                  @click="openMembersModal(role)"
                  class="inline-flex items-center gap-1 px-2 py-1 text-sm text-primary hover:bg-primary/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-sm">people</span>
                  Manage
                </button>
              </td>
              <td class="py-3 px-4 text-center">
                <span
                  :class="[
                    'px-2 py-1 rounded-full text-xs font-medium',
                    role.isActive !== false
                      ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                      : 'bg-zinc-100 dark:bg-border-dark text-zinc-600 dark:text-zinc-400'
                  ]"
                >
                  {{ role.isActive !== false ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4">
                <div class="flex items-center justify-end gap-1">
                  <button
                    @click="openEditModal(role)"
                    class="p-1.5 text-zinc-400 hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                    title="Edit"
                  >
                    <span class="material-symbols-outlined text-lg">edit</span>
                  </button>
                  <button
                    @click="confirmDelete(role)"
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
      <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        <div
          v-for="(role, index) in filteredRoles"
          :key="role.id"
          class="bg-zinc-50 dark:bg-surface-dark rounded-lg p-4 border border-zinc-100 dark:border-border-dark hover:border-primary/30 hover:shadow-md transition-all group"
        >
          <div class="flex items-start justify-between mb-3">
            <div :class="[getAvatarColor(index), 'w-10 h-10 rounded-lg flex items-center justify-center']">
              <span class="material-symbols-outlined text-white">badge</span>
            </div>
            <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
              <button
                @click="openEditModal(role)"
                class="p-1 text-zinc-400 hover:text-primary hover:bg-white dark:hover:bg-border-dark rounded transition-colors"
                title="Edit"
              >
                <span class="material-symbols-outlined text-sm">edit</span>
              </button>
              <button
                @click="confirmDelete(role)"
                class="p-1 text-zinc-400 hover:text-red-500 hover:bg-white dark:hover:bg-border-dark rounded transition-colors"
                title="Delete"
              >
                <span class="material-symbols-outlined text-sm">delete</span>
              </button>
            </div>
          </div>

          <h3 class="font-semibold text-zinc-900 dark:text-white text-sm mb-1">{{ role.name }}</h3>
          <p class="text-xs text-zinc-500 dark:text-zinc-400 line-clamp-2 mb-3">{{ role.description || 'No description' }}</p>

          <div class="flex items-center justify-between pt-3 border-t border-zinc-200 dark:border-border-dark">
            <span
              :class="[
                'px-2 py-0.5 rounded-full text-xs font-medium',
                role.isActive !== false
                  ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400'
                  : 'bg-zinc-200 dark:bg-zinc-600 text-zinc-600 dark:text-zinc-400'
              ]"
            >
              {{ role.isActive !== false ? 'Active' : 'Inactive' }}
            </span>
            <button
              @click="openMembersModal(role)"
              class="text-xs text-primary hover:text-primary/80 font-medium flex items-center gap-1"
            >
              <span class="material-symbols-outlined text-xs">people</span>
              Members
            </button>
          </div>
        </div>
      </div>
      </div>
    </div>

    <!-- Create/Edit Role Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        leave-active-class="duration-200 ease-in"
        leave-to-class="opacity-0"
      >
        <div v-if="showRoleModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">{{ editingRole ? 'edit' : 'add_circle' }}</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">{{ modalTitle }}</h3>
                  <p class="text-sm text-white/70">{{ editingRole ? 'Update role details' : 'Define a new role' }}</p>
                </div>
              </div>
              <button
                @click="showRoleModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-white">close</span>
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-5">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Role Name *</label>
                <input
                  v-model="roleForm.name"
                  type="text"
                  placeholder="e.g., Administrator"
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Description</label>
                <textarea
                  v-model="roleForm.description"
                  rows="3"
                  placeholder="Describe the role's purpose and permissions..."
                  class="w-full px-4 py-3 border border-gray-200 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white resize-none"
                ></textarea>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-gray-100 dark:border-gray-700 p-4 bg-gray-50 dark:bg-surface-dark/50 flex justify-end gap-3">
              <button
                @click="showRoleModal = false"
                class="px-5 py-2.5 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg font-medium transition-colors"
              >
                Cancel
              </button>
              <button
                @click="saveRole"
                :disabled="!roleForm.name.trim() || isSaving"
                class="px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-lg font-medium hover:shadow-lg hover:shadow-primary/25 disabled:opacity-50 transition-all flex items-center gap-2"
              >
                <span v-if="isSaving" class="material-symbols-outlined animate-spin text-sm">progress_activity</span>
                {{ isSaving ? 'Saving...' : (editingRole ? 'Update Role' : 'Create Role') }}
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
        <div v-if="showMembersModal && selectedRole" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-lg overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
                  <span class="material-symbols-outlined text-white">people</span>
                </div>
                <div>
                  <h3 class="text-lg font-bold text-white">Role Members</h3>
                  <p class="text-sm text-white/70">{{ selectedRole.name }}</p>
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
            <div class="p-5 max-h-96 overflow-y-auto">
              <div v-if="isLoadingMembers" class="flex items-center justify-center py-8">
                <span class="material-symbols-outlined animate-spin text-primary text-3xl">progress_activity</span>
              </div>

              <div v-else-if="roleMembers.length === 0" class="text-center py-8">
                <span class="material-symbols-outlined text-4xl text-gray-300 dark:text-gray-600">person_off</span>
                <p class="text-gray-500 dark:text-gray-400 mt-2">No members in this role</p>
              </div>

              <div v-else class="space-y-2">
                <div
                  v-for="(member, index) in roleMembers"
                  :key="member.id"
                  class="flex items-center gap-3 p-3 bg-gray-50 dark:bg-surface-dark rounded-lg"
                >
                  <div :class="[getAvatarColor(index), 'w-10 h-10 rounded-full flex items-center justify-center text-white font-medium']">
                    {{ getInitials(member.displayName || member.username || '') }}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="font-medium text-gray-900 dark:text-white truncate">{{ member.displayName || member.username }}</div>
                    <div class="text-sm text-gray-500 dark:text-gray-400 truncate">{{ member.email }}</div>
                  </div>
                  <button
                    @click="removeMember(member.id)"
                    class="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                    title="Remove from role"
                  >
                    <span class="material-symbols-outlined">person_remove</span>
                  </button>
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-gray-100 dark:border-gray-700 p-4 bg-gray-50 dark:bg-surface-dark/50">
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

    <!-- Delete Confirmation -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        leave-active-class="duration-200 ease-in"
        leave-to-class="opacity-0"
      >
        <div v-if="showDeleteConfirm && selectedRole" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-sm p-6 text-center">
            <div class="w-16 h-16 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center mx-auto mb-4">
              <span class="material-symbols-outlined text-red-600 dark:text-red-400 text-3xl">warning</span>
            </div>
            <h3 class="text-lg font-bold text-gray-900 dark:text-white">Delete Role?</h3>
            <p class="text-gray-500 dark:text-gray-400 mt-2">
              Are you sure you want to delete "{{ selectedRole.name }}"? This action cannot be undone.
            </p>
            <div class="flex gap-3 mt-6">
              <button
                @click="showDeleteConfirm = false"
                class="flex-1 px-4 py-2.5 border border-gray-200 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg font-medium hover:bg-gray-50 dark:hover:bg-surface-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="deleteRole"
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
