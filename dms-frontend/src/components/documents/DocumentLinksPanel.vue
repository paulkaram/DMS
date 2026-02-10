<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { documentLinksApi, documentsApi } from '@/api/client'
import { useRouter } from 'vue-router'

interface DocumentLink {
  id: string
  sourceDocumentId: string
  sourceDocumentName?: string
  targetDocumentId: string
  targetDocumentName?: string
  targetDocumentPath?: string
  linkType: string
  description?: string
  createdBy: string
  createdByName?: string
  createdAt: string
}

const props = defineProps<{
  documentId: string
  canEdit?: boolean
  embedded?: boolean
}>()

const emit = defineEmits<{
  close: []
}>()

const router = useRouter()
const outgoingLinks = ref<DocumentLink[]>([])
const incomingLinks = ref<DocumentLink[]>([])
const isLoading = ref(true)
const showLinkForm = ref(false)
const searchQuery = ref('')
const searchResults = ref<any[]>([])
const isSearching = ref(false)
const selectedDocument = ref<any>(null)
const linkType = ref('related')
const linkDescription = ref('')

const linkTypes = [
  { value: 'related', label: 'Related', icon: 'link' },
  { value: 'reference', label: 'Reference', icon: 'bookmark' },
  { value: 'supersedes', label: 'Supersedes', icon: 'upgrade' },
  { value: 'attachment', label: 'Attachment', icon: 'attach_file' }
]

onMounted(async () => {
  await loadLinks()
})

