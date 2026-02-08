<script setup lang="ts">
import { ref, onMounted, nextTick, watch } from 'vue'
import { Canvas, PencilBrush } from 'fabric'
import type { SavedSignature } from '@/types'
import { savedSignaturesApi } from '@/api/client'

const props = defineProps<{
  modelValue: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  'select-signature': [dataUrl: string]
}>()

const activeTab = ref<'draw' | 'type' | 'saved'>('draw')
const drawCanvasRef = ref<HTMLCanvasElement | null>(null)
let drawCanvas: Canvas | null = null

// Type tab
const typedText = ref('')
const typedFont = ref('Dancing Script')
const typedPreviewRef = ref<HTMLCanvasElement | null>(null)

// Saved signatures
const savedSignatures = ref<SavedSignature[]>([])
const isLoadingSaved = ref(false)

// Save options
const saveForReuse = ref(false)
const signatureName = ref('My Signature')

const fonts = [
  { name: 'Dancing Script', label: 'Script' },
  { name: 'Caveat', label: 'Casual' },
  { name: 'Great Vibes', label: 'Elegant' },
  { name: 'Sacramento', label: 'Flowing' }
]

watch(() => props.modelValue, async (isOpen) => {
  if (isOpen) {
    await loadSavedSignatures()
    nextTick(() => {
      if (activeTab.value === 'draw') initDrawCanvas()
    })
  } else {
    if (drawCanvas) {
      drawCanvas.dispose()
      drawCanvas = null
    }
  }
})

watch(activeTab, (tab) => {
  nextTick(() => {
    if (tab === 'draw') initDrawCanvas()
  })
})

function initDrawCanvas() {
  if (!drawCanvasRef.value) return
  if (drawCanvas) {
    drawCanvas.dispose()
  }

  drawCanvas = new Canvas(drawCanvasRef.value, {
    width: 500,
    height: 200,
    isDrawingMode: true,
    backgroundColor: '#ffffff'
  })

  drawCanvas.freeDrawingBrush = new PencilBrush(drawCanvas)
  drawCanvas.freeDrawingBrush.color = '#000000'
  drawCanvas.freeDrawingBrush.width = 3
}

async function loadSavedSignatures() {
  isLoadingSaved.value = true
  try {
    const response = await savedSignaturesApi.getAll()
    savedSignatures.value = response.data || []
  } catch {
    savedSignatures.value = []
  } finally {
    isLoadingSaved.value = false
  }
}

function clearDrawCanvas() {
  if (drawCanvas) {
    drawCanvas.clear()
    drawCanvas.backgroundColor = '#ffffff'
    drawCanvas.renderAll()
  }
}

async function useDrawnSignature() {
  if (!drawCanvas) return

  // Export as data URL (trimmed to content)
  const dataUrl = drawCanvas.toDataURL({
    format: 'png',
    multiplier: 2
  })

  if (saveForReuse.value) {
    await saveSignature(dataUrl, 'drawn')
  }

  emit('select-signature', dataUrl)
  close()
}

function useTypedSignature() {
  // Create a canvas to render the text as image
  const tempCanvas = document.createElement('canvas')
  tempCanvas.width = 500
  tempCanvas.height = 100
  const ctx = tempCanvas.getContext('2d')
  if (!ctx) return

  ctx.fillStyle = '#ffffff'
  ctx.fillRect(0, 0, 500, 100)
  ctx.fillStyle = '#000000'
  ctx.font = `48px "${typedFont.value}", cursive`
  ctx.textBaseline = 'middle'
  ctx.fillText(typedText.value, 20, 50)

  const dataUrl = tempCanvas.toDataURL('image/png')

  if (saveForReuse.value) {
    saveSignature(dataUrl, 'typed')
  }

  emit('select-signature', dataUrl)
  close()
}

function useSavedSignature(signature: SavedSignature) {
  emit('select-signature', signature.signatureData)
  close()
}

async function saveSignature(dataUrl: string, type: 'drawn' | 'typed') {
  try {
    await savedSignaturesApi.create({
      name: signatureName.value || 'My Signature',
      signatureData: dataUrl,
      signatureType: type,
      isDefault: savedSignatures.value.length === 0
    })
  } catch {
    // Silently fail
  }
}

