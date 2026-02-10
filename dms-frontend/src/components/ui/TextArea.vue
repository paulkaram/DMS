<script setup lang="ts">
import { computed, ref } from 'vue'

interface Props {
  modelValue?: string
  label?: string
  placeholder?: string
  disabled?: boolean
  readonly?: boolean
  error?: string
  hint?: string
  rows?: number
  maxlength?: number
  resize?: 'none' | 'vertical' | 'horizontal' | 'both'
  showCount?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  readonly: false,
  rows: 4,
  resize: 'vertical',
  showCount: false
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'focus': [event: FocusEvent]
  'blur': [event: FocusEvent]
}>()

const textareaRef = ref<HTMLTextAreaElement>()
const isFocused = ref(false)

const inputValue = computed({
  get: () => props.modelValue ?? '',
  set: (value) => emit('update:modelValue', value)
})

const charCount = computed(() => inputValue.value.length)

const resizeClass = computed(() => {
  switch (props.resize) {
    case 'none': return 'resize-none'
    case 'horizontal': return 'resize-x'
    case 'both': return 'resize'
    default: return 'resize-y'
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

function focus() {
  textareaRef.value?.focus()
}

defineExpose({ focus, textareaRef })
</script>

<template>
  <div class="w-full">
    <label v-if="label" class="block text-sm font-medium text-gray-700 mb-1.5">{{ label }}</label>

    <div class="relative">
      <textarea
        ref="textareaRef"
        v-model="inputValue"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        :rows="rows"
        :maxlength="maxlength"
        class="w-full bg-white border rounded-lg px-4 py-3 text-sm transition-all duration-200
               focus:outline-none focus:ring-2 focus:ring-teal/50 focus:border-teal
               disabled:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-60
               placeholder:text-gray-400"
        :class="[
          resizeClass,
          error ? 'border-red-300 focus:ring-red-500 focus:border-red-500' : 'border-gray-300 hover:border-gray-400'
        ]"
        @focus="handleFocus"
        @blur="handleBlur"
      />
    </div>

    <div class="flex items-center justify-between mt-1.5">
      <!-- Error/Hint Message -->
      <p v-if="error" class="text-sm text-red-600">{{ error }}</p>
      <p v-else-if="hint" class="text-sm text-gray-500">{{ hint }}</p>
      <span v-else />

      <!-- Character Count -->
      <span v-if="showCount || maxlength" class="text-xs text-gray-400">
        {{ charCount }}<span v-if="maxlength"> / {{ maxlength }}</span>
      </span>
    </div>
  </div>
</template>
