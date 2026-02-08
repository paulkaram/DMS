<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import VuePdfEmbed from 'vue-pdf-embed'
import type { ManagedPage, PageReorganizeResult } from '@/types'
import { pdfPagesApi } from '@/api/client'
import * as pdfjsLib from 'pdfjs-dist'

const props = defineProps<{
  documentId: string
  pdfSource: string
  totalPages: number
  isDarkMode: boolean
}>()

const emit = defineEmits<{
  close: []
  applied: [result: PageReorganizeResult]
}>()

// ── State ──────────────────────────────────────────
const managedPages = ref<ManagedPage[]>([])
const originalSnapshot = ref<string>('')
const uploadedFiles = ref<File[]>([])
const uploadBlobUrls = ref<Map<number, string>>(new Map())

const isApplying = ref(false)
const applyProgress = ref(0)
const errorMessage = ref('')

// Drag state
const draggedIndex = ref<number | null>(null)
const dragOverIndex = ref<number | null>(null)
const isDragging = ref(false)

// File input ref
const fileInputRef = ref<HTMLInputElement | null>(null)

// ── Initialization ─────────────────────────────────
function initializePages() {
  const pages: ManagedPage[] = []
  for (let i = 1; i <= props.totalPages; i++) {
    pages.push({
      id: crypto.randomUUID(),
      source: 'existing',
      originalPage: i,
      label: `Page ${i}`
    })
  }
  managedPages.value = pages
  originalSnapshot.value = JSON.stringify(pages.map(p => `${p.source}:${p.originalPage}`))
}

onMounted(() => {
  initializePages()
})

// Cleanup blob URLs on unmount
onUnmounted(() => {
  for (const url of uploadBlobUrls.value.values()) {
    URL.revokeObjectURL(url)
  }
})

// ── Change Detection ───────────────────────────────
const hasChanges = computed(() => {
  const current = JSON.stringify(managedPages.value.map(p => `${p.source}:${p.originalPage ?? ''}:${p.fileIndex ?? ''}:${p.uploadPageNumber ?? ''}`))
  return current !== originalSnapshot.value || uploadedFiles.value.length > 0
})

const canApply = computed(() => hasChanges.value && managedPages.value.length > 0 && !isApplying.value)

// ── Drag & Drop ────────────────────────────────────
function onDragStart(index: number, e: DragEvent) {
  draggedIndex.value = index
  isDragging.value = true
  if (e.dataTransfer) {
    e.dataTransfer.effectAllowed = 'move'
    e.dataTransfer.setData('text/plain', String(index))
  }
}

function onDragOver(index: number, e: DragEvent) {
  e.preventDefault()
  if (e.dataTransfer) e.dataTransfer.dropEffect = 'move'
  dragOverIndex.value = index
}

function onDragLeave() {
  dragOverIndex.value = null
}

function onDrop(index: number, e: DragEvent) {
  e.preventDefault()
  if (draggedIndex.value !== null && draggedIndex.value !== index) {
    const pages = [...managedPages.value]
    const [moved] = pages.splice(draggedIndex.value, 1)
    pages.splice(index, 0, moved)
    managedPages.value = pages
  }
  draggedIndex.value = null
  dragOverIndex.value = null
  isDragging.value = false
}

function onDragEnd() {
  draggedIndex.value = null
  dragOverIndex.value = null
  isDragging.value = false
}

// ── Delete ─────────────────────────────────────────
function deletePage(index: number) {
  if (managedPages.value.length <= 1) return
  managedPages.value.splice(index, 1)
}

// ── Add Pages ──────────────────────────────────────
function triggerAddFiles() {
  fileInputRef.value?.click()
}

