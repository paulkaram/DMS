<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { scanApi, scanConfigsApi, scanAgentApi } from '@/api/client'
import type { ScanPageItem, ScanProcessResult, ScannerDevice } from '@/types'

interface Props {
  folderId: string
  folderName?: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  close: []
  uploaded: []
}>()

// Wizard state
const currentStep = ref(1)
const stepLabels = ['Capture', 'Pages', 'Settings', 'Process']

// Step 1: Capture / Import
const pages = ref<ScanPageItem[]>([])
const isCameraActive = ref(false)
const isCameraSupported = ref(true)
const videoRef = ref<HTMLVideoElement | null>(null)
const canvasRef = ref<HTMLCanvasElement | null>(null)
const cameraStream = ref<MediaStream | null>(null)
const facingMode = ref<'user' | 'environment'>('environment')
const fileInputRef = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)
const captureFlash = ref(false)

// Scanner Agent
const captureMode = ref<'camera' | 'file' | 'scanner'>('file')
const scannerAgentAvailable = ref(false)
const scannerAgentChecking = ref(false)
const scanners = ref<ScannerDevice[]>([])
const selectedScannerId = ref<string | null>(null)
const selectedScannerDriver = ref('wia')
const scannerDpi = ref(300)
const scannerColorMode = ref('color')
const scannerPageSize = ref('A4')
const scannerPaperSource = ref('flatbed')
const scannerDuplex = ref(false)
const isScanning = ref(false)
const scannerError = ref('')

// Step 3: Settings
interface ScanConfig {
  id: string
  name: string
  description?: string
  resolution: number
  colorMode: string
  outputFormat: string
  enableOCR: boolean
  ocrLanguage: string
  autoDeskew: boolean
  autoCrop: boolean
  removeBlankPages: boolean
  compressionQuality?: number
  targetFolderId?: string
  isDefault: boolean
  isActive: boolean
}
const scanConfigs = ref<ScanConfig[]>([])
const selectedConfigId = ref<string | null>(null)
const documentName = ref('')
const description = ref('')
const enableOCR = ref(true)
const ocrLanguage = ref('eng')
const autoDeskew = ref(true)
const autoCrop = ref(false)
const compressionQuality = ref(85)

// Step 4: Processing
const isProcessing = ref(false)
const uploadProgress = ref(0)
const processingPhase = ref<'uploading' | 'processing' | 'complete' | 'error'>('uploading')
const processResult = ref<ScanProcessResult | null>(null)
const error = ref('')

// Drag-drop reorder
const draggedIndex = ref<number | null>(null)
const dragOverIndex = ref<number | null>(null)

// Computed
const canGoNext = computed(() => {
  if (currentStep.value === 1) return pages.value.length > 0
  if (currentStep.value === 2) return pages.value.length > 0
  if (currentStep.value === 3) return documentName.value.trim().length > 0
  return false
})

// Initialize
onMounted(async () => {
  // Generate default document name
  const now = new Date()
  documentName.value = `Scan_${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}_${String(now.getHours()).padStart(2, '0')}-${String(now.getMinutes()).padStart(2, '0')}`

  // Load scan configs
  try {
    const response = await scanConfigsApi.getAll()
    scanConfigs.value = response.data || []
    const defaultConfig = scanConfigs.value.find((c: ScanConfig) => c.isDefault)
    if (defaultConfig) {
      selectedConfigId.value = defaultConfig.id
      applyConfig(defaultConfig)
    }
  } catch { /* scan configs are optional */ }

  // Check camera support
  isCameraSupported.value = !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia)
})

onUnmounted(() => {
  stopCamera()
  pages.value.forEach(p => URL.revokeObjectURL(p.thumbnailUrl))
})

// Camera functions
async function startCamera() {
  try {
    const stream = await navigator.mediaDevices.getUserMedia({
      video: {
        facingMode: facingMode.value,
        width: { ideal: 1920 },
        height: { ideal: 1080 }
      }
    })
    cameraStream.value = stream
    if (videoRef.value) {
      videoRef.value.srcObject = stream
    }
    isCameraActive.value = true
  } catch {
    isCameraSupported.value = false
  }
}

function stopCamera() {
  if (cameraStream.value) {
    cameraStream.value.getTracks().forEach(t => t.stop())
    cameraStream.value = null
  }
  isCameraActive.value = false
}

async function switchCamera() {
  stopCamera()
  facingMode.value = facingMode.value === 'user' ? 'environment' : 'user'
  await startCamera()
}

function captureFrame() {
  if (!videoRef.value || !canvasRef.value) return
  const video = videoRef.value
  const canvas = canvasRef.value
  canvas.width = video.videoWidth
  canvas.height = video.videoHeight
  const ctx = canvas.getContext('2d')!
  ctx.drawImage(video, 0, 0)

  // Flash effect
  captureFlash.value = true
  setTimeout(() => { captureFlash.value = false }, 200)

  canvas.toBlob((blob) => {
    if (!blob) return
    const file = new File([blob], `scan-page-${pages.value.length + 1}.png`, { type: 'image/png' })
    pages.value.push({
      id: crypto.randomUUID(),
      file,
      thumbnailUrl: URL.createObjectURL(blob),
      rotationDegrees: 0,
      originalIndex: pages.value.length
    })
  }, 'image/png', 0.92)
}

// File import functions
function triggerFileInput() {
  fileInputRef.value?.click()
}

function handleFileSelect(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files) {
    addFiles(Array.from(input.files))
    input.value = ''
  }
}

function handleDrop(e: DragEvent) {
  e.preventDefault()
  isDragging.value = false
  if (e.dataTransfer?.files) {
    addFiles(Array.from(e.dataTransfer.files))
  }
}

function handleDragOver(e: DragEvent) {
  e.preventDefault()
  isDragging.value = true
}

function addFiles(files: File[]) {
  const imageFiles = files.filter(f => f.type.startsWith('image/'))
  for (const file of imageFiles) {
    pages.value.push({
      id: crypto.randomUUID(),
      file,
      thumbnailUrl: URL.createObjectURL(file),
      rotationDegrees: 0,
      originalIndex: pages.value.length
    })
  }
}

