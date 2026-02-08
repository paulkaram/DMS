<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  currentPage: number
  totalPages: number
  totalItems: number
  itemsPerPage: number
}>()

const emit = defineEmits<{
  'page-change': [page: number]
}>()

const pages = computed(() => {
  const result: (number | string)[] = []
  const total = props.totalPages

  if (total <= 7) {
    for (let i = 1; i <= total; i++) result.push(i)
  } else {
    if (props.currentPage <= 4) {
      for (let i = 1; i <= 5; i++) result.push(i)
      result.push('...')
      result.push(total)
    } else if (props.currentPage >= total - 3) {
      result.push(1)
      result.push('...')
      for (let i = total - 4; i <= total; i++) result.push(i)
    } else {
      result.push(1)
      result.push('...')
      for (let i = props.currentPage - 1; i <= props.currentPage + 1; i++) result.push(i)
      result.push('...')
      result.push(total)
    }
  }

  return result
})

const startItem = computed(() => (props.currentPage - 1) * props.itemsPerPage + 1)
const endItem = computed(() => Math.min(props.currentPage * props.itemsPerPage, props.totalItems))

function goToPage(page: number | string) {
  if (typeof page === 'number' && page >= 1 && page <= props.totalPages) {
    emit('page-change', page)
  }
}
</script>

<template>
  <div class="flex items-center justify-between">
    <!-- Item count -->
    <p class="text-sm text-zinc-600 dark:text-zinc-400">
      Showing <span class="font-semibold text-zinc-900 dark:text-zinc-100">{{ startItem }}</span>
      to <span class="font-semibold text-zinc-900 dark:text-zinc-100">{{ endItem }}</span>
      of <span class="font-semibold text-zinc-900 dark:text-zinc-100">{{ totalItems }}</span>
    </p>

    <!-- Pagination controls -->
    <div class="flex items-center gap-1">
      <!-- Previous -->
      <button
        @click="goToPage(currentPage - 1)"
        :disabled="currentPage === 1"
        class="p-2 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 disabled:opacity-30 disabled:cursor-not-allowed rounded-lg hover:bg-zinc-100 dark:hover:bg-zinc-700 transition-colors"
      >
        <span class="material-symbols-outlined text-lg">chevron_left</span>
      </button>

      <!-- Page numbers -->
      <template v-for="page in pages" :key="page">
        <span v-if="page === '...'" class="px-2 text-zinc-400">...</span>
        <button
          v-else
          @click="goToPage(page)"
          class="min-w-[36px] h-9 px-3 rounded-lg text-sm font-medium transition-colors"
          :class="page === currentPage
            ? 'bg-gradient-to-r from-navy to-teal text-white shadow-md'
            : 'text-zinc-600 dark:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-zinc-700'"
        >
          {{ page }}
        </button>
      </template>

      <!-- Next -->
      <button
        @click="goToPage(currentPage + 1)"
        :disabled="currentPage === totalPages"
        class="p-2 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 disabled:opacity-30 disabled:cursor-not-allowed rounded-lg hover:bg-zinc-100 dark:hover:bg-zinc-700 transition-colors"
      >
        <span class="material-symbols-outlined text-lg">chevron_right</span>
      </button>
    </div>
  </div>
</template>
