<script setup lang="ts">
import { ref, computed } from 'vue'

const searchQuery = ref('')
const expandedSections = ref<Set<string>>(new Set(['governance', 'verification', 'reporting', 'system']))

const sections = [
  {
    id: 'governance',
    title: 'Governance',
    icon: 'shield',
    color: 'teal',
    items: [
      { name: 'Compliance Overview', path: '/admin/compliance', icon: 'verified_user', description: 'Audit trail, retention, and integrity overview' },
      { name: 'Retention Policies', path: '/admin/retention-policies', icon: 'schedule', description: 'Document lifecycle and archival rules' },
      { name: 'Disposal Management', path: '/records/disposal', icon: 'delete_sweep', description: 'Multi-level disposal approval workflow' },
      { name: 'Legal Holds', path: '/admin/legal-holds', icon: 'gavel', description: 'Case-based document preservation' }
    ]
  },
  {
    id: 'verification',
    title: 'Verification',
    icon: 'verified',
    color: 'blue',
    items: [
      { name: 'Integrity Verification', path: '/admin/compliance', icon: 'enhanced_encryption', description: 'SHA-256 hash verification and tamper detection' },
      { name: 'Access Review', path: '/admin/access-review', icon: 'policy', description: 'Periodic access review campaigns' },
      { name: 'Preservation', path: '/admin/preservation', icon: 'lock', description: 'ISO 14721 OAIS format preservation' }
    ]
  },
  {
    id: 'reporting',
    title: 'Reporting',
    icon: 'monitoring',
    color: 'indigo',
    items: [
      { name: 'Audit Trail', path: '/activity', icon: 'history', description: 'Complete activity audit log' },
      { name: 'Reports', path: '/reports', icon: 'bar_chart', description: 'Document statistics and analytics' },
      { name: 'Activity Log', path: '/activity', icon: 'timeline', description: 'Recent system activity' }
    ]
  },
  {
    id: 'system',
    title: 'System',
    icon: 'dns',
    color: 'slate',
    items: [
      { name: 'System Health', path: '/admin/system-health', icon: 'monitor_heart', description: 'Background jobs and system metrics' }
    ]
  }
]

const filteredSections = computed(() => {
  if (!searchQuery.value.trim()) return sections
  const q = searchQuery.value.toLowerCase()
  return sections.map(s => ({
    ...s,
    items: s.items.filter(i => i.name.toLowerCase().includes(q) || i.description.toLowerCase().includes(q))
  })).filter(s => s.items.length > 0)
})

function toggleSection(id: string) {
  if (expandedSections.value.has(id)) {
    expandedSections.value.delete(id)
  } else {
    expandedSections.value.add(id)
  }
  expandedSections.value = new Set(expandedSections.value)
}

function getSectionColorClass(color: string) {
  const map: Record<string, string> = {
    teal: 'bg-teal/10 text-teal border-teal/20',
    blue: 'bg-blue-500/10 text-blue-600 dark:text-blue-400 border-blue-500/20',
    indigo: 'bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border-indigo-500/20',
    slate: 'bg-zinc-500/10 text-zinc-600 dark:text-zinc-400 border-zinc-500/20'
  }
  return map[color] || map.teal
}

function getIconBg(color: string) {
  const map: Record<string, string> = {
    teal: 'bg-teal/10 group-hover:bg-teal/20',
    blue: 'bg-blue-500/10 group-hover:bg-blue-500/20',
    indigo: 'bg-indigo-500/10 group-hover:bg-indigo-500/20',
    slate: 'bg-zinc-500/10 group-hover:bg-zinc-500/20'
  }
  return map[color] || map.teal
}

function getIconText(color: string) {
  const map: Record<string, string> = {
    teal: 'text-teal',
    blue: 'text-blue-600 dark:text-blue-400',
    indigo: 'text-indigo-600 dark:text-indigo-400',
    slate: 'text-zinc-600 dark:text-zinc-400'
  }
  return map[color] || map.teal
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Records Management</h1>
        <p class="text-zinc-500 text-sm mt-1">Governance, compliance, verification, and reporting</p>
      </div>
      <div class="relative w-full lg:w-80">
        <span class="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400">search</span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search records management..."
          class="w-full pl-12 pr-4 py-3 bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
        />
      </div>
    </div>

    <!-- Sections -->
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
              <span class="material-symbols-outlined" :class="getIconText(section.color)">{{ section.icon }}</span>
            </div>
            <div class="text-left">
              <h2 class="font-bold text-zinc-900 dark:text-white">{{ section.title }}</h2>
              <p class="text-xs text-zinc-500">{{ section.items.length }} items</p>
            </div>
          </div>
          <div class="flex items-center gap-3">
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest hidden sm:block">
              {{ expandedSections.has(section.id) ? 'Collapse' : 'Expand' }}
            </span>
            <span
              class="material-symbols-outlined text-zinc-400 transition-transform duration-200"
              :class="{ 'rotate-180': expandedSections.has(section.id) }"
            >expand_more</span>
          </div>
        </button>

        <!-- Section Content -->
        <div v-show="expandedSections.has(section.id)" class="px-6 pb-6">
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
                  :class="getIconBg(section.color)"
                >
                  <span class="material-symbols-outlined" :class="getIconText(section.color)">{{ item.icon }}</span>
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
