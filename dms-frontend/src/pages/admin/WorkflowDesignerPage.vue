<script setup lang="ts">
import { ref, computed, reactive, onMounted, onUnmounted, watch } from 'vue'
import { UiButton, UiInput, UiTextArea, UiSelect, UiModal } from '@/components/ui'
import AdminBreadcrumb from '@/components/admin/AdminBreadcrumb.vue'
import { structuresApi, usersApi, rolesApi, approvalsApi, foldersApi, cabinetsApi, workflowStatusesApi } from '@/api/client'
import type { StructureTree, User, Role, WorkflowStatus } from '@/types'

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
  fromPort?: 'bottom-left' | 'bottom' | 'bottom-right'
  toPort?: 'top-left' | 'top' | 'top-right'
  label?: string
  condition?: string
}

interface WorkflowDefinition {
  id: string
  name: string
  description: string
  status: 'Active' | 'Draft' | 'Inactive'
  triggerType: 'Manual' | 'OnUpload' | 'OnStatusChange' | 'Scheduled'
  inheritToSubfolders: boolean
  folderId?: string
  folderName?: string
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

// Connecting state (port drag)
const isConnecting = ref(false)
const connectFromNodeId = ref<string | null>(null)
const connectMousePos = reactive({ x: 0, y: 0 })
const connectFromPort = ref('bottom')
const canvasContainerRef = ref<HTMLElement | null>(null)

// Structure tree for assignment picker
const structureTree = ref<StructureTree[]>([])
const flatStructures = computed(() => {
  const result: { id: string; name: string; type: string }[] = []
  function flatten(nodes: StructureTree[], depth = 0) {
    for (const node of nodes) {
      result.push({ id: node.id, name: '\u00A0'.repeat(depth * 3) + node.name, type: node.type })
      if (node.children?.length) flatten(node.children, depth + 1)
    }
  }
  flatten(structureTree.value)
  return result
})

async function loadStructureTree() {
  try {
    const res = await structuresApi.getTree()
    structureTree.value = res.data
  } catch { /* silent */ }
}

// ── Autocomplete state ───────────────────────────────────────────────
// User autocomplete
const userSearchQuery = ref('')
const userSearchResults = ref<User[]>([])
const showUserDropdown = ref(false)
const isSearchingUsers = ref(false)
let userSearchTimer: ReturnType<typeof setTimeout> | null = null

// Role autocomplete
const roleSearchQuery = ref('')
const allRoles = ref<Role[]>([])
const showRoleDropdown = ref(false)

// Structure autocomplete
const structureSearchQuery = ref('')
const showStructureDropdown = ref(false)

const filteredRoles = computed(() => {
  const q = roleSearchQuery.value.toLowerCase().trim()
  if (!q) return allRoles.value
  return allRoles.value.filter(r => r.name.toLowerCase().includes(q))
})

const filteredStructures = computed(() => {
  const q = structureSearchQuery.value.toLowerCase().trim()
  if (!q) return flatStructures.value
  return flatStructures.value.filter(s => s.name.trim().toLowerCase().includes(q))
})

async function loadRoles() {
  try {
    const res = await rolesApi.getAll()
    const data = res.data
    allRoles.value = Array.isArray(data) ? data : data.items ?? []
  } catch { /* silent */ }
}

// Workflow statuses
const workflowStatuses = ref<WorkflowStatus[]>([])

async function loadWorkflowStatuses() {
  try {
    const res = await workflowStatusesApi.getAll()
    workflowStatuses.value = res.data
  } catch { /* silent */ }
}

const stepStatusOptions = computed(() => [
  { value: null, label: 'No status' },
  ...workflowStatuses.value.map(ws => ({ value: ws.id, label: ws.name }))
])

function onStatusChange(val: string | number | null) {
  if (!selectedNode.value) return
  selectedNode.value.config.statusId = val || undefined
  const ws = workflowStatuses.value.find(s => s.id === val)
  selectedNode.value.config.statusName = ws?.name
  selectedNode.value.config.statusColor = ws?.color
}

function onUserSearchInput(query: string) {
  userSearchQuery.value = query
  if (userSearchTimer) clearTimeout(userSearchTimer)
  if (!query.trim()) {
    userSearchResults.value = []
    showUserDropdown.value = false
    return
  }
  isSearchingUsers.value = true
  userSearchTimer = setTimeout(async () => {
    try {
      const res = await usersApi.search(query)
      const data = res.data
      userSearchResults.value = Array.isArray(data) ? data : data.items ?? []
      showUserDropdown.value = userSearchResults.value.length > 0
    } catch {
      userSearchResults.value = []
    } finally {
      isSearchingUsers.value = false
    }
  }, 300)
}

function selectUser(user: User) {
  if (!selectedNode.value) return
  selectedNode.value.config.approverUserId = user.id
  selectedNode.value.config.assignee = user.displayName || user.username
  selectedNode.value.config.assignmentMode = 'user'
  userSearchQuery.value = user.displayName || user.username
  showUserDropdown.value = false
}

function selectRole(role: Role) {
  if (!selectedNode.value) return
  selectedNode.value.config.approverRoleId = role.id
  selectedNode.value.config.assignee = role.name
  selectedNode.value.config.assignmentMode = 'role'
  roleSearchQuery.value = role.name
  showRoleDropdown.value = false
}

function selectStructure(structure: { id: string; name: string; type: string }) {
  if (!selectedNode.value) return
  selectedNode.value.config.approverStructureId = structure.id
  selectedNode.value.config.assignmentMode = 'structure'
  structureSearchQuery.value = structure.name.trim()
  showStructureDropdown.value = false
}

function clearAssignment() {
  if (!selectedNode.value) return
  delete selectedNode.value.config.approverUserId
  delete selectedNode.value.config.approverRoleId
  delete selectedNode.value.config.approverStructureId
  delete selectedNode.value.config.assignee
  delete selectedNode.value.config.assignToManager
  userSearchQuery.value = ''
  roleSearchQuery.value = ''
  structureSearchQuery.value = ''
}

// Sync autocomplete fields when node selection changes
watch(selectedNodeId, () => {
  showUserDropdown.value = false
  showRoleDropdown.value = false
  showStructureDropdown.value = false
  userSearchQuery.value = ''
  roleSearchQuery.value = ''
  structureSearchQuery.value = ''

  if (selectedNode.value && (selectedNode.value.type === 'approval' || selectedNode.value.type === 'review')) {
    const mode = getAssignmentMode(selectedNode.value)
    if (mode === 'user' && selectedNode.value.config.approverUserId) {
      userSearchQuery.value = selectedNode.value.config.assignee || ''
    } else if (mode === 'role' && selectedNode.value.config.approverRoleId) {
      roleSearchQuery.value = selectedNode.value.config.assignee || ''
    } else if (mode === 'structure' && selectedNode.value.config.approverStructureId) {
      const s = flatStructures.value.find(s => s.id === selectedNode.value!.config.approverStructureId)
      structureSearchQuery.value = s?.name?.trim() || ''
    }
  }
})

// Palette drag-drop state
const isDraggingFromPalette = ref(false)
const paletteDropType = ref('')
const paletteGhostPos = reactive({ x: 0, y: 0 })

// ── Designer state ───────────────────────────────────────────────────
const isNewWorkflow = ref(false)
const isLoadingList = ref(false)
const isSaving = ref(false)
const showWorkflowProps = ref(false)
const toast = ref<{ show: boolean; message: string; type: 'success' | 'error' }>({ show: false, message: '', type: 'success' })

function showToast(message: string, type: 'success' | 'error' = 'success') {
  toast.value = { show: true, message, type }
  setTimeout(() => { toast.value.show = false }, 3000)
}

const designerWorkflow = ref<WorkflowDefinition>({
  id: '',
  name: 'Untitled Workflow',
  description: '',
  status: 'Draft',
  triggerType: 'Manual',
  inheritToSubfolders: true,
  folderId: undefined,
  folderName: undefined,
  version: 1,
  steps: [],
  connections: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  createdBy: 'Admin'
})

// ── Workflows from API ──────────────────────────────────────────────
const workflows = ref<WorkflowDefinition[]>([])

async function loadWorkflows() {
  isLoadingList.value = true
  try {
    const res = await approvalsApi.getWorkflows()
    const data = res.data
    const list = Array.isArray(data) ? data : data.items ?? []
    workflows.value = list.map(mapApiToDesigner)
  } catch {
    showToast('Failed to load workflows', 'error')
  } finally {
    isLoadingList.value = false
  }
}

function mapApiToDesigner(apiWf: any): WorkflowDefinition {
  // If designerData exists, restore the full visual canvas state
  if (apiWf.designerData) {
    try {
      const saved = JSON.parse(apiWf.designerData)
      return {
        id: apiWf.id,
        name: apiWf.name,
        description: apiWf.description || '',
        status: saved.status || (apiWf.isActive ? 'Active' : 'Draft'),
        triggerType: apiWf.triggerType || saved.triggerType || 'Manual',
        inheritToSubfolders: apiWf.inheritToSubfolders ?? true,
        folderId: apiWf.folderId || undefined,
        folderName: apiWf.folderName || undefined,
        version: saved.version || 1,
        steps: saved.steps || [],
        connections: saved.connections || [],
        createdAt: apiWf.createdAt,
        updatedAt: apiWf.createdAt,
        createdBy: 'Admin'
      }
    } catch { /* fall through to auto-layout */ }
  }

  // Fallback: auto-layout from API steps (legacy workflows without designerData)
  const steps: WorkflowNode[] = []
  const connections: WorkflowConnection[] = []

  steps.push({ id: 'start', type: 'start', label: 'Start', x: 300, y: 60, config: {} })

  const apiSteps = (apiWf.steps || []).slice().sort((a: any, b: any) => a.stepOrder - b.stepOrder)
  let prevId = 'start'
  apiSteps.forEach((step: any, i: number) => {
    const nodeId = `step-${step.id}`
    const config: Record<string, any> = { stepId: step.id, isRequired: step.isRequired }
    if (step.approverUserId) {
      config.approverUserId = step.approverUserId
      config.assignee = step.approverUserName || ''
      config.assignmentMode = 'user'
    } else if (step.approverRoleId) {
      config.approverRoleId = step.approverRoleId
      config.assignee = step.approverRoleName || ''
      config.assignmentMode = 'role'
    } else if (step.approverStructureId) {
      config.approverStructureId = step.approverStructureId
      config.assignToManager = step.assignToManager
      config.assignmentMode = 'structure'
    }

    steps.push({
      id: nodeId,
      type: 'approval',
      label: step.approverUserName || step.approverRoleName || step.approverStructureName || `Step ${i + 1}`,
      x: 300,
      y: 180 + i * 130,
      config
    })
    connections.push({ id: `conn-${prevId}-${nodeId}`, fromNodeId: prevId, toNodeId: nodeId })
    prevId = nodeId
  })

  const endY = apiSteps.length > 0 ? 180 + apiSteps.length * 130 : 240
  steps.push({ id: 'end', type: 'end', label: 'End', x: 300, y: endY, config: {} })
  connections.push({ id: `conn-${prevId}-end`, fromNodeId: prevId, toNodeId: 'end' })

  return {
    id: apiWf.id,
    name: apiWf.name,
    description: apiWf.description || '',
    status: apiWf.isActive ? 'Active' : 'Draft',
    triggerType: apiWf.triggerType || 'Manual',
    inheritToSubfolders: apiWf.inheritToSubfolders ?? true,
    folderId: apiWf.folderId || undefined,
    folderName: apiWf.folderName || undefined,
    version: 1,
    steps,
    connections,
    createdAt: apiWf.createdAt,
    updatedAt: apiWf.createdAt,
    createdBy: 'Admin'
  }
}

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

// ── Assignment mode helpers ──────────────────────────────────────────
type AssignmentMode = 'user' | 'role' | 'structure'

function getAssignmentMode(node: WorkflowNode): AssignmentMode {
  if (node.config.assignmentMode) return node.config.assignmentMode
  if (node.config.approverStructureId) return 'structure'
  if (node.config.approverRoleId) return 'role'
  return 'user'
}

function setAssignmentMode(node: WorkflowNode, mode: AssignmentMode) {
  // Clear all assignment fields first
  delete node.config.assignee
  delete node.config.approverUserId
  delete node.config.approverRoleId
  delete node.config.approverStructureId
  delete node.config.assignToManager
  node.config.assignmentMode = mode
  if (mode === 'structure') {
    node.config.assignToManager = false
  }
  // Reset autocomplete fields
  userSearchQuery.value = ''
  roleSearchQuery.value = ''
  structureSearchQuery.value = ''
  showUserDropdown.value = false
  showRoleDropdown.value = false
  showStructureDropdown.value = false
}

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
    isNewWorkflow.value = false
  } else {
    nodeCounter = 100
    designerWorkflow.value = {
      id: '',
      name: 'Untitled Workflow',
      description: '',
      status: 'Draft',
      triggerType: 'Manual',
      inheritToSubfolders: true,
      folderId: undefined,
      folderName: undefined,
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
    isNewWorkflow.value = true
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

function addNodeAtPosition(type: string, x: number, y: number) {
  nodeCounter++
  const nt = getNodeType(type)
  const newNode: WorkflowNode = {
    id: `node-${nodeCounter}`,
    type: type as WorkflowNode['type'],
    label: nt.label,
    description: '',
    x: Math.round(x),
    y: Math.round(y),
    config: {}
  }
  designerWorkflow.value.steps.push(newNode)
  selectedNodeId.value = newNode.id
}

function addNode(type: string) {
  const nonEndNodes = designerWorkflow.value.steps.filter(n => n.type !== 'end')
  const maxY = nonEndNodes.length > 0 ? Math.max(...nonEndNodes.map(n => n.y)) : 60
  const x = 300, y = maxY + 130
  designerWorkflow.value.steps.forEach(n => {
    if (n.type === 'end' && n.y <= y) n.y = y + 130
  })
  addNodeAtPosition(type, x, y)
}

function onPaletteItemMouseDown(e: MouseEvent, type: string) {
  e.preventDefault()
  const startX = e.clientX, startY = e.clientY
  let hasMoved = false

  const onMouseMove = (ev: MouseEvent) => {
    if (!hasMoved && Math.hypot(ev.clientX - startX, ev.clientY - startY) > 5) {
      hasMoved = true
      isDraggingFromPalette.value = true
      paletteDropType.value = type
    }
    if (hasMoved) {
      const pos = screenToCanvas(ev)
      paletteGhostPos.x = pos.x
      paletteGhostPos.y = pos.y
    }
  }

  const onMouseUp = (ev: MouseEvent) => {
    if (hasMoved && isDraggingFromPalette.value) {
      const rect = canvasContainerRef.value?.getBoundingClientRect()
      if (rect && ev.clientX >= rect.left && ev.clientX <= rect.right
        && ev.clientY >= rect.top && ev.clientY <= rect.bottom) {
        const pos = screenToCanvas(ev)
        addNodeAtPosition(type, pos.x, pos.y)
      }
    } else {
      addNode(type)
    }
    isDraggingFromPalette.value = false
    paletteDropType.value = ''
    window.removeEventListener('mousemove', onMouseMove)
    window.removeEventListener('mouseup', onMouseUp)
  }

  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
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
  selectedNodeId.value = id
  showWorkflowProps.value = false
}

// ── Keyboard shortcuts ──────────────────────────────────────────────
function onKeyDown(e: KeyboardEvent) {
  if (currentView.value !== 'designer') return
  const tag = (e.target as HTMLElement)?.tagName
  if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return
  if (e.key === 'Delete' || e.key === 'Backspace') {
    e.preventDefault()
    deleteSelectedNode()
  }
}

// ── Folder list for folder picker ─────────────────────────────────
const folderOptions = ref<{ value: string; label: string }[]>([])

async function loadFolderOptions() {
  try {
    const cabRes = await cabinetsApi.getAll()
    const cabinets = Array.isArray(cabRes.data) ? cabRes.data : cabRes.data.items ?? []
    const options: { value: string; label: string }[] = []
    for (const cab of cabinets) {
      options.push({ value: cab.id, label: `📁 ${cab.name} (Cabinet)` })
      try {
        const treeRes = await foldersApi.getTree(cab.id)
        const tree = Array.isArray(treeRes.data) ? treeRes.data : treeRes.data.items ?? []
        function flattenTree(nodes: any[], prefix: string) {
          for (const node of nodes) {
            options.push({ value: node.id, label: `${prefix}${node.name}` })
            if (node.children?.length) flattenTree(node.children, prefix + '  ')
          }
        }
        flattenTree(tree, '    ')
      } catch { /* skip */ }
    }
    folderOptions.value = options
  } catch { /* skip */ }
}

onMounted(() => {
  window.addEventListener('keydown', onKeyDown)
  loadWorkflows()
  loadStructureTree()
  loadRoles()
  loadFolderOptions()
  loadWorkflowStatuses()
})
onUnmounted(() => window.removeEventListener('keydown', onKeyDown))

// ── Port-based connection drag ──────────────────────────────────────
function screenToCanvas(e: MouseEvent) {
  const rect = canvasContainerRef.value?.getBoundingClientRect()
  if (!rect) return { x: 0, y: 0 }
  return {
    x: (e.clientX - rect.left - canvasOffset.x) / zoom.value,
    y: (e.clientY - rect.top - canvasOffset.y) / zoom.value
  }
}

function onOutputPortMouseDown(e: MouseEvent, nodeId: string, port = 'bottom') {
  e.stopPropagation()
  e.preventDefault()
  isConnecting.value = true
  connectFromNodeId.value = nodeId
  connectFromPort.value = port
  const pos = screenToCanvas(e)
  connectMousePos.x = pos.x
  connectMousePos.y = pos.y

  const onMouseMove = (ev: MouseEvent) => {
    const p = screenToCanvas(ev)
    connectMousePos.x = p.x
    connectMousePos.y = p.y
  }

  const onMouseUp = () => {
    isConnecting.value = false
    connectFromNodeId.value = null
    window.removeEventListener('mousemove', onMouseMove)
    window.removeEventListener('mouseup', onMouseUp)
  }

  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
}

function onInputPortMouseUp(e: MouseEvent, nodeId: string, port = 'top') {
  if (isConnecting.value && connectFromNodeId.value && connectFromNodeId.value !== nodeId) {
    const exists = designerWorkflow.value.connections.some(
      c => c.fromNodeId === connectFromNodeId.value && c.toNodeId === nodeId
        && c.fromPort === connectFromPort.value && c.toPort === port
    )
    if (!exists) {
      designerWorkflow.value.connections.push({
        id: `conn-${Date.now()}`,
        fromNodeId: connectFromNodeId.value,
        toNodeId: nodeId,
        fromPort: connectFromPort.value as any,
        toPort: port as any
      })
    }
  }
}

const tempConnectionPath = computed(() => {
  if (!isConnecting.value || !connectFromNodeId.value) return ''
  const from = designerWorkflow.value.steps.find(n => n.id === connectFromNodeId.value)
  if (!from) return ''
  const startPos = getPortPosition(from, connectFromPort.value)
  const x1 = startPos.x, y1 = startPos.y
  const x2 = connectMousePos.x, y2 = connectMousePos.y
  if (Math.abs(x1 - x2) < 2 && Math.abs(y1 - y2) < 2) return ''
  if (Math.abs(x1 - x2) < 2) return `M ${x1} ${y1} L ${x2} ${y2}`
  const midY = (y1 + y2) / 2
  const dy1 = Math.abs(midY - y1), dy2 = Math.abs(y2 - midY), dx = Math.abs(x2 - x1) / 2
  if (dy1 < 1 || dy2 < 1 || dx < 1) return `M ${x1} ${y1} L ${x2} ${y2}`
  const r = Math.min(CORNER_RADIUS, dy1, dy2, dx)
  const dirX = x2 > x1 ? 1 : -1
  return [
    `M ${x1} ${y1}`, `L ${x1} ${midY - r}`,
    `Q ${x1} ${midY}, ${x1 + r * dirX} ${midY}`,
    `L ${x2 - r * dirX} ${midY}`,
    `Q ${x2} ${midY}, ${x2} ${midY + r}`,
    `L ${x2} ${y2}`
  ].join(' ')
})

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

// ── SVG path helper (orthogonal connectors) ─────────────────────────
const CORNER_RADIUS = 8

function getConnectionPath(conn: WorkflowConnection) {
  const from = designerWorkflow.value.steps.find(n => n.id === conn.fromNodeId)
  const to = designerWorkflow.value.steps.find(n => n.id === conn.toNodeId)
  if (!from || !to) return ''
  const startPos = getPortPosition(from, conn.fromPort || 'bottom')
  const endPos = getPortPosition(to, conn.toPort || 'top')
  return buildConnectionPath(startPos, endPos)
}

function getConnectionMidpoint(conn: WorkflowConnection) {
  const from = designerWorkflow.value.steps.find(n => n.id === conn.fromNodeId)
  const to = designerWorkflow.value.steps.find(n => n.id === conn.toNodeId)
  if (!from || !to) return { x: 0, y: 0 }
  const s = getPortPosition(from, conn.fromPort || 'bottom')
  const e = getPortPosition(to, conn.toPort || 'top')
  const midY = (s.y + e.y) / 2
  return { x: (s.x + e.x) / 2, y: midY }
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

function getPortOffset(type: string) {
  return Math.max(15, getNodeWidth(type) / 2 * 0.5)
}

// Absolute port positions (node.x/y = center) — used for drawing connections
function getPortPosition(node: WorkflowNode, port: string) {
  const nt = getNodeType(node.type)
  const hw = getNodeWidth(node.type) / 2
  const hh = getNodeHeight(node.type) / 2

  if (nt.shape === 'circle') {
    // Single entry at top, exits at left/bottom/right edges
    const r = hw
    switch (port) {
      case 'top':          return { x: node.x, y: node.y - r }
      case 'bottom-left':  return { x: node.x - r, y: node.y }
      case 'bottom':       return { x: node.x, y: node.y + r }
      case 'bottom-right': return { x: node.x + r, y: node.y }
      default:             return { x: node.x, y: node.y + r }
    }
  }

  if (nt.shape === 'diamond') {
    // Entry near top vertex, exits at left/bottom/right vertices
    switch (port) {
      case 'top-left':     return { x: node.x - 8, y: node.y - hh + 8 }
      case 'top':          return { x: node.x, y: node.y - hh }
      case 'top-right':    return { x: node.x + 8, y: node.y - hh + 8 }
      case 'bottom-left':  return { x: node.x - hw, y: node.y }
      case 'bottom':       return { x: node.x, y: node.y + hh }
      case 'bottom-right': return { x: node.x + hw, y: node.y }
      default:             return { x: node.x, y: node.y + hh }
    }
  }

  // Rectangle — ports along top/bottom edges
  const offset = getPortOffset(node.type)
  switch (port) {
    case 'top-left':     return { x: node.x - offset, y: node.y - hh }
    case 'top':          return { x: node.x, y: node.y - hh }
    case 'top-right':    return { x: node.x + offset, y: node.y - hh }
    case 'bottom-left':  return { x: node.x - offset, y: node.y + hh }
    case 'bottom':       return { x: node.x, y: node.y + hh }
    case 'bottom-right': return { x: node.x + offset, y: node.y + hh }
    default:             return { x: node.x, y: node.y + hh }
  }
}

interface PortDef { id: string; cx: number; cy: number }

// Relative port positions (top-left = 0,0) — used for rendering port dots on nodes
function getNodePortPositions(type: string): { entries: PortDef[]; exits: PortDef[] } {
  const w = getNodeWidth(type)
  const h = getNodeHeight(type)
  const hw = w / 2
  const hh = h / 2
  const nt = getNodeType(type)

  if (nt.shape === 'circle') {
    // Single entry at top, exits at left/bottom/right edges
    return {
      entries: [
        { id: 'top', cx: hw, cy: 0 }
      ],
      exits: [
        { id: 'bottom-left',  cx: 0,  cy: hh },
        { id: 'bottom',       cx: hw, cy: h },
        { id: 'bottom-right', cx: w,  cy: hh }
      ]
    }
  }

  if (nt.shape === 'diamond') {
    // Single entry at top vertex, exits at left/bottom/right vertices
    return {
      entries: [
        { id: 'top', cx: hw, cy: 0 }
      ],
      exits: [
        { id: 'bottom-left',  cx: 0,  cy: hh },
        { id: 'bottom',       cx: hw, cy: h },
        { id: 'bottom-right', cx: w,  cy: hh }
      ]
    }
  }

  // Rectangle
  const offset = getPortOffset(type)
  return {
    entries: [
      { id: 'top-left',  cx: hw - offset, cy: 0 },
      { id: 'top',       cx: hw,          cy: 0 },
      { id: 'top-right', cx: hw + offset, cy: 0 }
    ],
    exits: [
      { id: 'bottom-left',  cx: hw - offset, cy: h },
      { id: 'bottom',       cx: hw,          cy: h },
      { id: 'bottom-right', cx: hw + offset, cy: h }
    ]
  }
}

function buildConnectionPath(
  start: { x: number; y: number },
  end: { x: number; y: number }
): string {
  const x1 = start.x, y1 = start.y
  const x2 = end.x, y2 = end.y
  if (Math.abs(x1 - x2) < 2) return `M ${x1} ${y1} L ${x2} ${y2}`
  const midY = (y1 + y2) / 2
  const dy1 = Math.abs(midY - y1), dy2 = Math.abs(y2 - midY), dx = Math.abs(x2 - x1) / 2
  if (dy1 < 1 || dy2 < 1 || dx < 1) return `M ${x1} ${y1} L ${x2} ${y2}`
  const r = Math.min(CORNER_RADIUS, dy1, dy2, dx)
  const dirX = x2 > x1 ? 1 : -1
  return [
    `M ${x1} ${y1}`,
    `L ${x1} ${midY - r}`,
    `Q ${x1} ${midY}, ${x1 + r * dirX} ${midY}`,
    `L ${x2 - r * dirX} ${midY}`,
    `Q ${x2} ${midY}, ${x2} ${midY + r}`,
    `L ${x2} ${y2}`
  ].join(' ')
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

function extractApiSteps() {
  // Extract approval/review nodes sorted by Y position (top-to-bottom) and map to API format
  const approvalNodes = designerWorkflow.value.steps
    .filter(n => n.type === 'approval' || n.type === 'review')
    .sort((a, b) => a.y - b.y)

  return approvalNodes.map((node, i) => ({
    stepOrder: i + 1,
    approverUserId: node.config.approverUserId || undefined,
    approverRoleId: node.config.approverRoleId || undefined,
    approverStructureId: node.config.approverStructureId || undefined,
    assignToManager: node.config.assignToManager || false,
    isRequired: node.config.isRequired !== false,
    statusId: node.config.statusId || undefined
  }))
}

async function handleSaveWorkflow() {
  isSaving.value = true
  try {
    const steps = extractApiSteps()
    // Serialize full visual canvas state for persistence
    const designerData = JSON.stringify({
      steps: designerWorkflow.value.steps,
      connections: designerWorkflow.value.connections,
      status: designerWorkflow.value.status,
      triggerType: designerWorkflow.value.triggerType,
      version: designerWorkflow.value.version
    })
    const payload = {
      name: designerWorkflow.value.name,
      description: designerWorkflow.value.description || undefined,
      folderId: designerWorkflow.value.folderId || undefined,
      requiredApprovers: steps.length || 1,
      isSequential: true,
      triggerType: designerWorkflow.value.triggerType,
      inheritToSubfolders: designerWorkflow.value.inheritToSubfolders,
      designerData,
      steps
    }

    if (isNewWorkflow.value || !designerWorkflow.value.id) {
      const res = await approvalsApi.createWorkflow(payload)
      designerWorkflow.value.id = res.data
      isNewWorkflow.value = false
      showToast('Workflow created successfully')
    } else {
      await approvalsApi.updateWorkflow(designerWorkflow.value.id, payload)
      showToast('Workflow saved successfully')
    }

    showSaveModal.value = false
    await loadWorkflows()
  } catch (err: any) {
    showToast(err?.response?.data?.message || 'Failed to save workflow', 'error')
  } finally {
    isSaving.value = false
  }
}

async function publishWorkflow() {
  designerWorkflow.value.status = 'Active'
  await handleSaveWorkflow()
  if (!isSaving.value) backToList()
}

async function duplicateWorkflow(wf: WorkflowDefinition) {
  try {
    // Extract steps from the workflow to duplicate
    const steps = wf.steps
      .filter(n => n.type === 'approval' || n.type === 'review')
      .sort((a, b) => a.y - b.y)
      .map((node, i) => ({
        stepOrder: i + 1,
        approverUserId: node.config.approverUserId || undefined,
        approverRoleId: node.config.approverRoleId || undefined,
        approverStructureId: node.config.approverStructureId || undefined,
        assignToManager: node.config.assignToManager || false,
        isRequired: node.config.isRequired !== false
      }))

    const designerData = JSON.stringify({
      steps: wf.steps,
      connections: wf.connections,
      status: 'Draft',
      triggerType: wf.triggerType,
      version: 1
    })
    await approvalsApi.createWorkflow({
      name: `${wf.name} (Copy)`,
      description: wf.description || undefined,
      folderId: wf.folderId || undefined,
      requiredApprovers: steps.length || 1,
      isSequential: true,
      triggerType: wf.triggerType,
      inheritToSubfolders: wf.inheritToSubfolders,
      designerData,
      steps
    })
    showToast('Workflow duplicated successfully')
    await loadWorkflows()
  } catch {
    showToast('Failed to duplicate workflow', 'error')
  }
}

async function deleteWorkflow(id: string) {
  try {
    await approvalsApi.deleteWorkflow(id)
    showToast('Workflow deleted')
    await loadWorkflows()
  } catch {
    showToast('Failed to delete workflow', 'error')
  }
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
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
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
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
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
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
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
        <div class="bg-[#0d1117] p-6 rounded-lg text-white shadow-xl border border-zinc-800/50 min-h-[120px] flex flex-col justify-between relative overflow-hidden">
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

      <!-- Loading State -->
      <div v-if="isLoadingList" class="flex flex-col items-center justify-center py-20 gap-4">
        <span class="w-8 h-8 border-3 border-teal border-t-transparent rounded-full animate-spin"></span>
        <p class="text-sm text-zinc-400">Loading workflows...</p>
      </div>

      <!-- Workflow Cards Grid -->
      <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        <!-- Create New Card -->
        <button
          @click="openDesigner()"
          class="group min-h-[240px] rounded-lg border-2 border-dashed border-zinc-300 dark:border-border-dark hover:border-teal dark:hover:border-teal bg-white/50 dark:bg-background-dark/50 flex flex-col items-center justify-center gap-4 transition-all hover:shadow-lg hover:shadow-teal/5"
        >
          <div class="w-16 h-16 rounded-lg bg-teal/10 group-hover:bg-teal/20 flex items-center justify-center transition-colors">
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
          class="group bg-white dark:bg-background-dark rounded-lg border border-zinc-200 dark:border-border-dark hover:border-teal/40 dark:hover:border-teal/40 shadow-sm hover:shadow-lg hover:shadow-teal/5 transition-all overflow-hidden"
        >
          <!-- Card Header - Mini Flow Preview -->
          <div class="h-28 bg-zinc-50 dark:bg-surface-dark/50 relative overflow-hidden border-b border-zinc-100 dark:border-border-dark">
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
            <div class="flex items-center justify-between pt-3 border-t border-zinc-100 dark:border-border-dark">
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
    <div v-else class="fixed inset-0 z-40 flex flex-col bg-zinc-50">

      <!-- Top Toolbar -->
      <div class="flex-shrink-0 h-12 bg-[#0d1117] flex items-center px-3 gap-2 z-20 shadow-lg">
        <!-- Back -->
        <button @click="backToList" class="p-1.5 rounded text-zinc-400 hover:text-white hover:bg-white/10 transition-colors">
          <span class="material-symbols-outlined text-[20px]">arrow_back</span>
        </button>
        <div class="w-px h-5 bg-zinc-700"></div>

        <!-- Workflow Name -->
        <div class="flex items-center gap-2.5 flex-1 min-w-0">
          <span class="material-symbols-outlined text-teal text-[20px]">account_tree</span>
          <input
            v-model="designerWorkflow.name"
            class="bg-transparent border-none outline-none text-sm font-semibold text-white placeholder-zinc-500 min-w-0 flex-1"
            placeholder="Workflow name..."
          />
          <span :class="['px-2 py-0.5 text-[9px] font-bold uppercase tracking-wider rounded', designerWorkflow.status === 'Active' ? 'bg-emerald-500/20 text-emerald-400' : designerWorkflow.status === 'Draft' ? 'bg-amber-500/20 text-amber-400' : 'bg-zinc-500/20 text-zinc-400']">
            {{ designerWorkflow.status }}
          </span>
          <button
            @click="selectedNodeId = null; showWorkflowProps = !showWorkflowProps"
            class="p-1.5 rounded transition-colors"
            :class="showWorkflowProps && !selectedNodeId ? 'text-teal bg-teal/15' : 'text-zinc-500 hover:text-teal hover:bg-white/5'"
            title="Workflow Properties"
          >
            <span class="material-symbols-outlined text-[18px]">settings</span>
          </button>
        </div>

        <div class="w-px h-5 bg-zinc-700"></div>

        <!-- Zoom Controls -->
        <div class="flex items-center gap-0.5">
          <button @click="zoom = Math.max(0.3, zoom - 0.1)" class="p-1.5 text-zinc-500 hover:text-white transition-colors rounded">
            <span class="material-symbols-outlined text-[18px]">remove</span>
          </button>
          <span class="text-[10px] font-mono text-zinc-500 w-10 text-center">{{ Math.round(zoom * 100) }}%</span>
          <button @click="zoom = Math.min(2, zoom + 0.1)" class="p-1.5 text-zinc-500 hover:text-white transition-colors rounded">
            <span class="material-symbols-outlined text-[18px]">add</span>
          </button>
          <button @click="zoom = 1; canvasOffset.x = 0; canvasOffset.y = 0" class="p-1.5 text-zinc-500 hover:text-white transition-colors rounded" title="Reset view">
            <span class="material-symbols-outlined text-[18px]">fit_screen</span>
          </button>
        </div>

        <div class="w-px h-5 bg-zinc-700"></div>

        <!-- Save / Publish -->
        <button
          @click="showSaveModal = true"
          :disabled="isSaving"
          class="h-7 px-3 rounded text-[11px] font-semibold border border-zinc-600 text-zinc-300 hover:text-white hover:border-zinc-500 transition-colors flex items-center gap-1.5 disabled:opacity-40"
        >
          <span class="material-symbols-outlined text-[16px]">save</span>
          Save
        </button>
        <button
          @click="publishWorkflow"
          :disabled="isSaving"
          class="h-7 px-3 rounded text-[11px] font-semibold bg-teal text-white hover:bg-teal/90 transition-colors flex items-center gap-1.5 disabled:opacity-40"
        >
          <span v-if="isSaving" class="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span v-else class="material-symbols-outlined text-[16px]">rocket_launch</span>
          {{ isSaving ? 'Saving...' : 'Publish' }}
        </button>
      </div>

      <div class="flex-1 flex overflow-hidden">
        <!-- Left Sidebar - Node Palette -->
        <div class="flex-shrink-0 w-52 bg-white border-r border-zinc-200/80 flex flex-col z-10">
          <!-- Palette Header -->
          <div class="px-3 py-2.5 border-b border-zinc-200/80 flex items-center gap-2">
            <span class="material-symbols-outlined text-teal text-[16px]">widgets</span>
            <p class="text-[10px] font-bold text-[#0d1117] uppercase tracking-widest">Components</p>
          </div>

          <!-- Node Items -->
          <div class="flex-1 overflow-y-auto p-2 space-y-0.5">
            <div
              v-for="nt in nodeTypes"
              :key="nt.type"
              @mousedown="onPaletteItemMouseDown($event, nt.type)"
              class="w-full flex items-center gap-2.5 px-2.5 py-2 rounded-md text-left transition-all group cursor-grab active:cursor-grabbing select-none hover:bg-zinc-50"
            >
              <div
                class="w-8 h-8 rounded-md flex items-center justify-center flex-shrink-0 border transition-transform group-hover:scale-105"
                :style="{ backgroundColor: nt.color + '10', borderColor: nt.color + '25' }"
              >
                <span class="material-symbols-outlined" style="font-size: 16px;" :style="{ color: nt.color }">{{ nt.icon }}</span>
              </div>
              <div class="min-w-0 flex-1">
                <span class="block text-[12px] font-semibold text-zinc-800 leading-tight">{{ nt.label }}</span>
                <span class="block text-[9px] text-zinc-400 font-medium leading-tight">
                  {{ nt.shape === 'circle' ? 'Event' : nt.shape === 'diamond' ? 'Gateway' : 'Task' }}
                </span>
              </div>
              <span class="material-symbols-outlined text-zinc-300 group-hover:text-zinc-400 transition-colors" style="font-size: 14px;">drag_indicator</span>
            </div>
          </div>

          <!-- Quick Help -->
          <div class="p-2 border-t border-zinc-200/80">
            <div class="bg-[#0d1117] rounded-md p-2.5">
              <div class="space-y-1.5 text-[9px] text-zinc-400">
                <p class="flex items-center gap-1.5"><span class="material-symbols-outlined text-teal" style="font-size: 11px;">drag_indicator</span> Drag to add</p>
                <p class="flex items-center gap-1.5"><span class="material-symbols-outlined text-teal" style="font-size: 11px;">commit</span> Port to connect</p>
                <p class="flex items-center gap-1.5"><span class="material-symbols-outlined text-teal" style="font-size: 11px;">delete</span> Del key to remove</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Center Canvas -->
        <div
          ref="canvasContainerRef"
          class="flex-1 relative overflow-hidden"
          :class="{ 'cursor-grab': !isPanning && !isDraggingFromPalette, 'cursor-grabbing': isPanning, 'cursor-copy': isDraggingFromPalette }"
          @mousedown="onCanvasMouseDown"
          @wheel.prevent="onCanvasWheel"
        >
          <!-- Grid Background -->
          <svg class="absolute inset-0 w-full h-full pointer-events-none" xmlns="http://www.w3.org/2000/svg">
            <defs>
              <pattern id="grid-small" width="20" height="20" patternUnits="userSpaceOnUse"
                :patternTransform="`translate(${canvasOffset.x % 20},${canvasOffset.y % 20})`">
                <circle cx="10" cy="10" r="0.5" fill="#d1d5db" />
              </pattern>
              <pattern id="grid-large" width="100" height="100" patternUnits="userSpaceOnUse"
                :patternTransform="`translate(${canvasOffset.x % 100},${canvasOffset.y % 100})`">
                <circle cx="50" cy="50" r="1" fill="#9ca3af" />
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
              <g v-for="conn in designerWorkflow.connections" :key="conn.id" class="group/conn">
                <!-- Visible connection line -->
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
                <!-- Invisible wide hit area for clicking -->
                <path
                  :d="getConnectionPath(conn)"
                  fill="none"
                  stroke="transparent"
                  stroke-width="16"
                  stroke-linecap="round"
                  class="pointer-events-auto cursor-pointer"
                  @click.stop="deleteConnection(conn.id)"
                />
                <!-- Hover delete button at midpoint -->
                <g
                  class="pointer-events-auto cursor-pointer opacity-0 group-hover/conn:opacity-100 transition-opacity"
                  @click.stop="deleteConnection(conn.id)"
                >
                  <circle
                    :cx="getConnectionMidpoint(conn).x"
                    :cy="getConnectionMidpoint(conn).y"
                    r="10"
                    fill="#ffffff"
                    stroke="#ef4444"
                    stroke-width="1.5"
                    filter="drop-shadow(0 1px 3px rgba(0,0,0,0.15))"
                  />
                  <line
                    :x1="getConnectionMidpoint(conn).x - 4"
                    :y1="getConnectionMidpoint(conn).y - 4"
                    :x2="getConnectionMidpoint(conn).x + 4"
                    :y2="getConnectionMidpoint(conn).y + 4"
                    stroke="#ef4444" stroke-width="2" stroke-linecap="round"
                  />
                  <line
                    :x1="getConnectionMidpoint(conn).x + 4"
                    :y1="getConnectionMidpoint(conn).y - 4"
                    :x2="getConnectionMidpoint(conn).x - 4"
                    :y2="getConnectionMidpoint(conn).y + 4"
                    stroke="#ef4444" stroke-width="2" stroke-linecap="round"
                  />
                </g>
                <!-- Connection label (if any) -->
                <g v-if="conn.label" class="pointer-events-auto cursor-pointer" @click.stop="deleteConnection(conn.id)">
                  <rect
                    :x="getConnectionMidpoint(conn).x - 20"
                    :y="getConnectionMidpoint(conn).y - 10"
                    width="40" height="20" rx="6"
                    fill="#ffffff"
                    stroke="#d1d5db"
                    stroke-width="1"
                  />
                  <text
                    :x="getConnectionMidpoint(conn).x"
                    :y="getConnectionMidpoint(conn).y + 4"
                    text-anchor="middle"
                    fill="#0d1117"
                    font-size="10"
                    font-weight="600"
                  >{{ conn.label }}</text>
                </g>
              </g>
              <!-- Temporary connection while dragging -->
              <path
                v-if="tempConnectionPath"
                :d="tempConnectionPath"
                fill="none"
                stroke="#00ae8c"
                stroke-width="2.5"
                stroke-linecap="round"
                stroke-dasharray="8 4"
                opacity="0.7"
                marker-end="url(#arrowhead)"
                style="animation: dash-flow 0.6s linear infinite"
              />
            </svg>

            <!-- Nodes -->
            <div
              v-for="node in designerWorkflow.steps"
              :key="node.id"
              class="absolute select-none group/node"
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
                  selectedNodeId === node.id ? 'ring-2 ring-teal ring-offset-2 ring-offset-zinc-50 scale-110' : 'hover:scale-105'
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
                    selectedNodeId === node.id ? 'ring-2 ring-teal ring-offset-2 ring-offset-zinc-50 scale-110' : 'hover:scale-105'
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
                class="h-14 rounded-lg shadow-md shadow-zinc-200/60 flex items-center gap-2.5 px-3 transition-all cursor-move bg-white border"
                :class="[
                  selectedNodeId === node.id
                    ? 'border-teal shadow-teal/20 shadow-xl scale-105'
                    : 'border-zinc-200 hover:border-zinc-400 hover:scale-[1.02]'
                ]"
                :style="{ width: getNodeWidth(node.type) + 'px' }"
              >
                <div
                  class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0"
                  :style="{ backgroundColor: getNodeType(node.type).color + '18' }"
                >
                  <span class="material-symbols-outlined text-base" :style="{ color: getNodeType(node.type).color }">{{ getNodeType(node.type).icon }}</span>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-xs font-semibold text-zinc-800 truncate leading-tight">{{ node.label }}</p>
                  <p class="text-[9px] text-zinc-500 truncate leading-tight mt-0.5" v-if="node.config?.assignee || node.config?.approverStructureId">{{ node.config.approverStructureId ? (node.config.assignToManager ? 'Manager: ' : '') + (flatStructures.find(s => s.id === node.config.approverStructureId)?.name?.trim() || 'Structure') : node.config.assignee }}</p>
                  <div v-if="node.config?.statusId && workflowStatuses.find(s => s.id === node.config.statusId)" class="flex items-center gap-1 mt-0.5">
                    <span class="w-1.5 h-1.5 rounded-full flex-shrink-0" :style="{ backgroundColor: workflowStatuses.find(s => s.id === node.config.statusId)?.color }"></span>
                    <span class="text-[8px] font-medium truncate" :style="{ color: workflowStatuses.find(s => s.id === node.config.statusId)?.color }">{{ workflowStatuses.find(s => s.id === node.config.statusId)?.name }}</span>
                  </div>
                </div>
              </div>

              <!-- Entry ports (3: top-left, top, top-right) -->
              <template v-if="node.type !== 'start'">
                <div
                  v-for="ep in getNodePortPositions(node.type).entries"
                  :key="'in-' + ep.id"
                  class="absolute -translate-x-1/2 -translate-y-1/2 z-30 transition-all duration-200"
                  :style="{ left: ep.cx + 'px', top: ep.cy + 'px' }"
                  :class="isConnecting && connectFromNodeId !== node.id
                    ? 'opacity-100 scale-100'
                    : 'opacity-0 scale-75 group-hover/node:opacity-100 group-hover/node:scale-100'"
                  @mouseup="onInputPortMouseUp($event, node.id, ep.id)"
                >
                  <div
                    class="w-2.5 h-2.5 rounded-full border-2 border-white shadow-sm transition-all cursor-crosshair"
                    :class="isConnecting && connectFromNodeId !== node.id
                      ? 'bg-sky-400 scale-[2.2] ring-4 ring-sky-400/30 animate-pulse'
                      : 'bg-zinc-400 dark:bg-zinc-500 hover:bg-sky-400 hover:scale-[1.8] hover:ring-4 hover:ring-sky-400/20'"
                  ></div>
                </div>
              </template>

              <!-- Exit ports (3: bottom-left, bottom, bottom-right) -->
              <template v-if="node.type !== 'end'">
                <div
                  v-for="xp in getNodePortPositions(node.type).exits"
                  :key="'out-' + xp.id"
                  class="absolute -translate-x-1/2 -translate-y-1/2 z-30 cursor-crosshair transition-all duration-200"
                  :style="{ left: xp.cx + 'px', top: xp.cy + 'px' }"
                  :class="isConnecting
                    ? (connectFromNodeId === node.id ? 'opacity-100 scale-100' : 'opacity-0 scale-75 pointer-events-none')
                    : 'opacity-0 scale-75 group-hover/node:opacity-100 group-hover/node:scale-100'"
                  @mousedown.stop="onOutputPortMouseDown($event, node.id, xp.id)"
                >
                  <div class="w-2.5 h-2.5 rounded-full border-2 border-white bg-teal shadow-sm transition-all hover:scale-[1.8] hover:ring-4 hover:ring-teal/20"></div>
                </div>
              </template>
            </div>

            <!-- Palette drag ghost -->
            <div
              v-if="isDraggingFromPalette && paletteDropType"
              class="absolute pointer-events-none"
              :style="{
                left: (paletteGhostPos.x - getNodeWidth(paletteDropType) / 2) + 'px',
                top: (paletteGhostPos.y - getNodeHeight(paletteDropType) / 2) + 'px',
                zIndex: 50,
                opacity: 0.6,
                filter: 'drop-shadow(0 4px 12px rgba(0, 174, 140, 0.3))'
              }"
            >
              <!-- Circle ghost -->
              <div
                v-if="getNodeType(paletteDropType).shape === 'circle'"
                class="w-14 h-14 rounded-full flex items-center justify-center border-2 border-dashed border-teal"
                :style="{ backgroundColor: getNodeType(paletteDropType).color + '30' }"
              >
                <span class="material-symbols-outlined text-white text-xl" :style="{ color: getNodeType(paletteDropType).color }">{{ getNodeType(paletteDropType).icon }}</span>
              </div>
              <!-- Diamond ghost -->
              <div
                v-else-if="getNodeType(paletteDropType).shape === 'diamond'"
                class="w-14 h-14 flex items-center justify-center"
              >
                <div class="w-11 h-11 rotate-45 rounded-md border-2 border-dashed border-teal" :style="{ backgroundColor: getNodeType(paletteDropType).color + '30' }">
                  <div class="w-full h-full flex items-center justify-center -rotate-45">
                    <span class="material-symbols-outlined text-lg" :style="{ color: getNodeType(paletteDropType).color }">{{ getNodeType(paletteDropType).icon }}</span>
                  </div>
                </div>
              </div>
              <!-- Rect ghost -->
              <div
                v-else
                class="h-14 rounded-lg flex items-center gap-2.5 px-3 border-2 border-dashed border-teal bg-white/80"
                :style="{ width: getNodeWidth(paletteDropType) + 'px' }"
              >
                <div class="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0" :style="{ backgroundColor: getNodeType(paletteDropType).color + '20' }">
                  <span class="material-symbols-outlined text-base" :style="{ color: getNodeType(paletteDropType).color }">{{ getNodeType(paletteDropType).icon }}</span>
                </div>
                <p class="text-xs font-semibold text-zinc-500">{{ getNodeType(paletteDropType).label }}</p>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Panel - Properties -->
        <div class="flex-shrink-0 w-80 bg-white border-l border-zinc-200 flex flex-col z-10">
          <div v-if="selectedNode" class="flex-1 flex flex-col overflow-hidden">
            <!-- Panel Header -->
            <div class="px-4 py-3 border-b border-zinc-200 flex items-center justify-between"
              :style="{ borderBottom: `2px solid ${selectedNodeType?.color}20` }">
              <div class="flex items-center gap-3">
                <div class="w-9 h-9 rounded-xl flex items-center justify-center shadow-sm"
                  :style="{ backgroundColor: selectedNodeType?.color + '15', border: `1px solid ${selectedNodeType?.color}30` }">
                  <span class="material-symbols-outlined text-lg" :style="{ color: selectedNodeType?.color }">{{ selectedNodeType?.icon }}</span>
                </div>
                <div>
                  <p class="text-sm font-bold text-zinc-800">{{ selectedNodeType?.label }}</p>
                  <p class="text-[10px] text-zinc-400 font-mono">{{ selectedNode.id }}</p>
                </div>
              </div>
              <button
                v-if="selectedNode.type !== 'start' && selectedNode.type !== 'end'"
                @click="deleteSelectedNode"
                class="p-1.5 rounded-lg text-zinc-400 hover:text-red-500 hover:bg-red-50 transition-colors"
                title="Delete (Del)"
              >
                <span class="material-symbols-outlined text-lg">delete</span>
              </button>
            </div>

            <!-- Properties -->
            <div class="flex-1 overflow-y-auto">
              <!-- Basic Info Section -->
              <div class="p-4 space-y-3 border-b border-zinc-100">
                <div>
                  <label class="block text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-1.5">Label</label>
                  <input
                    v-model="selectedNode.label"
                    class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                  />
                </div>

                <div>
                  <label class="block text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-1.5">Description</label>
                  <textarea
                    v-model="selectedNode.description"
                    rows="2"
                    class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400 resize-none"
                    placeholder="Describe this step..."
                  ></textarea>
                </div>
              </div>

              <!-- Assignment Section for approval/review -->
              <div v-if="selectedNode.type === 'approval' || selectedNode.type === 'review'" class="p-4 border-b border-zinc-100">
                <label class="block text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-2.5">Assign To</label>

                <!-- 3-mode segmented picker -->
                <div class="flex bg-zinc-100 rounded-lg p-0.5 mb-3">
                  <button
                    v-for="mode in ([
                      { key: 'user', label: 'User', icon: 'person' },
                      { key: 'role', label: 'Role', icon: 'shield_person' },
                      { key: 'structure', label: 'Unit', icon: 'apartment' }
                    ] as const)"
                    :key="mode.key"
                    @click="setAssignmentMode(selectedNode!, mode.key)"
                    class="flex-1 flex items-center justify-center gap-1 py-1.5 rounded-md text-[11px] font-semibold transition-all"
                    :class="getAssignmentMode(selectedNode!) === mode.key
                      ? 'bg-teal text-white shadow-sm'
                      : 'text-zinc-400 hover:text-zinc-600'"
                  >
                    <span class="material-symbols-outlined" style="font-size: 14px;">{{ mode.icon }}</span>
                    {{ mode.label }}
                  </button>
                </div>

                <!-- Selected chip (shown when a selection is made) -->
                <div
                  v-if="selectedNode.config.approverUserId || selectedNode.config.approverRoleId || selectedNode.config.approverStructureId"
                  class="flex items-center gap-2 px-3 py-2 rounded-lg bg-teal/10 border border-teal/25 mb-3"
                >
                  <div class="w-6 h-6 rounded-full bg-teal/20 flex items-center justify-center flex-shrink-0">
                    <span class="material-symbols-outlined text-teal" style="font-size: 14px;">
                      {{ getAssignmentMode(selectedNode!) === 'user' ? 'person' : getAssignmentMode(selectedNode!) === 'role' ? 'shield_person' : 'apartment' }}
                    </span>
                  </div>
                  <span class="text-xs font-semibold text-teal flex-1 truncate">
                    {{ getAssignmentMode(selectedNode!) === 'structure'
                      ? (flatStructures.find(s => s.id === selectedNode!.config.approverStructureId)?.name?.trim() || 'Structure')
                      : selectedNode.config.assignee }}
                  </span>
                  <button @click="clearAssignment" class="text-teal/50 hover:text-red-500 transition-colors flex-shrink-0 p-0.5 rounded hover:bg-red-50" title="Clear">
                    <span class="material-symbols-outlined" style="font-size: 14px;">close</span>
                  </button>
                </div>

                <!-- User mode (autocomplete) -->
                <div v-if="getAssignmentMode(selectedNode!) === 'user'" class="relative">
                  <div class="relative">
                    <span class="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-400" style="font-size: 16px;">search</span>
                    <input
                      v-model="userSearchQuery"
                      @input="onUserSearchInput(userSearchQuery)"
                      @focus="showUserDropdown = userSearchResults.length > 0"
                      @blur="showUserDropdown = false"
                      class="w-full pl-9 pr-8 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                      placeholder="Type to search users..."
                    />
                    <span v-if="isSearchingUsers" class="absolute right-3 top-1/2 -translate-y-1/2">
                      <span class="w-3.5 h-3.5 border-2 border-teal border-t-transparent rounded-full animate-spin inline-block"></span>
                    </span>
                  </div>
                  <div
                    v-if="showUserDropdown && userSearchResults.length > 0"
                    class="absolute z-50 w-full mt-1 max-h-48 overflow-y-auto bg-white rounded-lg border border-zinc-200 shadow-xl ring-1 ring-black/5"
                  >
                    <button
                      v-for="user in userSearchResults"
                      :key="user.id"
                      @mousedown.prevent="selectUser(user)"
                      class="w-full px-3 py-2.5 text-left hover:bg-teal/5 flex items-center gap-2.5 transition-colors border-b border-zinc-50 last:border-0"
                      :class="{ 'bg-teal/5': selectedNode.config.approverUserId === user.id }"
                    >
                      <div class="w-7 h-7 rounded-full bg-zinc-100 flex items-center justify-center flex-shrink-0">
                        <span class="material-symbols-outlined text-zinc-400" style="font-size: 14px;">person</span>
                      </div>
                      <div class="min-w-0 flex-1">
                        <p class="text-zinc-900 truncate text-xs font-medium">{{ user.displayName || user.username }}</p>
                        <p class="text-[10px] text-zinc-400 truncate">{{ user.username }}</p>
                      </div>
                    </button>
                  </div>
                </div>

                <!-- Role mode (autocomplete) -->
                <div v-if="getAssignmentMode(selectedNode!) === 'role'" class="relative">
                  <div class="relative">
                    <span class="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-400" style="font-size: 16px;">search</span>
                    <input
                      v-model="roleSearchQuery"
                      @focus="showRoleDropdown = true"
                      @blur="showRoleDropdown = false"
                      class="w-full pl-9 pr-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                      placeholder="Type to search roles..."
                    />
                  </div>
                  <div
                    v-if="showRoleDropdown && filteredRoles.length > 0"
                    class="absolute z-50 w-full mt-1 max-h-48 overflow-y-auto bg-white rounded-lg border border-zinc-200 shadow-xl ring-1 ring-black/5"
                  >
                    <button
                      v-for="role in filteredRoles"
                      :key="role.id"
                      @mousedown.prevent="selectRole(role)"
                      class="w-full px-3 py-2.5 text-left hover:bg-teal/5 flex items-center gap-2.5 transition-colors border-b border-zinc-50 last:border-0"
                      :class="{ 'bg-teal/5': selectedNode.config.approverRoleId === role.id }"
                    >
                      <div class="w-7 h-7 rounded-full bg-zinc-100 flex items-center justify-center flex-shrink-0">
                        <span class="material-symbols-outlined text-zinc-400" style="font-size: 14px;">shield_person</span>
                      </div>
                      <span class="text-zinc-900 truncate text-xs font-medium">{{ role.name }}</span>
                    </button>
                  </div>
                </div>

                <!-- Structure mode (autocomplete) -->
                <div v-if="getAssignmentMode(selectedNode!) === 'structure'" class="space-y-3">
                  <div class="relative">
                    <div class="relative">
                      <span class="absolute left-3 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-400" style="font-size: 16px;">search</span>
                      <input
                        v-model="structureSearchQuery"
                        @focus="showStructureDropdown = true"
                        @blur="showStructureDropdown = false"
                        class="w-full pl-9 pr-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                        placeholder="Type to search units..."
                      />
                    </div>
                    <div
                      v-if="showStructureDropdown && filteredStructures.length > 0"
                      class="absolute z-50 w-full mt-1 max-h-48 overflow-y-auto bg-white rounded-lg border border-zinc-200 shadow-xl ring-1 ring-black/5"
                    >
                      <button
                        v-for="s in filteredStructures"
                        :key="s.id"
                        @mousedown.prevent="selectStructure(s)"
                        class="w-full px-3 py-2.5 text-left hover:bg-teal/5 flex items-center gap-2.5 transition-colors border-b border-zinc-50 last:border-0"
                        :class="{ 'bg-teal/5': selectedNode.config.approverStructureId === s.id }"
                      >
                        <div class="w-7 h-7 rounded-full bg-zinc-100 flex items-center justify-center flex-shrink-0">
                          <span class="material-symbols-outlined text-zinc-400" style="font-size: 14px;">apartment</span>
                        </div>
                        <span class="text-zinc-900 truncate text-xs font-medium flex-1">{{ s.name }}</span>
                        <span class="text-[9px] text-zinc-400 bg-zinc-100 px-1.5 py-0.5 rounded font-medium flex-shrink-0">{{ s.type }}</span>
                      </button>
                    </div>
                  </div>

                  <!-- Manager Only toggle -->
                  <label class="flex items-center justify-between gap-2 px-3 py-2.5 rounded-lg bg-zinc-50 border border-zinc-200 cursor-pointer hover:border-teal/40 transition-colors">
                    <div class="flex items-center gap-2">
                      <span class="material-symbols-outlined text-zinc-500" style="font-size: 16px;">manage_accounts</span>
                      <div>
                        <span class="text-xs font-medium text-zinc-700 block">Manager Only</span>
                        <span class="text-[10px] text-zinc-400 block">
                          {{ selectedNode.config.assignToManager ? 'Only the unit manager' : 'All active members' }}
                        </span>
                      </div>
                    </div>
                    <button
                      type="button"
                      @click="selectedNode!.config.assignToManager = !selectedNode!.config.assignToManager"
                      class="relative w-9 h-5 rounded-full transition-colors duration-200 flex-shrink-0"
                      :class="selectedNode!.config.assignToManager ? 'bg-teal' : 'bg-zinc-300'"
                    >
                      <span
                        class="absolute top-0.5 left-0.5 w-4 h-4 bg-white rounded-full shadow transition-transform duration-200"
                        :class="selectedNode!.config.assignToManager ? 'translate-x-4' : 'translate-x-0'"
                      ></span>
                    </button>
                  </label>
                </div>
              </div>

              <!-- Workflow Status (approval/review nodes) -->
              <div v-if="selectedNode.type === 'approval' || selectedNode.type === 'review'" class="p-4 border-b border-zinc-100">
                <label class="block text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-2.5">Step Status</label>
                <UiSelect
                  :model-value="selectedNode.config.statusId || null"
                  @update:model-value="onStatusChange"
                  :options="stepStatusOptions"
                  placeholder="No status"
                  clearable
                  searchable
                  size="sm"
                />
                <div v-if="selectedNode.config.statusId" class="flex items-center gap-2 mt-2 px-2.5 py-1.5 rounded-lg bg-zinc-50 border border-zinc-100">
                  <span
                    class="w-2.5 h-2.5 rounded-full flex-shrink-0"
                    :style="{ backgroundColor: workflowStatuses.find(s => s.id === selectedNode.config.statusId)?.color }"
                  ></span>
                  <span
                    v-if="workflowStatuses.find(s => s.id === selectedNode.config.statusId)?.icon"
                    class="material-symbols-outlined"
                    :style="{ color: workflowStatuses.find(s => s.id === selectedNode.config.statusId)?.color, fontSize: '14px' }"
                  >{{ workflowStatuses.find(s => s.id === selectedNode.config.statusId)?.icon }}</span>
                  <span class="text-xs font-medium" :style="{ color: workflowStatuses.find(s => s.id === selectedNode.config.statusId)?.color }">
                    {{ workflowStatuses.find(s => s.id === selectedNode.config.statusId)?.name }}
                  </span>
                </div>
              </div>

              <!-- Configuration Section -->
              <div v-if="selectedNode.type === 'condition' || selectedNode.type === 'approval' || selectedNode.type === 'review' || selectedNode.type === 'timer' || selectedNode.type === 'notification'"
                class="p-4 border-b border-zinc-100 space-y-3">
                <p class="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider">Configuration</p>

                <!-- Condition for branch nodes -->
                <div v-if="selectedNode.type === 'condition'">
                  <label class="block text-[10px] text-zinc-400 mb-1">Condition Expression</label>
                  <textarea
                    v-model="selectedNode.config.condition"
                    rows="3"
                    class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-xs text-zinc-900 font-mono focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all resize-none"
                    placeholder="e.g. document.classification == 'Confidential'"
                  ></textarea>
                </div>

                <!-- Timeout for approval/review -->
                <div v-if="selectedNode.type === 'approval' || selectedNode.type === 'review' || selectedNode.type === 'timer'">
                  <label class="block text-[10px] text-zinc-400 mb-1">
                    {{ selectedNode.type === 'timer' ? 'Duration' : 'Timeout' }}
                  </label>
                  <input
                    v-model="selectedNode.config.timeout"
                    class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                    placeholder="e.g. 48h, 3d, 1w"
                  />
                </div>

                <!-- Notification template -->
                <div v-if="selectedNode.type === 'notification'" class="space-y-3">
                  <div>
                    <label class="block text-[10px] text-zinc-400 mb-1">Template</label>
                    <input
                      v-model="selectedNode.config.template"
                      class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                      placeholder="Notification template name..."
                    />
                  </div>
                  <div>
                    <label class="block text-[10px] text-zinc-400 mb-1">Recipients</label>
                    <input
                      v-model="selectedNode.config.recipients"
                      class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                      placeholder="Users, roles, or groups..."
                    />
                  </div>
                </div>
              </div>

              <!-- Advanced Section (Position + Connections) -->
              <div class="p-4 space-y-3">
                <p class="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider">Layout</p>

                <!-- Position -->
                <div class="flex gap-2">
                  <div class="flex-1">
                    <label class="text-[9px] text-zinc-400 font-medium">X</label>
                    <input
                      v-model.number="selectedNode.x"
                      type="number"
                      class="w-full px-2.5 py-1.5 rounded-lg border border-zinc-200 bg-white text-xs text-zinc-900 font-mono focus:ring-1 focus:ring-teal/30 outline-none"
                    />
                  </div>
                  <div class="flex-1">
                    <label class="text-[9px] text-zinc-400 font-medium">Y</label>
                    <input
                      v-model.number="selectedNode.y"
                      type="number"
                      class="w-full px-2.5 py-1.5 rounded-lg border border-zinc-200 bg-white text-xs text-zinc-900 font-mono focus:ring-1 focus:ring-teal/30 outline-none"
                    />
                  </div>
                </div>

                <!-- Connections -->
                <div>
                  <label class="block text-[10px] text-zinc-400 font-medium mb-1.5">Connections</label>
                  <div class="space-y-1">
                    <div
                      v-for="conn in designerWorkflow.connections.filter(c => c.fromNodeId === selectedNode!.id || c.toNodeId === selectedNode!.id)"
                      :key="conn.id"
                      class="flex items-center justify-between bg-zinc-50 rounded-lg px-2.5 py-1.5 group/conn"
                    >
                      <div class="flex items-center gap-1.5">
                        <span class="material-symbols-outlined text-zinc-400" style="font-size: 12px;">
                          {{ conn.fromNodeId === selectedNode!.id ? 'arrow_forward' : 'arrow_back' }}
                        </span>
                        <span class="text-[11px] text-zinc-500 font-mono">
                          {{ conn.fromNodeId === selectedNode!.id ? conn.toNodeId : conn.fromNodeId }}
                        </span>
                        <span v-if="conn.label" class="text-[9px] bg-zinc-200 text-zinc-600 px-1.5 py-0.5 rounded font-medium">{{ conn.label }}</span>
                      </div>
                      <button @click="deleteConnection(conn.id)" class="text-zinc-300 hover:text-red-500 transition-colors opacity-0 group-hover/conn:opacity-100">
                        <span class="material-symbols-outlined" style="font-size: 14px;">close</span>
                      </button>
                    </div>
                    <p v-if="designerWorkflow.connections.filter(c => c.fromNodeId === selectedNode!.id || c.toNodeId === selectedNode!.id).length === 0" class="text-[10px] text-zinc-400 italic py-1">
                      No connections yet
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Workflow Properties (when settings icon clicked) -->
          <div v-else-if="showWorkflowProps" class="flex-1 flex flex-col overflow-hidden">
            <!-- Panel Header -->
            <div class="px-4 py-3 border-b border-zinc-200 flex items-center gap-3"
              style="border-bottom: 2px solid rgba(0,174,140,0.15)">
              <div class="w-9 h-9 rounded-xl bg-teal/10 border border-teal/20 flex items-center justify-center">
                <span class="material-symbols-outlined text-lg text-teal">account_tree</span>
              </div>
              <div>
                <p class="text-sm font-bold text-zinc-800">Workflow Properties</p>
                <p class="text-[10px] text-zinc-400">General settings</p>
              </div>
            </div>

            <div class="flex-1 overflow-y-auto">
              <!-- Basic Info -->
              <div class="p-4 space-y-3 border-b border-zinc-100">
                <p class="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider">Basic Info</p>
                <div>
                  <label class="block text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-1.5">Name</label>
                  <input
                    v-model="designerWorkflow.name"
                    class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400"
                    placeholder="Workflow name..."
                  />
                </div>
                <div>
                  <label class="block text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-1.5">Description</label>
                  <textarea
                    v-model="designerWorkflow.description"
                    rows="3"
                    class="w-full px-3 py-2 rounded-lg border border-zinc-200 bg-white text-sm text-zinc-900 focus:ring-2 focus:ring-teal/30 focus:border-teal outline-none transition-all placeholder-zinc-400 resize-none"
                    placeholder="Describe the purpose of this workflow..."
                  ></textarea>
                </div>
              </div>

              <!-- Settings -->
              <div class="p-4 space-y-3 border-b border-zinc-100">
                <p class="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider">Settings</p>
                <div>
                  <UiSelect
                    v-model="designerWorkflow.folderId"
                    :options="[{ value: null, label: 'None' }, ...folderOptions]"
                    label="Assigned Folder"
                    placeholder="Select a folder..."
                    searchable
                    clearable
                    size="sm"
                  />
                </div>
                <div>
                  <UiSelect
                    v-model="designerWorkflow.triggerType"
                    :options="triggerOptions"
                    label="Trigger Type"
                    size="sm"
                  />
                </div>
                <div>
                  <label class="flex items-center gap-2 cursor-pointer">
                    <input
                      type="checkbox"
                      v-model="designerWorkflow.inheritToSubfolders"
                      class="w-4 h-4 rounded border-zinc-300 text-teal focus:ring-teal/30"
                    />
                    <span class="text-sm text-zinc-700">Inherit to Subfolders</span>
                  </label>
                  <p class="text-[10px] text-zinc-400 mt-1 ml-6">When enabled, this workflow also triggers for documents uploaded to child folders</p>
                </div>
                <div>
                  <UiSelect
                    v-model="designerWorkflow.status"
                    :options="statusOptions"
                    label="Status"
                    size="sm"
                  />
                </div>
              </div>

              <!-- Summary -->
              <div class="p-4 space-y-3">
                <p class="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider">Summary</p>
                <div class="grid grid-cols-3 gap-3">
                  <div class="bg-[#0d1117] rounded-lg p-3 text-center">
                    <p class="text-lg font-bold text-white">{{ designerWorkflow.steps.length }}</p>
                    <p class="text-[9px] text-teal font-medium">Nodes</p>
                  </div>
                  <div class="bg-[#0d1117] rounded-lg p-3 text-center">
                    <p class="text-lg font-bold text-white">{{ designerWorkflow.connections.length }}</p>
                    <p class="text-[9px] text-teal font-medium">Connections</p>
                  </div>
                  <div class="bg-[#0d1117] rounded-lg p-3 text-center">
                    <p class="text-lg font-bold text-white">v{{ designerWorkflow.version }}</p>
                    <p class="text-[9px] text-teal font-medium">Version</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Empty state -->
          <div v-else class="flex-1 flex flex-col items-center justify-center p-6 text-center">
            <div class="w-16 h-16 rounded-2xl bg-zinc-50 border border-zinc-200 flex items-center justify-center mb-4">
              <span class="material-symbols-outlined text-3xl text-zinc-300">touch_app</span>
            </div>
            <p class="text-sm font-semibold text-zinc-600">Select a Node</p>
            <p class="text-xs text-zinc-400 mt-1 max-w-[180px]">Click any node to edit, or click <span class="material-symbols-outlined align-middle text-sm">settings</span> for workflow properties</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Toast Notification -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-300"
        enter-from-class="translate-y-2 opacity-0"
        enter-to-class="translate-y-0 opacity-100"
        leave-active-class="transition ease-in duration-200"
        leave-from-class="translate-y-0 opacity-100"
        leave-to-class="translate-y-2 opacity-0"
      >
        <div
          v-if="toast.show"
          class="fixed bottom-6 right-6 z-[100] flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg border"
          :class="toast.type === 'success'
            ? 'bg-white border-emerald-200 text-emerald-700'
            : 'bg-white border-red-200 text-red-700'"
        >
          <span class="material-symbols-outlined text-lg">
            {{ toast.type === 'success' ? 'check_circle' : 'error' }}
          </span>
          <span class="text-sm font-medium">{{ toast.message }}</span>
        </div>
      </Transition>
    </Teleport>

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
        <UiSelect
          v-model="designerWorkflow.folderId"
          :options="[{ value: null, label: 'None' }, ...folderOptions]"
          label="Assigned Folder"
          placeholder="Select a folder..."
          searchable
          clearable
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
        <div>
          <label class="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              v-model="designerWorkflow.inheritToSubfolders"
              class="w-4 h-4 rounded border-zinc-300 text-teal focus:ring-teal/30"
            />
            <span class="text-sm text-zinc-700 dark:text-zinc-300">Inherit to Subfolders</span>
          </label>
          <p class="text-[10px] text-zinc-400 mt-1 ml-6">When enabled, this workflow also triggers for documents uploaded to child folders</p>
        </div>
        <div class="bg-zinc-50 dark:bg-surface-dark rounded-lg p-4">
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
          <UiButton variant="outline" @click="showSaveModal = false" :disabled="isSaving">Cancel</UiButton>
          <UiButton @click="handleSaveWorkflow" :disabled="isSaving">
            <span v-if="isSaving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            {{ isSaving ? 'Saving...' : 'Save Workflow' }}
          </UiButton>
        </div>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
@keyframes dash-flow {
  to { stroke-dashoffset: -24; }
}
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
