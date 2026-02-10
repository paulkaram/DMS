<script setup lang="ts">
import { ref, computed } from 'vue'

const searchQuery = ref('')
const expandedSections = ref<Set<string>>(new Set(['documents', 'config', 'reference', 'users', 'system']))

const adminSections = [
  {
    id: 'documents',
    title: 'Document Management',
    icon: 'description',
    color: 'teal',
    items: [
      { name: 'Workflow Designer', path: '/admin/workflow-designer', icon: 'account_tree', description: 'Design automated document processes' },
      { name: 'Content Type Builder', path: '/admin/content-type-builder', icon: 'build', description: 'Create custom metadata schemas' },
      { name: 'Folder Content Type', path: '/admin/folder-content-types', icon: 'folder_special', description: 'Define folder classification types' },
      { name: 'File Content Type', path: '/settings/content-types', icon: 'insert_drive_file', description: 'Configure document file types' },
      { name: 'Filing Plan', path: '/admin/filing-plans', icon: 'account_tree', description: 'Automatic filing rules & workflows' },
      { name: 'Pattern', path: '/admin/patterns', icon: 'pattern', description: 'Document matching patterns' },
      { name: 'Retention Policies', path: '/admin/retention-policies', icon: 'schedule', description: 'Document lifecycle & archival rules' },
      { name: 'Folder Templates', path: '/admin/folder-templates', icon: 'account_tree', description: 'Reusable folder structure templates' }
    ]
  },
  {
    id: 'config',
    title: 'Configuration',
    icon: 'settings',
    color: 'navy',
    items: [
      { name: 'Search Configuration', path: '/admin/search-config', icon: 'manage_search', description: 'Search indexing & ranking settings' },
      { name: 'Scan Configuration', path: '/admin/scan-config', icon: 'document_scanner', description: 'OCR and scanning options' },
      { name: 'Naming Convention', path: '/admin/naming-convention', icon: 'text_fields', description: 'Document naming rules & patterns' },
      { name: 'Bookmarks', path: '/admin/bookmarks', icon: 'bookmarks', description: 'Template bookmark replacements' },
      { name: 'Export', path: '/admin/export', icon: 'download', description: 'Export settings and history' }
    ]
  },
  {
    id: 'reference',
    title: 'Reference Data',
    icon: 'dataset',
    color: 'purple',
    items: [
      { name: 'Purpose', path: '/admin/purpose', icon: 'flag', description: 'Document purpose classifications' },
      { name: 'Lookup', path: '/admin/lookups', icon: 'list', description: 'Lookup tables and dropdown values' },
      { name: 'Permission Levels', path: '/admin/permission-levels', icon: 'admin_panel_settings', description: 'Access level definitions' }
    ]
  },
  {
    id: 'users',
    title: 'User & Access Management',
    icon: 'group',
    color: 'blue',
    items: [
      { name: 'Users', path: '/admin/users', icon: 'person', description: 'User accounts & profiles' },
      { name: 'Roles', path: '/admin/roles', icon: 'badge', description: 'Role definitions' },
      { name: 'Role Permissions', path: '/admin/role-permissions', icon: 'security', description: 'Role-based action permissions matrix' },
      { name: 'Organizational Structures', path: '/admin/structures', icon: 'account_tree', description: 'Departments, divisions & units' }
    ]
  },
  {
    id: 'system',
    title: 'System',
    icon: 'dns',
    color: 'slate',
    items: [
      { name: 'System Dashboard', path: '/', icon: 'dashboard', description: 'System overview & metrics' },
      { name: 'Activity Report', path: '/activity', icon: 'monitoring', description: 'Audit logs & activity tracking' },
      { name: 'Endpoints', path: '/admin/endpoints', icon: 'api', description: 'External service endpoints' },
      { name: 'Recycle Bin', path: '/recycle-bin', icon: 'delete', description: 'Deleted items recovery' }
    ]
  }
]

