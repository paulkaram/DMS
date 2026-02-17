<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import type { EnterprisePermission, NodePermissions, PermissionAudit, Principal } from '@/types'
import { PermissionLevels } from '@/types'
import { permissionsApi } from '@/api/client'
import PrincipalSelector from './PrincipalSelector.vue'
import { UiToggle, UiDatePicker } from '@/components/ui'

// Preset permission levels - using available Tailwind colors from config
const permissionPresets = [
  {
    id: 'viewer',
    label: 'Viewer',
    description: 'Can view content only',
    icon: 'visibility',
    color: 'bg-zinc-500',
    bgColor: 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark',
    selectedBg: 'bg-primary/10 dark:bg-primary/20 border-primary ring-2 ring-primary/30',
    level: PermissionLevels.Read
  },
  {
    id: 'contributor',
    label: 'Contributor',
    description: 'Can view and edit',
    icon: 'edit_note',
    color: 'bg-primary',
    bgColor: 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark',
    selectedBg: 'bg-primary/10 dark:bg-primary/20 border-primary ring-2 ring-primary/30',
    level: PermissionLevels.Read | PermissionLevels.Write
  },
  {
    id: 'editor',
    label: 'Editor',
    description: 'Can view, edit & delete',
    icon: 'edit_document',
    color: 'bg-zinc-700',
    bgColor: 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark',
    selectedBg: 'bg-primary/10 dark:bg-primary/20 border-primary ring-2 ring-primary/30',
    level: PermissionLevels.Read | PermissionLevels.Write | PermissionLevels.Delete
  },
  {
    id: 'admin',
    label: 'Administrator',
    description: 'Full access & manage',
    icon: 'admin_panel_settings',
    color: 'bg-navy',
    bgColor: 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark',
    selectedBg: 'bg-primary/10 dark:bg-primary/20 border-primary ring-2 ring-primary/30',
    level: PermissionLevels.Read | PermissionLevels.Write | PermissionLevels.Delete | PermissionLevels.Admin
  }
]

const props = defineProps<{
  show: boolean
  nodeType: 'Cabinet' | 'Folder' | 'Document'
  nodeId: string
  nodeName: string
}>()

const emit = defineEmits<{
  close: []
  updated: []
}>()

// State
const activeTab = ref<'permissions' | 'audit'>('permissions')
const isLoading = ref(false)
const isSaving = ref(false)
const error = ref<string | null>(null)
const successMessage = ref<string | null>(null)

// Permissions data
const nodePermissions = ref<NodePermissions | null>(null)
const auditLogs = ref<PermissionAudit[]>([])

// Add permission form
const showAddForm = ref(false)
const selectedPrincipal = ref<Principal | null>(null)
const selectedPreset = ref('viewer')
const includeChildStructures = ref(true)
const expiresAt = ref<string | null>(null)
const grantedReason = ref('')
const showAdvancedOptions = ref(false)

// Edit permission
const editingPermission = ref<EnterprisePermission | null>(null)
const editSelectedPreset = ref('viewer')
const editIncludeChildStructures = ref(true)
const editExpiresAt = ref<string | null>(null)
const editGrantedReason = ref('')

// Break inheritance confirmation
const showBreakInheritanceDialog = ref(false)
const copyPermissionsOnBreak = ref(true)

// Get permission level from selected preset
const computedNewPermissionLevel = computed(() => {
  const preset = permissionPresets.find(p => p.id === selectedPreset.value)
  return preset?.level || PermissionLevels.Read
})

const computedEditPermissionLevel = computed(() => {
  const preset = permissionPresets.find(p => p.id === editSelectedPreset.value)
  return preset?.level || PermissionLevels.Read
})

// Convert permission level to preset id
function levelToPresetId(level: number): string {
  // Find exact match first
  const exactMatch = permissionPresets.find(p => p.level === level)
  if (exactMatch) return exactMatch.id

  // Otherwise find the closest preset that covers the permissions
  if (level >= (PermissionLevels.Read | PermissionLevels.Write | PermissionLevels.Delete | PermissionLevels.Admin)) return 'admin'
  if (level >= (PermissionLevels.Read | PermissionLevels.Write | PermissionLevels.Delete)) return 'editor'
  if (level >= (PermissionLevels.Read | PermissionLevels.Write)) return 'contributor'
  return 'viewer'
}

// Load data when modal opens
watch(() => props.show, async (show) => {
  if (show) {
    await loadPermissions()
  }
}, { immediate: true })

async function loadPermissions() {
  isLoading.value = true
  error.value = null
  try {
    const response = await permissionsApi.getNodePermissions(props.nodeType, props.nodeId)
    nodePermissions.value = response.data
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to load permissions'
  } finally {
    isLoading.value = false
  }
}

