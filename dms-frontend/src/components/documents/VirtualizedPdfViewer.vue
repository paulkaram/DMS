<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted, nextTick } from 'vue'
import VuePdfEmbed from 'vue-pdf-embed'
import 'vue-pdf-embed/dist/styles/annotationLayer.css'
import 'vue-pdf-embed/dist/styles/textLayer.css'

const props = defineProps<{
  source: string
  width?: number
  zoom?: number
  rotation?: number
  isDarkMode?: boolean
}>()

const emit = defineEmits<{
  loaded: [{ numPages: number }]
  'page-change': [page: number]
  error: [error: Error]
}>()

// State
const containerRef = ref<HTMLElement | null>(null)
const totalPages = ref(0)
const currentPage = ref(1)
const isLoading = ref(true)
const error = ref<string | null>(null)

// Track which pages are visible
const visiblePages = ref<Set<number>>(new Set())

// Intersection observer
let observer: IntersectionObserver | null = null

// Computed
const effectiveWidth = computed(() => {
  const base = props.width || 700
  const zoomFactor = (props.zoom || 100) / 100
  return Math.round(base * zoomFactor)
})

// Page placeholders with estimated height (will be updated after render)
const pageHeights = ref<Map<number, number>>(new Map())
const estimatedPageHeight = computed(() => effectiveWidth.value * 1.414) // A4 ratio

function getPageHeight(pageNum: number): number {
  return pageHeights.value.get(pageNum) || estimatedPageHeight.value
}

// Handle PDF loaded
function handlePdfLoaded(pdfProxy: any) {
  totalPages.value = pdfProxy.numPages
  isLoading.value = false
  emit('loaded', { numPages: pdfProxy.numPages })

  // Setup observer after DOM updates
  nextTick(() => {
    setupIntersectionObserver()
  })
}

// Handle render error
function handleRenderError(err: Error) {
  error.value = 'Failed to render PDF'
  emit('error', err)
}

// Handle page rendered - capture actual height
function handlePageRendered(args: any) {
  if (args?.page?.view) {
    const pageNum = args.page.pageNumber
    const viewport = args.page.getViewport({ scale: 1 })
    const scale = effectiveWidth.value / viewport.width
    const height = viewport.height * scale
    pageHeights.value.set(pageNum, height)
  }
}

// Setup intersection observer
function setupIntersectionObserver() {
  if (observer) {
    observer.disconnect()
  }

  if (!containerRef.value) return

  observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        const pageNum = parseInt(entry.target.getAttribute('data-page') || '0')
        if (!pageNum) return

        if (entry.isIntersecting) {
          // Add only this page to visible set (neighbors added on scroll)
          visiblePages.value.add(pageNum)

          // Update current page
          const rect = entry.boundingClientRect
          if (rect.top >= -100 && rect.top < window.innerHeight / 2) {
            if (currentPage.value !== pageNum) {
              currentPage.value = pageNum
              emit('page-change', pageNum)

              // Pre-render adjacent pages
              if (pageNum > 1) visiblePages.value.add(pageNum - 1)
              if (pageNum < totalPages.value) visiblePages.value.add(pageNum + 1)
            }
          }
        } else {
          // Remove from visible - be aggressive to save memory
          const distance = Math.abs(pageNum - currentPage.value)
          if (distance > 2) {
            visiblePages.value.delete(pageNum)
          }
        }
      })
    },
    {
      root: containerRef.value,
      rootMargin: '200px 0px',
      threshold: 0
    }
  )

  // Observe all page placeholders
  const placeholders = containerRef.value.querySelectorAll('[data-page]')
  placeholders.forEach((el) => observer!.observe(el))
}

// Check if page should be rendered
function shouldRenderPage(pageNum: number): boolean {
  // Always render page 1 (needed to get total page count)
  if (pageNum === 1) return true
  // Render first 2 pages once we know total
  if (pageNum === 2 && totalPages.value > 1) return true
  // Render if in visible set
  return visiblePages.value.has(pageNum)
}

// Limit concurrent renders for performance
const MAX_RENDERED_PAGES = 7
const renderedPageCount = computed(() => {
  let count = 0
  for (let i = 1; i <= totalPages.value; i++) {
    if (shouldRenderPage(i)) count++
  }
  return count
})

