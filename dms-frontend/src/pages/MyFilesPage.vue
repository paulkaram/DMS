<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import type { Document } from '@/types'
import { documentsApi } from '@/api/client'
import DocumentIcon from '@/components/common/DocumentIcon.vue'
import DocumentViewer from '@/components/documents/DocumentViewer.vue'

const router = useRouter()
const myDocuments = ref<Document[]>([])
const myCheckouts = ref<Document[]>([])
const isLoading = ref(true)
const activeTab = ref<'uploads' | 'checkouts'>('uploads')

// Document Preview
const showPreview = ref(false)
const previewDocument = ref<Document | null>(null)

function openPreview(doc: Document) {
  previewDocument.value = doc
  showPreview.value = true
}

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [documentsRes, checkoutsRes] = await Promise.all([
      documentsApi.getMyDocuments(50),
      documentsApi.getMyCheckouts()
    ])
    myDocuments.value = documentsRes.data
    myCheckouts.value = checkoutsRes.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function viewDocument(doc: Document) {
  router.push(`/documents/${doc.id}`)
}

function editDocument(doc: Document) {
  router.push(`/documents/${doc.id}?edit=true`)
}

function goToLocation(doc: Document) {
  if (doc.folderId) {
    router.push(`/explorer?folderId=${doc.folderId}`)
  }
}

function searchSimilar(doc: Document) {
  const searchTerm = doc.name.split('.')[0] // Get name without extension
  router.push(`/search?q=${encodeURIComponent(searchTerm)}`)
}

function formatSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
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


