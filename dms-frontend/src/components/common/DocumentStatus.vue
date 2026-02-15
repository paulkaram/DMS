<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import type { Document, ApprovalRequest } from '@/types'
import { ApprovalStatus, ApprovalActionTypes } from '@/types'
import { approvalsApi } from '@/api/client'

const props = defineProps<{
  document: Document
  showIcon?: boolean
  size?: 'sm' | 'md'
}>()

const statusInfo = computed(() => {
  if (props.document.approvalStatus === ApprovalStatus.Pending) {
    return {
      label: 'Pending Approval',
      bgClass: 'bg-indigo-50 dark:bg-indigo-900/30 border border-indigo-200 dark:border-indigo-700',
      textClass: 'text-indigo-600 dark:text-indigo-400',
      dotClass: 'bg-indigo-500',
      icon: 'hourglass_top'
    }
  }
  if (props.document.approvalStatus === ApprovalStatus.Approved) {
    return {
      label: 'Approved',
      bgClass: 'bg-emerald-50 dark:bg-emerald-900/30 border border-emerald-200 dark:border-emerald-700',
      textClass: 'text-emerald-600 dark:text-emerald-400',
      dotClass: 'bg-emerald-500',
      icon: 'verified'
    }
  }
  if (props.document.approvalStatus === ApprovalStatus.Rejected) {
    return {
      label: 'Rejected',
      bgClass: 'bg-rose-50 dark:bg-rose-900/30 border border-rose-200 dark:border-rose-700',
      textClass: 'text-rose-600 dark:text-rose-400',
      dotClass: 'bg-rose-500',
      icon: 'cancel'
    }
  }
  if (props.document.approvalStatus === ApprovalStatus.ReturnedForRevision) {
    return {
      label: 'Revision Required',
      bgClass: 'bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700',
      textClass: 'text-amber-600 dark:text-amber-400',
      dotClass: 'bg-amber-500',
      icon: 'undo'
    }
  }
  if (props.document.isCheckedOut) {
    return {
      label: 'Checked Out',
      bgClass: 'bg-amber-50 dark:bg-amber-900/30 border border-amber-200 dark:border-amber-700',
      textClass: 'text-amber-600 dark:text-amber-400',
      dotClass: 'bg-amber-500',
      icon: 'assignment_ind'
    }
  }
  if (props.document.isLocked) {
    return {
      label: 'Locked',
      bgClass: 'bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700',
      textClass: 'text-red-600 dark:text-red-400',
      dotClass: 'bg-red-500',
      icon: 'lock'
    }
  }
  return {
    label: 'Available',
    bgClass: 'bg-teal/10 border border-teal/30',
    textClass: 'text-teal',
    dotClass: 'bg-teal',
    icon: 'check_circle'
  }
})

const sizeClasses = computed(() => {
  return props.size === 'sm'
    ? 'px-2 py-0.5 text-[9px]'
    : 'px-2.5 py-1 text-[10px]'
})

// Popover for approval details
const hasApprovalStatus = computed(() =>
  props.document.approvalStatus !== undefined &&
  props.document.approvalStatus !== null &&
  props.document.approvalStatus >= 0
)

const showPopover = ref(false)
const isLoadingDetails = ref(false)
const approvalDetails = ref<ApprovalRequest | null>(null)
const popoverRef = ref<HTMLElement | null>(null)
const badgeRef = ref<HTMLElement | null>(null)

async function togglePopover(e: Event) {
  e.stopPropagation()
  e.preventDefault()
  if (!hasApprovalStatus.value) return

  if (showPopover.value) {
    showPopover.value = false
    return
  }

  showPopover.value = true

  if (!approvalDetails.value) {
    isLoadingDetails.value = true
    try {
      const res = await approvalsApi.getDocumentRequests(props.document.id)
      const requests = res.data as ApprovalRequest[]
      if (requests.length > 0) {
        approvalDetails.value = requests[0]
      }
    } catch {
      // silently fail
    } finally {
      isLoadingDetails.value = false
    }
  }
}

function handleClickOutside(e: Event) {
  if (
    popoverRef.value && !popoverRef.value.contains(e.target as Node) &&
    badgeRef.value && !badgeRef.value.contains(e.target as Node)
  ) {
    showPopover.value = false
  }
}

function getActionLabel(action: number): string {
  switch (action) {
    case ApprovalActionTypes.Approved: return 'Approved'
    case ApprovalActionTypes.Rejected: return 'Rejected'
    case ApprovalActionTypes.ReturnedForRevision: return 'Returned for Revision'
    default: return 'Unknown'
  }
}

function getActionIcon(action: number): string {
  switch (action) {
    case ApprovalActionTypes.Approved: return 'check_circle'
    case ApprovalActionTypes.Rejected: return 'cancel'
    case ApprovalActionTypes.ReturnedForRevision: return 'undo'
    default: return 'help'
  }
}

