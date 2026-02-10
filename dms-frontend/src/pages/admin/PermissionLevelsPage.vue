<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { permissionLevelsApi } from '@/api/client'
import type { PermissionLevelDefinition } from '@/types'
import { UiCheckbox, UiModal, UiButton } from '@/components/ui'
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

const capabilityConfig: Record<string, { key: keyof PermissionLevelDefinition; label: string; bg: string; text: string; darkBg: string; darkText: string }> = {
  canRead:   { key: 'canRead',   label: 'Read',   bg: 'bg-blue-500/10',   text: 'text-blue-400',   darkBg: 'dark:bg-blue-500/15',   darkText: 'dark:text-blue-300' },
  canWrite:  { key: 'canWrite',  label: 'Write',  bg: 'bg-emerald-500/10',text: 'text-emerald-400',darkBg: 'dark:bg-emerald-500/15',darkText: 'dark:text-emerald-300' },
  canDelete: { key: 'canDelete', label: 'Delete', bg: 'bg-amber-500/10',  text: 'text-amber-400',  darkBg: 'dark:bg-amber-500/15',  darkText: 'dark:text-amber-300' },
  canAdmin:  { key: 'canAdmin',  label: 'Admin',  bg: 'bg-violet-500/10', text: 'text-violet-400', darkBg: 'dark:bg-violet-500/15', darkText: 'dark:text-violet-300' },
  canShare:  { key: 'canShare',  label: 'Share',  bg: 'bg-cyan-500/10',   text: 'text-cyan-400',   darkBg: 'dark:bg-cyan-500/15',   darkText: 'dark:text-cyan-300' },
  canExport: { key: 'canExport', label: 'Export', bg: 'bg-rose-500/10',   text: 'text-rose-400',   darkBg: 'dark:bg-rose-500/15',   darkText: 'dark:text-rose-300' },
}

