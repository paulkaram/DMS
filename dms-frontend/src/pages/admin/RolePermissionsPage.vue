<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { rolePermissionsApi, rolesApi } from '@/api/client'
import type { Role, SystemAction, RolePermissionMatrix } from '@/types'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'
import { UiSelect } from '@/components/ui'

// State
const isLoading = ref(true)
const isSaving = ref<string | null>(null)
const roles = ref<Role[]>([])
const actions = ref<SystemAction[]>([])
const rolePermissions = ref<Map<string, Set<string>>>(new Map())
const categories = ref<string[]>([])
const selectedCategory = ref<string>('all')

// Load data
onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [rolesRes, actionsRes, matrixRes] = await Promise.all([
      rolesApi.getAll(),
      rolePermissionsApi.getAllActions(),
      rolePermissionsApi.getMatrix()
    ])

    roles.value = rolesRes.data
    actions.value = actionsRes.data

    // Build permission map
    const permMap = new Map<string, Set<string>>()
    for (const rolePerm of matrixRes.data as RolePermissionMatrix[]) {
      permMap.set(rolePerm.roleId, new Set(rolePerm.allowedActionCodes))
    }
    rolePermissions.value = permMap

    // Extract categories
    const cats = new Set<string>()
    for (const action of actions.value) {
      cats.add(action.category)
    }
    categories.value = Array.from(cats).sort()
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

// Filtered actions by category
const filteredActions = computed(() => {
  if (selectedCategory.value === 'all') {
    return actions.value
  }
  return actions.value.filter(a => a.category === selectedCategory.value)
})

// Grouped actions by category
const groupedActions = computed(() => {
  const groups: Record<string, SystemAction[]> = {}
  for (const action of filteredActions.value) {
    if (!groups[action.category]) {
      groups[action.category] = []
    }
    groups[action.category].push(action)
  }
  return groups
})

// Check if role has permission
function hasPermission(roleId: string, actionCode: string): boolean {
  return rolePermissions.value.get(roleId)?.has(actionCode) ?? false
}

// Toggle permission
async function togglePermission(roleId: string, actionCode: string) {
  const currentPerms = rolePermissions.value.get(roleId) ?? new Set<string>()
  const newPerms = new Set(currentPerms)

  if (newPerms.has(actionCode)) {
    newPerms.delete(actionCode)
  } else {
    newPerms.add(actionCode)
  }

  // Update local state immediately for responsiveness
  rolePermissions.value.set(roleId, newPerms)

  // Save to server
  isSaving.value = `${roleId}-${actionCode}`
  try {
    const actionIds = actions.value
      .filter(a => newPerms.has(a.code))
      .map(a => a.id)
    await rolePermissionsApi.setRolePermissions(roleId, actionIds)
  } catch (error) {
    // Revert on error
    if (newPerms.has(actionCode)) {
      newPerms.delete(actionCode)
    } else {
      newPerms.add(actionCode)
    }
    rolePermissions.value.set(roleId, newPerms)
  } finally {
    isSaving.value = null
  }
}

// Select/deselect all for a role
async function toggleAllForRole(roleId: string, category: string) {
  const categoryActions = actions.value.filter(a => a.category === category)
  const currentPerms = rolePermissions.value.get(roleId) ?? new Set<string>()
  const allSelected = categoryActions.every(a => currentPerms.has(a.code))

  const newPerms = new Set(currentPerms)
  for (const action of categoryActions) {
    if (allSelected) {
      newPerms.delete(action.code)
    } else {
      newPerms.add(action.code)
    }
  }

  rolePermissions.value.set(roleId, newPerms)

  isSaving.value = `${roleId}-${category}`
  try {
    const actionIds = actions.value
      .filter(a => newPerms.has(a.code))
      .map(a => a.id)
    await rolePermissionsApi.setRolePermissions(roleId, actionIds)
  } catch (error) {
    await loadData() // Reload on error
  } finally {
    isSaving.value = null
  }
}

function getCategoryAllSelected(roleId: string, category: string): boolean {
  const categoryActions = actions.value.filter(a => a.category === category)
  const currentPerms = rolePermissions.value.get(roleId) ?? new Set<string>()
  return categoryActions.every(a => currentPerms.has(a.code))
}

function getCategorySomeSelected(roleId: string, category: string): boolean {
  const categoryActions = actions.value.filter(a => a.category === category)
  const currentPerms = rolePermissions.value.get(roleId) ?? new Set<string>()
  const selectedCount = categoryActions.filter(a => currentPerms.has(a.code)).length
  return selectedCount > 0 && selectedCount < categoryActions.length
}

// Category options for dropdown
const categoryOptions = computed(() => [
  { value: 'all', label: 'All Categories' },
  ...categories.value.map(cat => ({ value: cat, label: cat }))
])
</script>

