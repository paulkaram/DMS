<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { sharesApi } from '@/api/client'

const route = useRoute()
const router = useRouter()

const isLoading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  const token = route.params.token as string
  if (!token) {
    error.value = 'Invalid share link'
    isLoading.value = false
    return
  }

  try {
    const response = await sharesApi.resolveShareToken(token)
    const { documentId } = response.data
    router.replace({
      name: 'document-details',
      params: { id: documentId },
      query: { shareToken: token }
    })
  } catch (err: any) {
    error.value = err.response?.data?.[0] || 'This share link is not available or has expired'
    isLoading.value = false
  }
})
</script>

<template>
  <div class="min-h-[60vh] flex items-center justify-center">
    <!-- Loading -->
    <div v-if="isLoading" class="text-center">
      <div class="animate-spin w-10 h-10 border-4 border-teal border-t-transparent rounded-full mx-auto mb-4"></div>
      <p class="text-zinc-500">Resolving share link...</p>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="text-center max-w-md">
      <div class="w-20 h-20 rounded-full bg-red-50 dark:bg-red-900/20 flex items-center justify-center mx-auto mb-6">
        <span class="material-symbols-outlined text-5xl text-red-500">link_off</span>
      </div>
      <h2 class="text-xl font-semibold text-zinc-900 dark:text-white mb-2">Link Not Available</h2>
      <p class="text-zinc-500 mb-6">{{ error }}</p>
      <button
        @click="router.push('/')"
        class="px-6 py-2.5 bg-teal text-white rounded-lg font-medium hover:bg-teal/90 transition-colors"
      >
        Go to Dashboard
      </button>
    </div>
  </div>
</template>
