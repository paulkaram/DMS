<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { folderTemplatesApi, contentTypeDefinitionsApi } from '@/api/client'
import type { FolderTemplate, FolderTemplateNode, ContentTypeDefinition } from '@/types'
import { UiCheckbox, UiSelect } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'
import FolderTreeNode from '@/components/templates/FolderTreeNode.vue'

const templates = ref<FolderTemplate[]>([])
const contentTypes = ref<ContentTypeDefinition[]>([])
const isLoading = ref(false)
const showModal = ref(false)
const showNodeModal = ref(false)
const isEditing = ref(false)
const selectedTemplate = ref<FolderTemplate | null>(null)

const formData = ref({
  id: '',
  name: '',
  description: '',
  category: '',
  icon: 'folder_special',
  isDefault: false,
  isActive: true
})

const nodeFormData = ref({
  id: '',
  parentNodeId: null as string | null,
  name: '',
  description: '',
  contentTypeId: null as string | null,
  sortOrder: 0,
  breakContentTypeInheritance: false
})

const categories = ref<string[]>([])
const editingNode = ref<FolderTemplateNode | null>(null)

onMounted(async () => {
  await Promise.all([loadTemplates(), loadContentTypes(), loadCategories()])
})

async function loadTemplates() {
  isLoading.value = true
  try {
    const response = await folderTemplatesApi.getAll(true)
    templates.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

async function loadContentTypes() {
  try {
    const response = await contentTypeDefinitionsApi.getAll()
    contentTypes.value = response.data
  } catch (err) {
  }
}

async function loadCategories() {
  try {
    const response = await folderTemplatesApi.getCategories()
    categories.value = response.data || []
  } catch (err) {
  }
}

function openCreateModal() {
  formData.value = {
    id: '',
    name: '',
    description: '',
    category: '',
    icon: 'folder_special',
    isDefault: false,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(template: FolderTemplate) {
  formData.value = {
    id: template.id,
    name: template.name,
    description: template.description || '',
    category: template.category || '',
    icon: template.icon || 'folder_special',
    isDefault: template.isDefault,
    isActive: template.isActive
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  try {
    if (isEditing.value) {
      await folderTemplatesApi.update(formData.value.id, {
        name: formData.value.name,
        description: formData.value.description || undefined,
        category: formData.value.category || undefined,
        icon: formData.value.icon || undefined,
        isDefault: formData.value.isDefault,
        isActive: formData.value.isActive
      })
    } else {
      await folderTemplatesApi.create({
        name: formData.value.name,
        description: formData.value.description || undefined,
        category: formData.value.category || undefined,
        icon: formData.value.icon || undefined,
        isDefault: formData.value.isDefault,
        nodes: []
      })
    }
    showModal.value = false
    await loadTemplates()
  } catch (err) {
    alert('Failed to save template')
  }
}

async function deleteTemplate(id: string) {
  if (!confirm('Are you sure you want to delete this template?')) return
  try {
    await folderTemplatesApi.delete(id)
    if (selectedTemplate.value?.id === id) {
      selectedTemplate.value = null
    }
    await loadTemplates()
  } catch (err) {
  }
}

async function duplicateTemplate(template: FolderTemplate) {
  const newName = prompt('Enter name for duplicated template:', `${template.name} (Copy)`)
  if (!newName) return
  try {
    await folderTemplatesApi.duplicate(template.id, newName)
    await loadTemplates()
  } catch (err) {
  }
}

async function selectTemplate(template: FolderTemplate) {
  try {
    const response = await folderTemplatesApi.getById(template.id)
    selectedTemplate.value = response.data
  } catch (err) {
  }
}

function openAddNodeModal(parentNode?: FolderTemplateNode) {
  nodeFormData.value = {
    id: '',
    parentNodeId: parentNode?.id || null,
    name: '',
    description: '',
    contentTypeId: null,
    sortOrder: 0,
    breakContentTypeInheritance: false
  }
  editingNode.value = null
  showNodeModal.value = true
}

function openEditNodeModal(node: FolderTemplateNode) {
  nodeFormData.value = {
    id: node.id,
    parentNodeId: node.parentNodeId || null,
    name: node.name,
    description: node.description || '',
    contentTypeId: node.contentTypeId || null,
    sortOrder: node.sortOrder,
    breakContentTypeInheritance: node.breakContentTypeInheritance
  }
  editingNode.value = node
  showNodeModal.value = true
}

async function handleSaveNode() {
  if (!selectedTemplate.value) return

  try {
    if (editingNode.value) {
      await folderTemplatesApi.updateNode(nodeFormData.value.id, {
        parentNodeId: nodeFormData.value.parentNodeId || undefined,
        name: nodeFormData.value.name,
        description: nodeFormData.value.description || undefined,
        contentTypeId: nodeFormData.value.contentTypeId || undefined,
        sortOrder: nodeFormData.value.sortOrder,
        breakContentTypeInheritance: nodeFormData.value.breakContentTypeInheritance
      })
    } else {
      await folderTemplatesApi.addNode(selectedTemplate.value.id, {
        parentNodeId: nodeFormData.value.parentNodeId || undefined,
        name: nodeFormData.value.name,
        description: nodeFormData.value.description || undefined,
        contentTypeId: nodeFormData.value.contentTypeId || undefined,
        sortOrder: nodeFormData.value.sortOrder,
        breakContentTypeInheritance: nodeFormData.value.breakContentTypeInheritance,
        children: []
      })
    }
    showNodeModal.value = false
    await selectTemplate(selectedTemplate.value)
  } catch (err) {
    alert('Failed to save folder node')
  }
}

async function deleteNode(nodeId: string) {
  if (!confirm('Are you sure you want to delete this folder from the template?')) return
  if (!selectedTemplate.value) return

  try {
    await folderTemplatesApi.deleteNode(nodeId)
    await selectTemplate(selectedTemplate.value)
  } catch (err) {
  }
}

function getContentTypeName(id?: string): string {
  if (!id) return 'None'
  const ct = contentTypes.value.find(c => c.id === id)
  return ct?.name || 'Unknown'
}

const contentTypeOptions = computed(() => [
  { value: null, label: 'None' },
  ...contentTypes.value.map(ct => ({ value: ct.id, label: ct.name }))
])

function countNodes(nodes: FolderTemplateNode[]): number {
  return nodes.reduce((count, node) => count + 1 + countNodes(node.children || []), 0)
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-7xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Folder Templates" icon="account_tree" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-semibold text-zinc-800 dark:text-zinc-100">Folder Templates</h1>
          <p class="text-sm text-zinc-500 mt-1">Create reusable folder structures with content type assignments</p>
        </div>
        <button
          @click="openCreateModal"
          class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors"
        >
          <span class="material-symbols-outlined text-lg">add</span>
          New Template
        </button>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Templates List -->
        <div class="lg:col-span-1">
          <div class="bg-white dark:bg-surface-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark">
            <div class="px-4 py-3 border-b border-zinc-200 dark:border-border-dark">
              <h2 class="font-medium text-zinc-700 dark:text-zinc-200">Templates</h2>
            </div>

            <div v-if="isLoading" class="p-8 text-center text-zinc-500">
              Loading templates...
            </div>

            <div v-else-if="templates.length === 0" class="p-8 text-center text-zinc-500">
              <span class="material-symbols-outlined text-4xl mb-2 opacity-50">folder_off</span>
              <p>No templates yet</p>
              <p class="text-sm">Create your first template to get started</p>
            </div>

            <div v-else class="divide-y divide-zinc-200 dark:divide-zinc-700 max-h-[600px] overflow-y-auto">
              <div
                v-for="template in templates"
                :key="template.id"
                @click="selectTemplate(template)"
                class="p-4 cursor-pointer hover:bg-zinc-50 dark:hover:bg-border-dark/50 transition-colors"
                :class="{ 'bg-teal/5 border-l-2 border-teal': selectedTemplate?.id === template.id }"
              >
                <div class="flex items-start justify-between">
                  <div class="flex items-center gap-3">
                    <span class="material-symbols-outlined text-xl text-zinc-400">{{ template.icon || 'folder_special' }}</span>
                    <div>
                      <div class="font-medium text-zinc-800 dark:text-zinc-200 flex items-center gap-2">
                        {{ template.name }}
                        <span v-if="template.isDefault" class="text-xs bg-teal/10 text-teal px-1.5 py-0.5 rounded">Default</span>
                        <span v-if="!template.isActive" class="text-xs bg-red-100 text-red-600 px-1.5 py-0.5 rounded">Inactive</span>
                      </div>
                      <div class="text-xs text-zinc-500 mt-0.5">
                        {{ template.category || 'Uncategorized' }}
                        <span class="mx-1">•</span>
                        {{ template.usageCount }} uses
                      </div>
                    </div>
                  </div>
                  <div class="flex items-center gap-1">
                    <button
                      @click.stop="duplicateTemplate(template)"
                      class="p-1 text-zinc-400 hover:text-zinc-600 rounded"
                      title="Duplicate"
                    >
                      <span class="material-symbols-outlined text-lg">content_copy</span>
                    </button>
                    <button
                      @click.stop="openEditModal(template)"
                      class="p-1 text-zinc-400 hover:text-zinc-600 rounded"
                      title="Edit"
                    >
                      <span class="material-symbols-outlined text-lg">edit</span>
                    </button>
                    <button
                      @click.stop="deleteTemplate(template.id)"
                      class="p-1 text-zinc-400 hover:text-red-500 rounded"
                      title="Delete"
                    >
                      <span class="material-symbols-outlined text-lg">delete</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Template Builder -->
        <div class="lg:col-span-2">
          <div class="bg-white dark:bg-surface-dark rounded-lg shadow-sm border border-zinc-200 dark:border-border-dark">
            <div class="px-4 py-3 border-b border-zinc-200 dark:border-border-dark flex items-center justify-between">
              <h2 class="font-medium text-zinc-700 dark:text-zinc-200">
                {{ selectedTemplate ? `Template: ${selectedTemplate.name}` : 'Select a Template' }}
              </h2>
              <button
                v-if="selectedTemplate"
                @click="openAddNodeModal()"
                class="flex items-center gap-1 px-3 py-1.5 text-sm bg-teal/10 text-teal rounded hover:bg-teal/20 transition-colors"
              >
                <span class="material-symbols-outlined text-base">add</span>
                Add Folder
              </button>
            </div>

            <div v-if="!selectedTemplate" class="p-12 text-center text-zinc-500">
              <span class="material-symbols-outlined text-5xl mb-3 opacity-50">account_tree</span>
              <p class="font-medium">Select a template to view and edit its structure</p>
              <p class="text-sm mt-1">Or create a new template to get started</p>
            </div>

            <div v-else class="p-4">
              <!-- Template Info -->
              <div class="mb-4 p-3 bg-zinc-50 dark:bg-border-dark/50 rounded-lg text-sm">
                <div class="flex items-center gap-4 text-zinc-600 dark:text-zinc-300">
                  <span v-if="selectedTemplate.description">{{ selectedTemplate.description }}</span>
                  <span v-else class="italic text-zinc-400">No description</span>
                  <span class="ml-auto text-zinc-500">
                    {{ countNodes(selectedTemplate.nodes || []) }} folders
                  </span>
                </div>
              </div>

              <!-- Folder Tree -->
              <div v-if="selectedTemplate.nodes?.length" class="space-y-1">
                <template v-for="node in selectedTemplate.nodes" :key="node.id">
                  <FolderTreeNode
                    :node="node"
                    :depth="0"
                    :content-types="contentTypes"
                    @edit="openEditNodeModal"
                    @delete="deleteNode"
                    @add-child="openAddNodeModal"
                  />
                </template>
              </div>

              <div v-else class="py-8 text-center text-zinc-500">
                <span class="material-symbols-outlined text-3xl mb-2 opacity-50">folder_open</span>
                <p>No folders in this template</p>
                <p class="text-sm">Click "Add Folder" to start building the structure</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Template Modal -->
    <div
      v-if="showModal"
      class="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
      @click.self="showModal = false"
    >
      <div class="bg-white dark:bg-surface-dark rounded-lg shadow-xl w-full max-w-md p-6">
        <h3 class="text-lg font-semibold text-zinc-800 dark:text-zinc-100 mb-4">
          {{ isEditing ? 'Edit Template' : 'New Template' }}
        </h3>

        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Name *</label>
            <input
              v-model="formData.name"
              type="text"
              class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
              placeholder="e.g., Project Folder"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
            <textarea
              v-model="formData.description"
              rows="2"
              class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
              placeholder="What is this template for?"
            />
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Category</label>
              <input
                v-model="formData.category"
                type="text"
                list="categories"
                class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
                placeholder="e.g., Project"
              />
              <datalist id="categories">
                <option v-for="cat in categories" :key="cat" :value="cat" />
              </datalist>
            </div>

            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Icon</label>
              <input
                v-model="formData.icon"
                type="text"
                class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
                placeholder="folder_special"
              />
            </div>
          </div>

          <div class="flex items-center gap-4">
            <UiCheckbox v-model="formData.isDefault" label="Default template" />
            <UiCheckbox v-if="isEditing" v-model="formData.isActive" label="Active" />
          </div>
        </div>

        <div class="flex justify-end gap-3 mt-6">
          <button
            @click="showModal = false"
            class="px-4 py-2 text-zinc-600 hover:text-zinc-800 dark:text-zinc-400"
          >
            Cancel
          </button>
          <button
            @click="handleSave"
            :disabled="!formData.name"
            class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ isEditing ? 'Save Changes' : 'Create Template' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Create/Edit Node Modal -->
    <div
      v-if="showNodeModal"
      class="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
      @click.self="showNodeModal = false"
    >
      <div class="bg-white dark:bg-surface-dark rounded-lg shadow-xl w-full max-w-md p-6">
        <h3 class="text-lg font-semibold text-zinc-800 dark:text-zinc-100 mb-4">
          {{ editingNode ? 'Edit Folder' : 'Add Folder' }}
        </h3>

        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Folder Name *</label>
            <input
              v-model="nodeFormData.name"
              type="text"
              class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
              placeholder="e.g., Documents"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
            <input
              v-model="nodeFormData.description"
              type="text"
              class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
              placeholder="Optional description"
            />
          </div>

          <div>
            <UiSelect
              v-model="nodeFormData.contentTypeId"
              :options="contentTypeOptions"
              label="Content Type"
              placeholder="Select content type"
              searchable
              clearable
            />
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Sort Order</label>
              <input
                v-model.number="nodeFormData.sortOrder"
                type="number"
                class="w-full px-3 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal dark:bg-border-dark dark:text-white"
              />
            </div>
          </div>

          <UiCheckbox
            v-model="nodeFormData.breakContentTypeInheritance"
            label="Break content type inheritance"
          />
        </div>

        <div class="flex justify-end gap-3 mt-6">
          <button
            @click="showNodeModal = false"
            class="px-4 py-2 text-zinc-600 hover:text-zinc-800 dark:text-zinc-400"
          >
            Cancel
          </button>
          <button
            @click="handleSaveNode"
            :disabled="!nodeFormData.name"
            class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {{ editingNode ? 'Save Changes' : 'Add Folder' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

