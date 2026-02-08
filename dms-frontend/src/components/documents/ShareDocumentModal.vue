<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { Document } from '@/types'
import { sharesApi, usersApi } from '@/api/client'

interface User {
  id: string
  username: string
  displayName?: string
  email?: string
}

const props = defineProps<{
  document: Document
}>()

const emit = defineEmits<{
  close: []
  shared: []
}>()

const users = ref<User[]>([])
const selectedUsers = ref<User[]>([])
const permissionLevel = ref(1) // 1 = Read, 2 = Write
const message = ref('')
const isLoading = ref(false)
const isSubmitting = ref(false)
const error = ref('')
const searchQuery = ref('')
const linkCopied = ref(false)
const showUserDropdown = ref(false)
const activeTab = ref<'people' | 'link'>('people')

const shareLink = computed(() => {
  if (typeof window !== 'undefined') {
    return `${window.location.origin}/documents/${props.document.id}`
  }
  return `/documents/${props.document.id}`
})

const permissionOptions = [
  { value: 1, label: 'Can view', icon: 'visibility' },
  { value: 2, label: 'Can edit', icon: 'edit' }
]

onMounted(async () => {
  await loadUsers()
})

async function loadUsers() {
  isLoading.value = true
  try {
    const response = await usersApi.getAll()
    users.value = response.data || []
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

const filteredUsers = computed(() => {
  if (!searchQuery.value) return []
  const query = searchQuery.value.toLowerCase()
  return users.value
    .filter(u =>
      !selectedUsers.value.some(s => s.id === u.id) &&
      (u.username.toLowerCase().includes(query) ||
        u.displayName?.toLowerCase().includes(query) ||
        u.email?.toLowerCase().includes(query))
    )
    .slice(0, 5)
})

function selectUser(user: User) {
  if (!selectedUsers.value.some(u => u.id === user.id)) {
    selectedUsers.value.push(user)
  }
  searchQuery.value = ''
  showUserDropdown.value = false
}

function removeUser(userId: string) {
  selectedUsers.value = selectedUsers.value.filter(u => u.id !== userId)
}

function getInitials(user: User): string {
  const name = user.displayName || user.username
  return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2)
}

function getAvatarColor(userId: string): string {
  const colors = [
    'bg-blue-500', 'bg-green-500', 'bg-purple-500', 'bg-pink-500',
    'bg-orange-500', 'bg-teal-500', 'bg-indigo-500', 'bg-red-500'
  ]
  const index = userId.charCodeAt(0) % colors.length
  return colors[index]
}

async function copyLink() {
  try {
    await navigator.clipboard.writeText(shareLink.value)
    linkCopied.value = true
    setTimeout(() => { linkCopied.value = false }, 2000)
  } catch (err) {
  }
}

async function handleSubmit() {
  if (selectedUsers.value.length === 0) {
    error.value = 'Please select at least one person to share with'
    return
  }

  isSubmitting.value = true
  error.value = ''

  try {
    // Share with all selected users
    for (const user of selectedUsers.value) {
      await sharesApi.share({
        documentId: props.document.id,
        sharedWithUserId: user.id,
        permissionLevel: permissionLevel.value,
        message: message.value || undefined
      })
    }
    emit('shared')
    emit('close')
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to share document'
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-50" @click.self="emit('close')">
    <div class="bg-white dark:bg-zinc-900 rounded-2xl shadow-2xl w-full max-w-md mx-4 overflow-hidden">
      <!-- Header -->
      <div class="px-6 py-5 border-b border-zinc-200 dark:border-zinc-700">
        <div class="flex items-center justify-between">
          <h2 class="text-xl font-semibold text-zinc-900 dark:text-white">Share "{{ document.name }}"</h2>
          <button @click="emit('close')" class="p-1.5 hover:bg-zinc-100 dark:hover:bg-zinc-800 rounded-lg transition-colors">
            <span class="material-symbols-outlined text-zinc-500">close</span>
          </button>
        </div>

        <!-- Tabs -->
        <div class="flex gap-1 mt-4 p-1 bg-zinc-100 dark:bg-zinc-800 rounded-lg">
          <button
            @click="activeTab = 'people'"
            class="flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors"
            :class="activeTab === 'people'
              ? 'bg-white dark:bg-zinc-700 text-zinc-900 dark:text-white shadow-sm'
              : 'text-zinc-600 dark:text-zinc-400 hover:text-zinc-900'"
          >
            <span class="material-symbols-outlined text-lg align-middle mr-1">group</span>
            People
          </button>
          <button
            @click="activeTab = 'link'"
            class="flex-1 py-2 px-4 rounded-md text-sm font-medium transition-colors"
            :class="activeTab === 'link'
              ? 'bg-white dark:bg-zinc-700 text-zinc-900 dark:text-white shadow-sm'
              : 'text-zinc-600 dark:text-zinc-400 hover:text-zinc-900'"
          >
            <span class="material-symbols-outlined text-lg align-middle mr-1">link</span>
            Copy link
          </button>
        </div>
      </div>

      <!-- Content -->
      <div class="p-6">
        <!-- Error -->
        <div v-if="error" class="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
          <p class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
        </div>

        <!-- People Tab -->
        <div v-if="activeTab === 'people'" class="space-y-4">
          <!-- User Search Input -->
          <div class="relative">
            <div class="flex flex-wrap gap-2 p-3 border border-zinc-300 dark:border-zinc-600 rounded-lg bg-white dark:bg-zinc-800 min-h-[48px]">
              <!-- Selected Users Chips -->
              <div
                v-for="user in selectedUsers"
                :key="user.id"
                class="flex items-center gap-1.5 pl-1 pr-2 py-0.5 bg-teal/10 text-teal rounded-full"
              >
                <div :class="[getAvatarColor(user.id), 'w-6 h-6 rounded-full flex items-center justify-center text-white text-xs font-medium']">
                  {{ getInitials(user) }}
                </div>
                <span class="text-sm font-medium">{{ user.displayName || user.username }}</span>
                <button @click="removeUser(user.id)" class="hover:bg-teal/20 rounded-full p-0.5">
                  <span class="material-symbols-outlined text-sm">close</span>
                </button>
              </div>

              <!-- Search Input -->
              <input
                v-model="searchQuery"
                @focus="showUserDropdown = true"
                type="text"
                placeholder="Enter a name or email"
                class="flex-1 min-w-[150px] bg-transparent outline-none text-zinc-900 dark:text-white placeholder-zinc-400"
              />
            </div>

            <!-- User Dropdown -->
            <div
              v-if="showUserDropdown && (filteredUsers.length > 0 || isLoading)"
              class="absolute top-full left-0 right-0 mt-1 bg-white dark:bg-zinc-800 border border-zinc-200 dark:border-zinc-700 rounded-lg shadow-lg z-10 max-h-60 overflow-y-auto"
            >
              <div v-if="isLoading" class="p-4 text-center text-zinc-500">
                <span class="material-symbols-outlined animate-spin">progress_activity</span>
              </div>
              <button
                v-for="user in filteredUsers"
                :key="user.id"
                @click="selectUser(user)"
                class="w-full flex items-center gap-3 p-3 hover:bg-zinc-50 dark:hover:bg-zinc-700 transition-colors text-left"
              >
                <div :class="[getAvatarColor(user.id), 'w-10 h-10 rounded-full flex items-center justify-center text-white font-medium']">
                  {{ getInitials(user) }}
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-zinc-900 dark:text-white truncate">
                    {{ user.displayName || user.username }}
                  </p>
                  <p v-if="user.email" class="text-xs text-zinc-500 truncate">{{ user.email }}</p>
                </div>
              </button>
              <div v-if="!isLoading && filteredUsers.length === 0 && searchQuery" class="p-4 text-center text-zinc-500 text-sm">
                No users found
              </div>
            </div>
          </div>

          <!-- Permission Selector -->
          <div class="flex items-center gap-3">
            <span class="text-sm text-zinc-600 dark:text-zinc-400">Permission:</span>
            <div class="flex gap-2">
              <button
                v-for="opt in permissionOptions"
                :key="opt.value"
                @click="permissionLevel = opt.value"
                class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm font-medium transition-colors"
                :class="permissionLevel === opt.value
                  ? 'bg-teal text-white'
                  : 'bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 hover:bg-zinc-200'"
              >
                <span class="material-symbols-outlined text-lg">{{ opt.icon }}</span>
                {{ opt.label }}
              </button>
            </div>
          </div>

          <!-- Message -->
          <div>
            <textarea
              v-model="message"
              rows="2"
              placeholder="Add a message (optional)"
              class="w-full px-4 py-3 border border-zinc-300 dark:border-zinc-600 rounded-lg bg-white dark:bg-zinc-800 text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal resize-none placeholder-zinc-400"
            ></textarea>
          </div>

          <!-- Send Button -->
          <button
            @click="handleSubmit"
            :disabled="isSubmitting || selectedUsers.length === 0"
            class="w-full py-3 bg-teal text-white rounded-lg font-semibold hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
          >
            <span v-if="isSubmitting" class="material-symbols-outlined animate-spin">progress_activity</span>
            <span class="material-symbols-outlined" v-else>send</span>
            {{ isSubmitting ? 'Sending...' : 'Send' }}
          </button>
        </div>

        <!-- Link Tab -->
        <div v-if="activeTab === 'link'" class="space-y-4">
          <div class="p-4 bg-zinc-50 dark:bg-zinc-800 rounded-xl">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-10 h-10 rounded-lg bg-teal/10 flex items-center justify-center">
                <span class="material-symbols-outlined text-teal">link</span>
              </div>
              <div>
                <p class="text-sm font-medium text-zinc-900 dark:text-white">Anyone with the link</p>
                <p class="text-xs text-zinc-500">Users with access can view this document</p>
              </div>
            </div>

            <div class="flex gap-2">
              <input
                :value="shareLink"
                readonly
                class="flex-1 px-3 py-2 bg-white dark:bg-zinc-700 border border-zinc-200 dark:border-zinc-600 rounded-lg text-sm text-zinc-600 dark:text-zinc-300"
              />
              <button
                @click="copyLink"
                class="px-4 py-2 bg-teal text-white rounded-lg font-medium hover:bg-teal/90 transition-colors flex items-center gap-2"
              >
                <span class="material-symbols-outlined text-lg">{{ linkCopied ? 'check' : 'content_copy' }}</span>
                {{ linkCopied ? 'Copied!' : 'Copy' }}
              </button>
            </div>
          </div>

          <div class="flex items-center gap-3 text-zinc-500 text-sm">
            <span class="material-symbols-outlined text-lg">info</span>
            <p>Only people with existing access can open this link.</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
