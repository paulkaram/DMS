<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { referenceDataApi } from '@/api/client'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

interface LookupItem {
  id: string
  lookupId: string
  value: string
  displayText: string
  language?: string
  sortOrder: number
}

interface Lookup {
  id: string
  name: string
  description?: string
  itemCount: number
}

interface LookupDetail {
  id: string
  name: string
  description?: string
  items: LookupItem[]
}

const lookups = ref<Lookup[]>([])
const selectedLookup = ref<LookupDetail | null>(null)
const loading = ref(false)
const loadingItems = ref(false)
const saving = ref(false)
const searchQuery = ref('')

// Modals
const showLookupModal = ref(false)
const showItemModal = ref(false)
const showDeleteConfirm = ref(false)
const showDeleteItemConfirm = ref(false)
const isEditing = ref(false)
const deleteTarget = ref<{ id: string; name: string } | null>(null)
const deleteItemTarget = ref<{ id: string; displayText: string } | null>(null)

const lookupForm = ref({ name: '', description: '' })
const itemForm = ref({ id: '', value: '', displayText: '', language: '', sortOrder: 0 })

const filteredLookups = computed(() => {
  if (!searchQuery.value) return lookups.value
  const q = searchQuery.value.toLowerCase()
  return lookups.value.filter(l =>
    l.name.toLowerCase().includes(q) ||
    l.description?.toLowerCase().includes(q)
  )
})

const sortedItems = computed(() => {
  if (!selectedLookup.value) return []
  return [...selectedLookup.value.items].sort((a, b) => a.sortOrder - b.sortOrder)
})

async function fetchLookups() {
  loading.value = true
  try {
    const { data } = await referenceDataApi.getLookups()
    lookups.value = data
  } catch (e) {
    console.error('Failed to fetch lookups', e)
  } finally {
    loading.value = false
  }
}

async function selectLookup(lookup: Lookup) {
  loadingItems.value = true
  try {
    const { data } = await referenceDataApi.getLookupById(lookup.id)
    selectedLookup.value = data
  } catch (e) {
    console.error('Failed to fetch lookup detail', e)
  } finally {
    loadingItems.value = false
  }
}

function openCreateLookupModal() {
  lookupForm.value = { name: '', description: '' }
  isEditing.value = false
  showLookupModal.value = true
}

function openEditLookupModal() {
  if (!selectedLookup.value) return
  lookupForm.value = {
    name: selectedLookup.value.name,
    description: selectedLookup.value.description || ''
  }
  isEditing.value = true
  showLookupModal.value = true
}

async function handleSaveLookup() {
  saving.value = true
  try {
    if (isEditing.value && selectedLookup.value) {
      await referenceDataApi.updateLookup(selectedLookup.value.id, lookupForm.value)
    } else {
      await referenceDataApi.createLookup(lookupForm.value)
    }
    showLookupModal.value = false
    await fetchLookups()
    if (isEditing.value && selectedLookup.value) {
      await selectLookup({ id: selectedLookup.value.id, name: lookupForm.value.name, description: lookupForm.value.description, itemCount: 0 })
    }
  } catch (e) {
    console.error('Failed to save lookup', e)
  } finally {
    saving.value = false
  }
}

function confirmDeleteLookup(lookup: Lookup) {
  deleteTarget.value = { id: lookup.id, name: lookup.name }
  showDeleteConfirm.value = true
}

async function handleDeleteLookup() {
  if (!deleteTarget.value) return
  try {
    await referenceDataApi.deleteLookup(deleteTarget.value.id)
    if (selectedLookup.value?.id === deleteTarget.value.id) {
      selectedLookup.value = null
    }
    showDeleteConfirm.value = false
    deleteTarget.value = null
    await fetchLookups()
  } catch (e) {
    console.error('Failed to delete lookup', e)
  }
}

function openCreateItemModal() {
  if (!selectedLookup.value) return
  const maxOrder = selectedLookup.value.items.length > 0
    ? Math.max(...selectedLookup.value.items.map(i => i.sortOrder))
    : 0
  itemForm.value = { id: '', value: '', displayText: '', language: '', sortOrder: maxOrder + 1 }
  isEditing.value = false
  showItemModal.value = true
}

