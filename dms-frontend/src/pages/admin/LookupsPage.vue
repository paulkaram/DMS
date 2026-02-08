<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { referenceDataApi } from '@/api/client'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

interface Lookup {
  id: string
  name: string
  description?: string
  items: LookupItem[]
}

interface LookupItem {
  id: string
  value: string
  displayText: string
  sortOrder: number
}

const lookups = ref<Lookup[]>([
  { id: '1', name: 'DocumentStatus', description: 'Document status values', items: [
    { id: '1', value: 'draft', displayText: 'Draft', sortOrder: 1 },
    { id: '2', value: 'pending', displayText: 'Pending Review', sortOrder: 2 },
    { id: '3', value: 'approved', displayText: 'Approved', sortOrder: 3 },
    { id: '4', value: 'archived', displayText: 'Archived', sortOrder: 4 }
  ]},
  { id: '2', name: 'Priority', description: 'Priority levels', items: [
    { id: '5', value: 'low', displayText: 'Low', sortOrder: 1 },
    { id: '6', value: 'medium', displayText: 'Medium', sortOrder: 2 },
    { id: '7', value: 'high', displayText: 'High', sortOrder: 3 },
    { id: '8', value: 'urgent', displayText: 'Urgent', sortOrder: 4 }
  ]}
])

const selectedLookup = ref<Lookup | null>(null)
const showLookupModal = ref(false)
const showItemModal = ref(false)
const isEditing = ref(false)
const lookupForm = ref({ id: '', name: '', description: '' })
const itemForm = ref({ id: '', value: '', displayText: '', sortOrder: 0 })

function selectLookup(lookup: Lookup) {
  selectedLookup.value = lookup
}

function openCreateLookupModal() {
  lookupForm.value = { id: '', name: '', description: '' }
  isEditing.value = false
  showLookupModal.value = true
}

function openEditLookupModal(lookup: Lookup) {
  lookupForm.value = { id: lookup.id, name: lookup.name, description: lookup.description || '' }
  isEditing.value = true
  showLookupModal.value = true
}

function handleSaveLookup() {
  if (isEditing.value) {
    const idx = lookups.value.findIndex(l => l.id === lookupForm.value.id)
    if (idx !== -1) {
      lookups.value[idx].name = lookupForm.value.name
      lookups.value[idx].description = lookupForm.value.description
    }
  } else {
    lookups.value.push({ ...lookupForm.value, id: Date.now().toString(), items: [] })
  }
  showLookupModal.value = false
}

function deleteLookup(id: string) {
  lookups.value = lookups.value.filter(l => l.id !== id)
  if (selectedLookup.value?.id === id) selectedLookup.value = null
}

function openCreateItemModal() {
  if (!selectedLookup.value) return
  const maxOrder = Math.max(0, ...selectedLookup.value.items.map(i => i.sortOrder))
  itemForm.value = { id: '', value: '', displayText: '', sortOrder: maxOrder + 1 }
  isEditing.value = false
  showItemModal.value = true
}

function openEditItemModal(item: LookupItem) {
  itemForm.value = { ...item }
  isEditing.value = true
  showItemModal.value = true
}

function handleSaveItem() {
  if (!selectedLookup.value) return
  if (isEditing.value) {
    const idx = selectedLookup.value.items.findIndex(i => i.id === itemForm.value.id)
    if (idx !== -1) selectedLookup.value.items[idx] = { ...itemForm.value }
  } else {
    selectedLookup.value.items.push({ ...itemForm.value, id: Date.now().toString() })
  }
  showItemModal.value = false
}

function deleteItem(id: string) {
  if (!selectedLookup.value) return
  selectedLookup.value.items = selectedLookup.value.items.filter(i => i.id !== id)
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Lookups" icon="list" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Lookups</h1>
          <p class="text-gray-500 mt-1">Manage lookup tables and their values</p>
        </div>
        <button @click="openCreateLookupModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Lookup
        </button>
      </div>

      <div class="grid grid-cols-3 gap-6">
        <!-- Lookups List -->
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <div class="p-4 border-b border-gray-200">
            <h3 class="font-medium text-gray-900">Lookup Tables</h3>
          </div>
          <div class="divide-y divide-gray-100">
            <div
              v-for="lookup in lookups"
              :key="lookup.id"
              @click="selectLookup(lookup)"
              class="p-4 cursor-pointer hover:bg-gray-50 transition-colors"
              :class="{ 'bg-blue-50': selectedLookup?.id === lookup.id }"
            >
              <div class="flex items-center justify-between">
                <div>
                  <p class="font-medium text-gray-900">{{ lookup.name }}</p>
                  <p class="text-xs text-gray-500">{{ lookup.items.length }} items</p>
                </div>
                <div class="flex items-center gap-1">
                  <button @click.stop="openEditLookupModal(lookup)" class="p-1 text-gray-400 hover:text-teal">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button @click.stop="deleteLookup(lookup.id)" class="p-1 text-gray-400 hover:text-red-600">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Lookup Items -->
        <div class="col-span-2 bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <div class="p-4 border-b border-gray-200 flex items-center justify-between">
            <h3 class="font-medium text-gray-900">
              {{ selectedLookup ? `Items in "${selectedLookup.name}"` : 'Select a lookup' }}
            </h3>
            <button
              v-if="selectedLookup"
              @click="openCreateItemModal"
              class="flex items-center gap-1 px-3 py-1 text-sm bg-teal text-white rounded-lg hover:bg-teal/90"
            >
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Add Item
            </button>
          </div>

          <div v-if="!selectedLookup" class="p-8 text-center text-gray-500">
            Select a lookup table to view its items
          </div>

          <table v-else-if="selectedLookup.items.length > 0" class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Order</th>
                <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Value</th>
                <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Display Text</th>
                <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in selectedLookup.items.sort((a, b) => a.sortOrder - b.sortOrder)" :key="item.id" class="border-t border-gray-100">
                <td class="py-3 px-4 text-sm text-gray-500">{{ item.sortOrder }}</td>
                <td class="py-3 px-4">
                  <code class="px-2 py-1 bg-gray-100 text-sm rounded">{{ item.value }}</code>
                </td>
                <td class="py-3 px-4 text-sm text-gray-700">{{ item.displayText }}</td>
                <td class="py-3 px-4 text-right">
                  <button @click="openEditItemModal(item)" class="p-1 text-gray-400 hover:text-teal mr-2">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button @click="deleteItem(item.id)" class="p-1 text-gray-400 hover:text-red-600">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>

          <div v-else class="p-8 text-center text-gray-500">
            No items in this lookup table
          </div>
        </div>
      </div>
    </div>

    <!-- Lookup Modal -->
    <div v-if="showLookupModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4 p-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Lookup' : 'New Lookup' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input v-model="lookupForm.name" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea v-model="lookupForm.description" rows="2" class="w-full px-4 py-2 border border-gray-300 rounded-lg"></textarea>
          </div>
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showLookupModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
          <button @click="handleSaveLookup" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">Save</button>
        </div>
      </div>
    </div>

    <!-- Item Modal -->
    <div v-if="showItemModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4 p-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Item' : 'New Item' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Value</label>
            <input v-model="itemForm.value" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Display Text</label>
            <input v-model="itemForm.displayText" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Sort Order</label>
            <input v-model.number="itemForm.sortOrder" type="number" class="w-full px-4 py-2 border border-gray-300 rounded-lg" />
          </div>
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showItemModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
          <button @click="handleSaveItem" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">Save</button>
        </div>
      </div>
    </div>
  </div>
</template>
