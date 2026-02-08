import { ref, reactive } from 'vue'
import type { AnnotationTool, AnnotationToolSettings, DocumentAnnotation } from '@/types'
import { documentAnnotationsApi } from '@/api/client'
import { type Canvas, FabricImage } from 'fabric'

// Annotation mode state
const isAnnotationMode = ref(false)
const activeTool = ref<AnnotationTool>('select')
const hasUnsavedChanges = ref(false)
const isSaving = ref(false)
const canAnnotate = ref(false) // user has Write permission

const toolSettings = reactive<AnnotationToolSettings>({
  strokeColor: '#000000',
  strokeWidth: 3,
  highlightColor: '#FFFF00',
  fontSize: 16
})

// Canvas instances per page (managed by PdfPageAnnotationLayer)
const canvasInstances = new Map<number, Canvas>()

// Annotation data per page (Fabric.js JSON strings)
const annotationDataMap = reactive<Map<number, string>>(new Map())

// Undo/redo stacks per page
const undoStacks = new Map<number, string[]>()
const redoStacks = new Map<number, string[]>()
const canUndo = ref(false)
const canRedo = ref(false)

// Current document
const currentDocumentId = ref<string | null>(null)
const existingAnnotations = ref<DocumentAnnotation[]>([])

export function useAnnotations() {
  function registerCanvas(pageNumber: number, canvas: Canvas) {
    canvasInstances.set(pageNumber, canvas)
  }

  function unregisterCanvas(pageNumber: number) {
    // Serialize state before unregistering (for virtualization survival)
    const canvas = canvasInstances.get(pageNumber)
    if (canvas) {
      const json = JSON.stringify(canvas.toJSON())
      annotationDataMap.set(pageNumber, json)
    }
    canvasInstances.delete(pageNumber)
  }

  function getCanvasForPage(pageNumber: number): Canvas | undefined {
    return canvasInstances.get(pageNumber)
  }

  async function loadAnnotations(documentId: string) {
    currentDocumentId.value = documentId
    try {
      const response = await documentAnnotationsApi.getByDocument(documentId)
      existingAnnotations.value = response.data || []

      // Populate annotation data map
      annotationDataMap.clear()
      for (const ann of existingAnnotations.value) {
        annotationDataMap.set(ann.pageNumber, ann.annotationData)
      }
    } catch {
      existingAnnotations.value = []
    }
  }

  async function saveAnnotations() {
    if (!currentDocumentId.value) return
    isSaving.value = true

    try {
      // Collect current state from all canvases
      const pages: Array<{ pageNumber: number; annotationData: string }> = []

      // First, serialize all live canvases
      for (const [pageNum, canvas] of canvasInstances) {
        const json = JSON.stringify(canvas.toJSON())
        annotationDataMap.set(pageNum, json)
      }

      // Then collect all pages that have annotation data
      for (const [pageNum, data] of annotationDataMap) {
        if (data && data !== '{"version":"","objects":[]}' && data !== '{"objects":[]}') {
          pages.push({ pageNumber: pageNum, annotationData: data })
        }
      }

      if (pages.length > 0) {
        await documentAnnotationsApi.save(currentDocumentId.value, { pages })
      }

      hasUnsavedChanges.value = false
    } finally {
      isSaving.value = false
    }
  }

  function discardChanges() {
    // Restore from existing annotations
    annotationDataMap.clear()
    for (const ann of existingAnnotations.value) {
      annotationDataMap.set(ann.pageNumber, ann.annotationData)
    }

    // Reload all live canvases
    for (const [pageNum, canvas] of canvasInstances) {
      const data = annotationDataMap.get(pageNum)
      if (data) {
        canvas.loadFromJSON(data).then(() => {
          canvas.renderAll()
        })
      } else {
        canvas.clear()
        canvas.renderAll()
      }
    }

    // Clear undo/redo stacks
    undoStacks.clear()
    redoStacks.clear()
    canUndo.value = false
    canRedo.value = false
    hasUnsavedChanges.value = false
  }

  function enterAnnotationMode() {
    isAnnotationMode.value = true
    activeTool.value = 'select'
  }

  function exitAnnotationMode() {
    isAnnotationMode.value = false
    activeTool.value = 'select'
    // Clear canvases
    canvasInstances.clear()
    undoStacks.clear()
    redoStacks.clear()
    canUndo.value = false
    canRedo.value = false
  }

  function setTool(tool: AnnotationTool) {
    activeTool.value = tool
  }

  function pushUndoState(pageNumber: number) {
    const canvas = canvasInstances.get(pageNumber)
    if (!canvas) return

    if (!undoStacks.has(pageNumber)) undoStacks.set(pageNumber, [])
    const stack = undoStacks.get(pageNumber)!
    stack.push(JSON.stringify(canvas.toJSON()))

    // Limit stack size
    if (stack.length > 50) stack.shift()

    // Clear redo stack on new action
    redoStacks.set(pageNumber, [])

    canUndo.value = true
    canRedo.value = false
    hasUnsavedChanges.value = true
  }

  function undo() {
    // Find active page from any canvas
    const pageNumber = findActivePageNumber()
    if (pageNumber === null) return

    const stack = undoStacks.get(pageNumber)
    if (!stack || stack.length === 0) return

    const canvas = canvasInstances.get(pageNumber)
    if (!canvas) return

    // Save current state to redo stack
    if (!redoStacks.has(pageNumber)) redoStacks.set(pageNumber, [])
    redoStacks.get(pageNumber)!.push(JSON.stringify(canvas.toJSON()))

    const prevState = stack.pop()!
    canvas.loadFromJSON(prevState).then(() => {
      canvas.renderAll()
    })

    canUndo.value = stack.length > 0
    canRedo.value = true
  }

  function redo() {
    const pageNumber = findActivePageNumber()
    if (pageNumber === null) return

    const stack = redoStacks.get(pageNumber)
    if (!stack || stack.length === 0) return

    const canvas = canvasInstances.get(pageNumber)
    if (!canvas) return

    // Save current state to undo stack
    if (!undoStacks.has(pageNumber)) undoStacks.set(pageNumber, [])
    undoStacks.get(pageNumber)!.push(JSON.stringify(canvas.toJSON()))

    const nextState = stack.pop()!
    canvas.loadFromJSON(nextState).then(() => {
      canvas.renderAll()
    })

    canUndo.value = true
    canRedo.value = stack.length > 0
  }

  function findActivePageNumber(): number | null {
    // Return the first page that has a canvas (simplistic approach)
    for (const [pageNum] of canvasInstances) {
      return pageNum
    }
    return null
  }

  function deleteSelected() {
    for (const [, canvas] of canvasInstances) {
      const active = canvas.getActiveObjects()
      if (active.length > 0) {
        active.forEach(obj => canvas.remove(obj))
        canvas.discardActiveObject()
        canvas.renderAll()
        hasUnsavedChanges.value = true
        return
      }
    }
  }

  function placeSignatureOnPage(pageNumber: number, dataUrl: string): boolean {
    const canvas = canvasInstances.get(pageNumber)
    if (!canvas) return false

    pushUndoState(pageNumber)

    FabricImage.fromURL(dataUrl).then((img) => {
      const maxWidth = 200
      const scale = img.width && img.width > maxWidth ? maxWidth / img.width : 1

      img.set({
        left: (canvas.getWidth() / 2) - (((img.width ?? 200) * scale) / 2),
        top: (canvas.getHeight() / 2) - (((img.height ?? 80) * scale) / 2),
        scaleX: scale,
        scaleY: scale,
        selectable: true
      })

      canvas.add(img)
      canvas.setActiveObject(img)
      canvas.renderAll()
    })

    return true
  }

  function cleanup() {
    isAnnotationMode.value = false
    activeTool.value = 'select'
    canvasInstances.clear()
    annotationDataMap.clear()
    undoStacks.clear()
    redoStacks.clear()
    canUndo.value = false
    canRedo.value = false
    hasUnsavedChanges.value = false
    currentDocumentId.value = null
    existingAnnotations.value = []
    canAnnotate.value = false
  }

  return {
    // State
    isAnnotationMode,
    activeTool,
    toolSettings,
    hasUnsavedChanges,
    isSaving,
    canUndo,
    canRedo,
    canAnnotate,
    annotationDataMap,
    existingAnnotations,

    // Canvas management
    registerCanvas,
    unregisterCanvas,
    getCanvasForPage,

    // Operations
    loadAnnotations,
    saveAnnotations,
    discardChanges,
    enterAnnotationMode,
    exitAnnotationMode,
    setTool,
    pushUndoState,
    undo,
    redo,
    deleteSelected,
    placeSignatureOnPage,
    cleanup
  }
}