// Quick access items for the hero section
const quickAccess = [
  { name: 'Users', path: '/admin/users', icon: 'group', count: '1,208', label: 'Total Users' },
  { name: 'Roles', path: '/admin/roles', icon: 'badge', count: '12', label: 'Active Roles' },
  { name: 'Content Types', path: '/admin/content-type-builder', icon: 'build', count: '8', label: 'Defined Types' },
  { name: 'Retention', path: '/admin/retention-policies', icon: 'schedule', count: '5', label: 'Policies' }
]

const filteredSections = computed(() => {
  if (!searchQuery.value.trim()) return adminSections

  const query = searchQuery.value.toLowerCase()
  return adminSections.map(section => ({
    ...section,
    items: section.items.filter(item =>
      item.name.toLowerCase().includes(query) ||
      item.description.toLowerCase().includes(query)
    )
  })).filter(section => section.items.length > 0)
})

function toggleSection(sectionId: string) {
  if (expandedSections.value.has(sectionId)) {
    expandedSections.value.delete(sectionId)
  } else {
    expandedSections.value.add(sectionId)
  }
  // Trigger reactivity
  expandedSections.value = new Set(expandedSections.value)
}

function isSectionExpanded(sectionId: string): boolean {
  return expandedSections.value.has(sectionId)
}

function getSectionColorClass(color: string) {
  const colors: Record<string, string> = {
    teal: 'bg-teal/10 text-teal border-teal/20',
    navy: 'bg-[#1e3a5f]/10 text-[#1e3a5f] dark:text-blue-400 border-[#1e3a5f]/20',
    purple: 'bg-purple-500/10 text-purple-600 dark:text-purple-400 border-purple-500/20',
    blue: 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20',
    slate: 'bg-zinc-500/10 text-zinc-600 dark:text-zinc-400 border-zinc-500/20'
  }
  return colors[color] || colors.teal
}

function getIconBgClass(color: string) {
  const colors: Record<string, string> = {
    teal: 'bg-teal/10 group-hover:bg-teal/20',
    navy: 'bg-[#1e3a5f]/10 group-hover:bg-[#1e3a5f]/20',
    purple: 'bg-purple-500/10 group-hover:bg-purple-500/20',
    blue: 'bg-blue-500/10 group-hover:bg-blue-500/20',
    slate: 'bg-zinc-500/10 group-hover:bg-zinc-500/20'
  }
  return colors[color] || colors.teal
}

