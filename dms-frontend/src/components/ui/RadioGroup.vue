<script setup lang="ts">
import { computed, provide } from 'vue'

interface Option {
  value: string | number | boolean
  label: string
  description?: string
  disabled?: boolean
}

interface Props {
  modelValue?: string | number | boolean
  options?: Option[]
  label?: string
  orientation?: 'horizontal' | 'vertical'
  size?: 'sm' | 'md' | 'lg'
  color?: 'blue' | 'green' | 'red' | 'purple' | 'orange'
  name?: string
  disabled?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  options: () => [],
  orientation: 'vertical',
  size: 'md',
  color: 'blue',
  disabled: false
})

const emit = defineEmits<{
  'update:modelValue': [value: string | number | boolean]
}>()

const selectedValue = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value!)
})

const groupName = computed(() => props.name || `radio-group-${Math.random().toString(36).substr(2, 9)}`)

provide('radioGroupName', groupName)
provide('radioGroupColor', props.color)
provide('radioGroupSize', props.size)
</script>

<template>
  <div class="space-y-2">
    <label v-if="label" class="block text-sm font-medium text-gray-700">{{ label }}</label>
    <div
      :class="[
        orientation === 'horizontal' ? 'flex flex-wrap gap-6' : 'space-y-3'
      ]"
    >
      <slot :selected="selectedValue" :update="(v: any) => selectedValue = v">
        <label
          v-for="option in options"
          :key="String(option.value)"
          class="relative flex items-start gap-3 cursor-pointer group"
          :class="{ 'opacity-50 cursor-not-allowed': option.disabled || disabled }"
        >
          <div class="flex items-center h-5 mt-0.5">
            <div
              class="relative rounded-full border-2 transition-all duration-200 cursor-pointer w-5 h-5"
              :class="[
                selectedValue === option.value
                  ? `border-${color}-600`
                  : 'border-gray-300 hover:border-gray-400'
              ]"
            >
              <input
                v-model="selectedValue"
                type="radio"
                :name="groupName"
                :value="option.value"
                :disabled="option.disabled || disabled"
                class="absolute inset-0 opacity-0 cursor-pointer disabled:cursor-not-allowed"
              />
              <div
                class="absolute inset-0 m-auto rounded-full w-2 h-2 transition-transform duration-200"
                :class="[
                  `bg-${color}-600`,
                  selectedValue === option.value ? 'scale-100' : 'scale-0'
                ]"
              />
            </div>
          </div>
          <div class="flex flex-col">
            <span class="text-sm font-medium text-gray-900 group-hover:text-gray-700 transition-colors">
              {{ option.label }}
            </span>
            <span v-if="option.description" class="text-gray-500 text-xs mt-0.5">
              {{ option.description }}
            </span>
          </div>
        </label>
      </slot>
    </div>
  </div>
</template>
