<script setup lang="ts">
import type { TreeNode, Cabinet, Folder } from '@/types'

const props = defineProps<{
  nodes: TreeNode[]
  selectedId?: string
  level?: number
  isChildLevel?: boolean
}>()

const emit = defineEmits<{
  selectCabinet: [cabinet: Cabinet]
  selectFolder: [folder: Folder]
  expandCabinet: [cabinetId: string]
  toggleCabinet: [cabinetId: string]
  contextMenu: [event: MouseEvent, node: TreeNode]
}>()

const currentLevel = props.level ?? 0
const isChildLevel = props.isChildLevel ?? false

function handleNodeClick(node: TreeNode) {
  if (node.type === 'cabinet') {
    emit('selectCabinet', { id: node.id, name: node.name } as Cabinet)
    emit('toggleCabinet', node.id)
  } else {
    emit('selectFolder', { id: node.id, name: node.name, parentFolderId: node.parentId } as Folder)
  }
}

function handleExpandClick(event: MouseEvent, node: TreeNode) {
  event.stopPropagation()
  emit('toggleCabinet', node.id)
}

function handleContextMenu(event: MouseEvent, node: TreeNode) {
  event.preventDefault()
  event.stopPropagation()
  emit('contextMenu', event, node)
}

function renderFolderNode(node: TreeNode): boolean {
  return isChildLevel || node.type === 'folder'
}
</script>

