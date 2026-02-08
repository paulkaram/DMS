<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import type { ApprovalRequest, ApprovalWorkflow } from '@/types'
import { approvalsApi } from '@/api/client'

const router = useRouter()
const activeTab = ref<'pending' | 'myRequests' | 'workflows'>('pending')

const pendingRequests = ref<ApprovalRequest[]>([])
const myRequests = ref<ApprovalRequest[]>([])
const workflows = ref<ApprovalWorkflow[]>([])
const isLoading = ref(true)

const showActionModal = ref(false)
const selectedRequest = ref<ApprovalRequest | null>(null)
const actionData = ref({
  action: 1,
  comments: ''
})

onMounted(async () => {
  await loadData()
})

async function loadData() {
  isLoading.value = true
  try {
    const [pendingRes, myRes, workflowsRes] = await Promise.all([
      approvalsApi.getPendingRequests(),
      approvalsApi.getMyRequests(),
      approvalsApi.getWorkflows()
    ])
    pendingRequests.value = pendingRes.data
    myRequests.value = myRes.data
    workflows.value = workflowsRes.data
  } catch (err) {
  } finally {
    isLoading.value = false
  }
}

function openActionModal(request: ApprovalRequest) {
  selectedRequest.value = request
  actionData.value = { action: 1, comments: '' }
  showActionModal.value = true
}

async function submitAction() {
  if (!selectedRequest.value) return

  try {
    await approvalsApi.submitAction(selectedRequest.value.id, actionData.value)
    showActionModal.value = false
    await loadData()
  } catch (err) {
  }
}

async function cancelRequest(request: ApprovalRequest) {
  if (!confirm('Cancel this approval request?')) return

  try {
    await approvalsApi.cancelRequest(request.id)
    await loadData()
  } catch (err) {
  }
}

function viewDocument(documentId: string) {
  router.push(`/documents/${documentId}`)
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}

function getStatusClass(status: number): string {
  switch (status) {
    case 0: return 'bg-amber-100 text-amber-700'
    case 1: return 'bg-green-100 text-green-700'
    case 2: return 'bg-red-100 text-red-700'
    case 3: return 'bg-gray-100 text-gray-600'
    default: return 'bg-gray-100 text-gray-600'
  }
}

function getStatusName(status: number): string {
  switch (status) {
    case 0: return 'Pending'
    case 1: return 'Approved'
    case 2: return 'Rejected'
    case 3: return 'Cancelled'
    default: return 'Unknown'
  }
}
</script>

