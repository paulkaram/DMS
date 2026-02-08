<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { UiButton, UiInput, UiTextArea, UiSelect, UiModal } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'

// ── Types ────────────────────────────────────────────────────────────
interface WorkflowNode {
  id: string
  type: 'start' | 'approval' | 'review' | 'condition' | 'parallel' | 'notification' | 'timer' | 'end'
  label: string
  description?: string
  x: number
  y: number
  config: Record<string, any>
}

interface WorkflowConnection {
  id: string
  fromNodeId: string
  toNodeId: string
  label?: string
  condition?: string
}

interface WorkflowDefinition {
  id: string
  name: string
  description: string
  status: 'Active' | 'Draft' | 'Inactive'
  triggerType: 'Manual' | 'OnUpload' | 'OnStatusChange' | 'Scheduled'
  version: number
  steps: WorkflowNode[]
  connections: WorkflowConnection[]
  createdAt: string
  updatedAt: string
  createdBy: string
}

// ── Node type definitions ────────────────────────────────────────────
const nodeTypes = [
  { type: 'start', label: 'Start', icon: 'play_circle', color: '#10b981', bgClass: 'bg-emerald-500', shape: 'circle' },
  { type: 'approval', label: 'Approval', icon: 'verified', color: '#00ae8c', bgClass: 'bg-teal', shape: 'rect' },
  { type: 'review', label: 'Review', icon: 'rate_review', color: '#3b82f6', bgClass: 'bg-blue-500', shape: 'rect' },
  { type: 'condition', label: 'Condition', icon: 'call_split', color: '#f59e0b', bgClass: 'bg-amber-500', shape: 'diamond' },
  { type: 'parallel', label: 'Parallel', icon: 'mediation', color: '#a855f7', bgClass: 'bg-purple-500', shape: 'diamond' },
  { type: 'notification', label: 'Notify', icon: 'notifications', color: '#eab308', bgClass: 'bg-yellow-500', shape: 'rect' },
  { type: 'timer', label: 'Timer', icon: 'timer', color: '#64748b', bgClass: 'bg-zinc-500', shape: 'rect' },
  { type: 'end', label: 'End', icon: 'stop_circle', color: '#ef4444', bgClass: 'bg-red-500', shape: 'circle' }
] as const

function getNodeType(type: string) {
  return nodeTypes.find(n => n.type === type) || nodeTypes[0]
}

// ── State ────────────────────────────────────────────────────────────
const currentView = ref<'list' | 'designer'>('list')
const selectedNodeId = ref<string | null>(null)
const showSaveModal = ref(false)
const zoom = ref(1)
const canvasOffset = reactive({ x: 0, y: 0 })
const isPanning = ref(false)
const panStart = reactive({ x: 0, y: 0 })

// Connecting state
const isConnecting = ref(false)
const connectFromNodeId = ref<string | null>(null)

// ── Designer state ───────────────────────────────────────────────────
const designerWorkflow = ref<WorkflowDefinition>({
  id: '',
  name: 'Untitled Workflow',
  description: '',
  status: 'Draft',
  triggerType: 'Manual',
  version: 1,
  steps: [],
  connections: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'Admin'
})

// ── Sample workflows ─────────────────────────────────────────────────
const workflows = ref<WorkflowDefinition[]>([
  {
    id: '1',
    name: 'Document Approval Process',
    description: 'Standard document review and approval workflow with manager sign-off and compliance check.',
    status: 'Active',
    triggerType: 'OnUpload',
    version: 3,
    steps: [
      { id: 'n1', type: 'start', label: 'Document Uploaded', x: 300, y: 60, config: {}, description: 'Triggered on file upload' },
      { id: 'n2', type: 'review', label: 'Initial Review', x: 300, y: 180, config: { assignee: 'Document Controller' }, description: 'Verify document metadata' },
      { id: 'n3', type: 'condition', label: 'Requires Approval?', x: 300, y: 310, config: { condition: 'document.classification == Confidential' }, description: 'Check classification level' },
      { id: 'n4', type: 'approval', label: 'Manager Approval', x: 160, y: 440, config: { assignee: 'Department Manager', timeout: '48h' }, description: 'Manager reviews and approves' },
      { id: 'n5', type: 'notification', label: 'Notify Author', x: 460, y: 440, config: { template: 'auto-approved' }, description: 'Send auto-approval notification' },
      { id: 'n6', type: 'end', label: 'Complete', x: 300, y: 570, config: {}, description: 'Workflow complete' }
    ],
    connections: [
      { id: 'c1', fromNodeId: 'n1', toNodeId: 'n2' },
      { id: 'c2', fromNodeId: 'n2', toNodeId: 'n3' },
      { id: 'c3', fromNodeId: 'n3', toNodeId: 'n4', label: 'Yes', condition: 'true' },
      { id: 'c4', fromNodeId: 'n3', toNodeId: 'n5', label: 'No', condition: 'false' },
      { id: 'c5', fromNodeId: 'n4', toNodeId: 'n6' },
      { id: 'c6', fromNodeId: 'n5', toNodeId: 'n6' }
    ],
    createdAt: '2025-12-01T10:00:00Z',
    updatedAt: '2026-01-28T14:30:00Z',
    createdBy: 'Admin'
  },
  {
    id: '2',
    name: 'Contract Review Pipeline',
    description: 'Multi-stage contract review involving legal, finance, and executive teams.',
    status: 'Active',
    triggerType: 'Manual',
    version: 2,
    steps: [
      { id: 'n1', type: 'start', label: 'Start', x: 300, y: 60, config: {} },
      { id: 'n2', type: 'parallel', label: 'Parallel Review', x: 300, y: 180, config: {} },
      { id: 'n3', type: 'review', label: 'Legal Review', x: 140, y: 310, config: { assignee: 'Legal Team' } },
      { id: 'n4', type: 'review', label: 'Finance Review', x: 460, y: 310, config: { assignee: 'Finance Team' } },
      { id: 'n5', type: 'approval', label: 'Executive Sign-off', x: 300, y: 440, config: { assignee: 'CTO' } },
      { id: 'n6', type: 'end', label: 'Finalized', x: 300, y: 570, config: {} }
    ],
    connections: [
      { id: 'c1', fromNodeId: 'n1', toNodeId: 'n2' },
      { id: 'c2', fromNodeId: 'n2', toNodeId: 'n3', label: 'Branch A' },
      { id: 'c3', fromNodeId: 'n2', toNodeId: 'n4', label: 'Branch B' },
      { id: 'c4', fromNodeId: 'n3', toNodeId: 'n5' },
      { id: 'c5', fromNodeId: 'n4', toNodeId: 'n5' },
      { id: 'c6', fromNodeId: 'n5', toNodeId: 'n6' }
    ],
    createdAt: '2025-11-15T08:00:00Z',
    updatedAt: '2026-02-02T09:15:00Z',
    createdBy: 'Admin'
  },
  {
    id: '3',
    name: 'Expense Report Workflow',
    description: 'Automated expense report processing with threshold-based routing.',
    status: 'Draft',
    triggerType: 'OnStatusChange',
    version: 1,
    steps: [
      { id: 'n1', type: 'start', label: 'Start', x: 300, y: 60, config: {} },
      { id: 'n2', type: 'condition', label: 'Amount Check', x: 300, y: 180, config: { condition: 'amount > 5000' } },
      { id: 'n3', type: 'end', label: 'Done', x: 300, y: 310, config: {} }
    ],
    connections: [
      { id: 'c1', fromNodeId: 'n1', toNodeId: 'n2' },
      { id: 'c2', fromNodeId: 'n2', toNodeId: 'n3' }
    ],
    createdAt: '2026-01-20T12:00:00Z',
    updatedAt: '2026-02-05T16:45:00Z',
    createdBy: 'Admin'
  },
  {
    id: '4',
    name: 'Policy Update Notification',
    description: 'Notify all department heads when policies are updated.',
    status: 'Inactive',
    triggerType: 'OnStatusChange',
    version: 1,
    steps: [
      { id: 'n1', type: 'start', label: 'Start', x: 300, y: 60, config: {} },
      { id: 'n2', type: 'notification', label: 'Notify Heads', x: 300, y: 180, config: {} },
      { id: 'n3', type: 'end', label: 'Done', x: 300, y: 310, config: {} }
    ],
    connections: [
      { id: 'c1', fromNodeId: 'n1', toNodeId: 'n2' },
      { id: 'c2', fromNodeId: 'n2', toNodeId: 'n3' }
    ],
    createdAt: '2025-10-05T07:00:00Z',
    updatedAt: '2025-12-10T11:00:00Z',
    createdBy: 'Admin'
  }
])