async function loadLinks() {
  isLoading.value = true
  try {
    const [outgoing, incoming] = await Promise.all([
      documentLinksApi.getByDocument(props.documentId),
      documentLinksApi.getIncoming(props.documentId)
    ])
    outgoingLinks.value = outgoing.data
    incomingLinks.value = incoming.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

async function searchDocuments() {
  if (!searchQuery.value.trim()) {
    searchResults.value = []
    return
  }

  isSearching.value = true
  try {
    const response = await documentsApi.search({ search: searchQuery.value })
    const data = response.data
    const items = Array.isArray(data) ? data : data.items ?? []
    // Filter out the current document
    searchResults.value = items.filter((doc: any) => doc.id !== props.documentId)
  } catch (error) {
  } finally {
    isSearching.value = false
  }
}

function selectDocument(doc: any) {
  selectedDocument.value = doc
  searchQuery.value = ''
  searchResults.value = []
}

async function createLink() {
  if (!selectedDocument.value) return

  try {
    await documentLinksApi.create(props.documentId, {
      targetDocumentId: selectedDocument.value.id,
      linkType: linkType.value,
      description: linkDescription.value || undefined
    })
    resetForm()
    await loadLinks()
  } catch (error) {
  }
}

async function deleteLink(linkId: string) {
  if (!confirm('Are you sure you want to remove this link?')) return

  try {
    await documentLinksApi.delete(props.documentId, linkId)
    await loadLinks()
  } catch (error) {
  }
}

function resetForm() {
  showLinkForm.value = false
  selectedDocument.value = null
  searchQuery.value = ''
  searchResults.value = []
  linkType.value = 'related'
  linkDescription.value = ''
}

function navigateToDocument(documentId: string) {
  router.push(`/documents/${documentId}`)
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString()
}

function getLinkTypeInfo(type: string) {
  return linkTypes.find(t => t.value === type) || linkTypes[0]
}

const totalLinks = computed(() => outgoingLinks.value.length + incomingLinks.value.length)
</script>

<template>
  <div :class="embedded ? '-mx-6 -my-5' : 'flex flex-col h-full bg-white dark:bg-background-dark'">
    <!-- Header (hidden when embedded in UiModal) -->
    <div v-if="!embedded" class="flex items-center justify-between px-6 py-4 border-b border-zinc-200 dark:border-border-dark">
      <div>
        <h2 class="text-lg font-semibold text-zinc-900 dark:text-zinc-100">Document Links</h2>
        <p class="text-sm text-zinc-500">{{ totalLinks }} linked documents</p>
      </div>
      <div class="flex items-center gap-2">
        <button
          v-if="canEdit"
          @click="showLinkForm = !showLinkForm"
          class="flex items-center gap-2 px-3 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors"
        >
          <span class="material-symbols-outlined text-lg">add_link</span>
          Link
        </button>
        <button
          @click="emit('close')"
          class="p-2 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 rounded-lg hover:bg-zinc-100 dark:hover:bg-surface-dark transition-colors"
        >
          <span class="material-symbols-outlined">close</span>
        </button>
      </div>
    </div>

    <!-- Toolbar (shown when embedded in UiModal) -->
    <div v-if="embedded" class="flex items-center justify-between px-6 py-3 border-b border-zinc-200 dark:border-border-dark bg-zinc-50 dark:bg-surface-dark/30">
      <p class="text-sm text-zinc-500 dark:text-zinc-400">{{ totalLinks }} linked document{{ totalLinks !== 1 ? 's' : '' }}</p>
      <button
        v-if="canEdit"
        @click="showLinkForm = !showLinkForm"
        class="inline-flex items-center gap-1.5 px-3 py-1.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors shadow-sm"
      >
        <span class="material-symbols-outlined text-lg">add</span>
        Add Link
      </button>
    </div>

    <!-- Link Form -->
    <div v-if="showLinkForm" class="p-4 border-b border-zinc-200 dark:border-border-dark bg-zinc-50 dark:bg-surface-dark/50">
      <div class="space-y-4">
        <!-- Selected Document -->
        <div v-if="selectedDocument" class="flex items-center gap-3 p-3 bg-teal/10 rounded-lg">
          <span class="material-symbols-outlined text-teal">description</span>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100 truncate">
              {{ selectedDocument.name }}
            </p>
          </div>
          <button
            @click="selectedDocument = null"
            class="p-1 text-zinc-400 hover:text-zinc-600 rounded"
          >
            <span class="material-symbols-outlined text-lg">close</span>
          </button>
        </div>

        <!-- Search Documents -->
        <div v-else class="relative">
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Search for document</label>
          <div class="relative">
            <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400">search</span>
            <input
              v-model="searchQuery"
              @input="searchDocuments"
              type="text"
              class="w-full pl-10 pr-4 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
              placeholder="Type to search documents..."
            />
          </div>

          <!-- Search Results -->
          <div v-if="searchResults.length > 0" class="absolute z-10 mt-1 w-full bg-white dark:bg-background-dark border border-zinc-200 dark:border-border-dark rounded-lg shadow-lg max-h-48 overflow-y-auto">
            <button
              v-for="doc in searchResults"
              :key="doc.id"
              @click="selectDocument(doc)"
              class="w-full flex items-center gap-3 p-3 hover:bg-zinc-50 dark:hover:bg-surface-dark text-left"
            >
              <span class="material-symbols-outlined text-zinc-400">description</span>
              <div class="min-w-0">
                <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100 truncate">{{ doc.name }}</p>
                <p class="text-xs text-zinc-400 truncate">{{ doc.path }}</p>
              </div>
            </button>
          </div>

          <div v-if="isSearching" class="absolute z-10 mt-1 w-full p-4 bg-white dark:bg-background-dark border border-zinc-200 dark:border-border-dark rounded-lg shadow-lg text-center">
            <div class="animate-spin w-5 h-5 border-2 border-teal border-t-transparent rounded-full mx-auto"></div>
          </div>
        </div>

        <!-- Link Type -->
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Link Type</label>
          <select
            v-model="linkType"
            class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
          >
            <option v-for="type in linkTypes" :key="type.value" :value="type.value">
              {{ type.label }}
            </option>
          </select>
        </div>

        <!-- Description -->
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description (optional)</label>
          <input
            v-model="linkDescription"
            type="text"
            class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-sm focus:ring-2 focus:ring-teal/50 focus:border-teal"
            placeholder="Describe the relationship..."
          />
        </div>

        <!-- Actions -->
        <div class="flex justify-end gap-2">
          <button
            @click="resetForm"
            class="px-4 py-2 text-sm text-zinc-600 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg transition-colors"
          >
            Cancel
          </button>
          <button
            @click="createLink"
            :disabled="!selectedDocument"
            class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Create Link
          </button>
        </div>
      </div>
    </div>

    <!-- Links List -->
    <div class="flex-1 overflow-y-auto p-6">
      <div v-if="isLoading" class="flex items-center justify-center py-12">
        <div class="animate-spin w-8 h-8 border-3 border-teal border-t-transparent rounded-full"></div>
      </div>

      <div v-else-if="totalLinks === 0" class="text-center py-12">
        <span class="material-symbols-outlined text-4xl text-zinc-300 dark:text-zinc-600 mb-2">link_off</span>
        <p class="text-zinc-500 dark:text-zinc-400">No linked documents</p>
        <p class="text-sm text-zinc-400 dark:text-zinc-500">Link related documents together</p>
      </div>

      <div v-else class="space-y-6">
        <!-- Outgoing Links -->
        <div v-if="outgoingLinks.length > 0">
          <h3 class="text-sm font-semibold text-zinc-700 dark:text-zinc-300 mb-3 flex items-center gap-2">
            <span class="material-symbols-outlined text-lg">arrow_forward</span>
            Links from this document ({{ outgoingLinks.length }})
          </h3>
          <div class="space-y-2">
            <div
              v-for="link in outgoingLinks"
              :key="link.id"
              class="flex items-center gap-4 p-4 bg-zinc-50 dark:bg-surface-dark rounded-lg hover:bg-zinc-100 dark:hover:bg-surface-dark/80 transition-colors group"
            >
              <div class="w-10 h-10 rounded-lg bg-teal/10 flex items-center justify-center text-teal flex-shrink-0">
                <span class="material-symbols-outlined">{{ getLinkTypeInfo(link.linkType).icon }}</span>
              </div>
              <div class="flex-1 min-w-0 cursor-pointer" @click="navigateToDocument(link.targetDocumentId)">
                <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100 truncate hover:text-teal">
                  {{ link.targetDocumentName || 'Unknown Document' }}
                </p>
                <div class="flex items-center gap-2 text-xs text-zinc-500">
                  <span class="px-2 py-0.5 bg-zinc-200 dark:bg-border-dark rounded">{{ getLinkTypeInfo(link.linkType).label }}</span>
                  <span>{{ formatDate(link.createdAt) }}</span>
                </div>
                <p v-if="link.description" class="text-xs text-zinc-400 mt-1 truncate">{{ link.description }}</p>
              </div>
              <button
                v-if="canEdit"
                @click="deleteLink(link.id)"
                class="p-2 text-zinc-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg opacity-0 group-hover:opacity-100 transition-all"
                title="Remove link"
              >
                <span class="material-symbols-outlined text-lg">link_off</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Incoming Links -->
        <div v-if="incomingLinks.length > 0">
          <h3 class="text-sm font-semibold text-zinc-700 dark:text-zinc-300 mb-3 flex items-center gap-2">
            <span class="material-symbols-outlined text-lg">arrow_back</span>
            Links to this document ({{ incomingLinks.length }})
          </h3>
          <div class="space-y-2">
            <div
              v-for="link in incomingLinks"
              :key="link.id"
              class="flex items-center gap-4 p-4 bg-zinc-50 dark:bg-surface-dark rounded-lg hover:bg-zinc-100 dark:hover:bg-surface-dark/80 transition-colors cursor-pointer"
              @click="navigateToDocument(link.sourceDocumentId)"
            >
              <div class="w-10 h-10 rounded-lg bg-blue-500/10 flex items-center justify-center text-blue-500 flex-shrink-0">
                <span class="material-symbols-outlined">{{ getLinkTypeInfo(link.linkType).icon }}</span>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-zinc-900 dark:text-zinc-100 truncate hover:text-teal">
                  {{ link.sourceDocumentName || 'Unknown Document' }}
                </p>
                <div class="flex items-center gap-2 text-xs text-zinc-500">
                  <span class="px-2 py-0.5 bg-zinc-200 dark:bg-border-dark rounded">{{ getLinkTypeInfo(link.linkType).label }}</span>
                  <span>{{ formatDate(link.createdAt) }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
