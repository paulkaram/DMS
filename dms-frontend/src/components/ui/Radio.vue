<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  modelValue?: string | number | boolean
  value: string | number | boolean
  label?: string
  description?: string
  disabled?: boolean
  size?: 'sm' | 'md' | 'lg'
  color?: 'teal' | 'green' | 'red' | 'purple' | 'orange'
  name?: string
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  size: 'md',
  color: 'teal'
})

const emit = defineEmits<{
  'update:modelValue': [value: string | number | boolean]
}>()

const isSelected = computed(() => props.modelValue === props.value)

const handleChange = () => {
  if (!props.disabled) {
    emit('update:modelValue', props.value)
  }
}

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return { outer: 'w-4 h-4', inner: 'w-1.5 h-1.5' }
    case 'lg': return { outer: 'w-6 h-6', inner: 'w-2.5 h-2.5' }
    default: return { outer: 'w-5 h-5', inner: 'w-2 h-2' }
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
    teal: { border: 'border-teal', bg: 'bg-teal', ring: 'focus:ring-teal/50' },
    green: { border: 'border-green-600', bg: 'bg-green-600', ring: 'focus:ring-green-500' },
    red: { border: 'border-red-600', bg: 'bg-red-600', ring: 'focus:ring-red-500' },
    purple: { border: 'border-purple-600', bg: 'bg-purple-600', ring: 'focus:ring-purple-500' },
    orange: { border: 'border-orange-600', bg: 'bg-orange-600', ring: 'focus:ring-orange-500' }
  }
  return colors[props.color]
})
</script>

<template>
  <label
    class="relative flex items-start gap-3 cursor-pointer group"
    :class="{ 'opacity-50 cursor-not-allowed': disabled }"
  >
    <div class="flex items-center h-5 mt-0.5">
      <div
        class="relative rounded-full border-2 transition-all duration-200 cursor-pointer
               focus-within:ring-2 focus-within:ring-offset-2"
        :class="[
          sizeClasses.outer,
          colorClasses.ring,
          isSelected ? colorClasses.border : 'border-gray-300 hover:border-gray-400'
        ]"
      >
        <input
          type="radio"
          :name="name"
          :value="value"
          :checked="isSelected"
          :disabled="disabled"
          class="absolute inset-0 opacity-0 cursor-pointer disabled:cursor-not-allowed"
          @change="handleChange"
        />
        <div
          class="absolute inset-0 m-auto rounded-full transition-transform duration-200"
          :class="[
            sizeClasses.inner,
            colorClasses.bg,
            isSelected ? 'scale-100' : 'scale-0'
          ]"
        />
      </div>
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
