<script setup lang="ts">
import { computed } from 'vue'
import type { Document } from '@/types'
import DocumentIcon from './DocumentIcon.vue'
import DocumentStatus from './DocumentStatus.vue'
import { formatFileSize, formatRelativeDate } from '@/utils/formatters'
import { UiCheckbox } from '@/components/ui'

function getExpiryInfo(expiryDate?: string | null): { status: 'expired' | 'expiring-soon' | 'active' | null; label: string } | null {
  if (!expiryDate) return null
  const now = new Date()
  const expiry = new Date(expiryDate)
  const diffMs = expiry.getTime() - now.getTime()
  const diffDays = Math.ceil(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays <= 0) {
    return { status: 'expired', label: 'Expired' }
  }
  if (diffDays <= 7) {
    return { status: 'expiring-soon', label: `Expires ${expiry.toLocaleDateString()}` }
  }
  return { status: 'active', label: `Expires ${expiry.toLocaleDateString()}` }
}

const props = defineProps<{
  document: Document
  index?: number
  selected?: boolean
  isFavorite?: boolean
  selectable?: boolean
}>()

const emit = defineEmits<{
  select: [docId: string]
  preview: [doc: Document]
  view: [doc: Document]
  download: [doc: Document]
  share: [doc: Document]
  'context-menu': [event: MouseEvent, doc: Document]
  'version-history': [doc: Document]
}>()

const isSelected = computed({
  get: () => props.selected ?? false,
  set: () => emit('select', props.document.id)
})

const expiryInfo = computed(() => getExpiryInfo(props.document.expiryDate))
</script>

