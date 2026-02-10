<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { patternsApi } from '@/api/client'
import type { Pattern } from '@/types'
import { UiSelect, UiCheckbox } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

const patterns = ref<Pattern[]>([])
const isLoading = ref(false)
const showModal = ref(false)
const isEditing = ref(false)
const testResult = ref<{ show: boolean; matches: boolean; value: string }>({ show: false, matches: false, value: '' })

const formData = ref({
  id: '',
  name: '',
  regex: '',
  description: '',
  patternType: 'Naming',
  targetFolderId: '',
  priority: 100,
  isActive: true
})

const patternTypes = ['Naming', 'Filing', 'Validation', 'Search']

onMounted(() => {
  loadPatterns()
})

async function loadPatterns() {
  isLoading.value = true
  try {
    const response = await patternsApi.getAll(true)
    patterns.value = response.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  formData.value = {
    id: '',
    name: '',
    regex: '',
    description: '',
    patternType: 'Naming',
    targetFolderId: '',
    priority: 100,
    isActive: true
  }
  isEditing.value = false
  showModal.value = true
}

function openEditModal(pattern: Pattern) {
  formData.value = {
    id: pattern.id,
    name: pattern.name,
    regex: pattern.regex,
    description: pattern.description || '',
    patternType: pattern.patternType,
    targetFolderId: pattern.targetFolderId || '',
    priority: pattern.priority,
    isActive: pattern.isActive
  }
  isEditing.value = true
  showModal.value = true
}

async function handleSave() {
  try {
    if (isEditing.value) {
      await patternsApi.update(formData.value.id, {
        name: formData.value.name,
        regex: formData.value.regex,
        description: formData.value.description || undefined,
        patternType: formData.value.patternType,
        targetFolderId: formData.value.targetFolderId || undefined,
        priority: formData.value.priority,
        isActive: formData.value.isActive
      })
    } else {
      await patternsApi.create({
        name: formData.value.name,
        regex: formData.value.regex,
        description: formData.value.description || undefined,
        patternType: formData.value.patternType,
        targetFolderId: formData.value.targetFolderId || undefined,
        priority: formData.value.priority
      })
    }
    showModal.value = false
    await loadPatterns()
  } catch (err) {
  }
}

async function deletePattern(id: string) {
  if (!confirm('Are you sure you want to delete this pattern?')) return
  try {
    await patternsApi.delete(id)
    await loadPatterns()
  } catch (err) {
  }
}

async function testPattern(pattern: Pattern) {
  const testValue = prompt('Enter a value to test:')
  if (!testValue) return

  try {
    const response = await patternsApi.testPattern(pattern.regex, testValue)
    testResult.value = {
      show: true,
      matches: response.data.matches,
      value: testValue
    }
    setTimeout(() => { testResult.value.show = false }, 3000)
  } catch (err) {
    alert('Error testing pattern')
  }
}

function getPatternTypeColor(type: string): string {
  const colors: Record<string, string> = {
    Naming: 'bg-blue-100 text-blue-700',
    Filing: 'bg-green-100 text-green-700',
    Validation: 'bg-amber-100 text-amber-700',
    Search: 'bg-purple-100 text-purple-700'
  }
  return colors[type] || 'bg-gray-100 text-gray-700'
}

const patternTypeOptions = computed(() =>
  patternTypes.map(type => ({ value: type, label: type }))
)
</script>

<template>
  <div class="p-6">
    <div class="max-w-6xl mx-auto">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Patterns" icon="code_blocks" />

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Patterns</h1>
          <p class="text-gray-500 mt-1">Define regex patterns for document matching, naming validation, and auto-filing</p>
        </div>
        <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          New Pattern
        </button>
      </div>

      <!-- Test Result Toast -->
      <div v-if="testResult.show" class="fixed top-4 right-4 z-50 p-4 rounded-lg shadow-lg"
           :class="testResult.matches ? 'bg-green-500 text-white' : 'bg-red-500 text-white'">
        <div class="flex items-center gap-2">
          <svg v-if="testResult.matches" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
          <span>{{ testResult.matches ? 'Match!' : 'No match' }} for "{{ testResult.value }}"</span>
        </div>
      </div>

      <!-- Info Box -->
      <div class="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
        <div class="flex items-start gap-3">
          <svg class="w-5 h-5 text-blue-600 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <div class="text-sm text-blue-700">
            <p class="font-medium text-blue-900">Pattern Types:</p>
            <ul class="mt-1 space-y-1">
              <li><strong>Naming:</strong> Validate document names against a pattern</li>
              <li><strong>Filing:</strong> Auto-file documents to folders based on name match</li>
              <li><strong>Validation:</strong> Validate metadata field values</li>
              <li><strong>Search:</strong> Search patterns for advanced document finding</li>
            </ul>
          </div>
        </div>
      </div>

      <div v-if="isLoading" class="text-center py-12 text-gray-500">Loading patterns...</div>

      <div v-else class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
        <table class="w-full">
          <thead class="bg-gray-50">
            <tr>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Name</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Pattern</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Type</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Target</th>
              <th class="text-center py-3 px-4 text-sm font-medium text-gray-500">Priority</th>
              <th class="text-left py-3 px-4 text-sm font-medium text-gray-500">Status</th>
              <th class="text-right py-3 px-4 text-sm font-medium text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="patterns.length === 0">
              <td colspan="7" class="py-12 text-center text-gray-500">No patterns defined</td>
            </tr>
            <tr v-for="pattern in patterns" :key="pattern.id" class="border-t border-gray-100 hover:bg-gray-50">
              <td class="py-3 px-4">
                <p class="font-medium text-gray-900">{{ pattern.name }}</p>
                <p v-if="pattern.description" class="text-xs text-gray-500">{{ pattern.description }}</p>
              </td>
              <td class="py-3 px-4">
                <code class="px-2 py-1 bg-gray-100 text-sm rounded font-mono">{{ pattern.regex }}</code>
              </td>
              <td class="py-3 px-4">
                <span :class="getPatternTypeColor(pattern.patternType)" class="px-2 py-1 text-xs rounded-full">
                  {{ pattern.patternType }}
                </span>
              </td>
              <td class="py-3 px-4 text-sm text-gray-500">{{ pattern.targetFolderName || 'Global' }}</td>
              <td class="py-3 px-4 text-center text-sm text-gray-500">{{ pattern.priority }}</td>
              <td class="py-3 px-4">
                <span :class="pattern.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'" class="px-2 py-1 text-xs rounded-full">
                  {{ pattern.isActive ? 'Active' : 'Inactive' }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <button @click="testPattern(pattern)" class="p-1 text-gray-400 hover:text-green-600 mr-2" title="Test Pattern">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </button>
                <button @click="openEditModal(pattern)" class="p-1 text-gray-400 hover:text-teal mr-2">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                </button>
                <button @click="deletePattern(pattern.id)" class="p-1 text-gray-400 hover:text-red-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-lg shadow-xl w-full max-w-lg mx-4 p-6">
        <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ isEditing ? 'Edit Pattern' : 'New Pattern' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Name *</label>
            <input v-model="formData.name" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg" placeholder="e.g., Invoice Number Pattern" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Regex Pattern *</label>
            <input v-model="formData.regex" type="text" class="w-full px-4 py-2 border border-gray-300 rounded-lg font-mono" placeholder="^INV-\d{4}-\d{6}$" />
            <p class="text-xs text-gray-500 mt-1">Use JavaScript-compatible regular expressions</p>
          </div>
          <UiSelect
            v-model="formData.patternType"
            :options="patternTypeOptions"
            label="Pattern Type"
            placeholder="Select type"
          />
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Priority</label>
            <input v-model.number="formData.priority" type="number" class="w-full px-4 py-2 border border-gray-300 rounded-lg" placeholder="100" />
            <p class="text-xs text-gray-500 mt-1">Lower values are matched first</p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <textarea v-model="formData.description" rows="2" class="w-full px-4 py-2 border border-gray-300 rounded-lg" placeholder="Describe what this pattern matches"></textarea>
          </div>
          <UiCheckbox v-if="isEditing" v-model="formData.isActive" label="Active" />
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showModal = false" class="px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50">Cancel</button>
          <button @click="handleSave" class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90">Save</button>
        </div>
      </div>
    </div>
  </div>
</template>
