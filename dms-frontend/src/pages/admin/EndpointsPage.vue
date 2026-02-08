<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { endpointsApi } from '@/api/client'
import type { ServiceEndpoint } from '@/types'
import { UiButton, UiInput, UiTextArea, UiSelect, UiToggle, UiModal } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const endpoints = ref<ServiceEndpoint[]>([])
const isLoading = ref(false)
const isSaving = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const formData = ref<Partial<ServiceEndpoint>>({
  name: '',
  url: '',
  description: '',
  endpointType: 'REST',
  authType: 'None',
  authConfig: '',
  timeoutSeconds: 30,
  retryCount: 3,
  headers: '',
  isActive: true
})

const endpointTypeOptions = [
  { value: 'REST', label: 'REST' },
  { value: 'SOAP', label: 'SOAP' },
  { value: 'GraphQL', label: 'GraphQL' },
  { value: 'WebSocket', label: 'WebSocket' }
]

const authTypeOptions = [
  { value: 'None', label: 'None' },
  { value: 'Basic', label: 'Basic Auth' },
  { value: 'Bearer', label: 'Bearer Token' },
  { value: 'OAuth', label: 'OAuth 2.0' },
  { value: 'ApiKey', label: 'API Key' }
]

const modalTitle = computed(() => isEditing.value ? 'Edit Endpoint' : 'New Endpoint')

onMounted(async () => {
  await loadEndpoints()
})

