<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { documentsApi, documentStateApi } from '@/api/client'
import type { Document } from '@/types'

const props = defineProps<{
  document: Document
  canTransition?: boolean
}>()

const emit = defineEmits<{
  transitioned: [doc: Document]
}>()

const showDropdown = ref(false)
const showReasonModal = ref(false)
const pendingTarget = ref('')
const reason = ref('')
const isTransitioning = ref(false)
const transitionError = ref('')
const allowedTransitions = ref<{ toState: string; description?: string }[]>([])

const currentState = computed(() => props.document.state || 'Draft')

const stateConfig: Record<string, { icon: string; bg: string; text: string; border: string; label: string }> = {
  Draft: { icon: 'edit_note', bg: 'bg-zinc-100 dark:bg-zinc-700/50', text: 'text-zinc-600 dark:text-zinc-300', border: 'border-zinc-200 dark:border-zinc-600', label: 'Draft' },
  Active: { icon: 'play_circle', bg: 'bg-teal/10', text: 'text-teal', border: 'border-teal/30', label: 'Active' },
  Record: { icon: 'inventory', bg: 'bg-blue-50 dark:bg-blue-900/20', text: 'text-blue-600 dark:text-blue-400', border: 'border-blue-200 dark:border-blue-700', label: 'Record' },
  Archived: { icon: 'archive', bg: 'bg-amber-50 dark:bg-amber-900/20', text: 'text-amber-600 dark:text-amber-400', border: 'border-amber-200 dark:border-amber-700', label: 'Archived' },
  Disposed: { icon: 'delete_forever', bg: 'bg-rose-50 dark:bg-rose-900/20', text: 'text-rose-600 dark:text-rose-400', border: 'border-rose-200 dark:border-rose-700', label: 'Disposed' },
  OnHold: { icon: 'pause_circle', bg: 'bg-purple-50 dark:bg-purple-900/20', text: 'text-purple-600 dark:text-purple-400', border: 'border-purple-200 dark:border-purple-700', label: 'On Hold' },
  PendingDisposal: { icon: 'pending', bg: 'bg-orange-50 dark:bg-orange-900/20', text: 'text-orange-600 dark:text-orange-400', border: 'border-orange-200 dark:border-orange-700', label: 'Pending Disposal' },
  Quarantined: { icon: 'shield', bg: 'bg-red-50 dark:bg-red-900/20', text: 'text-red-600 dark:text-red-400', border: 'border-red-200 dark:border-red-700', label: 'Quarantined' }
}

const stateFlow = ['Draft', 'Active', 'Record', 'Archived', 'PendingDisposal', 'Disposed']

const currentConfig = computed(() => stateConfig[currentState.value] || stateConfig.Draft)

async function loadAllowedTransitions() {
  if (!props.canTransition || !props.document.id) return
  try {
    const res = await documentStateApi.getAllowedTransitions(props.document.id)
    allowedTransitions.value = res.data || []
  } catch {
    // Fallback: compute from stateFlow
    const idx = stateFlow.indexOf(currentState.value)
    if (idx >= 0 && idx < stateFlow.length - 1) {
      allowedTransitions.value = [{ toState: stateFlow[idx + 1] }]
    }
  }
}

onMounted(loadAllowedTransitions)
watch(() => props.document.state, loadAllowedTransitions)

function promptTransition(target: string) {
  showDropdown.value = false
  pendingTarget.value = target
  reason.value = ''
  transitionError.value = ''
  showReasonModal.value = true
}

async function confirmTransition() {
  if (!pendingTarget.value) return
  isTransitioning.value = true
  transitionError.value = ''
  try {
    const res = await documentsApi.transitionState(props.document.id, {
      targetState: pendingTarget.value,
      reason: reason.value || undefined
    })
    showReasonModal.value = false
    emit('transitioned', res.data)
  } catch (err: any) {
    transitionError.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Transition failed'
  } finally {
    isTransitioning.value = false
  }
}

const currentStepIndex = computed(() => {
  // OnHold and Quarantined are special states not in linear flow
  if (currentState.value === 'OnHold' || currentState.value === 'Quarantined') {
    return -1 // Not in linear flow
  }
  const idx = stateFlow.indexOf(currentState.value)
  return idx >= 0 ? idx : 0
})
</script>