// Scanner Agent functions
async function checkScannerAgent() {
  scannerAgentChecking.value = true
  scannerError.value = ''
  try {
    await scanAgentApi.checkStatus()
    scannerAgentAvailable.value = true
    await loadScanners()
  } catch {
    scannerAgentAvailable.value = false
  } finally {
    scannerAgentChecking.value = false
  }
}

async function loadScanners() {
  try {
    const response = await scanAgentApi.getScanners()
    scanners.value = response.data
    if (scanners.value.length > 0 && !selectedScannerId.value) {
      selectedScannerId.value = scanners.value[0].id
      selectedScannerDriver.value = scanners.value[0].driver
    }
  } catch {
    scanners.value = []
  }
}

async function scanFromDevice() {
  if (!selectedScannerId.value) return
  isScanning.value = true
  scannerError.value = ''
  try {
    const response = await scanAgentApi.scan({
      scannerId: selectedScannerId.value,
      driver: selectedScannerDriver.value,
      dpi: scannerDpi.value,
      colorMode: scannerColorMode.value,
      pageSize: scannerPageSize.value,
      paperSource: scannerPaperSource.value,
      duplex: scannerDuplex.value
    })

    const scannedPages = response.data.pages || []
    for (const page of scannedPages) {
      const byteString = atob(page.data)
      const ab = new ArrayBuffer(byteString.length)
      const ia = new Uint8Array(ab)
      for (let i = 0; i < byteString.length; i++) {
        ia[i] = byteString.charCodeAt(i)
      }
      const blob = new Blob([ab], { type: `image/${page.format}` })
      const file = new File([blob], `scan-page-${pages.value.length + 1}.${page.format}`, { type: `image/${page.format}` })
      pages.value.push({
        id: crypto.randomUUID(),
        file,
        thumbnailUrl: URL.createObjectURL(blob),
        rotationDegrees: 0,
        originalIndex: pages.value.length
      })
    }
  } catch (err: any) {
    scannerError.value = err.response?.data?.error || err.message || 'Scan failed'
  } finally {
    isScanning.value = false
  }
}

function onScannerSelected(scannerId: string) {
  selectedScannerId.value = scannerId
  const scanner = scanners.value.find(s => s.id === scannerId)
  if (scanner) selectedScannerDriver.value = scanner.driver
}

// Page management
function rotatePage(index: number) {
  pages.value[index].rotationDegrees = (pages.value[index].rotationDegrees + 90) % 360
}

function deletePage(index: number) {
  URL.revokeObjectURL(pages.value[index].thumbnailUrl)
  pages.value.splice(index, 1)
}

// Drag reorder
function onDragStart(index: number) {
  draggedIndex.value = index
}

function onDragOver(e: DragEvent, index: number) {
  e.preventDefault()
  dragOverIndex.value = index
}

function onDragEnd() {
  if (draggedIndex.value !== null && dragOverIndex.value !== null && draggedIndex.value !== dragOverIndex.value) {
    const [moved] = pages.value.splice(draggedIndex.value, 1)
    pages.value.splice(dragOverIndex.value, 0, moved)
  }
  draggedIndex.value = null
  dragOverIndex.value = null
}

// Settings
function applyConfig(config: ScanConfig) {
  enableOCR.value = config.enableOCR
  ocrLanguage.value = config.ocrLanguage
  autoDeskew.value = config.autoDeskew
  autoCrop.value = config.autoCrop
  compressionQuality.value = config.compressionQuality ?? 85
}

watch(selectedConfigId, (newId) => {
  if (!newId) return
  const config = scanConfigs.value.find(c => c.id === newId)
  if (config) applyConfig(config)
})

// Navigation
function goNext() {
  if (currentStep.value === 1 && pages.value.length > 0) {
    stopCamera()
    currentStep.value = 2
  } else if (currentStep.value === 2) {
    currentStep.value = 3
  } else if (currentStep.value === 3) {
    currentStep.value = 4
    startProcessing()
  }
}

function goBack() {
  if (currentStep.value === 2) {
    currentStep.value = 1
  } else if (currentStep.value === 3) {
    currentStep.value = 2
  }
}

function goToStep1FromStep2() {
  currentStep.value = 1
}

// Processing
async function startProcessing() {
  isProcessing.value = true
  uploadProgress.value = 0
  processingPhase.value = 'uploading'
  error.value = ''

  try {
    const orderedFiles = pages.value.map(p => p.file)
    const pageInstructions = pages.value.map((p, index) => ({
      fileIndex: index,
      rotationDegrees: p.rotationDegrees
    }))

    const response = await scanApi.process(
      orderedFiles,
      {
        scanConfigId: selectedConfigId.value || undefined,
        targetFolderId: props.folderId,
        documentName: documentName.value,
        description: description.value || undefined,
        pages: pageInstructions,
        enableOCR: enableOCR.value,
        ocrLanguage: ocrLanguage.value,
        autoDeskew: autoDeskew.value,
        autoCrop: autoCrop.value,
        compressionQuality: compressionQuality.value
      },
      (progress) => {
        uploadProgress.value = progress
        if (progress >= 100) {
          processingPhase.value = 'processing'
        }
      }
    )

    processResult.value = response.data
    processingPhase.value = 'complete'
    uploadProgress.value = 100
  } catch (err: any) {
    error.value = err.response?.data?.[0] || err.response?.data?.message || 'Scan processing failed'
    processingPhase.value = 'error'
  } finally {
    isProcessing.value = false
  }
}

function retryProcessing() {
  startProcessing()
}

function finishAndClose() {
  emit('uploaded')
  emit('close')
}

// Helpers
function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