async function handleFileSelect(e: Event) {
  const input = e.target as HTMLInputElement
  if (!input.files || input.files.length === 0) return

  for (const file of Array.from(input.files)) {
    const fileIndex = uploadedFiles.value.length
    uploadedFiles.value.push(file)

    if (file.type === 'application/pdf') {
      // Get page count from PDF using pdfjs-dist
      try {
        const arrayBuffer = await file.arrayBuffer()
        const pdf = await pdfjsLib.getDocument({ data: arrayBuffer }).promise
        const blobUrl = URL.createObjectURL(file)
        uploadBlobUrls.value.set(fileIndex, blobUrl)

        for (let p = 1; p <= pdf.numPages; p++) {
          managedPages.value.push({
            id: crypto.randomUUID(),
            source: 'upload',
            fileIndex,
            uploadPageNumber: p,
            label: pdf.numPages > 1 ? `${file.name} (p${p})` : file.name,
            thumbnailUrl: blobUrl
          })
        }
      } catch {
        errorMessage.value = `Failed to read PDF: ${file.name}`
      }
    } else if (file.type.startsWith('image/')) {
      const blobUrl = URL.createObjectURL(file)
      uploadBlobUrls.value.set(fileIndex, blobUrl)

      managedPages.value.push({
        id: crypto.randomUUID(),
        source: 'upload',
        fileIndex,
        label: file.name,
        thumbnailUrl: blobUrl
      })
    }
  }

  // Reset input so the same files can be re-selected
  input.value = ''
}

// ── Move Up / Down ─────────────────────────────────
function moveUp(index: number) {
  if (index <= 0) return
  const pages = [...managedPages.value]
  ;[pages[index - 1], pages[index]] = [pages[index], pages[index - 1]]
  managedPages.value = pages
}

function moveDown(index: number) {
  if (index >= managedPages.value.length - 1) return
  const pages = [...managedPages.value]
  ;[pages[index], pages[index + 1]] = [pages[index + 1], pages[index]]
  managedPages.value = pages
}

// ── Apply Changes ──────────────────────────────────
async function applyChanges() {
  if (!canApply.value) return
  isApplying.value = true
  applyProgress.value = 0
  errorMessage.value = ''

  try {
    const manifest = {
      pages: managedPages.value.map(p => ({
        source: p.source,
        originalPage: p.originalPage,
        fileIndex: p.fileIndex,
        uploadPageNumber: p.uploadPageNumber
      })),
      comment: 'Pages reorganized'
    }

    const files = uploadedFiles.value.length > 0 ? uploadedFiles.value : undefined
    const response = await pdfPagesApi.reorganize(
      props.documentId,
      manifest,
      files,
      (progress) => { applyProgress.value = progress }
    )

    emit('applied', response.data)
  } catch (err: any) {
    errorMessage.value = err.response?.data?.[0] || err.message || 'Failed to apply changes'
  } finally {
    isApplying.value = false
  }
}

// ── Reset ──────────────────────────────────────────
function resetChanges() {
  for (const url of uploadBlobUrls.value.values()) {
    URL.revokeObjectURL(url)
  }
  uploadedFiles.value = []
  uploadBlobUrls.value = new Map()
  errorMessage.value = ''
  initializePages()
}
</script>

