<script setup lang="ts">
import { ref, computed } from 'vue'

const searchQuery = ref('')

const items = [
  { name: 'Physical Locations', path: '/physical-archive', icon: 'location_on', color: 'teal', description: 'Manage buildings, rooms, racks, shelves, and boxes' },
  { name: 'Physical Items', path: '/physical-archive', icon: 'inventory_2', color: 'blue', description: 'Track physical documents, files, and media' },
  { name: 'Accession Requests', path: '/accessions', icon: 'move_to_inbox', color: 'indigo', description: 'Request and manage records transfers to archive' },
  { name: 'Circulation', path: '/circulation', icon: 'swap_horiz', color: 'amber', description: 'Check out, return, and track physical items' }
]

const filteredItems = computed(() => {
  if (!searchQuery.value.trim()) return items
  const q = searchQuery.value.toLowerCase()
  return items.filter(i => i.name.toLowerCase().includes(q) || i.description.toLowerCase().includes(q))
})

function getIconBg(color: string) {
  const map: Record<string, string> = {
    teal: 'bg-teal/10 group-hover:bg-teal/20',
    blue: 'bg-blue-500/10 group-hover:bg-blue-500/20',
    indigo: 'bg-indigo-500/10 group-hover:bg-indigo-500/20',
    amber: 'bg-amber-500/10 group-hover:bg-amber-500/20'
  }
  return map[color] || map.teal
}

function getIconText(color: string) {
  const map: Record<string, string> = {
    teal: 'text-teal',
    blue: 'text-blue-500',
    indigo: 'text-indigo-500',
    amber: 'text-amber-500'
  }
  return map[color] || map.teal
}
</script>

<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold text-zinc-900 dark:text-white">Archive</h1>
        <p class="text-zinc-500 text-sm mt-1">Physical archive management, accessions, and circulation</p>
      </div>
      <div class="relative w-full lg:w-80">
        <span class="material-symbols-outlined absolute left-4 top-1/2 -translate-y-1/2 text-zinc-400">search</span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="Search archive..."
          class="w-full pl-12 pr-4 py-3 bg-white dark:bg-surface-dark border border-zinc-200 dark:border-border-dark rounded-lg text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal transition-all"
        />
      </div>
    </div>

    <!-- Cards Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <router-link
        v-for="item in filteredItems"
        :key="item.path + item.name"
        :to="item.path"
        class="group p-6 rounded-xl border border-zinc-200 dark:border-border-dark hover:border-teal dark:hover:border-teal bg-white dark:bg-background-dark hover:bg-teal/5 dark:hover:bg-teal/10 transition-all shadow-sm hover:shadow-md"
      >
        <div class="flex items-start gap-4">
          <div
            class="w-14 h-14 rounded-xl flex items-center justify-center flex-shrink-0 transition-colors"
            :class="getIconBg(item.color)"
          >
            <span class="material-symbols-outlined text-3xl" :class="getIconText(item.color)">{{ item.icon }}</span>
          </div>
          <div class="flex-1 min-w-0">
            <h3 class="font-semibold text-lg text-zinc-900 dark:text-white group-hover:text-teal transition-colors">
              {{ item.name }}
            </h3>
            <p class="text-sm text-zinc-500 mt-1">{{ item.description }}</p>
          </div>
          <span class="material-symbols-outlined text-zinc-300 dark:text-zinc-600 group-hover:text-teal group-hover:translate-x-1 transition-all text-xl mt-2">
            arrow_forward
          </span>
        </div>
      </router-link>
    </div>
  </div>
</template>
