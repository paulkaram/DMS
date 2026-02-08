<script setup lang="ts">
import { computed } from 'vue'
import type { Document } from '@/types'
import DocumentIcon from './DocumentIcon.vue'
import DocumentStatus from './DocumentStatus.vue'
import { formatFileSize, formatRelativeDate } from '@/utils/formatters'
import { UiCheckbox } from '@/components/ui'

const props = defineProps<{
  document: Document
  index?: number
  selected?: boolean
  isFavorite?: boolean
  selectable?: boolean
}>()

const emit = defineEmits<{
  select: [docId: string]
  preview: [doc: Document]
  view: [doc: Document]
  download: [doc: Document]
  share: [doc: Document]
  'context-menu': [event: MouseEvent, doc: Document]
  'version-history': [doc: Document]
}>()

const isSelected = computed({
  get: () => props.selected ?? false,
  set: () => emit('select', props.document.id)
})
</script>

<template>
  <tr
    @contextmenu="emit('context-menu', $event, document)"
    @dblclick="emit('view', document)"
    class="transition-all duration-200 group cursor-pointer border-b border-zinc-100 dark:border-zinc-800 hover:bg-teal/5"
    :class="selected ? 'bg-teal/10 border-l-2 border-l-teal' : 'bg-white dark:bg-zinc-900'"
  >
    <!-- Checkbox -->
    <td v-if="selectable" class="px-5 py-4 w-12" @click.stop>
      <UiCheckbox
        v-model="isSelected"
        size="sm"
      />
    </td>

    <!-- Name -->
    <td class="px-5 py-4">
      <div class="flex items-center gap-3">
        <DocumentIcon
          :extension="document.extension"
          :index="index"
          size="md"
        />
        <div class="min-w-0 flex-1">
          <div class="flex items-center gap-2">
            <p class="text-sm font-medium text-zinc-700 dark:text-zinc-200 group-hover:text-teal transition-colors truncate">
              {{ document.name }}
            </p>
            <!-- Favorite Star - inline with name -->
            <span
              v-if="isFavorite"
              class="material-symbols-outlined text-amber-500 text-sm"
              style="font-variation-settings: 'FILL' 1;"
            >star</span>
            <!-- Password Protected Badge -->
            <div
              v-if="document.hasPassword"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-teal/10 border border-teal/30"
              title="Password protected"
            >
              <span
                class="material-symbols-outlined text-teal"
                style="font-size: 12px; font-variation-settings: 'FILL' 1;"
              >shield_lock</span>
              <span class="text-[9px] font-semibold text-teal uppercase tracking-wide pr-0.5">Secured</span>
            </div>
          </div>
          <span class="text-[10px] text-zinc-400 uppercase tracking-wide">{{ document.extension?.replace('.', '') }}</span>
        </div>
      </div>
    </td>

    <!-- Size -->
    <td class="px-5 py-4">
      <span class="text-sm text-zinc-500 dark:text-zinc-400 tabular-nums">
        {{ formatFileSize(document.size) }}
      </span>
    </td>

    <!-- Created By -->
    <td class="px-5 py-4">
      <span class="text-sm text-zinc-500 dark:text-zinc-400 truncate max-w-[120px] block">
        {{ document.createdByName || '-' }}
      </span>
    </td>

    <!-- Created -->
    <td class="px-5 py-4">
      <span class="text-sm text-zinc-500 dark:text-zinc-400">
        {{ formatRelativeDate(document.createdAt) }}
      </span>
    </td>

    <!-- Content Type -->
    <td class="px-5 py-4">
      <span
        v-if="document.contentTypeName"
        class="inline-flex items-center px-2.5 py-1 bg-teal/10 text-teal rounded-lg text-xs font-medium truncate max-w-[100px]"
        :title="document.contentTypeName"
      >
        {{ document.contentTypeName }}
      </span>
      <span v-else class="text-sm text-zinc-400">-</span>
    </td>

    <!-- Version -->
    <td class="px-5 py-4">
      <span class="inline-flex items-center justify-center min-w-[40px] px-2.5 py-1 bg-[#0d1117] rounded-lg text-xs font-semibold text-white">
        v{{ document.currentMajorVersion || document.currentVersion }}.{{ document.currentMinorVersion || 0 }}
      </span>
    </td>

    <!-- Status -->
    <td class="px-5 py-4">
      <DocumentStatus :document="document" show-icon />
    </td>

    <!-- Actions -->
    <td class="px-5 py-4">
      <div class="flex items-center justify-end gap-0.5 opacity-50 group-hover:opacity-100 transition-opacity">
        <!-- Quick Actions -->
        <button
          @click.stop="emit('preview', document)"
          class="p-2 text-zinc-400 hover:text-teal hover:bg-teal/10 rounded-lg transition-all"
          title="Preview"
        >
          <span class="material-symbols-outlined text-lg">open_in_new</span>
        </button>
        <button
          @click.stop="emit('view', document)"
          class="p-2 text-zinc-400 hover:text-teal hover:bg-teal/10 rounded-lg transition-all"
          title="Details"
        >
          <span class="material-symbols-outlined text-lg">info</span>
        </button>
        <button
          @click.stop="emit('download', document)"
          class="p-2 text-zinc-400 hover:text-teal hover:bg-teal/10 rounded-lg transition-all"
          title="Download"
        >
          <span class="material-symbols-outlined text-lg">download</span>
        </button>
        <!-- More Menu Button -->
        <button
          @click.stop="emit('context-menu', $event, document)"
          class="p-2 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-zinc-700 rounded-lg transition-all"
          title="More actions"
        >
          <span class="material-symbols-outlined text-lg">more_vert</span>
        </button>
      </div>
    </td>
  </tr>
</template>
