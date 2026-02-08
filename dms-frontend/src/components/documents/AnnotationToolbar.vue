<script setup lang="ts">
import { ref, computed } from 'vue'
import type { AnnotationTool, AnnotationToolSettings } from '@/types'

const props = defineProps<{
  activeTool: AnnotationTool
  toolSettings: AnnotationToolSettings
  canUndo: boolean
  canRedo: boolean
  isSaving: boolean
  hasUnsavedChanges: boolean
}>()

const emit = defineEmits<{
  'update:activeTool': [tool: AnnotationTool]
  'update:toolSettings': [settings: Partial<AnnotationToolSettings>]
  save: []
  discard: []
  close: []
  undo: []
  redo: []
  'delete-selected': []
  'open-signature': []
}>()

const showColorPicker = ref(false)
const colorPickerRef = ref<HTMLElement | null>(null)

const tools: Array<{ id: AnnotationTool; icon: string; label: string }> = [
  { id: 'select', icon: 'near_me', label: 'Select' },
  { id: 'freehand', icon: 'draw', label: 'Draw' },
  { id: 'highlight', icon: 'ink_highlighter', label: 'Highlight' },
  { id: 'redaction', icon: 'block', label: 'Redact' },
  { id: 'signature', icon: 'signature', label: 'Signature' },
  { id: 'text', icon: 'text_fields', label: 'Text' }
]

const strokeWidths = [
  { value: 2, label: 'Thin' },
  { value: 4, label: 'Medium' },
  { value: 8, label: 'Thick' }
]

const quickColors = ['#000000', '#FF0000', '#0066FF', '#00AA44', '#FF6600', '#8B00FF']

function selectTool(tool: AnnotationTool) {
  if (tool === 'signature') {
    emit('open-signature')
    return
  }
  emit('update:activeTool', tool)
}

function setStrokeColor(color: string) {
  emit('update:toolSettings', { strokeColor: color })
  showColorPicker.value = false
}

function setStrokeWidth(width: number) {
  emit('update:toolSettings', { strokeWidth: width })
}

const currentStrokeWidthLabel = computed(() => {
  return strokeWidths.find(s => s.value === props.toolSettings.strokeWidth)?.label || 'Medium'
})
</script>

<template>
  <div class="annotation-toolbar flex items-center gap-1 px-3 py-1.5 bg-white border-b border-slate-200 shadow-sm">
    <!-- Tool Buttons -->
    <div class="flex items-center gap-0.5 bg-slate-50 rounded-xl px-1 py-0.5 border border-slate-200">
      <button
        v-for="tool in tools"
        :key="tool.id"
        @click="selectTool(tool.id)"
        class="p-1.5 rounded-lg transition-colors relative group"
        :class="activeTool === tool.id
          ? 'bg-teal text-white shadow-sm'
          : 'text-slate-500 hover:text-teal hover:bg-teal/10'"
        :title="tool.label"
      >
        <span class="material-symbols-outlined text-lg">{{ tool.icon }}</span>
        <!-- Tooltip -->
        <span class="absolute -bottom-7 left-1/2 -translate-x-1/2 px-2 py-0.5 bg-slate-800 text-white text-[10px] rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none z-20">
          {{ tool.label }}
        </span>
      </button>
    </div>

    <!-- Delete selected -->
    <button
      @click="emit('delete-selected')"
      class="p-1.5 rounded-lg transition-colors text-slate-400 hover:text-red-500 hover:bg-red-50"
      title="Delete selected"
    >
      <span class="material-symbols-outlined text-lg">delete</span>
    </button>

    <!-- Divider -->
    <div class="w-px h-6 bg-slate-200 mx-1"></div>

    <!-- Undo / Redo -->
    <button
      @click="emit('undo')"
      :disabled="!canUndo"
      class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-slate-500 hover:text-teal hover:bg-teal/10"
      title="Undo"
    >
      <span class="material-symbols-outlined text-lg">undo</span>
    </button>
    <button
      @click="emit('redo')"
      :disabled="!canRedo"
      class="p-1.5 rounded-lg transition-colors disabled:opacity-30 text-slate-500 hover:text-teal hover:bg-teal/10"
      title="Redo"
    >
      <span class="material-symbols-outlined text-lg">redo</span>
    </button>

    <!-- Divider -->
    <div class="w-px h-6 bg-slate-200 mx-1"></div>

    <!-- Color Picker -->
    <div class="relative" ref="colorPickerRef">
      <button
        @click="showColorPicker = !showColorPicker"
        class="p-1.5 rounded-lg transition-colors text-slate-500 hover:bg-slate-100 flex items-center gap-1"
        title="Stroke color"
      >
        <div
          class="w-5 h-5 rounded-full border-2 border-slate-300"
          :style="{ backgroundColor: toolSettings.strokeColor }"
        ></div>
      </button>

      <!-- Color dropdown -->
      <div
        v-if="showColorPicker"
        class="absolute top-full left-0 mt-1 p-2 bg-white rounded-xl shadow-xl border border-slate-200 z-30"
      >
        <div class="flex gap-1.5">
          <button
            v-for="color in quickColors"
            :key="color"
            @click="setStrokeColor(color)"
            class="w-6 h-6 rounded-full border-2 transition-transform hover:scale-110"
            :class="toolSettings.strokeColor === color ? 'border-teal ring-2 ring-teal/30' : 'border-slate-200'"
            :style="{ backgroundColor: color }"
          ></button>
        </div>
      </div>
    </div>

    <!-- Stroke Width -->
    <div class="flex items-center gap-0.5 bg-slate-50 rounded-lg px-1 py-0.5 border border-slate-100">
      <button
        v-for="sw in strokeWidths"
        :key="sw.value"
        @click="setStrokeWidth(sw.value)"
        class="px-2 py-0.5 rounded text-[10px] font-medium transition-colors"
        :class="toolSettings.strokeWidth === sw.value
          ? 'bg-teal text-white'
          : 'text-slate-400 hover:text-slate-600'"
      >
        {{ sw.label }}
      </button>
    </div>

    <!-- Spacer -->
    <div class="flex-1"></div>

    <!-- Unsaved indicator -->
    <span v-if="hasUnsavedChanges" class="text-[10px] text-amber-500 font-medium flex items-center gap-1">
      <span class="w-1.5 h-1.5 rounded-full bg-amber-500 animate-pulse"></span>
      Unsaved
    </span>

    <!-- Save -->
    <button
      @click="emit('save')"
      :disabled="isSaving || !hasUnsavedChanges"
      class="px-3 py-1.5 bg-teal text-white text-xs font-medium rounded-lg hover:bg-teal/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center gap-1.5"
    >
      <svg v-if="isSaving" class="animate-spin w-3 h-3" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
      </svg>
      <span class="material-symbols-outlined text-sm" v-else>save</span>
      {{ isSaving ? 'Saving...' : 'Save' }}
    </button>

    <!-- Discard -->
    <button
      @click="emit('discard')"
      :disabled="!hasUnsavedChanges"
      class="px-3 py-1.5 text-slate-500 text-xs font-medium rounded-lg hover:bg-slate-100 disabled:opacity-50 transition-colors"
    >
      Discard
    </button>

    <!-- Close annotation mode -->
    <button
      @click="emit('close')"
      class="p-1.5 rounded-lg transition-colors text-slate-400 hover:text-slate-600 hover:bg-slate-100"
      title="Exit annotation mode"
    >
      <span class="material-symbols-outlined text-lg">close</span>
    </button>
  </div>
</template>
