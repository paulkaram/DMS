<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const props = defineProps<{
  open: boolean
}>()

const emit = defineEmits<{
  toggle: []
}>()

const route = useRoute()

const menuItems = [
  { name: 'Dashboard', path: '/', icon: 'dashboard' },
  { name: 'Explorer', path: '/explorer', icon: 'explore' },
  { name: 'Search', path: '/search', icon: 'search' },
  { name: 'Scan', path: '/scan', icon: 'document_scanner' },
  { name: 'My Workspace', path: '/my-workspace', icon: 'workspaces' },
  { name: 'Records Management', path: '/records', icon: 'verified_user' },
  { name: 'Archive', path: '/archive', icon: 'inventory_2' }
]

function isActive(path: string) {
  if (path === '/') {
    return route.path === '/'
  }
  return route.path === path || route.path.startsWith(path + '/')
}

function isAdminActive() {
  return route.path === '/admin' || route.path.startsWith('/admin/') || route.path.startsWith('/users') || route.path.startsWith('/settings')
}

const sidebarWidth = computed(() => props.open ? 'w-64' : 'w-20')
</script>

<template>
  <aside
    class="sidebar-container fixed left-0 top-0 bottom-0 transition-all duration-300 ease-in-out z-40 hidden lg:flex flex-col"
    :class="sidebarWidth"
  >
    <!-- Brand Header -->
    <div class="relative h-16 flex-shrink-0 overflow-hidden">
      <!-- Header gradient - charcoal with subtle teal -->
      <div class="absolute inset-0 bg-gradient-to-r from-[#0a0d10] via-[#0d1117] to-[#0a0d10]"></div>

      <!-- Subtle teal overlay -->
      <div class="absolute inset-0 bg-gradient-to-br from-teal/5 via-transparent to-teal/3"></div>

      <!-- Cyan accent line -->
      <div class="absolute bottom-0 left-0 right-0 h-[1px] bg-gradient-to-r from-transparent via-teal to-transparent opacity-60"></div>

      <!-- Content -->
      <div class="relative h-full flex items-center justify-between px-4 transition-all duration-300">
        <!-- Logo - always visible, clickable to expand when collapsed -->
        <component
          :is="open ? 'router-link' : 'button'"
          :to="open ? '/' : undefined"
          @click="!open && emit('toggle')"
          class="flex items-center gap-3 min-w-0 transition-all duration-300"
          :class="{ 'flex-1 justify-center': !open }"
          :title="!open ? 'Expand sidebar' : undefined"
        >
          <!-- Logo with glow -->
          <div class="relative flex-shrink-0">
            <div class="absolute inset-0 bg-teal/30 blur-xl rounded-full"></div>
            <div class="relative w-9 h-9 rounded-lg bg-gradient-to-br from-teal via-teal to-teal/80 flex items-center justify-center shadow-lg shadow-teal/40 hover:shadow-teal/60 transition-all duration-300">
              <span class="material-symbols-outlined text-white text-xl">description</span>
            </div>
          </div>

          <!-- Brand text - fades in/out -->
          <div
            class="flex flex-col min-w-0 transition-all duration-300 overflow-hidden"
            :class="open ? 'w-auto opacity-100' : 'w-0 opacity-0'"
          >
            <span class="text-base font-bold text-white tracking-tight truncate whitespace-nowrap">INTALIO</span>
            <span class="text-[9px] text-teal font-semibold tracking-widest uppercase whitespace-nowrap">Enterprise DMS</span>
          </div>
        </component>

        <!-- Collapse toggle button - fades in/out -->
        <button
          @click="emit('toggle')"
          class="flex-shrink-0 w-7 h-7 rounded-md bg-white/5 hover:bg-teal/20 flex items-center justify-center text-zinc-400 hover:text-teal transition-all duration-300 border border-white/10 hover:border-teal/30"
          :class="open ? 'opacity-100 scale-100' : 'opacity-0 scale-0 absolute'"
          title="Collapse sidebar"
        >
          <span class="material-symbols-outlined text-lg">
            chevron_left
          </span>
        </button>
      </div>
    </div>

    <!-- Navigation -->
    <nav class="flex-1 overflow-y-auto overflow-x-hidden py-4 px-2.5 scrollbar-thin">
      <div class="space-y-0.5">
        <router-link
          v-for="item in menuItems"
          :key="item.path"
          :to="item.path"
          class="menu-item flex items-center gap-3 px-3 py-2 text-[13px] rounded-lg transition-all duration-200 group relative overflow-hidden"
          :class="[
            isActive(item.path)
              ? 'menu-item-active'
              : 'menu-item-inactive',
            { 'justify-center': !open }
          ]"
          :title="!open ? item.name : ''"
        >
          <span
            class="material-symbols-outlined text-xl transition-all duration-200 relative z-10"
            :class="isActive(item.path) ? 'text-teal' : 'text-zinc-300 group-hover:text-teal'"
          >
            {{ item.icon }}
          </span>
          <span v-if="open" class="truncate relative z-10">{{ item.name }}</span>
        </router-link>
      </div>
    </nav>

    <!-- Footer -->
    <div class="flex-shrink-0 p-2.5 relative overflow-hidden bg-[#0a0d10]">
      <!-- Top border with gradient -->
      <div class="absolute top-0 left-4 right-4 h-px bg-gradient-to-r from-transparent via-teal/40 to-transparent"></div>

      <!-- Admin link -->
      <router-link
        to="/admin"
        class="menu-item relative flex items-center gap-3 px-3 py-2 text-[13px] rounded-lg transition-all duration-200 group overflow-hidden"
        :class="[
          isAdminActive()
            ? 'menu-item-active'
            : 'menu-item-inactive',
          { 'justify-center': !open }
        ]"
        :title="!open ? 'Administration' : ''"
      >
        <span
          class="material-symbols-outlined text-xl transition-all duration-200 relative z-10"
          :class="isAdminActive() ? 'text-teal' : 'text-zinc-300 group-hover:text-teal'"
        >
          admin_panel_settings
        </span>
        <span v-if="open" class="truncate relative z-10">Administration</span>
        <span v-if="open && !isAdminActive()" class="ml-auto relative z-10">
          <span class="material-symbols-outlined text-sm text-zinc-400 group-hover:text-teal transition-colors">chevron_right</span>
        </span>
      </router-link>
    </div>
  </aside>
