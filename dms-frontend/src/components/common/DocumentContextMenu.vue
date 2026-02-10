<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import type { Document } from '@/types'

export interface DocumentPermissions {
  canRead: boolean
  canWrite: boolean
  canDelete: boolean
  canAdmin: boolean
  canShare: boolean
  canExport: boolean
  canCheckout: boolean
  canCheckin: boolean
  canManageVersions: boolean
  canManagePermissions: boolean
  canStartWorkflow: boolean
  canRoute: boolean
}

export interface MenuItem {
  id: string
  label: string
  icon?: string
  divider?: boolean
  disabled?: boolean
}

const props = defineProps<{
  document: Document
  permissions: DocumentPermissions
  x: number
  y: number
  isFavorite?: boolean
}>()

const emit = defineEmits<{
  action: [action: string, document: Document]
  close: []
}>()

const authStore = useAuthStore()
const menuRef = ref<HTMLElement | null>(null)
const adjustedPosition = ref({ x: props.x, y: props.y })

// Action code mapping for role-based permissions
const actionCodeMap: Record<string, string> = {
  'view-properties': 'document.view',
  'preview': 'document.preview',
  'checkout': 'document.checkout',
  'checkin': 'document.checkin',
  'discard-checkout': 'document.discard-checkout',
  'edit-comments': 'document.comments.edit',
  'view-comments': 'document.comments.view',
  'link-files': 'document.link.manage',
  'view-links': 'document.link.view',
  'view-attachments': 'document.attachments.view',
  'edit-attachments': 'document.attachments.edit',
  'send-email': 'document.email',
  'share': 'document.share',
  'toggle-favorite': 'document.favorite',
  'manage-password': 'document.password',
  'download': 'document.download',
  'delete': 'document.delete',
  'manage-permissions': 'document.permissions',
  'version-history': 'document.version.view',
  'copy': 'document.copy',
  'cut': 'document.cut',
  'move': 'document.move',
  'route': 'document.route',
  'audit-trail': 'audit.document',
  'start-workflow': 'document.workflow',
  'duplicate': 'document.duplicate',
  'create-shortcut': 'document.shortcut.create',
  'remove-shortcut': 'document.shortcut.remove'
}

// Check if user has role-based permission for an action
const hasRolePermission = (menuId: string): boolean => {
  const actionCode = actionCodeMap[menuId]
  if (!actionCode) return true // No action code means no role restriction
  const hasIt = authStore.hasPermission(actionCode)
  if (!hasIt) {
  }
  return hasIt
}

// Helper to check both node-level AND role-based permissions
const canPerform = (menuId: string, nodePermission: boolean): boolean => {
  if (!nodePermission) return false
  return hasRolePermission(menuId)
}