async function deleteSavedSignature(id: string) {
  try {
    await savedSignaturesApi.delete(id)
    savedSignatures.value = savedSignatures.value.filter(s => s.id !== id)
  } catch {
    // Silently fail
  }
}

function close() {
  emit('update:modelValue', false)
}
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
      <div v-if="modelValue" class="fixed inset-0 z-[60] flex items-center justify-center">
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/50" @click="close"></div>

        <!-- Modal -->
        <Transition
          enter-active-class="duration-200 ease-out"
          enter-from-class="opacity-0 scale-95"
          enter-to-class="opacity-100 scale-100"
          leave-active-class="duration-150 ease-in"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div v-if="modelValue" class="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg mx-4 overflow-hidden">
            <!-- Header -->
            <div class="relative overflow-hidden px-6 py-4 bg-gradient-to-r from-[#0d1117] to-teal">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 -bottom-8 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-xl bg-white/10 flex items-center justify-center">
                    <span class="material-symbols-outlined text-white">signature</span>
                  </div>
                  <div>
                    <h2 class="text-lg font-bold text-white">Add Signature</h2>
                    <p class="text-xs text-white/60">Draw, type, or use a saved signature</p>
                  </div>
                </div>
                <button
                  @click="close"
                  class="p-1.5 rounded-lg text-white/60 hover:text-white hover:bg-white/10 transition-colors"
                >
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Tabs -->
            <div class="flex border-b border-slate-200">
              <button
                v-for="tab in [
                  { id: 'draw' as const, label: 'Draw', icon: 'draw' },
                  { id: 'type' as const, label: 'Type', icon: 'keyboard' },
                  { id: 'saved' as const, label: 'Saved', icon: 'bookmark' }
                ]"
                :key="tab.id"
                @click="activeTab = tab.id"
                class="flex-1 flex items-center justify-center gap-2 px-4 py-3 text-sm font-medium transition-colors border-b-2"
                :class="activeTab === tab.id
                  ? 'text-teal border-teal'
                  : 'text-slate-500 border-transparent hover:text-slate-700'"
              >
                <span class="material-symbols-outlined text-lg">{{ tab.icon }}</span>
                {{ tab.label }}
                <span
                  v-if="tab.id === 'saved' && savedSignatures.length > 0"
                  class="px-1.5 py-0.5 bg-teal/10 text-teal text-[10px] font-bold rounded-full"
                >
                  {{ savedSignatures.length }}
                </span>
              </button>
            </div>

            <!-- Tab Content -->
            <div class="p-6">
              <!-- Draw Tab -->
              <div v-if="activeTab === 'draw'">
                <div class="border-2 border-dashed border-slate-200 rounded-xl overflow-hidden mb-4">
                  <canvas ref="drawCanvasRef" class="w-full" />
                </div>
                <div class="flex items-center justify-between mb-4">
                  <button
                    @click="clearDrawCanvas"
                    class="text-xs text-slate-500 hover:text-red-500 flex items-center gap-1 transition-colors"
                  >
                    <span class="material-symbols-outlined text-sm">delete</span>
                    Clear
                  </button>
                </div>

                <!-- Save for reuse -->
                <div class="flex items-center gap-3 mb-4 p-3 bg-slate-50 rounded-xl">
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      v-model="saveForReuse"
                      class="w-4 h-4 text-teal border-slate-300 rounded focus:ring-teal"
                    />
                    <span class="text-xs text-slate-600">Save for reuse</span>
                  </label>
                  <input
                    v-if="saveForReuse"
                    v-model="signatureName"
                    type="text"
                    placeholder="Signature name"
                    class="flex-1 text-xs px-3 py-1.5 border border-slate-200 rounded-lg focus:border-teal focus:ring-1 focus:ring-teal/20 outline-none"
                  />
                </div>

                <button
                  @click="useDrawnSignature"
                  class="w-full py-2.5 bg-teal text-white text-sm font-medium rounded-xl hover:bg-teal/90 transition-colors"
                >
                  Use This Signature
                </button>
              </div>

              <!-- Type Tab -->
              <div v-if="activeTab === 'type'">
                <input
                  v-model="typedText"
                  type="text"
                  placeholder="Type your name..."
                  class="w-full px-4 py-3 border border-slate-200 rounded-xl text-lg focus:border-teal focus:ring-1 focus:ring-teal/20 outline-none mb-4"
                />

                <!-- Font selection -->
                <div class="flex gap-2 mb-4">
                  <button
                    v-for="font in fonts"
                    :key="font.name"
                    @click="typedFont = font.name"
                    class="flex-1 py-2 px-3 border rounded-lg text-sm transition-colors"
                    :class="typedFont === font.name
                      ? 'border-teal bg-teal/5 text-teal'
                      : 'border-slate-200 text-slate-500 hover:border-slate-300'"
                  >
                    {{ font.label }}
                  </button>
                </div>

                <!-- Preview -->
                <div class="border-2 border-dashed border-slate-200 rounded-xl p-6 mb-4 bg-white text-center">
                  <span
                    class="text-4xl text-slate-800"
                    :style="{ fontFamily: `'${typedFont}', cursive` }"
                  >
                    {{ typedText || 'Your Name' }}
                  </span>
                </div>

                <!-- Save for reuse -->
                <div class="flex items-center gap-3 mb-4 p-3 bg-slate-50 rounded-xl">
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      v-model="saveForReuse"
                      class="w-4 h-4 text-teal border-slate-300 rounded focus:ring-teal"
                    />
                    <span class="text-xs text-slate-600">Save for reuse</span>
                  </label>
                  <input
                    v-if="saveForReuse"
                    v-model="signatureName"
                    type="text"
                    placeholder="Signature name"
                    class="flex-1 text-xs px-3 py-1.5 border border-slate-200 rounded-lg focus:border-teal focus:ring-1 focus:ring-teal/20 outline-none"
                  />
                </div>

                <button
                  @click="useTypedSignature"
                  :disabled="!typedText"
                  class="w-full py-2.5 bg-teal text-white text-sm font-medium rounded-xl hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  Use This Signature
                </button>
              </div>

              <!-- Saved Tab -->
              <div v-if="activeTab === 'saved'">
                <div v-if="isLoadingSaved" class="flex items-center justify-center py-8">
                  <span class="material-symbols-outlined animate-spin text-2xl text-teal">progress_activity</span>
                </div>

                <div v-else-if="savedSignatures.length === 0" class="text-center py-8">
                  <div class="w-16 h-16 mx-auto mb-3 rounded-2xl bg-slate-100 flex items-center justify-center">
                    <span class="material-symbols-outlined text-3xl text-slate-300">signature</span>
                  </div>
                  <p class="text-sm text-slate-500">No saved signatures yet</p>
                  <p class="text-xs text-slate-400 mt-1">Draw or type a signature and save it for reuse</p>
                </div>

                <div v-else class="space-y-3 max-h-64 overflow-y-auto">
                  <div
                    v-for="sig in savedSignatures"
                    :key="sig.id"
                    class="flex items-center gap-3 p-3 border border-slate-200 rounded-xl hover:border-teal/30 hover:bg-teal/5 transition-colors cursor-pointer group"
                    @click="useSavedSignature(sig)"
                  >
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2 mb-1">
                        <span class="text-sm font-medium text-slate-700 truncate">{{ sig.name }}</span>
                        <span
                          v-if="sig.isDefault"
                          class="px-1.5 py-0.5 bg-teal/10 text-teal text-[9px] font-bold rounded-full uppercase"
                        >
                          Default
                        </span>
                      </div>
                      <img :src="sig.signatureData" class="h-8 object-contain" />
                    </div>
                    <button
                      @click.stop="deleteSavedSignature(sig.id)"
                      class="p-1 rounded-lg text-slate-300 hover:text-red-500 hover:bg-red-50 opacity-0 group-hover:opacity-100 transition-all"
                    >
                      <span class="material-symbols-outlined text-sm">delete</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>
