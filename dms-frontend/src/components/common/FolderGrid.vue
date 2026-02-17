<script setup lang="ts">
import { ref, computed } from 'vue'
import type { Folder } from '@/types'
import { UiCheckbox } from '@/components/ui'

const props = defineProps<{
  folders: Folder[]
  isLoading?: boolean
  viewMode?: 'list' | 'grid'
  selectable?: boolean
}>()

const emit = defineEmits<{
  'folder-click': [folder: Folder]
  'folder-dblclick': [folder: Folder]
  'context-menu': [event: MouseEvent, folder: Folder]
  'selection-change': [selectedIds: string[]]
}>()

// Selection state
const selectedFolders = ref<Set<string>>(new Set())

const isAllSelected = computed({
  get: () => props.folders.length > 0 && selectedFolders.value.size === props.folders.length,
  set: (value: boolean) => {
    if (value) {
      props.folders.forEach(f => selectedFolders.value.add(f.id))
    } else {
      selectedFolders.value.clear()
    }
    emit('selection-change', Array.from(selectedFolders.value))
  }
})

const isSomeSelected = computed(() =>
  selectedFolders.value.size > 0 && selectedFolders.value.size < props.folders.length
)

function toggleSelect(folderId: string) {
  if (selectedFolders.value.has(folderId)) {
    selectedFolders.value.delete(folderId)
  } else {
    selectedFolders.value.add(folderId)
  }
  emit('selection-change', Array.from(selectedFolders.value))
}

function handleContextMenu(event: MouseEvent, folder: Folder) {
  event.preventDefault()
  emit('context-menu', event, folder)
}

function clearSelection() {
  selectedFolders.value.clear()
  emit('selection-change', [])
}

defineExpose({ clearSelection, selectedFolders })
</script>

