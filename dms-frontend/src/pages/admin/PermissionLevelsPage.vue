<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { permissionLevelsApi } from '@/api/client'
import type { PermissionLevelDefinition } from '@/types'
import { UiCheckbox } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const permissionLevels = ref<PermissionLevelDefinition[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<PermissionLevelDefinition>>({
  name: '',
  description: '',
  level: 16,
  color: '#3B82F6',
  icon: '',
  canRead: true,
  canWrite: false,
  canDelete: false,
  canAdmin: false,
  canShare: false,
  canExport: false,
  isSystem: false,
  sortOrder: 0,
  isActive: true
})

const sortedLevels = computed(() => {
  return [...permissionLevels.value].sort((a, b) => a.level - b.level)
})

onMounted(async () => {
  await loadPermissionLevels()
})

async function loadPermissionLevels() {
  isLoading.value = true
  try {
    const response = await permissionLevelsApi.getAll()
    permissionLevels.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  const maxLevel = Math.max(0, ...permissionLevels.value.map(p => p.level))
  formData.value = {
    name: '',
    description: '',
    level: maxLevel * 2 || 16,
    color: '#3B82F6',
    icon: '',
    canRead: true,
    canWrite: false,
    canDelete: false,
    canAdmin: false,
    canShare: false,
    canExport: false,
    isSystem: false,
    sortOrder: permissionLevels.value.length,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(level: PermissionLevelDefinition) {
  formData.value = { ...level }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await permissionLevelsApi.update(formData.value.id, {
        name: formData.value.name!,
        description: formData.value.description,
        level: formData.value.level!,
        color: formData.value.color,
        icon: formData.value.icon,
        canRead: formData.value.canRead,
        canWrite: formData.value.canWrite,
        canDelete: formData.value.canDelete,
        canAdmin: formData.value.canAdmin,
        canShare: formData.value.canShare,
        canExport: formData.value.canExport,
        isSystem: formData.value.isSystem,
        sortOrder: formData.value.sortOrder,
        isActive: formData.value.isActive!
      })
    } else {
      await permissionLevelsApi.create({
        name: formData.value.name!,
        description: formData.value.description,
        level: formData.value.level!,
        color: formData.value.color,
        icon: formData.value.icon,
        canRead: formData.value.canRead,
        canWrite: formData.value.canWrite,
        canDelete: formData.value.canDelete,
        canAdmin: formData.value.canAdmin,
        canShare: formData.value.canShare,
        canExport: formData.value.canExport,
        isSystem: formData.value.isSystem,
        sortOrder: formData.value.sortOrder
      })
    }
    showModal.value = false
    await loadPermissionLevels()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deleteLevel(id: string) {
  const level = permissionLevels.value.find(p => p.id === id)
  if (level?.isSystem) return
  if (!confirm('Are you sure you want to delete this permission level?')) return
  try {
    await permissionLevelsApi.delete(id)
    await loadPermissionLevels()
  } catch (error) {
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Permission Levels" icon="security" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Permission Levels</h1>
          <p class="text-gray-500 mt-1">Configure permission levels and access rights</p>
        </div>
        <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Level
        </button>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4 mb-6">
        <h3 class="font-medium text-gray-900 mb-2">Permission System</h3>
        <p class="text-sm text-gray-500">
          Permissions use a bitmask system. Each level has a value that is a power of 2, allowing them to be combined.
          System levels cannot be modified or deleted.
        </p>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">Loading...</div>
        <div v-else-if="permissionLevels.length === 0" class="p-8 text-center text-gray-500">
          <p class="text-lg font-medium">No permission levels configured</p>
          <p class="text-sm mt-1">Create a permission level to define access rights</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Value</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Description</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Capabilities</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Type</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="level in sortedLevels" :key="level.id" class="border-t border-gray-100 hover:bg-gray-50">
              <td class="py-3 px-4">
                <div class="flex items-center gap-2">
                  <div v-if="level.color" class="w-3 h-3 rounded-full" :style="{ backgroundColor: level.color }"></div>
                  <span class="font-medium text-gray-900">{{ level.name }}</span>
                </div>
              </td>
              <td class="py-3 px-4">
                <code class="px-2 py-1 bg-gray-100 text-sm rounded">{{ level.level }}</code>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ level.description || '-' }}</td>
              <td class="py-3 px-4">
                <div class="flex items-center gap-1 flex-wrap">
                  <span v-if="level.canRead" class="px-2 py-0.5 text-xs bg-blue-100 text-blue-700 rounded">Read</span>
                  <span v-if="level.canWrite" class="px-2 py-0.5 text-xs bg-green-100 text-green-700 rounded">Write</span>
                  <span v-if="level.canDelete" class="px-2 py-0.5 text-xs bg-orange-100 text-orange-700 rounded">Delete</span>
                  <span v-if="level.canAdmin" class="px-2 py-0.5 text-xs bg-purple-100 text-purple-700 rounded">Admin</span>
                  <span v-if="level.canShare" class="px-2 py-0.5 text-xs bg-cyan-100 text-cyan-700 rounded">Share</span>
                  <span v-if="level.canExport" class="px-2 py-0.5 text-xs bg-yellow-100 text-yellow-700 rounded">Export</span>
                </div>
              </td>
              <td class="py-3 px-4">
                <span :class="level.isSystem ? 'bg-gray-100 text-gray-500' : 'bg-blue-100 text-blue-700'" class="px-2 py-1 text-xs rounded-full">
                  {{ level.isSystem ? 'System' : 'Custom' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <template v-if="!level.isSystem">
                  <button @click="openEditModal(level)" class="p-1 text-gray-400 hover:text-teal mr-2">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </button>
                  <button @click="deleteLevel(level.id)" class="p-1 text-gray-400 hover:text-red-600">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </template>
                <span v-else class="text-xs text-gray-400">Locked</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-md mx-4 p-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Permission Level' : 'New Permission Level' }}</h3>
        <div class="space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Name</label>
              <input v-model="formData.name" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Value</label>
              <input v-model.number="formData.level" type="number" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea v-model="formData.description" rows="2" class="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal"></textarea>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Color</label>
            <div class="flex items-center gap-3">
              <input v-model="formData.color" type="color" class="w-12 h-10 border border-gray-300 rounded cursor-pointer" />
              <input v-model="formData.color" type="text" class="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal" />
            </div>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">Capabilities</label>
            <div class="grid grid-cols-3 gap-3">
              <UiCheckbox v-model="formData.canRead" label="Read" />
              <UiCheckbox v-model="formData.canWrite" label="Write" />
              <UiCheckbox v-model="formData.canDelete" label="Delete" />
              <UiCheckbox v-model="formData.canAdmin" label="Admin" />
              <UiCheckbox v-model="formData.canShare" label="Share" />
              <UiCheckbox v-model="formData.canExport" label="Export" />
            </div>
          </div>
          <UiCheckbox v-model="formData.isActive" label="Active" />
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
          <button @click="handleSave" :disabled="isSaving" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 disabled:opacity-50">
            {{ isSaving ? 'Saving...' : 'Save' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
