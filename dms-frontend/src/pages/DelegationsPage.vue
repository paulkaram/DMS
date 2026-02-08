<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { delegationsApi, usersApi, rolesApi } from '@/api/client'
import type { Delegation, User, Role } from '@/types'
import { UiSelect, UiDatePicker } from '@/components/ui'

const myDelegations = ref<Delegation[]>([])
const delegationsToMe = ref<Delegation[]>([])
const users = ref<User[]>([])
const roles = ref<Role[]>([])
const isLoading = ref(true)
const activeTab = ref<'my' | 'to-me'>('my')

const showCreateModal = ref(false)
const newDelegation = ref({
  toUserId: '',
  roleId: '',
  startDate: new Date().toISOString().split('T')[0],
  endDate: ''
})
const isSaving = ref(false)
const error = ref('')

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [myResponse, toMeResponse, usersResponse, rolesResponse] = await Promise.all([
      delegationsApi.getMyDelegations(),
      delegationsApi.getDelegationsToMe(),
      usersApi.getAll(),
      rolesApi.getAll()
    ])
    myDelegations.value = myResponse.data
    delegationsToMe.value = toMeResponse.data
    users.value = usersResponse.data
    roles.value = rolesResponse.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

async function createDelegation() {
  if (!newDelegation.value.toUserId) {
    error.value = 'Please select a user'
    return
  }

  isSaving.value = true
  error.value = ''

  try {
    await delegationsApi.create({
      toUserId: newDelegation.value.toUserId,
      roleId: newDelegation.value.roleId || undefined,
      startDate: newDelegation.value.startDate,
      endDate: newDelegation.value.endDate || undefined
    })
    showCreateModal.value = false
    resetForm()
    await loadData()
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to create delegation'
  } finally {
    isSaving.value = false
  }
}

async function deleteDelegation(id: string) {
  if (!confirm('Are you sure you want to delete this delegation?')) return

  try {
    await delegationsApi.delete(id)
    await loadData()
  } catch (err) {
  }
}

