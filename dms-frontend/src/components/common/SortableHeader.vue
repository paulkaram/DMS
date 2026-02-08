<script setup lang="ts">
const props = defineProps<{
  label: string
  field: string
  activeField: string
  direction: 'asc' | 'desc'
}>()

const emit = defineEmits<{
  sort: [field: string]
}>()

const isActive = () => props.field === props.activeField
</script>

<template>
  <th
    class="px-5 py-4 cursor-pointer hover:text-teal transition-all select-none group"
    :class="isActive() ? 'text-teal' : 'text-zinc-300'"
    @click="emit('sort', field)"
  >
    <div class="flex items-center gap-1.5">
      {{ label }}
      <span
        v-if="isActive()"
        class="material-symbols-outlined text-sm transition-transform"
        :class="{ 'rotate-180': direction === 'desc' }"
      >
        arrow_upward
      </span>
      <span
        v-else
        class="material-symbols-outlined text-sm opacity-0 group-hover:opacity-50 transition-opacity"
      >
        arrow_upward
      </span>
    </div>
  </th>
</template>