function getCapabilities(level: PermissionLevelDefinition) {
  return Object.values(capabilityConfig).filter(c => level[c.key])
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Permission Levels" icon="security" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Permission Levels</h1>
          <p class="text-zinc-500 dark:text-zinc-400 mt-1">Configure permission levels and access rights</p>
        </div>
        <UiButton @click="openCreateModal">
          <span class="flex items-center gap-2">
            <span class="material-symbols-outlined text-lg">add</span>
            New Level
          </span>
        </UiButton>
      </div>

      <!-- Info Banner -->
      <div class="relative overflow-hidden rounded-lg bg-gradient-to-r from-navy via-navy/95 to-primary p-5 mb-6">
        <div class="absolute top-0 right-0 w-40 h-40 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/3"></div>
        <div class="absolute bottom-0 left-1/3 w-24 h-24 bg-primary/10 rounded-full translate-y-1/2"></div>
        <div class="relative flex items-start gap-4">
          <div class="w-10 h-10 bg-white/10 backdrop-blur rounded-lg flex items-center justify-center flex-shrink-0">
            <span class="material-symbols-outlined text-teal text-xl">shield</span>
          </div>
          <div>
            <h3 class="font-semibold text-white mb-1">Permission System</h3>
            <p class="text-sm text-zinc-300 leading-relaxed">
              Permissions use a bitmask system. Each level has a value that is a power of 2, allowing them to be combined.
              System levels cannot be modified or deleted.
            </p>
          </div>
        </div>
      </div>

      <!-- Permission Cards Grid -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <div class="flex items-center gap-3 text-zinc-500 dark:text-zinc-400">
          <div class="w-5 h-5 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
          <span class="text-sm">Loading permission levels...</span>
        </div>
      </div>

      <div v-else-if="permissionLevels.length === 0" class="rounded-lg border border-zinc-200 dark:border-border-dark bg-white dark:bg-zinc-900 p-12 text-center">
        <div class="w-16 h-16 mx-auto mb-4 rounded-lg bg-zinc-100 dark:bg-zinc-800 flex items-center justify-center">
          <span class="material-symbols-outlined text-3xl text-zinc-400">shield</span>
        </div>
        <p class="text-lg font-semibold text-zinc-700 dark:text-zinc-200">No permission levels configured</p>
        <p class="text-sm text-zinc-500 dark:text-zinc-400 mt-1">Create a permission level to define access rights</p>
      </div>

      <div v-else class="grid gap-4">
        <!-- Column Headers -->
        <div class="grid grid-cols-12 gap-4 px-5 text-[11px] font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500">
          <div class="col-span-3">Level</div>
          <div class="col-span-3">Description</div>
          <div class="col-span-3">Capabilities</div>
          <div class="col-span-1 text-center">Value</div>
          <div class="col-span-1 text-center">Type</div>
          <div class="col-span-1 text-right">Actions</div>
        </div>

        <!-- Level Rows -->
        <div
          v-for="level in sortedLevels"
          :key="level.id"
          class="group grid grid-cols-12 gap-4 items-center rounded-lg border px-5 py-4 transition-all duration-200"
          :class="[
            'bg-white dark:bg-zinc-900/80',
            'border-zinc-200 dark:border-border-dark/60',
            'hover:border-teal/30 dark:hover:border-teal/20',
            'hover:shadow-md hover:shadow-teal/5'
          ]"
        >
          <!-- Name with color indicator -->
          <div class="col-span-3 flex items-center gap-3">
            <div
              class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 shadow-sm"
              :style="{ backgroundColor: (level.color || '#6B7280') + '18' }"
            >
              <span
                class="material-symbols-outlined text-xl"
                :style="{ color: level.color || '#6B7280' }"
              >{{ level.icon || 'shield' }}</span>
            </div>
            <div>
              <p class="font-semibold text-zinc-800 dark:text-zinc-100 text-sm">{{ level.name }}</p>
              <p v-if="!level.isActive" class="text-[10px] text-red-400 font-medium mt-0.5">Inactive</p>
            </div>
          </div>

          <!-- Description -->
          <div class="col-span-3">
            <p class="text-sm text-zinc-500 dark:text-zinc-400 leading-snug">{{ level.description || '-' }}</p>
          </div>

          <!-- Capabilities -->
          <div class="col-span-3">
            <div class="flex items-center gap-1.5 flex-wrap">
              <span
                v-for="cap in getCapabilities(level)"
                :key="cap.label"
                class="px-2 py-0.5 text-[11px] font-semibold rounded-md"
                :class="[cap.bg, cap.text, cap.darkBg, cap.darkText]"
              >{{ cap.label }}</span>
              <span v-if="getCapabilities(level).length === 0" class="text-xs text-zinc-400">None</span>
            </div>
          </div>

          <!-- Value -->
          <div class="col-span-1 text-center">
            <code class="px-2.5 py-1 bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-300 text-xs font-mono font-bold rounded-lg">{{ level.level }}</code>
          </div>

          <!-- Type badge -->
          <div class="col-span-1 text-center">
            <span
              v-if="level.isSystem"
              class="inline-flex items-center gap-1 px-2 py-0.5 text-[11px] font-semibold rounded-full bg-zinc-100 dark:bg-zinc-800 text-zinc-500 dark:text-zinc-400"
            >
              <span class="material-symbols-outlined" style="font-size: 12px;">lock</span>
              System
            </span>
            <span
              v-else
              class="inline-flex items-center gap-1 px-2 py-0.5 text-[11px] font-semibold rounded-full bg-teal/10 text-teal dark:bg-teal/15"
            >
              Custom
            </span>
          </div>

          <!-- Actions -->
          <div class="col-span-1 flex items-center justify-end gap-1">
            <template v-if="!level.isSystem">
              <button
                @click="openEditModal(level)"
                class="w-8 h-8 flex items-center justify-center rounded-lg text-zinc-400 hover:text-teal hover:bg-teal/10 transition-colors"
                title="Edit"
              >
                <span class="material-symbols-outlined text-[18px]">edit</span>
              </button>
              <button
                @click="deleteLevel(level.id)"
                class="w-8 h-8 flex items-center justify-center rounded-lg text-zinc-400 hover:text-red-500 hover:bg-red-500/10 transition-colors"
                title="Delete"
              >
                <span class="material-symbols-outlined text-[18px]">delete</span>
              </button>
            </template>
            <span v-else class="text-[11px] text-zinc-400 dark:text-zinc-500 font-medium">Locked</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal -->
    <UiModal v-model="showModal" :title="isEditing ? 'Edit Permission Level' : 'New Permission Level'" size="sm">
      <div class="space-y-4">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Name</label>
            <input v-model="formData.name" type="text" class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100" />
          </div>
          <div>
            <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Value</label>
            <input v-model.number="formData.level" type="number" class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Description</label>
          <textarea v-model="formData.description" rows="2" class="w-full px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100"></textarea>
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Color</label>
          <div class="flex items-center gap-3">
            <input v-model="formData.color" type="color" class="w-12 h-10 border border-zinc-300 dark:border-border-dark rounded-lg cursor-pointer bg-white dark:bg-surface-dark" />
            <input v-model="formData.color" type="text" class="flex-1 px-4 py-2 border border-zinc-300 dark:border-border-dark rounded-lg focus:ring-2 focus:ring-teal/50 focus:border-teal bg-white dark:bg-surface-dark text-zinc-900 dark:text-zinc-100" />
          </div>
        </div>
        <div>
          <label class="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">Capabilities</label>
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
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showModal = false">Cancel</UiButton>
          <UiButton :loading="isSaving" @click="handleSave">
            {{ isSaving ? 'Saving...' : 'Save' }}
          </UiButton>
        </div>
      </template>
    </UiModal>
  </div>
</template>
