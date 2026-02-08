<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import Modal from '@/components/ui/Modal.vue'
import type { Cabinet, Folder, TreeNode } from '@/types'
import { cabinetsApi, foldersApi } from '@/api/client'

const props = defineProps<{
  modelValue: boolean
  documentCount: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'confirm': [folderId: string]
}>()

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const cabinets = ref<Cabinet[]>([])
const treeNodes = ref<TreeNode[]>([])
const selectedFolderId = ref<string | null>(null)
const expandedCabinets = ref<Set<string>>(new Set())
const isLoading = ref(false)

onMounted(async () => {
  await loadCabinets()
})

watch(() => props.modelValue, (newVal) => {
  if (newVal) {
    selectedFolderId.value = null
  }
})

async function loadCabinets() {
  isLoading.value = true
  try {
    const response = await cabinetsApi.getAll()
    cabinets.value = response.data
    treeNodes.value = cabinets.value.map(cab => ({
      id: cab.id,
      name: cab.name,
      type: 'cabinet' as const,
      children: [],
      isExpanded: false
    }))
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

async function toggleCabinet(cabinetId: string) {
  const node = treeNodes.value.find(n => n.id === cabinetId)
  if (!node) return

  if (expandedCabinets.value.has(cabinetId)) {
    expandedCabinets.value.delete(cabinetId)
    node.isExpanded = false
  } else {
    expandedCabinets.value.add(cabinetId)
    node.isExpanded = true

    // Load folders if not already loaded
    if (!node.children || node.children.length === 0) {
      node.isLoading = true
      try {
        const response = await foldersApi.getTree(cabinetId)
        node.children = mapFoldersToTreeNodes(response.data)
      } catch (err) {
      } finally {
        node.isLoading = false
      }
    }
  }
}

function mapFoldersToTreeNodes(folders: Folder[]): TreeNode[] {
  return folders.map(folder => ({
    id: folder.id,
    name: folder.name,
    type: 'folder' as const,
    children: folder.children ? mapFoldersToTreeNodes(folder.children) : [],
    isExpanded: false
  }))
}

function toggleFolder(node: TreeNode) {
  node.isExpanded = !node.isExpanded
}

function selectFolder(folderId: string) {
  selectedFolderId.value = folderId
}

function handleConfirm() {
  if (selectedFolderId.value) {
    emit('confirm', selectedFolderId.value)
  }
}

function renderFolderTree(nodes: TreeNode[], level = 0): TreeNode[] {
  return nodes
}
</script>

<template>
  <Modal v-model="isOpen" title="Move Documents" size="md">
    <div class="min-h-[400px]">
      <p class="text-slate-600 mb-4">
        Select a folder to move {{ documentCount }} document{{ documentCount > 1 ? 's' : '' }} to:
      </p>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <svg class="animate-spin w-8 h-8 text-teal" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
        </svg>
      </div>

      <!-- Tree -->
      <div v-else class="border border-slate-200 rounded-lg max-h-[350px] overflow-auto">
        <div class="p-2">
          <!-- Cabinets -->
          <div v-for="cabinet in treeNodes" :key="cabinet.id" class="select-none">
            <div
              @click="toggleCabinet(cabinet.id)"
              class="flex items-center gap-2 py-2 px-2 cursor-pointer rounded-lg hover:bg-slate-100 transition-colors"
            >
              <button class="w-5 h-5 flex items-center justify-center">
                <span
                  class="material-symbols-outlined text-[16px] text-slate-400 transition-transform"
                  :class="cabinet.isExpanded ? '' : '-rotate-90'"
                >expand_more</span>
              </button>
              <span class="material-symbols-outlined text-[20px] text-teal">inventory_2</span>
              <span class="text-sm font-medium text-slate-700">{{ cabinet.name }}</span>
              <svg v-if="cabinet.isLoading" class="animate-spin w-4 h-4 text-teal ml-auto" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
            </div>

            <!-- Folders -->
            <div v-if="cabinet.isExpanded && cabinet.children" class="ml-7">
              <template v-for="folder in cabinet.children" :key="folder.id">
                <div
                  @click.stop="selectFolder(folder.id)"
                  class="flex items-center gap-2 py-1.5 px-2 cursor-pointer rounded-lg transition-colors"
                  :class="selectedFolderId === folder.id
                    ? 'bg-teal/10 ring-2 ring-teal'
                    : 'hover:bg-slate-100'"
                >
                  <button
                    v-if="folder.children && folder.children.length > 0"
                    @click.stop="toggleFolder(folder)"
                    class="w-5 h-5 flex items-center justify-center"
                  >
                    <span
                      class="material-symbols-outlined text-[16px] text-slate-400 transition-transform"
                      :class="folder.isExpanded ? '' : '-rotate-90'"
                    >expand_more</span>
                  </button>
                  <span v-else class="w-5"></span>
                  <span
                    class="material-symbols-outlined text-[18px]"
                    :class="selectedFolderId === folder.id ? 'text-teal' : ''"
                    :style="selectedFolderId !== folder.id ? { color: '#94a3b8' } : {}"
                  >{{ folder.isExpanded ? 'folder_open' : 'folder' }}</span>
                  <span
                    class="text-sm"
                    :class="selectedFolderId === folder.id ? 'font-semibold text-teal' : 'text-slate-600'"
                  >{{ folder.name }}</span>
                </div>

                <!-- Nested folders -->
                <div v-if="folder.isExpanded && folder.children" class="ml-7">
                  <div
                    v-for="subfolder in folder.children"
                    :key="subfolder.id"
                    @click.stop="selectFolder(subfolder.id)"
                    class="flex items-center gap-2 py-1.5 px-2 cursor-pointer rounded-lg transition-colors"
                    :class="selectedFolderId === subfolder.id
                      ? 'bg-teal/10 ring-2 ring-teal'
                      : 'hover:bg-slate-100'"
                  >
                    <span class="w-5"></span>
                    <span
                      class="material-symbols-outlined text-[18px]"
                      :class="selectedFolderId === subfolder.id ? 'text-teal' : ''"
                      :style="selectedFolderId !== subfolder.id ? { color: '#94a3b8' } : {}"
                    >folder</span>
                    <span
                      class="text-sm"
                      :class="selectedFolderId === subfolder.id ? 'font-semibold text-teal' : 'text-slate-600'"
                    >{{ subfolder.name }}</span>
                  </div>
                </div>
              </template>
            </div>
          </div>

          <!-- Empty state -->
          <div v-if="treeNodes.length === 0" class="text-center py-8 text-slate-400">
            <span class="material-symbols-outlined text-3xl mb-2 block">folder_off</span>
            No cabinets available
          </div>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-end gap-3">
        <button
          @click="isOpen = false"
          class="px-4 py-2 border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors"
        >
          Cancel
        </button>
        <button
          @click="handleConfirm"
          :disabled="!selectedFolderId"
          class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed inline-flex items-center gap-2"
        >
          <span class="material-symbols-outlined text-[18px]">drive_file_move</span>
          Move Here
        </button>
      </div>
    </template>
  </Modal>
</template>
