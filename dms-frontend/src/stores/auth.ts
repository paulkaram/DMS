import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { User } from '@/types'
import { authApi, rolePermissionsApi } from '@/api/client'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refreshToken'))
  const user = ref<User | null>(null)
  const allowedActions = ref<string[]>([])
  const isLoading = ref(false)
  const isLoadingPermissions = ref(false)
  const error = ref<string | null>(null)

  const isAuthenticated = computed(() => !!token.value)
  const isPermissionsLoaded = computed(() => allowedActions.value.length > 0)

  // Check if user has a specific permission
  function hasPermission(actionCode: string): boolean {
    return allowedActions.value.includes(actionCode)
  }

  // Check if user has any of the specified permissions
  function hasAnyPermission(actionCodes: string[]): boolean {
    return actionCodes.some(code => allowedActions.value.includes(code))
  }

  // Check if user has all of the specified permissions
  function hasAllPermissions(actionCodes: string[]): boolean {
    return actionCodes.every(code => allowedActions.value.includes(code))
  }

  function setToken(newToken: string, newRefreshToken?: string) {
    token.value = newToken
    localStorage.setItem('token', newToken)
    if (newRefreshToken) {
      refreshToken.value = newRefreshToken
      localStorage.setItem('refreshToken', newRefreshToken)
    }
  }

  function setUser(newUser: User, actions?: string[]) {
    user.value = newUser
    if (actions) {
      allowedActions.value = actions
    }
  }

  async function login(username: string, password: string): Promise<boolean> {
    isLoading.value = true
    error.value = null

    try {
      const response = await authApi.login(username, password)
      const data = response.data

      setToken(data.token, data.refreshToken)
      setUser(data.user)
      // Fetch allowed actions after successful login
      await fetchAllowedActions()
      return true
    } catch (err: any) {
      error.value = err.response?.data?.errors?.[0] || err.response?.data?.message || 'Login failed'
      return false
    } finally {
      isLoading.value = false
    }
  }

  async function fetchAllowedActions() {
    if (!token.value) {
      return
    }
    if (isLoadingPermissions.value) {
      return
    }

    isLoadingPermissions.value = true
    try {
      const response = await rolePermissionsApi.getMyAllowedActions()
      allowedActions.value = response.data || []
    } catch (err) {
      allowedActions.value = []
    } finally {
      isLoadingPermissions.value = false
    }
  }

  async function fetchCurrentUser() {
    if (!token.value) return

    try {
      const response = await authApi.getCurrentUser()
      setUser(response.data)
      // Also fetch allowed actions
      await fetchAllowedActions()
    } catch (err: any) {
      // Only logout on 401 (invalid/expired token).
      // Other errors (429, network, 500) are transient — keep the session alive.
      if (err.response?.status === 401) {
        logout()
      }
    }
  }

  async function refreshAccessToken(): Promise<boolean> {
    if (!refreshToken.value) return false

    try {
      const response = await authApi.refreshToken(refreshToken.value)
      setToken(response.data.token, response.data.refreshToken)
      return true
    } catch (err) {
      logout()
      return false
    }
  }

  function logout() {
    token.value = null
    refreshToken.value = null
    user.value = null
    allowedActions.value = []
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
  }

  // For development - auto login with demo token
  function devLogin() {
    const demoUser: User = {
      id: '00000000-0000-0000-0000-000000000001',
      username: 'demo',
      email: 'demo@example.com',
      firstName: 'Demo',
      lastName: 'User',
      displayName: 'Demo User'
    }
    setToken('demo-token')
    setUser(demoUser)
  }

  // Initialize user if token exists
  if (token.value && !user.value) {
    fetchCurrentUser()
  }

  return {
    token,
    user,
    isAuthenticated,
    isLoading,
    isPermissionsLoaded,
    error,
    allowedActions,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    login,
    logout,
    devLogin,
    fetchCurrentUser,
    fetchAllowedActions,
    refreshAccessToken
  }
})
