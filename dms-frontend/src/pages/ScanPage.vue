<script setup lang="ts">
import { ref, onMounted } from 'vue'
import type { Cabinet, Folder, TreeNode } from '@/types'
import { cabinetsApi, foldersApi } from '@/api/client'
import ScanDocumentModal from '@/components/documents/ScanDocumentModal.vue'

// Folder selection state
const cabinets = ref<Cabinet[]>([])
const treeNodes = ref<TreeNode[]>([])
const selectedFolderId = ref<string | null>(null)
const selectedFolderName = ref<string>('')
const expandedNodes = ref<Set<string>>(new Set())
const isLoading = ref(false)

// Scan modal
const showScanModal = ref(false)

onMounted(async () => {
  await loadCabinets()
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
  } catch {
  } finally {
    isLoading.value = false
  }
}

async function toggleNode(node: TreeNode) {
  if (expandedNodes.value.has(node.id)) {
    expandedNodes.value.delete(node.id)
    node.isExpanded = false
  } else {
    expandedNodes.value.add(node.id)
    node.isExpanded = true

    if (node.type === 'cabinet' && (!node.children || node.children.length === 0)) {
      node.isLoading = true
      try {
        const response = await foldersApi.getTree(node.id)
        node.children = mapFoldersToTreeNodes(response.data)
      } catch {
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

function selectFolder(id: string, name: string) {
  selectedFolderId.value = id
  selectedFolderName.value = name
}

function startScan() {
  if (selectedFolderId.value) {
    showScanModal.value = true
  }
}

function handleScanUploaded() {
  showScanModal.value = false
  selectedFolderId.value = null
  selectedFolderName.value = ''
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div>
      <h1 class="text-2xl font-bold text-zinc-900 dark:text-white flex items-center gap-3">
        <div class="w-10 h-10 rounded-xl bg-teal/10 flex items-center justify-center">
          <span class="material-symbols-outlined text-teal text-xl">document_scanner</span>
        </div>
        Scan Document
      </h1>
      <p class="text-zinc-500 text-sm mt-2 ml-[52px]">Capture or import images and create a searchable PDF document</p>
    </div>

    <!-- Folder Selection -->
    <div class="bg-white dark:bg-zinc-900 rounded-2xl border border-zinc-200 dark:border-zinc-800 overflow-hidden shadow-sm">
      <div class="px-6 py-4 border-b border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800/50">
        <h2 class="font-semibold text-zinc-900 dark:text-white flex items-center gap-2">
          <span class="material-symbols-outlined text-teal text-lg">folder_open</span>
          Select Target Folder
        </h2>
        <p class="text-xs text-zinc-500 mt-1">Choose where the scanned document will be saved</p>
      </div>

      <div class="p-6">
        <!-- Loading -->
        <div v-if="isLoading" class="flex items-center justify-center py-12">
          <span class="material-symbols-outlined animate-spin text-3xl text-teal">progress_activity</span>
        </div>

        <!-- Tree -->
        <div v-else class="border border-zinc-200 dark:border-zinc-700 rounded-xl max-h-[400px] overflow-auto">
          <div class="p-2">
            <div v-for="cabinet in treeNodes" :key="cabinet.id" class="select-none">
              <!-- Cabinet -->
              <div
                class="flex items-center gap-2 px-3 py-2 rounded-lg cursor-pointer transition-colors hover:bg-zinc-50 dark:hover:bg-zinc-800"
                @click="toggleNode(cabinet)"
              >
                <span class="material-symbols-outlined text-sm text-zinc-400 transition-transform" :class="{ 'rotate-90': cabinet.isExpanded }">
                  chevron_right
                </span>
                <span class="material-symbols-outlined text-lg text-amber-500">inventory_2</span>
                <span class="text-sm font-medium text-zinc-700 dark:text-zinc-200">{{ cabinet.name }}</span>
                <span v-if="cabinet.isLoading" class="material-symbols-outlined animate-spin text-sm text-zinc-400">progress_activity</span>
              </div>

              <!-- Folders -->
              <div v-if="cabinet.isExpanded && cabinet.children" class="ml-6">
                <template v-for="folder in cabinet.children" :key="folder.id">
                  <div
                    class="flex items-center gap-2 px-3 py-1.5 rounded-lg cursor-pointer transition-colors"
                    :class="selectedFolderId === folder.id
                      ? 'bg-teal/10 text-teal'
                      : 'hover:bg-zinc-50 dark:hover:bg-zinc-800'"
                    @click="selectFolder(folder.id, folder.name)"
                  >
                    <span
                      v-if="folder.children && folder.children.length > 0"
                      class="material-symbols-outlined text-sm text-zinc-400 cursor-pointer transition-transform"
                      :class="{ 'rotate-90': folder.isExpanded }"
                      @click.stop="toggleNode(folder)"
                    >chevron_right</span>
                    <span v-else class="w-5"></span>
                    <span class="material-symbols-outlined text-lg" :class="selectedFolderId === folder.id ? 'text-teal' : 'text-blue-500'">
                      {{ folder.isExpanded ? 'folder_open' : 'folder' }}
                    </span>
                    <span class="text-sm" :class="selectedFolderId === folder.id ? 'font-semibold' : 'text-zinc-700 dark:text-zinc-300'">
                      {{ folder.name }}
                    </span>
                  </div>

                  <!-- Sub-folders -->
                  <div v-if="folder.isExpanded && folder.children" class="ml-6">
                    <div
                      v-for="sub in folder.children"
                      :key="sub.id"
                      class="flex items-center gap-2 px-3 py-1.5 rounded-lg cursor-pointer transition-colors"
                      :class="selectedFolderId === sub.id
                        ? 'bg-teal/10 text-teal'
                        : 'hover:bg-zinc-50 dark:hover:bg-zinc-800'"
                      @click="selectFolder(sub.id, sub.name)"
                    >
                      <span class="w-5"></span>
                      <span class="material-symbols-outlined text-lg" :class="selectedFolderId === sub.id ? 'text-teal' : 'text-blue-400'">folder</span>
                      <span class="text-sm" :class="selectedFolderId === sub.id ? 'font-semibold' : 'text-zinc-600 dark:text-zinc-400'">
                        {{ sub.name }}
                      </span>
                    </div>
                  </div>
                </template>
              </div>
            </div>

            <div v-if="treeNodes.length === 0 && !isLoading" class="text-center py-8 text-zinc-500 text-sm">
              No cabinets found
            </div>
          </div>
        </div>

        <!-- Selected folder display + Start Scan button -->
        <div class="mt-4 flex items-center justify-between">
          <div v-if="selectedFolderId" class="flex items-center gap-2 text-sm">
            <span class="material-symbols-outlined text-teal text-lg">folder</span>
            <span class="text-zinc-600 dark:text-zinc-400">Selected:</span>
            <span class="font-semibold text-zinc-900 dark:text-white">{{ selectedFolderName }}</span>
          </div>
          <div v-else class="text-sm text-zinc-400">No folder selected</div>

          <button
            @click="startScan"
            :disabled="!selectedFolderId"
            class="px-6 py-2.5 bg-teal text-white font-medium rounded-xl hover:bg-teal/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors inline-flex items-center gap-2 shadow-sm"
          >
            <span class="material-symbols-outlined text-xl">document_scanner</span>
            Start Scanning
          </button>
        </div>
      </div>
    </div>

    <!-- Scan Modal -->
    <ScanDocumentModal
      v-if="showScanModal && selectedFolderId"
      :folder-id="selectedFolderId"
      :folder-name="selectedFolderName"
      @close="showScanModal = false"
      @uploaded="handleScanUploaded"
    />
  </div>
</template>
