<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import type { Document } from '@/types'
import { documentsApi, favoritesApi, permissionsApi } from '@/api/client'
import { DmsLoader, UiCheckbox } from '@/components/ui'
import { NodeTypes, PermissionLevels } from '@/types'
import DocumentContextMenu, { type DocumentPermissions } from './DocumentContextMenu.vue'
import DocumentRow from './DocumentRow.vue'
import EmptyState from './EmptyState.vue'
import SortableHeader from './SortableHeader.vue'

const props = defineProps<{
  documents: Document[]
  isLoading: boolean
  viewMode?: 'list' | 'grid'
  selectable?: boolean
}>()

const emit = defineEmits<{
  'document-action': [action: string, document: Document]
  'selection-change': [selectedIds: string[]]
  refresh: []
}>()

const router = useRouter()

// Selection state
const selectedDocuments = ref<Set<string>>(new Set())
const isAllSelected = computed({
  get: () => props.documents.length > 0 && selectedDocuments.value.size === props.documents.length,
  set: (value: boolean) => {
    if (value) {
      props.documents.forEach(doc => selectedDocuments.value.add(doc.id))
    } else {
      selectedDocuments.value.clear()
    }
    emit('selection-change', Array.from(selectedDocuments.value))
  }
})
const isSomeSelected = computed(() =>
  selectedDocuments.value.size > 0 && selectedDocuments.value.size < props.documents.length
)

// Context menu state
const showContextMenu = ref(false)
const contextMenuPosition = ref({ x: 0, y: 0 })
const contextMenuDocument = ref<Document | null>(null)
const contextMenuPermissions = ref<DocumentPermissions | null>(null)
const favoriteDocuments = ref<Set<string>>(new Set())
const isTogglingFavorite = ref(false)

// Permission level cache for documents
const documentPermissionLevels = ref<Map<string, number>>(new Map())

// Load all favorites once and filter for current documents
onMounted(async () => {
  await loadAllFavorites()
})

// Re-check favorites when documents change
watch(() => props.documents, () => {
  // Favorites are loaded from the full list, no need to re-fetch
}, { immediate: false })

async function loadAllFavorites() {
  try {
    const response = await favoritesApi.getMyFavorites()
    const allFavorites = response.data || []
    favoriteDocuments.value.clear()

    // Filter to only document type favorites
    for (const fav of allFavorites) {
      if (fav.nodeType === NodeTypes.Document) {
        favoriteDocuments.value.add(fav.nodeId)
      }
    }
  } catch (err) {
  }
}

// Sorting state
const sortField = ref<string>('name')
const sortDirection = ref<'asc' | 'desc'>('asc')

const sortedDocuments = computed(() => {
  return [...props.documents].sort((a, b) => {
    let comparison = 0
    switch (sortField.value) {
      case 'name':
        comparison = a.name.localeCompare(b.name)
        break
      case 'size':
        comparison = a.size - b.size
        break
      case 'createdAt':
        comparison = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime()
        break
      case 'createdByName':
        comparison = (a.createdByName || '').localeCompare(b.createdByName || '')
        break
      case 'contentTypeName':
        comparison = (a.contentTypeName || '').localeCompare(b.contentTypeName || '')
        break
      case 'currentVersion':
        comparison = a.currentVersion - b.currentVersion
        break
    }
    return sortDirection.value === 'asc' ? comparison : -comparison
  })
})

function handleSort(field: string) {
  if (sortField.value === field) {
    sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc'
  } else {
    sortField.value = field
    sortDirection.value = 'asc'
  }
}

// Get document permissions based on actual permission level
function getDocumentPermissions(doc: Document, permissionLevel: number): DocumentPermissions {
  const canRead = (permissionLevel & PermissionLevels.Read) === PermissionLevels.Read
  const canWrite = (permissionLevel & PermissionLevels.Write) === PermissionLevels.Write
  const canDelete = (permissionLevel & PermissionLevels.Delete) === PermissionLevels.Delete
  const canAdmin = (permissionLevel & PermissionLevels.Admin) === PermissionLevels.Admin

  return {
    canRead,
    canWrite: canWrite && !doc.isLocked && !doc.isCheckedOut,
    canDelete: canDelete && !doc.isLocked && !doc.isCheckedOut,
    canAdmin,
    canShare: canRead, // Can share if can read
    canExport: canRead, // Can export/download if can read
    canCheckout: canWrite && !doc.isCheckedOut && !doc.isLocked,
    canCheckin: canWrite && doc.isCheckedOut,
    canManageVersions: canWrite && !doc.isLocked,
    canManagePermissions: canAdmin,
    canStartWorkflow: canWrite && !doc.isCheckedOut,
    canRoute: canWrite
  }
}

