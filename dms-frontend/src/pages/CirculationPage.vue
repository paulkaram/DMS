<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { circulationApi } from '@/api/client'
import type { CirculationRecord } from '@/types'

const router = useRouter()

const allRecords = ref<CirculationRecord[]>([])
const loading = ref(true)
const actionLoading = ref<string | null>(null)

// Checkout modal
const showCheckoutModal = ref(false)
const checkoutForm = ref({
  physicalItemId: '',
  borrowerId: '',
  borrowerStructureId: '',
  purpose: '',
  dueDate: ''
})
const checkoutSubmitting = ref(false)

// Return modal
const showReturnModal = ref(false)
const returnTarget = ref<CirculationRecord | null>(null)
const returnForm = ref({
  condition: 'Good',
  notes: ''
})
const returnSubmitting = ref(false)

// Renew modal
const showRenewModal = ref(false)
const renewTarget = ref<CirculationRecord | null>(null)
const renewForm = ref({
  newDueDate: ''
})
const renewSubmitting = ref(false)

// Report Lost confirm
const showLostConfirm = ref(false)
const lostTarget = ref<CirculationRecord | null>(null)
const lostNotes = ref('')
const lostSubmitting = ref(false)

// Computed stats
const stats = computed(() => {
  const today = new Date().toISOString().split('T')[0]
  return {
    activeLoans: allRecords.value.filter(r => r.status === 'Active').length,
    overdueItems: allRecords.value.filter(r => r.status === 'Overdue').length,
    returnedToday: allRecords.value.filter(r =>
      r.status === 'Returned' && r.returnedAt && r.returnedAt.split('T')[0] === today
    ).length,
    totalCheckouts: allRecords.value.length
  }
})

// Status badge classes
function statusClass(status: string): string {
  switch (status) {
    case 'Active': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
    case 'Overdue': return 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400'
    case 'Returned': return 'bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-400'
    case 'Lost': return 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'
    case 'Renewed': return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
    default: return 'bg-zinc-100 text-zinc-600 dark:bg-zinc-800 dark:text-zinc-400'
  }
}

function canAct(record: CirculationRecord): boolean {
  return record.status === 'Active' || record.status === 'Overdue'
}

function formatDate(dateStr: string | undefined): string {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString()
}

// Load data
async function loadData() {
  loading.value = true
  try {
    const [allRes, overdueRes] = await Promise.all([
      circulationApi.getAll({}),
      circulationApi.getOverdue()
    ])
    const all = allRes.data.data ?? allRes.data ?? []
    const overdue = overdueRes.data.data ?? overdueRes.data ?? []

    // Merge: start with all records, then mark overdue ones
    const overdueIds = new Set(overdue.map((r: CirculationRecord) => r.id))
    allRecords.value = all.map((r: CirculationRecord) => ({
      ...r,
      status: overdueIds.has(r.id) && r.status === 'Active' ? 'Overdue' : r.status
    }))
  } catch {
    /* empty */
  }
  loading.value = false
}

// Checkout
function openCheckoutModal() {
  checkoutForm.value = {
    physicalItemId: '',
    borrowerId: '',
    borrowerStructureId: '',
    purpose: '',
    dueDate: ''
  }
  showCheckoutModal.value = true
}

async function submitCheckout() {
  if (!checkoutForm.value.physicalItemId || !checkoutForm.value.borrowerId || !checkoutForm.value.dueDate) return
  checkoutSubmitting.value = true
  try {
    await circulationApi.checkout({
      physicalItemId: checkoutForm.value.physicalItemId,
      borrowerId: checkoutForm.value.borrowerId,
      borrowerStructureId: checkoutForm.value.borrowerStructureId || undefined,
      purpose: checkoutForm.value.purpose || undefined,
      dueDate: checkoutForm.value.dueDate
    })
    showCheckoutModal.value = false
    await loadData()
  } catch {
    /* empty */
  }
  checkoutSubmitting.value = false
}

