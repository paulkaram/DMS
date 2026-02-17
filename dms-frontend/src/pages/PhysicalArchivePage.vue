<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { physicalLocationsApi, physicalItemsApi } from '@/api/client'
import type { PhysicalLocation, PhysicalItem } from '@/types'

const router = useRouter()

// ── Data ──
const locations = ref<PhysicalLocation[]>([])
const items = ref<PhysicalItem[]>([])
const selectedLocationId = ref<string | null>(null)
const loadingLocations = ref(false)
const loadingItems = ref(false)
const saving = ref(false)

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

// ── Computed ──
const selectedLocation = computed(() =>
  locations.value.find(l => l.id === selectedLocationId.value) ?? null
)

const filteredItems = computed(() => {
  if (!selectedLocationId.value) return items.value
  return items.value.filter(i => i.locationId === selectedLocationId.value)
})

const stats = computed(() => ({
  totalLocations: locations.value.length,
  totalItems: items.value.length,
  checkedOut: items.value.filter(i => i.circulationStatus === 'CheckedOut').length,
  poorCondition: items.value.filter(i => i.condition === 'Poor' || i.condition === 'Damaged' || i.condition === 'Destroyed').length
}))

// ── Data Loading ──
async function loadLocations() {
  loadingLocations.value = true
  try {
    const { data } = await physicalLocationsApi.getAll()
    locations.value = data.data ?? data ?? []
  } catch { /* empty */ }
  loadingLocations.value = false
}

async function loadItems() {
  loadingItems.value = true
  try {
    const { data } = await physicalItemsApi.getAll()
    items.value = data.data ?? data ?? []
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
  } catch { /* empty */ }
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
  } catch { /* empty */ }
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
  } catch { /* empty */ }
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
  } catch { /* empty */ }
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
  selectedLocationId.value = selectedLocationId.value === id ? null : id
}

