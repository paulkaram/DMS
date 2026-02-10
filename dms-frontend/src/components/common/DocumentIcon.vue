<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  extension?: string
  icon?: string
  size?: 'sm' | 'md' | 'lg'
  index?: number
}>()

const sizeClasses = {
  sm: { wrapper: 'w-8 h-8 rounded-lg', icon: 'text-base' },
  md: { wrapper: 'w-10 h-10 rounded-lg', icon: 'text-xl' },
  lg: { wrapper: 'w-12 h-12 rounded-lg', icon: 'text-2xl' }
}

// Alternating background colors - charcoal with teal accents
const bgColors = ['bg-[#0d1117]', 'bg-teal', 'bg-[#1a2332]', 'bg-[#0d1117]', 'bg-teal/90']
const bgColor = computed(() => {
  if (props.index !== undefined) {
    return bgColors[props.index % bgColors.length]
  }
  return 'bg-[#1e3a5f]' // Default dark navy
})

const fileIcon = computed(() => {
  const ext = props.extension?.toLowerCase()?.replace('.', '') || ''

  // Icon mapping by file type
  const iconMap: Record<string, string> = {
    // PDF
    'pdf': 'picture_as_pdf',
    // Word
    'doc': 'description',
    'docx': 'description',
    // Excel
    'xls': 'table_chart',
    'xlsx': 'table_chart',
    // PowerPoint
    'ppt': 'slideshow',
    'pptx': 'slideshow',
    // Images
    'jpg': 'image',
    'jpeg': 'image',
    'png': 'image',
    'gif': 'gif_box',
    'webp': 'image',
    'svg': 'image',
    // Video
    'mp4': 'movie',
    'mov': 'movie',
    'avi': 'movie',
    'mkv': 'movie',
    'webm': 'movie',
    // Audio
    'mp3': 'audio_file',
    'wav': 'audio_file',
    'flac': 'audio_file',
    'aac': 'audio_file',
    // Archives
    'zip': 'folder_zip',
    'rar': 'folder_zip',
    '7z': 'folder_zip',
    'tar': 'folder_zip',
    'gz': 'folder_zip',
    // Text
    'txt': 'article',
    'md': 'article',
    'rtf': 'article',
    // Data
    'json': 'data_object',
    'xml': 'code',
    'csv': 'table_view',
    // Code
    'html': 'html',
    'css': 'css',
    'js': 'javascript',
    'ts': 'code',
    'vue': 'code',
    'py': 'code',
  }

  return iconMap[ext] || 'description'
})

const displayIcon = computed(() => props.icon || fileIcon.value)

const currentSize = computed(() => sizeClasses[props.size || 'md'])
</script>

<template>
  <div
    class="flex items-center justify-center flex-shrink-0 shadow-sm"
    :class="[currentSize.wrapper, bgColor]"
  >
    <span
      class="material-symbols-outlined text-white leading-none"
      :class="currentSize.icon"
    >
      {{ displayIcon }}
    </span>
  </div>
</template>