// Return
function openReturnModal(record: CirculationRecord) {
  returnTarget.value = record
  returnForm.value = { condition: 'Good', notes: '' }
  showReturnModal.value = true
}

async function submitReturn() {
  if (!returnTarget.value) return
  returnSubmitting.value = true
  try {
    await circulationApi.return(returnTarget.value.id, {
      condition: returnForm.value.condition || undefined,
      notes: returnForm.value.notes || undefined
    })
    showReturnModal.value = false
    returnTarget.value = null
    await loadData()
  } catch {
    /* empty */
  }
  returnSubmitting.value = false
}

// Renew
function openRenewModal(record: CirculationRecord) {
  renewTarget.value = record
  // Default new due date: 14 days from now
  const d = new Date()
  d.setDate(d.getDate() + 14)
  renewForm.value = { newDueDate: d.toISOString().split('T')[0] }
  showRenewModal.value = true
}

async function submitRenew() {
  if (!renewTarget.value || !renewForm.value.newDueDate) return
  renewSubmitting.value = true
  try {
    await circulationApi.renew(renewTarget.value.id, {
      newDueDate: renewForm.value.newDueDate
    })
    showRenewModal.value = false
    renewTarget.value = null
    await loadData()
  } catch {
    /* empty */
  }
  renewSubmitting.value = false
}

// Report Lost
function openLostConfirm(record: CirculationRecord) {
  lostTarget.value = record
  lostNotes.value = ''
  showLostConfirm.value = true
}

async function submitReportLost() {
  if (!lostTarget.value) return
  lostSubmitting.value = true
  try {
    await circulationApi.reportLost(lostTarget.value.id, {
      notes: lostNotes.value || undefined
    })
    showLostConfirm.value = false
    lostTarget.value = null
    await loadData()
  } catch {
    /* empty */
  }
  lostSubmitting.value = false
}

onMounted(loadData)
</script>