function openEditItemModal(item: LookupItem) {
  itemForm.value = {
    id: item.id,
    value: item.value,
    displayText: item.displayText || '',
    language: item.language || '',
    sortOrder: item.sortOrder
  }
  isEditing.value = true
  showItemModal.value = true
}

async function handleSaveItem() {
  if (!selectedLookup.value) return
  saving.value = true
  try {
    const payload = {
      value: itemForm.value.value,
      displayText: itemForm.value.displayText || undefined,
      language: itemForm.value.language || undefined,
      sortOrder: itemForm.value.sortOrder
    }
    if (isEditing.value) {
      await referenceDataApi.updateLookupItem(itemForm.value.id, payload)
    } else {
      await referenceDataApi.createLookupItem(selectedLookup.value.id, payload)
    }
    showItemModal.value = false
    await selectLookup({ id: selectedLookup.value.id, name: selectedLookup.value.name, description: selectedLookup.value.description, itemCount: 0 })
    await fetchLookups()
  } catch (e) {
    console.error('Failed to save item', e)
  } finally {
    saving.value = false
  }
}

function confirmDeleteItem(item: LookupItem) {
  deleteItemTarget.value = { id: item.id, displayText: item.displayText || item.value }
  showDeleteItemConfirm.value = true
}

async function handleDeleteItem() {
  if (!deleteItemTarget.value || !selectedLookup.value) return
  try {
    await referenceDataApi.deleteLookupItem(deleteItemTarget.value.id)
    showDeleteItemConfirm.value = false
    deleteItemTarget.value = null
    await selectLookup({ id: selectedLookup.value.id, name: selectedLookup.value.name, description: selectedLookup.value.description, itemCount: 0 })
    await fetchLookups()
  } catch (e) {
    console.error('Failed to delete item', e)
  }
}

onMounted(fetchLookups)
</script>

