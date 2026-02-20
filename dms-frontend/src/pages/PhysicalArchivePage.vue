<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { physicalLocationsApi, physicalItemsApi } from '@/api/client'
import type { PhysicalLocation, PhysicalItem } from '@/types'
import SpatialView from '@/components/archive/SpatialView.vue'

const router = useRouter()

// ── Data ──
const locations = ref<PhysicalLocation[]>([])
const items = ref<PhysicalItem[]>([])
const selectedLocationId = ref<string | null>(null)
const loadingLocations = ref(false)
const loadingItems = ref(false)
const saving = ref(false)

// ── New: Search, View Mode, Tree State ──
const searchQuery = ref('')
const viewMode = ref<'table' | 'grid' | 'spatial'>('table')
const expandedIds = reactive(new Set<string>())

// ── Location modal ──
const showLocationModal = ref(false)
const isEditingLocation = ref(false)
const locationForm = ref({
  id: '',
  name: '',
  code: '',
  locationType: 'Room' as string,
  parentId: '' as string,
  capacity: null as number | null,
  description: ''
})

// ── Item modal ──
const showItemModal = ref(false)
const isEditingItem = ref(false)
const itemForm = ref({
  id: '',
  title: '',
  barcode: '',
  itemType: 'Document' as string,
  description: '',
  locationId: '' as string,
  condition: 'Good' as string
})

// ── Move modal ──
const showMoveModal = ref(false)
const moveTarget = ref<PhysicalItem | null>(null)
const moveLocationId = ref('')

// ── Condition modal ──
const showConditionModal = ref(false)
const conditionTarget = ref<PhysicalItem | null>(null)
const conditionValue = ref('Good')
const conditionNotes = ref('')

// ── Error banner ──
const errorMessage = ref('')
function showError(msg: string) {
  errorMessage.value = msg
  setTimeout(() => { errorMessage.value = '' }, 6000)
}
function extractError(err: any): string {
  const data = err?.response?.data
  if (Array.isArray(data) && data.length > 0) return data.join('; ')
  if (typeof data === 'string' && data) return data
  return data?.message || data?.error || data?.title || err?.message || 'An unexpected error occurred'
}

// ── Delete confirm ──
const showDeleteConfirm = ref(false)
const deleteType = ref<'location' | 'item'>('location')
const deleteTargetId = ref('')
const deleteTargetName = ref('')

// ── Constants ──
const locationTypes = ['Site', 'Building', 'Floor', 'Room', 'Rack', 'Shelf', 'Box', 'File']
const itemTypes = ['Document', 'File', 'Box', 'Volume', 'Map', 'Photograph', 'AudioTape', 'VideoTape', 'Microfilm', 'DigitalMedia']
const conditions = ['Good', 'Fair', 'Poor', 'Damaged', 'Destroyed']

const locationTypeColors: Record<string, string> = {
  Site: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  Building: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
  Floor: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-400',
  Room: 'bg-teal-100 text-teal-700 dark:bg-teal-900/30 dark:text-teal-400',
  Rack: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  Shelf: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  Box: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400',
  File: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400'
}

const conditionColors: Record<string, string> = {
  Good: 'text-green-600 dark:text-green-400',
  Fair: 'text-yellow-600 dark:text-yellow-400',
  Poor: 'text-orange-600 dark:text-orange-400',
  Damaged: 'text-red-600 dark:text-red-400',
  Destroyed: 'text-gray-500 dark:text-gray-500'
}

const conditionBadgeColors: Record<string, string> = {
  Good: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  Fair: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400',
  Poor: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  Damaged: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  Destroyed: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-500'
}

const circulationColors: Record<string, string> = {
  Available: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  CheckedOut: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  InTransit: 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400',
  Overdue: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
  Returned: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-400',
  Lost: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
}

// ── Icon Maps ──
const locationIconMap: Record<string, string> = {
  Site: 'domain',
  Building: 'apartment',
  Floor: 'layers',
  Room: 'meeting_room',
  Rack: 'view_column',
  Shelf: 'shelves',
  Box: 'package_2',
  File: 'folder'
}

const itemTypeIconMap: Record<string, string> = {
  Document: 'description',
  File: 'folder',
  Box: 'package_2',
  Volume: 'auto_stories',
  Map: 'map',
  Photograph: 'photo_camera',
  AudioTape: 'music_note',
  VideoTape: 'videocam',
  Microfilm: 'film_reel',
  DigitalMedia: 'sd_card'
}

const itemTypeColorMap: Record<string, string> = {
  Document: 'bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400',
  File: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  Box: 'bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400',
  Volume: 'bg-purple-100 text-purple-600 dark:bg-purple-900/30 dark:text-purple-400',
  Map: 'bg-green-100 text-green-600 dark:bg-green-900/30 dark:text-green-400',
  Photograph: 'bg-pink-100 text-pink-600 dark:bg-pink-900/30 dark:text-pink-400',
  AudioTape: 'bg-orange-100 text-orange-600 dark:bg-orange-900/30 dark:text-orange-400',
  VideoTape: 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400',
  Microfilm: 'bg-indigo-100 text-indigo-600 dark:bg-indigo-900/30 dark:text-indigo-400',
  DigitalMedia: 'bg-cyan-100 text-cyan-600 dark:bg-cyan-900/30 dark:text-cyan-400'
}

function getLocationIcon(type: string): string {
  return locationIconMap[type] || 'location_on'
}

function getItemTypeIcon(type: string): string {
  return itemTypeIconMap[type] || 'description'
}

// ── Tree Computed ──
interface LocationTreeNode extends PhysicalLocation {
  children: LocationTreeNode[]
  depth: number
}

const locationTree = computed<LocationTreeNode[]>(() => {
  const map = new Map<string, LocationTreeNode>()
  const roots: LocationTreeNode[] = []

  // Create nodes
  for (const loc of locations.value) {
    map.set(loc.id, { ...loc, children: [], depth: 0 })
  }

  // Build tree
  for (const node of map.values()) {
    if (node.parentId && map.has(node.parentId)) {
      const parent = map.get(node.parentId)!
      node.depth = parent.depth + 1
      parent.children.push(node)
    } else {
      roots.push(node)
    }
  }

  // Sort children alphabetically
  function sortChildren(nodes: LocationTreeNode[]) {
    nodes.sort((a, b) => a.name.localeCompare(b.name))
    for (const n of nodes) sortChildren(n.children)
  }
  sortChildren(roots)

  return roots
})

