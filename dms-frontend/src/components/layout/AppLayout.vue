<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Sidebar from './Sidebar.vue'
import TopNav from './TopNav.vue'

const router = useRouter()
const authStore = useAuthStore()
const sidebarOpen = ref(true)

function toggleSidebar() {
  sidebarOpen.value = !sidebarOpen.value
}

function handleLogout() {
  authStore.logout()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-screen bg-background-light dark:bg-background-dark text-zinc-700 dark:text-zinc-300 flex flex-col">
    <Sidebar :open="sidebarOpen" @toggle="toggleSidebar" />
    <div
      class="flex-1 flex flex-col transition-all duration-300"
      :class="sidebarOpen ? 'lg:ml-64' : 'lg:ml-20'"
    >
      <TopNav
        :sidebar-open="sidebarOpen"
        @toggle-sidebar="toggleSidebar"
        @logout="handleLogout"
      />
      <main class="flex-1 p-6 overflow-y-auto">
        <slot />
      </main>
    </div>
  </div>
</template>
