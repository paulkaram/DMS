<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

defineProps<{
  sidebarOpen: boolean
}>()

const emit = defineEmits<{
  toggleSidebar: []
  logout: []
}>()

const router = useRouter()
const authStore = useAuthStore()
const searchQuery = ref('')
const showUserMenu = ref(false)
const isDarkMode = ref(false)

function handleSearch() {
  if (searchQuery.value.trim()) {
    router.push({ name: 'search', query: { q: searchQuery.value } })
  }
}

function getInitials() {
  if (authStore.user?.firstName && authStore.user?.lastName) {
    return (authStore.user.firstName[0] + authStore.user.lastName[0]).toUpperCase()
  }
  return authStore.user?.username?.substring(0, 2).toUpperCase() || 'DU'
}

function closeMenu() {
  showUserMenu.value = false
}

function toggleDarkMode() {
  isDarkMode.value = !isDarkMode.value
  document.documentElement.classList.toggle('dark')
}
</script>

<template>
  <header class="h-16 border-b border-zinc-200 dark:border-border-dark bg-white dark:bg-background-dark flex items-center justify-between px-6 sticky top-0 z-30">
    <!-- Left side - Mobile menu & Breadcrumb area -->
    <div class="flex items-center gap-4">
      <button
        @click="emit('toggleSidebar')"
        class="lg:hidden p-2 text-zinc-500 hover:bg-zinc-100 dark:hover:bg-surface-dark rounded-lg transition-colors"
      >
        <span class="material-symbols-outlined">menu</span>
      </button>

      <!-- Page context / Breadcrumb placeholder -->
      <div class="hidden sm:flex items-center gap-2 text-sm">
        <span class="text-zinc-400">Welcome back,</span>
        <span class="font-semibold text-zinc-700 dark:text-zinc-200">
          {{ authStore.user?.firstName || authStore.user?.username || 'User' }}
        </span>
      </div>
    </div>

    <!-- Center - Search -->
    <div class="flex-1 max-w-2xl px-8 hidden md:block">
      <div class="relative group">
        <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400 group-focus-within:text-teal transition-colors">search</span>
        <input
          v-model="searchQuery"
          @keyup.enter="handleSearch"
          class="w-full pl-10 pr-4 py-2.5 bg-zinc-50 dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal/50 text-sm transition-all placeholder:text-zinc-400"
          placeholder="Search documents, folders, or keywords..."
          type="text"
        />
        <kbd class="absolute right-3 top-1/2 -translate-y-1/2 hidden lg:inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-medium text-zinc-400 bg-zinc-100 dark:bg-border-dark rounded border border-zinc-200 dark:border-border-dark">
          <span>Ctrl</span>
          <span>K</span>
        </kbd>
      </div>
    </div>

    <!-- Right side - Actions -->
    <div class="flex items-center gap-2">
      <!-- Quick actions -->
      <button
        class="p-2.5 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-all duration-200"
        title="Upload document"
      >
        <span class="material-symbols-outlined text-xl">upload</span>
      </button>

      <button
        @click="toggleDarkMode"
        class="p-2.5 text-zinc-500 hover:text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-900/20 rounded-lg transition-all duration-200"
        title="Toggle dark mode"
      >
        <span class="material-symbols-outlined text-xl" :class="isDarkMode ? 'hidden' : ''">dark_mode</span>
        <span class="material-symbols-outlined text-xl" :class="isDarkMode ? '' : 'hidden'">light_mode</span>
      </button>

      <button
        class="p-2.5 text-zinc-500 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg transition-all duration-200 relative"
        title="Notifications"
      >
        <span class="material-symbols-outlined text-xl">notifications</span>
        <span class="absolute top-1.5 right-1.5 w-2.5 h-2.5 bg-red-500 rounded-full border-2 border-white dark:border-zinc-900 animate-pulse"></span>
      </button>

      <!-- Divider -->
      <div class="w-px h-8 bg-zinc-200 dark:bg-border-dark mx-2"></div>

      <!-- User menu -->
      <div class="relative">
        <button
          @click="showUserMenu = !showUserMenu"
          class="flex items-center gap-3 p-1.5 rounded-lg hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors"
        >
          <div class="text-right hidden sm:block">
            <p class="text-sm font-semibold text-zinc-900 dark:text-zinc-100">
              {{ authStore.user?.displayName || authStore.user?.username || 'Demo User' }}
            </p>
            <p class="text-[11px] text-zinc-500">
              {{ authStore.user?.email || 'demo@example.com' }}
            </p>
          </div>
          <div class="relative">
            <div class="w-10 h-10 rounded-lg bg-gradient-to-br from-teal to-navy flex items-center justify-center text-white font-bold text-sm shadow-lg shadow-teal/20">
              {{ getInitials() }}
            </div>
            <span class="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-green-500 border-2 border-white dark:border-zinc-900 rounded-full"></span>
          </div>
          <span class="material-symbols-outlined text-zinc-400 hidden sm:block">expand_more</span>
        </button>

        <transition
          enter-active-class="transition ease-out duration-100"
          enter-from-class="transform opacity-0 scale-95"
          enter-to-class="transform opacity-100 scale-100"
          leave-active-class="transition ease-in duration-75"
          leave-from-class="transform opacity-100 scale-100"
          leave-to-class="transform opacity-0 scale-95"
        >
          <div
            v-if="showUserMenu"
            class="absolute right-0 top-14 w-64 bg-white dark:bg-background-dark rounded-lg shadow-xl border border-zinc-200 dark:border-border-dark py-2 overflow-hidden"
          >
            <!-- User info header -->
            <div class="px-4 py-3 border-b border-zinc-100 dark:border-border-dark">
              <div class="flex items-center gap-3">
                <div class="w-12 h-12 rounded-lg bg-gradient-to-br from-teal to-navy flex items-center justify-center text-white font-bold shadow-lg">
                  {{ getInitials() }}
                </div>
                <div>
                  <p class="font-semibold text-zinc-900 dark:text-zinc-100">
                    {{ authStore.user?.displayName || authStore.user?.username }}
                  </p>
                  <p class="text-xs text-zinc-500">{{ authStore.user?.email }}</p>
                </div>
              </div>
            </div>

            <!-- Menu items -->
            <div class="py-2">
              <router-link
                to="/profile"
                @click="closeMenu"
                class="flex items-center gap-3 px-4 py-2.5 text-sm text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-surface-dark transition-colors"
              >
                <span class="material-symbols-outlined text-lg text-zinc-400">person</span>
                My Profile
              </router-link>
              <router-link
                to="/settings"
                @click="closeMenu"
                class="flex items-center gap-3 px-4 py-2.5 text-sm text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-surface-dark transition-colors"
              >
                <span class="material-symbols-outlined text-lg text-zinc-400">settings</span>
                Settings
              </router-link>
            </div>

            <!-- Logout -->
            <div class="border-t border-zinc-100 dark:border-border-dark pt-2">
              <button
                @click="emit('logout'); closeMenu()"
                class="flex items-center gap-3 w-full px-4 py-2.5 text-sm text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
              >
                <span class="material-symbols-outlined text-lg">logout</span>
                Sign out
              </button>
            </div>
          </div>
        </transition>
      </div>
    </div>
  </header>
</template>
