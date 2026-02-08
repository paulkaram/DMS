<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import type { Vacation, User } from '@/types'
import { vacationsApi, usersApi } from '@/api/client'
import { UiSelect, UiDatePicker, UiCheckbox } from '@/components/ui'

const vacations = ref<Vacation[]>([])
const users = ref<User[]>([])
const isLoading = ref(true)
const showCreateModal = ref(false)
const editingVacation = ref<Vacation | null>(null)

const formData = ref({
  delegateToUserId: '',
  startDate: '',
  endDate: '',
  message: '',
  autoReply: false
})

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [vacationsRes, usersRes] = await Promise.all([
      vacationsApi.getMyVacations(),
      usersApi.getAll()
    ])
    vacations.value = vacationsRes.data
    users.value = usersRes.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  editingVacation.value = null
  formData.value = {
    delegateToUserId: '',
    startDate: '',
    endDate: '',
    message: '',
    autoReply: false
  }
  showCreateModal.value = true
}

function editVacation(vacation: Vacation) {
  editingVacation.value = vacation
  formData.value = {
    delegateToUserId: vacation.delegateToUserId || '',
    startDate: vacation.startDate.split('T')[0],
    endDate: vacation.endDate.split('T')[0],
    message: vacation.message || '',
    autoReply: vacation.autoReply
  }
  showCreateModal.value = true
}

async function saveVacation() {
  try {
    const data = {
      delegateToUserId: formData.value.delegateToUserId || undefined,
      startDate: formData.value.startDate,
      endDate: formData.value.endDate,
      message: formData.value.message || undefined,
      autoReply: formData.value.autoReply
    }

    if (editingVacation.value) {
      await vacationsApi.update(editingVacation.value.id, {
        ...data,
        isActive: editingVacation.value.isActive
      })
    } else {
      await vacationsApi.create(data)
    }

    showCreateModal.value = false
    await loadData()
  } catch (err) {
  }
}

async function cancelVacation(vacation: Vacation) {
  if (!confirm('Cancel this vacation schedule?')) return

  try {
    await vacationsApi.cancel(vacation.id)
    await loadData()
  } catch (err) {
  }
}