<template>
  <div class="p-6">
    <div class="max-w-7xl mx-auto">
      <!-- Back button -->
      <button
        @click="router.push('/archive')"
        class="flex items-center gap-1 text-sm text-gray-500 dark:text-gray-400 hover:text-teal dark:hover:text-teal mb-4 transition-colors"
      >
        <span class="material-symbols-outlined text-lg">arrow_back</span>
        Back to Archive
      </button>

      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Circulation</h1>
          <p class="text-gray-500 dark:text-gray-400 mt-1">Track physical item loans, returns, and overdue items</p>
        </div>
        <button
          @click="openCheckoutModal"
          class="flex items-center gap-2 px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors shadow-sm"
        >
          <span class="material-symbols-outlined text-lg">shopping_cart_checkout</span>
          Checkout Item
        </button>
      </div>

      <!-- Stat Cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <!-- Active Loans -->
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">swap_horiz</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Active Loans</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.activeLoans }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">check_circle</span>
              Currently checked out
            </p>
          </div>
        </div>

        <!-- Overdue Items -->
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">schedule</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Overdue Items</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.overdueItems }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">warning</span>
              Past due date
            </p>
          </div>
        </div>

        <!-- Returned Today -->
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">assignment_return</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Returned Today</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.returnedToday }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">today</span>
              Returns processed
            </p>
          </div>
        </div>

        <!-- Total Checkouts -->
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[140px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none">
            <path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/>
          </svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">inventory_2</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Total Checkouts</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.totalCheckouts }}</p>
            <p class="text-[10px] text-teal mt-4 font-bold flex items-center gap-1 uppercase tracking-tight">
              <span class="material-symbols-outlined text-xs">history</span>
              All time records
            </p>
          </div>
        </div>
      </div>

      <!-- Table -->
      <div class="bg-white dark:bg-surface-dark rounded-xl shadow-sm border border-gray-200 dark:border-border-dark overflow-hidden">
        <!-- Loading -->
        <div v-if="loading" class="flex items-center justify-center py-16">
          <div class="w-6 h-6 border-2 border-teal border-t-transparent rounded-full animate-spin"></div>
        </div>

        <!-- Empty state -->
        <div v-else-if="allRecords.length === 0" class="py-16 text-center">
          <span class="material-symbols-outlined text-5xl text-gray-300 dark:text-gray-600 block mb-3">swap_horiz</span>
          <p class="text-gray-500 dark:text-gray-400 font-medium">No circulation records found</p>
          <p class="text-sm text-gray-400 dark:text-gray-500 mt-1">Use "Checkout Item" to start tracking loans</p>
        </div>

        <!-- Data table -->
        <table v-else class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-[#0d1117] text-xs font-semibold text-zinc-300 uppercase tracking-wider">
              <th class="px-5 py-4">Item</th>
              <th class="px-5 py-4">Barcode</th>
              <th class="px-5 py-4">Borrower</th>
              <th class="px-5 py-4">Checked Out</th>
              <th class="px-5 py-4">Due Date</th>
              <th class="px-5 py-4">Renewals</th>
              <th class="px-5 py-4">Status</th>
              <th class="px-5 py-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="rec in allRecords"
              :key="rec.id"
              class="border-t border-gray-100 dark:border-border-dark hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
            >
              <td class="px-5 py-3.5">
                <span class="font-medium text-sm text-gray-900 dark:text-white">{{ rec.physicalItemTitle || '-' }}</span>
              </td>
              <td class="px-5 py-3.5">
                <span class="font-mono text-xs text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-background-dark px-2 py-0.5 rounded">{{ rec.physicalItemBarcode || '-' }}</span>
              </td>
              <td class="px-5 py-3.5 text-sm text-gray-700 dark:text-gray-300">{{ rec.borrowerName || '-' }}</td>
              <td class="px-5 py-3.5 text-sm text-gray-500 dark:text-gray-400">{{ formatDate(rec.checkedOutAt) }}</td>
              <td class="px-5 py-3.5 text-sm" :class="rec.status === 'Overdue' ? 'text-rose-600 dark:text-rose-400 font-semibold' : 'text-gray-500 dark:text-gray-400'">
                {{ formatDate(rec.dueDate) }}
              </td>
              <td class="px-5 py-3.5 text-sm text-gray-500 dark:text-gray-400">
                {{ rec.renewalCount }}/{{ rec.maxRenewals }}
              </td>
              <td class="px-5 py-3.5">
                <span
                  class="inline-flex items-center px-2.5 py-1 text-xs font-semibold rounded-full"
                  :class="statusClass(rec.status)"
                >
                  {{ rec.status }}
                </span>
              </td>
              <td class="px-5 py-3.5 text-right">
                <div v-if="canAct(rec)" class="flex items-center justify-end gap-1">
                  <button
                    @click="openReturnModal(rec)"
                    class="p-1.5 text-gray-400 hover:text-teal dark:hover:text-teal transition-colors rounded-lg hover:bg-teal/10"
                    title="Return Item"
                  >
                    <span class="material-symbols-outlined text-lg">assignment_return</span>
                  </button>
                  <button
                    @click="openRenewModal(rec)"
                    class="p-1.5 text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 transition-colors rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20"
                    title="Renew Loan"
                    :disabled="rec.renewalCount >= rec.maxRenewals"
                    :class="{ 'opacity-30 cursor-not-allowed': rec.renewalCount >= rec.maxRenewals }"
                  >
                    <span class="material-symbols-outlined text-lg">autorenew</span>
                  </button>
                  <button
                    @click="openLostConfirm(rec)"
                    class="p-1.5 text-gray-400 hover:text-amber-600 dark:hover:text-amber-400 transition-colors rounded-lg hover:bg-amber-50 dark:hover:bg-amber-900/20"
                    title="Report Lost"
                  >
                    <span class="material-symbols-outlined text-lg">report</span>
                  </button>
                </div>
                <span v-else class="text-xs text-gray-400 dark:text-gray-500">--</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ==================== CHECKOUT MODAL ==================== -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showCheckoutModal" class="fixed inset-0 bg-black/50 z-40" @click="showCheckoutModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showCheckoutModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showCheckoutModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-hidden flex flex-col">
            <!-- Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <span class="material-symbols-outlined text-2xl">shopping_cart_checkout</span>
                  <h3 class="text-lg font-bold">Checkout Item</h3>
                </div>
                <button @click="showCheckoutModal = false" class="p-1 hover:bg-white/20 rounded transition-colors">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Body -->
            <div class="p-6 space-y-4 overflow-y-auto">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Physical Item ID or Barcode *</label>
                <input
                  v-model="checkoutForm.physicalItemId"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Enter item ID or scan barcode"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Borrower ID *</label>
                <input
                  v-model="checkoutForm.borrowerId"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Enter borrower user ID"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Borrower Structure (optional)</label>
                <input
                  v-model="checkoutForm.borrowerStructureId"
                  type="text"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Department / structure ID"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Purpose</label>
                <textarea
                  v-model="checkoutForm.purpose"
                  rows="2"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Reason for checking out this item"
                ></textarea>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Due Date *</label>
                <input
                  v-model="checkoutForm.dueDate"
                  type="date"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                />
              </div>
            </div>

            <!-- Footer -->
            <div class="px-6 py-4 border-t border-gray-200 dark:border-border-dark flex justify-end gap-3">
              <button
                @click="showCheckoutModal = false"
                class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="submitCheckout"
                :disabled="!checkoutForm.physicalItemId || !checkoutForm.borrowerId || !checkoutForm.dueDate || checkoutSubmitting"
                class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <div v-if="checkoutSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                Checkout
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ==================== RETURN MODAL ==================== -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showReturnModal" class="fixed inset-0 bg-black/50 z-40" @click="showReturnModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showReturnModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showReturnModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md max-h-[90vh] overflow-hidden flex flex-col">
            <!-- Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <span class="material-symbols-outlined text-2xl">assignment_return</span>
                  <div>
                    <h3 class="text-lg font-bold">Return Item</h3>
                    <p class="text-sm text-white/70">{{ returnTarget?.physicalItemTitle }}</p>
                  </div>
                </div>
                <button @click="showReturnModal = false" class="p-1 hover:bg-white/20 rounded transition-colors">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Body -->
            <div class="p-6 space-y-4 overflow-y-auto">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Condition at Return</label>
                <select
                  v-model="returnForm.condition"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                >
                  <option value="Good">Good</option>
                  <option value="Fair">Fair</option>
                  <option value="Poor">Poor</option>
                  <option value="Damaged">Damaged</option>
                </select>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes</label>
                <textarea
                  v-model="returnForm.notes"
                  rows="3"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Any observations about the returned item..."
                ></textarea>
              </div>
            </div>

            <!-- Footer -->
            <div class="px-6 py-4 border-t border-gray-200 dark:border-border-dark flex justify-end gap-3">
              <button
                @click="showReturnModal = false"
                class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="submitReturn"
                :disabled="returnSubmitting"
                class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <div v-if="returnSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                Confirm Return
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ==================== RENEW MODAL ==================== -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showRenewModal" class="fixed inset-0 bg-black/50 z-40" @click="showRenewModal = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showRenewModal" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showRenewModal = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md max-h-[90vh] overflow-hidden flex flex-col">
            <!-- Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <span class="material-symbols-outlined text-2xl">autorenew</span>
                  <div>
                    <h3 class="text-lg font-bold">Renew Loan</h3>
                    <p class="text-sm text-white/70">{{ renewTarget?.physicalItemTitle }}</p>
                  </div>
                </div>
                <button @click="showRenewModal = false" class="p-1 hover:bg-white/20 rounded transition-colors">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Body -->
            <div class="p-6 space-y-4 overflow-y-auto">
              <!-- Current info -->
              <div class="bg-gray-50 dark:bg-background-dark rounded-lg p-3 space-y-2">
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500 dark:text-gray-400">Borrower</span>
                  <span class="font-medium text-gray-900 dark:text-white">{{ renewTarget?.borrowerName }}</span>
                </div>
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500 dark:text-gray-400">Current Due Date</span>
                  <span class="font-medium text-gray-900 dark:text-white">{{ formatDate(renewTarget?.dueDate) }}</span>
                </div>
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500 dark:text-gray-400">Renewals Used</span>
                  <span class="font-medium text-gray-900 dark:text-white">{{ renewTarget?.renewalCount }}/{{ renewTarget?.maxRenewals }}</span>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">New Due Date *</label>
                <input
                  v-model="renewForm.newDueDate"
                  type="date"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                />
              </div>
            </div>

            <!-- Footer -->
            <div class="px-6 py-4 border-t border-gray-200 dark:border-border-dark flex justify-end gap-3">
              <button
                @click="showRenewModal = false"
                class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
              >
                Cancel
              </button>
              <button
                @click="submitRenew"
                :disabled="!renewForm.newDueDate || renewSubmitting"
                class="px-4 py-2 bg-teal text-white rounded-lg hover:bg-teal/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
              >
                <div v-if="renewSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                Renew Loan
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <!-- ==================== REPORT LOST MODAL ==================== -->
    <Teleport to="body">
      <Transition name="backdrop">
        <div v-if="showLostConfirm" class="fixed inset-0 bg-black/50 z-40" @click="showLostConfirm = false"></div>
      </Transition>
      <Transition name="modal">
        <div v-if="showLostConfirm" class="fixed inset-0 flex items-center justify-center z-50 p-4" @click.self="showLostConfirm = false">
          <div class="bg-white dark:bg-surface-dark rounded-xl shadow-2xl w-full max-w-md overflow-hidden">
            <!-- Header -->
            <div class="bg-gradient-to-r from-[#0d1117] to-teal/80 p-5 text-white relative overflow-hidden">
              <div class="absolute -right-6 -top-6 w-24 h-24 rounded-full bg-white/5"></div>
              <div class="absolute -right-2 bottom-0 w-16 h-16 rounded-full bg-white/5"></div>
              <div class="relative flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <span class="material-symbols-outlined text-2xl">report</span>
                  <h3 class="text-lg font-bold">Report Item Lost</h3>
                </div>
                <button @click="showLostConfirm = false" class="p-1 hover:bg-white/20 rounded transition-colors">
                  <span class="material-symbols-outlined">close</span>
                </button>
              </div>
            </div>

            <!-- Body -->
            <div class="p-6">
              <div class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/50 rounded-lg p-3 mb-4">
                <div class="flex items-start gap-2">
                  <span class="material-symbols-outlined text-amber-600 dark:text-amber-400 text-lg mt-0.5">warning</span>
                  <div>
                    <p class="text-sm font-medium text-amber-800 dark:text-amber-300">
                      Mark "{{ lostTarget?.physicalItemTitle }}" as lost?
                    </p>
                    <p class="text-xs text-amber-600 dark:text-amber-400 mt-1">
                      This will update the item's circulation status. The borrower ({{ lostTarget?.borrowerName }}) will be recorded.
                    </p>
                  </div>
                </div>
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Notes</label>
                <textarea
                  v-model="lostNotes"
                  rows="3"
                  class="w-full px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg bg-white dark:bg-background-dark text-gray-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal"
                  placeholder="Additional details about the loss..."
                ></textarea>
              </div>

              <div class="flex justify-end gap-3 mt-4">
                <button
                  @click="showLostConfirm = false"
                  class="px-4 py-2 border border-gray-300 dark:border-border-dark rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-background-dark transition-colors"
                >
                  Cancel
                </button>
                <button
                  @click="submitReportLost"
                  :disabled="lostSubmitting"
                  class="px-4 py-2 bg-amber-500 text-white rounded-lg hover:bg-amber-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                >
                  <div v-if="lostSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  Report Lost
                </button>
              </div>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.backdrop-enter-active,
.backdrop-leave-active {
  transition: opacity 0.2s ease;
}
.backdrop-enter-from,
.backdrop-leave-to {
  opacity: 0;
}
.modal-enter-active,
.modal-leave-active {
  transition: all 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
  transform: scale(0.95);
}
</style>