<template>
  <div class="p-6 min-h-screen">
    <div class="max-w-7xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Lookups" icon="list" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Lookup Tables</h1>
          <p class="text-gray-500 dark:text-zinc-400 mt-1">Manage lookup tables and their values used across the system</p>
        </div>
        <button
          @click="openCreateLookupModal"
          class="flex items-center gap-2 px-5 py-2.5 bg-gradient-to-r from-teal to-teal/80 text-white rounded-xl
                 hover:shadow-lg hover:shadow-teal/25 transition-all duration-200 font-medium"
        >
          <span class="material-symbols-outlined text-[20px]">add</span>
          New Lookup
        </button>
      </div>

      <div class="grid grid-cols-12 gap-6">
        <!-- Left Panel: Lookup List -->
        <div class="col-span-4">
          <div class="bg-white dark:bg-surface-dark border border-gray-200 dark:border-border-dark rounded-2xl overflow-hidden shadow-sm">
            <!-- Search -->
            <div class="p-4 border-b border-gray-100 dark:border-border-dark">
              <div class="relative">
                <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 dark:text-zinc-500 text-[20px]">search</span>
                <input
                  v-model="searchQuery"
                  type="text"
                  placeholder="Search lookups..."
                  class="w-full pl-10 pr-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-xl
                         bg-gray-50 dark:bg-background-dark text-gray-900 dark:text-white
                         placeholder-gray-400 dark:placeholder-zinc-500
                         focus:outline-none focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                />
              </div>
            </div>

            <!-- Loading State -->
            <div v-if="loading" class="p-8 text-center">
              <div class="inline-flex items-center gap-2 text-gray-400 dark:text-zinc-500">
                <svg class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                </svg>
                Loading...
              </div>
            </div>

            <!-- Empty State -->
            <div v-else-if="filteredLookups.length === 0" class="p-8 text-center">
              <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-gray-100 dark:bg-border-dark flex items-center justify-center">
                <span class="material-symbols-outlined text-3xl text-gray-300 dark:text-zinc-600">list</span>
              </div>
              <p class="text-gray-500 dark:text-zinc-400 text-sm">
                {{ searchQuery ? 'No lookups match your search' : 'No lookup tables yet' }}
              </p>
              <button
                v-if="!searchQuery"
                @click="openCreateLookupModal"
                class="mt-3 text-sm text-teal hover:text-teal/80 font-medium transition-colors"
              >
                Create your first lookup
              </button>
            </div>

            <!-- Lookup List -->
            <div v-else class="divide-y divide-gray-100 dark:divide-border-dark max-h-[calc(100vh-320px)] overflow-y-auto">
              <div
                v-for="lookup in filteredLookups"
                :key="lookup.id"
                @click="selectLookup(lookup)"
                class="group p-4 cursor-pointer transition-all duration-150"
                :class="[
                  selectedLookup?.id === lookup.id
                    ? 'bg-teal/5 dark:bg-teal/10 border-l-[3px] border-l-teal'
                    : 'hover:bg-gray-50 dark:hover:bg-border-dark/50 border-l-[3px] border-l-transparent'
                ]"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-2">
                      <span class="material-symbols-outlined text-[18px]"
                            :class="selectedLookup?.id === lookup.id ? 'text-teal' : 'text-gray-400 dark:text-zinc-500'">
                        data_object
                      </span>
                      <p class="font-semibold text-sm truncate"
                         :class="selectedLookup?.id === lookup.id ? 'text-teal' : 'text-gray-900 dark:text-white'">
                        {{ lookup.name }}
                      </p>
                    </div>
                    <p v-if="lookup.description" class="text-xs text-gray-500 dark:text-zinc-400 mt-1 ml-[26px] line-clamp-1">
                      {{ lookup.description }}
                    </p>
                  </div>
                  <div class="flex items-center gap-1.5 flex-shrink-0">
                    <span class="inline-flex items-center px-2 py-0.5 text-[11px] font-medium rounded-full
                                 bg-gray-100 dark:bg-border-dark text-gray-500 dark:text-zinc-400">
                      {{ lookup.itemCount }}
                    </span>
                    <button
                      @click.stop="confirmDeleteLookup(lookup)"
                      class="p-1 rounded-lg opacity-0 group-hover:opacity-100 text-gray-400 hover:text-red-500
                             hover:bg-red-50 dark:hover:bg-red-500/10 transition-all"
                    >
                      <span class="material-symbols-outlined text-[16px]">delete</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Panel: Items -->
        <div class="col-span-8">
          <div class="bg-white dark:bg-surface-dark border border-gray-200 dark:border-border-dark rounded-2xl overflow-hidden shadow-sm">
            <!-- Items Header -->
            <div v-if="selectedLookup" class="p-5 border-b border-gray-100 dark:border-border-dark">
              <div class="flex items-center justify-between">
                <div>
                  <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl bg-gradient-to-br from-teal to-teal/70 flex items-center justify-center shadow-sm">
                      <span class="material-symbols-outlined text-white text-xl">data_object</span>
                    </div>
                    <div>
                      <h2 class="text-lg font-bold text-gray-900 dark:text-white">{{ selectedLookup.name }}</h2>
                      <p v-if="selectedLookup.description" class="text-sm text-gray-500 dark:text-zinc-400">
                        {{ selectedLookup.description }}
                      </p>
                    </div>
                  </div>
                </div>
                <div class="flex items-center gap-2">
                  <button
                    @click="openEditLookupModal"
                    class="flex items-center gap-1.5 px-3 py-2 text-sm text-gray-600 dark:text-zinc-300
                           border border-gray-200 dark:border-border-dark rounded-xl
                           hover:bg-gray-50 dark:hover:bg-border-dark transition-colors"
                  >
                    <span class="material-symbols-outlined text-[16px]">edit</span>
                    Edit
                  </button>
                  <button
                    @click="openCreateItemModal"
                    class="flex items-center gap-1.5 px-4 py-2 text-sm font-medium
                           bg-teal text-white rounded-xl hover:bg-teal/90 transition-colors shadow-sm"
                  >
                    <span class="material-symbols-outlined text-[16px]">add</span>
                    Add Item
                  </button>
                </div>
              </div>
            </div>

            <!-- Loading Items -->
            <div v-if="loadingItems" class="p-12 text-center">
              <div class="inline-flex items-center gap-2 text-gray-400 dark:text-zinc-500">
                <svg class="w-5 h-5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                </svg>
                Loading items...
              </div>
            </div>

            <!-- No Lookup Selected -->
            <div v-else-if="!selectedLookup" class="p-16 text-center">
              <div class="w-20 h-20 mx-auto mb-5 rounded-2xl bg-gradient-to-br from-gray-100 to-gray-50
                          dark:from-border-dark dark:to-background-dark flex items-center justify-center">
                <span class="material-symbols-outlined text-4xl text-gray-300 dark:text-zinc-600">touch_app</span>
              </div>
              <p class="text-gray-500 dark:text-zinc-400 font-medium">Select a lookup table</p>
              <p class="text-gray-400 dark:text-zinc-500 text-sm mt-1">Choose a lookup from the left panel to view and manage its items</p>
            </div>

            <!-- Items Table -->
            <div v-else-if="sortedItems.length > 0" class="overflow-x-auto">
              <table class="w-full">
                <thead>
                  <tr class="bg-gray-50/80 dark:bg-background-dark/50">
                    <th class="text-left py-3 px-5 text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider w-20">Order</th>
                    <th class="text-left py-3 px-5 text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Value</th>
                    <th class="text-left py-3 px-5 text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider">Display Text</th>
                    <th class="text-left py-3 px-5 text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider w-28">Language</th>
                    <th class="text-right py-3 px-5 text-xs font-semibold text-gray-500 dark:text-zinc-400 uppercase tracking-wider w-24">Actions</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-gray-100 dark:divide-border-dark">
                  <tr
                    v-for="item in sortedItems"
                    :key="item.id"
                    class="group hover:bg-gray-50/50 dark:hover:bg-border-dark/30 transition-colors"
                  >
                    <td class="py-3.5 px-5">
                      <span class="inline-flex items-center justify-center w-7 h-7 text-xs font-medium rounded-lg
                                   bg-gray-100 dark:bg-border-dark text-gray-500 dark:text-zinc-400">
                        {{ item.sortOrder }}
                      </span>
                    </td>
                    <td class="py-3.5 px-5">
                      <code class="px-2.5 py-1 text-sm font-mono rounded-lg
                                   bg-teal/5 dark:bg-teal/10 text-teal border border-teal/20">
                        {{ item.value }}
                      </code>
                    </td>
                    <td class="py-3.5 px-5 text-sm text-gray-700 dark:text-zinc-200 font-medium">
                      {{ item.displayText || '—' }}
                    </td>
                    <td class="py-3.5 px-5">
                      <span v-if="item.language" class="inline-flex items-center px-2 py-0.5 text-[11px] font-medium rounded-full
                                   bg-blue-50 dark:bg-blue-500/10 text-blue-600 dark:text-blue-400">
                        {{ item.language }}
                      </span>
                      <span v-else class="text-xs text-gray-300 dark:text-zinc-600">—</span>
                    </td>
                    <td class="py-3.5 px-5 text-right">
                      <div class="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <button
                          @click="openEditItemModal(item)"
                          class="p-1.5 rounded-lg text-gray-400 hover:text-teal hover:bg-teal/10 transition-colors"
                          title="Edit item"
                        >
                          <span class="material-symbols-outlined text-[18px]">edit</span>
                        </button>
                        <button
                          @click="confirmDeleteItem(item)"
                          class="p-1.5 rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors"
                          title="Delete item"
                        >
                          <span class="material-symbols-outlined text-[18px]">delete</span>
                        </button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>

            <!-- Empty Items -->
            <div v-else class="p-12 text-center">
              <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-gray-100 dark:bg-border-dark flex items-center justify-center">
                <span class="material-symbols-outlined text-3xl text-gray-300 dark:text-zinc-600">playlist_add</span>
              </div>
              <p class="text-gray-500 dark:text-zinc-400 font-medium">No items yet</p>
              <p class="text-gray-400 dark:text-zinc-500 text-sm mt-1">Add items to this lookup table to use them across the system</p>
              <button
                @click="openCreateItemModal"
                class="mt-4 inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium
                       bg-teal text-white rounded-xl hover:bg-teal/90 transition-colors"
              >
                <span class="material-symbols-outlined text-[16px]">add</span>
                Add First Item
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ========== Lookup Create/Edit Modal ========== -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showLookupModal" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4" @click.self="showLookupModal = false">
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
            appear
          >
            <div v-if="showLookupModal" class="bg-white dark:bg-surface-dark rounded-2xl shadow-2xl w-full max-w-md overflow-hidden">
              <!-- Header -->
              <div class="px-6 py-4 bg-gradient-to-r from-navy to-teal relative overflow-hidden">
                <div class="absolute -right-4 -top-4 w-24 h-24 rounded-full bg-white/5"></div>
                <div class="absolute -right-8 -bottom-8 w-32 h-32 rounded-full bg-white/5"></div>
                <div class="relative flex items-center gap-3">
                  <div class="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-white">{{ isEditing ? 'edit' : 'add_circle' }}</span>
                  </div>
                  <div>
                    <h3 class="text-lg font-bold text-white">{{ isEditing ? 'Edit Lookup' : 'New Lookup' }}</h3>
                    <p class="text-white/60 text-sm">{{ isEditing ? 'Update lookup table details' : 'Create a new lookup table' }}</p>
                  </div>
                </div>
              </div>

              <!-- Body -->
              <div class="p-6 space-y-5">
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Name</label>
                  <input
                    v-model="lookupForm.name"
                    type="text"
                    placeholder="e.g. DocumentStatus, Priority"
                    class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-xl
                           bg-white dark:bg-background-dark text-gray-900 dark:text-white
                           placeholder-gray-400 dark:placeholder-zinc-500
                           focus:outline-none focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                  />
                </div>
                <div>
                  <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Description</label>
                  <textarea
                    v-model="lookupForm.description"
                    rows="3"
                    placeholder="Optional description of this lookup table..."
                    class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-xl
                           bg-white dark:bg-background-dark text-gray-900 dark:text-white
                           placeholder-gray-400 dark:placeholder-zinc-500
                           focus:outline-none focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all resize-none"
                  ></textarea>
                </div>
              </div>

              <!-- Footer -->
              <div class="px-6 py-4 bg-gray-50 dark:bg-background-dark border-t border-gray-100 dark:border-border-dark flex justify-end gap-3">
                <button
                  @click="showLookupModal = false"
                  class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-zinc-300
                         border border-gray-200 dark:border-border-dark rounded-xl
                         hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="handleSaveLookup"
                  :disabled="saving || !lookupForm.name.trim()"
                  class="px-5 py-2 text-sm font-medium text-white bg-teal rounded-xl
                         hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed
                         flex items-center gap-2"
                >
                  <svg v-if="saving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                  </svg>
                  {{ isEditing ? 'Update' : 'Create' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- ========== Item Create/Edit Modal ========== -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showItemModal" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4" @click.self="showItemModal = false">
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
            appear
          >
            <div v-if="showItemModal" class="bg-white dark:bg-surface-dark rounded-2xl shadow-2xl w-full max-w-md overflow-hidden">
              <!-- Header -->
              <div class="px-6 py-4 bg-gradient-to-r from-navy to-teal relative overflow-hidden">
                <div class="absolute -right-4 -top-4 w-24 h-24 rounded-full bg-white/5"></div>
                <div class="absolute -right-8 -bottom-8 w-32 h-32 rounded-full bg-white/5"></div>
                <div class="relative flex items-center gap-3">
                  <div class="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-white">{{ isEditing ? 'edit_note' : 'playlist_add' }}</span>
                  </div>
                  <div>
                    <h3 class="text-lg font-bold text-white">{{ isEditing ? 'Edit Item' : 'New Item' }}</h3>
                    <p class="text-white/60 text-sm">{{ selectedLookup?.name }}</p>
                  </div>
                </div>
              </div>

              <!-- Body -->
              <div class="p-6 space-y-5">
                <div class="grid grid-cols-2 gap-4">
                  <div class="col-span-2">
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Value</label>
                    <input
                      v-model="itemForm.value"
                      type="text"
                      placeholder="e.g. active, pending"
                      class="w-full px-4 py-2.5 text-sm font-mono border border-gray-200 dark:border-border-dark rounded-xl
                             bg-white dark:bg-background-dark text-gray-900 dark:text-white
                             placeholder-gray-400 dark:placeholder-zinc-500
                             focus:outline-none focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                    />
                  </div>
                  <div class="col-span-2">
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Display Text</label>
                    <input
                      v-model="itemForm.displayText"
                      type="text"
                      placeholder="e.g. Active, Pending Review"
                      class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-xl
                             bg-white dark:bg-background-dark text-gray-900 dark:text-white
                             placeholder-gray-400 dark:placeholder-zinc-500
                             focus:outline-none focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                    />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Language</label>
                    <input
                      v-model="itemForm.language"
                      type="text"
                      placeholder="e.g. en, ar"
                      class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-xl
                             bg-white dark:bg-background-dark text-gray-900 dark:text-white
                             placeholder-gray-400 dark:placeholder-zinc-500
                             focus:outline-none focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                    />
                  </div>
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-zinc-300 mb-1.5">Sort Order</label>
                    <input
                      v-model.number="itemForm.sortOrder"
                      type="number"
                      min="0"
                      class="w-full px-4 py-2.5 text-sm border border-gray-200 dark:border-border-dark rounded-xl
                             bg-white dark:bg-background-dark text-gray-900 dark:text-white
                             focus:outline-none focus:ring-2 focus:ring-teal/30 focus:border-teal transition-all"
                    />
                  </div>
                </div>
              </div>

              <!-- Footer -->
              <div class="px-6 py-4 bg-gray-50 dark:bg-background-dark border-t border-gray-100 dark:border-border-dark flex justify-end gap-3">
                <button
                  @click="showItemModal = false"
                  class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-zinc-300
                         border border-gray-200 dark:border-border-dark rounded-xl
                         hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="handleSaveItem"
                  :disabled="saving || !itemForm.value.trim()"
                  class="px-5 py-2 text-sm font-medium text-white bg-teal rounded-xl
                         hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed
                         flex items-center gap-2"
                >
                  <svg v-if="saving" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                  </svg>
                  {{ isEditing ? 'Update' : 'Add Item' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- ========== Delete Lookup Confirmation ========== -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showDeleteConfirm" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4" @click.self="showDeleteConfirm = false">
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
            appear
          >
            <div v-if="showDeleteConfirm" class="bg-white dark:bg-surface-dark rounded-2xl shadow-2xl w-full max-w-sm overflow-hidden">
              <div class="p-6 text-center">
                <div class="w-14 h-14 mx-auto mb-4 rounded-2xl bg-red-50 dark:bg-red-500/10 flex items-center justify-center">
                  <span class="material-symbols-outlined text-3xl text-red-500">warning</span>
                </div>
                <h3 class="text-lg font-bold text-gray-900 dark:text-white mb-2">Delete Lookup?</h3>
                <p class="text-sm text-gray-500 dark:text-zinc-400">
                  Are you sure you want to delete <strong class="text-gray-700 dark:text-zinc-200">{{ deleteTarget?.name }}</strong>?
                  This will also deactivate all its items.
                </p>
              </div>
              <div class="px-6 py-4 bg-gray-50 dark:bg-background-dark border-t border-gray-100 dark:border-border-dark flex justify-end gap-3">
                <button
                  @click="showDeleteConfirm = false"
                  class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-zinc-300
                         border border-gray-200 dark:border-border-dark rounded-xl
                         hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="handleDeleteLookup"
                  class="px-5 py-2 text-sm font-medium text-white bg-red-500 rounded-xl
                         hover:bg-red-600 transition-colors"
                >
                  Delete
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>

    <!-- ========== Delete Item Confirmation ========== -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showDeleteItemConfirm" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4" @click.self="showDeleteItemConfirm = false">
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
            appear
          >
            <div v-if="showDeleteItemConfirm" class="bg-white dark:bg-surface-dark rounded-2xl shadow-2xl w-full max-w-sm overflow-hidden">
              <div class="p-6 text-center">
                <div class="w-14 h-14 mx-auto mb-4 rounded-2xl bg-red-50 dark:bg-red-500/10 flex items-center justify-center">
                  <span class="material-symbols-outlined text-3xl text-red-500">delete_forever</span>
                </div>
                <h3 class="text-lg font-bold text-gray-900 dark:text-white mb-2">Delete Item?</h3>
                <p class="text-sm text-gray-500 dark:text-zinc-400">
                  Are you sure you want to delete <strong class="text-gray-700 dark:text-zinc-200">{{ deleteItemTarget?.displayText }}</strong>?
                </p>
              </div>
              <div class="px-6 py-4 bg-gray-50 dark:bg-background-dark border-t border-gray-100 dark:border-border-dark flex justify-end gap-3">
                <button
                  @click="showDeleteItemConfirm = false"
                  class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-zinc-300
                         border border-gray-200 dark:border-border-dark rounded-xl
                         hover:bg-gray-100 dark:hover:bg-border-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="handleDeleteItem"
                  class="px-5 py-2 text-sm font-medium text-white bg-red-500 rounded-xl
                         hover:bg-red-600 transition-colors"
                >
                  Delete
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
