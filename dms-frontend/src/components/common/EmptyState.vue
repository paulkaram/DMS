<script setup lang="ts">
defineProps<{
  icon?: string
  title: string
  description?: string
  actionLabel?: string
  actionIcon?: string
}>()

const emit = defineEmits<{
  action: []
}>()
</script>

<template>
  <div class="text-center py-16 bg-white dark:bg-background-dark rounded-2xl border border-zinc-200 dark:border-border-dark">
    <div class="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-zinc-100 dark:bg-surface-dark mb-4">
      <span class="material-symbols-outlined text-4xl text-zinc-400">
        {{ icon || 'folder_off' }}
      </span>
    </div>
    <h3 class="text-lg font-semibold text-zinc-800 dark:text-zinc-200 mb-2">
      {{ title }}
    </h3>
    <p v-if="description" class="text-zinc-500 mb-6">{{ description }}</p>
    <button
      v-if="actionLabel"
      @click="emit('action')"
      class="inline-flex items-center gap-2 px-5 py-2.5 bg-teal text-white rounded-xl font-medium shadow-lg shadow-teal/25 hover:bg-teal/90 transition-all"
    >
      <span v-if="actionIcon" class="material-symbols-outlined">{{ actionIcon }}</span>
      {{ actionLabel }}
    </button>
    <slot />
  </div>
</template>
