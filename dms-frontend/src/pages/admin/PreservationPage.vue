<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { preservationApi } from '@/api/client'
import type { PreservationSummary, PreservationFormat } from '@/types'

const summary = ref<PreservationSummary | null>(null)
const formats = ref<PreservationFormat[]>([])
const loading = ref(true)

async function loadData() {
  loading.value = true
  try {
    const [summaryRes, formatsRes] = await Promise.all([
      preservationApi.getSummary(),
      preservationApi.getApprovedFormats()
    ])
    summary.value = summaryRes.data.data ?? summaryRes.data
    formats.value = formatsRes.data.data ?? formatsRes.data ?? []
  } catch { /* empty */ }
  loading.value = false
}

onMounted(loadData)
</script>

<template>
  <div class="max-w-6xl mx-auto px-6 py-8">
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Preservation Management</h1>
        <p class="text-sm text-gray-500 mt-1">ISO 14721 (OAIS) digital preservation format compliance</p>
      </div>
    </div>

    <div v-if="loading" class="flex items-center justify-center py-20">
      <span class="material-symbols-outlined animate-spin text-4xl text-gray-400">progress_activity</span>
    </div>

    <template v-else>
      <!-- Summary Cards -->
      <div v-if="summary" class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
          <div class="text-2xl font-bold text-gray-900">{{ summary.totalDocuments }}</div>
          <div class="text-sm text-gray-500">Records/Archived Documents</div>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
          <div class="text-2xl font-bold text-green-600">{{ summary.preservationCompliant }}</div>
          <div class="text-sm text-gray-500">Preservation Compliant</div>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
          <div class="text-2xl font-bold text-amber-600">{{ summary.needsMigration }}</div>
          <div class="text-sm text-gray-500">Needs Format Migration</div>
        </div>
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-5">
          <div class="text-2xl font-bold text-teal-600">{{ summary.pdfACompliant }}</div>
          <div class="text-sm text-gray-500">PDF/A Compliant</div>
        </div>
      </div>

      <!-- Format Distribution -->
      <div v-if="summary && summary.formatDistribution.length > 0" class="bg-white rounded-xl shadow-sm border border-gray-200 mb-8">
        <div class="p-4 border-b border-gray-100">
          <h3 class="text-sm font-semibold text-gray-700">Format Distribution</h3>
        </div>
        <div class="p-4 space-y-2">
          <div v-for="fd in summary.formatDistribution" :key="fd.extension" class="flex items-center gap-3">
            <span class="text-sm font-mono w-16">{{ fd.extension }}</span>
            <div class="flex-1 bg-gray-100 rounded-full h-5 relative overflow-hidden">
              <div class="h-full rounded-full transition-all"
                :class="fd.isPreservationFormat ? 'bg-green-500' : 'bg-amber-400'"
                :style="{ width: `${Math.max((fd.count / summary!.totalDocuments) * 100, 2)}%` }"
              ></div>
            </div>
            <span class="text-sm text-gray-600 w-12 text-right">{{ fd.count }}</span>
            <span class="material-symbols-outlined text-sm" :class="fd.isPreservationFormat ? 'text-green-600' : 'text-amber-500'">
              {{ fd.isPreservationFormat ? 'check_circle' : 'warning' }}
            </span>
          </div>
        </div>
      </div>

      <!-- Approved Formats -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200">
        <div class="p-4 border-b border-gray-100">
          <h3 class="text-sm font-semibold text-gray-700">Approved Preservation Formats</h3>
        </div>
        <table class="w-full text-sm">
          <thead class="bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
            <tr>
              <th class="px-4 py-3">Extension</th>
              <th class="px-4 py-3">Format</th>
              <th class="px-4 py-3">PRONOM PUID</th>
              <th class="px-4 py-3">Approved</th>
              <th class="px-4 py-3">Migration Target</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="f in formats" :key="f.extension" class="hover:bg-gray-50">
              <td class="px-4 py-3 font-mono">{{ f.extension }}</td>
              <td class="px-4 py-3 font-medium text-gray-900">{{ f.formatName }}</td>
              <td class="px-4 py-3 text-gray-500 font-mono text-xs">{{ f.pronomPuid || '-' }}</td>
              <td class="px-4 py-3">
                <span class="material-symbols-outlined text-sm" :class="f.isApprovedForPreservation ? 'text-green-600' : 'text-gray-400'">
                  {{ f.isApprovedForPreservation ? 'check_circle' : 'cancel' }}
                </span>
              </td>
              <td class="px-4 py-3 text-gray-500">{{ f.migrationTargetFormat || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>