// Fetch permission level for a document
async function fetchDocumentPermissionLevel(docId: string): Promise<number> {
  // Check cache first
  if (documentPermissionLevels.value.has(docId)) {
    return documentPermissionLevels.value.get(docId)!
  }

  try {
    const response = await permissionsApi.getMyPermissionLevel('Document', docId)
    const level = response.data.level || 0
    documentPermissionLevels.value.set(docId, level)
    return level
  } catch (error) {
    // Default to read-only if permission check fails
    return PermissionLevels.Read
  }
}

function viewDocument(doc: Document) {
  router.push(`/documents/${doc.id}`)
}

async function downloadDocument(doc: Document) {
  try {
    const response = await documentsApi.download(doc.id)
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', doc.name + (doc.extension || ''))
    document.body.appendChild(link)
    link.click()
    link.remove()
    window.URL.revokeObjectURL(url)
  } catch (error) {
  }
}

async function openContextMenu(event: MouseEvent, doc: Document) {
  event.preventDefault()
  event.stopPropagation()

  // Fetch actual permission level for this document
  const permissionLevel = await fetchDocumentPermissionLevel(doc.id)
  contextMenuPermissions.value = getDocumentPermissions(doc, permissionLevel)

  contextMenuDocument.value = doc
  contextMenuPosition.value = { x: event.clientX, y: event.clientY }
  showContextMenu.value = true
}

function closeContextMenu() {
  showContextMenu.value = false
  contextMenuDocument.value = null
}

async function handleDocumentAction(action: string, doc: Document) {
  switch (action) {
    case 'view-file':
    case 'view-properties':
      viewDocument(doc)
      break
    case 'download':
      await downloadDocument(doc)
      break
    case 'toggle-favorite':
      await toggleFavorite(doc)
      break
    case 'version-history':
      router.push(`/documents/${doc.id}?tab=versions`)
      break
    case 'checkout':
      await handleCheckout(doc)
      break
    case 'checkin':
      // Navigate to document details for check-in (requires file selection)
      router.push(`/documents/${doc.id}`)
      break
    case 'discard-checkout':
      await handleDiscardCheckout(doc)
      break
    default:
      emit('document-action', action, doc)
  }
}

async function handleCheckout(doc: Document) {
  try {
    await documentsApi.checkout(doc.id)
    doc.isCheckedOut = true
    emit('refresh')
  } catch (error: any) {
    alert(error.response?.data?.errors?.[0] || 'Checkout failed')
  }
}

async function handleDiscardCheckout(doc: Document) {
  if (!confirm(`Discard checkout for "${doc.name}"? All draft changes will be lost.`)) return
  try {
    await documentsApi.discardCheckout(doc.id)
    doc.isCheckedOut = false
    doc.checkedOutBy = undefined
    emit('refresh')
  } catch (error: any) {
    alert(error.response?.data?.errors?.[0] || 'Discard checkout failed')
  }
}

async function toggleFavorite(doc: Document) {
  if (isTogglingFavorite.value) return
  isTogglingFavorite.value = true

  const wasFavorite = favoriteDocuments.value.has(doc.id)

  // Optimistic update
  if (wasFavorite) {
    favoriteDocuments.value.delete(doc.id)
  } else {
    favoriteDocuments.value.add(doc.id)
  }

  try {
    await favoritesApi.toggle(NodeTypes.Document, doc.id)
  } catch (error) {
    // Revert on error
    if (wasFavorite) {
      favoriteDocuments.value.add(doc.id)
    } else {
      favoriteDocuments.value.delete(doc.id)
    }
  } finally {
    isTogglingFavorite.value = false
  }
}

function toggleSelectAll() {
  if (isAllSelected.value) {
    selectedDocuments.value.clear()
  } else {
    props.documents.forEach(doc => selectedDocuments.value.add(doc.id))
  }
  emit('selection-change', Array.from(selectedDocuments.value))
}

function toggleSelectDocument(docId: string) {
  if (selectedDocuments.value.has(docId)) {
    selectedDocuments.value.delete(docId)
  } else {
    selectedDocuments.value.add(docId)
  }
  emit('selection-change', Array.from(selectedDocuments.value))
}
</script>