<template>
  <aside
    class="w-72 shrink-0 flex flex-col border-l overflow-hidden select-none"
    :class="isDarkMode ? 'bg-zinc-900 border-zinc-700' : 'bg-white border-zinc-200'"
  >
    <!-- ─── Header ─────────────────────────────── -->
    <div
      class="px-3.5 py-3 border-b shrink-0"
      :class="isDarkMode ? 'border-zinc-700 bg-zinc-800' : 'border-zinc-200 bg-zinc-50'"
    >
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2.5 min-w-0">
          <div class="w-7 h-7 rounded-lg flex items-center justify-center shrink-0 bg-teal/10">
            <span class="material-symbols-outlined text-sm text-teal">auto_stories</span>
          </div>
          <div class="min-w-0">
            <h3
              class="font-semibold text-sm truncate"
              :class="isDarkMode ? 'text-white' : 'text-zinc-900'"
            >Pages</h3>
            <p class="text-[11px] text-zinc-500 truncate leading-tight">
              {{ managedPages.length }} page{{ managedPages.length !== 1 ? 's' : '' }}
              <span v-if="hasChanges" class="text-amber-500 font-medium"> · modified</span>
            </p>
          </div>
        </div>
        <button
          @click="$emit('close')"
          class="p-1 rounded-lg transition-colors shrink-0"
          :class="isDarkMode ? 'text-zinc-400 hover:text-zinc-200 hover:bg-zinc-700' : 'text-zinc-400 hover:text-zinc-600 hover:bg-zinc-100'"
        >
          <span class="material-symbols-outlined text-lg">close</span>
        </button>
      </div>
    </div>

    <!-- ─── Add Pages Bar ──────────────────────── -->
    <div
      class="px-3 py-2 border-b flex items-center gap-2 shrink-0"
      :class="isDarkMode ? 'border-zinc-700' : 'border-zinc-200'"
    >
      <button
        @click="triggerAddFiles"
        class="flex-1 flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg border border-dashed transition-colors"
        :class="isDarkMode
          ? 'border-zinc-600 text-zinc-300 hover:border-teal hover:text-teal hover:bg-teal/5'
          : 'border-zinc-300 text-zinc-600 hover:border-teal hover:text-teal hover:bg-teal/5'"
      >
        <span class="material-symbols-outlined text-sm">add_circle</span>
        Add Pages
      </button>
      <button
        v-if="hasChanges"
        @click="resetChanges"
        class="p-1.5 rounded-lg transition-colors"
        :class="isDarkMode ? 'text-zinc-500 hover:text-zinc-300 hover:bg-zinc-700' : 'text-zinc-400 hover:text-zinc-600 hover:bg-zinc-100'"
        title="Reset changes"
      >
        <span class="material-symbols-outlined text-sm">restart_alt</span>
      </button>
      <input
        ref="fileInputRef"
        type="file"
        accept="image/jpeg,image/png,image/tiff,image/bmp,image/gif,image/webp,.pdf"
        multiple
        hidden
        @change="handleFileSelect"
      />
    </div>

    <!-- ─── Page List ──────────────────────────── -->
    <div class="flex-1 overflow-y-auto overflow-x-hidden py-2 px-2">
      <TransitionGroup
        name="page-list"
        tag="div"
        class="space-y-1.5"
      >
        <div
          v-for="(page, index) in managedPages"
          :key="page.id"
          draggable="true"
          @dragstart="onDragStart(index, $event)"
          @dragover="onDragOver(index, $event)"
          @dragleave="onDragLeave"
          @drop="onDrop(index, $event)"
          @dragend="onDragEnd"
          class="group relative flex items-center gap-2 p-1.5 rounded-xl border transition-all duration-150 cursor-grab active:cursor-grabbing"
          :class="[
            draggedIndex === index ? 'opacity-40 scale-95' : '',
            dragOverIndex === index && draggedIndex !== index
              ? 'border-teal bg-teal/5 shadow-sm shadow-teal/10'
              : isDarkMode
                ? 'border-zinc-700/50 hover:border-zinc-600 bg-zinc-800/50 hover:bg-zinc-800'
                : 'border-zinc-200/80 hover:border-zinc-300 bg-white hover:bg-zinc-50'
          ]"
        >
          <!-- Drag Handle -->
          <div class="flex flex-col items-center gap-px shrink-0 pl-0.5">
            <span
              class="material-symbols-outlined text-sm transition-colors"
              :class="isDarkMode ? 'text-zinc-600 group-hover:text-zinc-400' : 'text-zinc-300 group-hover:text-zinc-500'"
            >drag_indicator</span>
          </div>

          <!-- Thumbnail -->
          <div
            class="w-12 h-16 rounded-md overflow-hidden shrink-0 border shadow-sm flex items-center justify-center"
            :class="isDarkMode ? 'border-zinc-700 bg-zinc-900' : 'border-zinc-200 bg-zinc-50'"
          >
            <!-- Existing page thumbnail via vue-pdf-embed -->
            <VuePdfEmbed
              v-if="page.source === 'existing'"
              :source="pdfSource"
              :page="page.originalPage"
              :width="96"
              :annotation-layer="false"
              :text-layer="false"
              class="pointer-events-none [&_canvas]:!w-full [&_canvas]:!h-auto"
            />
            <!-- Uploaded image thumbnail -->
            <img
              v-else-if="page.source === 'upload' && page.thumbnailUrl && !page.uploadPageNumber"
              :src="page.thumbnailUrl"
              class="w-full h-full object-cover"
              :alt="page.label"
            />
            <!-- Uploaded PDF page thumbnail -->
            <VuePdfEmbed
              v-else-if="page.source === 'upload' && page.thumbnailUrl && page.uploadPageNumber"
              :source="page.thumbnailUrl"
              :page="page.uploadPageNumber"
              :width="96"
              :annotation-layer="false"
              :text-layer="false"
              class="pointer-events-none [&_canvas]:!w-full [&_canvas]:!h-auto"
            />
            <!-- Fallback -->
            <span
              v-else
              class="material-symbols-outlined text-lg"
              :class="isDarkMode ? 'text-zinc-600' : 'text-zinc-300'"
            >description</span>
          </div>

          <!-- Label & Position -->
          <div class="flex-1 min-w-0 py-0.5">
            <p
              class="text-xs font-medium truncate leading-tight"
              :class="isDarkMode ? 'text-zinc-200' : 'text-zinc-700'"
            >{{ page.label }}</p>
            <p class="text-[10px] mt-0.5 leading-tight" :class="isDarkMode ? 'text-zinc-500' : 'text-zinc-400'">
              {{ index + 1 }} of {{ managedPages.length }}
              <span
                v-if="page.source === 'upload'"
                class="inline-flex items-center ml-1 px-1 py-px rounded text-[9px] font-semibold uppercase tracking-wider bg-teal/10 text-teal"
              >new</span>
            </p>
          </div>

          <!-- Actions (visible on hover) -->
          <div
            class="flex flex-col gap-0.5 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity"
          >
            <button
              @click.stop="moveUp(index)"
              :disabled="index === 0"
              class="p-0.5 rounded transition-colors disabled:opacity-20"
              :class="isDarkMode ? 'text-zinc-500 hover:text-zinc-300 hover:bg-zinc-700' : 'text-zinc-400 hover:text-zinc-600 hover:bg-zinc-100'"
              title="Move up"
            >
              <span class="material-symbols-outlined text-sm">keyboard_arrow_up</span>
            </button>
            <button
              @click.stop="moveDown(index)"
              :disabled="index === managedPages.length - 1"
              class="p-0.5 rounded transition-colors disabled:opacity-20"
              :class="isDarkMode ? 'text-zinc-500 hover:text-zinc-300 hover:bg-zinc-700' : 'text-zinc-400 hover:text-zinc-600 hover:bg-zinc-100'"
              title="Move down"
            >
              <span class="material-symbols-outlined text-sm">keyboard_arrow_down</span>
            </button>
            <button
              @click.stop="deletePage(index)"
              :disabled="managedPages.length <= 1"
              class="p-0.5 rounded transition-colors disabled:opacity-20"
              :class="'text-zinc-400 hover:text-red-500 hover:bg-red-50'"
              :title="managedPages.length <= 1 ? 'Cannot delete the only page' : 'Delete page'"
            >
              <span class="material-symbols-outlined text-sm">delete</span>
            </button>
          </div>
        </div>
      </TransitionGroup>

      <!-- Empty State -->
      <div
        v-if="managedPages.length === 0"
        class="flex flex-col items-center justify-center py-12 text-center"
      >
        <span
          class="material-symbols-outlined text-3xl mb-2"
          :class="isDarkMode ? 'text-zinc-600' : 'text-zinc-300'"
        >layers_clear</span>
        <p class="text-xs" :class="isDarkMode ? 'text-zinc-500' : 'text-zinc-400'">
          No pages. Add pages to continue.
        </p>
      </div>
    </div>

    <!-- ─── Error ──────────────────────────────── -->
    <div
      v-if="errorMessage"
      class="px-3 py-2 border-t text-xs text-red-500 flex items-start gap-1.5 shrink-0"
      :class="isDarkMode ? 'border-zinc-700 bg-red-950/30' : 'border-red-100 bg-red-50'"
    >
      <span class="material-symbols-outlined text-sm mt-px shrink-0">error</span>
      <span class="min-w-0">{{ errorMessage }}</span>
      <button @click="errorMessage = ''" class="ml-auto shrink-0 hover:text-red-700">
        <span class="material-symbols-outlined text-sm">close</span>
      </button>
    </div>

    <!-- ─── Footer ─────────────────────────────── -->
    <div
      class="px-3 py-2.5 border-t shrink-0 space-y-2"
      :class="isDarkMode ? 'border-zinc-700 bg-zinc-800' : 'border-zinc-200 bg-zinc-50'"
    >
      <!-- Progress bar -->
      <div v-if="isApplying" class="w-full h-1 rounded-full overflow-hidden bg-zinc-200">
        <div
          class="h-full rounded-full bg-teal transition-all duration-300"
          :style="{ width: `${applyProgress}%` }"
        ></div>
      </div>

      <div class="flex gap-2">
        <button
          @click="$emit('close')"
          :disabled="isApplying"
          class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors disabled:opacity-50"
          :class="isDarkMode
            ? 'text-zinc-400 hover:text-zinc-200 hover:bg-zinc-700'
            : 'text-zinc-500 hover:text-zinc-700 hover:bg-zinc-100'"
        >Cancel</button>
        <button
          @click="applyChanges"
          :disabled="!canApply"
          class="flex-1 py-1.5 text-xs font-medium rounded-lg transition-colors flex items-center justify-center gap-1.5 bg-teal text-white hover:bg-teal/90 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          <svg v-if="isApplying" class="animate-spin w-3 h-3" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
          </svg>
          <span class="material-symbols-outlined text-sm" v-else>check_circle</span>
          {{ isApplying ? 'Applying...' : 'Apply Changes' }}
        </button>
      </div>
    </div>
  </aside>
