<script setup lang="ts">
import { computed, watch, onMounted, onUnmounted } from 'vue'

interface Props {
  modelValue?: boolean
  title?: string
  size?: 'sm' | 'md' | 'lg' | 'xl' | 'full'
  closeOnOverlay?: boolean
  closeOnEsc?: boolean
  showClose?: boolean
  persistent?: boolean
  overflowVisible?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: false,
  size: 'md',
  closeOnOverlay: true,
  closeOnEsc: true,
  showClose: true,
  persistent: false,
  overflowVisible: false
})

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'close': []
}>()

const isOpen = computed({
  get: () => props.modelValue,
  set: (value) => emit('update:modelValue', value)
})

const sizeClasses = computed(() => {
  switch (props.size) {
    case 'sm': return 'max-w-sm'
    case 'lg': return 'max-w-2xl'
    case 'xl': return 'max-w-4xl'
    case 'full': return 'max-w-[95vw] max-h-[95vh]'
    default: return 'max-w-lg'
  }
})

function close() {
  if (!props.persistent) {
    isOpen.value = false
    emit('close')
  }
}

function handleOverlayClick() {
  if (props.closeOnOverlay) {
    close()
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape' && props.closeOnEsc && isOpen.value) {
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
          @click="handleOverlayClick"
        />

        <!-- Modal Container -->
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
              class="relative w-full bg-white dark:bg-background-dark rounded-2xl shadow-2xl ring-1 ring-black/5 dark:ring-white/10"
              :class="[sizeClasses, overflowVisible ? 'overflow-visible' : 'overflow-hidden']"
              @click.stop
            >
              <!-- Header with brand gradient -->
              <div
                v-if="title || showClose || $slots.header"
                class="relative bg-gradient-to-r from-navy via-navy/95 to-primary px-6 py-5 overflow-hidden rounded-t-2xl"
              >
                <!-- Decorative elements -->
                <div class="absolute top-0 right-0 w-32 h-32 bg-primary/20 rounded-full -translate-y-1/2 translate-x-1/2"></div>
                <div class="absolute bottom-0 left-0 w-20 h-20 bg-primary/10 rounded-full translate-y-1/2 -translate-x-1/2"></div>

                <div class="relative flex items-center justify-between">
                  <slot name="header">
                    <div class="flex items-center gap-3">
                      <div class="w-10 h-10 bg-primary/30 backdrop-blur rounded-xl flex items-center justify-center">
                        <span class="material-symbols-outlined text-white text-xl">description</span>
                      </div>
                      <h3 class="text-lg font-semibold text-white">{{ title }}</h3>
                    </div>
                  </slot>
                  <button
                    v-if="showClose"
                    type="button"
                    class="w-9 h-9 flex items-center justify-center rounded-xl bg-white/10 hover:bg-white/20 transition-colors"
                    @click="close"
                  >
                    <svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Body -->
              <div
                class="px-6 py-5"
                :class="overflowVisible ? 'overflow-visible' : 'max-h-[70vh] overflow-y-auto'"
              >
                <slot />
              </div>

              <!-- Footer -->
              <div
                v-if="$slots.footer"
                class="px-6 py-4 bg-gray-50 dark:bg-surface-dark/50 border-t border-gray-200 dark:border-gray-700/50 rounded-b-2xl"
              >
                <slot name="footer" />
              </div>
            </div>
          </Transition>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