<template>
  <tr
    @contextmenu="emit('context-menu', $event, document)"
    @dblclick="emit('view', document)"
    class="transition-all duration-200 group cursor-pointer border-b border-zinc-100 dark:border-border-dark hover:bg-teal/5"
    :class="selected ? 'bg-teal/10 border-l-2 border-l-teal' : 'bg-white dark:bg-background-dark'"
  >
    <!-- Checkbox -->
    <td v-if="selectable" class="px-5 py-4 w-12" @click.stop>
      <UiCheckbox
        v-model="isSelected"
        size="sm"
      />
    </td>

    <!-- Name -->
    <td class="px-5 py-4">
      <div class="flex items-center gap-3">
        <DocumentIcon
          :extension="document.extension"
          :index="index"
          size="md"
        />
        <div class="min-w-0 flex-1">
          <div class="flex items-center gap-2">
            <p class="text-sm font-medium text-zinc-700 dark:text-zinc-200 group-hover:text-teal transition-colors truncate">
              {{ document.name }}
            </p>
            <!-- Favorite Star - inline with name -->
            <span
              v-if="isFavorite"
              class="material-symbols-outlined text-amber-500 text-sm"
              style="font-variation-settings: 'FILL' 1;"
            >star</span>
            <!-- Password Protected Badge -->
            <div
              v-if="document.hasPassword"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-teal/10 border border-teal/30"
              title="Password protected"
            >
              <span
                class="material-symbols-outlined text-teal"
                style="font-size: 12px; font-variation-settings: 'FILL' 1;"
              >shield_lock</span>
              <span class="text-[9px] font-semibold text-teal uppercase tracking-wide pr-0.5">Secured</span>
            </div>
            <!-- Shortcut Badge -->
            <div
              v-if="document.isShortcut"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-blue-500/10 border border-blue-500/30"
              title="Shortcut — original is in another folder"
            >
              <span
                class="material-symbols-outlined text-blue-500"
                style="font-size: 12px;"
              >shortcut</span>
              <span class="text-[9px] font-semibold text-blue-500 uppercase tracking-wide pr-0.5">Shortcut</span>
            </div>
            <!-- Attachment Badge -->
            <div
              v-if="document.attachmentCount && document.attachmentCount > 0"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-violet-500/15 border border-violet-500/30"
              :title="`${document.attachmentCount} attachment${document.attachmentCount !== 1 ? 's' : ''}`"
            >
              <span
                class="material-symbols-outlined text-violet-500"
                style="font-size: 12px; font-variation-settings: 'FILL' 1;"
              >attach_file</span>
              <span class="text-[9px] font-bold text-violet-600 dark:text-violet-400 tabular-nums pr-0.5">{{ document.attachmentCount }}</span>
            </div>
            <!-- Privacy Level Badge -->
            <div
              v-if="document.privacyLevelName"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full border"
              :style="{
                backgroundColor: (document.privacyLevelColor || '#6b7280') + '15',
                borderColor: (document.privacyLevelColor || '#6b7280') + '30'
              }"
              :title="`Privacy: ${document.privacyLevelName}`"
            >
              <span
                class="material-symbols-outlined"
                style="font-size: 12px;"
                :style="{ color: document.privacyLevelColor || '#6b7280' }"
              >shield</span>
              <span
                class="text-[9px] font-semibold uppercase tracking-wide pr-0.5"
                :style="{ color: document.privacyLevelColor || '#6b7280' }"
              >{{ document.privacyLevelName }}</span>
            </div>
            <!-- Expiry Badge -->
            <div
              v-if="expiryInfo?.status === 'expired'"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-red-500/15 border border-red-500/30"
              title="This document has expired"
            >
              <span
                class="material-symbols-outlined text-red-500"
                style="font-size: 12px;"
              >event_busy</span>
              <span class="text-[9px] font-semibold text-red-600 dark:text-red-400 uppercase tracking-wide pr-0.5">Expired</span>
            </div>
            <div
              v-else-if="expiryInfo?.status === 'expiring-soon'"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-amber-500/15 border border-amber-500/30"
              :title="expiryInfo.label"
            >
              <span
                class="material-symbols-outlined text-amber-500"
                style="font-size: 12px;"
              >schedule</span>
              <span class="text-[9px] font-semibold text-amber-600 dark:text-amber-400 tracking-wide pr-0.5">{{ expiryInfo.label }}</span>
            </div>
            <div
              v-else-if="expiryInfo?.status === 'active'"
              class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-zinc-100 dark:bg-zinc-800 border border-zinc-200 dark:border-zinc-700"
              :title="expiryInfo.label"
            >
              <span
                class="material-symbols-outlined text-zinc-400"
                style="font-size: 12px;"
              >event</span>
              <span class="text-[9px] text-zinc-500 dark:text-zinc-400 tracking-wide pr-0.5">{{ expiryInfo.label }}</span>
            </div>
          </div>
          <span class="text-[10px] text-zinc-400 uppercase tracking-wide">{{ document.extension?.replace('.', '') }}</span>
        </div>
      </div>
    </td>

    <!-- Size -->
    <td class="px-5 py-4">
      <span class="text-sm text-zinc-500 dark:text-zinc-400 tabular-nums">
        {{ formatFileSize(document.size) }}
      </span>
    </td>

    <!-- Created By -->
    <td class="px-5 py-4">
      <span class="text-sm text-zinc-500 dark:text-zinc-400 truncate max-w-[120px] block">
        {{ document.createdByName || '-' }}
      </span>
    </td>

    <!-- Created -->
    <td class="px-5 py-4">
      <span class="text-sm text-zinc-500 dark:text-zinc-400">
        {{ formatRelativeDate(document.createdAt) }}
      </span>
    </td>

    <!-- Content Type -->
    <td class="px-5 py-4">
      <span
        v-if="document.contentTypeName"
        class="inline-flex items-center px-2.5 py-1 bg-teal/10 text-teal rounded-lg text-xs font-medium truncate max-w-[100px]"
        :title="document.contentTypeName"
      >
        {{ document.contentTypeName }}
      </span>
      <span v-else class="text-sm text-zinc-400">-</span>
    </td>

    <!-- Version -->
    <td class="px-5 py-4">
      <span class="inline-flex items-center justify-center min-w-[40px] px-2.5 py-1 bg-[#0d1117] rounded-lg text-xs font-semibold text-white">
        v{{ document.currentMajorVersion || document.currentVersion }}.{{ document.currentMinorVersion || 0 }}
      </span>
    </td>

    <!-- Status -->
    <td class="px-5 py-4">
      <DocumentStatus :document="document" show-icon />
    </td>

    <!-- Actions -->
    <td class="px-5 py-4">
      <div class="flex items-center justify-end gap-0.5">
        <!-- Quick Actions -->
        <button
          @click.stop="emit('preview', document)"
          class="p-2 text-teal/70 hover:text-teal hover:bg-teal/10 rounded-lg transition-all"
          title="Preview"
        >
          <span class="material-symbols-outlined text-lg">open_in_new</span>
        </button>
        <button
          @click.stop="emit('view', document)"
          class="p-2 text-blue-500/70 hover:text-blue-500 hover:bg-blue-500/10 rounded-lg transition-all"
          title="Details"
        >
          <span class="material-symbols-outlined text-lg">info</span>
        </button>
        <button
          @click.stop="emit('download', document)"
          class="p-2 text-emerald-500/70 hover:text-emerald-500 hover:bg-emerald-500/10 rounded-lg transition-all"
          title="Download"
        >
          <span class="material-symbols-outlined text-lg">download</span>
        </button>
        <!-- More Menu Button -->
        <button
          @click.stop="emit('context-menu', $event, document)"
          class="p-2 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-300 hover:bg-zinc-100 dark:hover:bg-border-dark rounded-lg transition-all"
          title="More actions"
        >
          <span class="material-symbols-outlined text-lg">more_vert</span>
        </button>
      </div>
    </td>
  </tr>
</template>
