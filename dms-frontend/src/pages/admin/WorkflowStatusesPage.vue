<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { workflowStatusesApi } from '@/api/client'
import type { WorkflowStatus } from '@/types'
import { UiModal, UiButton } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const statuses = ref<WorkflowStatus[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref({
  id: '',
  name: '',
  color: '#6366f1',
  icon: '',
  description: '',
  sortOrder: 0,
  isActive: true
})

const sortedStatuses = computed(() => {
  return [...statuses.value].sort((a, b) => a.sortOrder - b.sortOrder)
})

onMounted(async () => {
  await loadStatuses()
})

async function loadStatuses() {
  isLoading.value = true
  try {
    const response = await workflowStatusesApi.getAll(true)
    statuses.value = response.data
  } catch {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    id: '',
    name: '',
    color: '#6366f1',
    icon: '',
    description: '',
    sortOrder: statuses.value.length,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(status: WorkflowStatus) {
  formData.value = {
    id: status.id,
    name: status.name,
    color: status.color,
    icon: status.icon || '',
    description: status.description || '',
    sortOrder: status.sortOrder,
    isActive: status.isActive
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await workflowStatusesApi.update(formData.value.id, {
        name: formData.value.name,
        color: formData.value.color,
        icon: formData.value.icon || undefined,
        description: formData.value.description || undefined,
        sortOrder: formData.value.sortOrder,
        isActive: formData.value.isActive
      })
    } else {
      await workflowStatusesApi.create({
        name: formData.value.name,
        color: formData.value.color,
        icon: formData.value.icon || undefined,
        description: formData.value.description || undefined,
        sortOrder: formData.value.sortOrder
      })
    }
    showModal.value = false
    await loadStatuses()
  } catch {
  } finally {
    isSaving.value = false
  }
}

async function deleteStatus(id: string) {
  if (!confirm('Are you sure you want to delete this workflow status?')) return
  try {
    await workflowStatusesApi.delete(id)
    await loadStatuses()
  } catch {
  }
}

const presetColors = [
  { name: 'Indigo', value: '#6366f1' },
  { name: 'Blue', value: '#3b82f6' },
  { name: 'Teal', value: '#14b8a6' },
  { name: 'Emerald', value: '#10b981' },
  { name: 'Amber', value: '#f59e0b' },
  { name: 'Rose', value: '#f43f5e' },
  { name: 'Purple', value: '#a855f7' },
  { name: 'Zinc', value: '#71717a' }
]
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Workflow Statuses" icon="label" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Workflow Statuses</h1>
          <p class="text-zinc-500 dark:text-zinc-400 mt-1">Define statuses that can be assigned to workflow approval steps</p>
        </div>
        <UiButton @click="openCreateModal">
          <span class="flex items-center gap-2">
            <span class="material-symbols-outlined text-lg">add</span>
            New Status
          </span>
        </UiButton>
      </div>

      <!-- Info Banner -->
      <div class="relative overflow-hidden rounded-lg bg-gradient-to-r from-navy via-navy/95 to-primary p-5 mb-6">
        <div class="absolute top-0 right-0 w-40 h-40 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/3"></div>
        <div class="absolute bottom-0 left-1/3 w-24 h-24 bg-primary/10 rounded-full translate-y-1/2"></div>
        <div class="relative flex items-start gap-4">
          <div class="w-10 h-10 bg-white/10 backdrop-blur rounded-lg flex items-center justify-center flex-shrink-0">
            <span class="material-symbols-outlined text-teal text-xl">label</span>
          </div>
          <div>
            <h3 class="font-semibold text-white mb-1">Workflow Status Labels</h3>
            <p class="text-sm text-zinc-300 leading-relaxed">
              Create custom statuses with colors and icons to visually track where documents are in the approval process.
              Assign statuses to individual workflow steps in the Workflow Designer.
            </p>
          </div>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <div class="flex items-center gap-3 text-zinc-500 dark:text-zinc-400">
          <div class="w-5 h-5 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
          <span class="text-sm">Loading workflow statuses...</span>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else-if="statuses.length === 0" class="rounded-lg border border-zinc-200 dark:border-border-dark bg-white dark:bg-zinc-900 p-12 text-center">
        <div class="w-16 h-16 mx-auto mb-4 rounded-lg bg-zinc-100 dark:bg-zinc-800 flex items-center justify-center">
          <span class="material-symbols-outlined text-3xl text-zinc-400">label</span>
        </div>
        <p class="text-lg font-semibold text-zinc-700 dark:text-zinc-200">No workflow statuses configured</p>
        <p class="text-sm text-zinc-500 dark:text-zinc-400 mt-1">Create statuses to track document progress through workflows</p>
      </div>

      <!-- Statuses Table -->
      <div v-else class="rounded-lg border border-zinc-200 dark:border-border-dark bg-white dark:bg-zinc-900 overflow-hidden">
        <table class="w-full">
          <thead>
            <tr class="border-b border-zinc-200 dark:border-zinc-700/50 bg-zinc-50/50 dark:bg-zinc-800/30">
              <th class="text-left text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Status</th>
              <th class="text-left text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Description</th>
              <th class="text-center text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Order</th>
              <th class="text-center text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Active</th>
              <th class="text-right text-[10px] font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider px-5 py-3">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="status in sortedStatuses"
              :key="status.id"
              class="border-b border-zinc-100 dark:border-zinc-800 last:border-0 hover:bg-zinc-50/50 dark:hover:bg-zinc-800/30 transition-colors"
            >
              <!-- Status (color swatch + icon + name) -->
              <td class="px-5 py-3.5">
                <div class="flex items-center gap-3">
                  <div
                    class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                    :style="{ backgroundColor: status.color + '20' }"
                  >
                    <span
                      v-if="status.icon"
                      class="material-symbols-outlined"
                      :style="{ color: status.color, fontSize: '18px' }"
                    >{{ status.icon }}</span>
                    <span
                      v-else
                      class="w-3 h-3 rounded-full"
                      :style="{ backgroundColor: status.color }"
                    ></span>
                  </div>
                  <div>
                    <p class="text-sm font-semibold text-zinc-900 dark:text-white">{{ status.name }}</p>
                    <div class="flex items-center gap-1.5 mt-0.5">
                      <span
                        class="w-2.5 h-2.5 rounded-full border border-white/50"
                        :style="{ backgroundColor: status.color }"
                      ></span>
                      <span class="text-[10px] text-zinc-400 font-mono">{{ status.color }}</span>
                    </div>
                  </div>
                </div>
              </td>

              <!-- Description -->
              <td class="px-5 py-3.5">
                <p class="text-xs text-zinc-500 dark:text-zinc-400 max-w-xs truncate">{{ status.description || '—' }}</p>
              </td>

              <!-- Sort Order -->
              <td class="px-5 py-3.5 text-center">
                <span class="text-xs font-medium text-zinc-600 dark:text-zinc-300">{{ status.sortOrder }}</span>
              </td>

              <!-- Active -->
              <td class="px-5 py-3.5 text-center">
                <span
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold"
                  :class="status.isActive
                    ? 'bg-emerald-500/10 text-emerald-500 dark:bg-emerald-500/15 dark:text-emerald-400'
                    : 'bg-zinc-500/10 text-zinc-400 dark:bg-zinc-500/15 dark:text-zinc-500'"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="status.isActive ? 'bg-emerald-500' : 'bg-zinc-400'"></span>
                  {{ status.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>

              <!-- Actions -->
              <td class="px-5 py-3.5">
                <div class="flex items-center justify-end gap-1">
                  <button
                    @click="openEditModal(status)"
                    class="p-1.5 rounded-lg text-zinc-400 hover:text-teal hover:bg-teal/10 transition-colors"
                    title="Edit"
                  >
                    <span class="material-symbols-outlined text-base">edit</span>
                  </button>
                  <button
                    @click="deleteStatus(status.id)"
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
            <h3 class="text-lg font-semibold text-white">{{ isEditing ? 'Edit' : 'Create' }} Workflow Status</h3>
            <p class="text-sm text-zinc-300">Define a status label for workflow steps</p>
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
            placeholder="e.g. Under Review"
          />
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
              placeholder="#6366f1"
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

        <!-- Icon -->
        <div>
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Icon (Material Symbol)</label>
          <div class="flex items-center gap-3">
            <div
              class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 border border-zinc-200 dark:border-zinc-700"
              :style="{ backgroundColor: formData.color + '20' }"
            >
              <span
                v-if="formData.icon"
                class="material-symbols-outlined"
                :style="{ color: formData.color, fontSize: '20px' }"
              >{{ formData.icon }}</span>
              <span v-else class="material-symbols-outlined text-zinc-300 dark:text-zinc-600" style="font-size: 20px;">help</span>
            </div>
            <input
              v-model="formData.icon"
              type="text"
              class="flex-1 px-3 py-2.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
              placeholder="e.g. hourglass_top, check_circle, cancel"
            />
          </div>
          <p class="text-[10px] text-zinc-400 mt-1">Use any icon name from Material Symbols</p>
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

        <!-- Sort Order + Active -->
        <div class="flex gap-4">
          <div class="flex-1">
            <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Sort Order</label>
            <input
              v-model.number="formData.sortOrder"
              type="number"
              min="0"
              class="w-full px-3 py-2.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
            />
          </div>
          <div v-if="isEditing" class="flex-1">
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
        </div>

        <!-- Preview -->
        <div>
          <label class="block text-xs font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Preview</label>
          <div class="px-4 py-3 rounded-lg bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-zinc-700">
            <div class="flex items-center gap-2">
              <span
                class="w-2.5 h-2.5 rounded-full"
                :style="{ backgroundColor: formData.color }"
              ></span>
              <span
                v-if="formData.icon"
                class="material-symbols-outlined"
                :style="{ color: formData.color, fontSize: '16px' }"
              >{{ formData.icon }}</span>
              <span class="text-sm font-medium" :style="{ color: formData.color }">
                {{ formData.name || 'Status Name' }}
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