<template>
  <div class="space-y-2">
    <!-- State Badge -->
    <div class="flex items-center gap-2 flex-wrap">
      <span
        class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-semibold rounded-lg border"
        :class="[currentConfig.bg, currentConfig.text, currentConfig.border]"
      >
        <span class="material-symbols-outlined text-sm" style="font-variation-settings: 'FILL' 1;">{{ currentConfig.icon }}</span>
        {{ currentConfig.label }}
      </span>

      <!-- On Hold overlay badge -->
      <span
        v-if="document.isOnLegalHold && currentState !== 'OnHold'"
        class="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] font-bold rounded-full bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400 border border-purple-200 dark:border-purple-700"
      >
        <span class="material-symbols-outlined text-xs" style="font-variation-settings: 'FILL' 1;">pause_circle</span>
        Legal Hold
      </span>

      <!-- Transition buttons from API -->
      <button
        v-for="transition in allowedTransitions"
        :key="transition.toState"
        @click="promptTransition(transition.toState)"
        class="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-lg border border-dashed border-zinc-300 dark:border-zinc-600 text-zinc-500 dark:text-zinc-400 hover:border-teal hover:text-teal hover:bg-teal/5 transition-all"
        :title="transition.description"
      >
        <span class="material-symbols-outlined text-xs">arrow_forward</span>
        {{ stateConfig[transition.toState]?.label || transition.toState }}
      </button>
    </div>

    <!-- Progress Track -->
    <div class="flex items-center gap-0.5">
      <template v-for="(state, idx) in stateFlow" :key="state">
        <div
          class="flex items-center gap-1 px-1.5 py-0.5 rounded text-[9px] font-medium"
          :class="idx <= currentStepIndex
            ? [stateConfig[state].bg, stateConfig[state].text]
            : 'bg-zinc-50 dark:bg-zinc-800 text-zinc-300 dark:text-zinc-600'"
        >
          <span
            class="material-symbols-outlined"
            style="font-size: 11px;"
            :style="idx <= currentStepIndex ? 'font-variation-settings: \'FILL\' 1;' : ''"
          >{{ idx < currentStepIndex ? 'check_circle' : stateConfig[state].icon }}</span>
          {{ stateConfig[state].label }}
        </div>
        <span v-if="idx < stateFlow.length - 1" class="material-symbols-outlined text-zinc-300 dark:text-zinc-600" style="font-size: 10px;">chevron_right</span>
      </template>
    </div>

    <!-- Transition Reason Modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition duration-200 ease-out"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition duration-150 ease-in"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div v-if="showReasonModal" class="fixed inset-0 z-[100] flex items-center justify-center">
          <div class="absolute inset-0 bg-black/50" @click="showReasonModal = false"></div>
          <Transition
            enter-active-class="transition duration-200 ease-out"
            enter-from-class="opacity-0 scale-95"
            enter-to-class="opacity-100 scale-100"
            leave-active-class="transition duration-150 ease-in"
            leave-from-class="opacity-100 scale-100"
            leave-to-class="opacity-0 scale-95"
          >
            <div v-if="showReasonModal" class="relative bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md overflow-hidden">
              <!-- Header -->
              <div class="px-6 py-4 bg-gradient-to-r from-[#0d1117] to-teal/80 relative overflow-hidden">
                <div class="absolute -right-4 -top-4 w-24 h-24 bg-white/5 rounded-full"></div>
                <div class="absolute -right-8 -bottom-8 w-32 h-32 bg-white/5 rounded-full"></div>
                <div class="relative flex items-center gap-3">
                  <div class="w-10 h-10 rounded-lg bg-white/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-white">swap_horiz</span>
                  </div>
                  <div>
                    <h3 class="text-white font-bold">Transition Document State</h3>
                    <p class="text-white/70 text-xs mt-0.5">{{ currentConfig.label }} &rarr; {{ stateConfig[pendingTarget]?.label }}</p>
                  </div>
                </div>
              </div>

              <!-- Body -->
              <div class="p-6 space-y-4">
                <!-- State visual -->
                <div class="flex items-center justify-center gap-3">
                  <span
                    class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg border"
                    :class="[currentConfig.bg, currentConfig.text, currentConfig.border]"
                  >
                    <span class="material-symbols-outlined text-sm" style="font-variation-settings: 'FILL' 1;">{{ currentConfig.icon }}</span>
                    {{ currentConfig.label }}
                  </span>
                  <span class="material-symbols-outlined text-zinc-400">arrow_forward</span>
                  <span
                    v-if="stateConfig[pendingTarget]"
                    class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg border"
                    :class="[stateConfig[pendingTarget].bg, stateConfig[pendingTarget].text, stateConfig[pendingTarget].border]"
                  >
                    <span class="material-symbols-outlined text-sm" style="font-variation-settings: 'FILL' 1;">{{ stateConfig[pendingTarget].icon }}</span>
                    {{ stateConfig[pendingTarget].label }}
                  </span>
                </div>

                <!-- Reason -->
                <div>
                  <label class="block text-xs font-medium text-zinc-700 dark:text-zinc-300 mb-1.5">Reason (optional)</label>
                  <textarea
                    v-model="reason"
                    rows="3"
                    placeholder="Provide a reason for this state transition..."
                    class="w-full px-3 py-2 bg-zinc-50 dark:bg-zinc-800 border border-zinc-200 dark:border-border-dark rounded-lg text-sm text-zinc-900 dark:text-white placeholder-zinc-400 focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none resize-none"
                  ></textarea>
                </div>

                <!-- Error -->
                <div v-if="transitionError" class="px-3 py-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-xs text-red-600 dark:text-red-400">
                  {{ transitionError }}
                </div>
              </div>

              <!-- Footer -->
              <div class="px-6 py-4 bg-zinc-50 dark:bg-zinc-800/50 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3">
                <button
                  @click="showReasonModal = false"
                  class="px-4 py-2 text-sm font-medium text-zinc-600 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-white transition-colors"
                >Cancel</button>
                <button
                  @click="confirmTransition"
                  :disabled="isTransitioning"
                  class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 flex items-center gap-2"
                >
                  <span v-if="isTransitioning" class="material-symbols-outlined text-sm animate-spin">refresh</span>
                  <span v-else class="material-symbols-outlined text-sm">check</span>
                  {{ isTransitioning ? 'Transitioning...' : 'Confirm Transition' }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