async function loadAuditLogs() {
  if (auditLogs.value.length > 0) return
  try {
    const response = await permissionsApi.getNodeAudit(props.nodeType, props.nodeId)
    auditLogs.value = response.data
  } catch (err) {
  }
}

function handleTabChange(tab: 'permissions' | 'audit') {
  activeTab.value = tab
  if (tab === 'audit') {
    loadAuditLogs()
  }
}

function showSuccessMessage(message: string) {
  successMessage.value = message
  setTimeout(() => { successMessage.value = null }, 3000)
}

// Permission level helpers - consistent teal/navy theme
function getPermissionLevelInfo(level: number) {
  if (level >= (PermissionLevels.Read | PermissionLevels.Write | PermissionLevels.Delete | PermissionLevels.Admin)) {
    return { label: 'Administrator', icon: 'admin_panel_settings', color: 'bg-navy/20 text-navy dark:text-zinc-300 border-navy/30' }
  }
  if (level >= (PermissionLevels.Read | PermissionLevels.Write | PermissionLevels.Delete)) {
    return { label: 'Editor', icon: 'edit_document', color: 'bg-teal-600/20 text-teal-700 dark:text-teal-400 border-teal-600/30' }
  }
  if (level >= (PermissionLevels.Read | PermissionLevels.Write)) {
    return { label: 'Contributor', icon: 'edit_note', color: 'bg-primary/20 text-primary border-primary/30' }
  }
  return { label: 'Viewer', icon: 'visibility', color: 'bg-zinc-500/20 text-zinc-600 dark:text-zinc-400 border-zinc-500/30' }
}

function getPrincipalIcon(type: string) {
  switch (type) {
    case 'User': return 'person'
    case 'Role': return 'group'
    case 'Structure': return 'apartment'
    default: return 'help'
  }
}

// Alternating avatar colors matching the theme
const avatarColors = [
  'bg-primary',
  'bg-navy',
  'bg-zinc-600',
  'bg-primary/80',
  'bg-zinc-700'
]

function getAvatarColor(index: number): string {
  return avatarColors[index % avatarColors.length]
}

function getNodeIcon(type: string) {
  switch (type) {
    case 'Cabinet': return 'inventory_2'
    case 'Folder': return 'folder'
    case 'Document': return 'description'
    default: return 'article'
  }
}

// Inheritance management
function handleInheritanceClick() {
  if (!nodePermissions.value) return

  if (nodePermissions.value.breakInheritance) {
    // Restoring - just do it directly
    performInheritanceChange()
  } else {
    // Breaking - show confirmation dialog
    copyPermissionsOnBreak.value = true
    showBreakInheritanceDialog.value = true
  }
}

async function performInheritanceChange() {
  if (!nodePermissions.value) return

  isSaving.value = true
  showBreakInheritanceDialog.value = false
  try {
    if (nodePermissions.value.breakInheritance) {
      await permissionsApi.restoreInheritance(props.nodeType, props.nodeId)
      showSuccessMessage('Inheritance restored successfully')
    } else {
      await permissionsApi.breakInheritance(props.nodeType, props.nodeId, copyPermissionsOnBreak.value)
      showSuccessMessage(copyPermissionsOnBreak.value
        ? 'Inheritance broken - inherited permissions copied'
        : 'Inheritance broken - permissions cleared')
    }
    await loadPermissions()
    emit('updated')
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to update inheritance'
  } finally {
    isSaving.value = false
  }
}

// Add permission
function openAddForm() {
  selectedPrincipal.value = null
  selectedPreset.value = 'viewer'
  includeChildStructures.value = true
  expiresAt.value = null
  grantedReason.value = ''
  showAdvancedOptions.value = false
  showAddForm.value = true
}

async function addPermission() {
  if (!selectedPrincipal.value) return

  isSaving.value = true
  error.value = null
  try {
    await permissionsApi.grantPermission({
      nodeType: props.nodeType,
      nodeId: props.nodeId,
      principalType: selectedPrincipal.value.type,
      principalId: selectedPrincipal.value.id,
      permissionLevel: computedNewPermissionLevel.value,
      includeChildStructures: includeChildStructures.value,
      expiresAt: expiresAt.value || undefined,
      grantedReason: grantedReason.value || undefined
    })

    showAddForm.value = false
    showSuccessMessage('Permission granted successfully')
    await loadPermissions()
    emit('updated')
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to add permission'
  } finally {
    isSaving.value = false
  }
}

// Edit permission
function startEditPermission(permission: EnterprisePermission) {
  editingPermission.value = permission
  editSelectedPreset.value = levelToPresetId(permission.permissionLevel)
  editIncludeChildStructures.value = permission.includeChildStructures
  editExpiresAt.value = permission.expiresAt ? permission.expiresAt.split('T')[0] : null
  editGrantedReason.value = permission.grantedReason || ''
}

