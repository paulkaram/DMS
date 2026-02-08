<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  modelValue?: boolean
  label?: string
  description?: string
  disabled?: boolean
  indeterminate?: boolean
  size?: 'sm' | 'md' | 'lg'
  color?: 'teal' | 'blue' | 'green' | 'red' | 'purple' | 'orange'
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: false,
  disabled: false,
  indeterminate: false,
  size: 'md',
  color: 'teal'
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
}>()

const isChecked = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit('update:modelValue', value)
})

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return 'w-4 h-4'
    case 'lg': return 'w-6 h-6'
    default: return 'w-5 h-5'
  }
})

const labelSizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return 'text-sm'
    case 'lg': return 'text-base'
    default: return 'text-sm'
  }
})

const colorClasses = computed(() => {
  const colors = {
    teal: 'text-teal focus:ring-teal/50 border-teal',
    blue: 'text-blue-600 focus:ring-blue-500 border-blue-600',
    green: 'text-green-600 focus:ring-green-500 border-green-600',
    red: 'text-red-600 focus:ring-red-500 border-red-600',
    purple: 'text-purple-600 focus:ring-purple-500 border-purple-600',
    orange: 'text-orange-600 focus:ring-orange-500 border-orange-600'
  }
  return colors[props.color]
})
</script>

<template>
  <label class="relative flex items-start gap-3 cursor-pointer group" :class="{ 'opacity-50 cursor-not-allowed': disabled }">
    <div class="flex items-center h-5 mt-0.5">
      <input
        v-model="isChecked"
        type="checkbox"
        :disabled="disabled"
        :indeterminate="indeterminate"
        class="appearance-none border-2 border-gray-300 rounded transition-all duration-200 cursor-pointer
               checked:border-transparent checked:bg-current
               focus:outline-none focus:ring-2 focus:ring-offset-2
               disabled:cursor-not-allowed disabled:opacity-50
               hover:border-gray-400"
        :class="[sizeClasses, colorClasses]"
      />
      <svg
        class="absolute pointer-events-none text-white transition-transform duration-200"
        :class="[
          sizeClasses,
          isChecked ? 'scale-100' : 'scale-0'
        ]"
        viewBox="0 0 20 20"
        fill="currentColor"
      >
        <path
          v-if="!indeterminate"
          fill-rule="evenodd"
          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
          clip-rule="evenodd"
        />
        <path
          v-else
          fill-rule="evenodd"
          d="M5 10a1 1 0 011-1h8a1 1 0 110 2H6a1 1 0 01-1-1z"
          clip-rule="evenodd"
        />
      </svg>
    </div>
    <div v-if="label || description || $slots.default" class="flex flex-col">
      <span
        v-if="label || $slots.default"
        class="font-medium text-gray-900 group-hover:text-gray-700 transition-colors"
        :class="labelSizeClasses"
      >
        <slot>{{ label }}</slot>
      </span>
      <span v-if="description" class="text-gray-500 text-xs mt-0.5">{{ description }}</span>
    </div>
  </label>
</template>
