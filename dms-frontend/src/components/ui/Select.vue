<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'

interface Option {
  value: string | number | null
  label: string
  icon?: string
  disabled?: boolean
  group?: string
}

interface Props {
  modelValue?: string | number | null
  options: Option[]
  placeholder?: string
  label?: string
  disabled?: boolean
  error?: string
  searchable?: boolean
  clearable?: boolean
  size?: 'sm' | 'md' | 'lg'
  multiple?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'Select an option',
  disabled: false,
  searchable: false,
  clearable: false,
  size: 'md',
  multiple: false
})

const emit = defineEmits<{
  'update:modelValue': [value: string | number | null]
}>()

const isOpen = ref(false)
const searchQuery = ref('')
const containerRef = ref<HTMLElement>()
const searchInputRef = ref<HTMLInputElement>()

const selectedOption = computed(() => {
  return props.options.find(opt => opt.value === props.modelValue)
})

const filteredOptions = computed(() => {
  if (!props.searchable || !searchQuery.value) {
    return props.options
  }
  const query = searchQuery.value.toLowerCase()
  return props.options.filter(opt => opt.label.toLowerCase().includes(query))
})

const groupedOptions = computed(() => {
  const groups: Record<string, Option[]> = {}
  const ungrouped: Option[] = []

  filteredOptions.value.forEach(opt => {
    if (opt.group) {
      if (!groups[opt.group]) groups[opt.group] = []
      groups[opt.group].push(opt)
    } else {
      ungrouped.push(opt)
    }
  })

  return { groups, ungrouped }
})

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return 'px-3 py-1.5 text-sm'
    case 'lg': return 'px-4 py-3 text-base'
    default: return 'px-4 py-2.5 text-sm'
  }
})

function toggle() {
  if (props.disabled) return
  isOpen.value = !isOpen.value
  if (isOpen.value && props.searchable) {
    setTimeout(() => searchInputRef.value?.focus(), 50)
  }
}

function select(option: Option) {
  if (option.disabled) return
  emit('update:modelValue', option.value)
  isOpen.value = false
  searchQuery.value = ''
}

function clear(e: Event) {
  e.stopPropagation()
  emit('update:modelValue', null)
}

function handleClickOutside(event: MouseEvent) {
  if (containerRef.value && !containerRef.value.contains(event.target as Node)) {
    isOpen.value = false
    searchQuery.value = ''
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div ref="containerRef" class="relative">
    <label v-if="label" class="block text-sm font-medium text-gray-700 dark:text-slate-300 mb-1.5">{{ label }}</label>

    <!-- Trigger Button -->
    <button
      type="button"
      :disabled="disabled"
      class="relative w-full bg-white dark:bg-slate-800 border rounded-xl text-left cursor-pointer
             transition-all duration-200 flex items-center justify-between gap-2
             focus:outline-none focus:ring-2 focus:ring-teal/50 focus:border-teal"
      :class="[
        sizeClasses,
        disabled ? 'bg-gray-50 dark:bg-slate-900 cursor-not-allowed opacity-60' : 'hover:border-gray-400 dark:hover:border-slate-500',
        error ? 'border-red-300 focus:ring-red-500 focus:border-red-500' : 'border-gray-300 dark:border-slate-600',
        isOpen ? 'ring-2 ring-teal/50 border-teal' : ''
      ]"
      @click="toggle"
    >
      <span :class="selectedOption ? 'text-gray-900 dark:text-white' : 'text-gray-400 dark:text-slate-500'">
        {{ selectedOption?.label || placeholder }}
      </span>
      <div class="flex items-center gap-1">
        <!-- Clear Button -->
        <button
          v-if="clearable && selectedOption"
          type="button"
          class="p-0.5 hover:bg-gray-100 dark:hover:bg-slate-700 rounded-full transition-colors"
          @click="clear"
        >
          <svg class="w-4 h-4 text-gray-400 dark:text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
        <!-- Chevron -->
        <svg
          class="w-5 h-5 text-gray-400 dark:text-slate-500 transition-transform duration-200"
          :class="{ 'rotate-180': isOpen }"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </div>
    </button>

    <!-- Error Message -->
    <p v-if="error" class="mt-1 text-sm text-red-600 dark:text-red-400">{{ error }}</p>

    <!-- Dropdown Panel -->
    <Transition
      enter-active-class="transition duration-100 ease-out"
      enter-from-class="transform scale-95 opacity-0"
      enter-to-class="transform scale-100 opacity-100"
      leave-active-class="transition duration-75 ease-in"
      leave-from-class="transform scale-100 opacity-100"
      leave-to-class="transform scale-95 opacity-0"
    >
      <div
        v-if="isOpen"
        class="absolute z-50 w-full mt-1 bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-xl shadow-lg overflow-hidden"
      >
        <!-- Search Input -->
        <div v-if="searchable" class="p-2 border-b border-gray-100 dark:border-slate-700">
          <div class="relative">
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 dark:text-slate-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <input
              ref="searchInputRef"
              v-model="searchQuery"
              type="text"
              placeholder="Search..."
              class="w-full pl-9 pr-4 py-2 text-sm border border-gray-200 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-teal/50 focus:border-teal"
            />
          </div>
        </div>

        <!-- Options List -->
        <ul class="max-h-60 overflow-auto py-1">
          <template v-if="filteredOptions.length === 0">
            <li class="px-4 py-3 text-sm text-gray-500 dark:text-slate-500 text-center">No options found</li>
          </template>

          <!-- Ungrouped Options -->
          <li
            v-for="option in groupedOptions.ungrouped"
            :key="String(option.value)"
            class="px-4 py-2.5 text-sm cursor-pointer transition-colors flex items-center gap-2"
            :class="[
              option.disabled ? 'text-gray-400 dark:text-slate-600 cursor-not-allowed' : 'text-gray-900 dark:text-slate-100 hover:bg-teal/10',
              option.value === modelValue ? 'bg-teal/10 text-teal' : ''
            ]"
            @click="select(option)"
          >
            <svg
              v-if="option.value === modelValue"
              class="w-4 h-4 text-teal flex-shrink-0"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
            <span :class="{ 'ml-6': option.value !== modelValue }">{{ option.label }}</span>
          </li>

          <!-- Grouped Options -->
          <template v-for="(options, group) in groupedOptions.groups" :key="group">
            <li class="px-4 py-2 text-xs font-semibold text-gray-500 dark:text-slate-400 uppercase tracking-wider bg-gray-50 dark:bg-slate-900">
              {{ group }}
            </li>
            <li
              v-for="option in options"
              :key="String(option.value)"
              class="px-4 py-2.5 text-sm cursor-pointer transition-colors flex items-center gap-2"
              :class="[
                option.disabled ? 'text-gray-400 dark:text-slate-600 cursor-not-allowed' : 'text-gray-900 dark:text-slate-100 hover:bg-teal/10',
                option.value === modelValue ? 'bg-teal/10 text-teal' : ''
              ]"
              @click="select(option)"
            >
              <svg
                v-if="option.value === modelValue"
                class="w-4 h-4 text-teal flex-shrink-0"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
              </svg>
              <span :class="{ 'ml-6': option.value !== modelValue }">{{ option.label }}</span>
            </li>
          </template>
        </ul>
      </div>
    </Transition>
  </div>
</template>