<template>
  <div>
    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-8">
      <div class="flex items-center gap-3 text-zinc-500">
        <div class="w-5 h-5 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
        <span class="text-sm">Loading folders...</span>
      </div>
    </div>

    <!-- Empty State -->
    <div v-else-if="folders.length === 0" class="text-center py-8 text-zinc-400">
      <span class="material-symbols-outlined text-4xl mb-2 opacity-50">folder_off</span>
      <p class="text-sm">No subfolders</p>
    </div>

    <!-- Grid View -->
    <div v-else-if="viewMode === 'grid'">
      <!-- Select All Bar -->
      <div v-if="selectable" class="flex items-center gap-3 mb-3 px-1">
        <UiCheckbox
          v-model="isAllSelected"
          :indeterminate="isSomeSelected"
          size="sm"
        />
        <span class="text-xs text-zinc-500 dark:text-zinc-400 select-none">
          {{ selectedFolders.size > 0 ? `${selectedFolders.size} of ${folders.length} selected` : 'Select all' }}
        </span>
      </div>
      <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4">
      <div
        v-for="folder in folders"
        :key="folder.id"
        @click="emit('folder-click', folder)"
        @dblclick="emit('folder-dblclick', folder)"
        @contextmenu="handleContextMenu($event, folder)"
        class="group relative rounded-lg border cursor-pointer transition-all duration-200 hover:shadow-lg overflow-hidden"
        :class="selectedFolders.has(folder.id)
          ? 'border-teal bg-teal/5 ring-2 ring-teal/30 dark:bg-teal/5'
          : 'border-zinc-200 dark:border-border-dark/50 bg-white dark:bg-zinc-900 hover:border-teal/40 dark:hover:border-teal/30'"
      >
        <!-- Top bar: checkbox + action btn -->
        <div class="flex items-center justify-between px-3 pt-3 pb-0">
          <div
            v-if="selectable"
            class="transition-opacity"
            :class="selectedFolders.has(folder.id) ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'"
            @click.stop
          >
            <UiCheckbox
              :model-value="selectedFolders.has(folder.id)"
              @update:model-value="toggleSelect(folder.id)"
              size="sm"
            />
          </div>
          <div v-else class="w-5"></div>
          <button
            @click.stop="handleContextMenu($event, folder)"
            class="w-7 h-7 flex items-center justify-center rounded-lg opacity-0 group-hover:opacity-100 text-zinc-400 hover:text-teal hover:bg-teal/10 dark:hover:bg-teal/10 transition-all"
          >
            <span class="material-symbols-outlined text-[18px]">more_horiz</span>
          </button>
        </div>

        <!-- Folder Icon & Info -->
        <div class="flex flex-col items-center text-center px-4 pb-4 pt-1">
          <div class="w-14 h-14 mb-2.5 flex items-center justify-center rounded-lg bg-gradient-to-br from-teal to-teal/80 shadow-md shadow-teal/20 group-hover:shadow-teal/30 group-hover:scale-105 transition-all duration-200">
            <span class="material-symbols-outlined text-white text-[28px]" style="font-variation-settings: 'FILL' 1;">folder</span>
          </div>
          <p class="text-[13px] font-semibold text-zinc-700 dark:text-zinc-200 truncate w-full group-hover:text-teal transition-colors flex items-center justify-center gap-1">
            {{ folder.name }}
            <span v-if="folder.accessMode === 1" class="material-symbols-outlined text-amber-500" style="font-size: 13px;" title="Private folder">lock</span>
          </p>
          <span
            v-if="folder.privacyLevelName"
            class="inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium mt-0.5"
            :style="{ backgroundColor: (folder.privacyLevelColor || '#6b7280') + '20', color: folder.privacyLevelColor || '#6b7280' }"
            :title="'Privacy: ' + folder.privacyLevelName"
          >
            {{ folder.privacyLevelName }}
          </span>
          <p v-if="folder.description" class="text-[11px] text-zinc-400 dark:text-zinc-500 truncate w-full mt-0.5 leading-tight">
            {{ folder.description }}
          </p>
        </div>
      </div>
      </div>
    </div>

    <!-- List View -->
    <div v-else class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark/50 overflow-hidden">
      <!-- Header -->
      <div class="bg-zinc-50 dark:bg-surface-dark/50 border-b border-zinc-200 dark:border-border-dark/50 px-4 py-2.5">
        <div class="flex items-center gap-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">
          <div v-if="selectable" class="w-8">
            <UiCheckbox
              v-model="isAllSelected"
              :indeterminate="isSomeSelected"
              size="sm"
            />
          </div>
          <div class="flex-1">Folder Name</div>
          <div class="w-32 hidden sm:block">Description</div>
          <div class="w-20 text-right">Actions</div>
        </div>
      </div>

      <!-- Rows -->
      <div class="divide-y divide-zinc-100 dark:divide-zinc-800/50">
        <div
          v-for="folder in folders"
          :key="folder.id"
          @click="emit('folder-click', folder)"
          @dblclick="emit('folder-dblclick', folder)"
          @contextmenu="handleContextMenu($event, folder)"
          class="group flex items-center gap-4 px-4 py-3 cursor-pointer transition-colors"
          :class="selectedFolders.has(folder.id)
            ? 'bg-teal/10 dark:bg-teal/5'
            : 'hover:bg-zinc-50 dark:hover:bg-surface-dark/50'"
        >
          <!-- Checkbox -->
          <div v-if="selectable" class="w-8" @click.stop>
            <UiCheckbox
              :model-value="selectedFolders.has(folder.id)"
              @update:model-value="toggleSelect(folder.id)"
              size="sm"
            />
          </div>

          <!-- Folder Icon & Name -->
          <div class="flex-1 flex items-center gap-3 min-w-0">
            <div class="w-10 h-10 flex items-center justify-center rounded-lg bg-gradient-to-br from-teal to-teal/80 shadow-sm shadow-teal/20">
              <span class="material-symbols-outlined text-white text-xl" style="font-variation-settings: 'FILL' 1;">folder</span>
            </div>
            <div class="min-w-0">
              <p class="text-sm font-medium text-zinc-700 dark:text-zinc-200 truncate group-hover:text-teal transition-colors flex items-center gap-1">
                {{ folder.name }}
                <span v-if="folder.accessMode === 1" class="material-symbols-outlined text-amber-500 text-xs" title="Private folder">lock</span>
                <span
                  v-if="folder.privacyLevelName"
                  class="inline-flex items-center px-1.5 py-0.5 rounded-full text-[10px] font-medium flex-shrink-0"
                  :style="{ backgroundColor: (folder.privacyLevelColor || '#6b7280') + '20', color: folder.privacyLevelColor || '#6b7280' }"
                >
                  {{ folder.privacyLevelName }}
                </span>
              </p>
            </div>
          </div>

          <!-- Description -->
          <div class="w-32 hidden sm:block">
            <p class="text-sm text-zinc-400 dark:text-zinc-500 truncate">{{ folder.description || '-' }}</p>
          </div>

          <!-- Actions -->
          <div class="w-20 flex items-center justify-end gap-0.5">
            <button
              @click.stop="emit('folder-dblclick', folder)"
              class="w-8 h-8 flex items-center justify-center text-zinc-400 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
              title="Open"
            >
              <span class="material-symbols-outlined text-[18px]">arrow_forward</span>
            </button>
            <button
              @click.stop="handleContextMenu($event, folder)"
              class="w-8 h-8 flex items-center justify-center text-zinc-400 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
              title="More"
            >
              <span class="material-symbols-outlined text-[18px]">more_horiz</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Selection Summary -->
    <div
      v-if="selectable && selectedFolders.size > 0"
      class="mt-3 flex items-center gap-2 text-sm text-zinc-500"
    >
      <span class="font-medium text-teal">{{ selectedFolders.size }}</span>
      folder{{ selectedFolders.size === 1 ? '' : 's' }} selected
    </div>
  </div>
</template>
