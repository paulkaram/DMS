<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { usersApi, rolesApi, structuresApi } from '@/api/client'
import type { User, Role, Structure } from '@/types'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'
import Select from '@/components/ui/Select.vue'

// State
const users = ref<User[]>([])
const roles = ref<Role[]>([])
const structures = ref<Structure[]>([])
const searchQuery = ref('')
const isLoading = ref(true)
const selectedUser = ref<User | null>(null)
const showUserModal = ref(false)
const userRoles = ref<Role[]>([])
const userStructures = ref<Structure[]>([])
const isLoadingDetails = ref(false)
const viewMode = ref<'list' | 'grid'>('list')

// Computed
const filteredUsers = computed(() => {
  if (!searchQuery.value) return users.value
  const query = searchQuery.value.toLowerCase()
  return users.value.filter(u =>
    u.username?.toLowerCase().includes(query) ||
    u.displayName?.toLowerCase().includes(query) ||
    u.email?.toLowerCase().includes(query) ||
    u.firstName?.toLowerCase().includes(query) ||
    u.lastName?.toLowerCase().includes(query)
  )
})

const stats = computed(() => ({
  total: users.value.length,
  withEmail: users.value.filter(u => u.email).length,
  active: users.value.length
}))

// Avatar colors - alternating teal/navy
const avatarColors = [
  'bg-primary',
  'bg-navy',
  'bg-slate-600',
  'bg-primary/80',
  'bg-slate-700'
]

function getAvatarColor(index: number): string {
  return avatarColors[index % avatarColors.length]
}

// Load data
onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [usersResponse, rolesResponse, structuresResponse] = await Promise.all([
      usersApi.getAll(),
      rolesApi.getAll(),
      structuresApi.getAll()
    ])
    users.value = usersResponse.data
    roles.value = rolesResponse.data
    structures.value = structuresResponse.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