function capacityPercent(loc: PhysicalLocation): number {
  if (!loc.capacity || loc.capacity === 0) return 0
  return Math.min(100, Math.round((loc.currentCount / loc.capacity) * 100))
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-[1600px] mx-auto">
      <!-- Back Button -->
      <button
        @click="router.push('/archive')"
        class="flex items-center gap-1.5 text-sm text-gray-500 dark:text-gray-400 hover:text-teal dark:hover:text-teal mb-4 transition-colors"
      >
        <span class="material-symbols-outlined text-lg">arrow_back</span>
        Back to Archive
      </button>

      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Physical Archive</h1>
          <p class="text-gray-500 dark:text-gray-400 mt-1">Manage physical storage locations and items</p>
        </div>
        <div class="flex items-center gap-3">
          <button
            @click="openCreateLocation"
            class="flex items-center gap-2 px-4 py-2 border border-gray-200 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors text-sm font-medium"
          >
            <span class="material-symbols-outlined text-lg">add_location_alt</span>
            Create Location
          </button>
          <button
            @click="openCreateItem"
            class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors text-sm font-medium"
          >
            <span class="material-symbols-outlined text-lg">add</span>
            Create Item
          </button>
        </div>
      </div>

      <!-- Stat Cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">location_on</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Locations</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.totalLocations }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">warehouse</span>
              Storage locations
            </p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">inventory_2</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Items</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.totalItems }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">archive</span>
              Physical items
            </p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">output</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Checked Out</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.checkedOut }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">swap_horiz</span>
              In circulation
            </p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">warning</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Poor Condition</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.poorCondition }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">healing</span>
              Needs attention
            </p>
          </div>
        </div>
      </div>

      <!-- Two-Panel Layout -->
      <div class="flex gap-6">
        <!-- Left Panel: Location Hierarchy -->
        <div class="w-[380px] flex-shrink-0">
          <div class="bg-white dark:bg-surface-dark rounded-lg shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden sticky top-6">
            <!-- Panel Header -->
            <div class="px-4 py-3 border-b border-gray-100 dark:border-border-dark flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-teal text-lg">location_on</span>
                <h3 class="text-sm font-semibold text-gray-900 dark:text-white">Locations</h3>
                <span class="text-xs text-gray-400 dark:text-gray-500">({{ locations.length }})</span>
              </div>
              <button
                v-if="selectedLocationId"
                @click="selectedLocationId = null"
                class="text-xs text-teal hover:text-teal/80 transition-colors font-medium"
              >
                Clear filter
              </button>
            </div>

            <!-- Loading -->
            <div v-if="loadingLocations" class="flex items-center justify-center py-12">
              <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
            </div>

            <!-- Empty -->
            <div v-else-if="locations.length === 0" class="py-12 text-center">
              <span class="material-symbols-outlined text-4xl text-gray-300 dark:text-gray-600 block mb-2">location_off</span>
              <p class="text-sm text-gray-400 dark:text-gray-500">No locations defined</p>
              <button
                @click="openCreateLocation"
                class="mt-3 text-sm text-teal hover:text-teal/80 font-medium transition-colors"
              >
                Create a location
              </button>
            </div>

            <!-- Location List -->
            <div v-else class="max-h-[calc(100vh-400px)] overflow-y-auto divide-y divide-gray-50 dark:divide-border-dark">
              <div
                v-for="loc in locations"
                :key="loc.id"
                @click="selectLocation(loc.id)"
                class="px-4 py-3 cursor-pointer transition-all group"
                :class="selectedLocationId === loc.id
                  ? 'bg-teal/5 dark:bg-teal/10 border-l-[3px] border-l-teal'
                  : 'hover:bg-gray-50 dark:hover:bg-background-dark border-l-[3px] border-l-transparent'"
              >
                <div class="flex items-start justify-between gap-2">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 mb-1">
                      <span class="font-medium text-sm text-gray-900 dark:text-white truncate">{{ loc.name }}</span>
                      <span
                        class="text-[10px] px-1.5 py-0.5 rounded-full font-medium flex-shrink-0"
                        :class="locationTypeColors[loc.locationType] || 'bg-gray-100 text-gray-600'"
                      >
                        {{ loc.locationType }}
                      </span>
                    </div>
                    <div class="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
                      <span class="font-mono">{{ loc.code }}</span>
                      <span class="text-gray-300 dark:text-gray-600">&middot;</span>
                      <span class="truncate">{{ loc.path || '/' }}</span>
                    </div>
                    <!-- Capacity Bar -->
                    <div v-if="loc.capacity" class="mt-2 flex items-center gap-2">
                      <div class="flex-1 h-1.5 bg-gray-100 dark:bg-background-dark rounded-full overflow-hidden">
                        <div
                          class="h-full rounded-full transition-all"
                          :class="capacityPercent(loc) > 90 ? 'bg-red-500' : capacityPercent(loc) > 70 ? 'bg-amber-500' : 'bg-teal'"
                          :style="{ width: capacityPercent(loc) + '%' }"
                        ></div>
                      </div>
                      <span class="text-[10px] text-gray-400 dark:text-gray-500 tabular-nums">{{ loc.currentCount }}/{{ loc.capacity }}</span>
                    </div>
                    <div v-else class="mt-1">
                      <span class="text-[10px] text-gray-400 dark:text-gray-500">{{ loc.currentCount }} items</span>
                    </div>
                  </div>

                  <!-- Actions (show on hover) -->
                  <div class="flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity flex-shrink-0" @click.stop>
                    <button
                      @click="openEditLocation(loc)"
                      class="p-1 text-gray-400 hover:text-teal rounded transition-colors"
                      title="Edit location"
                    >
                      <span class="material-symbols-outlined text-base">edit</span>
                    </button>
                    <button
                      @click="confirmDeleteLocation(loc)"
                      class="p-1 text-gray-400 hover:text-red-500 rounded transition-colors"
                      title="Delete location"
                    >
                      <span class="material-symbols-outlined text-base">delete</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Panel: Items Table -->
        <div class="flex-1 min-w-0">
          <div class="bg-white dark:bg-surface-dark rounded-lg shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden">
            <!-- Panel Header -->
            <div class="px-4 py-3 border-b border-gray-100 dark:border-border-dark flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined text-teal text-lg">inventory_2</span>
                <h3 class="text-sm font-semibold text-gray-900 dark:text-white">
                  {{ selectedLocation ? selectedLocation.name + ' Items' : 'All Items' }}
                </h3>
                <span class="text-xs text-gray-400 dark:text-gray-500">({{ filteredItems.length }})</span>
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
            <div v-if="loadingItems" class="flex items-center justify-center py-16">
              <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
            </div>

            <!-- Empty -->
            <div v-else-if="filteredItems.length === 0" class="py-16 text-center">
              <span class="material-symbols-outlined text-5xl text-gray-300 dark:text-gray-600 block mb-3">inventory_2</span>
              <p class="text-sm text-gray-400 dark:text-gray-500 mb-1">
                {{ selectedLocation ? 'No items in this location' : 'No physical items found' }}
              </p>
              <button
                @click="openCreateItem"
                class="mt-3 text-sm text-teal hover:text-teal/80 font-medium transition-colors"
              >
                Add an item
              </button>
            </div>

            <!-- Items Table -->
            <div v-else class="overflow-x-auto">
              <table class="w-full">
                <thead class="bg-gray-50 dark:bg-background-dark">
                  <tr>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Barcode</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Title</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Type</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Condition</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Status</th>
                    <th class="text-left py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Location</th>
                    <th class="text-right py-3 px-4 text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="item in filteredItems"
                    :key="item.id"
                    class="border-t border-gray-100 dark:border-border-dark hover:bg-gray-50 dark:hover:bg-background-dark transition-colors group"
                  >
                    <td class="py-3 px-4 font-mono text-xs text-gray-600 dark:text-gray-300">{{ item.barcode }}</td>
                    <td class="py-3 px-4">
                      <router-link
                        :to="`/physical-archive/items/${item.id}`"
                        class="font-medium text-sm text-gray-900 dark:text-white hover:text-teal dark:hover:text-teal transition-colors"
                      >
                        {{ item.title }}
                      </router-link>
                    </td>
                    <td class="py-3 px-4 text-sm text-gray-600 dark:text-gray-300">{{ item.itemType }}</td>
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
                    <td class="py-3 px-4 text-sm text-gray-500 dark:text-gray-400">{{ item.locationName || '-' }}</td>
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
</style>
