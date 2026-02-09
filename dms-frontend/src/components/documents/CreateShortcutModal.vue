<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import Modal from '@/components/ui/Modal.vue'
import type { Cabinet, Folder, TreeNode, Document } from '@/types'
import { cabinetsApi, foldersApi, documentShortcutsApi } from '@/api/client'

const props = defineProps<{
  modelValue: boolean
  document: Document
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'created': []
}>()

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const treeNodes = ref<TreeNode[]>([])
const selectedFolderId = ref<string | null>(null)
const expandedCabinets = ref<Set<string>>(new Set())
const isLoading = ref(false)
const isCreating = ref(false)
const error = ref('')

onMounted(async () => {
  await loadCabinets()
})

watch(() => props.modelValue, (newVal) => {
  if (newVal) {
    selectedFolderId.value = null
    error.value = ''
  }
})

async function loadCabinets() {
  isLoading.value = true
  try {
    const response = await cabinetsApi.getAll()
    treeNodes.value = response.data.map((cab: Cabinet) => ({
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
  if (folderId === props.document.folderId) return
  selectedFolderId.value = folderId
}

function isSameFolder(folderId: string) {
  return folderId === props.document.folderId
}

async function handleConfirm() {
  if (!selectedFolderId.value) return
  isCreating.value = true
  error.value = ''
  try {
    await documentShortcutsApi.create({
      documentId: props.document.id,
      folderId: selectedFolderId.value
    })
    isOpen.value = false
    emit('created')
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0]
      || err.response?.data?.message
      || 'Failed to create shortcut'
  } finally {
    isCreating.value = false
  }
}
</script>

<template>
  <Modal v-model="isOpen" title="Create Shortcut" size="md">
    <div class="min-h-[400px]">
      <p class="text-zinc-600 dark:text-zinc-400 mb-2">
        Select a folder to place a shortcut to <strong class="text-zinc-800 dark:text-zinc-200">{{ document.name }}</strong>:
      </p>
      <p class="text-xs text-zinc-400 dark:text-zinc-500 mb-4">
        The document stays in its original location. Metadata is shared — any update is reflected everywhere.
      </p>

      <!-- Error -->
      <div v-if="error" class="mb-3 px-3 py-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-600 dark:text-red-400">
        {{ error }}
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <svg class="animate-spin w-8 h-8 text-teal" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
        </svg>
      </div>

      <!-- Tree -->
      <div v-else class="border border-zinc-200 dark:border-border-dark rounded-lg max-h-[350px] overflow-auto">
        <div class="p-2">
          <!-- Cabinets -->
          <div v-for="cabinet in treeNodes" :key="cabinet.id" class="select-none">
            <div
              @click="toggleCabinet(cabinet.id)"
              class="flex items-center gap-2 py-2 px-2 cursor-pointer rounded-lg hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors"
            >
              <button class="w-5 h-5 flex items-center justify-center">
                <span
                  class="material-symbols-outlined text-[16px] text-zinc-400 transition-transform"
                  :class="cabinet.isExpanded ? '' : '-rotate-90'"
                >expand_more</span>
              </button>
              <span class="material-symbols-outlined text-[20px] text-teal">inventory_2</span>
              <span class="text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ cabinet.name }}</span>
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
                  :class="[
                    isSameFolder(folder.id) ? 'opacity-40 cursor-not-allowed' : '',
                    selectedFolderId === folder.id
                      ? 'bg-teal/10 ring-2 ring-teal'
                      : isSameFolder(folder.id) ? '' : 'hover:bg-zinc-100 dark:hover:bg-surface-dark'
                  ]"
                >
                  <button
                    v-if="folder.children && folder.children.length > 0"
                    @click.stop="toggleFolder(folder)"
                    class="w-5 h-5 flex items-center justify-center"
                  >
                    <span
                      class="material-symbols-outlined text-[16px] text-zinc-400 transition-transform"
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
                    :class="selectedFolderId === folder.id ? 'font-semibold text-teal' : 'text-zinc-600 dark:text-zinc-300'"
                  >{{ folder.name }}</span>
                  <span
                    v-if="isSameFolder(folder.id)"
                    class="text-[10px] text-zinc-400 ml-auto"
                  >(current)</span>
                </div>

                <!-- Nested folders -->
                <div v-if="folder.isExpanded && folder.children" class="ml-7">
                  <div
                    v-for="subfolder in folder.children"
                    :key="subfolder.id"
                    @click.stop="selectFolder(subfolder.id)"
                    class="flex items-center gap-2 py-1.5 px-2 cursor-pointer rounded-lg transition-colors"
                    :class="[
                      isSameFolder(subfolder.id) ? 'opacity-40 cursor-not-allowed' : '',
                      selectedFolderId === subfolder.id
                        ? 'bg-teal/10 ring-2 ring-teal'
                        : isSameFolder(subfolder.id) ? '' : 'hover:bg-zinc-100 dark:hover:bg-surface-dark'
                    ]"
                  >
                    <span class="w-5"></span>
                    <span
                      class="material-symbols-outlined text-[18px]"
                      :class="selectedFolderId === subfolder.id ? 'text-teal' : ''"
                      :style="selectedFolderId !== subfolder.id ? { color: '#94a3b8' } : {}"
                    >folder</span>
                    <span
                      class="text-sm"
                      :class="selectedFolderId === subfolder.id ? 'font-semibold text-teal' : 'text-zinc-600 dark:text-zinc-300'"
                    >{{ subfolder.name }}</span>
                    <span
                      v-if="isSameFolder(subfolder.id)"
                      class="text-[10px] text-zinc-400 ml-auto"
                    >(current)</span>
                  </div>
                </div>
              </template>
            </div>
          </div>

          <!-- Empty state -->
          <div v-if="treeNodes.length === 0" class="text-center py-8 text-zinc-400">
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
          class="px-4 py-2 border border-zinc-200 dark:border-border-dark rounded-lg hover:bg-zinc-50 dark:hover:bg-surface-dark transition-colors text-zinc-700 dark:text-zinc-300"
        >
          Cancel
        </button>
        <button
          @click="handleConfirm"
          :disabled="!selectedFolderId || isCreating"
          class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed inline-flex items-center gap-2"
        >
          <svg v-if="isCreating" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
          </svg>
          <span class="material-symbols-outlined text-[18px]" v-else>shortcut</span>
          Create Shortcut
        </button>
      </div>
    </template>
  </Modal>
</template>