</template>

<style scoped>
/* Transition group animations */
.page-list-move {
  transition: transform 0.25s ease;
}

.page-list-enter-active {
  transition: all 0.2s ease-out;
}

.page-list-leave-active {
  transition: all 0.15s ease-in;
  position: absolute;
}

.page-list-enter-from {
  opacity: 0;
  transform: translateX(20px);
}

.page-list-leave-to {
  opacity: 0;
  transform: translateX(-20px) scale(0.95);
}

/* Scrollbar */
.overflow-y-auto {
  scrollbar-width: thin;
  scrollbar-color: rgba(113, 113, 122, 0.25) transparent;
}

.overflow-y-auto::-webkit-scrollbar {
  width: 4px;
}

.overflow-y-auto::-webkit-scrollbar-track {
  background: transparent;
}

.overflow-y-auto::-webkit-scrollbar-thumb {
  background: rgba(113, 113, 122, 0.25);
  border-radius: 4px;
}

.overflow-y-auto::-webkit-scrollbar-thumb:hover {
  background: rgba(113, 113, 122, 0.4);
}

/* Constrain vue-pdf-embed thumbnails within the 48x64 box */
:deep(.vue-pdf-embed) {
  display: flex;
  align-items: flex-start;
  justify-content: center;
  overflow: hidden;
  width: 100%;
  height: 100%;
}

:deep(.vue-pdf-embed__page) {
  box-shadow: none !important;
  margin: 0 !important;
}
</style>