function getActionColor(action: number): string {
  switch (action) {
    case ApprovalActionTypes.Approved: return 'text-emerald-600 dark:text-emerald-400'
    case ApprovalActionTypes.Rejected: return 'text-rose-600 dark:text-rose-400'
    case ApprovalActionTypes.ReturnedForRevision: return 'text-amber-600 dark:text-amber-400'
    default: return 'text-zinc-500'
  }
}

function formatDateTime(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit'
  })
}

const popoverStyle = computed(() => {
  if (!badgeRef.value) return {}
  const rect = badgeRef.value.getBoundingClientRect()
  return {
    top: rect.bottom + 6 + 'px',
    left: Math.min(rect.left, window.innerWidth - 336) + 'px'
  }
})

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
})
</script>

<template>
  <div class="relative inline-flex">
    <span
      ref="badgeRef"
      class="inline-flex items-center gap-1.5 rounded-lg font-semibold uppercase tracking-wider"
      :class="[
        statusInfo.bgClass, statusInfo.textClass, sizeClasses,
        hasApprovalStatus ? 'cursor-pointer hover:opacity-80 transition-opacity' : ''
      ]"
      @click="togglePopover"
    >
      <span v-if="showIcon" class="material-symbols-outlined text-xs" style="font-variation-settings: 'FILL' 1;">{{ statusInfo.icon }}</span>
      <span v-else class="w-1.5 h-1.5 rounded-full" :class="statusInfo.dotClass"></span>
      {{ statusInfo.label }}
      <span v-if="hasApprovalStatus" class="material-symbols-outlined text-[10px] opacity-60">expand_more</span>
    </span>

    <!-- Approval Details Popover -->
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
          v-if="showPopover && hasApprovalStatus"
          ref="popoverRef"
          class="fixed z-[100] w-80 bg-white dark:bg-surface-dark rounded-xl shadow-2xl border border-zinc-200 dark:border-border-dark overflow-hidden"
          :style="popoverStyle"
        >
          <!-- Header -->
          <div class="px-4 py-3 bg-gradient-to-r from-[#0d1117] to-[#161b22] flex items-center gap-2">
            <span class="material-symbols-outlined text-teal text-lg" style="font-variation-settings: 'FILL' 1;">approval</span>
            <span class="text-sm font-semibold text-white">Approval Details</span>
          </div>

          <!-- Loading -->
          <div v-if="isLoadingDetails" class="p-6 flex items-center justify-center gap-2">
            <div class="w-4 h-4 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
            <span class="text-xs text-zinc-500">Loading...</span>
          </div>

          <!-- No data -->
          <div v-else-if="!approvalDetails" class="p-6 text-center">
            <span class="text-xs text-zinc-400">No approval details found</span>
          </div>

          <!-- Details -->
          <div v-else class="p-4 space-y-3 max-h-[300px] overflow-y-auto">
            <!-- Workflow info -->
            <div v-if="approvalDetails.workflowName" class="flex items-center gap-2 text-xs text-zinc-500">
              <span class="material-symbols-outlined text-sm">account_tree</span>
              {{ approvalDetails.workflowName }}
            </div>

            <!-- Request comment -->
            <div v-if="approvalDetails.comments" class="text-xs text-zinc-600 dark:text-zinc-400 bg-zinc-50 dark:bg-zinc-800/50 rounded-lg px-3 py-2 border-l-2 border-indigo-300">
              {{ approvalDetails.comments }}
            </div>

            <!-- Actions timeline -->
            <div v-if="approvalDetails.actions && approvalDetails.actions.length > 0" class="space-y-2.5">
              <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">History</p>
              <div
                v-for="action in approvalDetails.actions"
                :key="action.id"
                class="flex items-start gap-2.5"
              >
                <span
                  class="material-symbols-outlined text-base mt-0.5 flex-shrink-0"
                  :class="getActionColor(action.action)"
                  style="font-variation-settings: 'FILL' 1;"
                >{{ getActionIcon(action.action) }}</span>
                <div class="flex-1 min-w-0">
                  <div class="flex items-baseline gap-1.5">
                    <span class="text-xs font-semibold text-zinc-700 dark:text-zinc-200">{{ action.approverName || 'Unknown' }}</span>
                    <span class="text-[10px]" :class="getActionColor(action.action)">{{ getActionLabel(action.action) }}</span>
                  </div>
                  <p v-if="action.comments" class="text-xs text-zinc-600 dark:text-zinc-400 mt-0.5 leading-relaxed">
                    "{{ action.comments }}"
                  </p>
                  <p class="text-[10px] text-zinc-400 mt-0.5">{{ formatDateTime(action.actionDate) }}</p>
                </div>
              </div>
            </div>

            <!-- No actions yet -->
            <div v-else class="text-center py-2">
              <span class="text-xs text-zinc-400">Awaiting reviewer action</span>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
