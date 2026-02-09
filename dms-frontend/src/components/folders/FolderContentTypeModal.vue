<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { contentTypeDefinitionsApi } from '@/api/client'
import type { ContentTypeDefinition, FolderContentTypeAssignment, EffectiveContentType } from '@/types'
import { UiSelect, UiCheckbox, UiToggle } from '@/components/ui'

interface Props {
  folderId: string
  folderName: string
  isCabinet?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  updated: []
}>()

// Data
const effectiveContentTypes = ref<EffectiveContentType[]>([])
const directAssignments = ref<FolderContentTypeAssignment[]>([])
const availableContentTypes = ref<ContentTypeDefinition[]>([])
const allContentTypes = ref<ContentTypeDefinition[]>([])

const isLoading = ref(false)
const isLoadingAssignments = ref(false)
const isSaving = ref(false)
const error = ref('')
const successMessage = ref('')

// Assignment form
const showAssignForm = ref(false)
const assignForm = ref({
  contentTypeId: '',
  isRequired: false,
  isDefault: false,
  inheritToChildren: true,
  displayOrder: 0
})

// Computed
const unassignedContentTypes = computed(() => {
  const assignedIds = new Set(directAssignments.value.map(a => a.contentTypeId))
  return allContentTypes.value.filter(ct => !assignedIds.has(ct.id) && ct.isActive)
})

const hasDirectAssignments = computed(() => directAssignments.value.length > 0)
const hasInheritedAssignments = computed(() => {
  return effectiveContentTypes.value.some(ct => ct.source !== 'Direct')
})

// Options for Select component
const contentTypeOptions = computed(() => {
  return unassignedContentTypes.value.map(ct => ({
    value: ct.id,
    label: ct.name + (ct.category ? ` (${ct.category})` : ''),
    icon: ct.icon
  }))
})

// Load data
onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  error.value = ''

  try {
    // Load all content types
    const allTypesRes = await contentTypeDefinitionsApi.getAll()
    allContentTypes.value = allTypesRes.data || []

    // Load effective content types (includes inheritance)
    const effectiveRes = await contentTypeDefinitionsApi.getEffectiveContentTypes(props.folderId)
    effectiveContentTypes.value = effectiveRes.data || []

    // Load direct assignments
    if (props.isCabinet) {
      const assignmentsRes = await contentTypeDefinitionsApi.getCabinetContentTypes(props.folderId)
      directAssignments.value = assignmentsRes.data || []
    } else {
      const assignmentsRes = await contentTypeDefinitionsApi.getFolderContentTypes(props.folderId)
      directAssignments.value = assignmentsRes.data || []
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to load content types'
  } finally {
    isLoading.value = false
  }
}

function getContentTypeName(contentTypeId: string): string {
  return allContentTypes.value.find(ct => ct.id === contentTypeId)?.name || 'Unknown'
}

function getContentTypeColor(contentTypeId: string): string {
  return allContentTypes.value.find(ct => ct.id === contentTypeId)?.color || '#00ae8c'
}

// Valid Material Symbols icon names for content types
const validIcons = new Set([
  'category', 'description', 'article', 'folder', 'note', 'assignment',
  'inventory_2', 'draft', 'file_present', 'task', 'checklist', 'list_alt',
  'receipt_long', 'contract', 'work', 'business', 'account_balance',
  'gavel', 'policy', 'verified', 'fact_check', 'rule', 'badge'
])

function getContentTypeIcon(contentTypeId: string): string {
  const icon = allContentTypes.value.find(ct => ct.id === contentTypeId)?.icon
  if (icon && validIcons.has(icon)) {
    return icon
  }
  return 'category' // Default icon
}

function openAssignForm() {
  assignForm.value = {
    contentTypeId: '',
    isRequired: false,
    isDefault: false,
    inheritToChildren: true,
    displayOrder: directAssignments.value.length
  }
  showAssignForm.value = true
}

