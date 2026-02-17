<script setup lang="ts">
import { ref, computed } from 'vue'

const searchQuery = ref('')

const sections = [
  {
    id: 'personal',
    title: 'Personal',
    items: [
      { name: 'Favorites', path: '/favorites', icon: 'star', color: 'amber', description: 'Starred documents and folders' },
      { name: 'My Files', path: '/my-files', icon: 'folder_special', color: 'teal', description: 'Documents you created or own' },
      { name: 'Shared With Me', path: '/shared-with-me', icon: 'folder_shared', color: 'blue', description: 'Items others have shared with you' },
      { name: 'My Shared Items', path: '/my-shared-items', icon: 'share', color: 'indigo', description: 'Items you shared with others' },
      { name: 'Recycle Bin', path: '/recycle-bin', icon: 'delete', color: 'rose', description: 'Recently deleted items' },
      { name: 'Approvals', path: '/approvals', icon: 'task_alt', color: 'emerald', description: 'Pending approval requests' }
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

function getIconBg(color: string) {
  const map: Record<string, string> = {
    amber: 'bg-amber-500/10 group-hover:bg-amber-500/20',
    teal: 'bg-teal/10 group-hover:bg-teal/20',
    blue: 'bg-blue-500/10 group-hover:bg-blue-500/20',
    indigo: 'bg-indigo-500/10 group-hover:bg-indigo-500/20',
    rose: 'bg-rose-500/10 group-hover:bg-rose-500/20',
    emerald: 'bg-emerald-500/10 group-hover:bg-emerald-500/20'
  }
  return map[color] || map.teal
}

function getIconText(color: string) {
  const map: Record<string, string> = {
    amber: 'text-amber-500',
    teal: 'text-teal',
    blue: 'text-blue-500',
    indigo: 'text-indigo-500',
    rose: 'text-rose-500',
    emerald: 'text-emerald-500'
  }
  return map[color] || map.teal
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">My Workspace</h1>
        <p class="text-zinc-500 text-sm mt-1">Your personal files, shares, and approvals</p>
      </div>
      <div class="relative w-full lg:w-80">
        <span class="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400">search</span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search workspace..."
          class="w-full pl-12 pr-4 py-3 bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
        />
      </div>
    </div>

    <!-- Cards Grid -->
    <div v-for="section in filteredSections" :key="section.id">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <router-link
          v-for="item in section.items"
          :key="item.path"
          :to="item.path"
          class="group p-5 rounded-xl border border-zinc-200 dark:border-border-dark hover:border-teal dark:hover:border-teal bg-white dark:bg-background-dark hover:bg-teal/5 dark:hover:bg-teal/10 transition-all shadow-sm hover:shadow-md"
        >
          <div class="flex items-start gap-4">
            <div
              class="w-12 h-12 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors"
              :class="getIconBg(item.color)"
            >
              <span class="material-symbols-outlined text-2xl" :class="getIconText(item.color)">{{ item.icon }}</span>
            </div>
            <div class="flex-1 min-w-0">
              <h3 class="font-semibold text-zinc-900 dark:text-white group-hover:text-teal transition-colors">
                {{ item.name }}
              </h3>
              <p class="text-sm text-zinc-500 mt-1">{{ item.description }}</p>
            </div>
            <span class="material-symbols-outlined text-zinc-300 dark:text-zinc-600 group-hover:text-teal group-hover:translate-x-1 transition-all text-lg mt-1">
              arrow_forward
            </span>
          </div>
        </router-link>
      </div>
    </div>
  </div>
</template>