// Flatten the tree for rendering (only expanded nodes)
const flattenedTree = computed<LocationTreeNode[]>(() => {
  const result: LocationTreeNode[] = []
  function walk(nodes: LocationTreeNode[]) {
    for (const node of nodes) {
      result.push(node)
      if (node.children.length > 0 && expandedIds.has(node.id)) {
        walk(node.children)
      }
    }
  }
  walk(locationTree.value)
  return result
})

function toggleExpand(id: string, e?: Event) {
  if (e) e.stopPropagation()
  if (expandedIds.has(id)) {
    expandedIds.delete(id)
  } else {
    expandedIds.add(id)
  }
}

// Auto-expand path to selected node
function expandPathTo(id: string) {
  const map = new Map<string, PhysicalLocation>()
  for (const loc of locations.value) map.set(loc.id, loc)

  let current = map.get(id)
  while (current?.parentId) {
    expandedIds.add(current.parentId)
    current = map.get(current.parentId)
  }
}

// ── Computed ──
const selectedLocation = computed(() =>
  locations.value.find(l => l.id === selectedLocationId.value) ?? null
)

const filteredItems = computed(() => {
  let result = items.value
  if (selectedLocationId.value) {
    result = result.filter(i => i.locationId === selectedLocationId.value)
  }
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim()
    result = result.filter(i =>
      i.title.toLowerCase().includes(q) ||
      i.barcode.toLowerCase().includes(q)
    )
  }
  return result
})

const stats = computed(() => ({
  totalLocations: locations.value.length,
  totalItems: items.value.length,
  checkedOut: items.value.filter(i => i.circulationStatus === 'CheckedOut').length,
  poorCondition: items.value.filter(i => i.condition === 'Poor' || i.condition === 'Damaged' || i.condition === 'Destroyed').length
}))

// ── Data Loading ──
// Flatten nested tree response into a flat array
function flattenLocations(nodes: any[]): PhysicalLocation[] {
  const result: PhysicalLocation[] = []
  function walk(list: any[]) {
    for (const n of list) {
      const { children, ...rest } = n
      result.push(rest as PhysicalLocation)
      if (Array.isArray(children) && children.length > 0) walk(children)
    }
  }
  walk(nodes)
  return result
}

async function loadLocations() {
  loadingLocations.value = true
  try {
    const { data } = await physicalLocationsApi.getAll()
    const raw = Array.isArray(data) ? data : (data?.data ?? [])
    locations.value = flattenLocations(raw)
    // Auto-expand all root nodes on first load
    for (const loc of locations.value) {
      if (!loc.parentId) expandedIds.add(loc.id)
    }
  } catch { /* empty */ }
  loadingLocations.value = false
}

async function loadItems() {
  loadingItems.value = true
  try {
    const { data } = await physicalItemsApi.getAll({ pageSize: 10000 })
    items.value = data?.items ?? data?.data ?? (Array.isArray(data) ? data : [])
  } catch { /* empty */ }
  loadingItems.value = false
}

async function loadAll() {
  await Promise.all([loadLocations(), loadItems()])
}

onMounted(loadAll)

// ── Location CRUD ──
function openCreateLocation() {
  locationForm.value = { id: '', name: '', code: '', locationType: 'Room', parentId: '', capacity: null, description: '' }
  isEditingLocation.value = false
  showLocationModal.value = true
}

function openEditLocation(loc: PhysicalLocation) {
  locationForm.value = {
    id: loc.id,
    name: loc.name,
    code: loc.code,
    locationType: loc.locationType,
    parentId: loc.parentId || '',
    capacity: loc.capacity ?? null,
    description: loc.environmentalConditions || ''
  }
  isEditingLocation.value = true
  showLocationModal.value = true
}

async function saveLocation() {
  saving.value = true
  try {
    const payload: any = {
      name: locationForm.value.name,
      code: locationForm.value.code,
      locationType: locationForm.value.locationType,
      parentId: locationForm.value.parentId || undefined,
      capacity: locationForm.value.capacity ?? undefined,
      environmentalConditions: locationForm.value.description || undefined
    }
    if (isEditingLocation.value) {
      await physicalLocationsApi.update(locationForm.value.id, payload)
    } else {
      await physicalLocationsApi.create(payload)
    }
    showLocationModal.value = false
    await loadLocations()
  } catch (err: any) {
    showError(extractError(err))
  }
  saving.value = false
}

function confirmDeleteLocation(loc: PhysicalLocation) {
  deleteType.value = 'location'
  deleteTargetId.value = loc.id
  deleteTargetName.value = loc.name
  showDeleteConfirm.value = true
}

// ── Item CRUD ──
function openCreateItem() {
  itemForm.value = {
    id: '',
    title: '',
    barcode: '',
    itemType: 'Document',
    description: '',
    locationId: selectedLocationId.value || '',
    condition: 'Good'
  }
  isEditingItem.value = false
  showItemModal.value = true
}

function openEditItem(item: PhysicalItem) {
  itemForm.value = {
    id: item.id,
    title: item.title,
    barcode: item.barcode,
    itemType: item.itemType,
    description: item.description || '',
    locationId: item.locationId || '',
    condition: item.condition
  }
  isEditingItem.value = true
  showItemModal.value = true
}

async function saveItem() {
  saving.value = true
  try {
    const payload: any = {
      title: itemForm.value.title,
      barcode: itemForm.value.barcode,
      itemType: itemForm.value.itemType,
      description: itemForm.value.description || undefined,
      locationId: itemForm.value.locationId || undefined,
      condition: itemForm.value.condition
    }
    if (isEditingItem.value) {
      await physicalItemsApi.update(itemForm.value.id, payload)
    } else {
      await physicalItemsApi.create(payload)
    }
    showItemModal.value = false
    await loadAll()
  } catch (err: any) {
    showError(extractError(err))
  }
  saving.value = false
}

