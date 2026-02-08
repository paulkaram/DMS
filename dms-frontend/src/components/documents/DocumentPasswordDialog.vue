<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { documentPasswordsApi } from '@/api/client'
import { UiDatePicker } from '@/components/ui'

const props = defineProps<{
  documentId: string
  documentName: string
}>()

const emit = defineEmits<{
  close: []
  updated: []
}>()

const isLoading = ref(true)
const hasPassword = ref(false)
const hint = ref('')
const expiresAt = ref<string | null>(null)

// Form state
const mode = ref<'view' | 'set' | 'change' | 'remove'>('view')
const newPassword = ref('')
const confirmPassword = ref('')
const currentPassword = ref('')
const newHint = ref('')
const newExpiresAt = ref('')
const isSubmitting = ref(false)
const error = ref('')
const showPassword = ref(false)

onMounted(async () => {
  await checkPasswordStatus()
})

async function checkPasswordStatus() {
  isLoading.value = true
  try {
    const response = await documentPasswordsApi.getStatus(props.documentId)
    hasPassword.value = response.data.hasPassword
    hint.value = response.data.hint || ''
    expiresAt.value = response.data.expiresAt
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

async function setPassword() {
  if (newPassword.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }
  if (newPassword.value.length < 4) {
    error.value = 'Password must be at least 4 characters'
    return
  }

  isSubmitting.value = true
  error.value = ''

  try {
    await documentPasswordsApi.setPassword(props.documentId, {
      password: newPassword.value,
      hint: newHint.value || undefined,
      expiresAt: newExpiresAt.value || undefined
    })
    emit('updated')
    resetForm()
    await checkPasswordStatus()
  } catch (err) {
    error.value = 'Failed to set password'
  } finally {
    isSubmitting.value = false
  }
}

async function changePassword() {
  if (newPassword.value !== confirmPassword.value) {
    error.value = 'Passwords do not match'
    return
  }
  if (newPassword.value.length < 4) {
    error.value = 'Password must be at least 4 characters'
    return
  }

  isSubmitting.value = true
  error.value = ''

  try {
    await documentPasswordsApi.changePassword(props.documentId, {
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
      hint: newHint.value || undefined
    })
    emit('updated')
    resetForm()
    await checkPasswordStatus()
  } catch (err) {
    error.value = 'Invalid current password or failed to change password'
  } finally {
    isSubmitting.value = false
  }
}

async function removePassword() {
  if (!confirm('Are you sure you want to remove password protection from this document?')) return

  isSubmitting.value = true
  error.value = ''

  try {
    await documentPasswordsApi.removePassword(props.documentId)
    emit('updated')
    resetForm()
    await checkPasswordStatus()
  } catch (err) {
    error.value = 'Failed to remove password'
  } finally {
    isSubmitting.value = false
  }
}

function resetForm() {
  mode.value = 'view'
  newPassword.value = ''
  confirmPassword.value = ''
  currentPassword.value = ''
  newHint.value = ''
  newExpiresAt.value = ''
  error.value = ''
  showPassword.value = false
}

function formatDate(dateString: string | null) {
  if (!dateString) return 'Never'
  return new Date(dateString).toLocaleDateString()
}

const passwordMatch = computed(() => {
  return newPassword.value === confirmPassword.value
})

const canSubmit = computed(() => {
  if (mode.value === 'set') {
    return newPassword.value.length >= 4 && passwordMatch.value
  }
  if (mode.value === 'change') {
    return currentPassword.value && newPassword.value.length >= 4 && passwordMatch.value
  }
  return false
})
</script>

<template>
  <div class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50" @click.self="emit('close')">
    <div class="bg-white dark:bg-zinc-900 rounded-2xl shadow-2xl ring-1 ring-black/5 dark:ring-white/10 w-full max-w-md mx-4 overflow-hidden">
      <!-- Header with brand gradient -->
      <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary px-6 py-5 overflow-hidden">
        <!-- Decorative elements -->
        <div class="absolute top-0 right-0 w-32 h-32 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
        <div class="absolute bottom-0 left-0 w-20 h-20 bg-primary/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>

        <div class="relative flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-xl flex items-center justify-center">
              <span class="material-symbols-outlined text-white text-xl">lock</span>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-white">Password Protection</h2>
              <p class="text-sm text-white/70 truncate max-w-[200px]">{{ documentName }}</p>
            </div>
          </div>
          <button
            @click="emit('close')"
            class="w-9 h-9 flex items-center justify-center rounded-xl bg-white/10 hover:bg-white/20 transition-colors"
          >
            <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Content -->
      <div class="p-6">
        <!-- Loading State -->
        <div v-if="isLoading" class="flex items-center justify-center py-8">
          <div class="animate-spin w-8 h-8 border-3 border-teal border-t-transparent rounded-full"></div>
        </div>

        <!-- View Mode -->
        <template v-else-if="mode === 'view'">
          <div class="text-center py-4">
            <div
              class="w-16 h-16 rounded-2xl mx-auto mb-4 flex items-center justify-center"
              :class="hasPassword ? 'bg-teal/10' : 'bg-zinc-100 dark:bg-zinc-800'"
            >
              <span
                class="material-symbols-outlined text-3xl"
                :class="hasPassword ? 'text-teal' : 'text-zinc-400'"
              >
                {{ hasPassword ? 'lock' : 'lock_open' }}
              </span>
            </div>
            <h3 class="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mb-1">
              {{ hasPassword ? 'Password Protected' : 'Not Protected' }}
            </h3>
            <p class="text-sm text-zinc-500 mb-4">
              {{ hasPassword ? 'This document requires a password to access' : 'Anyone with access can view this document' }}
            </p>

            <div v-if="hasPassword" class="text-left bg-teal/5 dark:bg-teal/10 border border-teal/20 rounded-xl p-4 mb-4 space-y-2">
              <div v-if="hint" class="flex items-center justify-between">
                <span class="text-sm text-zinc-500 dark:text-zinc-400">Hint:</span>
                <span class="text-sm text-zinc-700 dark:text-zinc-300">{{ hint }}</span>
              </div>
              <div class="flex items-center justify-between">
                <span class="text-sm text-zinc-500 dark:text-zinc-400">Expires:</span>
                <span class="text-sm text-zinc-700 dark:text-zinc-300">{{ formatDate(expiresAt) }}</span>
              </div>
            </div>

            <div class="flex flex-col gap-2">
              <button
                v-if="!hasPassword"
                @click="mode = 'set'"
                class="w-full py-2.5 bg-gradient-to-r from-teal to-teal/90 text-white font-medium rounded-xl hover:from-teal/90 hover:to-teal/80 shadow-lg shadow-teal/25 transition-all"
              >
                Set Password
              </button>
              <button
                v-if="hasPassword"
                @click="mode = 'change'"
                class="w-full py-2.5 bg-gradient-to-r from-teal to-teal/90 text-white font-medium rounded-xl hover:from-teal/90 hover:to-teal/80 shadow-lg shadow-teal/25 transition-all"
              >
                Change Password
              </button>
              <button
                v-if="hasPassword"
                @click="removePassword"
                class="w-full py-2.5 text-red-500 font-medium rounded-xl border border-red-200 dark:border-red-800 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
              >
                Remove Password
              </button>
            </div>
          </div>
        </template>

        <!-- Set Password Form -->
        <template v-else-if="mode === 'set'">
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">New Password</label>
              <div class="relative">
                <input
                  v-model="newPassword"
                  :type="showPassword ? 'text' : 'password'"
                  class="w-full px-4 py-2.5 pr-10 border border-zinc-200 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Enter password..."
                />
                <button
                  type="button"
                  @click="showPassword = !showPassword"
                  class="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-600"
                >
                  <span class="material-symbols-outlined text-lg">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
                </button>
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Confirm Password</label>
              <input
                v-model="confirmPassword"
                :type="showPassword ? 'text' : 'password'"
                class="w-full px-4 py-2.5 border border-zinc-200 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
                :class="{ 'border-red-500': confirmPassword && !passwordMatch }"
                placeholder="Confirm password..."
              />
              <p v-if="confirmPassword && !passwordMatch" class="text-xs text-red-500 mt-1">Passwords do not match</p>
            </div>

            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Hint (optional)</label>
              <input
                v-model="newHint"
                type="text"
                class="w-full px-4 py-2.5 border border-zinc-200 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
                placeholder="Password hint..."
              />
            </div>

            <UiDatePicker
              v-model="newExpiresAt"
              label="Expires (optional)"
              placeholder="Select expiration date"
              :min-date="new Date().toISOString().split('T')[0]"
            />

            <p v-if="error" class="text-sm text-red-500 bg-red-50 dark:bg-red-900/20 px-4 py-2 rounded-lg">{{ error }}</p>

            <div class="flex gap-3 pt-2">
              <button
                @click="resetForm"
                class="flex-1 py-2.5 text-zinc-600 dark:text-zinc-400 font-medium rounded-xl hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors"
              >
                Cancel
              </button>
              <button
                @click="setPassword"
                :disabled="!canSubmit || isSubmitting"
                class="flex-1 py-2.5 bg-gradient-to-r from-teal to-teal/90 text-white font-medium rounded-xl hover:from-teal/90 hover:to-teal/80 shadow-lg shadow-teal/25 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
              >
                {{ isSubmitting ? 'Setting...' : 'Set Password' }}
              </button>
            </div>
          </div>
        </template>

        <!-- Change Password Form -->
        <template v-else-if="mode === 'change'">
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Current Password</label>
              <input
                v-model="currentPassword"
                :type="showPassword ? 'text' : 'password'"
                class="w-full px-4 py-2.5 border border-zinc-200 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
                placeholder="Enter current password..."
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">New Password</label>
              <div class="relative">
                <input
                  v-model="newPassword"
                  :type="showPassword ? 'text' : 'password'"
                  class="w-full px-4 py-2.5 pr-10 border border-zinc-200 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Enter new password..."
                />
                <button
                  type="button"
                  @click="showPassword = !showPassword"
                  class="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-600"
                >
                  <span class="material-symbols-outlined text-lg">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
                </button>
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Confirm New Password</label>
              <input
                v-model="confirmPassword"
                :type="showPassword ? 'text' : 'password'"
                class="w-full px-4 py-2.5 border border-zinc-200 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
                :class="{ 'border-red-500': confirmPassword && !passwordMatch }"
                placeholder="Confirm new password..."
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">New Hint (optional)</label>
              <input
                v-model="newHint"
                type="text"
                class="w-full px-4 py-2.5 border border-zinc-200 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
                placeholder="Password hint..."
              />
            </div>

            <p v-if="error" class="text-sm text-red-500 bg-red-50 dark:bg-red-900/20 px-4 py-2 rounded-lg">{{ error }}</p>

            <div class="flex gap-3 pt-2">
              <button
                @click="resetForm"
                class="flex-1 py-2.5 text-zinc-600 dark:text-zinc-400 font-medium rounded-xl hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors"
              >
                Cancel
              </button>
              <button
                @click="changePassword"
                :disabled="!canSubmit || isSubmitting"
                class="flex-1 py-2.5 bg-gradient-to-r from-teal to-teal/90 text-white font-medium rounded-xl hover:from-teal/90 hover:to-teal/80 shadow-lg shadow-teal/25 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
              >
                {{ isSubmitting ? 'Changing...' : 'Change Password' }}
              </button>
            </div>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>
