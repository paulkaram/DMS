<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  modelValue?: boolean
  label?: string
  description?: string
  disabled?: boolean
  size?: 'sm' | 'md' | 'lg'
  color?: 'teal' | 'green' | 'red' | 'purple' | 'orange'
  labelPosition?: 'left' | 'right'
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: false,
  disabled: false,
  size: 'md',
  color: 'teal',
  labelPosition: 'right'
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const isEnabled = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value)
})

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return { track: 'w-8 h-4', thumb: 'w-3 h-3', translate: 'translate-x-4' }
    case 'lg': return { track: 'w-14 h-7', thumb: 'w-6 h-6', translate: 'translate-x-7' }
    default: return { track: 'w-11 h-6', thumb: 'w-5 h-5', translate: 'translate-x-5' }
  }
})

const colorClasses = computed(() => {
  const colors = {
    teal: 'bg-teal',
    green: 'bg-green-600',
    red: 'bg-red-600',
    purple: 'bg-purple-600',
    orange: 'bg-orange-600'
  }
  return colors[props.color]
})
</script>

<template>
  <label
    class="inline-flex items-center gap-3 cursor-pointer"
    :class="[
      { 'opacity-50 cursor-not-allowed': disabled },
      labelPosition === 'left' ? 'flex-row-reverse' : ''
    ]"
  >
    <button
      type="button"
      role="switch"
      :aria-checked="isEnabled"
      :disabled="disabled"
      class="relative inline-flex items-center rounded-full transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-teal/50"
      :class="[
        sizeClasses.track,
        isEnabled ? colorClasses : 'bg-gray-200'
      ]"
      @click="isEnabled = !isEnabled"
    >
      <span
        class="inline-block bg-white rounded-full shadow transform transition-transform duration-200 ease-in-out"
        :class="[
          sizeClasses.thumb,
          isEnabled ? sizeClasses.translate : 'translate-x-0.5'
        ]"
      />
    </button>
    <div v-if="label || description || $slots.default" class="flex flex-col">
      <span v-if="label || $slots.default" class="text-sm font-medium text-gray-900">
        <slot>{{ label }}</slot>
      </span>
      <span v-if="description" class="text-xs text-gray-500">{{ description }}</span>
    </div>
  </label>
</template>
