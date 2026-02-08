<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppLayout from './components/layout/AppLayout.vue'
import { useAuthStore } from './stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <AppLayout v-if="authStore.isAuthenticated">
    <RouterView />
  </AppLayout>
  <RouterView v-else />
</template>