<template>
  <div :class="currentLevel === 0 && !isChildLevel ? 'py-2 px-2' : ''">
    <div v-for="node in nodes" :key="node.id" class="select-none">
      <!-- Cabinet Node (only at root level) -->
      <template v-if="!renderFolderNode(node)">
        <div
          @click="handleNodeClick(node)"
          @contextmenu="handleContextMenu($event, node)"
          class="group flex items-center gap-2.5 py-2 px-2 cursor-pointer rounded-lg transition-all duration-200 mb-0.5 border-l-2"
          :class="selectedId === node.id
            ? 'bg-gray-100 dark:bg-surface-dark/80 border-teal'
            : 'border-transparent hover:bg-gray-50 dark:hover:bg-surface-dark/50'"
        >
          <!-- Expand Arrow -->
          <button
            @click="handleExpandClick($event, node)"
            class="w-5 h-5 flex items-center justify-center rounded transition-all duration-200 flex-shrink-0"
            :class="selectedId === node.id
              ? 'hover:bg-gray-200 dark:hover:bg-border-dark'
              : 'hover:bg-gray-200 dark:hover:bg-border-dark'"
          >
            <svg
              class="w-3.5 h-3.5 transition-transform duration-200"
              :class="[
                node.isExpanded ? 'rotate-0' : '-rotate-90',
                selectedId === node.id ? 'text-gray-600 dark:text-zinc-400' : 'text-gray-400 dark:text-zinc-500'
              ]"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              stroke-width="2.5"
            >
              <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          <!-- Cabinet Icon -->
          <svg
            class="w-5 h-5 flex-shrink-0"
            :class="selectedId === node.id ? 'text-teal' : 'text-gray-400 dark:text-zinc-500'"
            viewBox="0 0 24 24"
            fill="currentColor"
          >
            <path d="M4 4h16a2 2 0 012 2v12a2 2 0 01-2 2H4a2 2 0 01-2-2V6a2 2 0 012-2zm0 2v4h16V6H4zm0 6v6h16v-6H4zm2 1h4v2H6v-2zm0-6h4v2H6V7z"/>
          </svg>

          <!-- Name -->
          <span
            class="text-sm truncate flex-1 transition-colors"
            :class="selectedId === node.id ? 'font-semibold text-gray-900 dark:text-white' : 'font-medium text-gray-600 dark:text-zinc-400'"
          >{{ node.name }}</span>
          <span v-if="node.accessMode === 1" class="material-symbols-outlined text-amber-500 flex-shrink-0" style="font-size: 14px;" title="Private folder">lock</span>

          <!-- Folder Count Badge -->
          <span
            v-if="node.children && node.children.length > 0"
            class="text-[10px] min-w-[18px] h-[18px] px-1.5 rounded font-medium flex-shrink-0 flex items-center justify-center"
            :class="selectedId === node.id
              ? 'bg-teal text-white'
              : 'bg-gray-200 dark:bg-border-dark text-gray-500 dark:text-zinc-400'"
          >{{ node.children.length }}</span>
        </div>

        <!-- Cabinet Children -->
        <div v-if="node.isExpanded && node.children && node.children.length > 0" class="ml-5 mt-0.5">
          <TreeView
            :nodes="node.children"
            :selected-id="selectedId"
            :level="currentLevel + 1"
            :is-child-level="true"
            @select-cabinet="(c) => emit('selectCabinet', c)"
            @select-folder="(f) => emit('selectFolder', f)"
            @expand-cabinet="(id) => emit('expandCabinet', id)"
            @toggle-cabinet="(id) => emit('toggleCabinet', id)"
            @context-menu="(e, n) => emit('contextMenu', e, n)"
          />
        </div>
      </template>

      <!-- Folder Node (at any nested level) -->
      <template v-else>
        <div>
          <div
            @click="handleNodeClick(node)"
            @contextmenu="handleContextMenu($event, node)"
            class="group flex items-center gap-2 py-1.5 px-2 cursor-pointer rounded-md transition-all duration-200 mb-0.5 border-l-2"
            :class="selectedId === node.id
              ? 'bg-gray-100 dark:bg-surface-dark/60 border-teal'
              : 'border-transparent hover:bg-gray-50 dark:hover:bg-surface-dark/40'"
          >
            <!-- Expand Arrow -->
            <button
              v-if="node.children && node.children.length > 0"
              @click="handleExpandClick($event, node)"
              class="w-4 h-4 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-border-dark transition-colors flex-shrink-0"
            >
              <svg
                class="w-3 h-3 text-gray-400 dark:text-zinc-500 transition-transform duration-200"
                :class="node.isExpanded ? 'rotate-0' : '-rotate-90'"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-width="2.5"
              >
                <path stroke-linecap="round" stroke-linejoin="round" d="M19 9l-7 7-7-7" />
              </svg>
            </button>
            <span v-else class="w-4 flex-shrink-0"></span>

            <!-- Folder Icon -->
            <span
              class="material-symbols-outlined text-[18px] flex-shrink-0 transition-colors"
              :class="selectedId === node.id ? 'text-teal' : 'text-gray-400 dark:text-zinc-500'"
            >{{ node.isExpanded ? 'folder_open' : 'folder' }}</span>

            <!-- Name -->
            <span
              class="text-[13px] truncate flex-1 transition-colors"
              :class="selectedId === node.id
                ? 'font-medium text-gray-900 dark:text-white'
                : 'text-gray-600 dark:text-zinc-400'"
            >{{ node.name }}</span>
            <span v-if="node.accessMode === 1" class="material-symbols-outlined text-amber-500 flex-shrink-0" style="font-size: 12px;" title="Private folder">lock</span>

            <!-- Subfolder Count -->
            <span
              v-if="node.children && node.children.length > 0 && !node.isExpanded"
              class="text-[10px] px-1.5 py-0.5 rounded font-medium flex-shrink-0 bg-gray-200 dark:bg-border-dark text-gray-500 dark:text-zinc-400"
            >{{ node.children.length }}</span>
          </div>

          <!-- Nested Children (recursive) -->
          <div v-if="node.isExpanded && node.children && node.children.length > 0" class="ml-5">
            <TreeView
              :nodes="node.children"
              :selected-id="selectedId"
              :level="currentLevel + 1"
              :is-child-level="true"
              @select-cabinet="(c) => emit('selectCabinet', c)"
              @select-folder="(f) => emit('selectFolder', f)"
              @expand-cabinet="(id) => emit('expandCabinet', id)"
              @toggle-cabinet="(id) => emit('toggleCabinet', id)"
              @context-menu="(e, n) => emit('contextMenu', e, n)"
            />
          </div>
        </div>
      </template>
    </div>

    <!-- Empty State -->
    <div v-if="nodes.length === 0 && currentLevel === 0 && !isChildLevel" class="px-4 py-12 text-center">
      <div class="w-14 h-14 mx-auto mb-4 rounded-xl bg-gray-100 dark:bg-surface-dark flex items-center justify-center">
        <svg class="w-7 h-7 text-gray-300 dark:text-zinc-600" viewBox="0 0 24 24" fill="currentColor">
          <path d="M4 4h16a2 2 0 012 2v12a2 2 0 01-2 2H4a2 2 0 01-2-2V6a2 2 0 012-2zm0 2v4h16V6H4zm0 6v6h16v-6H4z"/>
        </svg>
      </div>
      <p class="text-sm font-medium text-gray-500 dark:text-zinc-400">No cabinets yet</p>
      <p class="text-xs text-gray-400 dark:text-zinc-500 mt-1">Create one to get started</p>
    </div>
  </div>
</template>