const menuItems = computed<MenuItem[]>(() => {
  const p = props.permissions
  const items: MenuItem[] = []

  // View actions (require Read + role permission)
  const hasViewActions = canPerform('view-properties', p.canRead) || canPerform('preview', p.canRead)
  if (hasViewActions) {
    if (canPerform('view-properties', p.canRead)) {
      items.push({ id: 'view-properties', label: 'View properties', icon: 'info' })
    }
    if (canPerform('preview', p.canRead)) {
      items.push({ id: 'preview', label: 'Preview', icon: 'preview' })
    }
  }

  // Checkout/Checkin actions
  if (canPerform('checkout', p.canCheckout) && !props.document.isCheckedOut) {
    if (items.length > 0) items.push({ id: 'divider-checkout1', label: '', divider: true })
    items.push({ id: 'checkout', label: 'Check out', icon: 'lock' })
  }
  const hasCheckinActions = canPerform('checkin', p.canCheckin) || canPerform('discard-checkout', p.canCheckin)
  if (hasCheckinActions && props.document.isCheckedOut) {
    if (items.length > 0) items.push({ id: 'divider-checkout2', label: '', divider: true })
    if (canPerform('checkin', p.canCheckin)) {
      items.push({ id: 'checkin', label: 'Check in', icon: 'lock_open' })
    }
    if (canPerform('discard-checkout', p.canCheckin)) {
      items.push({ id: 'discard-checkout', label: 'Discard checkout', icon: 'lock_reset' })
    }
  }

  // Comments section
  const hasCommentActions = canPerform('edit-comments', p.canWrite) || canPerform('view-comments', p.canRead)
  if (hasCommentActions) {
    if (items.length > 0) items.push({ id: 'divider-comments', label: '', divider: true })
    if (canPerform('edit-comments', p.canWrite)) {
      items.push({ id: 'edit-comments', label: 'Edit comments', icon: 'edit' })
    }
    if (canPerform('view-comments', p.canRead)) {
      items.push({ id: 'view-comments', label: 'View comments', icon: 'chat' })
    }
  }

  // Links section
  const hasLinkActions = canPerform('link-files', p.canWrite) || canPerform('view-links', p.canRead)
  if (hasLinkActions) {
    if (items.length > 0) items.push({ id: 'divider-links', label: '', divider: true })
    if (canPerform('link-files', p.canWrite)) {
      items.push({ id: 'link-files', label: 'Link to other files', icon: 'link' })
    }
    if (canPerform('view-links', p.canRead)) {
      items.push({ id: 'view-links', label: 'View link to other files', icon: 'link-view' })
    }
  }

  // Attachments section
  const hasAttachmentActions = canPerform('view-attachments', p.canRead) || canPerform('edit-attachments', p.canWrite)
  if (hasAttachmentActions) {
    if (items.length > 0) items.push({ id: 'divider-attachments', label: '', divider: true })
    if (canPerform('view-attachments', p.canRead)) {
      items.push({ id: 'view-attachments', label: 'View attachments', icon: 'attachment' })
    }
    if (canPerform('edit-attachments', p.canWrite)) {
      items.push({ id: 'edit-attachments', label: 'Edit attachments', icon: 'attachment-edit' })
    }
  }

  // Share section
  const hasShareActions = canPerform('send-email', p.canRead) || canPerform('share', p.canShare) || canPerform('toggle-favorite', p.canRead)
  if (hasShareActions) {
    if (items.length > 0) items.push({ id: 'divider-share', label: '', divider: true })
    if (canPerform('send-email', p.canRead)) {
      items.push({ id: 'send-email', label: 'Send by email', icon: 'mail' })
    }
    if (canPerform('share', p.canShare)) {
      items.push({ id: 'share', label: 'Share', icon: 'share' })
    }
    if (canPerform('toggle-favorite', p.canRead)) {
      items.push({ id: 'toggle-favorite', label: props.isFavorite ? 'Remove from favorites' : 'Add to favorite', icon: 'star' })
    }
  }

  // Admin section
  if (canPerform('manage-password', p.canAdmin)) {
    items.push({ id: 'divider-admin', label: '', divider: true })
    items.push({ id: 'manage-password', label: 'Manage password', icon: 'lock' })
  }

  // Download/Delete section
  const hasFileActions = canPerform('download', p.canExport) || canPerform('delete', p.canDelete) || (props.document.isShortcut && canPerform('remove-shortcut', p.canWrite))
  if (hasFileActions) {
    if (items.length > 0) items.push({ id: 'divider-file', label: '', divider: true })
    if (canPerform('download', p.canExport)) {
      items.push({ id: 'download', label: 'Download', icon: 'download' })
    }
    if (props.document.isShortcut && canPerform('remove-shortcut', p.canWrite)) {
      items.push({ id: 'remove-shortcut', label: 'Remove shortcut', icon: 'remove-shortcut' })
    } else if (canPerform('delete', p.canDelete)) {
      items.push({ id: 'delete', label: 'Delete', icon: 'delete' })
    }
  }

  // Permissions section
  if (canPerform('manage-permissions', p.canManagePermissions)) {
    items.push({ id: 'divider-permissions', label: '', divider: true })
    items.push({ id: 'manage-permissions', label: 'Manage permissions', icon: 'permission' })
  }

  // Version history
  if (canPerform('version-history', p.canRead)) {
    items.push({ id: 'divider-history', label: '', divider: true })
    items.push({ id: 'version-history', label: 'Version history', icon: 'history' })
  }

  // Copy/Move section
  const hasCopyMoveActions = canPerform('copy', p.canRead) || canPerform('cut', p.canWrite) || canPerform('move', p.canWrite)
  if (hasCopyMoveActions) {
    if (items.length > 0) items.push({ id: 'divider-copymove', label: '', divider: true })
    if (canPerform('copy', p.canRead)) {
      items.push({ id: 'copy', label: 'Copy', icon: 'copy' })
    }
    if (canPerform('cut', p.canWrite)) {
      items.push({ id: 'cut', label: 'Cut', icon: 'cut' })
    }
    if (canPerform('move', p.canWrite)) {
      items.push({ id: 'move', label: 'Move to...', icon: 'move' })
    }
    if (!props.document.isShortcut && canPerform('create-shortcut', p.canRead)) {
      items.push({ id: 'create-shortcut', label: 'Create shortcut', icon: 'shortcut' })
    }
  }

  // Route section
  if (canPerform('route', p.canRoute)) {
    items.push({ id: 'divider-route', label: '', divider: true })
    items.push({ id: 'route', label: 'Route', icon: 'route' })
  }

  // Audit section - requires role permission (audit.document)
  if (hasRolePermission('audit-trail')) {
    items.push({ id: 'divider-audit', label: '', divider: true })
    items.push({ id: 'audit-trail', label: 'Audit trail', icon: 'audit' })
  }

  // Workflow section
  if (canPerform('start-workflow', p.canStartWorkflow)) {
    items.push({ id: 'divider-workflow', label: '', divider: true })
    items.push({ id: 'start-workflow', label: 'Start workflow', icon: 'workflow' })
  }

  // Duplicate
  if (canPerform('duplicate', p.canWrite)) {
    items.push({ id: 'divider-duplicate', label: '', divider: true })
    items.push({ id: 'duplicate', label: 'Duplicate', icon: 'duplicate' })
  }

  return items
})

const hasAnyPermission = computed(() => {
  return menuItems.value.some(item => !item.divider)
})

