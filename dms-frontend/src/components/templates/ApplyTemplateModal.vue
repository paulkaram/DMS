<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { folderTemplatesApi } from '@/api/client'
import type { FolderTemplate, ApplyTemplateResult } from '@/types'

const props = defineProps<{
  folderId: string
  folderName: string
  isCabinet?: boolean
}>()

const emit = defineEmits<{
  close: []
  applied: [result: ApplyTemplateResult]
}>()

const templates = ref<FolderTemplate[]>([])
const selectedTemplateId = ref<string>('')
const namePrefix = ref('')
const isLoading = ref(false)
const isApplying = ref(false)
const previewResult = ref<ApplyTemplateResult | null>(null)

const selectedTemplate = computed(() =>
  templates.value.find(t => t.id === selectedTemplateId.value) || null
)

onMounted(async () => {
  await loadTemplates()
})

async function loadTemplates() {
  isLoading.value = true
  try {
    const response = await folderTemplatesApi.getAll()
    templates.value = response.data.filter((t: FolderTemplate) => t.isActive)
    // Auto-select default template if available
    const defaultTemplate = templates.value.find(t => t.isDefault)
    if (defaultTemplate) {
      selectedTemplateId.value = defaultTemplate.id
    }
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

watch(selectedTemplateId, async (newId) => {
  if (newId) {
    await loadPreview()
  } else {
    previewResult.value = null
  }
})

async function loadPreview() {
  if (!selectedTemplateId.value) return

  try {
    const response = props.isCabinet
      ? await folderTemplatesApi.previewCabinetApplication(props.folderId, selectedTemplateId.value)
      : await folderTemplatesApi.previewApplication(props.folderId, selectedTemplateId.value)
    previewResult.value = response.data
  } catch (err) {
    previewResult.value = null
  }
}

async function applyTemplate() {
  if (!selectedTemplateId.value) return

  isApplying.value = true
  try {
    const response = props.isCabinet
      ? await folderTemplatesApi.applyToCabinet(props.folderId, {
          templateId: selectedTemplateId.value,
          namePrefix: namePrefix.value || undefined
        })
      : await folderTemplatesApi.applyToFolder(props.folderId, {
          templateId: selectedTemplateId.value,
          namePrefix: namePrefix.value || undefined
        })
    emit('applied', response.data)
    emit('close')
  } catch (err: any) {
    alert(err.response?.data?.errors?.[0] || 'Failed to apply template')
  } finally {
    isApplying.value = false
  }
}

function countTotalFolders(nodes: any[]): number {
  return nodes.reduce((count: number, node: any) => count + 1 + countTotalFolders(node.children || []), 0)
}
</script>

<template>
  <div
    class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center z-50"
    @click.self="emit('close')"
  >
    <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl ring-1 ring-black/5 dark:ring-white/10 w-full max-w-lg max-h-[80vh] flex flex-col overflow-hidden">
      <!-- Header with brand gradient -->
      <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary px-6 py-5 overflow-hidden">
        <!-- Decorative elements -->
        <div class="absolute top-0 right-0 w-32 h-32 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
        <div class="absolute bottom-0 left-0 w-20 h-20 bg-primary/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>

        <div class="relative flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-lg flex items-center justify-center">
              <span class="material-symbols-outlined text-white text-xl">folder_special</span>
            </div>
            <div>
              <h3 class="text-lg font-semibold text-white">New Folder from Template</h3>
              <p class="text-sm text-white/70">
                Create folder structure in {{ isCabinet ? 'cabinet' : 'folder' }} "{{ folderName }}"
              </p>
            </div>
          </div>
          <button
            @click="emit('close')"
            class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors"
          >
            <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto p-6 space-y-4">
        <!-- Template Selection -->
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">Select Template</label>

          <div v-if="isLoading" class="text-center py-4 text-zinc-500">
            Loading templates...
          </div>

          <div v-else-if="templates.length === 0" class="text-center py-4 text-zinc-500">
            <span class="material-symbols-outlined text-3xl mb-2 opacity-50">folder_off</span>
            <p>No templates available</p>
            <p class="text-sm">Create templates in Admin > Folder Templates</p>
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="template in templates"
              :key="template.id"
              @click="selectedTemplateId = template.id"
              class="p-3 border rounded-lg cursor-pointer transition-all"
              :class="selectedTemplateId === template.id
                ? 'border-teal bg-teal/5 ring-1 ring-teal'
                : 'border-zinc-200 dark:border-border-dark hover:border-zinc-300'"
            >
              <div class="flex items-center gap-3">
                <span class="material-symbols-outlined text-zinc-400">{{ template.icon || 'folder_special' }}</span>
                <div class="flex-1">
                  <div class="font-medium text-zinc-800 dark:text-zinc-200 flex items-center gap-2">
                    {{ template.name }}
                    <span v-if="template.isDefault" class="text-xs bg-teal/10 text-teal px-1.5 py-0.5 rounded">Default</span>
                  </div>
                  <div class="text-xs text-zinc-500">
                    {{ template.category || 'Uncategorized' }}
                    <span class="mx-1">•</span>
                    {{ countTotalFolders(template.nodes || []) }} folders
                  </div>
                </div>
                <span
                  v-if="selectedTemplateId === template.id"
                  class="material-symbols-outlined text-teal"
                >
                  check_circle
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Name Prefix (optional) -->
        <div v-if="selectedTemplateId">
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
            Name Prefix (optional)
          </label>
          <input
            v-model="namePrefix"
            type="text"
            class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
            placeholder="e.g., 2024 Project"
          />
          <p class="text-xs text-zinc-500 mt-1">
            Prefix will be added to all folder names (e.g., "2024 Project - Documents")
          </p>
        </div>

        <!-- Preview -->
        <div v-if="previewResult && selectedTemplateId">
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
            Preview: Folders to Create
          </label>
          <div class="bg-zinc-50 dark:bg-border-dark/50 rounded-lg p-3 max-h-48 overflow-y-auto">
            <div
              v-for="(path, index) in previewResult.createdFolderPaths"
              :key="index"
              class="flex items-center gap-2 py-1 text-sm text-zinc-600 dark:text-zinc-300"
            >
              <span class="material-symbols-outlined text-amber-500 text-base">folder</span>
              {{ namePrefix ? `${namePrefix} - ${path}` : path }}
            </div>
          </div>
          <p class="text-xs text-zinc-500 mt-2">
            {{ previewResult.foldersCreated }} folder(s) will be created
          </p>
        </div>
      </div>

      <!-- Footer -->
      <div class="px-6 py-4 bg-gray-50 dark:bg-surface-dark/50 border-t border-gray-200 dark:border-gray-700/50 flex items-center justify-end gap-3">
        <button
          @click="emit('close')"
          class="px-4 py-2.5 text-zinc-600 hover:text-zinc-800 dark:text-zinc-400 dark:hover:text-zinc-200 font-medium transition-colors"
        >
          Cancel
        </button>
        <button
          @click="applyTemplate"
          :disabled="!selectedTemplateId || isApplying"
          class="flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-teal to-teal/90 text-white rounded-lg hover:from-teal/90 hover:to-teal/80 disabled:opacity-50 disabled:cursor-not-allowed font-medium shadow-lg shadow-teal/25 transition-all"
        >
          <span v-if="isApplying" class="material-symbols-outlined animate-spin text-lg">progress_activity</span>
          <span class="material-symbols-outlined text-lg" v-else>play_arrow</span>
          {{ isApplying ? 'Applying...' : 'Apply Template' }}
        </button>
      </div>
    </div>
  </div>
</template>
