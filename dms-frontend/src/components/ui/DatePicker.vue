<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch, nextTick } from 'vue'

interface Props {
  modelValue?: string | Date | null
  label?: string
  placeholder?: string
  disabled?: boolean
  error?: string
  minDate?: string | Date
  maxDate?: string | Date
  format?: string
  clearable?: boolean
  size?: 'sm' | 'md' | 'lg'
}

const props = withDefaults(defineProps<Props>(), {
  placeholder: 'Select date',
  disabled: false,
  clearable: true,
  size: 'md',
  format: 'yyyy-MM-dd'
})

const emit = defineEmits<{
  'update:modelValue': [value: string | null]
}>()

const isOpen = ref(false)
const containerRef = ref<HTMLElement>()
const buttonRef = ref<HTMLElement>()
const dropdownRef = ref<HTMLElement>()
const currentMonth = ref(new Date())
const openUpward = ref(false)
const showYearPicker = ref(false)
const dropdownStyle = ref<{ top?: string; left?: string; width?: string }>({})

const DAYS = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']
const MONTHS = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']
const MONTHS_SHORT = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']

const selectedDate = computed(() => {
  if (!props.modelValue) return null
  return new Date(props.modelValue)
})

const displayValue = computed(() => {
  if (!selectedDate.value) return ''
  const date = selectedDate.value
  return `${MONTHS_SHORT[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()}`
})

const calendarDays = computed(() => {
  const year = currentMonth.value.getFullYear()
  const month = currentMonth.value.getMonth()

  const firstDay = new Date(year, month, 1)
  const lastDay = new Date(year, month + 1, 0)

  const days: { date: Date; isCurrentMonth: boolean; isToday: boolean; isSelected: boolean; isDisabled: boolean }[] = []

  // Previous month days
  const startPadding = firstDay.getDay()
  for (let i = startPadding - 1; i >= 0; i--) {
    const date = new Date(year, month, -i)
    days.push({
      date,
      isCurrentMonth: false,
      isToday: isToday(date),
      isSelected: isSelected(date),
      isDisabled: isDisabled(date)
    })
  }

  // Current month days
  for (let i = 1; i <= lastDay.getDate(); i++) {
    const date = new Date(year, month, i)
    days.push({
      date,
      isCurrentMonth: true,
      isToday: isToday(date),
      isSelected: isSelected(date),
      isDisabled: isDisabled(date)
    })
  }

  // Next month days
  const endPadding = 42 - days.length
  for (let i = 1; i <= endPadding; i++) {
    const date = new Date(year, month + 1, i)
    days.push({
      date,
      isCurrentMonth: false,
      isToday: isToday(date),
      isSelected: isSelected(date),
      isDisabled: isDisabled(date)
    })
  }

  return days
})

const yearRange = computed(() => {
  const currentYear = currentMonth.value.getFullYear()
  const startYear = currentYear - 6
  const years = []
  for (let i = 0; i < 12; i++) {
    years.push(startYear + i)
  }
  return years
})

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return 'px-3 py-1.5 text-sm'
    case 'lg': return 'px-4 py-3 text-base'
    default: return 'px-4 py-2.5 text-sm'
  }
})