function confirmDeleteItem(item: PhysicalItem) {
  deleteType.value = 'item'
  deleteTargetId.value = item.id
  deleteTargetName.value = item.title
  showDeleteConfirm.value = true
}

// ── Delete ──
async function executeDelete() {
  saving.value = true
  try {
    if (deleteType.value === 'location') {
      await physicalLocationsApi.delete(deleteTargetId.value)
      if (selectedLocationId.value === deleteTargetId.value) {
        selectedLocationId.value = null
      }
    } else {
      await physicalItemsApi.delete(deleteTargetId.value)
    }
    showDeleteConfirm.value = false
    await loadAll()
  } catch (err: any) {
    showError(extractError(err))
  }
  saving.value = false
}

// ── Move Item ──
function openMoveModal(item: PhysicalItem) {
  moveTarget.value = item
  moveLocationId.value = item.locationId || ''
  showMoveModal.value = true
}

async function executeMove() {
  if (!moveTarget.value || !moveLocationId.value) return
  saving.value = true
  try {
    await physicalItemsApi.move(moveTarget.value.id, moveLocationId.value)
    showMoveModal.value = false
    moveTarget.value = null
    await loadAll()
  } catch (err: any) {
    showError(extractError(err))
  }
  saving.value = false
}

// ── Update Condition ──
function openConditionModal(item: PhysicalItem) {
  conditionTarget.value = item
  conditionValue.value = item.condition
  conditionNotes.value = ''
  showConditionModal.value = true
}

async function executeConditionUpdate() {
  if (!conditionTarget.value) return
  saving.value = true
  try {
    await physicalItemsApi.updateCondition(conditionTarget.value.id, {
      condition: conditionValue.value,
      notes: conditionNotes.value || undefined
    })
    showConditionModal.value = false
    conditionTarget.value = null
    await loadItems()
  } catch { /* empty */ }
  saving.value = false
}

// ── Location Selection ──
function selectLocation(id: string) {
  if (selectedLocationId.value === id) {
    selectedLocationId.value = null
  } else {
    selectedLocationId.value = id
    expandPathTo(id)
  }
}

function capacityPercent(loc: PhysicalLocation): number {
  if (!loc.capacity || loc.capacity === 0) return 0
  return Math.min(100, Math.round((loc.currentCount / loc.capacity) * 100))
}

// Capacity info for the move modal target location
const moveTargetLocation = computed(() =>
  locations.value.find(l => l.id === moveLocationId.value) ?? null
)

// Capacity info for the item form location
const itemFormLocation = computed(() =>
  locations.value.find(l => l.id === itemForm.value.locationId) ?? null
)
</script>

