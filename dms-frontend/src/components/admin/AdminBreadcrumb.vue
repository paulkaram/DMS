<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const props = defineProps<{
  currentPage: string
  icon?: string
}>()

const route = useRoute()

// Get parent section if available from route meta or path
const parentSection = computed(() => {
  const path = route.path
  // Could be extended to show intermediate sections
  return null
})
</script>

<template>
  <nav class="flex items-center gap-2 text-sm mb-6">
    <!-- Home / Admin link -->
    <router-link
      to="/admin"
      class="flex items-center gap-1.5 text-zinc-500 dark:text-zinc-400 hover:text-primary transition-colors"
    >
      <span class="material-symbols-outlined text-lg">admin_panel_settings</span>
      <span>Admin</span>
    </router-link>

    <!-- Separator -->
    <span class="material-symbols-outlined text-zinc-300 dark:text-zinc-600 text-lg">chevron_right</span>

    <!-- Current Page -->
    <div class="flex items-center gap-1.5 text-zinc-900 dark:text-white font-medium">
      <span v-if="icon" class="material-symbols-outlined text-primary text-lg">{{ icon }}</span>
      <span>{{ currentPage }}</span>
    </div>
  </nav>
</template>