function formatDateISO(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function isToday(date: Date): boolean {
  const today = new Date()
  return date.toDateString() === today.toDateString()
}

function isSelected(date: Date): boolean {
  if (!selectedDate.value) return false
  return date.toDateString() === selectedDate.value.toDateString()
}

function isDisabled(date: Date): boolean {
  if (props.minDate && date < new Date(props.minDate)) return true
  if (props.maxDate && date > new Date(props.maxDate)) return true
  return false
}

function selectDate(day: { date: Date; isDisabled: boolean }) {
  if (day.isDisabled) return
  emit('update:modelValue', formatDateISO(day.date))
  isOpen.value = false
}

function selectYear(year: number) {
  currentMonth.value = new Date(year, currentMonth.value.getMonth(), 1)
  showYearPicker.value = false
}

function selectMonth(monthIndex: number) {
  currentMonth.value = new Date(currentMonth.value.getFullYear(), monthIndex, 1)
}

function prevMonth() {
  currentMonth.value = new Date(currentMonth.value.getFullYear(), currentMonth.value.getMonth() - 1, 1)
}

function nextMonth() {
  currentMonth.value = new Date(currentMonth.value.getFullYear(), currentMonth.value.getMonth() + 1, 1)
}

function prevYear() {
  currentMonth.value = new Date(currentMonth.value.getFullYear() - 1, currentMonth.value.getMonth(), 1)
}

function nextYear() {
  currentMonth.value = new Date(currentMonth.value.getFullYear() + 1, currentMonth.value.getMonth(), 1)
}

function goToToday() {
  const today = new Date()
  currentMonth.value = new Date(today.getFullYear(), today.getMonth(), 1)
  emit('update:modelValue', formatDateISO(today))
  isOpen.value = false
}

function clear(e: Event) {
  e.stopPropagation()
  emit('update:modelValue', null)
}

async function toggle() {
  if (props.disabled) return

  if (!isOpen.value) {
    isOpen.value = true
    showYearPicker.value = false
    if (selectedDate.value) {
      currentMonth.value = new Date(selectedDate.value.getFullYear(), selectedDate.value.getMonth(), 1)
    }
    await nextTick()
    calculateDropdownPosition()
  } else {
    isOpen.value = false
  }
}

function calculateDropdownPosition() {
  if (!buttonRef.value) return

  const buttonRect = buttonRef.value.getBoundingClientRect()
  const dropdownHeight = 420 // Approximate height of the calendar dropdown
  const viewportHeight = window.innerHeight
  const spaceBelow = viewportHeight - buttonRect.bottom
  const spaceAbove = buttonRect.top

  // Open upward if not enough space below and more space above
  openUpward.value = spaceBelow < dropdownHeight && spaceAbove > spaceBelow

  // Calculate absolute position for teleported dropdown
  dropdownStyle.value = {
    left: `${buttonRect.left}px`,
    width: `320px`,
    top: openUpward.value
      ? `${buttonRect.top - dropdownHeight - 8}px`
      : `${buttonRect.bottom + 8}px`
  }
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as Node
  const clickedInContainer = containerRef.value?.contains(target)
  const clickedInDropdown = dropdownRef.value?.contains(target)

  if (!clickedInContainer && !clickedInDropdown) {
    isOpen.value = false
    showYearPicker.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
  window.addEventListener('scroll', calculateDropdownPosition, true)
  window.addEventListener('resize', calculateDropdownPosition)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
  window.removeEventListener('scroll', calculateDropdownPosition, true)
  window.removeEventListener('resize', calculateDropdownPosition)
})

watch(() => props.modelValue, (newVal) => {
  if (newVal) {
    const date = new Date(newVal)
    currentMonth.value = new Date(date.getFullYear(), date.getMonth(), 1)
  }
})
</script>

<template>
  <div ref="containerRef" class="relative">
    <label v-if="label" class="block text-xs font-medium text-gray-500 dark:text-zinc-400 uppercase tracking-wider mb-1.5">{{ label }}</label>

    <!-- Input Field -->
    <button
      ref="buttonRef"
      type="button"
      :disabled="disabled"
      class="relative w-full bg-white dark:bg-surface-dark border rounded-xl text-left cursor-pointer
             transition-all duration-200 flex items-center justify-between gap-2
             focus:outline-none"
      :class="[
        sizeClasses,
        disabled ? 'bg-gray-50 dark:bg-background-dark cursor-not-allowed opacity-60' : 'hover:border-teal/50',
        error ? 'border-red-300' : 'border-gray-300 dark:border-border-dark',
        isOpen ? 'border-teal' : ''
      ]"
      @click="toggle"
    >
      <div class="flex items-center gap-3">
        <div class="w-8 h-8 rounded-lg bg-teal/10 flex items-center justify-center flex-shrink-0">
          <span class="material-symbols-outlined text-teal text-lg">calendar_month</span>
        </div>
        <span :class="displayValue ? 'text-gray-900 dark:text-white font-medium' : 'text-gray-400 dark:text-zinc-500'">
          {{ displayValue || placeholder }}
        </span>
      </div>
      <div class="flex items-center gap-1">
        <button
          v-if="clearable && displayValue"
          type="button"
          class="p-1 hover:bg-gray-100 dark:hover:bg-border-dark rounded-full transition-colors"
          @click="clear"
        >
          <span class="material-symbols-outlined text-gray-400 dark:text-zinc-500 text-lg">close</span>
        </button>
        <span class="material-symbols-outlined text-gray-400 dark:text-zinc-500 text-lg transition-transform duration-200" :class="isOpen ? 'rotate-180' : ''">expand_more</span>
      </div>
    </button>

    <!-- Error Message -->
    <p v-if="error" class="mt-1 text-sm text-red-600 dark:text-red-400">{{ error }}</p>

    <!-- Calendar Dropdown - Teleported to body to escape overflow containers -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-150 ease-out"
        enter-from-class="transform scale-95 opacity-0"
        enter-to-class="transform scale-100 opacity-100"
        leave-active-class="transition duration-100 ease-in"
        leave-from-class="transform scale-100 opacity-100"
        leave-to-class="transform scale-95 opacity-0"
      >
        <div
          v-if="isOpen"
          ref="dropdownRef"
          class="fixed z-[9999] bg-white dark:bg-surface-dark border border-gray-200 dark:border-border-dark rounded-2xl shadow-2xl p-5"
          :style="{ ...dropdownStyle, transformOrigin: openUpward ? 'bottom center' : 'top center' }"
        >
        <!-- Header with Month/Year Navigation -->
        <div class="flex items-center justify-between mb-4">
          <button
            type="button"
            class="p-2 hover:bg-gray-100 dark:hover:bg-border-dark rounded-xl transition-colors"
            @click="prevMonth"
          >
            <span class="material-symbols-outlined text-gray-600 dark:text-zinc-300">chevron_left</span>
          </button>

          <button
            type="button"
            class="px-4 py-2 hover:bg-gray-100 dark:hover:bg-border-dark rounded-xl transition-colors flex items-center gap-2"
            @click="showYearPicker = !showYearPicker"
          >
            <span class="font-semibold text-gray-900 dark:text-white">
              {{ MONTHS[currentMonth.getMonth()] }} {{ currentMonth.getFullYear() }}
            </span>
            <span class="material-symbols-outlined text-gray-400 dark:text-zinc-500 text-sm transition-transform duration-200" :class="showYearPicker ? 'rotate-180' : ''">expand_more</span>
          </button>

          <button
            type="button"
            class="p-2 hover:bg-gray-100 dark:hover:bg-border-dark rounded-xl transition-colors"
            @click="nextMonth"
          >
            <span class="material-symbols-outlined text-gray-600 dark:text-zinc-300">chevron_right</span>
          </button>
        </div>

        <!-- Year/Month Picker -->
        <Transition
          enter-active-class="transition duration-150 ease-out"
          enter-from-class="opacity-0 -translate-y-2"
          enter-to-class="opacity-100 translate-y-0"
          leave-active-class="transition duration-100 ease-in"
          leave-from-class="opacity-100 translate-y-0"
          leave-to-class="opacity-0 -translate-y-2"
        >
          <div v-if="showYearPicker" class="mb-4 p-3 bg-gray-50 dark:bg-background-dark rounded-xl">
            <!-- Year Navigation -->
            <div class="flex items-center justify-between mb-3">
              <button type="button" class="p-1 hover:bg-gray-200 dark:hover:bg-border-dark rounded-lg" @click="prevYear">
                <span class="material-symbols-outlined text-gray-500 dark:text-zinc-400 text-sm">chevron_left</span>
              </button>
              <span class="text-sm font-medium text-gray-600 dark:text-zinc-400">{{ yearRange[0] }} - {{ yearRange[yearRange.length - 1] }}</span>
              <button type="button" class="p-1 hover:bg-gray-200 dark:hover:bg-border-dark rounded-lg" @click="nextYear">
                <span class="material-symbols-outlined text-gray-500 dark:text-zinc-400 text-sm">chevron_right</span>
              </button>
            </div>
            <!-- Year Grid -->
            <div class="grid grid-cols-4 gap-1 mb-3">
              <button
                v-for="year in yearRange"
                :key="year"
                type="button"
                class="py-1.5 text-sm rounded-lg transition-colors"
                :class="year === currentMonth.getFullYear() ? 'bg-teal text-white font-semibold' : 'text-gray-700 dark:text-zinc-300 hover:bg-gray-200 dark:hover:bg-border-dark'"
                @click="selectYear(year)"
              >
                {{ year }}
              </button>
            </div>
            <!-- Month Grid -->
            <div class="grid grid-cols-4 gap-1">
              <button
                v-for="(month, index) in MONTHS_SHORT"
                :key="month"
                type="button"
                class="py-1.5 text-sm rounded-lg transition-colors"
                :class="index === currentMonth.getMonth() ? 'bg-teal text-white font-semibold' : 'text-gray-700 dark:text-zinc-300 hover:bg-gray-200 dark:hover:bg-border-dark'"
                @click="selectMonth(index)"
              >
                {{ month }}
              </button>
            </div>
          </div>
        </Transition>

        <!-- Day Headers -->
        <div v-if="!showYearPicker" class="grid grid-cols-7 gap-1 mb-2">
          <div
            v-for="day in DAYS"
            :key="day"
            class="text-center text-[10px] font-bold text-gray-400 dark:text-zinc-500 uppercase tracking-wider py-2"
          >
            {{ day }}
          </div>
        </div>

        <!-- Calendar Grid -->
        <div v-if="!showYearPicker" class="grid grid-cols-7 gap-1">
          <button
            v-for="(day, index) in calendarDays"
            :key="index"
            type="button"
            :disabled="day.isDisabled"
            class="aspect-square flex items-center justify-center text-sm rounded-xl transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-teal/50"
            :class="[
              day.isDisabled ? 'text-gray-300 dark:text-zinc-600 cursor-not-allowed' : 'cursor-pointer',
              !day.isCurrentMonth && !day.isSelected ? 'text-gray-400 dark:text-zinc-600' : '',
              day.isCurrentMonth && !day.isSelected && !day.isToday ? 'text-gray-700 dark:text-zinc-300 hover:bg-gray-100 dark:hover:bg-border-dark' : '',
              day.isToday && !day.isSelected ? 'bg-teal/10 text-teal font-bold ring-1 ring-teal/30' : '',
              day.isSelected ? 'bg-teal text-white font-bold shadow-lg shadow-teal/30 hover:bg-teal/90' : ''
            ]"
            @click="selectDate(day)"
          >
            {{ day.date.getDate() }}
          </button>
        </div>

        <!-- Footer -->
        <div class="mt-4 pt-4 border-t border-gray-100 dark:border-border-dark flex items-center justify-between">
          <button
            type="button"
            class="flex items-center gap-2 px-3 py-2 text-sm text-teal hover:bg-teal/10 font-medium rounded-lg transition-colors"
            @click="goToToday"
          >
            <span class="material-symbols-outlined text-lg">today</span>
            Today
          </button>
          <button
            type="button"
            class="px-4 py-2 text-sm text-gray-500 dark:text-zinc-400 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg transition-colors"
            @click="isOpen = false"
          >
            Close
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
  </div>
</template>