</template>

<style scoped>
/* Sidebar container with charcoal-cyan gradient */
.sidebar-container {
  background: linear-gradient(
    180deg,
    #0d1117 0%,
    #111318 50%,
    #0f1419 100%
  );
  border-right: 1px solid rgba(0, 174, 140, 0.2);
  box-shadow:
    inset -1px 0 0 rgba(255, 255, 255, 0.02),
    4px 0 24px rgba(0, 0, 0, 0.3);
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
}

/* Sharp text for all menu items */
.menu-item {
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
  letter-spacing: 0.01em;
}

/* Active menu item with gradient background */
.menu-item-active {
  background: linear-gradient(90deg, rgba(0, 174, 140, 0.2) 0%, rgba(0, 174, 140, 0.08) 100%);
  color: #00ae8c;
  font-weight: 500;
  border-left: 2px solid #00ae8c;
  box-shadow:
    inset 0 0 20px rgba(0, 174, 140, 0.1),
    0 0 12px rgba(0, 174, 140, 0.05);
}

.menu-item-active::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: radial-gradient(ellipse at left center, rgba(0, 174, 140, 0.15) 0%, transparent 70%);
  pointer-events: none;
}

/* Inactive menu item */
.menu-item-inactive {
  color: #d4d4d8;
  border-left: 2px solid transparent;
}

.menu-item-inactive:hover {
  background: linear-gradient(90deg, rgba(255, 255, 255, 0.08) 0%, rgba(255, 255, 255, 0.03) 100%);
  color: #ffffff;
  border-left-color: rgba(0, 174, 140, 0.5);
}

.menu-item-inactive:hover::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: radial-gradient(ellipse at left center, rgba(0, 174, 140, 0.08) 0%, transparent 70%);
  pointer-events: none;
}

/* Scrollbar styling */
.scrollbar-thin::-webkit-scrollbar {
  width: 4px;
}

.scrollbar-thin::-webkit-scrollbar-track {
  background: transparent;
}

.scrollbar-thin::-webkit-scrollbar-thumb {
  background: linear-gradient(180deg, rgba(0, 174, 140, 0.3) 0%, rgba(0, 174, 140, 0.1) 100%);
  border-radius: 2px;
}

.scrollbar-thin::-webkit-scrollbar-thumb:hover {
  background: linear-gradient(180deg, rgba(0, 174, 140, 0.5) 0%, rgba(0, 174, 140, 0.2) 100%);
}

/* Filled icons for active menu items */
.menu-item-active .material-symbols-outlined {
  font-variation-settings:
    'FILL' 1,
    'wght' 500,
    'GRAD' 0,
    'opsz' 24;
}

/* Section headers sharp text */
.sidebar-container button span {
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
}

/* Brand text sharpness */
.sidebar-container a span {
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-rendering: optimizeLegibility;
}
</style>
