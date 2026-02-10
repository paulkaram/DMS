<script setup lang="ts">
interface Props {
  size?: 'sm' | 'md' | 'lg' | 'xl'
  text?: string
  fullScreen?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  size: 'md',
  fullScreen: false
})

const sizeClasses = {
  sm: 'w-8 h-8',
  md: 'w-12 h-12',
  lg: 'w-16 h-16',
  xl: 'w-24 h-24'
}

const textSizeClasses = {
  sm: 'text-xs mt-2',
  md: 'text-sm mt-3',
  lg: 'text-base mt-4',
  xl: 'text-lg mt-5'
}
</script>

<template>
  <div
    class="flex flex-col items-center justify-center"
    :class="fullScreen ? 'fixed inset-0 bg-white/90 dark:bg-background-dark/90 backdrop-blur-sm z-50' : ''"
  >
    <!-- Animated Logo Container -->
    <div class="relative" :class="sizeClasses[size]">
      <!-- Outer rotating ring -->
      <div
        class="absolute inset-0 rounded-lg animate-spin-slow"
        :class="sizeClasses[size]"
        style="animation-duration: 3s;"
      >
        <svg class="w-full h-full" viewBox="0 0 100 100">
          <defs>
            <linearGradient id="gradient-ring" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" style="stop-color:#112d48" />
              <stop offset="100%" style="stop-color:#00ae8c" />
            </linearGradient>
          </defs>
          <circle
            cx="50"
            cy="50"
            r="45"
            fill="none"
            stroke="url(#gradient-ring)"
            stroke-width="3"
            stroke-linecap="round"
            stroke-dasharray="70 200"
          />
        </svg>
      </div>

      <!-- Inner pulsing document icon -->
      <div class="absolute inset-0 flex items-center justify-center">
        <div class="relative animate-pulse-gentle">
          <svg
            class="drop-shadow-md"
            :class="size === 'sm' ? 'w-4 h-4' : size === 'md' ? 'w-6 h-6' : size === 'lg' ? 'w-8 h-8' : 'w-12 h-12'"
            viewBox="0 0 24 24"
            fill="none"
          >
            <defs>
              <linearGradient id="gradient-doc" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" style="stop-color:#112d48" />
                <stop offset="100%" style="stop-color:#00ae8c" />
              </linearGradient>
            </defs>
            <!-- Document shape -->
            <path
              d="M14 2H6C4.9 2 4 2.9 4 4V20C4 21.1 4.9 22 6 22H18C19.1 22 20 21.1 20 20V8L14 2Z"
              fill="url(#gradient-doc)"
            />
            <!-- Folded corner -->
            <path
              d="M14 2V8H20"
              stroke="white"
              stroke-width="1.5"
              stroke-linecap="round"
              stroke-linejoin="round"
              opacity="0.5"
            />
            <!-- Document lines -->
            <path
              d="M8 13H16M8 17H13"
              stroke="white"
              stroke-width="1.5"
              stroke-linecap="round"
              opacity="0.7"
            />
          </svg>
        </div>
      </div>

      <!-- Floating dots animation -->
      <div class="absolute inset-0">
        <div
          class="absolute w-1.5 h-1.5 bg-teal rounded-full animate-float-1"
          :style="{ left: '10%', top: '20%' }"
        />
        <div
          class="absolute w-1 h-1 bg-navy rounded-full animate-float-2"
          :style="{ right: '15%', top: '30%' }"
        />
        <div
          class="absolute w-1.5 h-1.5 bg-teal/70 rounded-full animate-float-3"
          :style="{ left: '20%', bottom: '25%' }"
        />
      </div>
    </div>

    <!-- Loading text -->
    <p
      v-if="text"
      class="font-medium bg-gradient-to-r from-navy to-teal bg-clip-text text-transparent"
      :class="textSizeClasses[size]"
    >
      {{ text }}
    </p>

    <!-- Progress bar (optional subtle animation) -->
    <div v-if="size !== 'sm'" class="mt-3 w-20 h-1 bg-zinc-200 dark:bg-border-dark rounded-full overflow-hidden">
      <div class="h-full bg-gradient-to-r from-navy to-teal rounded-full animate-progress" />
    </div>
  </div>
</template>

<style scoped>
@keyframes spin-slow {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}

@keyframes pulse-gentle {
  0%, 100% {
    transform: scale(1);
    opacity: 1;
  }
  50% {
    transform: scale(0.95);
    opacity: 0.85;
  }
}

@keyframes float-1 {
  0%, 100% {
    transform: translateY(0) translateX(0);
    opacity: 0.7;
  }
  50% {
    transform: translateY(-8px) translateX(4px);
    opacity: 1;
  }
}

@keyframes float-2 {
  0%, 100% {
    transform: translateY(0) translateX(0);
    opacity: 0.5;
  }
  50% {
    transform: translateY(-6px) translateX(-3px);
    opacity: 0.9;
  }
}

@keyframes float-3 {
  0%, 100% {
    transform: translateY(0) translateX(0);
    opacity: 0.6;
  }
  50% {
    transform: translateY(-10px) translateX(2px);
    opacity: 1;
  }
}

@keyframes progress {
  0% {
    width: 0%;
    margin-left: 0%;
  }
  50% {
    width: 60%;
    margin-left: 20%;
  }
  100% {
    width: 0%;
    margin-left: 100%;
  }
}

.animate-spin-slow {
  animation: spin-slow 3s linear infinite;
}

.animate-pulse-gentle {
  animation: pulse-gentle 2s ease-in-out infinite;
}

.animate-float-1 {
  animation: float-1 2.5s ease-in-out infinite;
}

.animate-float-2 {
  animation: float-2 3s ease-in-out infinite;
  animation-delay: 0.3s;
}

.animate-float-3 {
  animation: float-3 2.8s ease-in-out infinite;
  animation-delay: 0.6s;
}

.animate-progress {
  animation: progress 2s ease-in-out infinite;
}
</style>
