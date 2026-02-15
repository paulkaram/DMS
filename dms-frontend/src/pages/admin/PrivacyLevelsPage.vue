<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { privacyLevelsApi } from '@/api/client'
import type { PrivacyLevel } from '@/types'
import { UiModal, UiButton } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const levels = ref<PrivacyLevel[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref({
  id: '',
  name: '',
  level: 0,
  color: '#10b981',
  description: '',
  isActive: true
})

const sortedLevels = computed(() => {
  return [...levels.value].sort((a, b) => a.level - b.level)
})

onMounted(async () => {
  await loadLevels()
})

async function loadLevels() {
  isLoading.value = true
  try {
    const response = await privacyLevelsApi.getAll(true)
    levels.value = response.data
  } catch {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    id: '',
    name: '',
    level: levels.value.length > 0 ? Math.max(...levels.value.map(l => l.level)) + 1 : 0,
    color: '#10b981',
    description: '',
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(pl: PrivacyLevel) {
  formData.value = {
    id: pl.id,
    name: pl.name,
    level: pl.level,
    color: pl.color || '#10b981',
    description: pl.description || '',
    isActive: pl.isActive
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await privacyLevelsApi.update(formData.value.id, {
        name: formData.value.name,
        level: formData.value.level,
        color: formData.value.color || undefined,
        description: formData.value.description || undefined,
        isActive: formData.value.isActive
      })
    } else {
      await privacyLevelsApi.create({
        name: formData.value.name,
        level: formData.value.level,
        color: formData.value.color || undefined,
        description: formData.value.description || undefined
      })
    }
    showModal.value = false
    await loadLevels()
  } catch {
  } finally {
    isSaving.value = false
  }
}

async function deleteLevel(id: string) {
  if (!confirm('Are you sure you want to delete this privacy level?')) return
  try {
    await privacyLevelsApi.delete(id)
    await loadLevels()
  } catch {
  }
}

const presetColors = [
  { name: 'Emerald', value: '#10b981' },
  { name: 'Blue', value: '#3b82f6' },
  { name: 'Amber', value: '#f59e0b' },
  { name: 'Red', value: '#ef4444' },
  { name: 'Purple', value: '#a855f7' },
  { name: 'Teal', value: '#14b8a6' },
  { name: 'Rose', value: '#f43f5e' },
  { name: 'Zinc', value: '#71717a' }
]
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Privacy Levels" icon="shield" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Privacy Levels</h1>
          <p class="text-zinc-500 dark:text-zinc-400 mt-1">Define security classification levels for folder access control</p>
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
            <h3 class="font-semibold text-white mb-1">Security Classification System</h3>
            <p class="text-sm text-zinc-300 leading-relaxed">
              Privacy levels restrict folder visibility based on user clearance. Lower numeric levels are less restrictive.
              Users can only see folders at or below their assigned privacy level. Admins bypass all restrictions.
            </p>
          </div>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <div class="flex items-center gap-3 text-zinc-500 dark:text-zinc-400">
          <div class="w-5 h-5 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
          <span class="text-sm">Loading privacy levels...</span>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else-if="levels.length === 0" class="rounded-lg border border-zinc-200 dark:border-border-dark bg-white dark:bg-zinc-900 p-12 text-center">
        <div class="w-16 h-16 mx-auto mb-4 rounded-lg bg-zinc-100 dark:bg-zinc-800 flex items-center justify-center">
          <span class="material-symbols-outlined text-3xl text-zinc-400">shield</span>
        </div>
        <p class="text-lg font-semibold text-zinc-700 dark:text-zinc-200">No privacy levels configured</p>
        <p class="text-sm text-zinc-500 dark:text-zinc-400 mt-1">Create levels to control folder visibility by security clearance</p>
      </div>

      <!-- Levels Table -->
      <div v-else class="rounded-lg border border-zinc-200 dark:border-border-dark bg-white dark:bg-zinc-900 overflow-hidden">
        <table class="w-full">
          <thead>
            <tr class="border-b border-zinc-200 dark:border-zinc-700/50 bg-zinc-50/50 dark:bg-zinc-800/30">
              <th class="text-left text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Level</th>
              <th class="text-left text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Name</th>
              <th class="text-left text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Description</th>
              <th class="text-center text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Active</th>
              <th class="text-right text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="pl in sortedLevels"
              :key="pl.id"
              class="border-b border-zinc-100 dark:border-zinc-800 last:border-0 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/30 transition-colors"
            >
              <!-- Level Number -->
              <td class="px-5 py-3.5">
                <span class="inline-flex items-center justify-center w-8 h-8 rounded-lg text-sm font-bold text-white" :style="{ backgroundColor: pl.color || '#71717a' }">
                  {{ pl.level }}
                </span>
              </td>

              <!-- Name with color badge -->
              <td class="px-5 py-3.5">
                <div class="flex items-center gap-3">
                  <div
                    class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                    :style="{ backgroundColor: (pl.color || '#71717a') + '20' }"
                  >
                    <span class="material-symbols-outlined" :style="{ color: pl.color || '#71717a', fontSize: '18px' }">shield</span>
                  </div>
                  <div>
                    <p class="text-sm font-semibold text-zinc-900 dark:text-white">{{ pl.name }}</p>
                    <div class="flex items-center gap-1.5 mt-0.5">
                      <span
                        class="w-2.5 h-2.5 rounded-full border border-white/50"
                        :style="{ backgroundColor: pl.color || '#71717a' }"
                      ></span>
                      <span class="text-[10px] text-zinc-400 font-mono">{{ pl.color }}</span>
                    </div>
                  </div>
                </div>
              </td>

              <!-- Description -->
              <td class="px-5 py-3.5">
                <p class="text-xs text-zinc-500 dark:text-zinc-400 max-w-xs truncate">{{ pl.description || '—' }}</p>
              </td>

              <!-- Active -->
              <td class="px-5 py-3.5 text-center">
                <span
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold"
                  :class="pl.isActive
                    ? 'bg-emerald-500/10 text-emerald-500 dark:bg-emerald-500/15 dark:text-emerald-400'
                    : 'bg-zinc-500/10 text-zinc-400 dark:bg-zinc-500/15 dark:text-zinc-500'"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="pl.isActive ? 'bg-emerald-500' : 'bg-zinc-400'"></span>
                  {{ pl.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>

              <!-- Actions -->
              <td class="px-5 py-3.5">
                <div class="flex items-center justify-end gap-1">
                  <button
                    @click="openEditModal(pl)"
                    class="p-1.5 rounded-lg text-zinc-400 hover:text-teal hover:bg-teal/10 transition-colors"
                    title="Edit"
                  >
                    <span class="material-symbols-outlined text-base">edit</span>
                  </button>
                  <button
                    @click="deleteLevel(pl.id)"
                    class="p-1.5 rounded-lg text-zinc-400 hover:text-rose-500 hover:bg-rose-500/10 transition-colors"
                    title="Delete"
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

    <!-- Create/Edit Modal -->
    <UiModal v-model="showModal" size="md">
      <template #header>
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 bg-teal/30 backdrop-blur rounded-lg flex items-center justify-center">
            <span class="material-symbols-outlined text-white text-xl">{{ isEditing ? 'edit' : 'add' }}</span>
          </div>
          <div>
            <h3 class="text-lg font-semibold text-white">{{ isEditing ? 'Edit' : 'Create' }} Privacy Level</h3>
            <p class="text-sm text-zinc-300">Define a security classification level</p>
          </div>
        </div>
      </template>

      <div class="space-y-5">
        <!-- Name -->
        <div>
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Name</label>
          <input
            v-model="formData.name"
            type="text"
            class="w-full px-3 py-2.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
            placeholder="e.g. Confidential"
          />
        </div>

        <!-- Level Number -->
        <div>
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Level (Numeric Rank)</label>
          <input
            v-model.number="formData.level"
            type="number"
            min="0"
            class="w-full px-3 py-2.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
            placeholder="0"
          />
          <p class="text-[10px] text-zinc-400 mt-1">Higher numbers = more restrictive. Users need at least this level to see folders with this classification.</p>
        </div>

        <!-- Color -->
        <div>
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Color</label>
          <div class="flex items-center gap-3 mb-2">
            <input
              v-model="formData.color"
              type="color"
              class="w-10 h-10 rounded-lg border border-zinc-200 dark:border-zinc-700 cursor-pointer p-0.5"
            />
            <input
              v-model="formData.color"
              type="text"
              class="flex-1 px-3 py-2.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white font-mono focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
              placeholder="#10b981"
            />
          </div>
          <div class="flex items-center gap-1.5 flex-wrap">
            <button
              v-for="preset in presetColors"
              :key="preset.value"
              @click="formData.color = preset.value"
              class="w-7 h-7 rounded-lg border-2 transition-all hover:scale-110"
              :class="formData.color === preset.value ? 'border-white shadow-lg scale-110' : 'border-transparent'"
              :style="{ backgroundColor: preset.value }"
              :title="preset.name"
            ></button>
          </div>
        </div>

        <!-- Description -->
        <div>
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Description</label>
          <textarea
            v-model="formData.description"
            rows="2"
            class="w-full px-3 py-2.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all resize-none"
            placeholder="Optional description..."
          ></textarea>
        </div>

        <!-- Active (edit only) -->
        <div v-if="isEditing">
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Status</label>
          <label class="flex items-center gap-2.5 px-3 py-2.5 rounded-lg border border-zinc-200 dark:border-zinc-700 cursor-pointer hover:border-teal/40 transition-colors">
            <button
              type="button"
              @click="formData.isActive = !formData.isActive"
              class="relative w-9 h-5 rounded-full transition-colors duration-200 flex-shrink-0"
              :class="formData.isActive ? 'bg-teal' : 'bg-zinc-300 dark:bg-zinc-600'"
            >
              <span
                class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform duration-200"
                :class="formData.isActive ? 'translate-x-4' : 'translate-x-0'"
              ></span>
            </button>
            <span class="text-sm text-zinc-700 dark:text-zinc-300">{{ formData.isActive ? 'Active' : 'Inactive' }}</span>
          </label>
        </div>

        <!-- Preview -->
        <div>
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Preview</label>
          <div class="px-4 py-3 rounded-lg bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-zinc-700">
            <div class="flex items-center gap-2">
              <span
                class="inline-flex items-center justify-center w-6 h-6 rounded text-[10px] font-bold text-white"
                :style="{ backgroundColor: formData.color }"
              >{{ formData.level }}</span>
              <span class="material-symbols-outlined" :style="{ color: formData.color, fontSize: '16px' }">shield</span>
              <span class="text-sm font-medium" :style="{ color: formData.color }">
                {{ formData.name || 'Level Name' }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="ghost" @click="showModal = false">Cancel</UiButton>
          <UiButton @click="handleSave" :disabled="isSaving || !formData.name.trim()">
            <span class="flex items-center gap-2">
              <span v-if="isSaving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              {{ isEditing ? 'Update' : 'Create' }}
            </span>
          </UiButton>
        </div>
      </template>
    </UiModal>
  </div>
</template>