async function handleSearch() {
  if (!searchQuery.value.trim()) {
    await loadData()
    return
  }
  isLoading.value = true
  try {
    const response = await usersApi.search(searchQuery.value)
    users.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

const isAssigningRole = ref(false)
const isRemovingRole = ref<string | null>(null)
const selectedRoleToAdd = ref<string>('')

async function viewUser(user: User) {
  selectedUser.value = user
  showUserModal.value = true
  isLoadingDetails.value = true
  userRoles.value = []
  userStructures.value = []
  selectedRoleToAdd.value = ''

  try {
    const [structuresRes, rolesRes] = await Promise.all([
      structuresApi.getUserStructures(user.id),
      usersApi.getUserRoles(user.id)
    ])
    userStructures.value = structuresRes.data
    userRoles.value = rolesRes.data
  } catch (err) {
  } finally {
    isLoadingDetails.value = false
  }
}

// Available roles that the user doesn't already have
const availableRolesToAssign = computed(() => {
  const userRoleIds = userRoles.value.map(r => r.id)
  return roles.value.filter(r => !userRoleIds.includes(r.id))
})

// Options formatted for Select component
const roleOptions = computed(() => {
  return availableRolesToAssign.value.map(r => ({
    value: r.id,
    label: r.name
  }))
})

async function assignRoleToUser() {
  if (!selectedUser.value || !selectedRoleToAdd.value) return

  isAssigningRole.value = true
  try {
    await usersApi.assignRole(selectedUser.value.id, selectedRoleToAdd.value)
    // Reload user roles
    const rolesRes = await usersApi.getUserRoles(selectedUser.value.id)
    userRoles.value = rolesRes.data
    selectedRoleToAdd.value = ''
  } catch (err) {
  } finally {
    isAssigningRole.value = false
  }
}

async function removeRoleFromUser(roleId: string) {
  if (!selectedUser.value) return

  isRemovingRole.value = roleId
  try {
    await usersApi.removeRole(selectedUser.value.id, roleId)
    // Remove from local state
    userRoles.value = userRoles.value.filter(r => r.id !== roleId)
  } catch (err) {
  } finally {
    isRemovingRole.value = null
  }
}

function getInitials(user: User) {
  if (user.firstName && user.lastName) {
    return (user.firstName[0] + user.lastName[0]).toUpperCase()
  }
  if (user.displayName) {
    return user.displayName.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase()
  }
  return user.username?.substring(0, 2).toUpperCase() || '??'
}

function getFullName(user: User): string {
  if (user.firstName && user.lastName) {
    return `${user.firstName} ${user.lastName}`
  }
  return user.displayName || user.username || '-'
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
</script>

<template>
  <div class="p-6">
    <div class="max-w-7xl mx-auto space-y-6">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Users" icon="group" />

      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-slate-900 dark:text-white">Users Management</h1>
          <p class="text-slate-500 dark:text-slate-400 mt-1">View and manage system users</p>
        </div>
        <div class="flex items-center gap-3">
          <!-- View Toggle -->
          <div class="flex items-center bg-slate-100 dark:bg-slate-800 rounded-lg p-1">
            <button
              @click="viewMode = 'list'"
              :class="[
                'p-2 rounded-md transition-all',
                viewMode === 'list'
                  ? 'bg-white dark:bg-slate-700 text-primary shadow-sm'
                  : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
              ]"
              title="List view"
            >
              <span class="material-symbols-outlined text-lg">view_list</span>
            </button>
            <button
              @click="viewMode = 'grid'"
              :class="[
                'p-2 rounded-md transition-all',
                viewMode === 'grid'
                  ? 'bg-white dark:bg-slate-700 text-primary shadow-sm'
                  : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
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
              placeholder="Search users..."
              class="w-64 pl-10 pr-4 py-2.5 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-900 dark:text-white placeholder-slate-400 text-sm focus:ring-2 focus:ring-primary/30 focus:border-primary outline-none transition-all"
              @keyup.enter="handleSearch"
            />
          </div>
        </div>
      </div>

      <!-- Stats Cards - More Compact -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-navy rounded-xl p-4 flex items-center gap-4">
          <div class="w-12 h-12 rounded-lg bg-primary/20 flex items-center justify-center">
            <span class="material-symbols-outlined text-primary text-2xl">groups</span>
          </div>
          <div>
            <p class="text-3xl font-bold text-white">{{ stats.total }}</p>
            <p class="text-xs text-slate-400 uppercase tracking-wide">Total Users</p>
          </div>
        </div>
        <div class="bg-slate-800 rounded-xl p-4 flex items-center gap-4">
          <div class="w-12 h-12 rounded-lg bg-primary/20 flex items-center justify-center">
            <span class="material-symbols-outlined text-primary text-2xl">mail</span>
          </div>
          <div>
            <p class="text-3xl font-bold text-white">{{ stats.withEmail }}</p>
            <p class="text-xs text-slate-400 uppercase tracking-wide">With Email</p>
          </div>
        </div>
        <div class="bg-slate-700 rounded-xl p-4 flex items-center gap-4">
          <div class="w-12 h-12 rounded-lg bg-primary/20 flex items-center justify-center">
            <span class="material-symbols-outlined text-primary text-2xl">shield_person</span>
          </div>
          <div>
            <p class="text-3xl font-bold text-white">{{ roles.length }}</p>
            <p class="text-xs text-slate-400 uppercase tracking-wide">Available Roles</p>
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <!-- Loading -->
        <div v-if="isLoading" class="p-8">
          <div class="flex items-center justify-center gap-3">
            <span class="material-symbols-outlined animate-spin text-primary text-2xl">progress_activity</span>
            <span class="text-slate-500">Loading users...</span>
          </div>
        </div>

        <!-- Empty State -->
        <div v-else-if="filteredUsers.length === 0" class="text-center py-16">
          <div class="w-16 h-16 rounded-2xl bg-slate-100 dark:bg-slate-800 flex items-center justify-center mx-auto mb-4">
            <span class="material-symbols-outlined text-4xl text-slate-400">person_off</span>
          </div>
          <h3 class="text-lg font-semibold text-slate-700 dark:text-slate-300">No Users Found</h3>
          <p class="text-slate-500 dark:text-slate-400 mt-1">{{ searchQuery ? 'Try a different search term' : 'No users in the system' }}</p>
        </div>

        <!-- LIST VIEW -->
        <div v-else-if="viewMode === 'list'">
          <table class="w-full">
            <thead class="bg-slate-50 dark:bg-slate-800/50">
              <tr>
                <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">User</th>
                <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Username</th>
                <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Email</th>
                <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Roles</th>
                <th class="text-left py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Status</th>
                <th class="text-right py-3 px-4 text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr
                v-for="(user, index) in filteredUsers"
                :key="user.id"
                class="hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors cursor-pointer"
                @click="viewUser(user)"
              >
                <td class="py-3 px-4">
                  <div class="flex items-center gap-3">
                    <div
                      :class="[
                        'w-10 h-10 rounded-full flex items-center justify-center text-white text-sm font-semibold',
                        getAvatarColor(index)
                      ]"
                    >
                      {{ getInitials(user) }}
                    </div>
                    <div>
                      <p class="font-medium text-slate-900 dark:text-white">{{ getFullName(user) }}</p>
                      <p class="text-xs text-slate-500 dark:text-slate-400">{{ user.displayName || '-' }}</p>
                    </div>
                  </div>
                </td>
                <td class="py-3 px-4">
                  <span class="text-sm text-slate-700 dark:text-slate-300 font-mono">{{ user.username }}</span>
                </td>
                <td class="py-3 px-4">
                  <span class="text-sm text-slate-600 dark:text-slate-400">{{ user.email || '-' }}</span>
                </td>
                <td class="py-3 px-4">
                  <div class="flex flex-wrap gap-1">
                    <span
                      v-for="role in user.roles"
                      :key="role.id"
                      class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400"
                    >
                      {{ role.name }}
                    </span>
                    <span v-if="!user.roles?.length" class="text-xs text-slate-400">No roles</span>
                  </div>
                </td>
                <td class="py-3 px-4">
                  <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400">
                    <span class="w-1.5 h-1.5 rounded-full bg-green-500"></span>
                    Active
                  </span>
                </td>
                <td class="py-3 px-4 text-right">
                  <button
                    @click.stop="viewUser(user)"
                    class="p-2 text-slate-400 hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"
                    title="View details"
                  >
                    <span class="material-symbols-outlined text-lg">visibility</span>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- GRID VIEW - Compact Cards -->
        <div v-else class="p-4">
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            <div
              v-for="(user, index) in filteredUsers"
              :key="user.id"
              @click="viewUser(user)"
              class="bg-slate-50 dark:bg-slate-800 rounded-xl p-4 hover:shadow-md hover:ring-1 hover:ring-primary/30 transition-all cursor-pointer group"
            >
              <div class="flex items-start gap-3">
                <div
                  :class="[
                    'w-12 h-12 rounded-xl flex items-center justify-center text-white text-lg font-semibold flex-shrink-0',
                    getAvatarColor(index)
                  ]"
                >
                  {{ getInitials(user) }}
                </div>
                <div class="flex-1 min-w-0">
                  <h3 class="font-semibold text-slate-900 dark:text-white truncate">
                    {{ getFullName(user) }}
                  </h3>
                  <p class="text-sm text-slate-500 dark:text-slate-400 truncate">
                    @{{ user.username }}
                  </p>
                </div>
                <span class="w-2 h-2 rounded-full bg-green-500 mt-2 flex-shrink-0" title="Active"></span>
              </div>

              <div class="mt-3 pt-3 border-t border-slate-200 dark:border-slate-700 space-y-2">
                <div class="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
                  <span class="material-symbols-outlined text-base">mail</span>
                  <span class="truncate">{{ user.email || 'No email' }}</span>
                </div>
                <div class="flex flex-wrap gap-1">
                  <span
                    v-for="role in user.roles"
                    :key="role.id"
                    class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400"
                  >
                    {{ role.name }}
                  </span>
                  <span v-if="!user.roles?.length" class="text-xs text-slate-400 italic">No roles</span>
                </div>
              </div>

              <button
                class="w-full mt-3 py-2 text-sm font-medium text-slate-500 dark:text-slate-400 bg-white dark:bg-slate-900 rounded-lg group-hover:bg-primary group-hover:text-white transition-colors flex items-center justify-center gap-1.5"
              >
                <span class="material-symbols-outlined text-base">visibility</span>
                View
              </button>
            </div>
          </div>
        </div>

        <!-- Footer with count -->
        <div class="px-4 py-3 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800">
          <p class="text-sm text-slate-500 dark:text-slate-400">
            Showing <span class="font-medium text-slate-700 dark:text-slate-300">{{ filteredUsers.length }}</span>
            of <span class="font-medium text-slate-700 dark:text-slate-300">{{ users.length }}</span> users
          </p>
        </div>
      </div>
    </div>

    <!-- User Details Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="duration-300 ease-out"
        enter-from-class="opacity-0"
        leave-active-class="duration-200 ease-in"
        leave-to-class="opacity-0"
      >
        <div v-if="showUserModal && selectedUser" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] flex flex-col overflow-hidden">
            <!-- Modal Header -->
            <div class="relative bg-gradient-to-r from-navy to-primary p-6">
              <div class="relative text-center">
                <div class="w-16 h-16 bg-white/20 backdrop-blur rounded-full flex items-center justify-center text-white text-xl font-bold mx-auto">
                  {{ getInitials(selectedUser) }}
                </div>
                <h3 class="text-xl font-bold text-white mt-4">{{ getFullName(selectedUser) }}</h3>
                <p class="text-white/70">{{ selectedUser.email || 'No email' }}</p>
              </div>

              <button
                @click="showUserModal = false"
                class="absolute top-4 right-4 p-2 bg-white/10 hover:bg-white/20 rounded-xl transition-colors"
              >
                <span class="material-symbols-outlined text-white">close</span>
              </button>
            </div>

            <!-- Modal Body -->
            <div class="p-6 overflow-y-auto flex-1">
              <!-- User Info -->
              <div class="space-y-4">
                <div class="flex items-center justify-between py-3 border-b border-slate-100 dark:border-slate-700">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center">
                      <span class="material-symbols-outlined text-blue-600 dark:text-blue-400">badge</span>
                    </div>
                    <span class="text-slate-500 dark:text-slate-400">Username</span>
                  </div>
                  <span class="font-medium text-slate-900 dark:text-white font-mono">{{ selectedUser.username }}</span>
                </div>

                <div class="flex items-center justify-between py-3 border-b border-slate-100 dark:border-slate-700">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl bg-purple-100 dark:bg-purple-900/30 flex items-center justify-center">
                      <span class="material-symbols-outlined text-purple-600 dark:text-purple-400">person</span>
                    </div>
                    <span class="text-slate-500 dark:text-slate-400">First Name</span>
                  </div>
                  <span class="font-medium text-slate-900 dark:text-white">{{ selectedUser.firstName || '-' }}</span>
                </div>

                <div class="flex items-center justify-between py-3 border-b border-slate-100 dark:border-slate-700">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl bg-teal-100 dark:bg-teal-900/30 flex items-center justify-center">
                      <span class="material-symbols-outlined text-teal-600 dark:text-teal-400">person</span>
                    </div>
                    <span class="text-slate-500 dark:text-slate-400">Last Name</span>
                  </div>
                  <span class="font-medium text-slate-900 dark:text-white">{{ selectedUser.lastName || '-' }}</span>
                </div>

                <div class="flex items-center justify-between py-3">
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center">
                      <span class="material-symbols-outlined text-amber-600 dark:text-amber-400">mail</span>
                    </div>
                    <span class="text-slate-500 dark:text-slate-400">Email</span>
                  </div>
                  <span class="font-medium text-slate-900 dark:text-white">{{ selectedUser.email || '-' }}</span>
                </div>
              </div>

              <!-- User Roles -->
              <div class="mt-6">
                <h4 class="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-3">Assigned Roles</h4>
                <div v-if="isLoadingDetails" class="flex items-center justify-center py-4">
                  <span class="material-symbols-outlined animate-spin text-primary">progress_activity</span>
                </div>
                <template v-else>
                  <div v-if="userRoles.length === 0" class="text-center py-4 bg-slate-50 dark:bg-slate-800 rounded-xl mb-3">
                    <span class="material-symbols-outlined text-3xl text-slate-300 dark:text-slate-600">admin_panel_settings</span>
                    <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">No roles assigned</p>
                  </div>
                  <div v-else class="space-y-2 mb-3">
                    <div
                      v-for="role in userRoles"
                      :key="role.id"
                      class="flex items-center gap-3 p-3 bg-slate-50 dark:bg-slate-800 rounded-xl group"
                    >
                      <div class="w-10 h-10 rounded-xl bg-gradient-to-r from-purple-500 to-indigo-600 flex items-center justify-center">
                        <span class="material-symbols-outlined text-white text-lg">shield_person</span>
                      </div>
                      <div class="flex-1 min-w-0">
                        <div class="font-medium text-slate-900 dark:text-white truncate">{{ role.name }}</div>
                        <div class="text-sm text-slate-500 dark:text-slate-400">{{ role.description || 'System role' }}</div>
                      </div>
                      <button
                        @click="removeRoleFromUser(role.id)"
                        :disabled="isRemovingRole === role.id"
                        class="p-2 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors opacity-0 group-hover:opacity-100"
                        title="Remove role"
                      >
                        <span v-if="isRemovingRole === role.id" class="material-symbols-outlined animate-spin text-lg">progress_activity</span>
                        <span v-else class="material-symbols-outlined text-lg">close</span>
                      </button>
                    </div>
                  </div>

                  <!-- Add Role -->
                  <div v-if="availableRolesToAssign.length > 0" class="flex items-stretch gap-2">
                    <div class="flex-1">
                      <Select
                        v-model="selectedRoleToAdd"
                        :options="roleOptions"
                        placeholder="Select a role to add..."
                        size="md"
                      />
                    </div>
                    <button
                      @click="assignRoleToUser"
                      :disabled="!selectedRoleToAdd || isAssigningRole"
                      class="px-4 bg-primary text-white rounded-xl text-sm font-medium hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-1.5 whitespace-nowrap"
                    >
                      <span v-if="isAssigningRole" class="material-symbols-outlined animate-spin text-base">progress_activity</span>
                      <span v-else class="material-symbols-outlined text-base">add</span>
                      Add
                    </button>
                  </div>
                </template>
              </div>

              <!-- User Structures -->
              <div class="mt-6">
                <h4 class="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-3">Organizational Structures</h4>
                <div v-if="isLoadingDetails" class="flex items-center justify-center py-4">
                  <span class="material-symbols-outlined animate-spin text-primary">progress_activity</span>
                </div>
                <div v-else-if="userStructures.length === 0" class="text-center py-4 bg-slate-50 dark:bg-slate-800 rounded-xl">
                  <span class="material-symbols-outlined text-3xl text-slate-300 dark:text-slate-600">apartment</span>
                  <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">No structures assigned</p>
                </div>
                <div v-else class="space-y-2">
                  <div
                    v-for="structure in userStructures"
                    :key="structure.id"
                    class="flex items-center gap-3 p-3 bg-slate-50 dark:bg-slate-800 rounded-xl"
                  >
                    <div class="w-10 h-10 rounded-xl bg-gradient-to-r from-primary to-teal-600 flex items-center justify-center">
                      <span class="material-symbols-outlined text-white text-lg">{{ getTypeIcon(structure.type) }}</span>
                    </div>
                    <div class="flex-1 min-w-0">
                      <div class="font-medium text-slate-900 dark:text-white truncate">{{ structure.name }}</div>
                      <div class="text-sm text-slate-500 dark:text-slate-400">{{ structure.type }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="border-t border-slate-100 dark:border-slate-700 p-4 bg-slate-50 dark:bg-slate-800/50">
              <button
                @click="showUserModal = false"
                class="w-full px-5 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-xl font-medium hover:shadow-lg hover:shadow-primary/25 transition-all"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