function resetForm() {
  newDelegation.value = {
    toUserId: '',
    roleId: '',
    startDate: new Date().toISOString().split('T')[0],
    endDate: ''
  }
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

function getStatusColor(delegation: Delegation) {
  if (!delegation.isActive) return 'bg-gray-100 text-gray-600'
  const now = new Date()
  const start = new Date(delegation.startDate)
  const end = delegation.endDate ? new Date(delegation.endDate) : null

  if (now < start) return 'bg-yellow-100 text-yellow-700'
  if (end && now > end) return 'bg-gray-100 text-gray-600'
  return 'bg-green-100 text-green-700'
}

function getStatusText(delegation: Delegation) {
  if (!delegation.isActive) return 'Inactive'
  const now = new Date()
  const start = new Date(delegation.startDate)
  const end = delegation.endDate ? new Date(delegation.endDate) : null

  if (now < start) return 'Pending'
  if (end && now > end) return 'Expired'
  return 'Active'
}

function getUserName(userId: string) {
  const user = users.value.find(u => u.id === userId)
  return user?.displayName || user?.username || userId
}

const userOptions = computed(() => [
  { value: '', label: 'Select a user' },
  ...users.value.map(user => ({
    value: user.id,
    label: user.displayName || user.username
  }))
])

const roleOptions = computed(() => [
  { value: '', label: 'All roles' },
  ...roles.value.map(role => ({
    value: role.id,
    label: role.name
  }))
])
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Delegations</h1>
        <p class="text-zinc-500 mt-1">Manage your permission delegations</p>
      </div>
      <button
        @click="showCreateModal = true"
        class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        New Delegation
      </button>
    </div>

    <!-- Tabs -->
    <div class="flex gap-4 mb-6">
      <button
        @click="activeTab = 'my'"
        :class="[
          'px-4 py-2 rounded-lg font-medium transition-colors',
          activeTab === 'my' ? 'bg-teal text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
        ]"
      >
        My Delegations ({{ myDelegations.length }})
      </button>
      <button
        @click="activeTab = 'to-me'"
        :class="[
          'px-4 py-2 rounded-lg font-medium transition-colors',
          activeTab === 'to-me' ? 'bg-teal text-white' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
        ]"
      >
        Delegated to Me ({{ delegationsToMe.length }})
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12">
      <div class="flex items-center justify-center gap-3">
        <svg class="animate-spin w-6 h-6 text-blue-600" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
        </svg>
        <span class="text-gray-600">Loading delegations...</span>
      </div>
    </div>

    <!-- My Delegations Tab -->
    <div v-else-if="activeTab === 'my'">
      <div v-if="myDelegations.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
        <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
        </svg>
        <p class="text-gray-500 mb-4">You haven't created any delegations yet</p>
        <button
          @click="showCreateModal = true"
          class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
        >
          Create Delegation
        </button>
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="delegation in myDelegations"
          :key="delegation.id"
          class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"
        >
          <div class="flex items-start justify-between">
            <div class="flex items-center gap-4">
              <div class="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
                <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              </div>
              <div>
                <p class="font-medium text-gray-900">Delegated to: {{ delegation.toUserName || getUserName(delegation.toUserId) }}</p>
                <p v-if="delegation.roleName" class="text-sm text-gray-500">Role: {{ delegation.roleName }}</p>
              </div>
            </div>
            <div class="flex items-center gap-3">
              <span :class="['px-3 py-1 rounded-full text-xs font-medium', getStatusColor(delegation)]">
                {{ getStatusText(delegation) }}
              </span>
              <button
                @click="deleteDelegation(delegation.id)"
                class="p-2 text-red-500 hover:bg-red-50 rounded-lg transition-colors"
                title="Delete delegation"
              >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
          </div>
          <div class="mt-4 pt-4 border-t border-gray-100 flex gap-6 text-sm text-gray-500">
            <div>
              <span class="text-gray-400">Start Date:</span>
              <span class="ml-1 text-gray-700">{{ formatDate(delegation.startDate) }}</span>
            </div>
            <div v-if="delegation.endDate">
              <span class="text-gray-400">End Date:</span>
              <span class="ml-1 text-gray-700">{{ formatDate(delegation.endDate) }}</span>
            </div>
            <div v-else>
              <span class="text-gray-400">End Date:</span>
              <span class="ml-1 text-gray-700">No expiration</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Delegations To Me Tab -->
    <div v-else>
      <div v-if="delegationsToMe.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
        <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
        </svg>
        <p class="text-gray-500">No one has delegated permissions to you</p>
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="delegation in delegationsToMe"
          :key="delegation.id"
          class="bg-white rounded-xl shadow-sm border border-gray-100 p-6"
        >
          <div class="flex items-start justify-between">
            <div class="flex items-center gap-4">
              <div class="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center">
                <svg class="w-6 h-6 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                </svg>
              </div>
              <div>
                <p class="font-medium text-gray-900">From: {{ delegation.fromUserName || getUserName(delegation.fromUserId) }}</p>
                <p v-if="delegation.roleName" class="text-sm text-gray-500">Role: {{ delegation.roleName }}</p>
              </div>
            </div>
            <span :class="['px-3 py-1 rounded-full text-xs font-medium', getStatusColor(delegation)]">
              {{ getStatusText(delegation) }}
            </span>
          </div>
          <div class="mt-4 pt-4 border-t border-gray-100 flex gap-6 text-sm text-gray-500">
            <div>
              <span class="text-gray-400">Start Date:</span>
              <span class="ml-1 text-gray-700">{{ formatDate(delegation.startDate) }}</span>
            </div>
            <div v-if="delegation.endDate">
              <span class="text-gray-400">End Date:</span>
              <span class="ml-1 text-gray-700">{{ formatDate(delegation.endDate) }}</span>
            </div>
            <div v-else>
              <span class="text-gray-400">End Date:</span>
              <span class="ml-1 text-gray-700">No expiration</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Delegation Modal -->
    <div v-if="showCreateModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4">
        <div class="p-6">
          <div class="flex items-center justify-between mb-6">
            <h3 class="text-lg font-semibold text-gray-900">Create Delegation</h3>
            <button @click="showCreateModal = false; resetForm()" class="text-gray-400 hover:text-gray-600">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <div class="space-y-4">
            <UiSelect
              v-model="newDelegation.toUserId"
              :options="userOptions"
              label="Delegate To *"
              placeholder="Select a user"
              searchable
            />

            <UiSelect
              v-model="newDelegation.roleId"
              :options="roleOptions"
              label="Role (Optional)"
              placeholder="All roles"
              clearable
            />

            <UiDatePicker
              v-model="newDelegation.startDate"
              label="Start Date *"
              placeholder="Select start date"
              :clearable="false"
            />

            <UiDatePicker
              v-model="newDelegation.endDate"
              label="End Date (Optional)"
              placeholder="Select end date"
            />

            <div v-if="error" class="bg-red-50 border border-red-200 rounded-lg p-3 text-red-600 text-sm">
              {{ error }}
            </div>
          </div>

          <div class="mt-6 flex justify-end gap-3">
            <button
              @click="showCreateModal = false; resetForm()"
              class="px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              @click="createDelegation"
              :disabled="isSaving"
              class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50"
            >
              {{ isSaving ? 'Creating...' : 'Create Delegation' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