async function saveEditPermission() {
  if (!editingPermission.value) return

  isSaving.value = true
  error.value = null
  try {
    await permissionsApi.updatePermission(editingPermission.value.id, {
      permissionLevel: computedEditPermissionLevel.value,
      includeChildStructures: editIncludeChildStructures.value,
      expiresAt: editExpiresAt.value || undefined,
      grantedReason: editGrantedReason.value || undefined
    })

    editingPermission.value = null
    showSuccessMessage('Permission updated successfully')
    await loadPermissions()
    emit('updated')
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to update permission'
  } finally {
    isSaving.value = false
  }
}

// Revoke permission
async function revokePermission(permission: EnterprisePermission) {
  if (!confirm(`Are you sure you want to revoke ${permission.principalName}'s permission?`)) return

  try {
    await permissionsApi.revokePermission(permission.id)
    showSuccessMessage('Permission revoked successfully')
    await loadPermissions()
    emit('updated')
  } catch (err: any) {
    error.value = err.response?.data?.errors?.[0] || 'Failed to revoke permission'
  }
}

function formatDate(dateString: string) {
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function formatDateShort(dateString: string) {
  return new Date(dateString).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
}

// Computed
const directPermissions = computed(() =>
  nodePermissions.value?.permissions.filter(p => !p.isInherited) || []
)

const inheritedPermissions = computed(() =>
  nodePermissions.value?.permissions.filter(p => p.isInherited) || []
)
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="show" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
        <Transition
          enter-active-class="duration-300 ease-out"
          enter-from-class="opacity-0 scale-95 translate-y-4"
          enter-to-class="opacity-100 scale-100 translate-y-0"
          leave-active-class="duration-200 ease-in"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col ring-1 ring-black/5 dark:ring-white/10">
            <!-- Header - Dark gradient like FolderContentTypeModal -->
            <div class="relative bg-gradient-to-r from-navy via-navy/95 to-primary p-5 overflow-hidden">
              <div class="absolute top-0 right-0 w-48 h-48 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
              <div class="absolute bottom-0 left-0 w-32 h-32 bg-cyan-500/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>

              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-4">
                  <div class="w-12 h-12 bg-primary/30 backdrop-blur rounded-lg flex items-center justify-center ring-1 ring-white/20">
                    <span class="material-symbols-outlined text-white text-2xl">admin_panel_settings</span>
                  </div>
                  <div>
                    <h3 class="text-lg font-bold text-white">Manage Permissions</h3>
                    <p class="text-sm text-white/70 flex items-center gap-1.5">
                      <span class="material-symbols-outlined text-sm">{{ getNodeIcon(nodeType) }}</span>
                      {{ nodeName }}
                    </p>
                  </div>
                </div>
                <button
                  @click="emit('close')"
                  class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors"
                >
                  <span class="material-symbols-outlined text-white">close</span>
                </button>
              </div>
            </div>

            <!-- Tabs - Styled like FolderContentTypeModal -->
            <div class="px-5 border-b border-gray-200 dark:border-gray-700/50 flex gap-1 bg-gray-50 dark:bg-surface-dark/50">
              <button
                @click="handleTabChange('permissions')"
                class="px-4 py-3 text-sm font-medium transition-colors relative flex items-center gap-2"
                :class="activeTab === 'permissions' ? 'text-primary' : 'text-gray-500 hover:text-gray-700 dark:hover:text-gray-300'"
              >
                <span class="material-symbols-outlined text-lg">shield</span>
                Permissions
                <span
                  v-if="activeTab === 'permissions'"
                  class="absolute bottom-0 left-0 right-0 h-0.5 bg-gradient-to-r from-primary to-cyan-400 rounded-full"
                ></span>
              </button>
              <button
                @click="handleTabChange('audit')"
                class="px-4 py-3 text-sm font-medium transition-colors relative flex items-center gap-2"
                :class="activeTab === 'audit' ? 'text-primary' : 'text-gray-500 hover:text-gray-700 dark:hover:text-gray-300'"
              >
                <span class="material-symbols-outlined text-lg">history</span>
                Audit Trail
                <span
                  v-if="activeTab === 'audit'"
                  class="absolute bottom-0 left-0 right-0 h-0.5 bg-gradient-to-r from-primary to-cyan-400 rounded-full"
                ></span>
              </button>
            </div>

            <!-- Content -->
            <div class="flex-1 overflow-y-auto p-5">
              <!-- Messages -->
              <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
                <div v-if="error" class="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-lg flex items-center gap-2">
                  <span class="material-symbols-outlined text-red-500">error</span>
                  <p class="text-sm text-red-600 dark:text-red-400 flex-1">{{ error }}</p>
                  <button @click="error = null" class="p-1 hover:bg-red-100 dark:hover:bg-red-900/30 rounded-lg">
                    <span class="material-symbols-outlined text-red-400 text-sm">close</span>
                  </button>
                </div>
              </Transition>

              <Transition enter-active-class="duration-300 ease-out" enter-from-class="opacity-0 -translate-y-2">
                <div v-if="successMessage" class="mb-4 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800/50 rounded-lg flex items-center gap-2">
                  <span class="material-symbols-outlined text-green-500">check_circle</span>
                  <p class="text-sm text-green-600 dark:text-green-400">{{ successMessage }}</p>
                </div>
              </Transition>

              <!-- Loading -->
              <div v-if="isLoading" class="flex flex-col items-center justify-center py-12">
                <div class="w-12 h-12 rounded-lg bg-primary/10 flex items-center justify-center mb-3">
                  <span class="material-symbols-outlined animate-spin text-primary text-2xl">progress_activity</span>
                </div>
                <p class="text-sm text-gray-500">Loading permissions...</p>
              </div>

              <!-- Permissions Tab -->
              <div v-else-if="activeTab === 'permissions'" class="space-y-6">
                <!-- Inheritance Control -->
                <div
                  v-if="nodeType !== 'Document'"
                  class="p-4 rounded-lg border"
                  :class="nodePermissions?.breakInheritance
                    ? 'bg-zinc-50 dark:bg-surface-dark/50 border-zinc-200 dark:border-border-dark'
                    : 'bg-primary/5 dark:bg-primary/10 border-primary/20 dark:border-primary/30'"
                >
                  <div class="flex items-center justify-between">
                    <div class="flex items-center gap-4">
                      <div
                        class="w-12 h-12 rounded-lg flex items-center justify-center"
                        :class="nodePermissions?.breakInheritance
                          ? 'bg-zinc-200 dark:bg-border-dark'
                          : 'bg-primary/20 dark:bg-primary/30'"
                      >
                        <span
                          class="material-symbols-outlined text-2xl"
                          :class="nodePermissions?.breakInheritance ? 'text-zinc-600 dark:text-zinc-400' : 'text-primary'"
                        >
                          {{ nodePermissions?.breakInheritance ? 'link_off' : 'link' }}
                        </span>
                      </div>
                      <div>
                        <div class="font-semibold" :class="nodePermissions?.breakInheritance ? 'text-zinc-700 dark:text-zinc-300' : 'text-primary'">
                          {{ nodePermissions?.breakInheritance ? 'Inheritance Broken' : 'Inheriting Permissions' }}
                        </div>
                        <div class="text-sm text-zinc-500 dark:text-zinc-400">
                          {{ nodePermissions?.breakInheritance
                            ? 'This node has unique permissions separate from its parent.'
                            : 'Permissions are inherited from the parent node.' }}
                        </div>
                      </div>
                    </div>
                    <button
                      @click="handleInheritanceClick"
                      :disabled="isSaving"
                      class="px-4 py-2.5 rounded-lg font-medium transition-all flex items-center gap-2 border-2 border-primary text-primary hover:bg-primary hover:text-white"
                    >
                      <span class="material-symbols-outlined text-lg">{{ nodePermissions?.breakInheritance ? 'link' : 'link_off' }}</span>
                      {{ nodePermissions?.breakInheritance ? 'Restore Inheritance' : 'Break Inheritance' }}
                    </button>
                  </div>
                </div>

                <!-- Add Permission Button/Form -->
                <div v-if="!showAddForm" class="flex justify-end">
                  <button
                    @click="openAddForm"
                    class="flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-navy to-primary text-white rounded-lg hover:shadow-lg hover:shadow-primary/25 transition-all font-medium"
                  >
                    <span class="material-symbols-outlined">add</span>
                    Add Permission
                  </button>
                </div>

                <!-- Add Permission Form -->
                <Transition
                  enter-active-class="duration-200 ease-out"
                  enter-from-class="opacity-0 -translate-y-2"
                  enter-to-class="opacity-100 translate-y-0"
                  leave-active-class="duration-150 ease-in"
                  leave-from-class="opacity-100"
                  leave-to-class="opacity-0"
                >
                  <div v-if="showAddForm" class="p-5 bg-zinc-50 dark:bg-surface-dark/50 rounded-lg border border-zinc-200 dark:border-border-dark space-y-5">
                    <div class="flex items-center justify-between">
                      <h4 class="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
                        <span class="material-symbols-outlined text-primary">add_circle</span>
                        Add New Permission
                      </h4>
                      <button @click="showAddForm = false" class="p-1.5 hover:bg-zinc-200 dark:hover:bg-border-dark rounded-lg transition-colors">
                        <span class="material-symbols-outlined text-gray-400">close</span>
                      </button>
                    </div>

                    <!-- Principal Selector -->
                    <div>
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Grant To</label>
                      <PrincipalSelector v-model="selectedPrincipal" />
                    </div>

                    <!-- Permission Level Presets -->
                    <div>
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Permission Level</label>
                      <div class="grid grid-cols-2 md:grid-cols-4 gap-3">
                        <button
                          v-for="preset in permissionPresets"
                          :key="preset.id"
                          type="button"
                          @click="selectedPreset = preset.id"
                          class="relative p-4 rounded-lg border-2 transition-all duration-200 text-left group hover:shadow-md"
                          :class="selectedPreset === preset.id ? preset.selectedBg : preset.bgColor + ' hover:border-gray-300 dark:hover:border-gray-600'"
                        >
                          <!-- Selected indicator -->
                          <div
                            v-if="selectedPreset === preset.id"
                            class="absolute top-2 right-2 w-5 h-5 rounded-full bg-primary flex items-center justify-center"
                          >
                            <span class="material-symbols-outlined text-white text-sm">check</span>
                          </div>

                          <!-- Icon -->
                          <div
                            class="w-10 h-10 rounded-lg flex items-center justify-center mb-3 shadow-lg transition-transform group-hover:scale-105"
                            :class="preset.color"
                          >
                            <span class="material-symbols-outlined text-white">{{ preset.icon }}</span>
                          </div>

                          <!-- Label & Description -->
                          <div class="font-semibold text-gray-900 dark:text-white text-sm">{{ preset.label }}</div>
                          <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ preset.description }}</div>
                        </button>
                      </div>
                    </div>

                    <!-- Advanced Options Toggle -->
                    <div>
                      <button
                        type="button"
                        @click="showAdvancedOptions = !showAdvancedOptions"
                        class="flex items-center gap-2 text-sm text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
                      >
                        <span class="material-symbols-outlined text-lg transition-transform" :class="{ 'rotate-180': showAdvancedOptions }">expand_more</span>
                        Advanced options
                      </button>

                      <Transition
                        enter-active-class="duration-200 ease-out"
                        enter-from-class="opacity-0 -translate-y-2"
                        leave-active-class="duration-150 ease-in"
                        leave-to-class="opacity-0 -translate-y-2"
                      >
                        <div v-if="showAdvancedOptions" class="mt-4 grid grid-cols-1 md:grid-cols-2 gap-4">
                          <!-- Expires At -->
                          <div>
                            <UiDatePicker
                              v-model="expiresAt"
                              label="Expires At (Optional)"
                              placeholder="No expiration"
                              :min-date="new Date().toISOString().split('T')[0]"
                              clearable
                            />
                          </div>

                          <!-- Reason -->
                          <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Reason (Optional)</label>
                            <input
                              v-model="grantedReason"
                              type="text"
                              placeholder="Why is this permission being granted?"
                              class="w-full px-4 py-2.5 border border-gray-200 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                            />
                          </div>

                          <!-- Include Child Structures -->
                          <div v-if="selectedPrincipal?.type === 'Structure'" class="md:col-span-2">
                            <UiToggle
                              v-model="includeChildStructures"
                              label="Include child structures"
                              description="Members of child structures will also receive this permission"
                            />
                          </div>
                        </div>
                      </Transition>
                    </div>

                    <div class="flex justify-end gap-3 pt-2 border-t border-zinc-200 dark:border-border-dark">
                      <button
                        @click="showAddForm = false"
                        class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg transition-colors font-medium"
                      >
                        Cancel
                      </button>
                      <button
                        @click="addPermission"
                        :disabled="!selectedPrincipal || isSaving"
                        class="px-5 py-2 bg-gradient-to-r from-navy to-primary text-white rounded-lg hover:shadow-lg hover:shadow-primary/25 disabled:opacity-50 transition-all font-medium flex items-center gap-2"
                      >
                        <span v-if="isSaving" class="material-symbols-outlined animate-spin text-sm">progress_activity</span>
                        {{ isSaving ? 'Saving...' : 'Add Permission' }}
                      </button>
                    </div>
                  </div>
                </Transition>

                <!-- Direct Permissions -->
                <div>
                  <h4 class="text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2 mb-3">
                    <span class="material-symbols-outlined text-primary text-lg">shield</span>
                    Direct Permissions
                    <span class="px-2 py-0.5 bg-primary/10 text-primary text-xs rounded-full">{{ directPermissions.length }}</span>
                  </h4>

                  <div v-if="directPermissions.length === 0" class="text-center py-8 bg-gray-50 dark:bg-surface-dark/50 rounded-lg border border-dashed border-gray-300 dark:border-gray-700">
                    <span class="material-symbols-outlined text-4xl text-gray-400 mb-2">shield</span>
                    <p class="text-sm text-gray-500 dark:text-gray-400">No direct permissions configured</p>
                    <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">Click "Add Permission" to grant access</p>
                  </div>

                  <div v-else class="space-y-2">
                    <div
                      v-for="(perm, index) in directPermissions"
                      :key="perm.id"
                      class="p-4 bg-white dark:bg-surface-dark border border-gray-100 dark:border-gray-700 rounded-lg hover:border-primary/30 dark:hover:border-primary/50 transition-all hover:shadow-md"
                    >
                      <!-- Edit Mode -->
                      <div v-if="editingPermission?.id === perm.id" class="space-y-4">
                        <!-- Permission Level Presets (compact) -->
                        <div>
                          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Permission Level</label>
                          <div class="grid grid-cols-4 gap-2">
                            <button
                              v-for="preset in permissionPresets"
                              :key="preset.id"
                              type="button"
                              @click="editSelectedPreset = preset.id"
                              class="relative p-2.5 rounded-lg border-2 transition-all duration-200 text-center group hover:shadow-sm"
                              :class="editSelectedPreset === preset.id ? preset.selectedBg : preset.bgColor + ' hover:border-gray-300 dark:hover:border-gray-600'"
                            >
                              <div
                                class="w-8 h-8 mx-auto rounded-lg flex items-center justify-center mb-1.5 shadow"
                                :class="preset.color"
                              >
                                <span class="material-symbols-outlined text-white text-sm">{{ preset.icon }}</span>
                              </div>
                              <div class="font-medium text-gray-900 dark:text-white text-xs">{{ preset.label }}</div>
                            </button>
                          </div>
                        </div>

                        <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                          <div>
                            <UiDatePicker
                              v-model="editExpiresAt"
                              label="Expires At"
                              placeholder="No expiration"
                              :min-date="new Date().toISOString().split('T')[0]"
                              clearable
                              size="sm"
                            />
                          </div>
                          <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Reason</label>
                            <input
                              v-model="editGrantedReason"
                              type="text"
                              class="w-full px-3 py-2 border border-gray-200 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none text-sm bg-white dark:bg-surface-dark text-gray-900 dark:text-white"
                            />
                          </div>
                        </div>

                        <div class="flex justify-end gap-2 pt-2 border-t border-gray-100 dark:border-gray-700">
                          <button
                            @click="editingPermission = null"
                            class="px-3 py-1.5 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg text-sm font-medium"
                          >
                            Cancel
                          </button>
                          <button
                            @click="saveEditPermission"
                            :disabled="isSaving"
                            class="px-4 py-1.5 bg-gradient-to-r from-navy to-primary text-white rounded-lg hover:shadow-lg text-sm font-medium disabled:opacity-50"
                          >
                            Save
                          </button>
                        </div>
                      </div>

                      <!-- View Mode -->
                      <div v-else class="flex items-center gap-4">
                        <div :class="['w-11 h-11 rounded-lg flex items-center justify-center text-white shadow-lg', getAvatarColor(index)]">
                          <span class="material-symbols-outlined">{{ getPrincipalIcon(perm.principalType) }}</span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <div class="font-medium text-gray-900 dark:text-white">{{ perm.principalName }}</div>
                          <div class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2 flex-wrap">
                            <span class="px-2 py-0.5 bg-gray-100 dark:bg-gray-700 rounded text-xs">{{ perm.principalType }}</span>
                            <span v-if="perm.expiresAt" class="text-amber-600 dark:text-amber-400 flex items-center gap-1">
                              <span class="material-symbols-outlined text-xs">schedule</span>
                              Expires {{ formatDateShort(perm.expiresAt) }}
                            </span>
                          </div>
                        </div>
                        <div :class="['px-3 py-1.5 rounded-lg text-sm font-medium flex items-center gap-1.5 border', getPermissionLevelInfo(perm.permissionLevel).color]">
                          <span class="material-symbols-outlined text-sm">{{ getPermissionLevelInfo(perm.permissionLevel).icon }}</span>
                          {{ getPermissionLevelInfo(perm.permissionLevel).label }}
                        </div>
                        <div class="flex items-center gap-1">
                          <button
                            @click="startEditPermission(perm)"
                            class="p-2 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg transition-colors"
                            title="Edit"
                          >
                            <span class="material-symbols-outlined text-gray-400 hover:text-primary">edit</span>
                          </button>
                          <button
                            @click="revokePermission(perm)"
                            class="p-2 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                            title="Revoke"
                          >
                            <span class="material-symbols-outlined text-gray-400 hover:text-red-500">delete</span>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Inherited Permissions -->
                <div v-if="inheritedPermissions.length > 0">
                  <h4 class="text-sm font-semibold text-gray-900 dark:text-white flex items-center gap-2 mb-3">
                    <span class="material-symbols-outlined text-primary text-lg">subdirectory_arrow_right</span>
                    Inherited Permissions
                    <span class="px-2 py-0.5 bg-primary/10 text-primary text-xs rounded-full">{{ inheritedPermissions.length }}</span>
                  </h4>

                  <div class="space-y-2">
                    <div
                      v-for="(perm, index) in inheritedPermissions"
                      :key="perm.id"
                      class="p-4 bg-primary/5 dark:bg-primary/10 border border-primary/20 dark:border-primary/30 rounded-lg"
                    >
                      <div class="flex items-center gap-4">
                        <div :class="['w-11 h-11 rounded-lg flex items-center justify-center text-white shadow-lg opacity-80', getAvatarColor(index)]">
                          <span class="material-symbols-outlined">{{ getPrincipalIcon(perm.principalType) }}</span>
                        </div>
                        <div class="flex-1 min-w-0">
                          <div class="font-medium text-gray-700 dark:text-gray-300">{{ perm.principalName }}</div>
                          <div class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2 flex-wrap">
                            <span class="px-2 py-0.5 bg-primary/10 dark:bg-primary/20 text-primary rounded text-xs flex items-center gap-1">
                              <span class="material-symbols-outlined text-xs">link</span>
                              Inherited
                            </span>
                            <span v-if="perm.inheritedFromNodeName" class="text-xs text-gray-400 flex items-center gap-1">
                              <span class="material-symbols-outlined text-xs">{{ getNodeIcon(perm.inheritedFromNodeType || '') }}</span>
                              from {{ perm.inheritedFromNodeName }}
                            </span>
                          </div>
                        </div>
                        <div :class="['px-3 py-1.5 rounded-lg text-sm font-medium flex items-center gap-1.5 border opacity-80', getPermissionLevelInfo(perm.permissionLevel).color]">
                          <span class="material-symbols-outlined text-sm">{{ getPermissionLevelInfo(perm.permissionLevel).icon }}</span>
                          {{ getPermissionLevelInfo(perm.permissionLevel).label }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Audit Tab -->
              <div v-else-if="activeTab === 'audit'" class="space-y-4">
                <div v-if="auditLogs.length === 0" class="text-center py-12">
                  <div class="w-16 h-16 rounded-lg bg-gray-100 dark:bg-surface-dark flex items-center justify-center mx-auto mb-4">
                    <span class="material-symbols-outlined text-4xl text-gray-400">history</span>
                  </div>
                  <p class="text-sm font-medium text-gray-700 dark:text-gray-300">No Audit Records</p>
                  <p class="text-xs text-gray-500 mt-1">Permission changes will be logged here</p>
                </div>

                <div v-else class="space-y-3">
                  <div
                    v-for="log in auditLogs"
                    :key="log.id"
                    class="p-4 bg-white dark:bg-surface-dark border border-gray-100 dark:border-gray-700 rounded-lg hover:shadow-md transition-shadow"
                  >
                    <div class="flex items-start gap-4">
                      <div
                        class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0"
                        :class="{
                          'bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400': log.action === 'Grant',
                          'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400': log.action === 'Revoke',
                          'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400': log.action === 'Update',
                          'bg-amber-100 dark:bg-amber-900/30 text-amber-600 dark:text-amber-400': log.action === 'BreakInheritance',
                          'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400': log.action === 'RestoreInheritance',
                          'bg-zinc-100 dark:bg-border-dark text-zinc-600 dark:text-zinc-400': !['Grant', 'Revoke', 'Update', 'BreakInheritance', 'RestoreInheritance'].includes(log.action)
                        }"
                      >
                        <span class="material-symbols-outlined">
                          {{ log.action === 'Grant' ? 'add_circle' :
                             log.action === 'Revoke' ? 'remove_circle' :
                             log.action === 'Update' ? 'edit' :
                             log.action === 'BreakInheritance' ? 'link_off' :
                             log.action === 'RestoreInheritance' ? 'link' : 'info' }}
                        </span>
                      </div>
                      <div class="flex-1 min-w-0">
                        <div class="font-medium text-gray-900 dark:text-white">
                          {{ log.action }} permission
                          <span v-if="log.principalName" class="text-gray-600 dark:text-gray-400"> for {{ log.principalName }}</span>
                        </div>
                        <div class="text-sm text-gray-500 dark:text-gray-400 mt-1">
                          <span v-if="log.newLevel !== null && log.newLevel !== undefined" :class="['px-2 py-0.5 rounded text-xs', getPermissionLevelInfo(log.newLevel).color]">
                            {{ getPermissionLevelInfo(log.newLevel).label }}
                          </span>
                          <span v-if="log.oldLevel !== null && log.newLevel !== null" class="text-gray-400"> (was {{ getPermissionLevelInfo(log.oldLevel || 0).label }})</span>
                        </div>
                        <div v-if="log.reason" class="text-sm text-gray-500 dark:text-gray-400 mt-1 italic">
                          "{{ log.reason }}"
                        </div>
                        <div class="text-xs text-gray-400 dark:text-gray-500 mt-2 flex items-center gap-2">
                          <span class="material-symbols-outlined text-xs">person</span>
                          {{ log.performedByName }}
                          <span class="material-symbols-outlined text-xs">schedule</span>
                          {{ formatDate(log.performedAt) }}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Footer -->
            <div class="border-t border-gray-200 dark:border-gray-700/50 p-4 bg-gray-50 dark:bg-surface-dark/50">
              <div class="flex justify-end">
                <button
                  @click="emit('close')"
                  class="px-5 py-2.5 text-sm font-medium bg-gradient-to-r from-navy to-primary text-white rounded-lg hover:shadow-lg hover:shadow-primary/25 transition-all"
                >
                  Done
                </button>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>

    <!-- Break Inheritance Confirmation Dialog -->
    <Transition
      enter-active-class="duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div v-if="showBreakInheritanceDialog" class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-[60] p-4" @click.self="showBreakInheritanceDialog = false">
        <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-md overflow-hidden ring-1 ring-black/5 dark:ring-white/10">
          <!-- Header -->
          <div class="p-5 bg-gradient-to-r from-amber-500 to-orange-500">
            <div class="flex items-center gap-3">
              <div class="w-10 h-10 bg-white/20 rounded-lg flex items-center justify-center">
                <span class="material-symbols-outlined text-white text-xl">link_off</span>
              </div>
              <div>
                <h3 class="text-lg font-bold text-white">Break Permission Inheritance</h3>
                <p class="text-sm text-white/80">Configure how inheritance will be broken</p>
              </div>
            </div>
          </div>

          <!-- Content -->
          <div class="p-5 space-y-4">
            <p class="text-sm text-gray-600 dark:text-gray-400">
              Breaking inheritance will stop this {{ nodeType.toLowerCase() }} from receiving permissions from its parent.
            </p>

            <div class="space-y-3">
              <label class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-all"
                :class="copyPermissionsOnBreak
                  ? 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-300 dark:border-emerald-700'
                  : 'bg-white dark:bg-surface-dark border-gray-200 dark:border-gray-700 hover:border-gray-300'"
              >
                <input type="radio" :value="true" v-model="copyPermissionsOnBreak"
                  class="mt-0.5 w-4 h-4 text-emerald-600 focus:ring-emerald-500" />
                <div>
                  <div class="font-medium text-gray-900 dark:text-white flex items-center gap-2">
                    <span class="material-symbols-outlined text-emerald-600 text-lg">content_copy</span>
                    Copy inherited permissions
                  </div>
                  <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                    Current inherited permissions will be copied as direct permissions. Users will keep their current access.
                  </div>
                </div>
              </label>

              <label class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-all"
                :class="!copyPermissionsOnBreak
                  ? 'bg-red-50 dark:bg-red-900/20 border-red-300 dark:border-red-700'
                  : 'bg-white dark:bg-surface-dark border-gray-200 dark:border-gray-700 hover:border-gray-300'"
              >
                <input type="radio" :value="false" v-model="copyPermissionsOnBreak"
                  class="mt-0.5 w-4 h-4 text-red-600 focus:ring-red-500" />
                <div>
                  <div class="font-medium text-gray-900 dark:text-white flex items-center gap-2">
                    <span class="material-symbols-outlined text-red-600 text-lg">delete_sweep</span>
                    Start with no permissions
                  </div>
                  <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                    No permissions will be copied. You'll need to manually add permissions for users to access this {{ nodeType.toLowerCase() }}.
                  </div>
                </div>
              </label>
            </div>

            <div v-if="!copyPermissionsOnBreak" class="p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/50 rounded-lg">
              <div class="flex items-start gap-2">
                <span class="material-symbols-outlined text-amber-600 text-lg">warning</span>
                <p class="text-xs text-amber-700 dark:text-amber-400">
                  <strong>Warning:</strong> Users may lose access to this {{ nodeType.toLowerCase() }} until you add new permissions.
                </p>
              </div>
            </div>
          </div>

          <!-- Footer -->
          <div class="p-4 bg-gray-50 dark:bg-surface-dark/50 border-t border-gray-200 dark:border-gray-700 flex justify-end gap-3">
            <button
              @click="showBreakInheritanceDialog = false"
              class="px-4 py-2 text-sm font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              @click="performInheritanceChange"
              :disabled="isSaving"
              class="px-4 py-2 text-sm font-medium bg-gradient-to-r from-amber-500 to-orange-500 text-white rounded-lg hover:shadow-lg transition-all disabled:opacity-50 flex items-center gap-2"
            >
              <span v-if="isSaving" class="material-symbols-outlined animate-spin text-sm">progress_activity</span>
              Break Inheritance
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
