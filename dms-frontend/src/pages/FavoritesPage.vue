<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import type { FavoriteItem, Document } from '@/types'
import { favoritesApi, documentsApi } from '@/api/client'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'
import DocumentIcon from '@/components/common/DocumentIcon.vue'

const router = useRouter()
const favorites = ref<FavoriteItem[]>([])
const isLoading = ref(true)

// Document Preview
const showPreview = ref(false)
const previewDocument = ref<Document | null>(null)

async function openPreview(item: FavoriteItem) {
  closeContextMenu()
  if (item.nodeType !== 3) return // Only documents can be previewed
  try {
    const response = await documentsApi.getById(item.nodeId)
    previewDocument.value = response.data
    showPreview.value = true
  } catch (err) {
  }
}

// Context menu state
const contextMenu = ref<{ show: boolean; x: number; y: number; item: FavoriteItem | null }>({
  show: false,
  x: 0,
  y: 0,
  item: null
})

function getExtension(name: string): string {
  const parts = name.split('.')
  return parts.length > 1 ? parts.pop()! : ''
}

onMounted(async () => {
  await loadFavorites()
  document.addEventListener('click', closeContextMenu)
})

onUnmounted(() => {
  document.removeEventListener('click', closeContextMenu)
})

async function loadFavorites() {
  isLoading.value = true
  try {
    const response = await favoritesApi.getMyFavorites()
    favorites.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function showContextMenu(event: MouseEvent, item: FavoriteItem) {
  event.preventDefault()
  event.stopPropagation()
  contextMenu.value = {
    show: true,
    x: event.clientX,
    y: event.clientY,
    item
  }
}

function closeContextMenu() {
  contextMenu.value.show = false
}

// Open file - navigate to document details or open cabinet/folder
function openFile(item: FavoriteItem) {
  closeContextMenu()
  switch (item.nodeType) {
    case 1: // Cabinet
      router.push({ name: 'explorer', query: { cabinetId: item.nodeId } })
      break
    case 2: // Folder
      router.push({ name: 'explorer', query: { folderId: item.nodeId } })
      break
    case 3: // Document
      router.push(`/documents/${item.nodeId}`)
      break
  }
}

// Go to location - navigate to the containing folder
function goToLocation(item: FavoriteItem) {
  closeContextMenu()
  if (item.parentFolderId) {
    router.push({ name: 'explorer', query: { folderId: item.parentFolderId } })
  } else if (item.cabinetId) {
    router.push({ name: 'explorer', query: { cabinetId: item.cabinetId } })
  }
}

// Download document
async function downloadFile(item: FavoriteItem) {
  closeContextMenu()
  if (item.nodeType !== 3) return // Only documents can be downloaded

  try {
    const response = await documentsApi.download(item.nodeId)
    const blob = response.data
    const url = window.URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = item.name || 'download'
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch (err) {
  }
}

async function removeFavorite(item: FavoriteItem) {
  closeContextMenu()
  try {
    await favoritesApi.remove(item.nodeType, item.nodeId)
    favorites.value = favorites.value.filter(f => f.id !== item.id)
  } catch (err) {
  }
}

function getNodeTypeName(nodeType: number): string {
  switch (nodeType) {
    case 1: return 'Cabinet'
    case 2: return 'Folder'
    case 3: return 'Document'
    default: return 'Unknown'
  }
}

function getMaterialIcon(nodeType: number): string {
  switch (nodeType) {
    case 1: return 'inventory_2'
    case 2: return 'folder'
    case 3: return 'description'
    default: return 'help'
  }
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays} days ago`

  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Favorites</h1>
        <p class="text-zinc-500 mt-1">Quick access to your favorite items</p>
      </div>
      <button
        @click="loadFavorites"
        class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-surface-dark hover:bg-zinc-50 dark:hover:bg-border-dark text-zinc-700 dark:text-zinc-300 rounded-xl font-medium text-sm transition-colors border border-zinc-200 dark:border-border-dark"
      >
        <span class="material-symbols-outlined text-lg">refresh</span>
        Refresh
      </button>
    </div>

    <!-- Stats Card -->
    <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 max-w-xs relative overflow-hidden">
      <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
        <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
      </svg>
      <div class="flex items-center justify-between relative z-10">
        <span class="material-symbols-outlined text-teal">star</span>
        <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Favorites</span>
      </div>
      <div class="mt-2 relative z-10">
        <p class="text-4xl font-bold">{{ favorites.length }}</p>
        <p class="text-[10px] text-teal mt-2 font-medium">Quick access items</p>
      </div>
    </div>

    <!-- Main Content -->
    <div class="bg-white dark:bg-background-dark rounded-2xl shadow-sm border border-zinc-200 dark:border-border-dark overflow-hidden">
      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <div class="animate-spin w-8 h-8 border-4 border-teal border-t-transparent rounded-full"></div>
      </div>

      <!-- Empty State -->
      <div v-else-if="favorites.length === 0" class="text-center py-12">
        <div class="w-20 h-20 rounded-2xl bg-zinc-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
          <span class="material-symbols-outlined text-5xl text-zinc-400">star</span>
        </div>
        <h3 class="text-lg font-semibold text-zinc-700 dark:text-zinc-300">No favorites yet</h3>
        <p class="text-zinc-500 mt-1">Star items to add them to your favorites</p>
        <router-link
          to="/explorer"
          class="inline-flex items-center gap-2 mt-4 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-xl font-medium text-sm transition-colors"
        >
          <span class="material-symbols-outlined text-lg">explore</span>
          Go to Explorer
        </router-link>
      </div>

      <!-- Favorites List -->
      <div v-else class="p-6 space-y-2">
        <div
          v-for="(item, index) in favorites"
          :key="item.id"
          @contextmenu="showContextMenu($event, item)"
          @dblclick="openFile(item)"
          class="flex items-center gap-4 p-4 bg-zinc-50 dark:bg-surface-dark rounded-xl border border-zinc-100 dark:border-border-dark hover:border-teal/30 hover:shadow-md transition-all cursor-pointer"
        >
          <!-- Avatar with alternating colors -->
          <DocumentIcon
            v-if="item.nodeType === 3"
            :extension="getExtension(item.name)"
            :index="index"
            size="lg"
          />
          <DocumentIcon
            v-else
            :icon="getMaterialIcon(item.nodeType)"
            :index="index"
            size="lg"
          />

          <!-- Info -->
          <div class="flex-1 min-w-0 cursor-pointer" @click="openFile(item)">
            <p class="font-medium text-zinc-900 dark:text-white truncate">{{ item.name }}</p>
            <div class="flex items-center gap-3 mt-1 text-sm text-zinc-500">
              <span>{{ getNodeTypeName(item.nodeType) }}</span>
              <span v-if="item.path">•</span>
              <span v-if="item.path" class="truncate">{{ item.path }}</span>
            </div>
          </div>

          <!-- Date -->
          <div class="text-sm text-zinc-500 dark:text-zinc-400 flex-shrink-0 mr-2 hidden sm:block">
            {{ formatDate(item.favoritedAt) }}
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-1 flex-shrink-0">
            <!-- Preview (documents only) -->
            <div v-if="item.nodeType === 3" class="relative group">
              <button
                @click.stop="openPreview(item)"
                class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-xl">open_in_new</span>
              </button>
              <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                Preview
              </span>
            </div>
            <!-- Details / Open -->
            <div class="relative group">
              <button
                @click.stop="openFile(item)"
                class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-xl">info</span>
              </button>
              <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                {{ item.nodeType === 3 ? 'Details' : 'Open' }}
              </span>
            </div>
            <div v-if="item.nodeType !== 1 && (item.parentFolderId || item.cabinetId)" class="relative group">
              <button
                @click.stop="goToLocation(item)"
                class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-xl">folder_open</span>
              </button>
              <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                Go to Location
              </span>
            </div>
            <div v-if="item.nodeType === 3" class="relative group">
              <button
                @click.stop="downloadFile(item)"
                class="p-2 text-zinc-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-xl">download</span>
              </button>
              <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                Download
              </span>
            </div>
            <div class="relative group">
              <button
                @click.stop="removeFavorite(item)"
                class="p-2 text-zinc-500 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
              >
                <span class="material-symbols-outlined text-xl">delete</span>
              </button>
              <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-zinc-800 dark:bg-border-dark rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                Remove
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Context Menu -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-100 ease-out"
        enter-from-class="opacity-0 scale-95"
        enter-to-class="opacity-100 scale-100"
        leave-active-class="transition duration-75 ease-in"
        leave-from-class="opacity-100 scale-100"
        leave-to-class="opacity-0 scale-95"
      >
        <div
          v-if="contextMenu.show && contextMenu.item"
          class="fixed z-[9999] bg-white dark:bg-surface-dark rounded-xl shadow-xl border border-gray-200 dark:border-border-dark py-1.5 min-w-[180px]"
          :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }"
          @click.stop
        >
          <!-- Preview (documents only) -->
          <button
            v-if="contextMenu.item!.nodeType === 3"
            @click="openPreview(contextMenu.item!)"
            class="w-full flex items-center gap-3 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
          >
            <span class="material-symbols-outlined text-lg text-teal">open_in_new</span>
            Preview
          </button>

          <!-- Open / Details -->
          <button
            @click="openFile(contextMenu.item!)"
            class="w-full flex items-center gap-3 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
          >
            <span class="material-symbols-outlined text-lg text-blue-500">info</span>
            {{ contextMenu.item!.nodeType === 3 ? 'View details' : 'Open ' + getNodeTypeName(contextMenu.item!.nodeType).toLowerCase() }}
          </button>

          <!-- Go to Location (for documents and folders) -->
          <button
            v-if="contextMenu.item!.nodeType !== 1 && (contextMenu.item!.parentFolderId || contextMenu.item!.cabinetId)"
            @click="goToLocation(contextMenu.item!)"
            class="w-full flex items-center gap-3 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
          >
            <span class="material-symbols-outlined text-lg text-purple-500">folder_open</span>
            Go to location
          </button>

          <!-- Download (only for documents) -->
          <button
            v-if="contextMenu.item!.nodeType === 3"
            @click="downloadFile(contextMenu.item!)"
            class="w-full flex items-center gap-3 px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
          >
            <span class="material-symbols-outlined text-lg text-green-500">download</span>
            Download
          </button>

          <div class="border-t border-gray-100 dark:border-border-dark my-1"></div>

          <!-- Remove from favorites -->
          <button
            @click="removeFavorite(contextMenu.item!)"
            class="w-full flex items-center gap-3 px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
          >
            <span class="material-symbols-outlined text-lg">delete</span>
            Remove from favorites
          </button>
        </div>
      </Transition>
    </Teleport>

    <!-- Document Viewer -->
    <DocumentViewer
      v-model="showPreview"
      :document="previewDocument"
    />
  </div>
</template>