// ── Computed ─────────────────────────────────────────────────────────
const stats = computed(() => ({
  total: workflows.value.length,
  active: workflows.value.filter(w => w.status === 'Active').length,
  draft: workflows.value.filter(w => w.status === 'Draft').length,
  recent: workflows.value.filter(w => {
    const diff = Date.now() - new Date(w.updatedAt).getTime()
    return diff < 7 * 24 * 60 * 60 * 1000
  }).length
}))

const selectedNode = computed(() => {
  if (!selectedNodeId.value) return null
  return designerWorkflow.value.steps.find(n => n.id === selectedNodeId.value) || null
})

const selectedNodeType = computed(() => {
  if (!selectedNode.value) return null
  return getNodeType(selectedNode.value.type)
})

// ── List view helpers ────────────────────────────────────────────────
function getStatusClass(status: string) {
  switch (status) {
    case 'Active': return 'bg-emerald-500/15 text-emerald-600 dark:text-emerald-400'
    case 'Draft': return 'bg-amber-500/15 text-amber-600 dark:text-amber-400'
    case 'Inactive': return 'bg-zinc-500/15 text-zinc-500 dark:text-zinc-400'
    default: return 'bg-zinc-500/15 text-zinc-500'
  }
}

function getStatusDot(status: string) {
  switch (status) {
    case 'Active': return 'bg-emerald-500'
    case 'Draft': return 'bg-amber-500'
    case 'Inactive': return 'bg-zinc-400'
    default: return 'bg-zinc-400'
  }
}

function getTriggerIcon(trigger: string) {
  switch (trigger) {
    case 'Manual': return 'touch_app'
    case 'OnUpload': return 'cloud_upload'
    case 'OnStatusChange': return 'swap_horiz'
    case 'Scheduled': return 'schedule'
    default: return 'bolt'
  }
}

function getTriggerLabel(trigger: string) {
  switch (trigger) {
    case 'Manual': return 'Manual'
    case 'OnUpload': return 'On Upload'
    case 'OnStatusChange': return 'On Status Change'
    case 'Scheduled': return 'Scheduled'
    default: return trigger
  }
}

