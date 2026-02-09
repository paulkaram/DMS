<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost' | 'danger' | 'success'
  size?: 'sm' | 'md' | 'lg' | 'xl'
  disabled?: boolean
  loading?: boolean
  block?: boolean
  rounded?: boolean
  iconOnly?: boolean
  type?: 'button' | 'submit' | 'reset'
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  size: 'md',
  disabled: false,
  loading: false,
  block: false,
  rounded: false,
  iconOnly: false,
  type: 'button'
})

const emit = defineEmits<{
  click: [event: MouseEvent]
}>()

const variantClasses = computed(() => {
  const variants = {
    primary: 'bg-teal text-white hover:bg-teal/90 focus:ring-teal/50 shadow-sm',
    secondary: 'bg-zinc-100 dark:bg-surface-dark text-zinc-900 dark:text-zinc-100 hover:bg-zinc-200 dark:hover:bg-border-dark focus:ring-zinc-500',
    outline: 'border-2 border-zinc-300 dark:border-border-dark text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-surface-dark hover:border-zinc-400 focus:ring-zinc-500',
    ghost: 'text-zinc-700 dark:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-surface-dark focus:ring-zinc-500',
    danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500 shadow-sm',
    success: 'bg-emerald-600 text-white hover:bg-emerald-700 focus:ring-emerald-500 shadow-sm'
  }
  return variants[props.variant]
})

const sizeClasses = computed(() => {
  if (props.iconOnly) {
    switch (props.size) {
      case 'sm': return 'p-1.5'
      case 'lg': return 'p-3'
      case 'xl': return 'p-4'
      default: return 'p-2'
    }
  }
  switch (props.size) {
    case 'sm': return 'px-3 py-1.5 text-sm'
    case 'lg': return 'px-6 py-3 text-base'
    case 'xl': return 'px-8 py-4 text-lg'
    default: return 'px-4 py-2 text-sm'
  }
})

function handleClick(e: MouseEvent) {
  if (!props.disabled && !props.loading) {
    emit('click', e)
  }
}
</script>

<template>
  <button
    :type="type"
    :disabled="disabled || loading"
    class="inline-flex items-center justify-center gap-2 font-medium transition-all duration-200
           focus:outline-none focus:ring-2 focus:ring-offset-2
           disabled:opacity-50 disabled:cursor-not-allowed"
    :class="[
      variantClasses,
      sizeClasses,
      rounded ? 'rounded-full' : 'rounded-xl',
      block ? 'w-full' : ''
    ]"
    @click="handleClick"
  >
    <!-- Loading Spinner -->
    <svg
      v-if="loading"
      class="animate-spin h-4 w-4"
      :class="{ '-ml-1': !iconOnly }"
      fill="none"
      viewBox="0 0 24 24"
    >
      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
    </svg>

    <!-- Content -->
    <slot v-if="!loading || !iconOnly" />
  </button>
</template>