function getIconTextClass(color: string) {
  const colors: Record<string, string> = {
    teal: 'text-teal',
    navy: 'text-[#1e3a5f] dark:text-blue-400',
    purple: 'text-purple-600 dark:text-purple-400',
    blue: 'text-blue-600 dark:text-blue-400',
    slate: 'text-zinc-600 dark:text-zinc-400'
  }
  return colors[color] || colors.teal
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Administration</h1>
        <p class="text-zinc-500 text-sm mt-1">System configuration and management</p>
      </div>

      <!-- Search Bar -->
      <div class="relative w-full lg:w-96">
        <span class="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400">search</span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search settings..."
          class="w-full pl-12 pr-4 py-3 bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
        />
      </div>
    </div>

    <!-- Quick Access Stats -->
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <router-link
        v-for="item in quickAccess"
        :key="item.name"
        :to="item.path"
        class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden group hover:border-teal/30 transition-all"
      >
        <!-- Cyan Wave SVG -->
        <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
          <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
        </svg>
        <div class="relative z-10">
          <div class="flex items-center justify-between mb-3">
            <div class="w-10 h-10 rounded-lg bg-teal/20 flex items-center justify-center">
              <span class="material-symbols-outlined text-teal">{{ item.icon }}</span>
            </div>
            <span class="text-[10px] font-bold uppercase tracking-widest text-zinc-400">{{ item.label }}</span>
          </div>
          <p class="text-3xl font-bold text-white">{{ item.count }}</p>
          <p class="text-xs mt-1.5 font-medium text-zinc-400 group-hover:text-teal transition-colors">Manage {{ item.name }}</p>
        </div>
      </router-link>
    </div>

    <!-- Admin Sections -->
    <div class="space-y-6">
      <div
        v-for="section in filteredSections"
        :key="section.id"
        class="bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark overflow-hidden shadow-sm"
      >
        <!-- Section Header -->
        <button
          @click="toggleSection(section.id)"
          class="w-full px-6 py-4 flex items-center justify-between hover:bg-zinc-50 dark:hover:bg-surface-dark/50 transition-colors"
        >
          <div class="flex items-center gap-4">
            <div
              class="w-10 h-10 rounded-lg flex items-center justify-center border"
              :class="getSectionColorClass(section.color)"
            >
              <span class="material-symbols-outlined" :class="getIconTextClass(section.color)">{{ section.icon }}</span>
            </div>
            <div class="text-left">
              <h2 class="font-bold text-zinc-900 dark:text-white">{{ section.title }}</h2>
              <p class="text-xs text-zinc-500">{{ section.items.length }} settings</p>
            </div>
          </div>
          <div class="flex items-center gap-3">
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest hidden sm:block">
              {{ isSectionExpanded(section.id) ? 'Collapse' : 'Expand' }}
            </span>
            <span
              class="material-symbols-outlined text-zinc-400 transition-transform duration-200"
              :class="{ 'rotate-180': isSectionExpanded(section.id) }"
            >expand_more</span>
          </div>
        </button>

        <!-- Section Content -->
        <div
          v-show="isSectionExpanded(section.id)"
          class="px-6 pb-6"
        >
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            <router-link
              v-for="item in section.items"
              :key="item.path"
              :to="item.path"
              class="group p-4 rounded-lg border border-zinc-200 dark:border-border-dark hover:border-teal dark:hover:border-teal bg-zinc-50/50 dark:bg-surface-dark/30 hover:bg-teal/5 dark:hover:bg-teal/10 transition-all"
            >
              <div class="flex items-start gap-3">
                <div
                  class="w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0 transition-colors"
                  :class="getIconBgClass(section.color)"
                >
                  <span class="material-symbols-outlined" :class="getIconTextClass(section.color)">{{ item.icon }}</span>
                </div>
                <div class="flex-1 min-w-0">
                  <h3 class="font-semibold text-zinc-900 dark:text-white group-hover:text-teal transition-colors text-sm">
                    {{ item.name }}
                  </h3>
                  <p class="text-xs text-zinc-500 mt-1 line-clamp-2">{{ item.description }}</p>
                </div>
                <span class="material-symbols-outlined text-zinc-300 dark:text-zinc-600 group-hover:text-teal group-hover:translate-x-1 transition-all text-lg">
                  arrow_forward
                </span>
              </div>
            </router-link>
          </div>
        </div>
      </div>
    </div>

    <!-- Footer Help -->
    <div class="bg-gradient-to-r from-zinc-100 to-zinc-50 dark:from-zinc-800 dark:to-zinc-900 rounded-lg p-6 border border-zinc-200 dark:border-border-dark">
      <div class="flex flex-col md:flex-row items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 rounded-full bg-teal/10 flex items-center justify-center">
            <span class="material-symbols-outlined text-teal text-xl">help</span>
          </div>
          <div>
            <h3 class="font-semibold text-zinc-900 dark:text-white">Need Help?</h3>
            <p class="text-sm text-zinc-500">Check our documentation for detailed guides and tutorials.</p>
          </div>
        </div>
        <div class="flex items-center gap-3">
          <button class="px-4 py-2 text-sm font-medium text-zinc-600 dark:text-zinc-400 hover:text-teal transition-colors">
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">menu_book</span>
              Documentation
            </span>
          </button>
          <button class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors shadow-sm">
            <span class="flex items-center gap-2">
              <span class="material-symbols-outlined text-lg">support_agent</span>
              Contact Support
            </span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