<template>
  <div class="space-y-6">
    <div>
      <h1 class="text-2xl font-bold text-zinc-900 dark:text-zinc-100">Approvals</h1>
      <p class="text-zinc-500 mt-1">Document approval requests and workflows</p>
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200 mb-6">
      <nav class="flex gap-6">
        <button
          @click="activeTab = 'pending'"
          :class="[
            'py-3 text-sm font-medium border-b-2 transition-colors',
            activeTab === 'pending'
              ? 'border-blue-500 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          Pending Approval
          <span v-if="pendingRequests.length > 0" class="ml-2 px-2 py-0.5 text-xs bg-amber-100 text-amber-700 rounded-full">
            {{ pendingRequests.length }}
          </span>
        </button>
        <button
          @click="activeTab = 'myRequests'"
          :class="[
            'py-3 text-sm font-medium border-b-2 transition-colors',
            activeTab === 'myRequests'
              ? 'border-blue-500 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          My Requests
        </button>
        <button
          @click="activeTab = 'workflows'"
          :class="[
            'py-3 text-sm font-medium border-b-2 transition-colors',
            activeTab === 'workflows'
              ? 'border-blue-500 text-blue-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          ]"
        >
          Workflows
        </button>
      </nav>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex items-center justify-center py-12">
      <svg class="w-8 h-8 animate-spin text-blue-600" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
      </svg>
    </div>

    <div v-else>
      <!-- Pending Approval Tab -->
      <div v-if="activeTab === 'pending'">
        <div v-if="pendingRequests.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
          <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <p class="text-gray-500 text-lg">No pending approvals</p>
          <p class="text-gray-400 mt-1">Documents awaiting your approval will appear here</p>
        </div>

        <div v-else class="space-y-4">
          <div
            v-for="request in pendingRequests"
            :key="request.id"
            class="bg-white rounded-xl shadow-sm border border-gray-100 p-4"
          >
            <div class="flex items-start justify-between">
              <div class="flex items-start gap-4">
                <div class="w-12 h-12 bg-amber-50 rounded-lg flex items-center justify-center">
                  <svg class="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                </div>
                <div>
                  <h3 class="font-medium text-gray-900">{{ request.documentName }}</h3>
                  <p class="text-sm text-gray-500">Requested by {{ request.requestedByName }}</p>
                  <p class="text-sm text-gray-400 mt-1">{{ formatDate(request.createdAt) }}</p>
                  <p v-if="request.comments" class="text-sm text-gray-600 mt-2 italic">"{{ request.comments }}"</p>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <button
                  @click="viewDocument(request.documentId)"
                  class="px-3 py-1.5 text-sm bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200"
                >
                  View Document
                </button>
                <button
                  @click="openActionModal(request)"
                  class="px-3 py-1.5 text-sm bg-teal text-white rounded-lg hover:bg-teal/90"
                >
                  Review
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- My Requests Tab -->
      <div v-if="activeTab === 'myRequests'">
        <div v-if="myRequests.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
          <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
          </svg>
          <p class="text-gray-500 text-lg">No approval requests</p>
          <p class="text-gray-400 mt-1">Requests you submit will appear here</p>
        </div>

        <div v-else class="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
          <table class="w-full">
            <thead class="bg-gray-50">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Document</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Workflow</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Submitted</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200">
              <tr v-for="request in myRequests" :key="request.id">
                <td class="px-6 py-4">
                  <button
                    @click="viewDocument(request.documentId)"
                    class="text-teal hover:text-teal/80 font-medium"
                  >
                    {{ request.documentName }}
                  </button>
                </td>
                <td class="px-6 py-4 text-sm text-gray-500">
                  {{ request.workflowName || 'Standard' }}
                </td>
                <td class="px-6 py-4 text-sm text-gray-500">
                  {{ formatDate(request.createdAt) }}
                </td>
                <td class="px-6 py-4">
                  <span :class="['px-2.5 py-1 rounded-full text-xs font-medium', getStatusClass(request.status)]">
                    {{ getStatusName(request.status) }}
                  </span>
                </td>
                <td class="px-6 py-4 text-right">
                  <button
                    v-if="request.status === 0"
                    @click="cancelRequest(request)"
                    class="text-sm text-red-600 hover:text-red-700"
                  >
                    Cancel
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Workflows Tab -->
      <div v-if="activeTab === 'workflows'">
        <div v-if="workflows.length === 0" class="bg-white rounded-xl shadow-sm border border-gray-100 p-12 text-center">
          <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
          </svg>
          <p class="text-gray-500 text-lg">No workflows configured</p>
          <p class="text-gray-400 mt-1">Approval workflows can be configured by administrators</p>
        </div>

        <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div
            v-for="workflow in workflows"
            :key="workflow.id"
            class="bg-white rounded-xl shadow-sm border border-gray-100 p-4"
          >
            <div class="flex items-start gap-4">
              <div class="w-12 h-12 bg-purple-50 rounded-lg flex items-center justify-center">
                <svg class="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
                </svg>
              </div>
              <div class="flex-1">
                <h3 class="font-medium text-gray-900">{{ workflow.name }}</h3>
                <p v-if="workflow.description" class="text-sm text-gray-500 mt-1">{{ workflow.description }}</p>
                <div class="flex items-center gap-4 mt-3 text-sm text-gray-500">
                  <span>{{ workflow.requiredApprovers }} approver(s)</span>
                  <span v-if="workflow.isSequential">Sequential</span>
                  <span v-else>Parallel</span>
                </div>
                <div v-if="workflow.steps && workflow.steps.length > 0" class="mt-3">
                  <p class="text-xs font-medium text-gray-500 uppercase mb-2">Steps</p>
                  <div class="flex flex-wrap gap-2">
                    <span
                      v-for="step in workflow.steps"
                      :key="step.id"
                      class="px-2 py-1 bg-gray-100 rounded text-xs text-gray-600"
                    >
                      {{ step.stepOrder }}. {{ step.approverUserName || step.approverRoleName }}
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Action Modal -->
    <div v-if="showActionModal && selectedRequest" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <h2 class="text-lg font-semibold text-gray-900 mb-4">Review: {{ selectedRequest.documentName }}</h2>

        <form @submit.prevent="submitAction" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">Decision</label>
            <div class="flex gap-4">
              <label class="flex items-center gap-2">
                <input
                  v-model="actionData.action"
                  type="radio"
                  :value="1"
                  class="w-4 h-4 text-green-600"
                />
                <span class="text-sm text-gray-700">Approve</span>
              </label>
              <label class="flex items-center gap-2">
                <input
                  v-model="actionData.action"
                  type="radio"
                  :value="2"
                  class="w-4 h-4 text-red-600"
                />
                <span class="text-sm text-gray-700">Reject</span>
              </label>
              <label class="flex items-center gap-2">
                <input
                  v-model="actionData.action"
                  type="radio"
                  :value="3"
                  class="w-4 h-4 text-amber-600"
                />
                <span class="text-sm text-gray-700">Return</span>
              </label>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Comments</label>
            <textarea
              v-model="actionData.comments"
              rows="3"
              placeholder="Add your comments..."
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal/50"
            ></textarea>
          </div>

          <div class="flex justify-end gap-3 pt-4">
            <button
              type="button"
              @click="showActionModal = false"
              class="px-4 py-2 text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              type="submit"
              :class="[
                'px-4 py-2 text-white rounded-lg',
                actionData.action === 1 ? 'bg-green-600 hover:bg-green-700' :
                actionData.action === 2 ? 'bg-red-600 hover:bg-red-700' :
                'bg-amber-600 hover:bg-amber-700'
              ]"
            >
              {{ actionData.action === 1 ? 'Approve' : actionData.action === 2 ? 'Reject' : 'Return' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
