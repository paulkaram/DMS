<script setup lang="ts">
import type { FolderTemplateNode, ContentTypeDefinition } from '@/types'

const props = defineProps<{
  node: FolderTemplateNode
  depth?: number
  contentTypes?: ContentTypeDefinition[]
}>()

const emit = defineEmits<{
  edit: [node: FolderTemplateNode]
  delete: [nodeId: string]
  'add-child': [node: FolderTemplateNode]
}>()

function getContentTypeName(id?: string): string {
  if (!id || !props.contentTypes) return ''
  const ct = props.contentTypes.find(c => c.id === id)
  return ct?.name || ''
}
</script>

<template>
  <div>
    <div
      class="flex items-center gap-2 py-2 px-3 rounded-lg hover:bg-zinc-50 dark:hover:bg-zinc-700/50 group"
      :style="{ marginLeft: (depth || 0) * 24 + 'px' }"
    >
      <span class="material-symbols-outlined text-amber-500">folder</span>
      <span class="font-medium text-zinc-700 dark:text-zinc-200">{{ node.name }}</span>
      <span
        v-if="node.contentTypeName || getContentTypeName(node.contentTypeId)"
        class="text-xs bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400 px-2 py-0.5 rounded"
      >
        {{ node.contentTypeName || getContentTypeName(node.contentTypeId) }}
      </span>
      <div class="ml-auto flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
        <button
          @click="emit('add-child', node)"
          class="p-1 text-zinc-400 hover:text-teal rounded"
          title="Add subfolder"
        >
          <span class="material-symbols-outlined text-base">add</span>
        </button>
        <button
          @click="emit('edit', node)"
          class="p-1 text-zinc-400 hover:text-zinc-600 rounded"
          title="Edit"
        >
          <span class="material-symbols-outlined text-base">edit</span>
        </button>
        <button
          @click="emit('delete', node.id)"
          class="p-1 text-zinc-400 hover:text-red-500 rounded"
          title="Delete"
        >
          <span class="material-symbols-outlined text-base">delete</span>
        </button>
      </div>
    </div>
    <template v-if="node.children?.length">
      <FolderTreeNode
        v-for="child in node.children"
        :key="child.id"
        :node="child"
        :depth="(depth || 0) + 1"
        :content-types="contentTypes"
        @edit="emit('edit', $event)"
        @delete="emit('delete', $event)"
        @add-child="emit('add-child', $event)"
      />
    </template>
  </div>
</template>