onMounted(() => {
  document.addEventListener('click', handleClickOutside)
  document.addEventListener('contextmenu', handleClickOutside)

  // Adjust position if menu goes off screen
  if (menuRef.value) {
    const rect = menuRef.value.getBoundingClientRect()
    const windowWidth = window.innerWidth
    const windowHeight = window.innerHeight

    let x = props.x
    let y = props.y

    if (x + rect.width > windowWidth) {
      x = windowWidth - rect.width - 10
    }
    if (y + rect.height > windowHeight) {
      y = windowHeight - rect.height - 10
    }

    adjustedPosition.value = { x, y }
  }
})

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside)
  document.removeEventListener('contextmenu', handleClickOutside)
})

function handleClickOutside(e: Event) {
  if (menuRef.value && !menuRef.value.contains(e.target as Node)) {
    emit('close')
  }
}

function handleSelect(item: MenuItem) {
  if (item.disabled || item.divider) return
  emit('action', item.id, props.document)
  emit('close')
}

function getIconPath(icon: string): string {
  const icons: Record<string, string> = {
    'info': 'M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
    'eye': 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z',
    'edit': 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z',
    'chat': 'M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z',
    'link': 'M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1',
    'link-view': 'M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14',
    'attachment': 'M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13',
    'attachment-edit': 'M15.172 7l-6.586 6.586a2 2 0 102.828 2.828l6.414-6.586a4 4 0 00-5.656-5.656l-6.415 6.585a6 6 0 108.486 8.486L20.5 13M16 3l4 4',
    'mail': 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z',
    'share': 'M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z',
    'star': 'M11.049 2.927c.3-.921 1.603-.921 1.902 0l1.519 4.674a1 1 0 00.95.69h4.915c.969 0 1.371 1.24.588 1.81l-3.976 2.888a1 1 0 00-.363 1.118l1.518 4.674c.3.922-.755 1.688-1.538 1.118l-3.976-2.888a1 1 0 00-1.176 0l-3.976 2.888c-.783.57-1.838-.197-1.538-1.118l1.518-4.674a1 1 0 00-.363-1.118l-3.976-2.888c-.784-.57-.38-1.81.588-1.81h4.914a1 1 0 00.951-.69l1.519-4.674z',
    'lock': 'M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z',
    'lock_open': 'M8 11V7a4 4 0 118 0m-4 8v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2z',
    'lock_reset': 'M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15',
    'download': 'M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4',
    'delete': 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16',
    'permission': 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z',
    'history': 'M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z',
    'copy': 'M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z',
    'cut': 'M14.121 14.121L19 19m-7-7l7-7m-7 7l-2.879 2.879M12 12L9.121 9.121m0 5.758a3 3 0 10-4.243 4.243 3 3 0 004.243-4.243zm0-5.758a3 3 0 10-4.243-4.243 3 3 0 004.243 4.243z',
    'move': 'M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4',
    'route': 'M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7',
    'audit': 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4',
    'workflow': 'M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z M21 12a9 9 0 11-18 0 9 9 0 0118 0z',
    'duplicate': 'M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2',
    'preview': 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z',
    'shortcut': 'M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1',
    'remove-shortcut': 'M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1',
  }
  return icons[icon] || ''
}
</script>

<template>
  <div
    ref="menuRef"
    class="fixed bg-white dark:bg-surface-dark rounded-lg shadow-2xl border border-zinc-200 dark:border-border-dark py-2 z-50 min-w-[220px] max-h-[80vh] overflow-y-auto custom-scrollbar font-display"
    :style="{ left: `${adjustedPosition.x}px`, top: `${adjustedPosition.y}px` }"
  >
    <!-- No permissions message -->
    <div v-if="!hasAnyPermission" class="px-4 py-3 flex items-center gap-2 text-zinc-500">
      <span class="material-symbols-outlined text-lg">block</span>
      <span class="text-sm">No permissions</span>
    </div>

    <!-- Menu items -->
    <template v-else v-for="item in menuItems" :key="item.id">
      <div v-if="item.divider" class="border-t border-zinc-200 dark:border-border-dark my-1.5 mx-3"></div>
      <button
        v-else
        @click="handleSelect(item)"
        :disabled="item.disabled"
        class="w-full flex items-center gap-3 px-4 py-2.5 text-sm text-left transition-all mx-1 rounded-lg"
        :class="item.disabled ? 'text-zinc-400 cursor-not-allowed' : 'text-zinc-700 dark:text-zinc-300 hover:bg-teal/10 hover:text-teal'"
        :style="{ width: 'calc(100% - 8px)' }"
      >
        <svg v-if="item.icon" class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="getIconPath(item.icon)" />
        </svg>
        <span>{{ item.label }}</span>
      </button>
    </template>
  </div>
</template>

<style scoped>
.custom-scrollbar {
  scrollbar-width: thin;
  scrollbar-color: #e2e8f0 transparent;
}

.custom-scrollbar::-webkit-scrollbar {
  width: 5px;
}

.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}

.custom-scrollbar::-webkit-scrollbar-thumb {
  background: #e2e8f0;
  border-radius: 10px;
}

.custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: #00ae8c;
}
</style>
