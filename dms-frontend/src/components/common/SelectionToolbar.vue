<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const props = defineProps<{
  selectedCount: number
  isProcessing?: boolean
}>()

const emit = defineEmits<{
  'delete-selected': []
  'move-selected': []
  'download-selected': []
  'clear-selection': []
}>()

const isVisible = computed(() => props.selectedCount > 0)

// Check role permissions for bulk actions
const canDownload = computed(() => authStore.hasPermission('document.download'))
const canMove = computed(() => authStore.hasPermission('document.move'))
const canDelete = computed(() => authStore.hasPermission('document.delete'))
</script>

<template>
  <Transition name="slide-up">
    <div
      v-if="isVisible"
      class="fixed bottom-6 left-1/2 -translate-x-1/2 bg-slate-900 text-white rounded-2xl shadow-2xl px-6 py-4 flex items-center gap-6 z-50"
    >
      <!-- Selection Count -->
      <div class="flex items-center gap-2.5 flex-shrink-0">
        <span class="material-symbols-outlined text-teal text-xl">check_circle</span>
        <span class="font-semibold text-sm whitespace-nowrap">{{ selectedCount }} selected</span>
      </div>

      <!-- Divider -->
      <div class="w-px h-8 bg-slate-700"></div>

      <!-- Actions -->
      <div class="flex items-center gap-1">
        <!-- Download -->
        <button
          v-if="canDownload"
          @click="emit('download-selected')"
          :disabled="isProcessing"
          class="flex items-center gap-2 px-4 py-2 hover:bg-slate-800 rounded-xl transition-colors disabled:opacity-50"
          title="Download as ZIP"
        >
          <span class="material-symbols-outlined text-[20px]">download</span>
          <span class="text-sm font-medium">Download</span>
        </button>

        <!-- Move -->
        <button
          v-if="canMove"
          @click="emit('move-selected')"
          :disabled="isProcessing"
          class="flex items-center gap-2 px-4 py-2 hover:bg-slate-800 rounded-xl transition-colors disabled:opacity-50"
          title="Move selected"
        >
          <span class="material-symbols-outlined text-[20px]">drive_file_move</span>
          <span class="text-sm font-medium">Move</span>
        </button>

        <!-- Delete -->
        <button
          v-if="canDelete"
          @click="emit('delete-selected')"
          :disabled="isProcessing"
          class="flex items-center gap-2 px-4 py-2 hover:bg-red-600 rounded-xl transition-colors disabled:opacity-50"
          title="Delete selected"
        >
          <span class="material-symbols-outlined text-[20px]">delete</span>
          <span class="text-sm font-medium">Delete</span>
        </button>
      </div>

      <!-- Divider -->
      <div class="w-px h-8 bg-slate-700"></div>

      <!-- Clear Selection -->
      <button
        @click="emit('clear-selection')"
        :disabled="isProcessing"
        class="p-2 hover:bg-slate-800 rounded-xl transition-colors disabled:opacity-50"
        title="Clear selection"
      >
        <span class="material-symbols-outlined">close</span>
      </button>

      <!-- Processing Indicator -->
      <div v-if="isProcessing" class="absolute inset-0 bg-slate-900/80 rounded-2xl flex items-center justify-center">
        <div class="flex items-center gap-3">
          <svg class="animate-spin w-5 h-5 text-teal" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
          </svg>
          <span class="font-medium">Processing...</span>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.3s ease;
}

.slide-up-enter-from,
.slide-up-leave-to {
  opacity: 0;
  transform: translate(-50%, 20px);
}
</style>