<template>
  <div>
    <!-- Loading State -->
    <div v-if="isLoading" class="py-12">
      <DmsLoader size="lg" text="Loading documents..." />
    </div>

    <!-- Empty State -->
    <EmptyState
      v-else-if="documents.length === 0"
      icon="folder_off"
      title="No documents yet"
      description="Upload your first document to get started"
      action-label="Upload Document"
      action-icon="upload"
      @action="emit('document-action', 'upload', {} as Document)"
    />

    <!-- Documents Table - Light with Charcoal Header -->
    <div v-else class="bg-white dark:bg-background-dark rounded-lg overflow-hidden shadow-xl border border-zinc-200 dark:border-border-dark">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-[#0d1117] text-xs font-semibold text-zinc-300 uppercase tracking-wider">
              <th v-if="selectable" class="px-5 py-4 w-12">
                <div class="flex items-center gap-3">
                  <UiCheckbox
                    v-model="isAllSelected"
                    :indeterminate="isSomeSelected"
                    size="sm"
                  />
                  <span
                    v-if="selectedDocuments.size > 0"
                    class="text-[10px] font-bold text-teal bg-teal/20 px-2.5 py-1 rounded-full normal-case"
                  >
                    {{ selectedDocuments.size }} selected
                  </span>
                </div>
              </th>
              <SortableHeader
                label="Name"
                field="name"
                :active-field="sortField"
                :direction="sortDirection"
                @sort="handleSort"
              />
              <SortableHeader
                label="Size"
                field="size"
                :active-field="sortField"
                :direction="sortDirection"
                @sort="handleSort"
              />
              <SortableHeader
                label="Created By"
                field="createdByName"
                :active-field="sortField"
                :direction="sortDirection"
                @sort="handleSort"
              />
              <SortableHeader
                label="Created"
                field="createdAt"
                :active-field="sortField"
                :direction="sortDirection"
                @sort="handleSort"
              />
              <SortableHeader
                label="Content Type"
                field="contentTypeName"
                :active-field="sortField"
                :direction="sortDirection"
                @sort="handleSort"
              />
              <SortableHeader
                label="Version"
                field="currentVersion"
                :active-field="sortField"
                :direction="sortDirection"
                @sort="handleSort"
              />
              <th class="px-4 py-3">Status</th>
              <th class="px-4 py-3 text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <DocumentRow
              v-for="(doc, index) in sortedDocuments"
              :key="doc.id"
              :document="doc"
              :index="index"
              :selected="selectedDocuments.has(doc.id)"
              :is-favorite="favoriteDocuments.has(doc.id)"
              :selectable="selectable"
              @select="toggleSelectDocument"
              @preview="(d) => emit('document-action', 'preview', d)"
              @view="viewDocument"
              @download="downloadDocument"
              @share="(d) => emit('document-action', 'share', d)"
              @context-menu="openContextMenu"
              @version-history="(d) => handleDocumentAction('version-history', d)"
            />
          </tbody>
        </table>
      </div>

      <!-- Footer -->
      <div class="px-5 py-3 bg-zinc-50 dark:bg-surface-dark border-t border-zinc-200 dark:border-border-dark flex items-center justify-between">
        <span class="text-xs text-zinc-500">
          <span class="font-semibold text-teal">{{ documents.length }}</span>
          {{ documents.length === 1 ? 'document' : 'documents' }}
        </span>
        <div class="flex items-center gap-1 bg-white dark:bg-border-dark rounded-lg p-1 shadow-sm border border-zinc-200 dark:border-border-dark">
          <button class="p-1.5 text-zinc-400 disabled:opacity-30 disabled:cursor-not-allowed rounded-md transition-all hover:text-teal hover:bg-teal/10" disabled>
            <span class="material-symbols-outlined text-base">chevron_left</span>
          </button>
          <span class="px-3 py-1 text-xs font-medium text-zinc-600 dark:text-zinc-300 bg-zinc-100 dark:bg-zinc-600 rounded-md">1</span>
          <button class="p-1.5 text-zinc-400 hover:text-teal rounded-md hover:bg-teal/10 transition-all">
            <span class="material-symbols-outlined text-base">chevron_right</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Context Menu -->
    <DocumentContextMenu
      v-if="showContextMenu && contextMenuDocument && contextMenuPermissions"
      :document="contextMenuDocument"
      :permissions="contextMenuPermissions"
      :x="contextMenuPosition.x"
      :y="contextMenuPosition.y"
      :is-favorite="favoriteDocuments.has(contextMenuDocument.id)"
      @action="handleDocumentAction"
      @close="closeContextMenu"
    />
  </div>
</template>
