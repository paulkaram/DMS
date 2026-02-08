<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { contentTypeDefinitionsApi, foldersApi, cabinetsApi } from '@/api/client'
import type { ContentTypeDefinition, FolderContentTypeAssignment } from '@/types'
import { UiCheckbox } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

interface Cabinet {
  id: string
  name: string
}

interface Folder {
  id: string
  name: string
  cabinetId: string
  parentFolderId?: string
  children?: Folder[]
}

// Data
const contentTypes = ref<ContentTypeDefinition[]>([])
const cabinets = ref<Cabinet[]>([])
const folderTree = ref<Folder[]>([])
const selectedFolderId = ref<string>('')
const selectedCabinetId = ref<string>('')
const folderAssignments = ref<FolderContentTypeAssignment[]>([])
const availableContentTypes = ref<ContentTypeDefinition[]>([])
const expandedFolders = ref<Set<string>>(new Set())

const isLoading = ref(false)
const isLoadingAssignments = ref(false)

// Modal state
const showAssignModal = ref(false)
const assignFormData = ref({
  contentTypeId: '',
  isRequired: false,
  inheritToChildren: true
})

// Grouped content types by category
const groupedContentTypes = computed(() => {
  const groups: Record<string, ContentTypeDefinition[]> = {}
  for (const ct of contentTypes.value) {
    const category = ct.category || 'Uncategorized'
    if (!groups[category]) {
      groups[category] = []
    }
    groups[category].push(ct)
  }
  return groups
})

const selectedFolder = computed(() => {
  if (!selectedFolderId.value) return null
  return findFolderById(folderTree.value, selectedFolderId.value)
})

// Flatten folder tree for rendering
const flattenedFolders = computed(() => {
  const result: { folder: Folder; depth: number; hasChildren: boolean }[] = []

  function flatten(folders: Folder[], depth: number) {
    for (const folder of folders) {
      const hasChildren = !!(folder.children && folder.children.length > 0)
      result.push({ folder, depth, hasChildren })

      if (hasChildren && expandedFolders.value.has(folder.id)) {
        flatten(folder.children!, depth + 1)
      }
    }
  }

  flatten(folderTree.value, 0)
  return result
})

function findFolderById(folders: Folder[], id: string): Folder | null {
  for (const folder of folders) {
    if (folder.id === id) return folder
    if (folder.children) {
      const found = findFolderById(folder.children, id)
      if (found) return found
    }
  }
  return null
}

function toggleFolder(folderId: string) {
  if (expandedFolders.value.has(folderId)) {
    expandedFolders.value.delete(folderId)
  } else {
    expandedFolders.value.add(folderId)
  }
}

function isFolderExpanded(folderId: string): boolean {
  return expandedFolders.value.has(folderId)
}

onMounted(async () => {
  await loadInitialData()
})