async function handleAssign() {
  if (!assignForm.value.contentTypeId) return

  isSaving.value = true
  error.value = ''

  try {
    if (props.isCabinet) {
      await contentTypeDefinitionsApi.assignToCabinet(props.folderId, {
        contentTypeId: assignForm.value.contentTypeId,
        isRequired: assignForm.value.isRequired,
        isDefault: assignForm.value.isDefault,
        inheritToChildren: assignForm.value.inheritToChildren,
        displayOrder: assignForm.value.displayOrder
      })
    } else {
      await contentTypeDefinitionsApi.assignToFolder(props.folderId, {
        contentTypeId: assignForm.value.contentTypeId,
        isRequired: assignForm.value.isRequired,
        isDefault: assignForm.value.isDefault,
        inheritToChildren: assignForm.value.inheritToChildren,
        displayOrder: assignForm.value.displayOrder
      })
    }

    successMessage.value = 'Content type assigned successfully'
    showAssignForm.value = false
    await loadData()
    emit('updated')

    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to assign content type'
  } finally {
    isSaving.value = false
  }
}

async function handleSetDefault(contentTypeId: string) {
  isSaving.value = true
  error.value = ''

  try {
    if (props.isCabinet) {
      await contentTypeDefinitionsApi.setCabinetDefault(props.folderId, contentTypeId)
    } else {
      await contentTypeDefinitionsApi.setFolderDefault(props.folderId, contentTypeId)
    }

    successMessage.value = 'Default content type updated'
    await loadData()
    emit('updated')

    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to set default'
  } finally {
    isSaving.value = false
  }
}

async function handleRemove(contentTypeId: string) {
  if (!confirm('Are you sure you want to remove this content type assignment?')) return

  isSaving.value = true
  error.value = ''

  try {
    if (props.isCabinet) {
      await contentTypeDefinitionsApi.removeFromCabinet(props.folderId, contentTypeId)
    } else {
      await contentTypeDefinitionsApi.removeFromFolder(props.folderId, contentTypeId)
    }

    successMessage.value = 'Content type removed'
    await loadData()
    emit('updated')

    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to remove content type'
  } finally {
    isSaving.value = false
  }
}

async function handleToggleRequired(assignment: FolderContentTypeAssignment) {
  isSaving.value = true
  error.value = ''

  try {
    if (props.isCabinet) {
      await contentTypeDefinitionsApi.updateCabinetAssignment(props.folderId, assignment.contentTypeId, {
        isRequired: !assignment.isRequired,
        isDefault: assignment.isDefault,
        inheritToChildren: assignment.inheritToChildren,
        displayOrder: assignment.displayOrder
      })
    } else {
      await contentTypeDefinitionsApi.updateFolderAssignment(props.folderId, assignment.contentTypeId, {
        isRequired: !assignment.isRequired,
        isDefault: assignment.isDefault,
        inheritToChildren: assignment.inheritToChildren,
        displayOrder: assignment.displayOrder
      })
    }

    await loadData()
    emit('updated')
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to update'
  } finally {
    isSaving.value = false
  }
}