function formatDate(dateStr: string) {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffDays = Math.floor(diffMs / 86400000)
  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays}d ago`
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

// ── Designer actions ─────────────────────────────────────────────────
let nodeCounter = 100

function openDesigner(workflow?: WorkflowDefinition) {
  if (workflow) {
    designerWorkflow.value = JSON.parse(JSON.stringify(workflow))
  } else {
    nodeCounter = 100
    designerWorkflow.value = {
      id: crypto.randomUUID(),
      name: 'Untitled Workflow',
      description: '',
      status: 'Draft',
      triggerType: 'Manual',
      version: 1,
      steps: [
        { id: 'start-1', type: 'start', label: 'Start', x: 300, y: 60, config: {} },
        { id: 'end-1', type: 'end', label: 'End', x: 300, y: 240, config: {} }
      ],
      connections: [
        { id: 'conn-init', fromNodeId: 'start-1', toNodeId: 'end-1' }
      ],
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      createdBy: 'Admin'
    }
  }
  selectedNodeId.value = null
  zoom.value = 1
  canvasOffset.x = 0
  canvasOffset.y = 0
  currentView.value = 'designer'
}

function backToList() {
  currentView.value = 'list'
  selectedNodeId.value = null
}

function addNode(type: string) {
  nodeCounter++
  const nt = getNodeType(type)
  // Find a good position: below the last non-end node
  const nonEndNodes = designerWorkflow.value.steps.filter(n => n.type !== 'end')
  const maxY = nonEndNodes.length > 0 ? Math.max(...nonEndNodes.map(n => n.y)) : 60
  const newNode: WorkflowNode = {
    id: `node-${nodeCounter}`,
    type: type as WorkflowNode['type'],
    label: nt.label,
    description: '',
    x: 300,
    y: maxY + 130,
    config: {}
  }
  // Push end nodes further down
  designerWorkflow.value.steps.forEach(n => {
    if (n.type === 'end' && n.y <= newNode.y) {
      n.y = newNode.y + 130
    }
  })
  designerWorkflow.value.steps.push(newNode)
  selectedNodeId.value = newNode.id
}

function deleteSelectedNode() {
  if (!selectedNodeId.value) return
  const node = selectedNode.value
  if (!node || node.type === 'start' || node.type === 'end') return

  designerWorkflow.value.connections = designerWorkflow.value.connections.filter(
    c => c.fromNodeId !== node.id && c.toNodeId !== node.id
  )
  designerWorkflow.value.steps = designerWorkflow.value.steps.filter(n => n.id !== node.id)
  selectedNodeId.value = null
}

function selectNode(id: string) {
  if (isConnecting.value && connectFromNodeId.value) {
    // Complete the connection
    if (connectFromNodeId.value !== id) {
      const exists = designerWorkflow.value.connections.some(
        c => c.fromNodeId === connectFromNodeId.value && c.toNodeId === id
      )
      if (!exists) {
        designerWorkflow.value.connections.push({
          id: `conn-${Date.now()}`,
          fromNodeId: connectFromNodeId.value,
          toNodeId: id
        })
      }
    }
    isConnecting.value = false
    connectFromNodeId.value = null
    return
  }
  selectedNodeId.value = id
}

function startConnection(nodeId: string) {
  isConnecting.value = true
  connectFromNodeId.value = nodeId
}

function cancelConnection() {
  isConnecting.value = false
  connectFromNodeId.value = null
}

function deleteConnection(connId: string) {
  designerWorkflow.value.connections = designerWorkflow.value.connections.filter(c => c.id !== connId)
}

// ── Node dragging ────────────────────────────────────────────────────
const dragNodeId = ref<string | null>(null)
const dragOffset = reactive({ x: 0, y: 0 })

function onNodeMouseDown(e: MouseEvent, nodeId: string) {
  if (e.button !== 0) return
  e.stopPropagation()
  const node = designerWorkflow.value.steps.find(n => n.id === nodeId)
  if (!node) return
  dragNodeId.value = nodeId
  dragOffset.x = e.clientX - node.x * zoom.value
  dragOffset.y = e.clientY - node.y * zoom.value
  selectedNodeId.value = nodeId

  const onMouseMove = (ev: MouseEvent) => {
    if (!dragNodeId.value) return
    const nd = designerWorkflow.value.steps.find(n => n.id === dragNodeId.value)
    if (!nd) return
    nd.x = Math.round((ev.clientX - dragOffset.x) / zoom.value)
    nd.y = Math.round((ev.clientY - dragOffset.y) / zoom.value)
  }

  const onMouseUp = () => {
    dragNodeId.value = null
    window.removeEventListener('mousemove', onMouseMove)
    window.removeEventListener('mouseup', onMouseUp)
  }

  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
}

// ── Canvas panning ───────────────────────────────────────────────────
function onCanvasMouseDown(e: MouseEvent) {
  if (e.button !== 0 || e.target !== e.currentTarget) return
  if (isConnecting.value) {
    cancelConnection()
    return
  }
  selectedNodeId.value = null
  isPanning.value = true
  panStart.x = e.clientX - canvasOffset.x
  panStart.y = e.clientY - canvasOffset.y

  const onMouseMove = (ev: MouseEvent) => {
    if (!isPanning.value) return
    canvasOffset.x = ev.clientX - panStart.x
    canvasOffset.y = ev.clientY - panStart.y
  }

  const onMouseUp = () => {
    isPanning.value = false
    window.removeEventListener('mousemove', onMouseMove)
    window.removeEventListener('mouseup', onMouseUp)
  }

  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
}

function onCanvasWheel(e: WheelEvent) {
  e.preventDefault()
  const delta = e.deltaY > 0 ? -0.05 : 0.05
  zoom.value = Math.min(2, Math.max(0.3, zoom.value + delta))
}

// ── SVG path helper ──────────────────────────────────────────────────
function getConnectionPath(conn: WorkflowConnection) {
  const from = designerWorkflow.value.steps.find(n => n.id === conn.fromNodeId)
  const to = designerWorkflow.value.steps.find(n => n.id === conn.toNodeId)
  if (!from || !to) return ''

  const x1 = from.x
  const y1 = from.y + 28
  const x2 = to.x
  const y2 = to.y - 28

  const midY = (y1 + y2) / 2
  return `M ${x1} ${y1} C ${x1} ${midY}, ${x2} ${midY}, ${x2} ${y2}`
}

function getConnectionMidpoint(conn: WorkflowConnection) {
  const from = designerWorkflow.value.steps.find(n => n.id === conn.fromNodeId)
  const to = designerWorkflow.value.steps.find(n => n.id === conn.toNodeId)
  if (!from || !to) return { x: 0, y: 0 }
  return {
    x: (from.x + to.x) / 2,
    y: (from.y + to.y) / 2 + 4
  }
}

// ── Node shape dimensions ────────────────────────────────────────────
function getNodeWidth(type: string) {
  const nt = getNodeType(type)
  if (nt.shape === 'circle') return 56
  if (nt.shape === 'diamond') return 56
  return 180
}

function getNodeHeight(type: string) {
  const nt = getNodeType(type)
  if (nt.shape === 'circle') return 56
  if (nt.shape === 'diamond') return 56
  return 56
}

// ── Save modal ───────────────────────────────────────────────────────
const triggerOptions = [
  { value: 'Manual', label: 'Manual Trigger' },
  { value: 'OnUpload', label: 'On File Upload' },
  { value: 'OnStatusChange', label: 'On Status Change' },
  { value: 'Scheduled', label: 'Scheduled' }
]

const statusOptions = [
  { value: 'Draft', label: 'Draft' },
  { value: 'Active', label: 'Active' },
  { value: 'Inactive', label: 'Inactive' }
]

function handleSaveWorkflow() {
  designerWorkflow.value.updatedAt = new Date().toISOString()
  const idx = workflows.value.findIndex(w => w.id === designerWorkflow.value.id)
  if (idx >= 0) {
    workflows.value[idx] = JSON.parse(JSON.stringify(designerWorkflow.value))
  } else {
    workflows.value.push(JSON.parse(JSON.stringify(designerWorkflow.value)))
  }
  showSaveModal.value = false
}

function publishWorkflow() {
  designerWorkflow.value.status = 'Active'
  handleSaveWorkflow()
  backToList()
}

function duplicateWorkflow(wf: WorkflowDefinition) {
  const copy: WorkflowDefinition = JSON.parse(JSON.stringify(wf))
  copy.id = crypto.randomUUID()
  copy.name = `${wf.name} (Copy)`
  copy.status = 'Draft'
  copy.version = 1
  copy.createdAt = new Date().toISOString()
  copy.updatedAt = new Date().toISOString()
  workflows.value.push(copy)
}

function deleteWorkflow(id: string) {
  workflows.value = workflows.value.filter(w => w.id !== id)
}
</script>

<template>
  <div class="p-6">
    <div class="max-w-7xl mx-auto" v-if="currentView === 'list'">
      <!-- Breadcrumb -->
      <AdminBreadcrumb current-page="Workflow Designer" icon="account_tree" />

      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Workflow Designer</h1>
          <p class="text-zinc-500 mt-1">Design and manage automated document processes</p>
        </div>
        <UiButton @click="openDesigner()">
          <span class="material-symbols-outlined text-lg">add</span>
          New Workflow
        </UiButton>
      </div>

      <!-- Stats Cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-6">
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-teal">account_tree</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Total</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.total }}</p>
            <p class="text-[10px] text-teal mt-2 font-medium">All workflows</p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-emerald-400">check_circle</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Active</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.active }}</p>
            <p class="text-[10px] text-emerald-400 mt-2 font-medium">Running processes</p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-amber-400">edit_note</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Draft</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.draft }}</p>
            <p class="text-[10px] text-amber-400 mt-2 font-medium">In progress</p>
          </div>
        </div>
        <div class="bg-[#0d1117] p-6 rounded-2xl text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
          <svg class="absolute right-0 top-0 h-full w-32 opacity-20" viewBox="0 0 100 100" preserveAspectRatio="none"><path d="M0,0 Q50,50 0,100 L100,100 L100,0 Z" fill="#00ae8c"/></svg>
          <div class="flex items-center justify-between relative z-10">
            <span class="material-symbols-outlined text-blue-400">update</span>
            <span class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Recent</span>
          </div>
          <div class="relative z-10">
            <p class="text-4xl font-bold">{{ stats.recent }}</p>
            <p class="text-[10px] text-blue-400 mt-2 font-medium">Modified this week</p>
          </div>
        </div>
      </div>

      <!-- Workflow Cards Grid -->
      <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        <!-- Create New Card -->
        <button
          @click="openDesigner()"
          class="group min-h-[240px] rounded-2xl border-2 border-dashed border-zinc-300 dark:border-zinc-700 hover:border-teal dark:hover:border-teal bg-white/50 dark:bg-zinc-900/50 flex flex-col items-center justify-center gap-4 transition-all hover:shadow-lg hover:shadow-teal/5"
        >
          <div class="w-16 h-16 rounded-2xl bg-teal/10 group-hover:bg-teal/20 flex items-center justify-center transition-colors">
            <span class="material-symbols-outlined text-teal text-3xl">add</span>
          </div>
          <div class="text-center">
            <p class="font-semibold text-zinc-700 dark:text-zinc-300 group-hover:text-teal transition-colors">Create New Workflow</p>
            <p class="text-xs text-zinc-400 mt-1">Design a new automated process</p>
          </div>
        </button>

        <!-- Workflow Cards -->
        <div
          v-for="wf in workflows"
          :key="wf.id"
          class="group bg-white dark:bg-zinc-900 rounded-2xl border border-zinc-200 dark:border-zinc-800 hover:border-teal/40 dark:hover:border-teal/40 shadow-sm hover:shadow-lg hover:shadow-teal/5 transition-all overflow-hidden"
        >
          <!-- Card Header - Mini Flow Preview -->
          <div class="h-28 bg-zinc-50 dark:bg-zinc-800/50 relative overflow-hidden border-b border-zinc-100 dark:border-zinc-800">
            <svg class="w-full h-full" viewBox="0 0 600 120" preserveAspectRatio="xMidYMid meet">
              <!-- Mini node visualization -->
              <g v-for="(step, i) in wf.steps.slice(0, 6)" :key="step.id">
                <circle
                  v-if="getNodeType(step.type).shape === 'circle'"
                  :cx="80 + i * 90"
                  cy="60"
                  r="14"
                  :fill="getNodeType(step.type).color"
                  opacity="0.85"
                />
                <rect
                  v-else-if="getNodeType(step.type).shape === 'rect'"
                  :x="80 + i * 90 - 28"
                  :y="60 - 14"
                  width="56"
                  height="28"
                  rx="8"
                  :fill="getNodeType(step.type).color"
                  opacity="0.85"
                />
                <rect
                  v-else
                  :x="80 + i * 90 - 14"
                  :y="60 - 14"
                  width="28"
                  height="28"
                  rx="4"
                  :fill="getNodeType(step.type).color"
                  opacity="0.85"
                  :transform="`rotate(45, ${80 + i * 90}, 60)`"
                />
                <!-- Connect lines -->
                <line
                  v-if="i < wf.steps.slice(0, 6).length - 1"
                  :x1="80 + i * 90 + 18"
                  y1="60"
                  :x2="80 + (i + 1) * 90 - 18"
                  y2="60"
                  stroke="#00ae8c"
                  stroke-width="2"
                  opacity="0.3"
                  stroke-dasharray="4 3"
                />
              </g>
            </svg>
          </div>

          <!-- Card Body -->
          <div class="p-5">
            <div class="flex items-start justify-between gap-3 mb-3">
              <h3 class="font-bold text-zinc-900 dark:text-white group-hover:text-teal transition-colors line-clamp-1">{{ wf.name }}</h3>
              <span :class="['px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wider rounded-full flex items-center gap-1.5 flex-shrink-0', getStatusClass(wf.status)]">
                <span :class="['w-1.5 h-1.5 rounded-full', getStatusDot(wf.status)]"></span>
                {{ wf.status }}
              </span>
            </div>
            <p class="text-sm text-zinc-500 dark:text-zinc-400 line-clamp-2 mb-4">{{ wf.description }}</p>

            <!-- Meta Row -->
            <div class="flex items-center gap-4 text-xs text-zinc-400 mb-4">
              <span class="flex items-center gap-1">
                <span class="material-symbols-outlined text-sm">{{ getTriggerIcon(wf.triggerType) }}</span>
                {{ getTriggerLabel(wf.triggerType) }}
              </span>
              <span class="flex items-center gap-1">
                <span class="material-symbols-outlined text-sm">conversion_path</span>
                {{ wf.steps.length }} steps
              </span>
              <span class="flex items-center gap-1">
                <span class="material-symbols-outlined text-sm">update</span>
                v{{ wf.version }}
              </span>
            </div>

            <!-- Footer -->
            <div class="flex items-center justify-between pt-3 border-t border-zinc-100 dark:border-zinc-800">
              <span class="text-[10px] text-zinc-400 uppercase tracking-wider">{{ formatDate(wf.updatedAt) }}</span>
              <div class="flex items-center gap-1">
                <button
                  @click="openDesigner(wf)"
                  class="p-1.5 rounded-lg text-zinc-400 hover:text-teal hover:bg-teal/10 transition-colors"
                  title="Edit"
                >
                  <span class="material-symbols-outlined text-lg">edit</span>
                </button>
                <button
                  @click="duplicateWorkflow(wf)"
                  class="p-1.5 rounded-lg text-zinc-400 hover:text-blue-500 hover:bg-blue-500/10 transition-colors"
                  title="Duplicate"
                >
                  <span class="material-symbols-outlined text-lg">content_copy</span>
                </button>
                <button
                  @click="deleteWorkflow(wf.id)"
                  class="p-1.5 rounded-lg text-zinc-400 hover:text-red-500 hover:bg-red-500/10 transition-colors"
                  title="Delete"
                >
                  <span class="material-symbols-outlined text-lg">delete</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════ -->
    <!-- DESIGNER VIEW                                                  -->
    <!-- ═══════════════════════════════════════════════════════════════ -->
    <div v-else class="fixed inset-0 z-40 flex flex-col bg-zinc-100 dark:bg-zinc-950">

      <!-- Top Toolbar -->
      <div class="flex-shrink-0 h-14 bg-white dark:bg-zinc-900 border-b border-zinc-200 dark:border-zinc-800 flex items-center px-4 gap-3 shadow-sm z-20">
        <!-- Back -->
        <button @click="backToList" class="p-2 rounded-lg text-zinc-500 hover:text-zinc-700 dark:hover:text-white hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors">
          <span class="material-symbols-outlined">arrow_back</span>
        </button>
        <div class="w-px h-6 bg-zinc-200 dark:bg-zinc-700"></div>

        <!-- Workflow Name -->
        <div class="flex items-center gap-2 flex-1 min-w-0">
          <span class="material-symbols-outlined text-teal">account_tree</span>
          <input
            v-model="designerWorkflow.name"
            class="bg-transparent border-none outline-none text-lg font-bold text-zinc-900 dark:text-white placeholder-zinc-400 min-w-0 flex-1"
            placeholder="Workflow name..."
          />
          <span :class="['px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider rounded-full', getStatusClass(designerWorkflow.status)]">
            {{ designerWorkflow.status }}
          </span>
        </div>

        <!-- Zoom Controls -->
        <div class="flex items-center gap-1 bg-zinc-100 dark:bg-zinc-800 rounded-lg px-1">
          <button @click="zoom = Math.max(0.3, zoom - 0.1)" class="p-1.5 text-zinc-500 hover:text-zinc-700 dark:hover:text-white transition-colors rounded">
            <span class="material-symbols-outlined text-lg">remove</span>
          </button>
          <span class="text-xs font-mono text-zinc-500 w-12 text-center">{{ Math.round(zoom * 100) }}%</span>
          <button @click="zoom = Math.min(2, zoom + 0.1)" class="p-1.5 text-zinc-500 hover:text-zinc-700 dark:hover:text-white transition-colors rounded">
            <span class="material-symbols-outlined text-lg">add</span>
          </button>
          <button @click="zoom = 1; canvasOffset.x = 0; canvasOffset.y = 0" class="p-1.5 text-zinc-500 hover:text-zinc-700 dark:hover:text-white transition-colors rounded" title="Reset view">
            <span class="material-symbols-outlined text-lg">fit_screen</span>
          </button>
        </div>

        <div class="w-px h-6 bg-zinc-200 dark:bg-zinc-700"></div>

        <!-- Save / Publish -->
        <UiButton variant="outline" size="sm" @click="showSaveModal = true">
          <span class="material-symbols-outlined text-lg">save</span>
          Save
        </UiButton>
        <UiButton size="sm" @click="publishWorkflow">
          <span class="material-symbols-outlined text-lg">rocket_launch</span>
          Publish
        </UiButton>
      </div>

      <div class="flex-1 flex overflow-hidden">
        <!-- Left Sidebar - Node Palette -->
        <div class="flex-shrink-0 w-56 bg-white dark:bg-zinc-900 border-r border-zinc-200 dark:border-zinc-800 flex flex-col z-10">
          <div class="p-3 border-b border-zinc-100 dark:border-zinc-800">
            <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-widest">Node Palette</p>
          </div>
          <div class="flex-1 overflow-y-auto p-3 space-y-1.5">
            <button
              v-for="nt in nodeTypes"
              :key="nt.type"
              @click="addNode(nt.type)"
              class="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-left text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-800 transition-all group"
            >
              <div
                class="w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0 transition-transform group-hover:scale-110"
                :style="{ backgroundColor: nt.color + '20' }"
              >
                <span class="material-symbols-outlined text-lg" :style="{ color: nt.color }">{{ nt.icon }}</span>
              </div>
              <div>
                <span class="block">{{ nt.label }}</span>
                <span class="block text-[10px] text-zinc-400 font-normal">
                  {{ nt.shape === 'circle' ? 'Event' : nt.shape === 'diamond' ? 'Gateway' : 'Task' }}
                </span>
              </div>
            </button>
          </div>

          <!-- Connection Mode Indicator -->
          <div v-if="isConnecting" class="p-3 border-t border-zinc-100 dark:border-zinc-800">
            <div class="bg-teal/10 border border-teal/30 rounded-xl p-3 text-center">
              <p class="text-xs font-semibold text-teal">Connecting...</p>
              <p class="text-[10px] text-zinc-500 mt-1">Click a target node</p>
              <button @click="cancelConnection" class="mt-2 text-[10px] text-zinc-400 hover:text-red-500 transition-colors">Cancel</button>
            </div>
          </div>

          <!-- Quick Help -->
          <div class="p-3 border-t border-zinc-100 dark:border-zinc-800">
            <div class="bg-zinc-50 dark:bg-zinc-800 rounded-xl p-3">
              <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-wider mb-2">Quick Help</p>
              <div class="space-y-1.5 text-[10px] text-zinc-500">
                <p class="flex items-center gap-2"><span class="material-symbols-outlined text-xs">mouse</span> Drag nodes to move</p>
                <p class="flex items-center gap-2"><span class="material-symbols-outlined text-xs">open_with</span> Drag canvas to pan</p>
                <p class="flex items-center gap-2"><span class="material-symbols-outlined text-xs">unfold_more</span> Scroll to zoom</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Center Canvas -->
        <div
          class="flex-1 relative overflow-hidden"
          :class="{ 'cursor-crosshair': isConnecting, 'cursor-grab': !isConnecting && !isPanning, 'cursor-grabbing': isPanning }"
          @mousedown="onCanvasMouseDown"
          @wheel.prevent="onCanvasWheel"
        >
          <!-- Grid Background -->
          <svg class="absolute inset-0 w-full h-full pointer-events-none" xmlns="http://www.w3.org/2000/svg">
            <defs>
              <pattern id="grid-small" width="20" height="20" patternUnits="userSpaceOnUse"
                :patternTransform="`translate(${canvasOffset.x % 20},${canvasOffset.y % 20})`">
                <circle cx="10" cy="10" r="0.5" class="fill-zinc-300 dark:fill-zinc-700" />
              </pattern>
              <pattern id="grid-large" width="100" height="100" patternUnits="userSpaceOnUse"
                :patternTransform="`translate(${canvasOffset.x % 100},${canvasOffset.y % 100})`">
                <circle cx="50" cy="50" r="1" class="fill-zinc-300 dark:fill-zinc-600" />
              </pattern>
            </defs>
            <rect width="100%" height="100%" fill="url(#grid-small)" />
            <rect width="100%" height="100%" fill="url(#grid-large)" />
          </svg>

          <!-- Canvas Content -->
          <div
            class="absolute"
            :style="{
              transform: `translate(${canvasOffset.x}px, ${canvasOffset.y}px) scale(${zoom})`,
              transformOrigin: '0 0'
            }"
          >
            <!-- SVG Connections Layer -->
            <svg class="absolute top-0 left-0 overflow-visible pointer-events-none" width="1" height="1" style="z-index: 1;">
              <defs>
                <marker id="arrowhead" markerWidth="8" markerHeight="6" refX="7" refY="3" orient="auto">
                  <polygon points="0 0, 8 3, 0 6" fill="#00ae8c" />
                </marker>
                <marker id="arrowhead-dim" markerWidth="8" markerHeight="6" refX="7" refY="3" orient="auto">
                  <polygon points="0 0, 8 3, 0 6" fill="#94a3b8" />
                </marker>
              </defs>
              <g v-for="conn in designerWorkflow.connections" :key="conn.id">
                <path
                  :d="getConnectionPath(conn)"
                  fill="none"
                  stroke="#00ae8c"
                  stroke-width="2.5"
                  stroke-linecap="round"
                  marker-end="url(#arrowhead)"
                  class="transition-all"
                  :opacity="selectedNodeId && conn.fromNodeId !== selectedNodeId && conn.toNodeId !== selectedNodeId ? 0.3 : 0.8"
                />
                <!-- Connection label -->
                <g v-if="conn.label" class="pointer-events-auto cursor-pointer" @click="deleteConnection(conn.id)">
                  <rect
                    :x="getConnectionMidpoint(conn).x - 20"
                    :y="getConnectionMidpoint(conn).y - 10"
                    width="40" height="20" rx="6"
                    class="fill-white dark:fill-zinc-800 stroke-zinc-200 dark:stroke-zinc-700"
                    stroke-width="1"
                  />
                  <text
                    :x="getConnectionMidpoint(conn).x"
                    :y="getConnectionMidpoint(conn).y + 4"
                    text-anchor="middle"
                    class="fill-zinc-500 dark:fill-zinc-400"
                    font-size="10"
                    font-weight="600"
                  >{{ conn.label }}</text>
                </g>
              </g>
            </svg>

            <!-- Nodes -->
            <div
              v-for="node in designerWorkflow.steps"
              :key="node.id"
              class="absolute select-none"
              :style="{
                left: `${node.x - getNodeWidth(node.type) / 2}px`,
                top: `${node.y - getNodeHeight(node.type) / 2}px`,
                zIndex: selectedNodeId === node.id ? 20 : 10
              }"
              @mousedown="onNodeMouseDown($event, node.id)"
              @click.stop="selectNode(node.id)"
            >
              <!-- Circle shape (Start / End) -->
              <div
                v-if="getNodeType(node.type).shape === 'circle'"
                class="w-14 h-14 rounded-full flex items-center justify-center shadow-lg transition-all cursor-move"
                :class="[
                  selectedNodeId === node.id ? 'ring-2 ring-teal ring-offset-2 ring-offset-zinc-100 dark:ring-offset-zinc-950 scale-110' : 'hover:scale-105'
                ]"
                :style="{ backgroundColor: getNodeType(node.type).color }"
              >
                <span class="material-symbols-outlined text-white text-xl">{{ getNodeType(node.type).icon }}</span>
              </div>

              <!-- Diamond shape (Condition / Parallel) -->
              <div
                v-else-if="getNodeType(node.type).shape === 'diamond'"
                class="w-14 h-14 flex items-center justify-center cursor-move"
              >
                <div
                  class="w-11 h-11 rotate-45 rounded-md shadow-lg transition-all"
                  :class="[
                    selectedNodeId === node.id ? 'ring-2 ring-teal ring-offset-2 ring-offset-zinc-100 dark:ring-offset-zinc-950 scale-110' : 'hover:scale-105'
                  ]"
                  :style="{ backgroundColor: getNodeType(node.type).color }"
                >
                  <div class="w-full h-full flex items-center justify-center -rotate-45">
                    <span class="material-symbols-outlined text-white text-lg">{{ getNodeType(node.type).icon }}</span>
                  </div>
                </div>
              </div>

              <!-- Rect shape (Tasks) -->
              <div
                v-else
                class="h-14 rounded-xl shadow-lg flex items-center gap-2.5 px-3 transition-all cursor-move bg-white dark:bg-zinc-800 border-2"
                :class="[
                  selectedNodeId === node.id
                    ? 'border-teal shadow-teal/20 shadow-xl scale-105'
                    : 'border-zinc-200 dark:border-zinc-700 hover:border-zinc-300 dark:hover:border-zinc-600 hover:scale-[1.02]'
                ]"
                :style="{ width: getNodeWidth(node.type) + 'px' }"
              >
                <div
                  class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                  :style="{ backgroundColor: getNodeType(node.type).color + '20' }"
                >
                  <span class="material-symbols-outlined text-base" :style="{ color: getNodeType(node.type).color }">{{ getNodeType(node.type).icon }}</span>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-xs font-semibold text-zinc-800 dark:text-white truncate leading-tight">{{ node.label }}</p>
                  <p class="text-[9px] text-zinc-400 truncate leading-tight mt-0.5" v-if="node.config?.assignee">{{ node.config.assignee }}</p>
                </div>
              </div>

              <!-- Node label for circles/diamonds -->
              <p
                v-if="getNodeType(node.type).shape !== 'rect'"
                class="text-[10px] font-semibold text-zinc-600 dark:text-zinc-400 text-center mt-1 whitespace-nowrap"
                :style="{ width: '80px', marginLeft: '-13px' }"
              >{{ node.label }}</p>

              <!-- Connect button (hover) -->
              <button
                v-if="!isConnecting"
                @click.stop="startConnection(node.id)"
                class="absolute -bottom-2 left-1/2 -translate-x-1/2 w-5 h-5 rounded-full bg-teal text-white flex items-center justify-center opacity-0 hover:opacity-100 transition-all shadow-md hover:scale-125 z-30"
                :class="{ '!opacity-60': selectedNodeId === node.id }"
                title="Drag to connect"
              >
                <span class="material-symbols-outlined text-xs">add</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Right Panel - Properties -->
        <div class="flex-shrink-0 w-72 bg-white dark:bg-zinc-900 border-l border-zinc-200 dark:border-zinc-800 flex flex-col z-10">
          <div v-if="selectedNode" class="flex-1 flex flex-col overflow-hidden">
            <!-- Panel Header -->
            <div class="p-4 border-b border-zinc-100 dark:border-zinc-800 flex items-center justify-between">
              <div class="flex items-center gap-2.5">
                <div class="w-8 h-8 rounded-lg flex items-center justify-center" :style="{ backgroundColor: selectedNodeType?.color + '20' }">
                  <span class="material-symbols-outlined text-base" :style="{ color: selectedNodeType?.color }">{{ selectedNodeType?.icon }}</span>
                </div>
                <div>
                  <p class="text-xs font-bold text-zinc-900 dark:text-white">{{ selectedNodeType?.label }} Node</p>
                  <p class="text-[10px] text-zinc-400">{{ selectedNode.id }}</p>
                </div>
              </div>
              <button
                v-if="selectedNode.type !== 'start' && selectedNode.type !== 'end'"
                @click="deleteSelectedNode"
                class="p-1.5 rounded-lg text-zinc-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
              >
                <span class="material-symbols-outlined text-lg">delete</span>
              </button>
            </div>

            <!-- Properties -->
            <div class="flex-1 overflow-y-auto p-4 space-y-4">
              <div>
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">Label</label>
                <input
                  v-model="selectedNode.label"
                  class="w-full px-3 py-2 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none transition-all"
                />
              </div>

              <div>
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">Description</label>
                <textarea
                  v-model="selectedNode.description"
                  rows="2"
                  class="w-full px-3 py-2 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none transition-all resize-none"
                  placeholder="Describe this step..."
                ></textarea>
              </div>

              <!-- Assignee for approval/review -->
              <div v-if="selectedNode.type === 'approval' || selectedNode.type === 'review'">
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">Assignee</label>
                <input
                  v-model="selectedNode.config.assignee"
                  class="w-full px-3 py-2 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none transition-all"
                  placeholder="User or role..."
                />
              </div>

              <!-- Condition for branch nodes -->
              <div v-if="selectedNode.type === 'condition'">
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">Condition Expression</label>
                <textarea
                  v-model="selectedNode.config.condition"
                  rows="3"
                  class="w-full px-3 py-2 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white font-mono text-xs focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none transition-all resize-none"
                  placeholder="e.g. document.classification == 'Confidential'"
                ></textarea>
              </div>

              <!-- Timeout for approval/review -->
              <div v-if="selectedNode.type === 'approval' || selectedNode.type === 'review' || selectedNode.type === 'timer'">
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">
                  {{ selectedNode.type === 'timer' ? 'Duration' : 'Timeout' }}
                </label>
                <input
                  v-model="selectedNode.config.timeout"
                  class="w-full px-3 py-2 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none transition-all"
                  placeholder="e.g. 48h, 3d, 1w"
                />
              </div>

              <!-- Notification template -->
              <div v-if="selectedNode.type === 'notification'">
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">Template</label>
                <input
                  v-model="selectedNode.config.template"
                  class="w-full px-3 py-2 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none transition-all"
                  placeholder="Notification template name..."
                />
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5 mt-4">Recipients</label>
                <input
                  v-model="selectedNode.config.recipients"
                  class="w-full px-3 py-2 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-sm text-zinc-900 dark:text-white focus:ring-2 focus:ring-teal/50 focus:border-teal outline-none transition-all"
                  placeholder="Users, roles, or groups..."
                />
              </div>

              <!-- Position -->
              <div>
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">Position</label>
                <div class="flex gap-2">
                  <div class="flex-1">
                    <label class="text-[9px] text-zinc-400">X</label>
                    <input
                      v-model.number="selectedNode.x"
                      type="number"
                      class="w-full px-2 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-xs text-zinc-900 dark:text-white font-mono focus:ring-1 focus:ring-teal/50 outline-none"
                    />
                  </div>
                  <div class="flex-1">
                    <label class="text-[9px] text-zinc-400">Y</label>
                    <input
                      v-model.number="selectedNode.y"
                      type="number"
                      class="w-full px-2 py-1.5 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-800 text-xs text-zinc-900 dark:text-white font-mono focus:ring-1 focus:ring-teal/50 outline-none"
                    />
                  </div>
                </div>
              </div>

              <!-- Connections -->
              <div>
                <label class="block text-[10px] font-bold text-zinc-500 uppercase tracking-wider mb-1.5">Connections</label>
                <div class="space-y-1.5">
                  <div
                    v-for="conn in designerWorkflow.connections.filter(c => c.fromNodeId === selectedNode!.id || c.toNodeId === selectedNode!.id)"
                    :key="conn.id"
                    class="flex items-center justify-between bg-zinc-50 dark:bg-zinc-800 rounded-lg px-2.5 py-1.5"
                  >
                    <span class="text-[10px] text-zinc-600 dark:text-zinc-400 font-mono">
                      {{ conn.fromNodeId === selectedNode!.id ? '→ ' + conn.toNodeId : conn.fromNodeId + ' →' }}
                    </span>
                    <button @click="deleteConnection(conn.id)" class="text-zinc-400 hover:text-red-500 transition-colors">
                      <span class="material-symbols-outlined text-xs">close</span>
                    </button>
                  </div>
                  <p v-if="designerWorkflow.connections.filter(c => c.fromNodeId === selectedNode!.id || c.toNodeId === selectedNode!.id).length === 0" class="text-[10px] text-zinc-400 italic">
                    No connections
                  </p>
                </div>
              </div>
            </div>
          </div>

          <!-- No Selection -->
          <div v-else class="flex-1 flex flex-col items-center justify-center p-6 text-center">
            <div class="w-16 h-16 rounded-2xl bg-zinc-100 dark:bg-zinc-800 flex items-center justify-center mb-4">
              <span class="material-symbols-outlined text-3xl text-zinc-300 dark:text-zinc-600">touch_app</span>
            </div>
            <p class="text-sm font-semibold text-zinc-500 dark:text-zinc-400">Select a node</p>
            <p class="text-xs text-zinc-400 mt-1">Click a node on the canvas to view and edit its properties</p>
          </div>
        </div>
      </div>
    </div>

    <!-- ═══════════════════════════════════════════════════════════════ -->
    <!-- SAVE MODAL                                                     -->
    <!-- ═══════════════════════════════════════════════════════════════ -->
    <UiModal v-model="showSaveModal" title="Save Workflow" size="lg">
      <div class="space-y-5">
        <UiInput
          v-model="designerWorkflow.name"
          label="Workflow Name"
          placeholder="Enter workflow name"
        />
        <UiTextArea
          v-model="designerWorkflow.description"
          label="Description"
          placeholder="Describe the purpose of this workflow..."
          :rows="3"
        />
        <div class="grid grid-cols-2 gap-4">
          <UiSelect
            v-model="designerWorkflow.triggerType"
            label="Trigger Type"
            :options="triggerOptions"
          />
          <UiSelect
            v-model="designerWorkflow.status"
            label="Status"
            :options="statusOptions"
          />
        </div>
        <div class="bg-zinc-50 dark:bg-zinc-800 rounded-xl p-4">
          <p class="text-[10px] font-bold text-zinc-400 uppercase tracking-wider mb-2">Summary</p>
          <div class="grid grid-cols-3 gap-4 text-center">
            <div>
              <p class="text-2xl font-bold text-zinc-900 dark:text-white">{{ designerWorkflow.steps.length }}</p>
              <p class="text-[10px] text-zinc-400">Steps</p>
            </div>
            <div>
              <p class="text-2xl font-bold text-zinc-900 dark:text-white">{{ designerWorkflow.connections.length }}</p>
              <p class="text-[10px] text-zinc-400">Connections</p>
            </div>
            <div>
              <p class="text-2xl font-bold text-zinc-900 dark:text-white">v{{ designerWorkflow.version }}</p>
              <p class="text-[10px] text-zinc-400">Version</p>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <div class="flex justify-end gap-3">
          <UiButton variant="outline" @click="showSaveModal = false">Cancel</UiButton>
          <UiButton @click="handleSaveWorkflow">
            Save Workflow
          </UiButton>
        </div>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.line-clamp-1 {
  display: -webkit-box;
  -webkit-line-clamp: 1;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
