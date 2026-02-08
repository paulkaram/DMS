<script setup lang="ts">
import { computed } from 'vue'
import type { Document } from '@/types'

const props = defineProps<{
  document: Document
  showIcon?: boolean
  size?: 'sm' | 'md'
}>()

const statusInfo = computed(() => {
  if (props.document.isCheckedOut) {
    return {
      label: 'Checked Out',
      bgClass: 'bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700',
      textClass: 'text-amber-600 dark:text-amber-400',
      dotClass: 'bg-amber-500',
      icon: 'assignment_ind'
    }
  }
  if (props.document.isLocked) {
    return {
      label: 'Locked',
      bgClass: 'bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700',
      textClass: 'text-red-600 dark:text-red-400',
      dotClass: 'bg-red-500',
      icon: 'lock'
    }
  }
  return {
    label: 'Available',
    bgClass: 'bg-teal/10 border border-teal/30',
    textClass: 'text-teal',
    dotClass: 'bg-teal',
    icon: 'check_circle'
  }
})

const sizeClasses = computed(() => {
  return props.size === 'sm'
    ? 'px-2 py-0.5 text-[9px]'
    : 'px-2.5 py-1 text-[10px]'
})
</script>

<template>
  <span
    class="inline-flex items-center gap-1.5 rounded-lg font-semibold uppercase tracking-wider"
    :class="[statusInfo.bgClass, statusInfo.textClass, sizeClasses]"
  >
    <span v-if="showIcon" class="material-symbols-outlined text-xs" style="font-variation-settings: 'FILL' 1;">{{ statusInfo.icon }}</span>
    <span v-else class="w-1.5 h-1.5 rounded-full" :class="statusInfo.dotClass"></span>
    {{ statusInfo.label }}
  </span>
</template>