async function deleteVacation(vacation: Vacation) {
  if (!confirm('Delete this vacation record?')) return

  try {
    await vacationsApi.delete(vacation.id)
    vacations.value = vacations.value.filter(v => v.id !== vacation.id)
  } catch (err) {
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

const activeVacation = computed(() => {
  const now = new Date()
  return vacations.value.find(v => {
    const start = new Date(v.startDate)
    const end = new Date(v.endDate)
    return v.isActive && start <= now && end >= now
  })
})

const upcomingVacations = computed(() => {
  const now = new Date()
  return vacations.value.filter(v => {
    const start = new Date(v.startDate)
    return v.isActive && start > now
  })
})

const pastVacations = computed(() => {
  const now = new Date()
  return vacations.value.filter(v => {
    const end = new Date(v.endDate)
    return end < now || !v.isActive
  })
})

const userOptions = computed(() => [
  { value: '', label: 'No delegation' },
  ...users.value.map(user => ({
    value: user.id,
    label: user.displayName || user.username
  }))
])
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Vacation Settings</h1>
        <p class="text-zinc-500 mt-1">Manage your out-of-office schedule and delegation</p>
      </div>
      <button
        @click="openCreateModal"
        class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors flex items-center gap-2"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Schedule Vacation
      </button>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <svg class="w-8 h-8 animate-spin text-blue-600" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
      </svg>
    </div>

    <div v-else>
      <!-- Active Vacation Alert -->
      <div v-if="activeVacation" class="bg-amber-50 border border-amber-200 rounded-xl p-4 mb-6">
        <div class="flex items-start gap-3">
          <svg class="w-6 h-6 text-amber-500 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <div class="flex-1">
            <h3 class="font-medium text-amber-800">Currently on vacation</h3>
            <p class="text-sm text-amber-700 mt-1">
              {{ formatDate(activeVacation.startDate) }} - {{ formatDate(activeVacation.endDate) }}
              <span v-if="activeVacation.delegateToUserName">
                (Delegated to {{ activeVacation.delegateToUserName }})
              </span>
            </p>
          </div>
          <button
            @click="cancelVacation(activeVacation)"
            class="px-3 py-1.5 bg-amber-100 text-amber-700 rounded-lg hover:bg-amber-200 text-sm"
          >
            End Now
          </button>
        </div>
      </div>

      <!-- Upcoming Vacations -->
      <div v-if="upcomingVacations.length > 0" class="mb-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-3">Upcoming</h2>
        <div class="space-y-3">
          <div
            v-for="vacation in upcomingVacations"
            :key="vacation.id"
            class="bg-white rounded-xl shadow-sm border border-gray-100 p-4"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-4">
                <div class="w-12 h-12 bg-blue-50 rounded-lg flex items-center justify-center">
                  <svg class="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                  </svg>
                </div>
                <div>
                  <p class="font-medium text-gray-900">
                    {{ formatDate(vacation.startDate) }} - {{ formatDate(vacation.endDate) }}
                  </p>
                  <p v-if="vacation.delegateToUserName" class="text-sm text-gray-500">
                    Delegated to {{ vacation.delegateToUserName }}
                  </p>
                  <p v-if="vacation.message" class="text-sm text-gray-500 mt-1">
                    "{{ vacation.message }}"
                  </p>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <button
                  @click="editVacation(vacation)"
                  class="p-2 text-gray-400 hover:text-teal"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button
                  @click="cancelVacation(vacation)"
                  class="p-2 text-gray-400 hover:text-red-600"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Past Vacations -->
      <div v-if="pastVacations.length > 0">
        <h2 class="text-lg font-semibold text-gray-900 mb-3">Past</h2>
        <div class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Period</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Delegated To</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200">
              <tr v-for="vacation in pastVacations" :key="vacation.id">
                <td class="px-6 py-4 text-sm text-gray-900">
                  {{ formatDate(vacation.startDate) }} - {{ formatDate(vacation.endDate) }}
                </td>
                <td class="px-6 py-4 text-sm text-gray-500">
                  {{ vacation.delegateToUserName || '-' }}
                </td>
                <td class="px-6 py-4">
                  <span :class="[
                    'px-2.5 py-1 rounded-full text-xs font-medium',
                    vacation.isActive ? 'bg-gray-100 text-gray-600' : 'bg-red-100 text-red-600'
                  ]">
                    {{ vacation.isActive ? 'Completed' : 'Cancelled' }}
                  </span>
                </td>
                <td class="px-6 py-4 text-right">
                  <button
                    @click="deleteVacation(vacation)"
                    class="text-sm text-red-600 hover:text-red-700"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Empty State -->
      <div v-if="vacations.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
        <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        <p class="text-gray-500 text-lg">No vacation scheduled</p>
        <p class="text-gray-400 mt-1">Schedule your vacation to delegate work automatically</p>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="showCreateModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">
          {{ editingVacation ? 'Edit Vacation' : 'Schedule Vacation' }}
        </h2>

        <form @submit.prevent="saveVacation" class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <UiDatePicker
              v-model="formData.startDate"
              label="Start Date"
              placeholder="Select start date"
              :clearable="false"
            />
            <UiDatePicker
              v-model="formData.endDate"
              label="End Date"
              placeholder="Select end date"
              :clearable="false"
            />
          </div>

          <UiSelect
            v-model="formData.delegateToUserId"
            :options="userOptions"
            label="Delegate To (Optional)"
            placeholder="No delegation"
            clearable
            searchable
          />

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Out of Office Message</label>
            <textarea
              v-model="formData.message"
              rows="3"
              placeholder="I'm currently out of the office..."
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
            ></textarea>
          </div>

          <UiCheckbox
            v-model="formData.autoReply"
            label="Enable auto-reply"
          />

          <div class="flex justify-end gap-3 pt-4">
            <button
              type="button"
              @click="showCreateModal = false"
              class="px-4 py-2 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              type="submit"
              class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90"
            >
              {{ editingVacation ? 'Save Changes' : 'Schedule' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