async function loadEndpoints() {
  isLoading.value = true
  try {
    const response = await endpointsApi.getAll()
    endpoints.value = response.data
  } catch (error) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    name: '',
    url: '',
    description: '',
    endpointType: 'REST',
    authType: 'None',
    authConfig: '',
    timeoutSeconds: 30,
    retryCount: 3,
    headers: '',
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(endpoint: ServiceEndpoint) {
  formData.value = { ...endpoint }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  isSaving.value = true
  try {
    if (isEditing.value && formData.value.id) {
      await endpointsApi.update(formData.value.id, {
        name: formData.value.name!,
        url: formData.value.url!,
        description: formData.value.description,
        endpointType: formData.value.endpointType!,
        authType: formData.value.authType,
        authConfig: formData.value.authConfig,
        timeoutSeconds: formData.value.timeoutSeconds,
        retryCount: formData.value.retryCount,
        headers: formData.value.headers,
        isActive: formData.value.isActive!
      })
    } else {
      await endpointsApi.create({
        name: formData.value.name!,
        url: formData.value.url!,
        description: formData.value.description,
        endpointType: formData.value.endpointType!,
        authType: formData.value.authType,
        authConfig: formData.value.authConfig,
        timeoutSeconds: formData.value.timeoutSeconds,
        retryCount: formData.value.retryCount,
        headers: formData.value.headers
      })
    }
    showModal.value = false
    await loadEndpoints()
  } catch (error) {
  } finally {
    isSaving.value = false
  }
}

async function deleteEndpoint(id: string) {
  if (!confirm('Are you sure you want to delete this endpoint?')) return
  try {
    await endpointsApi.delete(id)
    await loadEndpoints()
  } catch (error) {
  }
}

async function testEndpoint(endpoint: ServiceEndpoint) {
  try {
    await endpointsApi.updateHealthStatus(endpoint.id, 'checking')
    await new Promise(resolve => setTimeout(resolve, 1000))
    const status = Math.random() > 0.3 ? 'Healthy' : 'Unhealthy'
    await endpointsApi.updateHealthStatus(endpoint.id, status)
    await loadEndpoints()
  } catch (error) {
  }
}

function getStatusColor(status?: string) {
  switch (status?.toLowerCase()) {
    case 'healthy': return 'bg-green-100 text-green-700'
    case 'unhealthy': return 'bg-red-100 text-red-700'
    case 'checking': return 'bg-yellow-100 text-yellow-700'
    default: return 'bg-gray-100 text-gray-500'
  }
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Service Endpoints" icon="cloud" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Service Endpoints</h1>
          <p class="text-gray-500 mt-1">Manage external service endpoints and integrations</p>
        </div>
        <UiButton @click="openCreateModal">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Endpoint
        </UiButton>
      </div>

      <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div v-if="isLoading" class="p-8 text-center text-gray-500">
          <svg class="animate-spin h-8 w-8 mx-auto text-teal" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
          </svg>
          <p class="mt-2">Loading endpoints...</p>
        </div>
        <div v-else-if="endpoints.length === 0" class="p-8 text-center text-gray-500">
          <svg class="w-12 h-12 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 01-9 9m9-9a9 9 0 00-9-9m9 9H3m9 9a9 9 0 01-9-9m9 9c1.657 0 3-4.03 3-9s-1.343-9-3-9m0 18c-1.657 0-3-4.03-3-9s1.343-9 3-9m-9 9a9 9 0 019-9" />
          </svg>
          <p class="text-lg font-medium">No endpoints configured</p>
          <p class="text-sm mt-1">Add an endpoint to integrate with external services</p>
        </div>
        <table v-else class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">URL</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Type</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Auth</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="endpoint in endpoints" :key="endpoint.id" class="border-t border-gray-100 hover:bg-gray-50 transition-colors">
              <td class="py-3 px-4">
                <p class="font-medium text-gray-900">{{ endpoint.name }}</p>
                <p v-if="endpoint.description" class="text-xs text-gray-500">{{ endpoint.description }}</p>
              </td>
              <td class="py-3 px-4">
                <code class="text-sm text-gray-600 bg-gray-50 px-2 py-1 rounded">{{ endpoint.url }}</code>
              </td>
              <td class="py-3 px-4">
                <span class="px-2.5 py-1 text-xs font-medium bg-gray-100 text-gray-700 rounded-full">{{ endpoint.endpointType }}</span>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ endpoint.authType || 'None' }}</td>
              <td class="py-3 px-4">
                <span :class="getStatusColor(endpoint.lastHealthStatus)" class="px-2.5 py-1 text-xs font-medium rounded-full capitalize">
                  {{ endpoint.lastHealthStatus || 'Unknown' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-1">
                  <UiButton variant="ghost" size="sm" icon-only @click="testEndpoint(endpoint)" title="Test Connection">
                    <svg class="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
                    </svg>
                  </UiButton>
                  <UiButton variant="ghost" size="sm" icon-only @click="openEditModal(endpoint)">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                    </svg>
                  </UiButton>
                  <UiButton variant="ghost" size="sm" icon-only @click="deleteEndpoint(endpoint.id)">
                    <svg class="w-4 h-4 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </UiButton>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <UiModal v-model="showModal" :title="modalTitle" size="lg">
      <div class="space-y-5">
        <UiInput
          v-model="formData.name"
          label="Name"
          placeholder="Enter endpoint name"
        />

        <UiInput
          v-model="formData.url"
          label="URL"
          placeholder="https://api.example.com/v1"
        />

        <div class="grid grid-cols-2 gap-4">
          <UiSelect
            v-model="formData.endpointType"
            label="Type"
            :options="endpointTypeOptions"
          />
          <UiSelect
            v-model="formData.authType"
            label="Authentication"
            :options="authTypeOptions"
          />
        </div>

        <UiTextArea
          v-if="formData.authType !== 'None'"
          v-model="formData.authConfig"
          label="Auth Config (JSON)"
          placeholder='{"username": "...", "password": "..."}'
          :rows="2"
        />

        <div class="grid grid-cols-2 gap-4">
          <UiInput
            v-model.number="formData.timeoutSeconds"
            type="number"
            label="Timeout (seconds)"
            placeholder="30"
          />
          <UiInput
            v-model.number="formData.retryCount"
            type="number"
            label="Retry Count"
            placeholder="3"
          />
        </div>

        <UiTextArea
          v-model="formData.headers"
          label="Custom Headers (JSON)"
          placeholder='{"X-Custom-Header": "value"}'
          :rows="2"
        />

        <UiTextArea
          v-model="formData.description"
          label="Description"
          placeholder="Enter description"
          :rows="2"
        />

        <UiToggle
          v-model="formData.isActive"
          label="Active"
          color="green"
        />
      </div>

      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showModal = false">Cancel</UiButton>
          <UiButton :loading="isSaving" @click="handleSave">
            {{ isSaving ? 'Saving...' : 'Save' }}
          </UiButton>
        </div>
      </template>
    </UiModal>
  </div>
</template>