const ocrLanguages = [
  { value: 'eng', label: 'English' },
  { value: 'ara', label: 'Arabic' },
  { value: 'fra', label: 'French' },
  { value: 'deu', label: 'German' },
  { value: 'spa', label: 'Spanish' }
]
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4" @click.self="emit('close')">
        <Transition
          enter-active-class="duration-300 ease-out"
          enter-from-class="opacity-0 scale-95 translate-y-4"
          enter-to-class="opacity-100 scale-100 translate-y-0"
          leave-active-class="duration-200 ease-in"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div class="bg-white dark:bg-background-dark rounded-lg shadow-2xl w-full max-w-4xl max-h-[90vh] overflow-hidden flex flex-col ring-1 ring-black/5 dark:ring-white/10">

            <!-- ═══════════════════ HEADER ═══════════════════ -->
            <div class="relative bg-gradient-to-r from-[#0d1117] via-[#0d1117]/95 to-teal p-5 overflow-hidden">
              <!-- Decorative -->
              <div class="absolute top-0 right-0 w-64 h-64 bg-teal/15 rounded-full -translate-y-1/2 translate-x-1/2"></div>
              <div class="absolute bottom-0 left-20 w-24 h-24 bg-teal/10 rounded-full translate-y-1/2"></div>

              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-4">
                  <div class="w-11 h-11 bg-teal/25 backdrop-blur rounded-lg flex items-center justify-center">
                    <span class="material-symbols-outlined text-white text-2xl">document_scanner</span>
                  </div>
                  <div>
                    <h3 class="text-lg font-bold text-white">Scan Document</h3>
                    <p class="text-sm text-white/70 flex items-center gap-1 mt-0.5">
                      <span class="material-symbols-outlined text-xs">folder</span>
                      {{ folderName || 'Select folder' }}
                    </p>
                  </div>
                </div>

                <div class="flex items-center gap-4">
                  <!-- Step Indicator -->
                  <div class="hidden sm:flex items-center gap-1">
                    <template v-for="(label, i) in stepLabels" :key="i">
                      <div class="flex items-center gap-1">
                        <div
                          :class="[
                            'w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold transition-all duration-300 border-2',
                            i + 1 < currentStep
                              ? 'bg-teal border-teal text-white'
                              : i + 1 === currentStep
                                ? 'bg-white/15 border-white text-white'
                                : 'bg-transparent border-white/25 text-white/40'
                          ]"
                        >
                          <span v-if="i + 1 < currentStep" class="material-symbols-outlined text-sm" style="font-variation-settings: 'FILL' 1;">check</span>
                          <span v-else>{{ i + 1 }}</span>
                        </div>
                        <span
                          class="text-[10px] font-semibold uppercase tracking-wider mr-1"
                          :class="i + 1 <= currentStep ? 'text-white/90' : 'text-white/30'"
                        >{{ label }}</span>
                      </div>
                      <div
                        v-if="i < stepLabels.length - 1"
                        class="w-6 h-0.5 rounded-full mx-0.5"
                        :class="i + 1 < currentStep ? 'bg-teal' : 'bg-white/15'"
                      ></div>
                    </template>
                  </div>

                  <button
                    @click="emit('close')"
                    class="w-9 h-9 flex items-center justify-center rounded-lg bg-white/10 hover:bg-white/20 transition-colors"
                  >
                    <span class="material-symbols-outlined text-white text-xl">close</span>
                  </button>
                </div>
              </div>

              <!-- Progress Bar -->
              <div class="mt-4">
                <div class="h-1 bg-white/10 rounded-full overflow-hidden">
                  <div
                    class="h-full bg-teal transition-all duration-500 rounded-full"
                    :style="{ width: ((currentStep - 1) / (stepLabels.length - 1)) * 100 + '%' }"
                  ></div>
                </div>
              </div>
            </div>

            <!-- ═══════════════════ CONTENT ═══════════════════ -->
            <div class="flex-1 overflow-y-auto">

              <!-- ──── STEP 1: CAPTURE / IMPORT ──── -->
              <div v-if="currentStep === 1" class="p-6">
                <!-- Mode Tabs -->
                <div class="flex gap-1 p-1 bg-zinc-100 dark:bg-surface-dark rounded-lg mb-5">
                  <button
                    @click="captureMode = 'file'"
                    class="flex-1 px-3 py-2 text-sm font-medium rounded-lg transition-all flex items-center justify-center gap-1.5"
                    :class="captureMode === 'file' ? 'bg-white dark:bg-border-dark text-zinc-900 dark:text-white shadow-sm' : 'text-zinc-500 hover:text-zinc-700'"
                  >
                    <span class="material-symbols-outlined text-lg">upload_file</span>
                    Import Files
                  </button>
                  <button
                    @click="captureMode = 'camera'"
                    class="flex-1 px-3 py-2 text-sm font-medium rounded-lg transition-all flex items-center justify-center gap-1.5"
                    :class="captureMode === 'camera' ? 'bg-white dark:bg-border-dark text-zinc-900 dark:text-white shadow-sm' : 'text-zinc-500 hover:text-zinc-700'"
                  >
                    <span class="material-symbols-outlined text-lg">photo_camera</span>
                    Camera
                  </button>
                  <button
                    @click="captureMode = 'scanner'; checkScannerAgent()"
                    class="flex-1 px-3 py-2 text-sm font-medium rounded-lg transition-all flex items-center justify-center gap-1.5"
                    :class="captureMode === 'scanner' ? 'bg-white dark:bg-border-dark text-zinc-900 dark:text-white shadow-sm' : 'text-zinc-500 hover:text-zinc-700'"
                  >
                    <span class="material-symbols-outlined text-lg">scanner</span>
                    Scanner
                  </button>
                </div>

                <div class="grid grid-cols-1 gap-5" :class="captureMode !== 'scanner' ? 'md:grid-cols-2' : ''">

                  <!-- Camera Capture -->
                  <div v-if="captureMode === 'camera' || captureMode === 'file'" class="border border-zinc-200 dark:border-border-dark rounded-lg overflow-hidden" :class="captureMode !== 'camera' ? 'hidden md:block' : ''">
                    <div class="px-4 py-3 bg-zinc-50 dark:bg-surface-dark border-b border-zinc-200 dark:border-border-dark">
                      <div class="flex items-center gap-2">
                        <span class="material-symbols-outlined text-teal text-lg">photo_camera</span>
                        <span class="font-semibold text-sm text-zinc-800 dark:text-white">Camera Capture</span>
                      </div>
                    </div>

                    <div class="relative aspect-[4/3] bg-zinc-900 flex items-center justify-center">
                      <video
                        v-show="isCameraActive"
                        ref="videoRef"
                        autoplay
                        playsinline
                        muted
                        class="w-full h-full object-cover"
                      ></video>
                      <canvas ref="canvasRef" class="hidden"></canvas>

                      <!-- Flash overlay -->
                      <div
                        v-if="captureFlash"
                        class="absolute inset-0 bg-white z-10 animate-pulse"
                      ></div>

                      <!-- Camera off state -->
                      <div v-if="!isCameraActive" class="text-center p-6">
                        <div class="w-16 h-16 mx-auto mb-4 rounded-lg bg-zinc-800 flex items-center justify-center">
                          <span class="material-symbols-outlined text-3xl text-zinc-500">videocam_off</span>
                        </div>
                        <p v-if="!isCameraSupported" class="text-sm text-zinc-500 mb-3">Camera not available</p>
                        <button
                          v-if="isCameraSupported"
                          @click="startCamera"
                          class="px-4 py-2 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors inline-flex items-center gap-2"
                        >
                          <span class="material-symbols-outlined text-lg">videocam</span>
                          Start Camera
                        </button>
                      </div>

                      <!-- Camera controls overlay -->
                      <div v-if="isCameraActive" class="absolute bottom-0 inset-x-0 p-3 bg-gradient-to-t from-black/70 to-transparent">
                        <div class="flex items-center justify-center gap-3">
                          <button
                            @click="switchCamera"
                            class="w-10 h-10 rounded-full bg-white/20 backdrop-blur flex items-center justify-center hover:bg-white/30 transition-colors"
                            title="Switch camera"
                          >
                            <span class="material-symbols-outlined text-white text-xl">cameraswitch</span>
                          </button>
                          <button
                            @click="captureFrame"
                            class="w-14 h-14 rounded-full bg-white flex items-center justify-center hover:scale-105 active:scale-95 transition-transform shadow-lg"
                            title="Capture"
                          >
                            <div class="w-12 h-12 rounded-full border-2 border-zinc-300"></div>
                          </button>
                          <button
                            @click="stopCamera"
                            class="w-10 h-10 rounded-full bg-red-500/80 backdrop-blur flex items-center justify-center hover:bg-red-500 transition-colors"
                            title="Stop camera"
                          >
                            <span class="material-symbols-outlined text-white text-xl">stop</span>
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>

                  <!-- File Import -->
                  <div v-if="captureMode === 'file' || captureMode === 'camera'" class="border border-zinc-200 dark:border-border-dark rounded-lg overflow-hidden" :class="captureMode !== 'file' ? 'hidden md:block' : ''">
                    <div class="px-4 py-3 bg-zinc-50 dark:bg-surface-dark border-b border-zinc-200 dark:border-border-dark">
                      <div class="flex items-center gap-2">
                        <span class="material-symbols-outlined text-teal text-lg">upload_file</span>
                        <span class="font-semibold text-sm text-zinc-800 dark:text-white">Import Files</span>
                      </div>
                    </div>

                    <div
                      class="aspect-[4/3] flex flex-col items-center justify-center p-6 transition-all cursor-pointer"
                      :class="isDragging
                        ? 'bg-teal/5 border-2 border-dashed border-teal'
                        : 'bg-zinc-50 dark:bg-surface-dark/50 hover:bg-teal/5'"
                      @dragover="handleDragOver"
                      @dragleave="isDragging = false"
                      @drop="handleDrop"
                      @click="triggerFileInput"
                    >
                      <input
                        ref="fileInputRef"
                        type="file"
                        accept="image/*"
                        multiple
                        class="hidden"
                        @change="handleFileSelect"
                      />

                      <div class="w-16 h-16 mb-4 rounded-lg flex items-center justify-center" :class="isDragging ? 'bg-teal/20' : 'bg-zinc-200 dark:bg-border-dark'">
                        <span class="material-symbols-outlined text-3xl" :class="isDragging ? 'text-teal' : 'text-zinc-400'">
                          {{ isDragging ? 'file_download' : 'add_photo_alternate' }}
                        </span>
                      </div>
                      <p class="font-semibold text-sm text-zinc-700 dark:text-zinc-300 mb-1">
                        {{ isDragging ? 'Drop images here' : 'Drag & drop images' }}
                      </p>
                      <p class="text-xs text-zinc-500">or click to browse</p>
                      <p class="text-[10px] text-zinc-400 mt-2 uppercase tracking-wide">PNG, JPG, TIFF, BMP</p>
                    </div>
                  </div>

                  <!-- Scanner -->
                  <div v-if="captureMode === 'scanner'" class="border border-zinc-200 dark:border-border-dark rounded-lg overflow-hidden">
                    <div class="px-4 py-3 bg-zinc-50 dark:bg-surface-dark border-b border-zinc-200 dark:border-border-dark">
                      <div class="flex items-center gap-2">
                        <span class="material-symbols-outlined text-teal text-lg">scanner</span>
                        <span class="font-semibold text-sm text-zinc-800 dark:text-white">Physical Scanner</span>
                      </div>
                    </div>

                    <div class="p-5">
                      <!-- Checking agent -->
                      <div v-if="scannerAgentChecking" class="flex flex-col items-center justify-center py-10">
                        <span class="material-symbols-outlined animate-spin text-3xl text-teal mb-3">progress_activity</span>
                        <p class="text-sm text-zinc-500">Detecting scan agent...</p>
                      </div>

                      <!-- Agent not available -->
                      <div v-else-if="!scannerAgentAvailable" class="text-center py-8">
                        <div class="w-16 h-16 mx-auto mb-4 rounded-lg bg-amber-50 dark:bg-amber-900/20 flex items-center justify-center">
                          <span class="material-symbols-outlined text-3xl text-amber-500">warning</span>
                        </div>
                        <h5 class="font-semibold text-sm text-zinc-800 dark:text-white mb-1">Scan Agent Not Detected</h5>
                        <p class="text-xs text-zinc-500 mb-4 max-w-xs mx-auto">
                          The DMS Scan Agent must be running on your computer to use a physical scanner.
                        </p>
                        <button
                          @click="checkScannerAgent"
                          class="px-4 py-2 text-sm font-medium text-teal border border-teal rounded-lg hover:bg-teal/5 transition-colors inline-flex items-center gap-1.5"
                        >
                          <span class="material-symbols-outlined text-lg">refresh</span>
                          Retry Detection
                        </button>
                      </div>

                      <!-- Agent available -->
                      <div v-else class="space-y-4">
                        <!-- Scanner selection -->
                        <div>
                          <label class="block text-xs font-semibold text-zinc-600 dark:text-zinc-400 mb-1 uppercase tracking-wide">Scanner</label>
                          <select
                            :value="selectedScannerId"
                            @change="onScannerSelected(($event.target as HTMLSelectElement).value)"
                            class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-zinc-900 dark:text-white text-sm focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none"
                          >
                            <option v-if="scanners.length === 0" value="">No scanners found</option>
                            <option v-for="s in scanners" :key="s.id" :value="s.id">
                              {{ s.name }} ({{ s.driver.toUpperCase() }})
                            </option>
                          </select>
                        </div>

                        <!-- Scan settings row -->
                        <div class="grid grid-cols-2 gap-3">
                          <div>
                            <label class="block text-xs font-semibold text-zinc-600 dark:text-zinc-400 mb-1">DPI</label>
                            <select v-model.number="scannerDpi" class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-sm text-zinc-900 dark:text-white outline-none">
                              <option :value="150">150 (Draft)</option>
                              <option :value="200">200</option>
                              <option :value="300">300 (Standard)</option>
                              <option :value="600">600 (High)</option>
                            </select>
                          </div>
                          <div>
                            <label class="block text-xs font-semibold text-zinc-600 dark:text-zinc-400 mb-1">Color</label>
                            <select v-model="scannerColorMode" class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-sm text-zinc-900 dark:text-white outline-none">
                              <option value="color">Color</option>
                              <option value="grayscale">Grayscale</option>
                              <option value="bw">Black & White</option>
                            </select>
                          </div>
                          <div>
                            <label class="block text-xs font-semibold text-zinc-600 dark:text-zinc-400 mb-1">Page Size</label>
                            <select v-model="scannerPageSize" class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-sm text-zinc-900 dark:text-white outline-none">
                              <option value="A4">A4</option>
                              <option value="A3">A3</option>
                              <option value="Letter">Letter</option>
                              <option value="Legal">Legal</option>
                            </select>
                          </div>
                          <div>
                            <label class="block text-xs font-semibold text-zinc-600 dark:text-zinc-400 mb-1">Source</label>
                            <select v-model="scannerPaperSource" class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-sm text-zinc-900 dark:text-white outline-none">
                              <option value="flatbed">Flatbed</option>
                              <option value="feeder">Document Feeder</option>
                              <option value="auto">Auto</option>
                            </select>
                          </div>
                        </div>

                        <!-- Duplex toggle -->
                        <label v-if="scannerPaperSource === 'feeder'" class="flex items-center gap-2 cursor-pointer">
                          <input v-model="scannerDuplex" type="checkbox" class="w-4 h-4 rounded border-zinc-300 text-teal focus:ring-teal" />
                          <span class="text-sm text-zinc-700 dark:text-zinc-300">Duplex (both sides)</span>
                        </label>

                        <!-- Error -->
                        <p v-if="scannerError" class="text-sm text-red-500 bg-red-50 dark:bg-red-900/20 rounded-lg px-3 py-2">{{ scannerError }}</p>

                        <!-- Scan button -->
                        <button
                          @click="scanFromDevice"
                          :disabled="!selectedScannerId || isScanning"
                          class="w-full px-5 py-3 bg-teal text-white text-sm font-semibold rounded-lg hover:bg-teal/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors inline-flex items-center justify-center gap-2"
                        >
                          <span v-if="isScanning" class="material-symbols-outlined animate-spin text-xl">progress_activity</span>
                          <span v-else class="material-symbols-outlined text-xl">scanner</span>
                          {{ isScanning ? 'Scanning...' : 'Scan Now' }}
                        </button>

                        <button
                          @click="loadScanners"
                          class="w-full text-xs text-zinc-500 hover:text-teal transition-colors flex items-center justify-center gap-1"
                        >
                          <span class="material-symbols-outlined text-sm">refresh</span>
                          Refresh scanner list
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Page Thumbnails Strip -->
                <div v-if="pages.length > 0" class="mt-5">
                  <div class="flex items-center gap-2 mb-3">
                    <span class="material-symbols-outlined text-teal text-lg">collections</span>
                    <span class="text-sm font-semibold text-zinc-700 dark:text-white">
                      {{ pages.length }} page{{ pages.length !== 1 ? 's' : '' }} captured
                    </span>
                  </div>
                  <div class="flex gap-2.5 overflow-x-auto pb-2">
                    <div
                      v-for="(page, index) in pages"
                      :key="page.id"
                      class="relative w-16 h-20 shrink-0 rounded-lg overflow-hidden border-2 border-zinc-200 dark:border-border-dark group"
                    >
                      <img
                        :src="page.thumbnailUrl"
                        class="w-full h-full object-cover"
                        :style="{ transform: `rotate(${page.rotationDegrees}deg)` }"
                      />
                      <div class="absolute top-0.5 left-0.5 bg-[#0d1117]/80 text-white text-[9px] font-bold px-1 py-0.5 rounded">
                        {{ index + 1 }}
                      </div>
                      <button
                        @click.stop="deletePage(index)"
                        class="absolute top-0.5 right-0.5 w-4 h-4 bg-red-500 text-white rounded-full flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity"
                      >
                        <span class="material-symbols-outlined" style="font-size: 10px;">close</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- ──── STEP 2: PAGE MANAGEMENT ──── -->
              <div v-else-if="currentStep === 2" class="p-6">
                <div class="flex items-center justify-between mb-4">
                  <div class="flex items-center gap-2">
                    <span class="material-symbols-outlined text-teal">auto_awesome_mosaic</span>
                    <h4 class="font-semibold text-zinc-800 dark:text-white">Arrange Pages</h4>
                    <span class="text-xs text-zinc-500">(drag to reorder)</span>
                  </div>
                  <button
                    @click="goToStep1FromStep2"
                    class="text-sm text-teal font-medium hover:text-teal/80 transition-colors inline-flex items-center gap-1"
                  >
                    <span class="material-symbols-outlined text-lg">add</span>
                    Add More
                  </button>
                </div>

                <div class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-3 max-h-[420px] overflow-y-auto pr-1">
                  <div
                    v-for="(page, index) in pages"
                    :key="page.id"
                    draggable="true"
                    @dragstart="onDragStart(index)"
                    @dragover="(e) => onDragOver(e, index)"
                    @dragend="onDragEnd"
                    class="relative group rounded-lg overflow-hidden cursor-grab active:cursor-grabbing aspect-[3/4] transition-all duration-200"
                    :class="[
                      dragOverIndex === index && draggedIndex !== index
                        ? 'ring-2 ring-teal ring-offset-2 scale-[1.02]'
                        : 'border-2 border-zinc-200 dark:border-border-dark hover:border-teal/50',
                      draggedIndex === index ? 'opacity-40 scale-95' : ''
                    ]"
                  >
                    <img
                      :src="page.thumbnailUrl"
                      class="w-full h-full object-cover pointer-events-none transition-transform"
                      :style="{ transform: `rotate(${page.rotationDegrees}deg)` }"
                    />

                    <!-- Page number -->
                    <div class="absolute top-1.5 left-1.5 bg-[#0d1117]/85 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-md">
                      {{ index + 1 }}
                    </div>

                    <!-- Action overlay -->
                    <div class="absolute bottom-0 inset-x-0 bg-gradient-to-t from-black/70 via-black/40 to-transparent pt-8 pb-1.5 px-1.5 opacity-0 group-hover:opacity-100 transition-opacity">
                      <div class="flex justify-center gap-1.5">
                        <button
                          @click.stop="rotatePage(index)"
                          class="w-7 h-7 rounded-lg bg-white/20 backdrop-blur flex items-center justify-center hover:bg-white/40 transition-colors"
                          title="Rotate 90°"
                        >
                          <span class="material-symbols-outlined text-white text-sm">rotate_right</span>
                        </button>
                        <button
                          @click.stop="deletePage(index)"
                          class="w-7 h-7 rounded-lg bg-red-500/70 backdrop-blur flex items-center justify-center hover:bg-red-500 transition-colors"
                          title="Delete page"
                        >
                          <span class="material-symbols-outlined text-white text-sm">delete</span>
                        </button>
                      </div>
                    </div>
                  </div>
                </div>

                <p v-if="pages.length === 0" class="text-center text-zinc-500 py-12">
                  No pages yet. Go back to capture or import images.
                </p>
              </div>

              <!-- ──── STEP 3: SETTINGS ──── -->
              <div v-else-if="currentStep === 3" class="p-6 space-y-5">
                <!-- Scan Config Profile -->
                <div v-if="scanConfigs.length > 0">
                  <label class="block text-sm font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Scan Profile</label>
                  <select
                    v-model="selectedConfigId"
                    class="w-full px-3 py-2.5 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-zinc-900 dark:text-white text-sm focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
                  >
                    <option :value="null">-- Custom Settings --</option>
                    <option v-for="config in scanConfigs" :key="config.id" :value="config.id">
                      {{ config.name }}{{ config.isDefault ? ' (Default)' : '' }}
                    </option>
                  </select>
                </div>

                <!-- Document Name & Description -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-sm font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">
                      Document Name <span class="text-red-500">*</span>
                    </label>
                    <input
                      v-model="documentName"
                      type="text"
                      class="w-full px-3 py-2.5 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-zinc-900 dark:text-white text-sm focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
                      placeholder="Enter document name..."
                    />
                  </div>
                  <div>
                    <label class="block text-sm font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Target Folder</label>
                    <div class="px-3 py-2.5 border border-zinc-200 dark:border-border-dark rounded-lg bg-zinc-50 dark:bg-surface-dark/50 text-sm text-zinc-600 dark:text-zinc-400 flex items-center gap-2">
                      <span class="material-symbols-outlined text-teal text-lg">folder</span>
                      {{ folderName || 'Current folder' }}
                    </div>
                  </div>
                </div>

                <div>
                  <label class="block text-sm font-semibold text-zinc-700 dark:text-zinc-300 mb-1.5">Description</label>
                  <textarea
                    v-model="description"
                    rows="2"
                    class="w-full px-3 py-2.5 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-zinc-900 dark:text-white text-sm focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all resize-none"
                    placeholder="Optional description..."
                  ></textarea>
                </div>

                <!-- Processing Options -->
                <div class="border border-zinc-200 dark:border-border-dark rounded-lg overflow-hidden">
                  <div class="px-4 py-3 bg-zinc-50 dark:bg-surface-dark border-b border-zinc-200 dark:border-border-dark">
                    <span class="font-semibold text-sm text-zinc-700 dark:text-white flex items-center gap-2">
                      <span class="material-symbols-outlined text-teal text-lg">tune</span>
                      Processing Options
                    </span>
                  </div>
                  <div class="p-4 space-y-4">
                    <!-- Toggle Row -->
                    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
                      <!-- Enable OCR -->
                      <label class="flex items-center gap-3 cursor-pointer group">
                        <div class="relative">
                          <input v-model="enableOCR" type="checkbox" class="sr-only peer" />
                          <div class="w-10 h-5.5 bg-zinc-200 dark:bg-zinc-600 rounded-full peer-checked:bg-teal transition-colors"></div>
                          <div class="absolute top-0.5 left-0.5 w-4.5 h-4.5 bg-white rounded-full shadow transition-transform peer-checked:translate-x-[18px]" style="width: 18px; height: 18px;"></div>
                        </div>
                        <div>
                          <span class="text-sm font-medium text-zinc-700 dark:text-white">OCR</span>
                          <p class="text-[10px] text-zinc-500">Extract text</p>
                        </div>
                      </label>

                      <!-- Auto Deskew -->
                      <label class="flex items-center gap-3 cursor-pointer group">
                        <div class="relative">
                          <input v-model="autoDeskew" type="checkbox" class="sr-only peer" />
                          <div class="w-10 h-5.5 bg-zinc-200 dark:bg-zinc-600 rounded-full peer-checked:bg-teal transition-colors"></div>
                          <div class="absolute top-0.5 left-0.5 bg-white rounded-full shadow transition-transform peer-checked:translate-x-[18px]" style="width: 18px; height: 18px;"></div>
                        </div>
                        <div>
                          <span class="text-sm font-medium text-zinc-700 dark:text-white">Deskew</span>
                          <p class="text-[10px] text-zinc-500">Straighten pages</p>
                        </div>
                      </label>

                      <!-- Auto Crop -->
                      <label class="flex items-center gap-3 cursor-pointer group">
                        <div class="relative">
                          <input v-model="autoCrop" type="checkbox" class="sr-only peer" />
                          <div class="w-10 h-5.5 bg-zinc-200 dark:bg-zinc-600 rounded-full peer-checked:bg-teal transition-colors"></div>
                          <div class="absolute top-0.5 left-0.5 bg-white rounded-full shadow transition-transform peer-checked:translate-x-[18px]" style="width: 18px; height: 18px;"></div>
                        </div>
                        <div>
                          <span class="text-sm font-medium text-zinc-700 dark:text-white">Crop</span>
                          <p class="text-[10px] text-zinc-500">Trim borders</p>
                        </div>
                      </label>
                    </div>

                    <!-- OCR Language -->
                    <div v-if="enableOCR" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                      <div>
                        <label class="block text-xs font-semibold text-zinc-600 dark:text-zinc-400 mb-1 uppercase tracking-wide">OCR Language</label>
                        <select
                          v-model="ocrLanguage"
                          class="w-full px-3 py-2 border border-zinc-200 dark:border-border-dark rounded-lg bg-white dark:bg-surface-dark text-zinc-900 dark:text-white text-sm focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all"
                        >
                          <option v-for="lang in ocrLanguages" :key="lang.value" :value="lang.value">
                            {{ lang.label }}
                          </option>
                        </select>
                      </div>
                      <div>
                        <label class="block text-xs font-semibold text-zinc-600 dark:text-zinc-400 mb-1 uppercase tracking-wide">
                          Compression Quality: {{ compressionQuality }}%
                        </label>
                        <input
                          v-model.number="compressionQuality"
                          type="range"
                          min="50"
                          max="100"
                          step="5"
                          class="w-full mt-1.5 accent-teal"
                        />
                        <div class="flex justify-between text-[10px] text-zinc-400 mt-0.5">
                          <span>Smaller file</span>
                          <span>Higher quality</span>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <!-- Summary -->
                <div class="bg-zinc-50 dark:bg-surface-dark/50 rounded-lg p-4 flex items-center gap-4">
                  <div class="w-10 h-10 rounded-lg bg-teal/10 flex items-center justify-center shrink-0">
                    <span class="material-symbols-outlined text-teal">summarize</span>
                  </div>
                  <div class="flex-1 grid grid-cols-3 gap-4 text-center">
                    <div>
                      <p class="text-lg font-bold text-zinc-900 dark:text-white">{{ pages.length }}</p>
                      <p class="text-[10px] text-zinc-500 uppercase tracking-wide">Pages</p>
                    </div>
                    <div>
                      <p class="text-lg font-bold text-zinc-900 dark:text-white">PDF</p>
                      <p class="text-[10px] text-zinc-500 uppercase tracking-wide">Output</p>
                    </div>
                    <div>
                      <p class="text-lg font-bold" :class="enableOCR ? 'text-teal' : 'text-zinc-400'">
                        {{ enableOCR ? 'On' : 'Off' }}
                      </p>
                      <p class="text-[10px] text-zinc-500 uppercase tracking-wide">OCR</p>
                    </div>
                  </div>
                </div>
              </div>

              <!-- ──── STEP 4: PROCESSING ──── -->
              <div v-else-if="currentStep === 4" class="p-6">
                <div class="max-w-md mx-auto py-8">

                  <!-- Uploading / Processing -->
                  <div v-if="processingPhase === 'uploading' || processingPhase === 'processing'" class="text-center">
                    <div class="relative w-24 h-24 mx-auto mb-6">
                      <svg class="w-24 h-24 -rotate-90" viewBox="0 0 100 100">
                        <circle cx="50" cy="50" r="42" fill="none" stroke-width="6" class="stroke-zinc-200 dark:stroke-zinc-700" />
                        <circle
                          cx="50" cy="50" r="42" fill="none" stroke-width="6"
                          class="stroke-teal transition-all duration-500"
                          :stroke-dasharray="264"
                          :stroke-dashoffset="264 - (264 * (processingPhase === 'processing' ? 85 : uploadProgress)) / 100"
                          stroke-linecap="round"
                        />
                      </svg>
                      <div class="absolute inset-0 flex items-center justify-center">
                        <span class="material-symbols-outlined text-3xl text-teal animate-pulse">
                          {{ processingPhase === 'uploading' ? 'cloud_upload' : 'psychology' }}
                        </span>
                      </div>
                    </div>

                    <h4 class="text-lg font-bold text-zinc-900 dark:text-white mb-2">
                      {{ processingPhase === 'uploading' ? 'Uploading Images...' : 'Processing OCR & Building PDF...' }}
                    </h4>
                    <p class="text-sm text-zinc-500">
                      {{ processingPhase === 'uploading'
                        ? `${uploadProgress}% uploaded`
                        : 'This may take a moment for large documents' }}
                    </p>
                  </div>

                  <!-- Complete -->
                  <div v-else-if="processingPhase === 'complete' && processResult" class="text-center">
                    <div class="w-20 h-20 mx-auto mb-5 rounded-lg bg-emerald-500/10 flex items-center justify-center">
                      <span class="material-symbols-outlined text-5xl text-emerald-500" style="font-variation-settings: 'FILL' 1;">check_circle</span>
                    </div>
                    <h4 class="text-xl font-bold text-zinc-900 dark:text-white mb-2">Scan Complete!</h4>
                    <p class="text-sm text-zinc-500 mb-6">Your document has been created successfully</p>

                    <div class="bg-zinc-50 dark:bg-surface-dark rounded-lg p-5 text-left space-y-3">
                      <div class="flex items-center gap-3">
                        <span class="material-symbols-outlined text-red-500 text-xl">picture_as_pdf</span>
                        <div class="flex-1 min-w-0">
                          <p class="font-semibold text-sm text-zinc-900 dark:text-white truncate">{{ processResult.documentName }}.pdf</p>
                          <p class="text-xs text-zinc-500">{{ formatFileSize(processResult.fileSize) }}</p>
                        </div>
                      </div>
                      <div class="grid grid-cols-3 gap-3 pt-2 border-t border-zinc-200 dark:border-border-dark">
                        <div class="text-center">
                          <p class="text-lg font-bold text-zinc-900 dark:text-white">{{ processResult.pageCount }}</p>
                          <p class="text-[10px] text-zinc-500 uppercase">Pages</p>
                        </div>
                        <div class="text-center">
                          <p class="text-lg font-bold text-zinc-900 dark:text-white">{{ formatFileSize(processResult.fileSize) }}</p>
                          <p class="text-[10px] text-zinc-500 uppercase">Size</p>
                        </div>
                        <div class="text-center">
                          <p class="text-lg font-bold" :class="processResult.ocrApplied ? 'text-teal' : 'text-zinc-400'">
                            {{ processResult.ocrApplied ? 'Yes' : 'No' }}
                          </p>
                          <p class="text-[10px] text-zinc-500 uppercase">OCR</p>
                        </div>
                      </div>
                    </div>
                  </div>

                  <!-- Error -->
                  <div v-else-if="processingPhase === 'error'" class="text-center">
                    <div class="w-20 h-20 mx-auto mb-5 rounded-lg bg-red-500/10 flex items-center justify-center">
                      <span class="material-symbols-outlined text-5xl text-red-500">error</span>
                    </div>
                    <h4 class="text-xl font-bold text-zinc-900 dark:text-white mb-2">Processing Failed</h4>
                    <p class="text-sm text-red-500 mb-6">{{ error }}</p>
                    <button
                      @click="retryProcessing"
                      class="px-5 py-2.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors inline-flex items-center gap-2"
                    >
                      <span class="material-symbols-outlined text-lg">refresh</span>
                      Retry
                    </button>
                  </div>
                </div>
              </div>

            </div>

            <!-- ═══════════════════ FOOTER ═══════════════════ -->
            <div
              v-if="currentStep < 4 || processingPhase === 'complete'"
              class="px-6 py-4 border-t border-zinc-200 dark:border-border-dark bg-zinc-50 dark:bg-surface-dark/50 flex items-center justify-between"
            >
              <button
                @click="emit('close')"
                class="px-4 py-2.5 text-sm font-medium text-zinc-600 dark:text-zinc-400 hover:text-zinc-900 dark:hover:text-white transition-colors"
              >
                Cancel
              </button>

              <div class="flex items-center gap-3">
                <button
                  v-if="currentStep > 1 && currentStep < 4"
                  @click="goBack"
                  class="px-4 py-2.5 text-sm font-medium text-zinc-600 dark:text-zinc-400 border border-zinc-200 dark:border-border-dark rounded-lg hover:bg-zinc-100 dark:hover:bg-border-dark transition-colors inline-flex items-center gap-1.5"
                >
                  <span class="material-symbols-outlined text-lg">arrow_back</span>
                  Back
                </button>

                <button
                  v-if="currentStep < 3"
                  @click="goNext"
                  :disabled="!canGoNext"
                  class="px-5 py-2.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors inline-flex items-center gap-1.5"
                >
                  Next
                  <span class="material-symbols-outlined text-lg">arrow_forward</span>
                </button>

                <button
                  v-else-if="currentStep === 3"
                  @click="goNext"
                  :disabled="!canGoNext"
                  class="px-5 py-2.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 disabled:opacity-40 disabled:cursor-not-allowed transition-colors inline-flex items-center gap-1.5"
                >
                  <span class="material-symbols-outlined text-lg">document_scanner</span>
                  Process & Upload
                </button>

                <button
                  v-else-if="processingPhase === 'complete'"
                  @click="finishAndClose"
                  class="px-5 py-2.5 bg-teal text-white text-sm font-medium rounded-lg hover:bg-teal/90 transition-colors inline-flex items-center gap-1.5"
                >
                  <span class="material-symbols-outlined text-lg" style="font-variation-settings: 'FILL' 1;">check_circle</span>
                  Done
                </button>
              </div>
            </div>

          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
/* Toggle switch sizing fix */
input[type="checkbox"]:checked + div {
  background-color: var(--color-teal, #00ae8c);
}
input[type="checkbox"]:checked + div + div {
  transform: translateX(18px);
}

/* Smooth drag feedback */
[draggable="true"] {
  touch-action: none;
}

/* Range input track */
input[type="range"] {
  -webkit-appearance: none;
  appearance: none;
  height: 6px;
  border-radius: 3px;
  background: #e2e8f0;
}
input[type="range"]::-webkit-slider-thumb {
  -webkit-appearance: none;
  appearance: none;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: #00ae8c;
  cursor: pointer;
  border: 2px solid white;
  box-shadow: 0 1px 3px rgba(0,0,0,0.2);
}

/* Scrollbar styling for page grid */
.overflow-y-auto::-webkit-scrollbar {
  width: 4px;
}
.overflow-y-auto::-webkit-scrollbar-track {
  background: transparent;
}
.overflow-y-auto::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 2px;
}
</style>
