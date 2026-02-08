<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { usersApi, rolesApi } from '@/api/client'
import type { User, Role } from '@/types'

const users = ref<User[]>([])
const roles = ref<Role[]>([])
const searchQuery = ref('')
const isLoading = ref(true)
const selectedUser = ref<User | null>(null)
const showUserModal = ref(false)

const filteredUsers = computed(() => {
  if (!searchQuery.value) return users.value
  const query = searchQuery.value.toLowerCase()
  return users.value.filter(u =>
    u.username?.toLowerCase().includes(query) ||
    u.displayName?.toLowerCase().includes(query) ||
    u.email?.toLowerCase().includes(query)
  )
})

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [usersResponse, rolesResponse] = await Promise.all([
      usersApi.getAll(),
      rolesApi.getAll()
    ])
    users.value = usersResponse.data
    roles.value = rolesResponse.data
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

function viewUser(user: User) {
  selectedUser.value = user
  showUserModal.value = true
}

function getInitials(user: User) {
  if (user.firstName && user.lastName) {
    return (user.firstName[0] + user.lastName[0]).toUpperCase()
  }
  return user.username?.substring(0, 2).toUpperCase() || '??'
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-slate-100">Users</h1>
        <p class="text-slate-500 mt-1">Manage system users</p>
      </div>
    </div>

    <!-- Search -->
    <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-4 mb-6">
      <div class="flex gap-4">
        <div class="flex-1 relative">
          <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search users by name, username, or email..."
            class="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-transparent"
            @keyup.enter="handleSearch"
          />
        </div>
        <button
          @click="handleSearch"
          class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
        >
          Search
        </button>
      </div>
    </div>

    <!-- Users Grid -->
    <div v-if="isLoading" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div v-for="i in 6" :key="i" class="bg-white rounded-xl shadow-sm border border-gray-100 p-6 animate-pulse">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 bg-gray-200 rounded-full"></div>
          <div class="flex-1">
            <div class="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
            <div class="h-3 bg-gray-200 rounded w-1/2"></div>
          </div>
        </div>
      </div>
    </div>

    <div v-else-if="filteredUsers.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
      <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
      </svg>
      <p class="text-gray-500">No users found</p>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="user in filteredUsers"
        :key="user.id"
        @click="viewUser(user)"
        class="bg-white rounded-xl shadow-sm border border-gray-100 p-6 hover:shadow-md transition-shadow cursor-pointer"
      >
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-cyan-400 rounded-full flex items-center justify-center text-white font-medium">
            {{ getInitials(user) }}
          </div>
          <div class="flex-1 min-w-0">
            <p class="font-medium text-gray-900 truncate">{{ user.displayName || user.username }}</p>
            <p class="text-sm text-gray-500 truncate">{{ user.email }}</p>
          </div>
        </div>
        <div class="mt-4 pt-4 border-t border-gray-100">
          <p class="text-xs text-gray-400">Username</p>
          <p class="text-sm text-gray-600">{{ user.username }}</p>
        </div>
      </div>
    </div>

    <!-- Roles Section -->
    <div class="mt-8">
      <h2 class="text-lg font-semibold text-gray-900 mb-4">Roles</h2>
      <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Role Name</th>
              <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Description</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr v-for="role in roles" :key="role.id" class="hover:bg-gray-50">
              <td class="px-6 py-4 whitespace-nowrap">
                <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                  {{ role.name }}
                </span>
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">{{ role.description || '-' }}</td>
            </tr>
            <tr v-if="roles.length === 0">
              <td colspan="2" class="px-6 py-8 text-center text-gray-500">No roles found</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- User Details Modal -->
    <div v-if="showUserModal && selectedUser" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-6">
            <h3 class="text-lg font-semibold text-gray-900">User Details</h3>
            <button @click="showUserModal = false" class="text-gray-400 hover:text-gray-600">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="text-center mb-6">
            <div class="w-20 h-20 bg-gradient-to-br from-blue-500 to-cyan-400 rounded-full flex items-center justify-center text-white text-2xl font-medium mx-auto mb-4">
              {{ getInitials(selectedUser) }}
            </div>
            <h4 class="text-xl font-semibold text-gray-900">{{ selectedUser.displayName || selectedUser.username }}</h4>
            <p class="text-gray-500">{{ selectedUser.email }}</p>
          </div>

          <div class="space-y-4">
            <div class="flex justify-between py-2 border-b border-gray-100">
              <span class="text-gray-500">Username</span>
              <span class="font-medium text-gray-900">{{ selectedUser.username }}</span>
            </div>
            <div class="flex justify-between py-2 border-b border-gray-100">
              <span class="text-gray-500">First Name</span>
              <span class="font-medium text-gray-900">{{ selectedUser.firstName || '-' }}</span>
            </div>
            <div class="flex justify-between py-2 border-b border-gray-100">
              <span class="text-gray-500">Last Name</span>
              <span class="font-medium text-gray-900">{{ selectedUser.lastName || '-' }}</span>
            </div>
            <div class="flex justify-between py-2">
              <span class="text-gray-500">Email</span>
              <span class="font-medium text-gray-900">{{ selectedUser.email || '-' }}</span>
            </div>
          </div>

          <div class="mt-6 flex justify-end">
            <button
              @click="showUserModal = false"
              class="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