<template>
  <div class="p-6 archive-page">
    <div class="max-w-[1600px] mx-auto">
      <!-- Back Button -->
      <button
        @click="router.push('/archive')"
        class="flex items-center gap-1.5 text-sm text-gray-500 dark:text-gray-400 hover:text-teal dark:hover:text-teal mb-4 transition-colors group"
      >
        <span class="material-symbols-outlined text-lg transition-transform group-hover:-translate-x-0.5">arrow_back</span>
        Back to Archive
      </button>

      <!-- Header -->
      <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-gradient-to-br from-teal to-teal/60 flex items-center justify-center shadow-lg shadow-teal/20">
              <span class="material-symbols-outlined text-white text-xl">warehouse</span>
            </div>
            Physical Archive
          </h1>
          <p class="text-gray-500 dark:text-gray-400 mt-1.5 ml-[52px] text-sm">Manage physical storage locations, items, and their conditions</p>
        </div>
        <div class="flex items-center gap-2 flex-wrap">
          <!-- Search -->
          <div class="relative">
            <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-lg">search</span>
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Search items..."
              class="pl-9 pr-4 py-2 w-56 border border-gray-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-sm text-gray-900 dark:text-white placeholder:text-gray-400 focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
            />
            <button
              v-if="searchQuery"
              @click="searchQuery = ''"
              class="absolute right-2.5 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 transition-colors"
            >
              <span class="material-symbols-outlined text-base">close</span>
            </button>
          </div>

          <!-- View Toggle -->
          <div class="flex items-center bg-gray-100 dark:bg-background-dark rounded-lg p-0.5 border border-gray-200 dark:border-border-dark">
            <button
              @click="viewMode = 'table'"
              class="flex items-center gap-1 px-2.5 py-1.5 rounded-md text-xs font-medium transition-all"
              :class="viewMode === 'table'
                ? 'bg-white dark:bg-surface-dark text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-500 dark:text-gray-400 hover:text-gray-700'"
            >
              <span class="material-symbols-outlined text-sm">table_rows</span>
              Table
            </button>
            <button
              @click="viewMode = 'grid'"
              class="flex items-center gap-1 px-2.5 py-1.5 rounded-md text-xs font-medium transition-all"
              :class="viewMode === 'grid'
                ? 'bg-white dark:bg-surface-dark text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-500 dark:text-gray-400 hover:text-gray-700'"
            >
              <span class="material-symbols-outlined text-sm">grid_view</span>
              Grid
            </button>
            <button
              @click="viewMode = 'spatial'"
              class="flex items-center gap-1 px-2.5 py-1.5 rounded-md text-xs font-medium transition-all"
              :class="viewMode === 'spatial'
                ? 'bg-white dark:bg-surface-dark text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-500 dark:text-gray-400 hover:text-gray-700'"
            >
              <span class="material-symbols-outlined text-sm">view_in_ar</span>
              Spatial
            </button>
          </div>

          <!-- Create Buttons -->
          <button
            @click="openCreateLocation"
            class="flex items-center gap-2 px-4 py-2 border border-gray-200 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors text-sm font-medium"
          >
            <span class="material-symbols-outlined text-lg">add_location_alt</span>
            <span class="hidden lg:inline">Location</span>
          </button>
          <button
            @click="openCreateItem"
            class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors text-sm font-medium shadow-sm shadow-teal/20"
          >
            <span class="material-symbols-outlined text-lg">add</span>
            <span class="hidden lg:inline">Item</span>
          </button>
        </div>
      </div>

      <!-- Stat Cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div class="bg-[#0d1117] p-5 rounded-xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">location_on</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Locations</span>
          </div>
          <div class="relative z-10">
            <p class="text-3xl font-bold tabular-nums">{{ stats.totalLocations }}</p>
            <p class="text-[10px] text-teal mt-2 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">warehouse</span>
              Storage locations
            </p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-5 rounded-xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">inventory_2</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Items</span>
          </div>
          <div class="relative z-10">
            <p class="text-3xl font-bold tabular-nums">{{ stats.totalItems }}</p>
            <p class="text-[10px] text-teal mt-2 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">archive</span>
              Physical items
            </p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-5 rounded-xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">output</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Checked Out</span>
          </div>
          <div class="relative z-10">
            <p class="text-3xl font-bold tabular-nums">{{ stats.checkedOut }}</p>
            <p class="text-[10px] text-teal mt-2 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">swap_horiz</span>
              In circulation
            </p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-5 rounded-xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">warning</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Attention</span>
          </div>
          <div class="relative z-10">
            <p class="text-3xl font-bold tabular-nums">{{ stats.poorCondition }}</p>
            <p class="text-[10px] text-teal mt-2 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">healing</span>
              Needs attention
            </p>
          </div>
        </div>
      </div>

      <!-- Error Banner -->
      <Transition name="modal">
        <div v-if="errorMessage" class="mb-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-lg p-3 flex items-start gap-2">
          <span class="material-symbols-outlined text-red-600 dark:text-red-400 text-lg mt-0.5 flex-shrink-0">error</span>
          <div class="flex-1">
            <p class="text-sm text-red-700 dark:text-red-300 font-medium">{{ errorMessage }}</p>
          </div>
          <button @click="errorMessage = ''" class="text-red-400 hover:text-red-600 transition-colors flex-shrink-0">
            <span class="material-symbols-outlined text-base">close</span>
          </button>
        </div>
      </Transition>

      <!-- Spatial Warehouse View -->
      <div v-if="viewMode === 'spatial'" class="mb-6">
        <SpatialView
          :locations="locations"
          :items="items"
          :selectedLocationId="selectedLocationId"
          @select="selectLocation"
          @selectItem="(id: string) => router.push(`/physical-archive/items/${id}`)"
        />
      </div>

      <!-- Two-Panel Layout -->
      <div v-else class="flex flex-col lg:flex-row gap-6">
        <!-- Left Panel: Location Tree Explorer -->
        <div class="w-full lg:w-[380px] flex-shrink-0">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden lg:sticky lg:top-6">
            <!-- Panel Header -->
            <div class="px-4 py-3 border-b border-gray-100 dark:border-border-dark bg-gray-50/50 dark:bg-background-dark/50">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <span class="material-symbols-outlined text-teal text-lg">account_tree</span>
                  <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Location Tree</h3>
                  <span class="text-[10px] text-gray-400 dark:text-gray-500 bg-gray-100 dark:bg-background-dark px-1.5 py-0.5 rounded-full tabular-nums">{{ locations.length }}</span>
                </div>
                <div class="flex items-center gap-1">
                  <button
                    v-if="selectedLocationId"
                    @click="selectedLocationId = null"
                    class="text-[11px] text-teal hover:text-teal/80 transition-colors font-medium px-2 py-0.5 rounded hover:bg-teal/5"
                  >
                    Clear
                  </button>
                  <button
                    @click="() => { for (const loc of locations) { if (!loc.parentId) expandedIds.add(loc.id) } }"
                    class="p-1 text-gray-400 hover:text-teal rounded transition-colors"
                    title="Expand all root"
                  >
                    <span class="material-symbols-outlined text-base">unfold_more</span>
                  </button>
                  <button
                    @click="expandedIds.clear()"
                    class="p-1 text-gray-400 hover:text-teal rounded transition-colors"
                    title="Collapse all"
                  >
                    <span class="material-symbols-outlined text-base">unfold_less</span>
                  </button>
                </div>
              </div>
            </div>

            <!-- Loading -->
            <div v-if="loadingLocations" class="flex items-center justify-center py-16">
              <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
            </div>

            <!-- Empty -->
            <div v-else-if="locations.length === 0" class="py-16 text-center">
              <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-gray-50 dark:bg-background-dark flex items-center justify-center">
                <span class="material-symbols-outlined text-3xl text-gray-300 dark:text-gray-600">location_off</span>
              </div>
              <p class="text-sm text-gray-500 dark:text-gray-400 font-medium">No locations defined</p>
              <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">Create your first storage location</p>
              <button
                @click="openCreateLocation"
                class="mt-4 inline-flex items-center gap-1.5 text-sm text-teal hover:text-teal/80 font-medium transition-colors"
              >
                <span class="material-symbols-outlined text-base">add</span>
                Create a location
              </button>
            </div>

            <!-- Location Tree -->
            <div v-else class="max-h-[calc(100vh-420px)] overflow-y-auto tree-scroll">
              <div
                v-for="node in flattenedTree"
                :key="node.id"
                @click="selectLocation(node.id)"
                class="tree-node cursor-pointer transition-all group border-l-[3px]"
                :class="selectedLocationId === node.id
                  ? 'bg-teal/5 dark:bg-teal/10 border-l-teal'
                  : 'border-l-transparent hover:bg-gray-50 dark:hover:bg-background-dark'"
              >
                <div
                  class="flex items-center gap-1.5 py-2 pr-3"
                  :style="{ paddingLeft: (node.depth * 20 + 12) + 'px' }"
                >
                  <!-- Expand/Collapse -->
                  <button
                    v-if="node.children.length > 0"
                    @click="toggleExpand(node.id, $event)"
                    class="w-5 h-5 flex items-center justify-center rounded hover:bg-gray-200 dark:hover:bg-border-dark transition-colors flex-shrink-0"
                  >
                    <span
                      class="material-symbols-outlined text-sm text-gray-400 transition-transform duration-200"
                      :class="expandedIds.has(node.id) ? 'rotate-90' : ''"
                    >chevron_right</span>
                  </button>
                  <div v-else class="w-5 flex-shrink-0"></div>

                  <!-- Location Type Icon -->
                  <span
                    class="material-symbols-outlined text-base flex-shrink-0"
                    :class="selectedLocationId === node.id ? 'text-teal' : 'text-gray-400 dark:text-gray-500'"
                  >{{ getLocationIcon(node.locationType) }}</span>

                  <!-- Name + Info -->
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-1.5">
                      <span
                        class="text-sm truncate font-medium"
                        :class="selectedLocationId === node.id
                          ? 'text-teal dark:text-teal'
                          : 'text-gray-800 dark:text-gray-200'"
                      >{{ node.name }}</span>
                      <span
                        class="text-[9px] px-1.5 py-px rounded-full font-semibold flex-shrink-0 uppercase tracking-wide"
                        :class="locationTypeColors[node.locationType] || 'bg-gray-100 text-gray-600'"
                      >{{ node.locationType }}</span>
                    </div>
                    <!-- Capacity bar -->
                    <div v-if="node.capacity" class="flex items-center gap-2 mt-0.5">
                      <div class="flex-1 h-1 bg-gray-100 dark:bg-background-dark rounded-full overflow-hidden max-w-[120px]">
                        <div
                          class="h-full rounded-full transition-all"
                          :class="capacityPercent(node) > 90 ? 'bg-rose-500' : capacityPercent(node) > 70 ? 'bg-amber-500' : 'bg-teal'"
                          :style="{ width: capacityPercent(node) + '%' }"
                        ></div>
                      </div>
                      <span class="text-[9px] text-gray-400 tabular-nums">{{ node.currentCount }}/{{ node.capacity }}</span>
                    </div>
                  </div>

                  <!-- Hover Actions -->
                  <div class="flex items-center gap-px opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0" @click.stop>
                    <button
                      @click="openEditLocation(node)"
                      class="p-0.5 text-gray-400 hover:text-teal rounded transition-colors"
                      title="Edit"
                    >
                      <span class="material-symbols-outlined text-sm">edit</span>
                    </button>
                    <button
                      @click="confirmDeleteLocation(node)"
                      class="p-0.5 text-gray-400 hover:text-red-500 rounded transition-colors"
                      title="Delete"
                    >
                      <span class="material-symbols-outlined text-sm">delete</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Panel: Items Display -->
        <div class="flex-1 min-w-0">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden">
            <!-- Panel Header -->
            <div class="px-4 py-3 border-b border-gray-100 dark:border-border-dark bg-gray-50/50 dark:bg-background-dark/50 flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-teal text-lg">inventory_2</span>
                <h3 class="text-sm font-semibold text-gray-900 dark:text-white">
                  {{ selectedLocation ? selectedLocation.name : 'All Items' }}
                </h3>
                <span class="text-[10px] text-gray-400 dark:text-gray-500 bg-gray-100 dark:bg-background-dark px-1.5 py-0.5 rounded-full tabular-nums">{{ filteredItems.length }}</span>
                <span v-if="searchQuery" class="text-[10px] text-teal bg-teal/10 px-2 py-0.5 rounded-full font-medium">
                  matching "{{ searchQuery }}"
                </span>
              </div>
              <div v-if="selectedLocation" class="flex items-center gap-2">
                <span
                  class="text-[10px] px-1.5 py-0.5 rounded-full font-medium"
                  :class="locationTypeColors[selectedLocation.locationType] || 'bg-gray-100 text-gray-600'"
                >
                  {{ selectedLocation.locationType }}
                </span>
                <span class="text-xs text-gray-400 dark:text-gray-500 font-mono">{{ selectedLocation.code }}</span>
              </div>
            </div>

            <!-- Loading -->
            <div v-if="loadingItems" class="flex items-center justify-center py-20">
              <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
            </div>

            <!-- Empty -->
            <div v-else-if="filteredItems.length === 0" class="py-20 text-center">
              <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-gray-50 dark:bg-background-dark flex items-center justify-center">
                <span class="material-symbols-outlined text-3xl text-gray-300 dark:text-gray-600">
                  {{ searchQuery ? 'search_off' : 'inventory_2' }}
                </span>
              </div>
              <p class="text-sm text-gray-500 dark:text-gray-400 font-medium">
                {{ searchQuery
                  ? 'No items match your search'
                  : selectedLocation
                    ? 'No items in this location'
                    : 'No physical items found' }}
              </p>
              <p v-if="searchQuery" class="text-xs text-gray-400 dark:text-gray-500 mt-1">
                Try a different search term
              </p>
              <button
                v-if="!searchQuery"
                @click="openCreateItem"
                class="mt-4 inline-flex items-center gap-1.5 text-sm text-teal hover:text-teal/80 font-medium transition-colors"
              >
                <span class="material-symbols-outlined text-base">add</span>
                Add an item
              </button>
            </div>

            <!-- Table View -->
            <div v-else-if="viewMode === 'table'" class="overflow-x-auto">
              <table class="w-full">
                <thead class="bg-gray-50 dark:bg-background-dark">
                  <tr>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Barcode</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Title</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Type</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Condition</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Status</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Location</th>
                    <th class="text-right py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest w-[100px]">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="item in filteredItems"
                    :key="item.id"
                    class="border-t border-gray-100 dark:border-border-dark hover:bg-gray-50/70 dark:hover:bg-background-dark/70 transition-colors group"
                  >
                    <td class="py-3 px-4">
                      <span class="font-mono text-xs text-gray-500 dark:text-gray-400 bg-gray-100 dark:bg-background-dark px-1.5 py-0.5 rounded">{{ item.barcode }}</span>
                    </td>
                    <td class="py-3 px-4">
                      <div class="flex items-center gap-2">
                        <router-link
                          :to="`/physical-archive/items/${item.id}`"
                          class="font-medium text-sm text-gray-900 dark:text-white hover:text-teal dark:hover:text-teal transition-colors"
                        >
                          {{ item.title }}
                        </router-link>
                        <span
                          v-if="item.isOnLegalHold"
                          class="material-symbols-outlined text-sm text-amber-500"
                          title="On Legal Hold"
                        >gavel</span>
                      </div>
                    </td>
                    <td class="py-3 px-4">
                      <div class="flex items-center gap-1.5">
                        <span class="material-symbols-outlined text-sm text-gray-400">{{ getItemTypeIcon(item.itemType) }}</span>
                        <span class="text-sm text-gray-600 dark:text-gray-300">{{ item.itemType }}</span>
                      </div>
                    </td>
                    <td class="py-3 px-4">
                      <span
                        class="text-xs px-2 py-0.5 rounded-full font-medium"
                        :class="conditionBadgeColors[item.condition] || 'bg-gray-100 text-gray-600'"
                      >
                        {{ item.condition }}
                      </span>
                    </td>
                    <td class="py-3 px-4">
                      <span
                        class="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded-full font-medium"
                        :class="circulationColors[item.circulationStatus] || 'bg-gray-100 text-gray-700'"
                      >
                        {{ item.circulationStatus }}
                      </span>
                    </td>
                    <td class="py-3 px-4 text-sm text-gray-500 dark:text-gray-400 max-w-[160px] truncate">{{ item.locationName || '-' }}</td>
                    <td class="py-3 px-4 text-right" @click.stop>
                      <div class="flex items-center justify-end gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity">
                        <button
                          @click="openConditionModal(item)"
                          class="p-1 text-gray-400 hover:text-amber-500 rounded transition-colors"
                          title="Update condition"
                        >
                          <span class="material-symbols-outlined text-base">healing</span>
                        </button>
                        <button
                          @click="openMoveModal(item)"
                          class="p-1 text-gray-400 hover:text-blue-500 rounded transition-colors"
                          title="Move item"
                        >
                          <span class="material-symbols-outlined text-base">drive_file_move</span>
                        </button>
                        <button
                          @click="openEditItem(item)"
                          class="p-1 text-gray-400 hover:text-teal rounded transition-colors"
                          title="Edit item"
                        >
                          <span class="material-symbols-outlined text-base">edit</span>
                        </button>
                        <button
                          @click="confirmDeleteItem(item)"
                          class="p-1 text-gray-400 hover:text-red-500 rounded transition-colors"
                          title="Delete item"
                        >
                          <span class="material-symbols-outlined text-base">delete</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Grid View -->
            <div v-else class="p-4 grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-3">
              <div
                v-for="item in filteredItems"
                :key="item.id"
                class="group relative bg-white dark:bg-surface-dark border border-gray-150 dark:border-border-dark rounded-xl p-4 hover:shadow-md hover:border-gray-300 dark:hover:border-gray-600 transition-all"
              >
                <!-- Top: Icon + Type -->
                <div class="flex items-start justify-between mb-3">
                  <div class="flex items-center gap-2.5">
                    <div
                      class="w-10 h-10 rounded-xl flex items-center justify-center flex-shrink-0"
                      :class="itemTypeColorMap[item.itemType] || 'bg-gray-100 text-gray-500'"
                    >
                      <span class="material-symbols-outlined text-xl">{{ getItemTypeIcon(item.itemType) }}</span>
                    </div>
                    <div class="min-w-0">
                      <router-link
                        :to="`/physical-archive/items/${item.id}`"
                        class="font-semibold text-sm text-gray-900 dark:text-white hover:text-teal dark:hover:text-teal transition-colors block truncate"
                      >
                        {{ item.title }}
                      </router-link>
                      <span class="font-mono text-[11px] text-gray-400 dark:text-gray-500">{{ item.barcode }}</span>
                    </div>
                  </div>
                  <span
                    v-if="item.isOnLegalHold"
                    class="material-symbols-outlined text-base text-amber-500 flex-shrink-0 mt-0.5"
                    title="On Legal Hold"
                  >gavel</span>
                </div>

                <!-- Badges -->
                <div class="flex items-center gap-1.5 mb-3">
                  <span
                    class="text-[10px] px-2 py-0.5 rounded-full font-medium"
                    :class="conditionBadgeColors[item.condition] || 'bg-gray-100 text-gray-600'"
                  >{{ item.condition }}</span>
                  <span
                    class="text-[10px] px-2 py-0.5 rounded-full font-medium"
                    :class="circulationColors[item.circulationStatus] || 'bg-gray-100 text-gray-700'"
                  >{{ item.circulationStatus }}</span>
                </div>

                <!-- Location -->
                <div class="flex items-center gap-1 text-xs text-gray-400 dark:text-gray-500 mb-3">
                  <span class="material-symbols-outlined text-sm">location_on</span>
                  <span class="truncate">{{ item.locationName || 'Unassigned' }}</span>
                </div>

                <!-- Actions (always visible in grid mode) -->
                <div class="flex items-center gap-1 pt-2.5 border-t border-gray-100 dark:border-border-dark" @click.stop>
                  <button
                    @click="openConditionModal(item)"
                    class="flex-1 flex items-center justify-center gap-1 py-1.5 text-xs text-gray-500 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/20 rounded-md transition-colors"
                    title="Condition"
                  >
                    <span class="material-symbols-outlined text-sm">healing</span>
                  </button>
                  <button
                    @click="openMoveModal(item)"
                    class="flex-1 flex items-center justify-center gap-1 py-1.5 text-xs text-gray-500 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-md transition-colors"
                    title="Move"
                  >
                    <span class="material-symbols-outlined text-sm">drive_file_move</span>
                  </button>
                  <button
                    @click="openEditItem(item)"
                    class="flex-1 flex items-center justify-center gap-1 py-1.5 text-xs text-gray-500 hover:text-teal hover:bg-teal/5 rounded-md transition-colors"
                    title="Edit"
                  >
                    <span class="material-symbols-outlined text-sm">edit</span>
                  </button>
                  <button
                    @click="confirmDeleteItem(item)"
                    class="flex-1 flex items-center justify-center gap-1 py-1.5 text-xs text-gray-500 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-md transition-colors"
                    title="Delete"
                  >
                    <span class="material-symbols-outlined text-sm">delete</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- Create/Edit Location Modal                                 -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showLocationModal" class="fixed inset-0 bg-black/50 z-40" @click="showLocationModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showLocationModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showLocationModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-hidden flex flex-col">
            <!-- Modal Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <span class="material-symbols-outlined text-2xl">add_location_alt</span>
                  <h3 class="text-lg font-bold">{{ isEditingLocation ? 'Edit Location' : 'Create Location' }}</h3>
                </div>
                <button @click="showLocationModal = false" class="p-1 hover:bg-white/20 rounded transition-colors">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-4 overflow-y-auto">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Name *</label>
                <input
                  v-model="locationForm.name"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="e.g., Main Archive Building"
                />
              </div>

              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Code *</label>
                  <input
                    v-model="locationForm.code"
                    type="text"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal font-mono"
                    placeholder="e.g., BLDG-A"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Type *</label>
                  <select
                    v-model="locationForm.locationType"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  >
                    <option v-for="lt in locationTypes" :key="lt" :value="lt">{{ lt }}</option>
                  </select>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Parent Location</label>
                <select
                  v-model="locationForm.parentId"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                >
                  <option value="">None (top level)</option>
                  <option
                    v-for="loc in locations.filter(l => l.id !== locationForm.id)"
                    :key="loc.id"
                    :value="loc.id"
                  >
                    {{ loc.path || loc.name }} ({{ loc.locationType }})
                  </option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Capacity</label>
                <input
                  v-model.number="locationForm.capacity"
                  type="number"
                  min="0"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Maximum number of items (optional)"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Description</label>
                <textarea
                  v-model="locationForm.description"
                  rows="2"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Environmental conditions, notes, etc."
                ></textarea>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="px-6 py-4 border-t border-gray-200 dark:border-border-dark flex justify-end gap-3">
              <button
                @click="showLocationModal = false"
                class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="saveLocation"
                :disabled="!locationForm.name.trim() || !locationForm.code.trim() || saving"
                class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <div v-if="saving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                {{ isEditingLocation ? 'Save Changes' : 'Create Location' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- Create/Edit Item Modal                                     -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showItemModal" class="fixed inset-0 bg-black/50 z-40" @click="showItemModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showItemModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showItemModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-hidden flex flex-col">
            <!-- Modal Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <span class="material-symbols-outlined text-2xl">inventory_2</span>
                  <h3 class="text-lg font-bold">{{ isEditingItem ? 'Edit Item' : 'Create Item' }}</h3>
                </div>
                <button @click="showItemModal = false" class="p-1 hover:bg-white/20 rounded transition-colors">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Modal Body -->
            <div class="p-6 space-y-4 overflow-y-auto">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Title *</label>
                <input
                  v-model="itemForm.title"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="e.g., Personnel Records 2024"
                />
              </div>

              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Barcode *</label>
                  <input
                    v-model="itemForm.barcode"
                    type="text"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal font-mono"
                    placeholder="e.g., PHY-000123"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Item Type *</label>
                  <select
                    v-model="itemForm.itemType"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  >
                    <option v-for="it in itemTypes" :key="it" :value="it">{{ it }}</option>
                  </select>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Description</label>
                <textarea
                  v-model="itemForm.description"
                  rows="2"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Brief description of the item"
                ></textarea>
              </div>

              <div class="grid grid-cols-2 gap-4">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Location</label>
                  <select
                    v-model="itemForm.locationId"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  >
                    <option value="">Unassigned</option>
                    <option v-for="loc in locations" :key="loc.id" :value="loc.id">
                      {{ loc.path || loc.name }} ({{ loc.code }})
                    </option>
                  </select>
                  <!-- Capacity indicator -->
                  <div v-if="itemFormLocation && itemFormLocation.capacity" class="mt-1.5">
                    <div class="flex items-center gap-2">
                      <div class="flex-1 h-1.5 bg-gray-100 dark:bg-background-dark rounded-full overflow-hidden">
                        <div
                          class="h-full rounded-full transition-all"
                          :class="capacityPercent(itemFormLocation) >= 100 ? 'bg-red-500' : capacityPercent(itemFormLocation) > 90 ? 'bg-rose-500' : capacityPercent(itemFormLocation) > 70 ? 'bg-amber-500' : 'bg-teal'"
                          :style="{ width: capacityPercent(itemFormLocation) + '%' }"
                        ></div>
                      </div>
                      <span class="text-[10px] text-gray-400 tabular-nums whitespace-nowrap">{{ itemFormLocation.currentCount }}/{{ itemFormLocation.capacity }}</span>
                    </div>
                    <p v-if="capacityPercent(itemFormLocation) >= 100" class="text-[11px] text-red-500 dark:text-red-400 font-medium mt-0.5 flex items-center gap-1">
                      <span class="material-symbols-outlined text-xs">error</span>
                      Location is at full capacity
                    </p>
                  </div>
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Condition</label>
                  <select
                    v-model="itemForm.condition"
                    class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  >
                    <option v-for="c in conditions" :key="c" :value="c">{{ c }}</option>
                  </select>
                </div>
              </div>
            </div>

            <!-- Modal Footer -->
            <div class="px-6 py-4 border-t border-gray-200 dark:border-border-dark flex justify-end gap-3">
              <button
                @click="showItemModal = false"
                class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="saveItem"
                :disabled="!itemForm.title.trim() || !itemForm.barcode.trim() || saving"
                class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <div v-if="saving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                {{ isEditingItem ? 'Save Changes' : 'Create Item' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- Move Item Modal                                            -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showMoveModal" class="fixed inset-0 bg-black/50 z-40" @click="showMoveModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showMoveModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showMoveModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Modal Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center gap-3">
                <span class="material-symbols-outlined text-2xl">drive_file_move</span>
                <h3 class="text-lg font-bold">Move Item</h3>
              </div>
            </div>

            <div class="p-6">
              <div class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800/50 rounded-lg p-3 mb-4">
                <div class="flex items-start gap-2">
                  <span class="material-symbols-outlined text-blue-600 dark:text-blue-400 text-lg mt-0.5">info</span>
                  <div>
                    <p class="text-sm font-medium text-blue-800 dark:text-blue-300">
                      Moving "{{ moveTarget?.title }}"
                    </p>
                    <p class="text-xs text-blue-600 dark:text-blue-400 mt-1">
                      Current location: {{ moveTarget?.locationName || 'Unassigned' }}
                    </p>
                  </div>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Target Location *</label>
                <select
                  v-model="moveLocationId"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                >
                  <option value="" disabled>Select a location</option>
                  <option v-for="loc in locations" :key="loc.id" :value="loc.id">
                    {{ loc.path || loc.name }} ({{ loc.code }})
                  </option>
                </select>
                <!-- Capacity indicator -->
                <div v-if="moveTargetLocation && moveTargetLocation.capacity" class="mt-1.5">
                  <div class="flex items-center gap-2">
                    <div class="flex-1 h-1.5 bg-gray-100 dark:bg-background-dark rounded-full overflow-hidden">
                      <div
                        class="h-full rounded-full transition-all"
                        :class="capacityPercent(moveTargetLocation) >= 100 ? 'bg-red-500' : capacityPercent(moveTargetLocation) > 90 ? 'bg-rose-500' : capacityPercent(moveTargetLocation) > 70 ? 'bg-amber-500' : 'bg-teal'"
                        :style="{ width: capacityPercent(moveTargetLocation) + '%' }"
                      ></div>
                    </div>
                    <span class="text-[10px] text-gray-400 tabular-nums whitespace-nowrap">{{ moveTargetLocation.currentCount }}/{{ moveTargetLocation.capacity }}</span>
                  </div>
                  <p v-if="capacityPercent(moveTargetLocation) >= 100" class="text-[11px] text-red-500 dark:text-red-400 font-medium mt-0.5 flex items-center gap-1">
                    <span class="material-symbols-outlined text-xs">error</span>
                    Location is at full capacity
                  </p>
                </div>
              </div>

              <div class="flex justify-end gap-3 mt-4">
                <button
                  @click="showMoveModal = false"
                  class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="executeMove"
                  :disabled="!moveLocationId || saving"
                  class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  <div v-if="saving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Move Item
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- Update Condition Modal                                     -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showConditionModal" class="fixed inset-0 bg-black/50 z-40" @click="showConditionModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showConditionModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showConditionModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Modal Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center gap-3">
                <span class="material-symbols-outlined text-2xl">healing</span>
                <h3 class="text-lg font-bold">Update Condition</h3>
              </div>
            </div>

            <div class="p-6">
              <div class="mb-4">
                <p class="text-sm text-gray-600 dark:text-gray-300">
                  Updating condition for <span class="font-medium text-gray-900 dark:text-white">{{ conditionTarget?.title }}</span>
                </p>
                <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">
                  Current: <span :class="conditionColors[conditionTarget?.condition || 'Good']" class="font-medium">{{ conditionTarget?.condition }}</span>
                </p>
              </div>

              <div class="mb-4">
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">New Condition *</label>
                <select
                  v-model="conditionValue"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                >
                  <option v-for="c in conditions" :key="c" :value="c">{{ c }}</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes</label>
                <textarea
                  v-model="conditionNotes"
                  rows="3"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Describe the condition assessment..."
                ></textarea>
              </div>

              <div class="flex justify-end gap-3 mt-4">
                <button
                  @click="showConditionModal = false"
                  class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="executeConditionUpdate"
                  :disabled="saving"
                  class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  <div v-if="saving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Update Condition
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ═══════════════════════════════════════════════════════════ -->
    <!-- Delete Confirmation Modal                                  -->
    <!-- ═══════════════════════════════════════════════════════════ -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showDeleteConfirm" class="fixed inset-0 bg-black/50 z-40" @click="showDeleteConfirm = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showDeleteConfirm" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showDeleteConfirm = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-sm overflow-hidden">
            <!-- Modal Header -->
            <div class="bg-gradient-to-r from-red-700 to-red-500 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="relative flex items-center gap-3">
                <span class="material-symbols-outlined text-2xl">warning</span>
                <h3 class="text-lg font-bold">Confirm Delete</h3>
              </div>
            </div>

            <div class="p-6">
              <p class="text-sm text-gray-600 dark:text-gray-300 mb-1">
                Are you sure you want to delete this {{ deleteType }}?
              </p>
              <p class="text-sm font-medium text-gray-900 dark:text-white mb-4">
                "{{ deleteTargetName }}"
              </p>
              <div class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-lg p-3 mb-4">
                <p class="text-xs text-red-600 dark:text-red-400 flex items-start gap-2">
                  <span class="material-symbols-outlined text-sm mt-0.5">error</span>
                  This action cannot be undone.
                </p>
              </div>

              <div class="flex justify-end gap-3">
                <button
                  @click="showDeleteConfirm = false"
                  class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="executeDelete"
                  :disabled="saving"
                  class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  <div v-if="saving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Delete
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.backdrop-enter-active,
.backdrop-leave-active {
  transition: opacity 0.2s ease;
}
.backdrop-enter-from,
.backdrop-leave-to {
  opacity: 0;
}
.modal-enter-active,
.modal-leave-active {
  transition: all 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
  transform: scale(0.95);
}

/* Tree scroll styling */
.tree-scroll::-webkit-scrollbar {
  width: 4px;
}
.tree-scroll::-webkit-scrollbar-track {
  background: transparent;
}
.tree-scroll::-webkit-scrollbar-thumb {
  background: rgba(0, 0, 0, 0.1);
  border-radius: 4px;
}
.tree-scroll::-webkit-scrollbar-thumb:hover {
  background: rgba(0, 0, 0, 0.2);
}
:root.dark .tree-scroll::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.1);
}
:root.dark .tree-scroll::-webkit-scrollbar-thumb:hover {
  background: rgba(255, 255, 255, 0.2);
}
</style>
