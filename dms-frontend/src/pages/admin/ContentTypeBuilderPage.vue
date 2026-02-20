<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { contentTypeDefinitionsApi, referenceDataApi } from '@/api/client'
import type { ContentTypeDefinition, ContentTypeField, Classification } from '@/types'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

// Import modern UI components
import UiInput from '@/components/ui/Input.vue'
import UiSelect from '@/components/ui/Select.vue'
import UiToggle from '@/components/ui/Toggle.vue'
import UiCheckbox from '@/components/ui/Checkbox.vue'

const route = useRoute()
const router = useRouter()

const contentType = ref<ContentTypeDefinition | null>(null)
const fields = ref<ContentTypeField[]>([])
const classifications = ref<Classification[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showSettings = ref(true)
const showPreview = ref(false)

// Field editing
const showFieldModal = ref(false)
const isEditingField = ref(false)
const editingField = ref<Partial<ContentTypeField>>({
  id: '',
  contentTypeId: '',
  fieldName: '',
  displayName: '',
  fieldType: 'Text',
  isRequired: false,
  isReadOnly: false,
  showInList: false,
  isSearchable: true,
  sortOrder: 0,
  columnSpan: 12,
  isActive: true
})

// Options for dropdown fields
const fieldOptions = ref<{ value: string; label: string }[]>([])
const newOptionValue = ref('')
const newOptionLabel = ref('')

// Drag and drop state
const isDraggingFromPalette = ref(false)
const draggedPaletteType = ref<string | null>(null)
const isDraggingField = ref(false)
const draggedFieldIndex = ref(-1)
const dropTargetIndex = ref(-1)
const isOverCanvas = ref(false)

// Field types organized by category
const fieldCategories = [
  {
    name: 'Text',
    color: 'bg-blue-500',
    dotColor: 'bg-blue-400',
    fields: [
      { value: 'Text', label: 'Text', icon: 'title', color: '#3B82F6' },
      { value: 'TextArea', label: 'Text Area', icon: 'notes', color: '#3B82F6' },
    ]
  },
  {
    name: 'Numbers',
    color: 'bg-emerald-500',
    dotColor: 'bg-emerald-400',
    fields: [
      { value: 'Number', label: 'Number', icon: 'tag', color: '#10B981' },
      { value: 'Decimal', label: 'Decimal', icon: 'percent', color: '#10B981' },
    ]
  },
  {
    name: 'Date & Time',
    color: 'bg-amber-500',
    dotColor: 'bg-amber-400',
    fields: [
      { value: 'Date', label: 'Date', icon: 'calendar_today', color: '#F59E0B' },
      { value: 'DateTime', label: 'Date Time', icon: 'schedule', color: '#F59E0B' },
    ]
  },
  {
    name: 'Selection',
    color: 'bg-purple-500',
    dotColor: 'bg-purple-400',
    fields: [
      { value: 'Boolean', label: 'Yes/No', icon: 'toggle_on', color: '#8B5CF6' },
      { value: 'Dropdown', label: 'Dropdown', icon: 'arrow_drop_down_circle', color: '#8B5CF6' },
      { value: 'MultiSelect', label: 'Multi Select', icon: 'checklist', color: '#8B5CF6' },
    ]
  },
  {
    name: 'Reference',
    color: 'bg-rose-500',
    dotColor: 'bg-rose-400',
    fields: [
      { value: 'User', label: 'User', icon: 'person', color: '#F43F5E' },
      { value: 'Lookup', label: 'Lookup', icon: 'search', color: '#F43F5E' },
    ]
  }
]

const allFieldTypes = fieldCategories.flatMap(cat => cat.fields)

const contentTypeId = computed(() => route.params.id as string)
const isNewContentType = computed(() => contentTypeId.value === 'new')

function flattenClassifications(nodes: Classification[], prefix = ''): { value: string; label: string }[] {
  const result: { value: string; label: string }[] = []
  for (const node of nodes) {
    const label = prefix ? `${prefix} > ${node.name}` : node.name
    result.push({ value: node.id, label })
    if (node.children?.length) {
      result.push(...flattenClassifications(node.children, label))
    }
  }
  return result
}

const classificationOptions = computed(() => [
  { value: '', label: '-- None --' },
  ...flattenClassifications(classifications.value)
])

const categoryOptions = [
  { value: '', label: '-- Select Category --' },
  { value: 'General', label: 'General' },
  { value: 'Financial', label: 'Financial' },
  { value: 'Legal', label: 'Legal' },
  { value: 'HR', label: 'Human Resources' },
  { value: 'Technical', label: 'Technical' },
  { value: 'Administrative', label: 'Administrative' },
  { value: 'Compliance', label: 'Compliance' },
  { value: 'Project', label: 'Project' }
]

const colorPresets = [
  '#14B8A6', '#0D9488', '#0F766E', '#115E59',
  '#3B82F6', '#6366F1', '#8B5CF6', '#A855F7',
  '#EC4899', '#F43F5E', '#EF4444', '#F97316',
  '#F59E0B', '#84CC16', '#22C55E', '#64748B'
]

async function loadClassifications() {
  try {
    const response = await referenceDataApi.getClassificationTree()
    classifications.value = response.data.data ?? response.data ?? []
  } catch {}
}

onMounted(async () => {
  await loadClassifications()
  if (!isNewContentType.value) {
    await loadContentType()
  } else {
    contentType.value = {
      id: '',
      name: '',
      description: '',
      icon: 'description',
      color: '#14B8A6',
      category: '',
      allowOnFolders: true,
      allowOnDocuments: true,
      isRequired: false,
      isSystemDefault: false,
      defaultClassificationId: undefined,
      isActive: true,
      sortOrder: 0,
      createdAt: new Date().toISOString(),
      fields: []
    }
  }
})

async function loadContentType() {
  isLoading.value = true
  try {
    const response = await contentTypeDefinitionsApi.getById(contentTypeId.value)
    contentType.value = response.data
    fields.value = response.data.fields || []
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

async function saveContentType() {
  if (!contentType.value?.name) return
  isSaving.value = true

  try {
    if (isNewContentType.value) {
      const response = await contentTypeDefinitionsApi.create({
        name: contentType.value.name,
        description: contentType.value.description,
        icon: contentType.value.icon,
        color: contentType.value.color,
        category: contentType.value.category,
        allowOnFolders: contentType.value.allowOnFolders,
        allowOnDocuments: contentType.value.allowOnDocuments,
        isRequired: contentType.value.isRequired,
        defaultClassificationId: contentType.value.defaultClassificationId || undefined,
        sortOrder: contentType.value.sortOrder
      })
      router.replace(`/admin/content-type-builder/${response.data}`)
    } else {
      await contentTypeDefinitionsApi.update(contentTypeId.value, {
        name: contentType.value.name,
        description: contentType.value.description,
        icon: contentType.value.icon,
        color: contentType.value.color,
        category: contentType.value.category,
        allowOnFolders: contentType.value.allowOnFolders,
        allowOnDocuments: contentType.value.allowOnDocuments,
        isRequired: contentType.value.isRequired,
        isSystemDefault: contentType.value.isSystemDefault,
        defaultClassificationId: contentType.value.defaultClassificationId || undefined,
        isActive: contentType.value.isActive,
        sortOrder: contentType.value.sortOrder
      })
    }
  } catch (err) {
  } finally {
    isSaving.value = false
  }
}

function onPaletteDragStart(e: DragEvent, fieldType: string) {
  if (isNewContentType.value) return
  isDraggingFromPalette.value = true
  draggedPaletteType.value = fieldType
  if (e.dataTransfer) {
    e.dataTransfer.effectAllowed = 'copy'
    e.dataTransfer.setData('text/plain', fieldType)
  }
}

function onPaletteDragEnd() {
  isDraggingFromPalette.value = false
  draggedPaletteType.value = null
  isOverCanvas.value = false
  dropTargetIndex.value = -1
}

function onFieldDragStart(e: DragEvent, index: number) {
  isDraggingField.value = true
  draggedFieldIndex.value = index
  if (e.dataTransfer) {
    e.dataTransfer.effectAllowed = 'move'
  }
}

function onFieldDragEnd() {
  isDraggingField.value = false
  draggedFieldIndex.value = -1
  dropTargetIndex.value = -1
}

function onCanvasDragOver(e: DragEvent) {
  e.preventDefault()
  isOverCanvas.value = true
  if (e.dataTransfer) {
    e.dataTransfer.dropEffect = isDraggingFromPalette.value ? 'copy' : 'move'
  }
}

function onCanvasDragLeave(e: DragEvent) {
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
  if (e.clientX < rect.left || e.clientX > rect.right || e.clientY < rect.top || e.clientY > rect.bottom) {
    isOverCanvas.value = false
    dropTargetIndex.value = -1
  }
}

function onFieldDragOver(e: DragEvent, index: number) {
  e.preventDefault()
  e.stopPropagation()
  dropTargetIndex.value = index
}

async function onCanvasDrop(e: DragEvent) {
  e.preventDefault()
  isOverCanvas.value = false
  if (isDraggingFromPalette.value && draggedPaletteType.value) {
    openNewFieldModal(draggedPaletteType.value, dropTargetIndex.value >= 0 ? dropTargetIndex.value + 1 : fields.value.length + 1)
  }
  onPaletteDragEnd()
}

async function onFieldDrop(e: DragEvent, targetIndex: number) {
  e.preventDefault()
  e.stopPropagation()

  if (isDraggingField.value && draggedFieldIndex.value !== -1 && draggedFieldIndex.value !== targetIndex) {
    const items = [...fields.value]
    const [removed] = items.splice(draggedFieldIndex.value, 1)
    items.splice(targetIndex, 0, removed)
    fields.value = items
    try {
      await contentTypeDefinitionsApi.reorderFields(contentTypeId.value, items.map(f => f.id))
    } catch (err) {
    }
  }

  if (isDraggingFromPalette.value && draggedPaletteType.value) {
    openNewFieldModal(draggedPaletteType.value, targetIndex + 1)
  }

  onFieldDragEnd()
  onPaletteDragEnd()
}

function openNewFieldModal(fieldType: string, sortOrder: number) {
  editingField.value = {
    id: '',
    contentTypeId: contentTypeId.value,
    fieldName: '',
    displayName: '',
    fieldType: fieldType,
    isRequired: false,
    isReadOnly: false,
    showInList: false,
    isSearchable: true,
    sortOrder: sortOrder,
    columnSpan: 12,
    isActive: true
  }
  fieldOptions.value = []
  isEditingField.value = false
  showFieldModal.value = true
}

function openEditFieldModal(field: ContentTypeField) {
  editingField.value = { ...field }
  if (field.options) {
    try {
      fieldOptions.value = JSON.parse(field.options)
    } catch {
      fieldOptions.value = []
    }
  } else {
    fieldOptions.value = []
  }
  isEditingField.value = true
  showFieldModal.value = true
}

async function saveField() {
  if (!editingField.value.displayName) return

  if (!editingField.value.fieldName) {
    editingField.value.fieldName = editingField.value.displayName
      .replace(/[^a-zA-Z0-9]/g, '')
      .replace(/^[0-9]/, '_$&')
  }

  if (['Dropdown', 'MultiSelect'].includes(editingField.value.fieldType || '') && fieldOptions.value.length > 0) {
    editingField.value.options = JSON.stringify(fieldOptions.value)
  }

  try {
    const fieldData = {
      fieldName: editingField.value.fieldName || '',
      displayName: editingField.value.displayName || '',
      description: editingField.value.description,
      fieldType: editingField.value.fieldType || 'Text',
      isRequired: editingField.value.isRequired || false,
      isReadOnly: editingField.value.isReadOnly || false,
      showInList: editingField.value.showInList || false,
      isSearchable: editingField.value.isSearchable ?? true,
      defaultValue: editingField.value.defaultValue,
      validationRules: editingField.value.validationRules,
      lookupName: editingField.value.lookupName,
      options: editingField.value.options,
      groupName: editingField.value.groupName,
      columnSpan: editingField.value.columnSpan || 12
    }

    if (isEditingField.value && editingField.value.id) {
      await contentTypeDefinitionsApi.updateField(editingField.value.id, {
        ...fieldData,
        sortOrder: editingField.value.sortOrder || 0,
        isActive: editingField.value.isActive ?? true
      })
    } else {
      await contentTypeDefinitionsApi.createField(contentTypeId.value, fieldData)
    }

    showFieldModal.value = false
    await loadContentType()
  } catch (err) {
  }
}

async function deleteField(fieldId: string) {
  if (!confirm('Delete this field?')) return
  try {
    await contentTypeDefinitionsApi.deleteField(fieldId)
    await loadContentType()
  } catch (err) {
  }
}

function duplicateField(field: ContentTypeField) {
  openNewFieldModal(field.fieldType, fields.value.length + 1)
  editingField.value = {
    ...editingField.value,
    displayName: `${field.displayName} (Copy)`,
    fieldName: '',
    description: field.description,
    isRequired: field.isRequired,
    isReadOnly: field.isReadOnly,
    showInList: field.showInList,
    isSearchable: field.isSearchable,
    columnSpan: field.columnSpan,
    lookupName: field.lookupName,
    options: field.options
  }
  if (field.options) {
    try {
      fieldOptions.value = JSON.parse(field.options)
    } catch {
      fieldOptions.value = []
    }
  }
}

function addOption() {
  if (!newOptionValue.value || !newOptionLabel.value) return
  fieldOptions.value.push({ value: newOptionValue.value, label: newOptionLabel.value })
  newOptionValue.value = ''
  newOptionLabel.value = ''
}

function removeOption(index: number) {
  fieldOptions.value.splice(index, 1)
}

function getFieldTypeInfo(type: string) {
  return allFieldTypes.find(ft => ft.value === type)
}

function getFieldOptions(field: ContentTypeField): { value: string; label: string }[] {
  if (!field.options) return []
  try {
    return JSON.parse(field.options)
  } catch {
    return []
  }
}

function getCategoryColor(fieldType: string): string {
  for (const cat of fieldCategories) {
    if (cat.fields.some(f => f.value === fieldType)) {
      return cat.color
    }
  }
  return 'bg-gray-500'
}

function getFieldColor(fieldType: string): string {
  for (const cat of fieldCategories) {
    const field = cat.fields.find(f => f.value === fieldType)
    if (field) return field.color
  }
  return '#6B7280'
}
</script>

<template>
  <div class="h-[calc(100vh-7rem)] p-4 bg-gray-100 dark:bg-zinc-950">
    <!-- Breadcrumb -->
    <div class="max-w-[1400px] mx-auto mb-3">
      <AdminBreadcrumb current-page="Content Type Builder" icon="construction" />
    </div>

    <!-- Main Card Container -->
    <div class="h-full bg-white dark:bg-background-dark rounded-lg shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden flex flex-col">
      <!-- Header -->
      <div class="px-5 py-3 flex items-center justify-between border-b border-gray-100 dark:border-border-dark">
      <div class="flex items-center gap-4">
        <button @click="router.push('/admin/content-type-builder')" class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-surface-dark rounded-lg transition-colors">
          <span class="material-symbols-outlined text-xl">arrow_back</span>
        </button>
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-lg flex items-center justify-center" :style="{ backgroundColor: contentType?.color || '#14B8A6' }">
            <span class="material-symbols-outlined text-white text-lg">description</span>
          </div>
          <div>
            <h1 class="text-base font-semibold text-gray-900 dark:text-white leading-tight">
              {{ isNewContentType ? 'New Content Type' : contentType?.name || 'Content Type' }}
            </h1>
            <p class="text-xs text-gray-500">{{ fields.length }} field{{ fields.length !== 1 ? 's' : '' }} defined</p>
          </div>
        </div>
      </div>
      <div class="flex items-center gap-2">
        <button
          @click="showPreview = true"
          :disabled="fields.length === 0"
          class="flex items-center gap-1.5 px-3 py-2 text-sm font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-surface-dark rounded-lg transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
        >
          <span class="material-symbols-outlined text-lg">visibility</span>
          Preview
        </button>
        <div class="w-px h-6 bg-gray-200 dark:bg-border-dark mx-1"></div>
        <button
          @click="showSettings = !showSettings"
          :class="['flex items-center gap-1.5 px-3 py-2 text-sm font-medium rounded-lg transition-colors', showSettings ? 'bg-gray-100 dark:bg-surface-dark text-gray-700 dark:text-gray-200' : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-surface-dark']"
        >
          <span class="material-symbols-outlined text-lg">tune</span>
          Settings
        </button>
        <div class="w-px h-6 bg-gray-200 dark:bg-border-dark mx-1"></div>
        <button
          @click="saveContentType"
          :disabled="isSaving || !contentType?.name"
          class="flex items-center gap-1.5 px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 disabled:opacity-50 transition-colors"
        >
          <span class="material-symbols-outlined text-lg">save</span>
          Save
        </button>
      </div>
    </div>

      <div v-if="isLoading" class="flex-1 flex items-center justify-center">
        <span class="material-symbols-outlined animate-spin text-4xl text-teal">progress_activity</span>
      </div>

      <div v-else-if="contentType" class="flex-1 flex overflow-hidden">
        <!-- Left: Field Palette -->
      <div class="w-56 bg-gray-50/50 dark:bg-surface-dark/50 border-r border-gray-100 dark:border-border-dark flex flex-col">
        <!-- Enhanced Header -->
        <div class="px-3 py-2.5 border-b border-gray-100 dark:border-border-dark">
          <div class="flex items-center gap-2">
            <div class="w-6 h-6 rounded-md bg-gradient-to-br from-teal to-teal/80 flex items-center justify-center">
              <span class="material-symbols-outlined text-white text-sm">dashboard_customize</span>
            </div>
            <div>
              <h3 class="text-xs font-semibold text-gray-700 dark:text-gray-200">Field Types</h3>
              <p class="text-[9px] text-gray-400">Drag to canvas</p>
            </div>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto p-2">
          <div v-for="(category, catIndex) in fieldCategories" :key="category.name" :class="catIndex > 0 ? 'mt-2.5' : ''">
            <div class="flex items-center gap-1.5 mb-1 px-1">
              <div :class="['w-1.5 h-1.5 rounded-full', category.color]"></div>
              <span class="text-[10px] font-semibold text-gray-400 uppercase tracking-wide">{{ category.name }}</span>
            </div>
            <div class="space-y-0.5">
              <div
                v-for="ft in category.fields"
                :key="ft.value"
                draggable="true"
                @dragstart="onPaletteDragStart($event, ft.value)"
                @dragend="onPaletteDragEnd"
                :class="[
                  'group flex items-center gap-2 px-2 py-1.5 rounded-md cursor-grab transition-all duration-150',
                  isNewContentType ? 'opacity-40 cursor-not-allowed' : '',
                  draggedPaletteType === ft.value
                    ? 'bg-teal/10 ring-1 ring-teal/30'
                    : 'hover:bg-gray-50 dark:hover:bg-surface-dark'
                ]"
              >
                <div
                  class="w-6 h-6 rounded flex items-center justify-center flex-shrink-0"
                  :style="{ backgroundColor: ft.color + '12' }"
                >
                  <span
                    class="material-symbols-outlined text-base"
                    :style="{ color: ft.color }"
                  >{{ ft.icon }}</span>
                </div>
                <span class="text-xs font-medium text-gray-600 dark:text-gray-300">{{ ft.label }}</span>
                <span class="material-symbols-outlined text-gray-300 dark:text-zinc-600 text-sm ml-auto opacity-0 group-hover:opacity-100 transition-opacity">drag_indicator</span>
              </div>
            </div>
          </div>
        </div>

        <div v-if="isNewContentType" class="p-2 bg-amber-50/80 dark:bg-amber-900/20 border-t border-amber-100 dark:border-amber-800">
          <div class="flex items-center gap-1.5 justify-center">
            <span class="material-symbols-outlined text-amber-500 text-sm">info</span>
            <p class="text-[10px] text-amber-600 dark:text-amber-400 font-medium">Save first to add fields</p>
          </div>
        </div>
      </div>

      <!-- Center: Settings + Canvas -->
      <div class="flex-1 flex flex-col overflow-hidden">
        <!-- Settings Panel -->
        <div v-if="showSettings" class="border-b border-gray-100 dark:border-border-dark">
          <div class="px-5 py-4">
            <div class="flex items-start gap-8">
              <!-- Name -->
              <div class="w-64">
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5 uppercase tracking-wide">Name</label>
                <input
                  v-model="contentType.name"
                  type="text"
                  class="w-full px-3.5 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                  placeholder="Content type name"
                />
              </div>

              <!-- Category -->
              <div class="w-48">
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5 uppercase tracking-wide">Category</label>
                <UiSelect
                  v-model="contentType.category"
                  :options="categoryOptions"
                  placeholder="Select..."
                  size="md"
                />
              </div>

              <!-- Description -->
              <div class="flex-1 min-w-0">
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5 uppercase tracking-wide">Description</label>
                <input
                  v-model="contentType.description"
                  type="text"
                  class="w-full px-3.5 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                  placeholder="Brief description of this content type"
                />
              </div>

              <!-- Divider -->
              <div class="w-px h-16 bg-gray-200 dark:bg-border-dark self-center"></div>

              <!-- Color -->
              <div>
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5 uppercase tracking-wide">Color</label>
                <div class="flex items-center gap-3">
                  <div
                    class="w-10 h-10 rounded-lg shadow-inner border-2 border-white dark:border-border-dark ring-1 ring-gray-200 dark:ring-zinc-600"
                    :style="{ backgroundColor: contentType.color }"
                  ></div>
                  <div class="grid grid-cols-8 gap-1">
                    <button
                      v-for="color in colorPresets"
                      :key="color"
                      @click="contentType.color = color"
                      class="w-4 h-4 rounded transition-all hover:scale-125"
                      :class="contentType.color === color ? 'ring-2 ring-offset-1 ring-gray-400 scale-110' : ''"
                      :style="{ backgroundColor: color }"
                    ></button>
                  </div>
                </div>
              </div>

              <!-- Divider -->
              <div class="w-px h-16 bg-gray-200 dark:bg-border-dark self-center"></div>

              <!-- Apply To -->
              <div>
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5 uppercase tracking-wide">Apply To</label>
                <div class="flex items-center gap-4 h-10">
                  <label class="flex items-center gap-2.5 cursor-pointer group">
                    <button
                      type="button"
                      @click="contentType.allowOnFolders = !contentType.allowOnFolders"
                      class="relative w-9 h-5 rounded-full transition-colors duration-200"
                      :class="contentType.allowOnFolders ? 'bg-teal' : 'bg-gray-300 dark:bg-zinc-600'"
                    >
                      <span
                        class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform duration-200"
                        :class="contentType.allowOnFolders ? 'translate-x-4' : 'translate-x-0'"
                      ></span>
                    </button>
                    <span class="text-sm text-gray-600 dark:text-gray-300 group-hover:text-gray-900 dark:group-hover:text-white transition-colors">Folders</span>
                  </label>
                  <label class="flex items-center gap-2.5 cursor-pointer group">
                    <button
                      type="button"
                      @click="contentType.allowOnDocuments = !contentType.allowOnDocuments"
                      class="relative w-9 h-5 rounded-full transition-colors duration-200"
                      :class="contentType.allowOnDocuments ? 'bg-teal' : 'bg-gray-300 dark:bg-zinc-600'"
                    >
                      <span
                        class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform duration-200"
                        :class="contentType.allowOnDocuments ? 'translate-x-4' : 'translate-x-0'"
                      ></span>
                    </button>
                    <span class="text-sm text-gray-600 dark:text-gray-300 group-hover:text-gray-900 dark:group-hover:text-white transition-colors">Documents</span>
                  </label>
                </div>
              </div>

              <!-- Divider -->
              <div class="w-px h-16 bg-gray-200 dark:bg-border-dark self-center"></div>

              <!-- Default Classification -->
              <div class="w-52">
                <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1.5 uppercase tracking-wide">Default Classification</label>
                <UiSelect
                  v-model="contentType.defaultClassificationId"
                  :options="classificationOptions"
                  placeholder="None"
                  size="md"
                />
              </div>
            </div>
          </div>
        </div>

        <!-- Form Canvas -->
        <div
          class="flex-1 overflow-y-auto p-4 bg-gray-50/50 dark:bg-surface-dark/30"
          @dragover="onCanvasDragOver"
          @dragleave="onCanvasDragLeave"
          @drop="onCanvasDrop"
        >
          <div
            :class="[
              'min-h-full rounded-lg border-2 border-dashed transition-all',
              isOverCanvas && isDraggingFromPalette ? 'border-teal bg-teal/5' : 'border-gray-200 dark:border-border-dark bg-white dark:bg-background-dark/50',
              fields.length === 0 ? 'flex items-center justify-center' : 'p-4'
            ]"
          >
            <!-- Empty State -->
            <div v-if="fields.length === 0 && !isOverCanvas" class="text-center py-16">
              <div class="w-20 h-20 mx-auto mb-4 rounded-lg bg-gray-100 dark:bg-surface-dark flex items-center justify-center">
                <span class="material-symbols-outlined text-4xl text-gray-300">drag_indicator</span>
              </div>
              <h3 class="text-lg font-medium text-gray-600 dark:text-gray-400 mb-2">No fields yet</h3>
              <p class="text-sm text-gray-400">Drag fields from the left panel to start building your form</p>
            </div>

            <div v-else-if="fields.length === 0 && isOverCanvas" class="text-center py-16">
              <div class="w-20 h-20 mx-auto mb-4 rounded-lg bg-teal/10 flex items-center justify-center">
                <span class="material-symbols-outlined text-4xl text-teal">add_circle</span>
              </div>
              <h3 class="text-lg font-medium text-teal mb-2">Drop to add field</h3>
              <p class="text-sm text-teal/70">Release to configure the field</p>
            </div>

            <!-- Fields List -->
            <div v-else class="space-y-3">
              <div
                v-for="(field, index) in fields"
                :key="field.id"
                draggable="true"
                @dragstart="onFieldDragStart($event, index)"
                @dragend="onFieldDragEnd"
                @dragover="onFieldDragOver($event, index)"
                @drop="onFieldDrop($event, index)"
                :class="[
                  'group flex items-center gap-4 px-4 py-3 rounded-lg border transition-all cursor-grab',
                  'bg-white dark:bg-surface-dark hover:shadow-md',
                  draggedFieldIndex === index ? 'opacity-50 border-teal shadow-lg' : 'border-gray-100 dark:border-border-dark',
                  dropTargetIndex === index && draggedFieldIndex !== index ? 'border-t-4 border-t-teal mt-4' : ''
                ]"
              >
                <span class="material-symbols-outlined text-gray-300 text-lg cursor-grab">drag_indicator</span>

                <div
                  class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 shadow-sm"
                  :style="{ backgroundColor: getFieldColor(field.fieldType) }"
                >
                  <span class="material-symbols-outlined text-white text-lg">{{ getFieldTypeInfo(field.fieldType)?.icon }}</span>
                </div>

                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="text-sm font-semibold text-gray-900 dark:text-white">{{ field.displayName }}</span>
                    <span v-if="field.isRequired" class="px-1.5 py-0.5 text-[10px] font-bold text-red-600 bg-red-50 rounded">Required</span>
                    <span v-if="field.showInList" class="px-1.5 py-0.5 text-[10px] font-bold text-blue-600 bg-blue-50 rounded">Listed</span>
                  </div>
                  <div class="flex items-center gap-2 mt-0.5">
                    <span class="text-xs font-medium text-teal">{{ getFieldTypeInfo(field.fieldType)?.label }}</span>
                    <span class="text-xs text-gray-400">{{ field.fieldName }}</span>
                  </div>
                </div>

                <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  <button @click="duplicateField(field)" class="p-2 text-gray-400 hover:text-blue-500 hover:bg-blue-50 rounded-lg transition-colors" title="Duplicate">
                    <span class="material-symbols-outlined text-lg">content_copy</span>
                  </button>
                  <button @click="openEditFieldModal(field)" class="p-2 text-gray-400 hover:text-teal hover:bg-teal/10 rounded-lg transition-colors" title="Edit">
                    <span class="material-symbols-outlined text-lg">edit</span>
                  </button>
                  <button @click="deleteField(field.id)" class="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors" title="Delete">
                    <span class="material-symbols-outlined text-lg">delete</span>
                  </button>
                </div>
              </div>

              <div
                v-if="isDraggingFromPalette || isDraggingField"
                @dragover.prevent="dropTargetIndex = fields.length"
                @drop="onFieldDrop($event, fields.length)"
                :class="[
                  'h-16 rounded-lg border-2 border-dashed flex items-center justify-center text-sm font-medium transition-colors',
                  dropTargetIndex === fields.length ? 'border-teal bg-teal/5 text-teal' : 'border-gray-200 text-gray-400'
                ]"
              >
                <span class="material-symbols-outlined mr-2">add</span>
                Drop field here
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
    </div>

    <!-- Field Modal -->
    <Teleport to="body">
      <div v-if="showFieldModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" @click.self="showFieldModal = false">
        <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-lg max-h-[85vh] overflow-hidden flex flex-col">
          <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-border-dark">
            <div class="flex items-center gap-3">
              <div
                class="w-10 h-10 rounded-lg flex items-center justify-center shadow-sm"
                :style="{ backgroundColor: getFieldColor(editingField.fieldType || 'Text') }"
              >
                <span class="material-symbols-outlined text-white text-lg">{{ getFieldTypeInfo(editingField.fieldType || 'Text')?.icon }}</span>
              </div>
              <div>
                <h3 class="text-base font-semibold text-gray-900 dark:text-white">{{ isEditingField ? 'Edit Field' : 'Add Field' }}</h3>
                <p class="text-xs text-gray-500">{{ getFieldTypeInfo(editingField.fieldType || 'Text')?.label }} field</p>
              </div>
            </div>
            <button @click="showFieldModal = false" class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors">
              <span class="material-symbols-outlined text-xl">close</span>
            </button>
          </div>

          <div class="flex-1 overflow-y-auto p-6 space-y-5">
            <!-- Type selector -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Field Type</label>
              <div class="flex flex-wrap gap-1.5">
                <button
                  v-for="ft in allFieldTypes"
                  :key="ft.value"
                  @click="editingField.fieldType = ft.value"
                  class="flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg text-xs font-medium transition-all duration-200"
                  :style="{
                    backgroundColor: editingField.fieldType === ft.value ? ft.color : '',
                    color: editingField.fieldType === ft.value ? 'white' : '#6B7280',
                    border: editingField.fieldType === ft.value ? 'none' : '1px solid #E5E7EB'
                  }"
                >
                  <span class="material-symbols-outlined text-sm">{{ ft.icon }}</span>
                  {{ ft.label }}
                </button>
              </div>
            </div>

            <!-- Name fields -->
            <div class="grid grid-cols-2 gap-4">
              <UiInput
                v-model="editingField.displayName"
                label="Display Name"
                placeholder="Field label"
              />
              <UiInput
                v-model="editingField.fieldName"
                label="Field Name"
                placeholder="Auto-generated"
                hint="Used in API"
              />
            </div>

            <UiInput
              v-model="editingField.description"
              label="Help Text"
              placeholder="Optional description shown below the field"
            />

            <!-- Dropdown options -->
            <div v-if="['Dropdown', 'MultiSelect'].includes(editingField.fieldType || '')" class="bg-gray-50 dark:bg-surface-dark rounded-lg p-4">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Options</label>
              <div class="space-y-2 max-h-32 overflow-y-auto mb-3">
                <div v-for="(opt, idx) in fieldOptions" :key="idx" class="flex items-center gap-3 text-sm bg-white dark:bg-border-dark px-3 py-2 rounded-lg">
                  <span class="flex-1 font-medium">{{ opt.label }}</span>
                  <code class="text-xs text-gray-400 bg-gray-100 dark:bg-zinc-600 px-2 py-0.5 rounded">{{ opt.value }}</code>
                  <button @click="removeOption(idx)" class="text-gray-400 hover:text-red-500 transition-colors">
                    <span class="material-symbols-outlined text-lg">close</span>
                  </button>
                </div>
                <div v-if="fieldOptions.length === 0" class="text-center py-3 text-sm text-gray-400">
                  No options added yet
                </div>
              </div>
              <div class="flex gap-2">
                <UiInput v-model="newOptionValue" placeholder="Value" size="sm" class="flex-1" @keyup.enter="addOption" />
                <UiInput v-model="newOptionLabel" placeholder="Label" size="sm" class="flex-1" @keyup.enter="addOption" />
                <button @click="addOption" class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors">
                  Add
                </button>
              </div>
            </div>

            <!-- Lookup -->
            <div v-if="editingField.fieldType === 'Lookup'" class="bg-teal/5 border border-teal/20 rounded-lg p-4">
              <UiInput
                v-model="editingField.lookupName"
                label="Lookup Name"
                placeholder="e.g., DocumentStatus, Priority"
                hint="Reference data lookup name"
              />
            </div>

            <!-- Width -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Column Width</label>
              <div class="flex gap-2">
                <button
                  v-for="w in [{ v: 12, l: 'Full Width' }, { v: 6, l: 'Half' }, { v: 4, l: 'Third' }, { v: 3, l: 'Quarter' }]"
                  :key="w.v"
                  @click="editingField.columnSpan = w.v"
                  :class="['flex-1 py-2.5 text-sm font-medium rounded-lg border-2 transition-colors', editingField.columnSpan === w.v ? 'bg-teal text-white border-teal' : 'border-gray-200 hover:border-gray-300 text-gray-600']"
                >
                  {{ w.l }}
                </button>
              </div>
            </div>

            <!-- Flags -->
            <div class="grid grid-cols-2 gap-4">
              <div class="bg-gray-50 dark:bg-surface-dark rounded-lg p-4">
                <UiCheckbox v-model="editingField.isRequired" label="Required" description="Field must have a value" />
              </div>
              <div class="bg-gray-50 dark:bg-surface-dark rounded-lg p-4">
                <UiCheckbox v-model="editingField.isReadOnly" label="Read Only" description="Cannot be edited" />
              </div>
              <div class="bg-gray-50 dark:bg-surface-dark rounded-lg p-4">
                <UiCheckbox v-model="editingField.showInList" label="Show in List" description="Visible in table view" />
              </div>
              <div class="bg-gray-50 dark:bg-surface-dark rounded-lg p-4">
                <UiCheckbox v-model="editingField.isSearchable" label="Searchable" description="Include in search" />
              </div>
            </div>
          </div>

          <div class="flex justify-end gap-3 px-6 py-4 border-t border-gray-100 dark:border-border-dark bg-gray-50 dark:bg-surface-dark/50">
            <button @click="showFieldModal = false" class="px-5 py-2.5 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-100 transition-colors">
              Cancel
            </button>
            <button @click="saveField" :disabled="!editingField.displayName" class="px-5 py-2.5 text-sm font-medium bg-teal text-white rounded-lg hover:bg-teal/90 disabled:opacity-50 transition-colors">
              {{ isEditingField ? 'Update Field' : 'Add Field' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Preview Modal -->
    <Teleport to="body">
      <div v-if="showPreview" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" @click.self="showPreview = false">
        <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-2xl max-h-[85vh] overflow-hidden flex flex-col">
          <div class="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-border-dark">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 rounded-lg flex items-center justify-center" :style="{ backgroundColor: contentType?.color }">
                <span class="material-symbols-outlined text-white text-lg">description</span>
              </div>
              <div>
                <h3 class="text-base font-semibold text-gray-900 dark:text-white">{{ contentType?.name || 'Form Preview' }}</h3>
                <p class="text-xs text-gray-500">{{ fields.length }} fields</p>
              </div>
            </div>
            <button @click="showPreview = false" class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition-colors">
              <span class="material-symbols-outlined text-xl">close</span>
            </button>
          </div>

          <div class="flex-1 overflow-y-auto p-6">
            <div class="grid grid-cols-12 gap-4">
              <div
                v-for="field in fields"
                :key="field.id"
                :class="[
                  field.columnSpan === 12 ? 'col-span-12' :
                  field.columnSpan === 6 ? 'col-span-6' :
                  field.columnSpan === 4 ? 'col-span-4' : 'col-span-3'
                ]"
              >
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                  {{ field.displayName }}
                  <span v-if="field.isRequired" class="text-red-500 ml-0.5">*</span>
                </label>

                <!-- Text -->
                <input
                  v-if="field.fieldType === 'Text'"
                  type="text"
                  class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-colors"
                  :placeholder="field.description || field.displayName"
                  :readonly="field.isReadOnly"
                />

                <!-- TextArea -->
                <textarea
                  v-else-if="field.fieldType === 'TextArea'"
                  rows="3"
                  class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-colors resize-none"
                  :placeholder="field.description || field.displayName"
                  :readonly="field.isReadOnly"
                ></textarea>

                <!-- Number/Decimal -->
                <input
                  v-else-if="['Number', 'Decimal'].includes(field.fieldType)"
                  type="number"
                  :step="field.fieldType === 'Decimal' ? '0.01' : '1'"
                  class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-colors"
                  :placeholder="field.description || field.displayName"
                  :readonly="field.isReadOnly"
                />

                <!-- Date/DateTime -->
                <input
                  v-else-if="['Date', 'DateTime'].includes(field.fieldType)"
                  :type="field.fieldType === 'Date' ? 'date' : 'datetime-local'"
                  class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal transition-colors"
                  :readonly="field.isReadOnly"
                />

                <!-- Boolean -->
                <div v-else-if="field.fieldType === 'Boolean'" class="py-2">
                  <UiToggle :model-value="false" :label="field.description || 'Yes'" :disabled="field.isReadOnly" />
                </div>

                <!-- Dropdown -->
                <UiSelect
                  v-else-if="field.fieldType === 'Dropdown'"
                  :options="getFieldOptions(field)"
                  :placeholder="'Select ' + field.displayName.toLowerCase()"
                  :disabled="field.isReadOnly"
                />

                <!-- MultiSelect - simplified for preview -->
                <select
                  v-else-if="field.fieldType === 'MultiSelect'"
                  multiple
                  class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark focus:ring-2 focus:ring-teal/50 focus:border-teal h-24"
                  :disabled="field.isReadOnly"
                >
                  <option v-for="opt in getFieldOptions(field)" :key="opt.value" :value="opt.value">{{ opt.label }}</option>
                </select>

                <!-- User/Lookup -->
                <UiSelect
                  v-else-if="['User', 'Lookup'].includes(field.fieldType)"
                  :options="[]"
                  :placeholder="'Select ' + (field.fieldType === 'User' ? 'user' : 'value')"
                  :disabled="field.isReadOnly"
                />

                <!-- Default -->
                <input
                  v-else
                  type="text"
                  class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark"
                  :placeholder="field.displayName"
                />

                <p v-if="field.description && field.fieldType !== 'Boolean'" class="text-xs text-gray-400 mt-1">{{ field.description }}</p>
              </div>
            </div>
          </div>

          <div class="flex justify-end gap-3 px-6 py-4 border-t border-gray-100 dark:border-border-dark bg-gray-50 dark:bg-surface-dark/50">
            <button @click="showPreview = false" class="px-5 py-2.5 text-sm font-medium bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors">
              Close Preview
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