async function handleToggleInherit(assignment: FolderContentTypeAssignment) {
  isSaving.value = true
  error.value = ''

  try {
    if (props.isCabinet) {
      await contentTypeDefinitionsApi.updateCabinetAssignment(props.folderId, assignment.contentTypeId, {
        isRequired: assignment.isRequired,
        isDefault: assignment.isDefault,
        inheritToChildren: !assignment.inheritToChildren,
        displayOrder: assignment.displayOrder
      })
    } else {
      await contentTypeDefinitionsApi.updateFolderAssignment(props.folderId, assignment.contentTypeId, {
        isRequired: assignment.isRequired,
        isDefault: assignment.isDefault,
        inheritToChildren: !assignment.inheritToChildren,
        displayOrder: assignment.displayOrder
      })
    }

    await loadData()
    emit('updated')
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to update'
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4" @click.self="emit('close')">
        <Transition
          enter-active-class="duration-300 ease-out"
          enter-from-class="opacity-0 scale-95 translate-y-4"
          enter-to-class="opacity-100 scale-100 translate-y-0"
          leave-active-class="duration-200 ease-in"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div class="bg-white dark:bg-background-dark rounded-2xl shadow-2xl w-full max-w-2xl max-h-[85vh] overflow-hidden flex flex-col ring-1 ring-black/5 dark:ring-white/10">
            <!-- Header -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5 overflow-hidden">
              <div class="absolute top-0 right-0 w-48 h-48 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>

              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-xl flex items-center justify-center">
                    <span class="material-symbols-outlined text-white text-xl">category</span>
                  </div>
                  <div>
                    <h3 class="text-lg font-bold text-white">Manage Content Types</h3>
                    <p class="text-sm text-white/80 flex items-center gap-1">
                      <span class="material-symbols-outlined text-sm">{{ isCabinet ? 'inventory_2' : 'folder' }}</span>
                      {{ folderName }}
                    </p>
                  </div>
                </div>
                <button
                  @click="emit('close')"
                  class="w-9 h-9 flex items-center justify-center rounded-xl bg-white/10 hover:bg-white/20 transition-colors"
                >
                  <span class="material-symbols-outlined text-white">close</span>
                </button>
              </div>
            </div>

            <!-- Content -->
            <div class="flex-1 overflow-y-auto p-5">
              <!-- Messages -->
              <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
                <div v-if="error" class="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-xl flex items-center gap-2">
                  <span class="material-symbols-outlined text-red-500">error</span>
                  <p class="text-sm text-red-600 dark:text-red-400">{{ error }}</p>
                </div>
              </Transition>

              <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
                <div v-if="successMessage" class="mb-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800/50 rounded-xl flex items-center gap-2">
                  <span class="material-symbols-outlined text-green-500">check_circle</span>
                  <p class="text-sm text-green-600 dark:text-green-400">{{ successMessage }}</p>
                </div>
              </Transition>

              <!-- Loading -->
              <div v-if="isLoading" class="flex flex-col items-center justify-center py-12">
                <div class="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center mb-3">
                  <span class="material-symbols-outlined animate-spin text-primary text-2xl">progress_activity</span>
                </div>
                <p class="text-sm text-gray-500">Loading content types...</p>
              </div>

              <template v-else>
                <!-- Direct Assignments Section -->
                <div class="mb-6">
                  <div class="flex items-center justify-between mb-3">
                    <h4 class="text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                      <span class="material-symbols-outlined text-primary text-lg">assignment</span>
                      Direct Assignments
                    </h4>
                    <button
                      @click="openAssignForm"
                      :disabled="unassignedContentTypes.length === 0"
                      class="px-3 py-1.5 text-sm font-medium bg-gradient-to-r from-navy to-primary text-white rounded-lg hover:shadow-lg hover:shadow-primary/25 transition-all disabled:opacity-50 flex items-center gap-1.5"
                    >
                      <span class="material-symbols-outlined text-sm">add</span>
                      Add
                    </button>
                  </div>

                  <div v-if="directAssignments.length === 0" class="text-center py-8 bg-gray-50 dark:bg-surface-dark/50 rounded-xl border border-dashed border-gray-300 dark:border-gray-700">
                    <span class="material-symbols-outlined text-4xl text-gray-400 mb-2">category</span>
                    <p class="text-sm text-gray-500">No content types assigned directly</p>
                    <p class="text-xs text-gray-400 mt-1">Click "Add" to assign a content type</p>
                  </div>

                  <div v-else class="space-y-2">
                    <div
                      v-for="assignment in directAssignments"
                      :key="assignment.id"
                      class="p-3 bg-gray-50 dark:bg-surface-dark/50 rounded-xl border border-gray-200 dark:border-gray-700 hover:border-primary/30 transition-colors"
                    >
                      <div class="flex items-center gap-3">
                        <div
                          class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0"
                          :style="{ backgroundColor: getContentTypeColor(assignment.contentTypeId) }"
                        >
                          <span class="material-symbols-outlined text-white text-xl">{{ getContentTypeIcon(assignment.contentTypeId) }}</span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <p class="font-medium text-gray-900 dark:text-white">{{ getContentTypeName(assignment.contentTypeId) }}</p>
                          <div class="flex flex-wrap items-center gap-2 mt-1">
                            <span
                              v-if="assignment.isDefault"
                              class="px-2 py-0.5 text-xs font-medium bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded"
                            >
                              Default
                            </span>
                            <span
                              v-if="assignment.isRequired"
                              class="px-2 py-0.5 text-xs font-medium bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 rounded"
                            >
                              Required
                            </span>
                            <span
                              v-if="assignment.inheritToChildren"
                              class="px-2 py-0.5 text-xs font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded"
                            >
                              Inherits
                            </span>
                          </div>
                        </div>
                        <div class="flex items-center gap-1">
                          <button
                            v-if="!assignment.isDefault"
                            @click="handleSetDefault(assignment.contentTypeId)"
                            :disabled="isSaving"
                            class="p-2 text-gray-400 hover:text-green-500 hover:bg-green-50 dark:hover:bg-green-900/20 rounded-lg transition-colors"
                            title="Set as default"
                          >
                            <span class="material-symbols-outlined text-lg">check_circle</span>
                          </button>
                          <button
                            @click="handleToggleRequired(assignment)"
                            :disabled="isSaving"
                            class="p-2 text-gray-400 hover:text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-900/20 rounded-lg transition-colors"
                            :title="assignment.isRequired ? 'Make optional' : 'Make required'"
                          >
                            <span class="material-symbols-outlined text-lg">{{ assignment.isRequired ? 'star' : 'star_border' }}</span>
                          </button>
                          <button
                            @click="handleToggleInherit(assignment)"
                            :disabled="isSaving"
                            class="p-2 text-gray-400 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg transition-colors"
                            :title="assignment.inheritToChildren ? 'Stop inheritance' : 'Enable inheritance'"
                          >
                            <span class="material-symbols-outlined text-lg">{{ assignment.inheritToChildren ? 'subdirectory_arrow_right' : 'remove' }}</span>
                          </button>
                          <button
                            @click="handleRemove(assignment.contentTypeId)"
                            :disabled="isSaving"
                            class="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                            title="Remove"
                          >
                            <span class="material-symbols-outlined text-lg">delete</span>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Inherited Assignments Section -->
                <div v-if="hasInheritedAssignments">
                  <h4 class="text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2 mb-3">
                    <span class="material-symbols-outlined text-blue-500 text-lg">subdirectory_arrow_right</span>
                    Inherited Content Types
                  </h4>

                  <div class="space-y-2">
                    <div
                      v-for="ct in effectiveContentTypes.filter(c => c.source !== 'Direct')"
                      :key="ct.contentTypeId"
                      class="p-3 bg-blue-50/50 dark:bg-blue-900/10 rounded-xl border border-blue-200/50 dark:border-blue-800/30"
                    >
                      <div class="flex items-center gap-3">
                        <div
                          class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 opacity-80"
                          :style="{ backgroundColor: ct.color || '#00ae8c' }"
                        >
                          <span class="material-symbols-outlined text-white text-xl">{{ ct.icon && validIcons.has(ct.icon) ? ct.icon : 'category' }}</span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <p class="font-medium text-gray-700 dark:text-gray-300">{{ ct.name }}</p>
                          <div class="flex flex-wrap items-center gap-2 mt-1">
                            <span class="px-2 py-0.5 text-xs font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded flex items-center gap-1">
                              <span class="material-symbols-outlined text-xs">{{ ct.source === 'Cabinet' ? 'inventory_2' : 'folder' }}</span>
                              {{ ct.source }}: {{ ct.sourceName }}
                            </span>
                            <span
                              v-if="ct.isDefault"
                              class="px-2 py-0.5 text-xs font-medium bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded"
                            >
                              Default
                            </span>
                            <span
                              v-if="ct.isRequired"
                              class="px-2 py-0.5 text-xs font-medium bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 rounded"
                            >
                              Required
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Empty state when no content types at all -->
                <div v-if="!hasDirectAssignments && !hasInheritedAssignments" class="text-center py-8">
                  <div class="w-16 h-16 rounded-2xl bg-gray-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
                    <span class="material-symbols-outlined text-4xl text-gray-400">category</span>
                  </div>
                  <p class="text-sm font-medium text-gray-700 dark:text-gray-300">No Content Types</p>
                  <p class="text-xs text-gray-500 mt-1">This {{ isCabinet ? 'cabinet' : 'folder' }} has no content types assigned or inherited</p>
                </div>
              </template>
            </div>

            <!-- Assign Form Modal -->
            <Transition
              enter-active-class="duration-200 ease-out"
              enter-from-class="opacity-0"
              enter-to-class="opacity-100"
              leave-active-class="duration-150 ease-in"
              leave-from-class="opacity-100"
              leave-to-class="opacity-0"
            >
              <div v-if="showAssignForm" class="absolute inset-0 bg-black/40 flex items-center justify-center p-6">
                <div class="bg-white dark:bg-background-dark rounded-xl shadow-xl w-full max-w-md p-5">
                  <h4 class="text-base font-semibold text-gray-900 dark:text-white mb-4 flex items-center gap-2">
                    <span class="material-symbols-outlined text-primary">add_circle</span>
                    Assign Content Type
                  </h4>

                  <div class="space-y-4">
                    <UiSelect
                      v-model="assignForm.contentTypeId"
                      :options="contentTypeOptions"
                      label="Content Type"
                      placeholder="Select a content type"
                      searchable
                    />

                    <div class="flex items-center gap-6">
                      <UiCheckbox
                        v-model="assignForm.isRequired"
                        label="Required"
                        description="Users must select this type"
                        color="red"
                      />
                      <UiCheckbox
                        v-model="assignForm.isDefault"
                        label="Default"
                        description="Auto-select on upload"
                        color="green"
                      />
                    </div>

                    <UiToggle
                      v-model="assignForm.inheritToChildren"
                      label="Inherit to child folders"
                      description="Child folders will inherit this content type"
                    />
                  </div>

                  <div class="flex justify-end gap-3 mt-6">
                    <button
                      @click="showAssignForm = false"
                      class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-surface-dark rounded-lg transition-colors"
                    >
                      Cancel
                    </button>
                    <button
                      @click="handleAssign"
                      :disabled="!assignForm.contentTypeId || isSaving"
                      class="px-4 py-2 text-sm font-medium bg-gradient-to-r from-navy to-primary text-white rounded-lg hover:shadow-lg hover:shadow-primary/25 transition-all disabled:opacity-50 flex items-center gap-2"
                    >
                      <span v-if="isSaving" class="material-symbols-outlined animate-spin text-sm">progress_activity</span>
                      {{ isSaving ? 'Saving...' : 'Assign' }}
                    </button>
                  </div>
                </div>
              </div>
            </Transition>

            <!-- Footer -->
            <div class="border-t border-gray-200 dark:border-gray-700/50 p-4 bg-gray-50 dark:bg-surface-dark/50">
              <div class="flex justify-end">
                <button
                  @click="emit('close')"
                  class="px-5 py-2 text-sm font-medium bg-gradient-to-r from-navy to-primary text-white rounded-xl hover:shadow-lg hover:shadow-primary/25 transition-all"
                >
                  Done
                </button>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>