async function discardCheckout(doc: Document) {
  if (!confirm(`Discard checkout for "${doc.name}"?`)) return
  try {
    await documentsApi.discardCheckout(doc.id)
    myCheckouts.value = myCheckouts.value.filter(d => d.id !== doc.id)
  } catch (err) {
  }
}
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-slate-900 dark:text-slate-100">My Files</h1>
        <p class="text-slate-500 mt-1">Your uploaded documents and checked out files</p>
      </div>
      <button
        @click="loadData"
        class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-xl font-medium text-sm transition-colors border border-slate-200 dark:border-slate-700"
      >
        <span class="material-symbols-outlined text-lg">refresh</span>
        Refresh
      </button>
    </div>

    <!-- Stats Cards -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-6">
      <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">cloud_upload</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Uploads</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ myDocuments.length }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Documents you created</p>
        </div>
      </div>
      <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="flex items-center justify-between relative z-10">
          <span class="material-symbols-outlined text-teal">edit_document</span>
          <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Checked Out</span>
        </div>
        <div class="relative z-10">
          <p class="text-4xl font-bold">{{ myCheckouts.length }}</p>
          <p class="text-[10px] text-teal mt-2 font-medium">Files being edited</p>
        </div>
      </div>
    </div>

    <!-- Tabs -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
      <div class="border-b border-slate-200 dark:border-slate-800">
        <nav class="flex">
          <button
            @click="activeTab = 'uploads'"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              activeTab === 'uploads'
                ? 'border-teal text-teal'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">cloud_upload</span>
              My Uploads
              <span v-if="myDocuments.length > 0" class="px-2 py-0.5 text-xs bg-teal/15 text-teal rounded-full">
                {{ myDocuments.length }}
              </span>
            </span>
          </button>
          <button
            @click="activeTab = 'checkouts'"
            :class="[
              'px-6 py-4 text-sm font-medium border-b-2 transition-colors',
              activeTab === 'checkouts'
                ? 'border-teal text-teal'
                : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
            ]"
          >
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">edit_document</span>
              My Checkouts
              <span v-if="myCheckouts.length > 0" class="px-2 py-0.5 text-xs bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 rounded-full">
                {{ myCheckouts.length }}
              </span>
            </span>
          </button>
        </nav>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <div class="animate-spin w-8 h-8 border-4 border-teal border-t-transparent rounded-full"></div>
      </div>

      <!-- Uploads Tab -->
      <div v-else-if="activeTab === 'uploads'" class="p-6">
        <div v-if="myDocuments.length === 0" class="text-center py-12">
          <div class="w-20 h-20 rounded-2xl bg-slate-100 dark:bg-slate-800 flex items-center justify-center mx-auto mb-4">
            <span class="material-symbols-outlined text-5xl text-slate-400">cloud_upload</span>
          </div>
          <h3 class="text-lg font-semibold text-slate-700 dark:text-slate-300">No uploads yet</h3>
          <p class="text-slate-500 mt-1">Documents you upload will appear here</p>
          <router-link
            to="/explorer"
            class="inline-flex items-center gap-2 mt-4 px-4 py-2 bg-teal hover:bg-teal/90 text-white rounded-xl font-medium text-sm transition-colors"
          >
            <span class="material-symbols-outlined text-lg">explore</span>
            Go to Explorer
          </router-link>
        </div>

        <div v-else class="space-y-2">
          <div
            v-for="(doc, index) in myDocuments"
            :key="doc.id"
            class="flex items-center gap-4 p-4 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-100 dark:border-slate-700 hover:border-teal/30 hover:shadow-md transition-all"
          >
            <DocumentIcon :extension="doc.extension" :index="index" size="lg" />
            <div class="flex-1 min-w-0 cursor-pointer" @click="viewDocument(doc)">
              <p class="font-medium text-slate-900 dark:text-white truncate">{{ doc.name }}</p>
              <div class="flex items-center gap-3 mt-1 text-sm text-slate-500">
                <span>{{ doc.extension || 'Unknown' }}</span>
                <span>•</span>
                <span>{{ formatSize(doc.size) }}</span>
              </div>
            </div>
            <div class="text-sm text-slate-500 dark:text-slate-400 flex-shrink-0 mr-2">
              {{ formatDate(doc.createdAt) }}
            </div>
            <div class="flex items-center gap-1 flex-shrink-0">
              <div class="relative group">
                <button
                  @click.stop="openPreview(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">open_in_new</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Preview
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="viewDocument(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">info</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Details
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="editDocument(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">edit</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Edit
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="goToLocation(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">folder_open</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Go to Location
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="searchSimilar(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">search</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Search
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Checkouts Tab -->
      <div v-else-if="activeTab === 'checkouts'" class="p-6">
        <div v-if="myCheckouts.length === 0" class="text-center py-12">
          <div class="w-20 h-20 rounded-2xl bg-slate-100 dark:bg-slate-800 flex items-center justify-center mx-auto mb-4">
            <span class="material-symbols-outlined text-5xl text-slate-400">check_circle</span>
          </div>
          <h3 class="text-lg font-semibold text-slate-700 dark:text-slate-300">No checked out files</h3>
          <p class="text-slate-500 mt-1">Files you check out for editing will appear here</p>
        </div>

        <div v-else class="space-y-2">
          <div
            v-for="(doc, index) in myCheckouts"
            :key="doc.id"
            class="flex items-center gap-4 p-4 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-100 dark:border-slate-700 hover:border-teal/30 hover:shadow-md transition-all"
          >
            <DocumentIcon :extension="doc.extension" :index="index" size="lg" />
            <div class="flex-1 min-w-0 cursor-pointer" @click="viewDocument(doc)">
              <p class="font-medium text-slate-900 dark:text-white truncate">{{ doc.name }}</p>
              <div class="flex items-center gap-3 mt-1 text-sm text-slate-500">
                <span>{{ doc.extension || 'Unknown' }}</span>
                <span>•</span>
                <span>{{ formatSize(doc.size) }}</span>
                <span>•</span>
                <span class="text-amber-600 dark:text-amber-400">Checked out {{ doc.checkedOutAt ? formatDate(doc.checkedOutAt) : '' }}</span>
              </div>
            </div>
            <div class="flex items-center gap-1 flex-shrink-0">
              <div class="relative group">
                <button
                  @click.stop="openPreview(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">open_in_new</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Preview
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="viewDocument(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">info</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Details
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="goToLocation(doc)"
                  class="p-2 text-slate-500 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">folder_open</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Go to Location
                </span>
              </div>
              <div class="relative group">
                <button
                  @click.stop="discardCheckout(doc)"
                  class="p-2 text-slate-500 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                >
                  <span class="material-symbols-outlined text-xl">close</span>
                </button>
                <span class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 text-xs font-medium text-white bg-slate-800 dark:bg-slate-700 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                  Discard Checkout
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Document Preview -->
    <DocumentViewer
      v-model="showPreview"
      :document="previewDocument"
    />
  </div>
</template>
