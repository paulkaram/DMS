<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import type { Principal, User, Role, Structure } from '@/types'
import { usersApi, rolesApi, structuresApi } from '@/api/client'

// For dropdown positioning
const triggerRef = ref<HTMLElement | null>(null)
const dropdownStyle = ref({ top: '0px', left: '0px', width: '0px' })

function updateDropdownPosition() {
  if (triggerRef.value) {
    const rect = triggerRef.value.getBoundingClientRect()
    dropdownStyle.value = {
      top: `${rect.bottom + 8}px`,
      left: `${rect.left}px`,
      width: `${rect.width}px`
    }
  }
}

const props = defineProps<{
  modelValue?: Principal | null
  principalTypes?: ('User' | 'Role' | 'Structure')[]
  placeholder?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [value: Principal | null]
}>()

const isOpen = ref(false)
const searchQuery = ref('')
const selectedType = ref<'User' | 'Role' | 'Structure'>('User')
const isLoading = ref(false)
const hasLoadedData = ref(false)

const users = ref<User[]>([])
const roles = ref<Role[]>([])
const structures = ref<Structure[]>([])

const allowedTypes = computed(() => props.principalTypes || ['User', 'Role', 'Structure'])

// Type definitions - consistent teal/navy theme
const typeConfig = {
  User: {
    icon: 'person',
    label: 'User',
    pluralLabel: 'Users',
    color: 'from-primary to-teal-600',
    bgColor: 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-300',
    selectedBg: 'bg-primary text-white border-primary',
    placeholder: 'Search users...',
    emptyIcon: 'person_off',
    emptyText: 'No users found'
  },
  Role: {
    icon: 'group',
    label: 'Role',
    pluralLabel: 'Roles',
    color: 'from-primary to-teal-600',
    bgColor: 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-300',
    selectedBg: 'bg-primary text-white border-primary',
    placeholder: 'Search roles...',
    emptyIcon: 'group_off',
    emptyText: 'No roles found'
  },
  Structure: {
    icon: 'apartment',
    label: 'Structure',
    pluralLabel: 'Structures',
    color: 'from-primary to-teal-600',
    bgColor: 'bg-white dark:bg-surface-dark border-zinc-200 dark:border-border-dark text-zinc-600 dark:text-zinc-300',
    selectedBg: 'bg-primary text-white border-primary',
    placeholder: 'Search structures...',
    emptyIcon: 'domain_disabled',
    emptyText: 'No structures found'
  }
}

const currentTypeConfig = computed(() => typeConfig[selectedType.value])

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

// Load data on mount and when type changes
watch(selectedType, async () => {
  if (!hasLoadedData.value) {
    await loadData()
  }
}, { immediate: true })

// Update position when dropdown opens
watch(isOpen, (open) => {
  if (open) {
    updateDropdownPosition()
  }
})

