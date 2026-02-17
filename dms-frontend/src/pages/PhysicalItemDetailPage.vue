<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { physicalItemsApi } from '@/api/client'
import type { PhysicalItem, CustodyTransfer } from '@/types'

const route = useRoute()
const item = ref<PhysicalItem | null>(null)
const custodyChain = ref<CustodyTransfer[]>([])
const loading = ref(true)

async function loadItem() {
  loading.value = true
  try {
    const id = route.params.id as string
    const { data } = await physicalItemsApi.getById(id)
    item.value = data.data ?? data
    const custodyRes = await physicalItemsApi.getCustody(id)
    custodyChain.value = custodyRes.data.data ?? custodyRes.data ?? []
  } catch { /* empty */ }
  loading.value = false
}

onMounted(loadItem)
</script>

<template>
  <div class="max-w-5xl mx-auto px-6 py-8">
    <div v-if="loading" class="flex items-center justify-center py-20">
      <span class="material-symbols-outlined animate-spin text-4xl text-gray-400">progress_activity</span>
    </div>

    <template v-else-if="item">
      <div class="flex items-center gap-4 mb-8">
        <button @click="$router.back()" class="p-2 rounded-lg hover:bg-gray-100">
          <span class="material-symbols-outlined">arrow_back</span>
        </button>
        <div>
          <h1 class="text-2xl font-bold text-gray-900">{{ item.title }}</h1>
          <p class="text-sm text-gray-500 font-mono">{{ item.barcode }}</p>
        </div>
      </div>

      <!-- Item Details Card -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-6">
        <h3 class="text-sm font-semibold text-gray-700 mb-4">Item Details</h3>
        <div class="grid grid-cols-2 md:grid-cols-3 gap-4 text-sm">
          <div><span class="text-gray-500">Type:</span> <span class="font-medium ml-2">{{ item.itemType }}</span></div>
          <div><span class="text-gray-500">Condition:</span> <span class="font-medium ml-2">{{ item.condition }}</span></div>
          <div><span class="text-gray-500">Status:</span> <span class="font-medium ml-2">{{ item.circulationStatus }}</span></div>
          <div><span class="text-gray-500">Location:</span> <span class="font-medium ml-2">{{ item.locationName || 'Unassigned' }}</span></div>
          <div v-if="item.pageCount"><span class="text-gray-500">Pages:</span> <span class="font-medium ml-2">{{ item.pageCount }}</span></div>
          <div v-if="item.dimensions"><span class="text-gray-500">Dimensions:</span> <span class="font-medium ml-2">{{ item.dimensions }}</span></div>
          <div v-if="item.itemDate"><span class="text-gray-500">Date:</span> <span class="font-medium ml-2">{{ new Date(item.itemDate).toLocaleDateString() }}</span></div>
          <div><span class="text-gray-500">Legal Hold:</span> <span class="font-medium ml-2" :class="item.isOnLegalHold ? 'text-red-600' : 'text-green-600'">{{ item.isOnLegalHold ? 'Yes' : 'No' }}</span></div>
        </div>
        <div v-if="item.description" class="mt-4 text-sm text-gray-600">{{ item.description }}</div>
      </div>

      <!-- Custody Chain -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <h3 class="text-sm font-semibold text-gray-700 mb-4">Chain of Custody</h3>
        <div v-if="custodyChain.length === 0" class="text-sm text-gray-400">No custody transfers recorded.</div>
        <div v-else class="space-y-3">
          <div
            v-for="transfer in custodyChain"
            :key="transfer.id"
            class="flex items-start gap-3 p-3 rounded-lg bg-gray-50"
          >
            <span class="material-symbols-outlined text-gray-400 mt-0.5">swap_horiz</span>
            <div class="flex-1 text-sm">
              <div class="font-medium text-gray-900">
                {{ transfer.fromUserName || 'System' }} → {{ transfer.toUserName || 'System' }}
              </div>
              <div class="text-xs text-gray-500 mt-0.5">
                {{ transfer.transferType }} &middot; {{ new Date(transfer.transferredAt).toLocaleString() }}
                <span v-if="transfer.isAcknowledged" class="text-green-600 ml-2">Acknowledged</span>
                <span v-else class="text-amber-600 ml-2">Pending acknowledgment</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
