<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted, nextTick } from 'vue'
import { Canvas, PencilBrush, Rect, Textbox, FabricImage } from 'fabric'
import type { AnnotationTool, AnnotationToolSettings } from '@/types'
import { useAnnotations } from '@/composables/useAnnotations'

const props = defineProps<{
  pageNumber: number
  width: number
  height: number
  isAnnotationMode: boolean
  activeTool: AnnotationTool
  toolSettings: AnnotationToolSettings
  annotationData?: string
  readOnly?: boolean
}>()

const emit = defineEmits<{
  modified: [pageNumber: number]
}>()

const canvasRef = ref<HTMLCanvasElement | null>(null)
let fabricCanvas: Canvas | null = null

const { registerCanvas, unregisterCanvas, pushUndoState } = useAnnotations()

// Track drawing state for highlight/redaction rectangles
let isDrawingRect = false
let rectStartX = 0
let rectStartY = 0
let currentRect: Rect | null = null

onMounted(() => {
  nextTick(() => {
    initCanvas()
  })
})

onUnmounted(() => {
  if (fabricCanvas) {
    unregisterCanvas(props.pageNumber)
    fabricCanvas.dispose()
    fabricCanvas = null
  }
})

function initCanvas() {
  if (!canvasRef.value) return

  fabricCanvas = new Canvas(canvasRef.value, {
    width: props.width,
    height: props.height,
    selection: props.isAnnotationMode && props.activeTool === 'select',
    isDrawingMode: false,
    backgroundColor: 'transparent'
  })

  // Load existing annotation data
  if (props.annotationData) {
    try {
      fabricCanvas.loadFromJSON(props.annotationData).then(() => {
        fabricCanvas!.renderAll()
      })
    } catch {
      // Invalid JSON, ignore
    }
  }

  // Register with composable
  registerCanvas(props.pageNumber, fabricCanvas)

  // Setup event listeners
  setupCanvasEvents()

  // Apply current tool
  applyTool(props.activeTool)

  // If read-only, disable all interactions
  if (props.readOnly) {
    fabricCanvas.selection = false
    fabricCanvas.forEachObject(obj => {
      obj.selectable = false
      obj.evented = false
    })
  }
}

function setupCanvasEvents() {
  if (!fabricCanvas) return

  fabricCanvas.on('object:added', () => {
    emit('modified', props.pageNumber)
  })

  fabricCanvas.on('object:modified', () => {
    pushUndoState(props.pageNumber)
    emit('modified', props.pageNumber)
  })

  fabricCanvas.on('path:created', () => {
    pushUndoState(props.pageNumber)
    emit('modified', props.pageNumber)
  })

  // Rectangle drawing for highlight/redaction
  fabricCanvas.on('mouse:down', (opt) => {
    if (props.readOnly || !fabricCanvas) return
    const tool = props.activeTool

    if (tool === 'highlight' || tool === 'redaction') {
      isDrawingRect = true
      const pointer = fabricCanvas.getScenePoint(opt.e)
      rectStartX = pointer.x
      rectStartY = pointer.y

      const fillColor = tool === 'highlight'
        ? props.toolSettings.highlightColor
        : '#000000'
      const opacity = tool === 'highlight' ? 0.3 : 1

      currentRect = new Rect({
        left: rectStartX,
        top: rectStartY,
        width: 0,
        height: 0,
        fill: fillColor,
        opacity: opacity,
        selectable: true,
        strokeWidth: 0
      })
      fabricCanvas.add(currentRect)
    }

    if (tool === 'text') {
      const pointer = fabricCanvas.getScenePoint(opt.e)
      // Only add text if clicking on empty space
      const target = fabricCanvas.findTarget(opt.e)
      if (!target) {
        pushUndoState(props.pageNumber)
        const textbox = new Textbox('Type here...', {
          left: pointer.x,
          top: pointer.y,
          fontSize: props.toolSettings.fontSize,
          fill: props.toolSettings.strokeColor,
          fontFamily: 'Arial',
          width: 200,
          editable: true
        })
        fabricCanvas.add(textbox)
        fabricCanvas.setActiveObject(textbox)
        textbox.enterEditing()
        textbox.selectAll()
      }
    }

    if (tool === 'signature') {
      // Signature placement handled by parent (opens modal, then places image)
    }
  })

  fabricCanvas.on('mouse:move', (opt) => {
    if (!isDrawingRect || !currentRect || !fabricCanvas) return

    const pointer = fabricCanvas.getScenePoint(opt.e)
    const left = Math.min(rectStartX, pointer.x)
    const top = Math.min(rectStartY, pointer.y)
    const width = Math.abs(pointer.x - rectStartX)
    const height = Math.abs(pointer.y - rectStartY)

    currentRect.set({ left, top, width, height })
    fabricCanvas.renderAll()
  })

  fabricCanvas.on('mouse:up', () => {
    if (isDrawingRect && currentRect && fabricCanvas) {
      isDrawingRect = false
      // Remove if too small (accidental click)
      if ((currentRect.width ?? 0) < 5 && (currentRect.height ?? 0) < 5) {
        fabricCanvas.remove(currentRect)
      } else {
        // Update bounding box for hit detection after drawing
        currentRect.setCoords()
        pushUndoState(props.pageNumber)
      }
      currentRect = null
    }
  })
}