async function loadData() {
  if (hasLoadedData.value) return

  isLoading.value = true
  try {
    const promises: Promise<any>[] = []

    if (allowedTypes.value.includes('User')) {
      promises.push(usersApi.getAll().then(r => { const d = r.data; users.value = Array.isArray(d) ? d : d.items ?? [] }))
    }
    if (allowedTypes.value.includes('Role')) {
      promises.push(rolesApi.getAll().then(r => { roles.value = r.data }))
    }
    if (allowedTypes.value.includes('Structure')) {
      promises.push(structuresApi.getAll().then(r => { structures.value = r.data }))
    }

    await Promise.all(promises)
    hasLoadedData.value = true
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

const filteredItems = computed(() => {
  const q = searchQuery.value.toLowerCase()

  if (selectedType.value === 'User') {
    if (!q) return users.value
    return users.value.filter(u =>
      u.displayName?.toLowerCase().includes(q) ||
      u.username?.toLowerCase().includes(q) ||
      u.email?.toLowerCase().includes(q)
    )
  }

  if (selectedType.value === 'Role') {
    if (!q) return roles.value
    return roles.value.filter(r =>
      r.name?.toLowerCase().includes(q) ||
      r.description?.toLowerCase().includes(q)
    )
  }

  if (selectedType.value === 'Structure') {
    if (!q) return structures.value
    return structures.value.filter(s =>
      s.name?.toLowerCase().includes(q) ||
      s.code?.toLowerCase().includes(q) ||
      s.nameAr?.toLowerCase().includes(q)
    )
  }

  return []
})

function selectPrincipal(item: any) {
  const type = selectedType.value
  const principal: Principal = {
    id: item.id,
    type,
    name: type === 'User' ? (item.displayName || item.username) :
          type === 'Role' ? item.name :
          item.name,
    displayName: type === 'User' ? item.displayName : undefined,
    description: type === 'Role' ? item.description : type === 'Structure' ? item.code : undefined,
    icon: type === 'User' ? 'person' : type === 'Role' ? 'group' : 'apartment'
  }
  emit('update:modelValue', principal)
  isOpen.value = false
  searchQuery.value = ''
}

function clearSelection() {
  emit('update:modelValue', null)
}

function selectType(type: 'User' | 'Role' | 'Structure') {
  selectedType.value = type
  searchQuery.value = ''
}
</script>

<template>
  <div class="space-y-3">
    <!-- Type Selector Buttons -->
    <div class="flex gap-2">
      <button
        v-for="type in allowedTypes"
        :key="type"
        type="button"
        @click="selectType(type)"
        class="flex-1 flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg border-2 font-medium text-sm transition-all duration-200"
        :class="selectedType === type
          ? typeConfig[type].selectedBg + ' shadow-lg'
          : typeConfig[type].bgColor + ' hover:shadow-md'"
      >
        <span class="material-symbols-outlined text-lg">{{ typeConfig[type].icon }}</span>
        {{ typeConfig[type].pluralLabel }}
      </button>
    </div>

    <!-- Selection Input -->
    <div class="relative">
      <!-- Selected Value or Input Trigger -->
      <div
        ref="triggerRef"
        @click="isOpen = !isOpen"
        class="w-full px-4 py-3 border-2 rounded-lg bg-white dark:bg-surface-dark cursor-pointer flex items-center justify-between transition-all duration-200"
        :class="isOpen
          ? 'ring-2 ring-primary/30 border-primary shadow-lg'
          : 'border-gray-200 dark:border-gray-600 hover:border-gray-300 dark:hover:border-gray-500'"
      >
        <div v-if="modelValue" class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-lg flex items-center justify-center text-white shadow-md bg-primary">
            <span class="material-symbols-outlined text-lg">{{ typeConfig[modelValue.type].icon }}</span>
          </div>
          <div>
            <div class="font-medium text-gray-900 dark:text-white">{{ modelValue.name }}</div>
            <div class="text-xs text-gray-500 dark:text-gray-400">
              {{ modelValue.description || modelValue.type }}
            </div>
          </div>
        </div>
        <div v-else class="text-gray-400 dark:text-gray-500 flex items-center gap-2">
          <span class="material-symbols-outlined text-lg">{{ currentTypeConfig.icon }}</span>
          Select a {{ currentTypeConfig.label.toLowerCase() }}...
        </div>
        <div class="flex items-center gap-2">
          <button
            v-if="modelValue"
            @click.stop="clearSelection"
            class="p-1.5 hover:bg-gray-100 dark:hover:bg-border-dark rounded-lg transition-colors"
          >
            <span class="material-symbols-outlined text-gray-400 text-lg">close</span>
          </button>
          <span class="material-symbols-outlined text-gray-400 transition-transform duration-200" :class="{ 'rotate-180': isOpen }">
            expand_more
          </span>
        </div>
      </div>

      <!-- Dropdown (Teleported to body) -->
      <Teleport to="body">
        <Transition
          enter-active-class="transition duration-150 ease-out"
          enter-from-class="opacity-0 translate-y-1"
          enter-to-class="opacity-100 translate-y-0"
          leave-active-class="transition duration-100 ease-in"
          leave-from-class="opacity-100 translate-y-0"
          leave-to-class="opacity-0 translate-y-1"
        >
          <div
            v-if="isOpen"
            class="fixed z-[9999] bg-white dark:bg-background-dark rounded-lg shadow-2xl border border-gray-200 dark:border-gray-700 overflow-hidden"
            :style="dropdownStyle"
          >
            <!-- Search -->
            <div class="p-3 border-b border-gray-100 dark:border-gray-800">
              <div class="relative">
                <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">search</span>
                <input
                  v-model="searchQuery"
                  type="text"
                  :placeholder="currentTypeConfig.placeholder"
                  class="w-full pl-10 pr-4 py-2.5 border border-gray-200 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-primary/20 focus:border-primary outline-none bg-white dark:bg-surface-dark text-gray-900 dark:text-white text-sm"
                  @click.stop
                />
              </div>
            </div>

            <!-- Items List -->
            <div class="max-h-64 overflow-y-auto">
              <!-- Loading -->
              <div v-if="isLoading" class="p-8 text-center">
                <div class="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center mx-auto mb-2">
                  <span class="material-symbols-outlined animate-spin text-primary text-xl">progress_activity</span>
                </div>
                <p class="text-sm text-gray-500 dark:text-gray-400">Loading {{ currentTypeConfig.pluralLabel.toLowerCase() }}...</p>
              </div>

              <!-- Empty State -->
              <div v-else-if="filteredItems.length === 0" class="p-8 text-center">
                <span class="material-symbols-outlined text-4xl text-gray-300 dark:text-gray-600 mb-2">{{ currentTypeConfig.emptyIcon }}</span>
                <p class="text-sm text-gray-400 dark:text-gray-500">{{ currentTypeConfig.emptyText }}</p>
              </div>

              <!-- Users List -->
              <template v-else-if="selectedType === 'User'">
                <div
                  v-for="(user, index) in filteredItems as User[]"
                  :key="user.id"
                  @click="selectPrincipal(user)"
                  class="px-4 py-3 hover:bg-primary/5 dark:hover:bg-primary/10 cursor-pointer flex items-center gap-3 transition-colors border-b border-gray-50 dark:border-gray-800 last:border-0"
                >
                  <div
                    class="w-10 h-10 rounded-full flex items-center justify-center text-white font-medium"
                    :class="getAvatarColor(index)"
                  >
                    {{ (user.displayName || user.username || '?').charAt(0).toUpperCase() }}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="font-medium text-gray-900 dark:text-white truncate">{{ user.displayName || user.username }}</div>
                    <div class="text-sm text-gray-500 dark:text-gray-400 truncate">{{ user.email }}</div>
                  </div>
                </div>
              </template>

              <!-- Roles List -->
              <template v-else-if="selectedType === 'Role'">
                <div
                  v-for="(role, index) in filteredItems as Role[]"
                  :key="role.id"
                  @click="selectPrincipal(role)"
                  class="px-4 py-3 hover:bg-primary/5 dark:hover:bg-primary/10 cursor-pointer flex items-center gap-3 transition-colors border-b border-gray-50 dark:border-gray-800 last:border-0"
                >
                  <div
                    class="w-10 h-10 rounded-lg flex items-center justify-center"
                    :class="getAvatarColor(index)"
                  >
                    <span class="material-symbols-outlined text-white">group</span>
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="font-medium text-gray-900 dark:text-white">{{ role.name }}</div>
                    <div v-if="role.description" class="text-sm text-gray-500 dark:text-gray-400 truncate">{{ role.description }}</div>
                  </div>
                </div>
              </template>

              <!-- Structures List -->
              <template v-else-if="selectedType === 'Structure'">
                <div
                  v-for="(structure, index) in filteredItems as Structure[]"
                  :key="structure.id"
                  @click="selectPrincipal(structure)"
                  class="px-4 py-3 hover:bg-primary/5 dark:hover:bg-primary/10 cursor-pointer flex items-center gap-3 transition-colors border-b border-gray-50 dark:border-gray-800 last:border-0"
                >
                  <div
                    class="w-10 h-10 rounded-lg flex items-center justify-center"
                    :class="getAvatarColor(index)"
                  >
                    <span class="material-symbols-outlined text-white">apartment</span>
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="font-medium text-gray-900 dark:text-white">{{ structure.name }}</div>
                    <div class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2">
                      <span class="px-1.5 py-0.5 bg-gray-100 dark:bg-gray-700 rounded text-xs">{{ structure.type }}</span>
                      <span v-if="structure.code" class="truncate">{{ structure.code }}</span>
                    </div>
                  </div>
                  <div class="text-xs text-gray-400 dark:text-gray-500 flex items-center gap-1">
                    <span class="material-symbols-outlined text-xs">people</span>
                    {{ structure.memberCount }}
                  </div>
                </div>
              </template>
            </div>
          </div>
        </Transition>

        <!-- Backdrop -->
        <div
          v-if="isOpen"
          @click="isOpen = false"
          class="fixed inset-0 z-[9998]"
        ></div>
      </Teleport>
    </div>
  </div>
</template>
