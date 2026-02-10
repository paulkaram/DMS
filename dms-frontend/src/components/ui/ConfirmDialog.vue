<script setup lang="ts">
import { computed, watch, onMounted, onUnmounted } from 'vue'

export type ConfirmDialogType = 'info' | 'warning' | 'danger' | 'success' | 'edit'

interface Props {
  modelValue?: boolean
  type?: ConfirmDialogType
  title?: string
  message?: string
  confirmText?: string
  cancelText?: string
  showCancel?: boolean
  persistent?: boolean
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: false,
  type: 'info',
  title: 'Confirm',
  message: 'Are you sure you want to proceed?',
  confirmText: 'Confirm',
  cancelText: 'Cancel',
  showCancel: true,
  persistent: false,
  loading: false
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'confirm': []
  'cancel': []
}>()

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const typeConfig = computed(() => {
  const configs = {
    info: {
      icon: 'info',
      iconBg: 'bg-blue-100 dark:bg-blue-900/30',
      iconColor: 'text-blue-600 dark:text-blue-400',
      confirmBg: 'bg-blue-600 hover:bg-blue-700',
      confirmText: 'text-white'
    },
    warning: {
      icon: 'warning',
      iconBg: 'bg-amber-100 dark:bg-amber-900/30',
      iconColor: 'text-amber-600 dark:text-amber-400',
      confirmBg: 'bg-amber-600 hover:bg-amber-700',
      confirmText: 'text-white'
    },
    danger: {
      icon: 'delete',
      iconBg: 'bg-red-100 dark:bg-red-900/30',
      iconColor: 'text-red-600 dark:text-red-400',
      confirmBg: 'bg-red-600 hover:bg-red-700',
      confirmText: 'text-white'
    },
    success: {
      icon: 'check_circle',
      iconBg: 'bg-emerald-100 dark:bg-emerald-900/30',
      iconColor: 'text-emerald-600 dark:text-emerald-400',
      confirmBg: 'bg-emerald-600 hover:bg-emerald-700',
      confirmText: 'text-white'
    },
    edit: {
      icon: 'edit',
      iconBg: 'bg-teal/10',
      iconColor: 'text-teal',
      confirmBg: 'bg-teal hover:bg-teal/90',
      confirmText: 'text-white'
    }
  }
  return configs[props.type]
})

function close() {
  if (!props.persistent && !props.loading) {
    isOpen.value = false
    emit('cancel')
  }
}

function handleConfirm() {
  if (props.loading) return
  emit('confirm')
}

function handleCancel() {
  if (props.loading) return
  isOpen.value = false
  emit('cancel')
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && isOpen.value && !props.persistent && !props.loading) {
    close()
  }
}

watch(isOpen, (open) => {
  if (open) {
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = ''
  }
})

onMounted(() => {
  document.addEventListener('keydown', handleKeydown)
})

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown)
  document.body.style.overflow = ''
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="duration-200 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-150 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isOpen"
        class="fixed inset-0 z-50 overflow-y-auto"
      >
        <!-- Overlay -->
        <div
          class="fixed inset-0 bg-black/50 backdrop-blur-sm"
          @click="close"
        />

        <!-- Dialog Container -->
        <div class="flex min-h-full items-center justify-center p-4">
          <Transition
            enter-active-class="duration-200 ease-out"
            enter-from-class="opacity-0 scale-95 translate-y-4"
            enter-to-class="opacity-100 scale-100 translate-y-0"
            leave-active-class="duration-150 ease-in"
            leave-from-class="opacity-100 scale-100 translate-y-0"
            leave-to-class="opacity-0 scale-95 translate-y-4"
          >
            <div
              v-if="isOpen"
              class="relative w-full max-w-md bg-white dark:bg-background-dark rounded-lg shadow-2xl overflow-hidden"
              @click.stop
            >
              <!-- Content -->
              <div class="p-6">
                <div class="flex items-start gap-4">
                  <!-- Icon -->
                  <div
                    class="flex-shrink-0 w-12 h-12 rounded-lg flex items-center justify-center"
                    :class="typeConfig.iconBg"
                  >
                    <span
                      class="material-symbols-outlined text-2xl"
                      :class="typeConfig.iconColor"
                    >{{ typeConfig.icon }}</span>
                  </div>

                  <!-- Text -->
                  <div class="flex-1 pt-1">
                    <h3 class="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
                      {{ title }}
                    </h3>
                    <p class="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
                      {{ message }}
                    </p>
                    <!-- Slot for custom content -->
                    <slot />
                  </div>
                </div>
              </div>

              <!-- Actions -->
              <div class="px-6 py-4 bg-zinc-50 dark:bg-surface-dark/50 border-t border-zinc-200 dark:border-border-dark flex items-center justify-end gap-3">
                <button
                  v-if="showCancel"
                  @click="handleCancel"
                  :disabled="loading"
                  class="px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 bg-white dark:bg-surface-dark border border-zinc-300 dark:border-border-dark rounded-lg hover:bg-zinc-50 dark:hover:bg-border-dark transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {{ cancelText }}
                </button>
                <button
                  @click="handleConfirm"
                  :disabled="loading"
                  class="px-4 py-2 text-sm font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                  :class="[typeConfig.confirmBg, typeConfig.confirmText]"
                >
                  <svg
                    v-if="loading"
                    class="animate-spin w-4 h-4"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                  </svg>
                  {{ confirmText }}
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
