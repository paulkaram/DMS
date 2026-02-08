<script setup lang="ts">
import { computed, ref } from 'vue'

interface Props {
  modelValue?: string | number
  type?: 'text' | 'email' | 'password' | 'number' | 'tel' | 'url' | 'search'
  label?: string
  placeholder?: string
  disabled?: boolean
  readonly?: boolean
  error?: string
  hint?: string
  size?: 'sm' | 'md' | 'lg'
  prefixIcon?: string
  suffixIcon?: string
  clearable?: boolean
  maxlength?: number
}

const props = withDefaults(defineProps<Props>(), {
  type: 'text',
  disabled: false,
  readonly: false,
  clearable: false,
  size: 'md'
})

const emit = defineEmits<{
  'update:modelValue': [value: string | number]
  'focus': [event: FocusEvent]
  'blur': [event: FocusEvent]
}>()

const inputRef = ref<HTMLInputElement>()
const isFocused = ref(false)
const showPassword = ref(false)

const inputValue = computed({
  get: () => props.modelValue ?? '',
  set: (value) => emit('update:modelValue', value)
})

const actualType = computed(() => {
  if (props.type === 'password' && showPassword.value) return 'text'
  return props.type
})

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return 'px-3 py-1.5 text-sm'
    case 'lg': return 'px-4 py-3 text-base'
    default: return 'px-4 py-2.5 text-sm'
  }
})

function handleFocus(e: FocusEvent) {
  isFocused.value = true
  emit('focus', e)
}

function handleBlur(e: FocusEvent) {
  isFocused.value = false
  emit('blur', e)
}

function clear() {
  emit('update:modelValue', '')
  inputRef.value?.focus()
}

function focus() {
  inputRef.value?.focus()
}

defineExpose({ focus, inputRef })
</script>

<template>
  <div class="w-full">
    <label v-if="label" class="block text-sm font-medium text-gray-700 mb-1.5">{{ label }}</label>

    <div class="relative">
      <!-- Prefix Icon -->
      <div v-if="$slots.prefix || prefixIcon" class="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">
        <slot name="prefix">
          <component :is="prefixIcon" v-if="prefixIcon" class="w-5 h-5" />
        </slot>
      </div>

      <input
        ref="inputRef"
        v-model="inputValue"
        :type="actualType"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        :maxlength="maxlength"
        class="w-full bg-white border rounded-xl transition-all duration-200
               focus:outline-none focus:ring-2 focus:ring-teal/50 focus:border-teal
               disabled:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60
               placeholder:text-gray-400"
        :class="[
          sizeClasses,
          error ? 'border-red-300 focus:ring-red-500 focus:border-red-500' : 'border-gray-300 hover:border-gray-400',
          $slots.prefix || prefixIcon ? 'pl-10' : '',
          (clearable && inputValue) || type === 'password' || $slots.suffix ? 'pr-10' : ''
        ]"
        @focus="handleFocus"
        @blur="handleBlur"
      />

      <!-- Suffix Actions -->
      <div class="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1">
        <!-- Clear Button -->
        <button
          v-if="clearable && inputValue && !disabled && !readonly"
          type="button"
          class="p-0.5 hover:bg-gray-100 rounded-full transition-colors text-gray-400 hover:text-gray-600"
          @click="clear"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>

        <!-- Password Toggle -->
        <button
          v-if="type === 'password'"
          type="button"
          class="p-0.5 hover:bg-gray-100 rounded-full transition-colors text-gray-400 hover:text-gray-600"
          @click="showPassword = !showPassword"
        >
          <svg v-if="showPassword" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
          </svg>
          <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
          </svg>
        </button>

        <!-- Suffix Slot -->
        <slot name="suffix" />
      </div>
    </div>

    <!-- Error/Hint Message -->
    <p v-if="error" class="mt-1.5 text-sm text-red-600">{{ error }}</p>
    <p v-else-if="hint" class="mt-1.5 text-sm text-gray-500">{{ hint }}</p>
  </div>
</template>