<template>
  <div class="p-6">
    <div class="max-w-full mx-auto space-y-6">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Role Permissions" icon="security" />

      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Role Permission Matrix</h1>
          <p class="text-zinc-500 dark:text-zinc-400 mt-1">Configure what actions each role can perform</p>
        </div>
        <div class="flex items-center gap-3">
          <!-- Category Filter -->
          <div class="w-56">
            <UiSelect
              v-model="selectedCategory"
              :options="categoryOptions"
              placeholder="All Categories"
            />
          </div>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="bg-white dark:bg-background-dark rounded-xl p-8 text-center">
        <span class="material-symbols-outlined animate-spin text-primary text-3xl">progress_activity</span>
        <p class="text-zinc-500 mt-2">Loading permissions...</p>
      </div>

      <!-- Permission Matrix -->
      <div v-else class="bg-white dark:bg-background-dark rounded-xl shadow-sm border border-zinc-200 dark:border-border-dark overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full">
            <thead class="bg-zinc-50 dark:bg-surface-dark/50 sticky top-0 z-10">
              <tr>
                <th class="text-left py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide min-w-[250px] sticky left-0 bg-zinc-50 dark:bg-surface-dark/50">
                  Action
                </th>
                <th
                  v-for="role in roles"
                  :key="role.id"
                  class="text-center py-3 px-4 text-xs font-semibold text-zinc-500 dark:text-zinc-400 uppercase tracking-wide min-w-[120px]"
                >
                  {{ role.name }}
                </th>
              </tr>
            </thead>
            <tbody>
              <template v-for="(categoryActions, category) in groupedActions" :key="category">
                <!-- Category Header -->
                <tr class="bg-zinc-100 dark:bg-surface-dark">
                  <td
                    class="py-2 px-4 font-semibold text-zinc-700 dark:text-zinc-300 sticky left-0 bg-zinc-100 dark:bg-surface-dark"
                  >
                    <div class="flex items-center gap-2">
                      <span class="material-symbols-outlined text-primary text-lg">folder</span>
                      {{ category }}
                    </div>
                  </td>
                  <td
                    v-for="role in roles"
                    :key="`${category}-${role.id}`"
                    class="text-center py-2 px-4"
                  >
                    <button
                      @click="toggleAllForRole(role.id, category)"
                      class="p-1 rounded hover:bg-zinc-200 dark:hover:bg-border-dark transition-colors"
                      :title="getCategoryAllSelected(role.id, category) ? 'Deselect all' : 'Select all'"
                    >
                      <span
                        v-if="getCategoryAllSelected(role.id, category)"
                        class="material-symbols-outlined text-primary text-lg"
                      >check_box</span>
                      <span
                        v-else-if="getCategorySomeSelected(role.id, category)"
                        class="material-symbols-outlined text-primary/60 text-lg"
                      >indeterminate_check_box</span>
                      <span
                        v-else
                        class="material-symbols-outlined text-zinc-400 text-lg"
                      >check_box_outline_blank</span>
                    </button>
                  </td>
                </tr>
                <!-- Actions -->
                <tr
                  v-for="action in categoryActions"
                  :key="action.id"
                  class="border-b border-zinc-100 dark:border-border-dark hover:bg-zinc-50 dark:hover:bg-surface-dark/30"
                >
                  <td class="py-3 px-4 sticky left-0 bg-white dark:bg-background-dark">
                    <div class="pl-6">
                      <div class="font-medium text-zinc-900 dark:text-white text-sm">{{ action.name }}</div>
                      <div class="text-xs text-zinc-500 dark:text-zinc-400">{{ action.code }}</div>
                    </div>
                  </td>
                  <td
                    v-for="role in roles"
                    :key="`${action.id}-${role.id}`"
                    class="text-center py-3 px-4"
                  >
                    <button
                      @click="togglePermission(role.id, action.code)"
                      :disabled="isSaving !== null"
                      class="p-1.5 rounded-lg transition-all hover:scale-110"
                      :class="[
                        hasPermission(role.id, action.code)
                          ? 'text-primary hover:bg-primary/10'
                          : 'text-zinc-300 dark:text-zinc-600 hover:text-zinc-400 hover:bg-zinc-100 dark:hover:bg-surface-dark'
                      ]"
                    >
                      <span
                        v-if="isSaving === `${role.id}-${action.code}`"
                        class="material-symbols-outlined animate-spin text-xl"
                      >progress_activity</span>
                      <span
                        v-else-if="hasPermission(role.id, action.code)"
                        class="material-symbols-outlined text-xl"
                      >check_circle</span>
                      <span
                        v-else
                        class="material-symbols-outlined text-xl"
                      >circle</span>
                    </button>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>

        <!-- Legend -->
        <div class="px-4 py-3 bg-zinc-50 dark:bg-surface-dark/50 border-t border-zinc-200 dark:border-border-dark">
          <div class="flex items-center gap-6 text-sm">
            <div class="flex items-center gap-2">
              <span class="material-symbols-outlined text-primary">check_circle</span>
              <span class="text-zinc-600 dark:text-zinc-400">Permission granted</span>
            </div>
            <div class="flex items-center gap-2">
              <span class="material-symbols-outlined text-zinc-300 dark:text-zinc-600">circle</span>
              <span class="text-zinc-600 dark:text-zinc-400">Permission denied</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