async function loadInitialData() {
  isLoading.value = true
  try {
    const [contentTypesRes, cabinetsRes] = await Promise.all([
      contentTypeDefinitionsApi.getAll(),
      cabinetsApi.getAll()
    ])
    contentTypes.value = contentTypesRes.data.filter((ct: ContentTypeDefinition) => ct.allowOnFolders && ct.isActive)
    cabinets.value = cabinetsRes.data || []
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

async function onCabinetChange() {
  if (!selectedCabinetId.value) {
    folderTree.value = []
    selectedFolderId.value = ''
    expandedFolders.value.clear()
    return
  }

  try {
    const response = await foldersApi.getTree(selectedCabinetId.value)
    folderTree.value = response.data || []
    selectedFolderId.value = ''
    folderAssignments.value = []
    expandedFolders.value.clear()
    // Auto-expand first level
    for (const folder of folderTree.value) {
      expandedFolders.value.add(folder.id)
    }
  } catch (err) {
  }
}

async function onFolderSelect(folderId: string) {
  selectedFolderId.value = folderId
  await loadFolderAssignments()
}

async function loadFolderAssignments() {
  if (!selectedFolderId.value) return

  isLoadingAssignments.value = true
  try {
    const [assignmentsRes, availableRes] = await Promise.all([
      contentTypeDefinitionsApi.getFolderContentTypes(selectedFolderId.value),
      contentTypeDefinitionsApi.getAvailableContentTypes(selectedFolderId.value)
    ])
    folderAssignments.value = assignmentsRes.data || []
    availableContentTypes.value = availableRes.data || []
  } catch (err) {
    // Fallback: show all content types as available if API doesn't exist yet
    folderAssignments.value = []
    availableContentTypes.value = contentTypes.value.filter(
      ct => !folderAssignments.value.some(a => a.contentTypeId === ct.id)
    )
  } finally {
    isLoadingAssignments.value = false
  }
}

function openAssignModal() {
  assignFormData.value = {
    contentTypeId: '',
    isRequired: false,
    inheritToChildren: true
  }
  showAssignModal.value = true
}

async function assignContentType() {
  if (!selectedFolderId.value || !assignFormData.value.contentTypeId) return

  try {
    await contentTypeDefinitionsApi.assignToFolder(selectedFolderId.value, {
      contentTypeId: assignFormData.value.contentTypeId,
      isRequired: assignFormData.value.isRequired,
      inheritToChildren: assignFormData.value.inheritToChildren
    })
    showAssignModal.value = false
    await loadFolderAssignments()
  } catch (err) {
  }
}

async function removeAssignment(contentTypeId: string) {
  if (!confirm('Remove this content type from the folder?')) return

  try {
    await contentTypeDefinitionsApi.removeFromFolder(selectedFolderId.value, contentTypeId)
    await loadFolderAssignments()
  } catch (err) {
  }
}

function getContentTypeById(id: string): ContentTypeDefinition | undefined {
  return contentTypes.value.find(ct => ct.id === id)
}

function getCategoryColor(category: string): string {
  const colors: Record<string, string> = {
    'General': '#6B7280',
    'Financial': '#10B981',
    'Legal': '#8B5CF6',
    'HR': '#F59E0B',
    'Technical': '#3B82F6',
    'Administrative': '#EC4899',
    'Compliance': '#EF4444',
    'Project': '#06B6D4'
  }
  return colors[category] || '#6B7280'
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-7xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Folder Content Types" icon="folder_special" />

      <!-- Header -->
      <div class="mb-6">
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Folder Content Types</h1>
        <p class="text-gray-500 dark:text-gray-400 mt-1">Assign content type definitions to folders to enable metadata capture on document upload</p>
      </div>

      <div class="grid grid-cols-12 gap-6">
        <!-- Left Panel: Folder Browser -->
        <div class="col-span-4">
          <div class="bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-gray-200 dark:border-slate-700 p-4">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">Select Folder</h2>

            <!-- Cabinet Selector -->
            <div class="mb-4">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Cabinet</label>
              <select
                v-model="selectedCabinetId"
                @change="onCabinetChange"
                class="w-full px-3 py-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
              >
                <option value="">-- Select Cabinet --</option>
                <option v-for="cabinet in cabinets" :key="cabinet.id" :value="cabinet.id">
                  {{ cabinet.name }}
                </option>
              </select>
            </div>

            <!-- Folder Tree -->
            <div v-if="selectedCabinetId" class="border border-gray-200 dark:border-slate-700 rounded-lg max-h-96 overflow-y-auto">
              <div v-if="folderTree.length === 0" class="p-4 text-center text-gray-500">
                No folders in this cabinet
              </div>
              <div v-else class="p-2">
                <div
                  v-for="{ folder, depth, hasChildren } in flattenedFolders"
                  :key="folder.id"
                  class="flex items-center gap-2 px-2 py-1.5 rounded cursor-pointer hover:bg-gray-100 dark:hover:bg-slate-700"
                  :class="{ 'bg-teal/10': selectedFolderId === folder.id }"
                  :style="{ paddingLeft: (depth * 16 + 8) + 'px' }"
                  @click="onFolderSelect(folder.id)"
                >
                  <button
                    v-if="hasChildren"
                    @click.stop="toggleFolder(folder.id)"
                    class="w-4 h-4 flex items-center justify-center text-gray-400"
                  >
                    <svg
                      class="w-3 h-3 transition-transform"
                      :class="{ 'rotate-90': isFolderExpanded(folder.id) }"
                      fill="currentColor"
                      viewBox="0 0 20 20"
                    >
                      <path fill-rule="evenodd" d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z" clip-rule="evenodd" />
                    </svg>
                  </button>
                  <span v-else class="w-4"></span>
                  <svg class="w-4 h-4 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
                  </svg>
                  <span class="text-sm truncate" :class="selectedFolderId === folder.id ? 'text-teal font-medium' : 'text-gray-900 dark:text-white'">
                    {{ folder.name }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Available Content Types -->
          <div class="bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-gray-200 dark:border-slate-700 p-4 mt-4">
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">Available Content Types</h2>

            <div v-if="isLoading" class="text-center py-4 text-gray-500">Loading...</div>

            <div v-else-if="contentTypes.length === 0" class="text-center py-4 text-gray-500">
              No content types defined. Create them in Content Type Builder.
            </div>

            <div v-else class="space-y-4">
              <div v-for="(types, category) in groupedContentTypes" :key="category">
                <div class="flex items-center gap-2 mb-2">
                  <div class="w-3 h-3 rounded-full" :style="{ backgroundColor: getCategoryColor(category as string) }"></div>
                  <span class="text-sm font-medium text-gray-700 dark:text-gray-300">{{ category }}</span>
                </div>
                <div class="space-y-1 pl-5">
                  <div
                    v-for="ct in types"
                    :key="ct.id"
                    class="flex items-center gap-2 p-2 bg-gray-50 dark:bg-slate-800 rounded text-sm"
                  >
                    <div class="w-6 h-6 rounded flex items-center justify-center text-white text-xs font-bold" :style="{ backgroundColor: ct.color }">
                      {{ ct.name.charAt(0) }}
                    </div>
                    <span class="text-gray-900 dark:text-white">{{ ct.name }}</span>
                    <span v-if="ct.fieldCount" class="text-xs text-gray-500">({{ ct.fieldCount }} fields)</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Panel: Folder Assignments -->
        <div class="col-span-8">
          <div class="bg-white dark:bg-slate-900 rounded-xl shadow-sm border border-gray-200 dark:border-slate-700 p-6">
            <div v-if="!selectedFolderId" class="text-center py-12">
              <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z" />
              </svg>
              <p class="text-lg font-medium text-gray-500">Select a folder</p>
              <p class="text-sm text-gray-400 mt-1">Choose a folder from the left panel to manage its content types</p>
            </div>

            <template v-else>
              <div class="flex items-center justify-between mb-6">
                <div>
                  <h2 class="text-lg font-semibold text-gray-900 dark:text-white">
                    Content Types for "{{ selectedFolder?.name }}"
                  </h2>
                  <p class="text-sm text-gray-500 mt-1">
                    Documents uploaded to this folder will require metadata from assigned content types
                  </p>
                </div>
                <button
                  @click="openAssignModal"
                  class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90"
                >
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                  </svg>
                  Assign Content Type
                </button>
              </div>

              <div v-if="isLoadingAssignments" class="text-center py-8 text-gray-500">
                Loading assignments...
              </div>

              <div v-else-if="folderAssignments.length === 0" class="text-center py-12 bg-gray-50 dark:bg-slate-800 rounded-lg">
                <svg class="w-12 h-12 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                <p class="text-gray-500">No content types assigned to this folder</p>
                <p class="text-sm text-gray-400 mt-1">Click "Assign Content Type" to add metadata requirements</p>
              </div>

              <div v-else class="space-y-4">
                <div
                  v-for="assignment in folderAssignments"
                  :key="assignment.id"
                  class="flex items-center justify-between p-4 bg-gray-50 dark:bg-slate-800 rounded-lg border border-gray-200 dark:border-slate-700"
                >
                  <div class="flex items-center gap-4">
                    <div
                      class="w-12 h-12 rounded-lg flex items-center justify-center text-white font-bold text-lg"
                      :style="{ backgroundColor: getContentTypeById(assignment.contentTypeId)?.color || '#6B7280' }"
                    >
                      {{ (assignment.contentTypeName || 'CT').charAt(0) }}
                    </div>
                    <div>
                      <h3 class="font-medium text-gray-900 dark:text-white">
                        {{ assignment.contentTypeName || 'Unknown Content Type' }}
                      </h3>
                      <p v-if="assignment.contentTypeDescription" class="text-sm text-gray-500">
                        {{ assignment.contentTypeDescription }}
                      </p>
                      <div class="flex items-center gap-3 mt-1">
                        <span v-if="assignment.isRequired" class="px-2 py-0.5 text-xs bg-red-100 text-red-700 rounded">
                          Required
                        </span>
                        <span v-if="assignment.inheritToChildren" class="px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded">
                          Inherited by children
                        </span>
                      </div>
                    </div>
                  </div>
                  <button
                    @click="removeAssignment(assignment.contentTypeId)"
                    class="p-2 text-gray-400 hover:text-red-600 transition-colors"
                    title="Remove assignment"
                  >
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </div>
            </template>
          </div>
        </div>
      </div>
    </div>

    <!-- Assign Modal -->
    <div v-if="showAssignModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-xl w-full max-w-md mx-4 p-6">
        <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">Assign Content Type</h3>

        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Content Type</label>
            <select
              v-model="assignFormData.contentTypeId"
              class="w-full px-4 py-2 border border-gray-300 dark:border-slate-600 rounded-lg bg-white dark:bg-slate-800 text-gray-900 dark:text-white"
            >
              <option value="">-- Select Content Type --</option>
              <optgroup v-for="(types, category) in groupedContentTypes" :key="category" :label="String(category)">
                <option
                  v-for="ct in types"
                  :key="ct.id"
                  :value="ct.id"
                  :disabled="folderAssignments.some(a => a.contentTypeId === ct.id)"
                >
                  {{ ct.name }} {{ ct.fieldCount ? `(${ct.fieldCount} fields)` : '' }}
                </option>
              </optgroup>
            </select>
          </div>

          <UiCheckbox
            v-model="assignFormData.isRequired"
            label="Required - Users must fill metadata when uploading"
          />

          <UiCheckbox
            v-model="assignFormData.inheritToChildren"
            label="Inherit to child folders"
          />
        </div>

        <div class="mt-6 flex justify-end gap-3">
          <button
            @click="showAssignModal = false"
            class="px-4 py-2 border border-gray-300 dark:border-slate-600 rounded-lg hover:bg-gray-50 dark:hover:bg-slate-800"
          >
            Cancel
          </button>
          <button
            @click="assignContentType"
            :disabled="!assignFormData.contentTypeId"
            class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 disabled:opacity-50"
          >
            Assign
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
