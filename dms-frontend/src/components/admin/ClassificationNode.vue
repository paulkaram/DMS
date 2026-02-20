<script setup lang="ts">
import type { Classification } from '@/types'

const props = defineProps<{
  node: Classification
  depth: number
  expandedNodes: Set<string>
}>()

const emit = defineEmits<{
  toggle: [id: string]
  edit: [node: Classification]
  delete: [node: Classification]
  'add-child': [parentId: string]
}>()

function hasChildren(): boolean {
  return (props.node.children?.length ?? 0) > 0
}

function isExpanded(): boolean {
  return props.expandedNodes.has(props.node.id)
}

function getConfidentialityColor(level?: string): string {
  switch (level) {
    case 'Public': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
    case 'Internal': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
    case 'Confidential': return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'
    case 'Secret': return 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400'
    case 'TopSecret': return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
    default: return ''
  }
}
</script>

<template>
  <div>
    <div
      class="group flex items-center gap-4 px-4 py-2.5 rounded-lg hover:bg-gray-50 dark:hover:bg-surface-dark/50 transition-colors"
      :style="{ paddingLeft: (depth * 24 + 16) + 'px' }"
    >
      <!-- Expand/Collapse -->
      <button
        v-if="hasChildren()"
        @click="emit('toggle', node.id)"
        class="w-5 h-5 flex items-center justify-center text-gray-400 hover:text-gray-600 transition-colors"
      >
        <span class="material-symbols-outlined text-lg transition-transform" :class="isExpanded() ? 'rotate-90' : ''">chevron_right</span>
      </button>
      <span v-else class="w-5"></span>

      <!-- Icon -->
      <div class="w-7 h-7 rounded-md flex items-center justify-center flex-shrink-0"
        :class="node.level === 0 ? 'bg-teal/15 dark:bg-teal/15' : 'bg-gray-100 dark:bg-zinc-700'"
      >
        <span class="material-symbols-outlined text-sm"
          :class="node.level === 0 ? 'text-teal' : 'text-gray-500 dark:text-gray-400'"
        >{{ hasChildren() ? 'folder_open' : 'label' }}</span>
      </div>

      <!-- Name -->
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2">
          <span class="text-sm font-medium text-gray-900 dark:text-white truncate">{{ node.name }}</span>
          <span v-if="!node.isActive" class="px-1.5 py-0.5 text-[10px] font-bold text-gray-400 bg-gray-100 dark:bg-zinc-700 rounded">Inactive</span>
        </div>
        <p v-if="node.description" class="text-xs text-gray-400 truncate">{{ node.description }}</p>
      </div>

      <!-- Code -->
      <div class="w-24 text-center">
        <code v-if="node.code" class="text-xs text-gray-500 bg-gray-100 dark:bg-zinc-700 dark:text-gray-400 px-2 py-0.5 rounded">{{ node.code }}</code>
      </div>

      <!-- Confidentiality -->
      <div class="w-28 text-center">
        <span
          v-if="node.confidentialityLevel"
          :class="['px-2 py-0.5 text-xs font-medium rounded-full', getConfidentialityColor(node.confidentialityLevel)]"
        >{{ node.confidentialityLevel }}</span>
      </div>

      <!-- Disposal Approval -->
      <div class="w-20 text-center">
        <span v-if="node.requiresDisposalApproval" class="material-symbols-outlined text-lg text-amber-500" title="Requires disposal approval">verified</span>
      </div>

      <!-- Status -->
      <div class="w-16 text-center">
        <span class="inline-block w-2 h-2 rounded-full" :class="node.isActive ? 'bg-green-500' : 'bg-gray-300'"></span>
      </div>

      <!-- Actions -->
      <div class="w-28 flex items-center justify-end gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity">
        <button
          @click="emit('add-child', node.id)"
          class="p-1.5 text-gray-400 hover:text-teal hover:bg-teal/10 dark:hover:bg-teal/10 rounded-md transition-colors"
          title="Add child"
        >
          <span class="material-symbols-outlined text-base">add</span>
        </button>
        <button
          @click="emit('edit', node)"
          class="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-md transition-colors"
          title="Edit"
        >
          <span class="material-symbols-outlined text-base">edit</span>
        </button>
        <button
          @click="emit('delete', node)"
          class="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-md transition-colors"
          title="Delete"
        >
          <span class="material-symbols-outlined text-base">delete</span>
        </button>
      </div>
    </div>

    <!-- Children (recursive) -->
    <template v-if="hasChildren() && isExpanded()">
      <ClassificationNode
        v-for="child in node.children"
        :key="child.id"
        :node="child"
        :depth="depth + 1"
        :expanded-nodes="expandedNodes"
        @toggle="(id: string) => emit('toggle', id)"
        @edit="(c: Classification) => emit('edit', c)"
        @delete="(c: Classification) => emit('delete', c)"
        @add-child="(id: string) => emit('add-child', id)"
      />
    </template>
  </div>
</template>