// Scroll to page
function scrollToPage(pageNum: number) {
  if (!containerRef.value) return
  const pageEl = containerRef.value.querySelector(`[data-page="${pageNum}"]`)
  if (pageEl) {
    pageEl.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

// Expose methods
defineExpose({
  scrollToPage,
  getCurrentPage: () => currentPage.value,
  getTotalPages: () => totalPages.value
})

// Watch for zoom changes - need to re-observe
watch(() => props.zoom, () => {
  nextTick(() => {
    setupIntersectionObserver()
  })
})

// Cleanup
onUnmounted(() => {
  if (observer) {
    observer.disconnect()
  }
})
</script>

<template>
  <div
    ref="containerRef"
    class="virtualized-pdf h-full w-full overflow-auto"
  >
    <!-- Loading State (skeleton while first page loads) -->
    <div v-if="isLoading && totalPages === 0" class="flex justify-center py-4">
      <div
        class="bg-white shadow-lg border border-slate-200 p-8"
        :style="{ width: `${effectiveWidth}px`, minHeight: `${estimatedPageHeight}px` }"
      >
        <div class="skeleton-loader h-full">
          <!-- Title line -->
          <div class="skeleton-line w-3/5 h-4 mb-4"></div>
          <!-- Text lines -->
          <div class="skeleton-line w-full h-3 mb-2"></div>
          <div class="skeleton-line w-full h-3 mb-2"></div>
          <div class="skeleton-line w-4/5 h-3 mb-6"></div>
          <!-- Image placeholder -->
          <div class="skeleton-image w-full h-32 mb-6 flex items-center justify-center">
            <span class="material-symbols-outlined text-4xl text-slate-200">image</span>
          </div>
          <!-- More text lines -->
          <div class="skeleton-line w-full h-3 mb-2"></div>
          <div class="skeleton-line w-3/4 h-3"></div>
        </div>
      </div>
    </div>

    <!-- Error State -->
    <div v-if="error" class="flex items-center justify-center h-64">
      <div class="flex flex-col items-center gap-3 text-red-500">
        <span class="material-symbols-outlined text-4xl">error</span>
        <span class="text-sm">{{ error }}</span>
      </div>
    </div>

    <!-- PDF Container -->
    <div class="pdf-pages flex flex-col items-center gap-4 py-4">
      <!-- First page (loads PDF and gets page count) -->
      <div
        v-if="totalPages === 0"
        data-page="1"
        class="pdf-page-wrapper relative bg-white shadow-lg"
        :style="{
          width: `${effectiveWidth}px`,
          minHeight: `${estimatedPageHeight}px`
        }"
      >
        <VuePdfEmbed
          :source="source"
          :page="1"
          :width="effectiveWidth"
          :rotation="rotation || 0"
          :annotation-layer="false"
          :text-layer="false"
          @loaded="handlePdfLoaded"
          @rendering-failed="handleRenderError"
        />
      </div>

      <!-- All pages (after we know total) -->
      <template v-if="totalPages > 0">
        <div
          v-for="pageNum in totalPages"
          :key="pageNum"
          :data-page="pageNum"
          class="pdf-page-wrapper relative bg-white shadow-lg"
          :style="{
            width: `${effectiveWidth}px`,
            minHeight: `${getPageHeight(pageNum)}px`
          }"
        >
          <!-- Render actual PDF page if visible -->
          <VuePdfEmbed
            v-if="shouldRenderPage(pageNum)"
            :source="source"
            :page="pageNum"
            :width="effectiveWidth"
            :rotation="rotation || 0"
            :annotation-layer="false"
            :text-layer="false"
            @rendered="handlePageRendered"
            @rendering-failed="handleRenderError"
          />

          <!-- Skeleton Placeholder when not rendered -->
          <div
            v-else
            class="absolute inset-0 bg-white p-8"
            :style="{ height: `${getPageHeight(pageNum)}px` }"
          >
            <div class="skeleton-loader h-full">
              <!-- Title line -->
              <div class="skeleton-line w-3/5 h-4 mb-4"></div>
              <!-- Text lines -->
              <div class="skeleton-line w-full h-3 mb-2"></div>
              <div class="skeleton-line w-full h-3 mb-2"></div>
              <div class="skeleton-line w-4/5 h-3 mb-6"></div>
              <!-- Image placeholder -->
              <div class="skeleton-image w-full h-32 mb-6 flex items-center justify-center">
                <span class="material-symbols-outlined text-4xl text-slate-200">image</span>
              </div>
              <!-- More text lines -->
              <div class="skeleton-line w-full h-3 mb-2"></div>
              <div class="skeleton-line w-3/4 h-3"></div>
            </div>
          </div>

          <!-- Page number badge -->
          <div class="absolute bottom-2 right-2 px-2 py-0.5 bg-black/60 text-white text-xs rounded">
            {{ pageNum }}
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.border-3 {
  border-width: 3px;
}

.pdf-page-wrapper {
  transition: min-height 0.2s;
}

/* Hide vue-pdf-embed internal elements when used as loader */
:deep(.vue-pdf-embed) {
  display: flex;
  flex-direction: column;
  align-items: center;
}

:deep(.vue-pdf-embed__page) {
  box-shadow: none !important;
}

/* Skeleton Loader Styles */
.skeleton-loader {
  animation: skeleton-pulse 1.5s ease-in-out infinite;
}

.skeleton-line {
  background: linear-gradient(90deg, #e2e8f0 0%, #f1f5f9 50%, #e2e8f0 100%);
  background-size: 200% 100%;
  animation: skeleton-shimmer 1.5s ease-in-out infinite;
  border-radius: 4px;
}

.skeleton-image {
  background: #f1f5f9;
  border-radius: 8px;
}

@keyframes skeleton-shimmer {
  0% {
    background-position: 200% 0;
  }
  100% {
    background-position: -200% 0;
  }
}

@keyframes skeleton-pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.7;
  }
}
</style>