function applyTool(tool: AnnotationTool) {
  if (!fabricCanvas) return

  // Reset drawing mode
  fabricCanvas.isDrawingMode = false
  fabricCanvas.selection = false
  fabricCanvas.defaultCursor = 'default'

  // Make objects non-selectable by default
  fabricCanvas.forEachObject(obj => {
    obj.selectable = tool === 'select'
    obj.evented = tool === 'select'
  })

  switch (tool) {
    case 'select':
      fabricCanvas.selection = true
      fabricCanvas.defaultCursor = 'default'
      fabricCanvas.forEachObject(obj => {
        obj.selectable = true
        obj.evented = true
        obj.setCoords()
      })
      break

    case 'freehand':
      fabricCanvas.isDrawingMode = true
      fabricCanvas.freeDrawingBrush = new PencilBrush(fabricCanvas)
      fabricCanvas.freeDrawingBrush.color = props.toolSettings.strokeColor
      fabricCanvas.freeDrawingBrush.width = props.toolSettings.strokeWidth
      break

    case 'highlight':
      fabricCanvas.defaultCursor = 'crosshair'
      break

    case 'redaction':
      fabricCanvas.defaultCursor = 'crosshair'
      break

    case 'text':
      fabricCanvas.defaultCursor = 'text'
      break

    case 'signature':
      fabricCanvas.defaultCursor = 'crosshair'
      break
  }
}

// Place a signature image on the canvas at center
function placeSignature(dataUrl: string) {
  if (!fabricCanvas) return

  pushUndoState(props.pageNumber)

  FabricImage.fromURL(dataUrl).then((img) => {
    // Scale signature to reasonable size (max 200px wide)
    const maxWidth = 200
    const scale = img.width && img.width > maxWidth ? maxWidth / img.width : 1

    img.set({
      left: (props.width / 2) - (((img.width ?? 200) * scale) / 2),
      top: (props.height / 2) - (((img.height ?? 80) * scale) / 2),
      scaleX: scale,
      scaleY: scale,
      selectable: true
    })

    fabricCanvas!.add(img)
    fabricCanvas!.setActiveObject(img)
    fabricCanvas!.renderAll()
  })
}

// Watch for tool changes
watch(() => props.activeTool, (newTool) => {
  applyTool(newTool)
})

// Watch for tool settings changes
watch(() => props.toolSettings, (settings) => {
  if (fabricCanvas && fabricCanvas.isDrawingMode && fabricCanvas.freeDrawingBrush) {
    fabricCanvas.freeDrawingBrush.color = settings.strokeColor
    fabricCanvas.freeDrawingBrush.width = settings.strokeWidth
  }
}, { deep: true })

// Watch for canvas size changes (zoom)
watch([() => props.width, () => props.height], ([newWidth, newHeight]) => {
  if (!fabricCanvas) return

  const oldWidth = fabricCanvas.getWidth()
  const oldHeight = fabricCanvas.getHeight()
  const scaleX = newWidth / oldWidth
  const scaleY = newHeight / oldHeight

  fabricCanvas.setDimensions({ width: newWidth, height: newHeight })

  // Scale all objects
  fabricCanvas.forEachObject(obj => {
    obj.set({
      left: (obj.left ?? 0) * scaleX,
      top: (obj.top ?? 0) * scaleY,
      scaleX: (obj.scaleX ?? 1) * scaleX,
      scaleY: (obj.scaleY ?? 1) * scaleY
    })
    obj.setCoords()
  })

  fabricCanvas.renderAll()
})

// Watch for annotation mode changes
watch(() => props.isAnnotationMode, (isActive) => {
  if (!fabricCanvas) return
  if (!isActive || props.readOnly) {
    fabricCanvas.selection = false
    fabricCanvas.isDrawingMode = false
    fabricCanvas.forEachObject(obj => {
      obj.selectable = false
      obj.evented = false
    })
  } else {
    applyTool(props.activeTool)
  }
})

defineExpose({
  placeSignature,
  getCanvas: () => fabricCanvas
})
</script>

<template>
  <div
    class="annotation-layer absolute inset-0"
    :class="{
      'pointer-events-none': !isAnnotationMode || readOnly,
      'pointer-events-auto': isAnnotationMode && !readOnly
    }"
    :style="{ zIndex: isAnnotationMode ? 10 : 5 }"
  >
    <canvas ref="canvasRef" />
  </div>
</template>

<style scoped>
.annotation-layer {
  position: absolute;
  top: 0;
  left: 0;
}

.annotation-layer :deep(canvas) {
  position: absolute !important;
  top: 0 !important;
  left: 0 !important;
}

/* Upper canvas (interaction layer) */
.annotation-layer :deep(.upper-canvas) {
  position: absolute !important;
  top: 0 !important;
  left: 0 !important;
}

/* Canvas wrapper from Fabric.js */
.annotation-layer :deep(.canvas-container) {
  position: absolute !important;
  top: 0 !important;
  left: 0 !important;
}
</style>
