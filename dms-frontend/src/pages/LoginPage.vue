<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const rememberMe = ref(false)
const localError = ref('')
const showPassword = ref(false)

const error = computed(() => localError.value || authStore.error)

async function handleLogin() {
  if (!username.value || !password.value) {
    localError.value = 'Please enter username and password'
    return
  }

  localError.value = ''

  const success = await authStore.login(username.value, password.value)
  if (success) {
    router.push('/')
  }
}
</script>

<template>
  <div class="min-h-screen flex">
    <!-- Left Panel - Branding -->
    <div class="hidden lg:flex lg:w-1/2 relative bg-gradient-to-br from-navy to-teal overflow-hidden items-center justify-center p-12">
      <!-- Abstract Pattern -->
      <div class="absolute inset-0 opacity-40" style="background-image: radial-gradient(circle at 2px 2px, rgba(255,255,255,0.05) 1px, transparent 0); background-size: 24px 24px;"></div>

      <!-- Decorative Blurs -->
      <div class="absolute top-[-10%] right-[-10%] w-[500px] h-[500px] bg-teal/20 rounded-full blur-[100px]"></div>
      <div class="absolute bottom-[-10%] left-[-10%] w-[500px] h-[500px] bg-navy/40 rounded-full blur-[100px]"></div>

      <!-- Content -->
      <div class="relative z-10 max-w-lg text-white">
        <div class="mb-12">
          <!-- Logo -->
          <div class="w-16 h-16 rounded-lg bg-white/10 backdrop-blur-sm border border-white/10 flex items-center justify-center mb-6">
            <span class="material-symbols-outlined text-4xl text-white">description</span>
          </div>

          <h1 class="text-5xl font-bold leading-tight mb-6">Master your document ecosystem.</h1>
          <p class="text-xl text-white/70 leading-relaxed">
            Access your enterprise assets securely with our next-generation Document Management System. Built for speed, security, and global collaboration.
          </p>
        </div>

        <!-- Feature Cards -->
        <div class="grid grid-cols-2 gap-6">
          <div class="p-6 rounded-lg bg-white/10 backdrop-blur-sm border border-white/10">
            <span class="material-symbols-outlined text-teal mb-3">folder_managed</span>
            <h3 class="font-semibold text-lg">Smart Organization</h3>
            <p class="text-sm text-white/60">Intelligent filing with automated classification.</p>
          </div>
          <div class="p-6 rounded-lg bg-white/10 backdrop-blur-sm border border-white/10">
            <span class="material-symbols-outlined text-teal mb-3">bolt</span>
            <h3 class="font-semibold text-lg">Instant Retrieval</h3>
            <p class="text-sm text-white/60">Find any document in milliseconds with search.</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Right Panel - Login Form -->
    <div class="w-full lg:w-1/2 flex items-center justify-center bg-white p-8 sm:p-12 md:p-16">
      <div class="w-full max-w-md">
        <!-- Header -->
        <div class="mb-10">
          <div class="flex items-center gap-3 mb-8">
            <div class="w-10 h-10 rounded-lg bg-gradient-to-br from-navy to-teal flex items-center justify-center text-white shadow-lg">
              <span class="material-symbols-outlined">description</span>
            </div>
            <span class="text-2xl font-bold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-navy to-teal">INTALIO</span>
          </div>
          <h2 class="text-3xl font-bold text-zinc-900 mb-2">Welcome Back</h2>
          <p class="text-zinc-500">Please enter your credentials to access the workspace.</p>
        </div>

        <!-- Login Form -->
        <form @submit.prevent="handleLogin" class="space-y-6">
          <!-- Username Field -->
          <div>
            <label for="username" class="block text-sm font-semibold text-zinc-700 mb-2">Username</label>
            <div class="relative">
              <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400 text-[20px]">person</span>
              <input
                id="username"
                v-model="username"
                type="text"
                autocomplete="username"
                class="w-full pl-10 pr-4 py-3 bg-zinc-50 border border-zinc-200 rounded-lg focus:ring-2 focus:ring-teal/20 focus:border-teal transition-all outline-none"
                placeholder="Enter your username"
                required
              />
            </div>
          </div>

          <!-- Password Field -->
          <div>
            <div class="flex items-center justify-between mb-2">
              <label for="password" class="block text-sm font-semibold text-zinc-700">Password</label>
              <a href="#" class="text-sm font-medium text-teal hover:text-navy transition-colors">Forgot password?</a>
            </div>
            <div class="relative">
              <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-zinc-400 text-[20px]">lock</span>
              <input
                id="password"
                v-model="password"
                :type="showPassword ? 'text' : 'password'"
                autocomplete="current-password"
                class="w-full pl-10 pr-12 py-3 bg-zinc-50 border border-zinc-200 rounded-lg focus:ring-2 focus:ring-teal/20 focus:border-teal transition-all outline-none"
                placeholder="••••••••"
                required
              />
              <button
                type="button"
                @click="showPassword = !showPassword"
                class="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-zinc-600 transition-colors"
              >
                <span class="material-symbols-outlined text-[20px]">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
              </button>
            </div>
          </div>

          <!-- Remember Me -->
          <div class="flex items-center">
            <input
              id="remember-me"
              v-model="rememberMe"
              type="checkbox"
              class="h-4 w-4 text-teal focus:ring-teal border-zinc-300 rounded cursor-pointer"
            />
            <label for="remember-me" class="ml-2 block text-sm text-zinc-600 cursor-pointer">
              Remember this device for 30 days
            </label>
          </div>

          <!-- Error Message -->
          <div v-if="error" class="p-4 bg-red-50 border border-red-200 rounded-lg text-red-600 text-sm flex items-center gap-2">
            <span class="material-symbols-outlined text-lg">error</span>
            {{ error }}
          </div>

          <!-- Submit Button -->
          <button
            type="submit"
            :disabled="authStore.isLoading"
            class="w-full py-4 bg-gradient-to-r from-navy to-teal text-white rounded-full font-bold text-lg shadow-xl shadow-teal/20 hover:opacity-95 transform active:scale-[0.98] transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <template v-if="authStore.isLoading">
              <svg class="animate-spin w-5 h-5" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              Signing in...
            </template>
            <template v-else>
              Sign In
            </template>
          </button>
        </form>

        <!-- Divider -->
        <div class="mt-10">
          <div class="relative mb-8">
            <div class="absolute inset-0 flex items-center">
              <div class="w-full border-t border-zinc-200"></div>
            </div>
            <div class="relative flex justify-center text-sm">
              <span class="px-4 bg-white text-zinc-500 font-medium">Or continue with</span>
            </div>
          </div>

          <!-- SSO Buttons -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <button
              type="button"
              class="flex items-center justify-center gap-3 px-4 py-3 border border-zinc-200 rounded-lg text-sm font-semibold text-zinc-700 hover:bg-zinc-50 transition-colors"
            >
              <svg class="w-5 h-5" viewBox="0 0 24 24">
                <path d="M12.48 10.92v3.28h7.84c-.24 1.84-.909 3.204-1.908 4.212-1.286 1.286-3.303 2.56-6.412 2.56-5.18 0-9.397-4.186-9.397-9.396S6.793 2.138 11.972 2.138c2.805 0 4.936 1.103 6.436 2.522l2.316-2.316C18.663 1.103 15.65 0 11.972 0 5.42 0 0 5.42 0 12s5.42 12 11.972 12c3.57 0 6.26-1.17 8.35-3.348 2.16-2.16 2.84-5.19 2.84-7.59 0-.718-.065-1.403-.185-2.142h-10.51z" fill="currentColor"></path>
              </svg>
              Google
            </button>
            <button
              type="button"
              class="flex items-center justify-center gap-3 px-4 py-3 border border-zinc-200 rounded-lg text-sm font-semibold text-zinc-700 hover:bg-zinc-50 transition-colors"
            >
              <svg class="w-5 h-5" viewBox="0 0 23 23">
                <path d="M11.4 24H0V12.6h11.4V24zM24 24H12.6V12.6H24V24zM11.4 11.4H0V0h11.4v11.4zM24 11.4H12.6V0H24v11.4z" fill="currentColor"></path>
              </svg>
              Azure AD
            </button>
          </div>
        </div>

        <!-- Footer -->
        <p class="mt-10 text-center text-sm text-zinc-500">
          New to the platform? <a href="#" class="font-bold text-teal hover:underline">Request access</a>
        </p>
      </div>
    </div>
  </div>
</template>
