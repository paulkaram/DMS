import axios, { type AxiosInstance, type AxiosError } from 'axios'
import type { ApiError, SaveWorkingCopy } from '@/types'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:144'

const apiClient: AxiosInstance = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiError>) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default apiClient

// API functions
export const cabinetsApi = {
  getAll: (search?: string) =>
    apiClient.get('/cabinets', { params: { search } }),
  getById: (id: string) =>
    apiClient.get(`/cabinets/${id}`),
  create: (data: { name: string; description?: string }) =>
    apiClient.post('/cabinets', data),
  update: (id: string, data: { name: string; description?: string; breakInheritance: boolean }) =>
    apiClient.put(`/cabinets/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/cabinets/${id}`)
}

export const foldersApi = {
  getByParent: (cabinetId: string, parentId?: string) =>
    apiClient.get('/folders', { params: { cabinetId, parentId } }),
  getTree: (cabinetId: string) =>
    apiClient.get(`/folders/tree/${cabinetId}`),
  getById: (id: string) =>
    apiClient.get(`/folders/${id}`),
  create: (data: { cabinetId: string; parentFolderId?: string; name: string; description?: string; accessMode?: number }) =>
    apiClient.post('/folders', data),
  update: (id: string, data: { name: string; description?: string; breakInheritance: boolean; accessMode?: number }) =>
    apiClient.put(`/folders/${id}`, data),
  move: (id: string, data: { newParentFolderId?: string; newCabinetId?: string }) =>
    apiClient.post(`/folders/${id}/move`, data),
  delete: (id: string) =>
    apiClient.delete(`/folders/${id}`)
}

export const documentsApi = {
  getByFolder: (folderId: string) =>
    apiClient.get('/documents', { params: { folderId } }),
  search: (params: { search?: string; folderId?: string; classificationId?: string; documentTypeId?: string }) =>
    apiClient.get('/documents', { params }),
  getById: (id: string, shareToken?: string) =>
    apiClient.get(`/documents/${id}`, { params: shareToken ? { shareToken } : {} }),
  getVersions: (id: string, shareToken?: string) =>
    apiClient.get(`/documents/${id}/versions`, { params: shareToken ? { shareToken } : {} }),
  download: (id: string, version?: number, shareToken?: string) =>
    apiClient.get(`/documents/${id}/download`, {
      params: { version, shareToken },
      responseType: 'blob'
    }),
  upload: (data: FormData) =>
    apiClient.post('/documents', data, {
      headers: { 'Content-Type': 'multipart/form-data' }
    }),
  updateMetadata: (id: string, data: { name: string; description?: string; classificationId?: string; importanceId?: string; documentTypeId?: string }) =>
    apiClient.put(`/documents/${id}`, data),
  updateContent: (id: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.put(`/documents/${id}/content`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },
  checkout: (id: string) =>
    apiClient.post(`/documents/${id}/checkout`),
  // ISO 15489 Checkout System
  checkin: (id: string, data: {
    file?: File
    comment?: string
    checkInType?: number
    keepCheckedOut?: boolean
    changeDescription?: string
  }) => {
    const formData = new FormData()
    if (data.file) formData.append('file', data.file)
    if (data.comment) formData.append('comment', data.comment)
    formData.append('checkInType', String(data.checkInType ?? 0))
    formData.append('keepCheckedOut', String(data.keepCheckedOut ?? false))
    if (data.changeDescription) formData.append('changeDescription', data.changeDescription)
    return apiClient.post(`/documents/${id}/checkin`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },
  discardCheckout: (id: string) =>
    apiClient.post(`/documents/${id}/discard-checkout`),
  forceDiscardCheckout: (id: string, reason: string) =>
    apiClient.post(`/documents/${id}/force-discard-checkout`, { reason }),

  // Working Copy Management
  getWorkingCopy: (id: string) =>
    apiClient.get(`/documents/${id}/working-copy`),
  saveWorkingCopy: (id: string, data: SaveWorkingCopy) =>
    apiClient.put(`/documents/${id}/working-copy`, data),
  saveWorkingCopyContent: (id: string, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.put(`/documents/${id}/working-copy/content`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },

  // Version Comparison and Restore
  compareVersions: (id: string, sourceVersionId: string, targetVersionId: string) =>
    apiClient.get(`/documents/${id}/versions/compare`, {
      params: { source: sourceVersionId, target: targetVersionId }
    }),
  restoreVersion: (id: string, versionId: string, data: {
    comment?: string
    restoreContent?: boolean
    restoreMetadata?: boolean
  }) => apiClient.post(`/documents/${id}/versions/${versionId}/restore`, data),

  // Document Lifecycle State Transition (ISO 15489)
  transitionState: (id: string, data: { targetState: string; reason?: string }) =>
    apiClient.post(`/documents/${id}/transition`, data),

  // Admin Checkout Management
  getStaleCheckouts: (staleHours = 24) =>
    apiClient.get('/documents/stale-checkouts', { params: { staleHours } }),
  move: (id: string, newFolderId: string) =>
    apiClient.post(`/documents/${id}/move`, { newFolderId }),
  copy: (id: string, data: { targetFolderId: string; newName?: string }) =>
    apiClient.post(`/documents/${id}/copy`, data),
  delete: (id: string) =>
    apiClient.delete(`/documents/${id}`),
  getMyCheckouts: () =>
    apiClient.get('/documents/my-checkouts'),
  getMyDocuments: (take = 50) =>
    apiClient.get('/documents/my-documents', { params: { take } }),

  // Preview
  getPreviewInfo: (id: string, version?: number, shareToken?: string) =>
    apiClient.get(`/documents/${id}/preview`, { params: { version, shareToken } }),

  // Bulk operations
  bulkDelete: (documentIds: string[]) =>
    apiClient.post('/documents/bulk/delete', { documentIds }),
  bulkMove: (documentIds: string[], targetFolderId: string) =>
    apiClient.post('/documents/bulk/move', { documentIds, targetFolderId }),
  bulkDownload: (documentIds: string[]) =>
    apiClient.post('/documents/bulk/download', { documentIds }, { responseType: 'blob' }),

  // Upload with progress
  uploadWithProgress: (data: FormData, onProgress?: (progress: number) => void) =>
    apiClient.post('/documents', data, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: (e) => {
        if (e.total && onProgress) {
          onProgress(Math.round((e.loaded * 100) / e.total))
        }
      }
    })
}

export const activityLogsApi = {
  getRecent: (page = 1, pageSize = 100) =>
    apiClient.get('/activitylogs', { params: { page, pageSize } }),
  getByNode: (nodeType: string, nodeId: string, page = 1, pageSize = 50) =>
    apiClient.get(`/activitylogs/by-node/${nodeType}/${nodeId}`, { params: { page, pageSize } }),
  getByUser: (userId: string, page = 1, pageSize = 50) =>
    apiClient.get(`/activitylogs/by-user/${userId}`, { params: { page, pageSize } }),
  getMyActivity: (page = 1, pageSize = 50) =>
    apiClient.get('/activitylogs/my-activity', { params: { page, pageSize } }),
  exportCsv: (page = 1, pageSize = 5000) =>
    apiClient.get('/activitylogs', { params: { page, pageSize } })
}

// Auth API
export const authApi = {
  login: (username: string, password: string) =>
    apiClient.post('/auth/login', { username, password }),
  refreshToken: (refreshToken: string) =>
    apiClient.post('/auth/refresh', { refreshToken }),
  getCurrentUser: () =>
    apiClient.get('/auth/me')
}

// Users API
export const usersApi = {
  getAll: () =>
    apiClient.get('/users'),
  search: (query: string, page = 1, pageSize = 20) =>
    apiClient.get('/users', { params: { search: query, page, pageSize } }),
  getById: (id: string) =>
    apiClient.get(`/users/${id}`),
  update: (id: string, data: { email?: string; firstName?: string; lastName?: string; privacyLevel?: number }) =>
    apiClient.put(`/users/${id}`, data),
  getUserRoles: (userId: string) =>
    apiClient.get(`/users/${userId}/roles`),
  assignRole: (userId: string, roleId: string) =>
    apiClient.post(`/users/${userId}/roles/${roleId}`),
  removeRole: (userId: string, roleId: string) =>
    apiClient.delete(`/users/${userId}/roles/${roleId}`)
}

// Roles API
export const rolesApi = {
  getAll: () =>
    apiClient.get('/roles'),
  getById: (id: string) =>
    apiClient.get(`/roles/${id}`),
  create: (data: { name: string; description?: string }) =>
    apiClient.post('/roles', data),
  update: (id: string, data: { name: string; description?: string; isActive?: boolean }) =>
    apiClient.put(`/roles/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/roles/${id}`),
  getPermissions: (roleId: string) =>
    apiClient.get(`/roles/${roleId}/permissions`),
  getMembers: (roleId: string) =>
    apiClient.get(`/roles/${roleId}/members`),
  addMember: (roleId: string, userId: string) =>
    apiClient.post(`/roles/${roleId}/members`, { userId }),
  removeMember: (roleId: string, userId: string) =>
    apiClient.delete(`/roles/${roleId}/members/${userId}`)
}

// Permissions API - Enterprise Edition
export const permissionsApi = {
  // Basic operations
  getNodePermissions: (nodeType: string, nodeId: string) =>
    apiClient.get(`/permissions/${nodeType}/${nodeId}`),
  grantPermission: (data: {
    nodeType: string
    nodeId: string
    principalType: string
    principalId: string
    permissionLevel: number
    includeChildStructures?: boolean
    expiresAt?: string
    grantedReason?: string
  }) => apiClient.post('/permissions', data),
  updatePermission: (id: string, data: {
    permissionLevel: number
    includeChildStructures: boolean
    expiresAt?: string
    grantedReason?: string
  }) => apiClient.put(`/permissions/${id}`, data),
  revokePermission: (id: string) =>
    apiClient.delete(`/permissions/${id}`),

  // Permission checking
  checkPermission: (nodeType: string, nodeId: string, level = 1) =>
    apiClient.get(`/permissions/check/${nodeType}/${nodeId}`, { params: { level } }),
  getEffectivePermission: (nodeType: string, nodeId: string) =>
    apiClient.get(`/permissions/effective/${nodeType}/${nodeId}`),
  getMyPermissionLevel: (nodeType: string, nodeId: string) =>
    apiClient.get(`/permissions/my-level/${nodeType}/${nodeId}`),

  // Inheritance management
  breakInheritance: (nodeType: string, nodeId: string, copyPermissions: boolean = true) =>
    apiClient.post(`/permissions/${nodeType}/${nodeId}/break-inheritance`, null, { params: { copyPermissions } }),
  restoreInheritance: (nodeType: string, nodeId: string) =>
    apiClient.post(`/permissions/${nodeType}/${nodeId}/restore-inheritance`),

  // Audit trail
  getNodeAudit: (nodeType: string, nodeId: string, take = 100) =>
    apiClient.get(`/permissions/audit/${nodeType}/${nodeId}`, { params: { take } }),
  getPrincipalAudit: (principalType: string, principalId: string, take = 100) =>
    apiClient.get(`/permissions/audit/principal/${principalType}/${principalId}`, { params: { take } }),

  // Cache management
  invalidateCache: (nodeType: string, nodeId: string) =>
    apiClient.post(`/permissions/cache/invalidate/${nodeType}/${nodeId}`),
  invalidateUserCache: (userId: string) =>
    apiClient.post(`/permissions/cache/invalidate/user/${userId}`)
}

// Structures API - Organizational Structure Management
export const structuresApi = {
  // Structure CRUD
  getAll: (includeInactive = false) =>
    apiClient.get('/structures', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/structures/${id}`),
  getByCode: (code: string) =>
    apiClient.get(`/structures/code/${code}`),
  create: (data: {
    code: string
    name: string
    nameAr?: string
    type: string
    parentId?: string
    sortOrder?: number
  }) => apiClient.post('/structures', data),
  update: (id: string, data: {
    code: string
    name: string
    nameAr?: string
    type: string
    parentId?: string
    isActive: boolean
    sortOrder?: number
  }) => apiClient.put(`/structures/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/structures/${id}`),

  // Hierarchy operations
  getRoots: () =>
    apiClient.get('/structures/roots'),
  getChildren: (id: string) =>
    apiClient.get(`/structures/${id}/children`),
  getDescendants: (id: string) =>
    apiClient.get(`/structures/${id}/descendants`),
  getAncestors: (id: string) =>
    apiClient.get(`/structures/${id}/ancestors`),
  getByType: (type: string) =>
    apiClient.get(`/structures/type/${type}`),
  getTree: () =>
    apiClient.get('/structures/tree'),

  // Member management
  getMembers: (id: string) =>
    apiClient.get(`/structures/${id}/members`),
  addMember: (id: string, data: {
    userId: string
    position?: string
    isPrimary: boolean
    startDate: string
    endDate?: string
  }) => apiClient.post(`/structures/${id}/members`, data),
  removeMember: (structureId: string, userId: string) =>
    apiClient.delete(`/structures/${structureId}/members/${userId}`),

  // User structure operations
  getUserStructures: (userId: string) =>
    apiClient.get(`/structures/user/${userId}`),
  getMyStructures: () =>
    apiClient.get('/structures/my'),
  getUserPrimaryStructure: (userId: string) =>
    apiClient.get(`/structures/user/${userId}/primary`),
  setUserPrimaryStructure: (userId: string, structureId: string) =>
    apiClient.put(`/structures/user/${userId}/primary/${structureId}`)
}

// Dashboard API
export const dashboardApi = {
  getStatistics: () =>
    apiClient.get('/dashboard/statistics'),
  getRecentDocuments: (take = 10) =>
    apiClient.get('/dashboard/recent-documents', { params: { take } }),
  getMyCheckouts: () =>
    apiClient.get('/dashboard/my-checkouts'),
  getExpiredDocuments: (take = 5) =>
    apiClient.get('/dashboard/expired-documents', { params: { take } })
}

// Reference Data API
export const referenceDataApi = {
  // Classifications
  getClassifications: (language?: string) =>
    apiClient.get('/classifications', { params: { language } }),
  getClassificationTree: (language?: string) =>
    apiClient.get('/classifications/tree', { params: { language } }),
  getClassification: (id: string) =>
    apiClient.get(`/classifications/${id}`),
  createClassification: (data: { name: string; description?: string; parentId?: string; code?: string; language?: string; confidentialityLevel?: string; defaultRetentionPolicyId?: string; defaultPrivacyLevelId?: string; requiresDisposalApproval?: boolean; sortOrder?: number }) =>
    apiClient.post('/classifications', data),
  updateClassification: (id: string, data: { name: string; description?: string; code?: string; language?: string; confidentialityLevel?: string; defaultRetentionPolicyId?: string; defaultPrivacyLevelId?: string; requiresDisposalApproval?: boolean; sortOrder?: number; isActive?: boolean }) =>
    apiClient.put(`/classifications/${id}`, data),
  deleteClassification: (id: string) =>
    apiClient.delete(`/classifications/${id}`),

  // Importances
  getImportances: (language?: string) =>
    apiClient.get('/importances', { params: { language } }),
  getImportance: (id: string) =>
    apiClient.get(`/importances/${id}`),
  createImportance: (data: { name: string; level: number; color?: string; language?: string }) =>
    apiClient.post('/importances', data),
  updateImportance: (id: string, data: { name: string; level: number; color?: string; language?: string; isActive: boolean }) =>
    apiClient.put(`/importances/${id}`, data),
  deleteImportance: (id: string) =>
    apiClient.delete(`/importances/${id}`),

  // Document Types
  getDocumentTypes: (language?: string) =>
    apiClient.get('/document-types', { params: { language } }),
  getDocumentType: (id: string) =>
    apiClient.get(`/document-types/${id}`),
  createDocumentType: (data: { name: string; description?: string; language?: string }) =>
    apiClient.post('/document-types', data),
  updateDocumentType: (id: string, data: { name: string; description?: string; language?: string; isActive: boolean }) =>
    apiClient.put(`/document-types/${id}`, data),
  deleteDocumentType: (id: string) =>
    apiClient.delete(`/document-types/${id}`),

  // Lookups
  getLookups: () =>
    apiClient.get('/lookups'),
  getLookupById: (id: string) =>
    apiClient.get(`/lookups/${id}`),
  getLookupItems: (name: string, language?: string) =>
    apiClient.get(`/lookups/by-name/${name}`, { params: { language } }),
  getLookupItemsById: (lookupId: string, language?: string) =>
    apiClient.get(`/lookups/${lookupId}/items`, { params: { language } }),
  createLookup: (data: { name: string; description?: string }) =>
    apiClient.post('/lookups', data),
  updateLookup: (id: string, data: { name: string; description?: string }) =>
    apiClient.put(`/lookups/${id}`, data),
  deleteLookup: (id: string) =>
    apiClient.delete(`/lookups/${id}`),
  createLookupItem: (lookupId: string, data: { value: string; displayText?: string; language?: string; sortOrder: number }) =>
    apiClient.post(`/lookups/${lookupId}/items`, data),
  updateLookupItem: (itemId: string, data: { value: string; displayText?: string; language?: string; sortOrder: number }) =>
    apiClient.put(`/lookups/items/${itemId}`, data),
  deleteLookupItem: (itemId: string) =>
    apiClient.delete(`/lookups/items/${itemId}`)
}

// Favorites API
export const favoritesApi = {
  getMyFavorites: () =>
    apiClient.get('/favorites'),
  isFavorite: (nodeType: number, nodeId: string) =>
    apiClient.get('/favorites/check', { params: { nodeType, nodeId } }),
  toggle: (nodeType: number, nodeId: string) =>
    apiClient.post('/favorites/toggle', { nodeType, nodeId }),
  add: (nodeType: number, nodeId: string) =>
    apiClient.post('/favorites', { nodeType, nodeId }),
  remove: (nodeType: number, nodeId: string) =>
    apiClient.delete('/favorites', { params: { nodeType, nodeId } })
}

// Shares API
export const sharesApi = {
  getSharedWithMe: () =>
    apiClient.get('/shares/shared-with-me'),
  getMySharedItems: () =>
    apiClient.get('/shares/my-shared-items'),
  getDocumentShares: (documentId: string) =>
    apiClient.get(`/shares/document/${documentId}`),
  share: (data: { documentId: string; sharedWithUserId: string; permissionLevel: number; expiresAt?: string; message?: string }) =>
    apiClient.post('/shares', data),
  update: (id: string, data: { permissionLevel: number; expiresAt?: string }) =>
    apiClient.put(`/shares/${id}`, data),
  revoke: (id: string) =>
    apiClient.delete(`/shares/${id}`),

  // Link sharing
  createLinkShare: (data: { documentId: string; permissionLevel: number; expiresAt?: string }) =>
    apiClient.post('/shares/link', data),
  getLinkShare: (documentId: string) =>
    apiClient.get(`/shares/link/${documentId}`),
  resolveShareToken: (token: string) =>
    apiClient.get(`/shares/resolve/${token}`)
}

// Recycle Bin API
export const recycleBinApi = {
  getMyRecycleBin: () =>
    apiClient.get('/recyclebin'),
  getAll: (nodeType?: number) =>
    apiClient.get('/recyclebin/all', { params: { nodeType } }),
  restore: (id: string, restoreToFolderId?: string) =>
    apiClient.post(`/recyclebin/${id}/restore`, { restoreToFolderId }),
  permanentlyDelete: (id: string) =>
    apiClient.delete(`/recyclebin/${id}`),
  empty: () =>
    apiClient.delete('/recyclebin/empty')
}

// Approvals API
export const approvalsApi = {
  // Workflows
  getWorkflows: () =>
    apiClient.get('/approvals/workflows'),
  getWorkflow: (id: string) =>
    apiClient.get(`/approvals/workflows/${id}`),
  createWorkflow: (data: { name: string; description?: string; folderId?: string; requiredApprovers: number; isSequential: boolean; designerData?: string; triggerType?: string; inheritToSubfolders?: boolean; steps?: { stepOrder: number; approverUserId?: string; approverRoleId?: string; approverStructureId?: string; assignToManager?: boolean; isRequired: boolean; statusId?: string }[] }) =>
    apiClient.post('/approvals/workflows', data),
  updateWorkflow: (id: string, data: { name: string; description?: string; folderId?: string; requiredApprovers: number; isSequential: boolean; designerData?: string; triggerType?: string; inheritToSubfolders?: boolean; steps?: { stepOrder: number; approverUserId?: string; approverRoleId?: string; approverStructureId?: string; assignToManager?: boolean; isRequired: boolean; statusId?: string }[] }) =>
    apiClient.put(`/approvals/workflows/${id}`, data),
  deleteWorkflow: (id: string) =>
    apiClient.delete(`/approvals/workflows/${id}`),

  // Requests
  getPendingRequests: (take?: number) =>
    apiClient.get('/approvals/requests/pending', { params: take ? { take } : {} }),
  getMyRequests: () =>
    apiClient.get('/approvals/requests/my'),
  submitAction: (requestId: string, data: { action: number; comments?: string }) =>
    apiClient.post(`/approvals/requests/${requestId}/action`, data),
  cancelRequest: (requestId: string) =>
    apiClient.post(`/approvals/requests/${requestId}/cancel`),
  resubmitRequest: (requestId: string) =>
    apiClient.post(`/approvals/requests/${requestId}/resubmit`)
}

// Content Types API
export const contentTypesApi = {
  getAll: () =>
    apiClient.get('/contenttypes'),
  getById: (id: string) =>
    apiClient.get(`/contenttypes/${id}`),
  getByExtension: (extension: string) =>
    apiClient.get(`/contenttypes/by-extension/${extension}`),
  create: (data: { extension: string; mimeType: string; displayName?: string; icon?: string; allowPreview: boolean; allowThumbnail: boolean; maxFileSizeMB: number }) =>
    apiClient.post('/contenttypes', data),
  update: (id: string, data: { extension: string; mimeType: string; displayName?: string; icon?: string; allowPreview: boolean; allowThumbnail: boolean; maxFileSizeMB: number; isActive: boolean }) =>
    apiClient.put(`/contenttypes/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/contenttypes/${id}`)
}

// Document Shortcuts API
export const documentShortcutsApi = {
  getByDocument: (documentId: string) =>
    apiClient.get(`/documentshortcuts/by-document/${documentId}`),
  create: (data: { documentId: string; folderId: string }) =>
    apiClient.post('/documentshortcuts', data),
  delete: (id: string) =>
    apiClient.delete(`/documentshortcuts/${id}`)
}

// Folder Links API
export const folderLinksApi = {
  getFromFolder: (folderId: string) =>
    apiClient.get(`/folderlinks/from/${folderId}`),
  getToFolder: (folderId: string) =>
    apiClient.get(`/folderlinks/to/${folderId}`),
  create: (data: { sourceFolderId: string; targetFolderId: string }) =>
    apiClient.post('/folderlinks', data),
  delete: (id: string) =>
    apiClient.delete(`/folderlinks/${id}`)
}

// Filing Plans API
export const filingPlansApi = {
  getByFolder: (folderId: string) =>
    apiClient.get(`/filingplans/folder/${folderId}`),
  getById: (id: string) =>
    apiClient.get(`/filingplans/${id}`),
  create: (data: { folderId: string; name: string; description?: string; pattern?: string; classificationId?: string; documentTypeId?: string }) =>
    apiClient.post('/filingplans', data),
  update: (id: string, data: { folderId: string; name: string; description?: string; pattern?: string; classificationId?: string; documentTypeId?: string; isActive: boolean }) =>
    apiClient.put(`/filingplans/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/filingplans/${id}`)
}

// Extended Approvals API (requests)
export const approvalRequestsApi = {
  getMyRequests: () =>
    apiClient.get('/approvals/requests/my'),
  getDocumentRequests: (documentId: string) =>
    apiClient.get(`/approvals/requests/document/${documentId}`),
  getRequest: (id: string) =>
    apiClient.get(`/approvals/requests/${id}`),
  createRequest: (data: { documentId: string; workflowId?: string; dueDate?: string; comments?: string }) =>
    apiClient.post('/approvals/requests', data),
  submitAction: (requestId: string, data: { action: number; comments?: string }) =>
    apiClient.post(`/approvals/requests/${requestId}/action`, data),
  cancelRequest: (requestId: string) =>
    apiClient.post(`/approvals/requests/${requestId}/cancel`),
  resubmitRequest: (requestId: string) =>
    apiClient.post(`/approvals/requests/${requestId}/resubmit`)
}

// Content Type Definitions API (Form Builder)
export const contentTypeDefinitionsApi = {
  // Content Type Definitions
  getAll: (includeInactive = false) =>
    apiClient.get('/content-type-definitions', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/content-type-definitions/${id}`),
  create: (data: { name: string; description?: string; icon?: string; color?: string; category?: string; allowOnFolders: boolean; allowOnDocuments: boolean; isRequired: boolean; isSystemDefault?: boolean; defaultClassificationId?: string; sortOrder?: number }) =>
    apiClient.post('/content-type-definitions', data),
  update: (id: string, data: { name: string; description?: string; icon?: string; color?: string; category?: string; allowOnFolders: boolean; allowOnDocuments: boolean; isRequired: boolean; isSystemDefault?: boolean; defaultClassificationId?: string; isActive: boolean; sortOrder?: number }) =>
    apiClient.put(`/content-type-definitions/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/content-type-definitions/${id}`),

  // System Default Content Type
  getSystemDefault: () =>
    apiClient.get('/content-type-definitions/system-default'),
  setSystemDefault: (id: string) =>
    apiClient.put(`/content-type-definitions/${id}/set-system-default`),
  clearSystemDefault: () =>
    apiClient.delete('/content-type-definitions/system-default'),

  // Content Type Fields
  getFields: (contentTypeId: string) =>
    apiClient.get(`/content-type-definitions/${contentTypeId}/fields`),
  getField: (fieldId: string) =>
    apiClient.get(`/content-type-definitions/fields/${fieldId}`),
  createField: (contentTypeId: string, data: { fieldName: string; displayName: string; description?: string; fieldType: string; isRequired: boolean; isReadOnly: boolean; showInList: boolean; isSearchable: boolean; defaultValue?: string; validationRules?: string; lookupName?: string; options?: string; groupName?: string; columnSpan: number }) =>
    apiClient.post(`/content-type-definitions/${contentTypeId}/fields`, data),
  updateField: (fieldId: string, data: { fieldName: string; displayName: string; description?: string; fieldType: string; isRequired: boolean; isReadOnly: boolean; showInList: boolean; isSearchable: boolean; defaultValue?: string; validationRules?: string; lookupName?: string; options?: string; groupName?: string; columnSpan: number; sortOrder: number; isActive: boolean }) =>
    apiClient.put(`/content-type-definitions/fields/${fieldId}`, data),
  deleteField: (fieldId: string) =>
    apiClient.delete(`/content-type-definitions/fields/${fieldId}`),
  reorderFields: (contentTypeId: string, fieldIds: string[]) =>
    apiClient.put(`/content-type-definitions/${contentTypeId}/fields/reorder`, { fieldIds }),

  // Document Metadata
  getDocumentMetadata: (documentId: string) =>
    apiClient.get(`/content-type-definitions/documents/${documentId}/metadata`),
  saveDocumentMetadata: (documentId: string, contentTypeId: string, metadata: { fieldId: string; fieldName: string; value?: string; numericValue?: number; dateValue?: string }[]) =>
    apiClient.post(`/content-type-definitions/documents/${documentId}/metadata/${contentTypeId}`, metadata),
  deleteDocumentMetadata: (documentId: string) =>
    apiClient.delete(`/content-type-definitions/documents/${documentId}/metadata`),

  // Folder Content Type Assignments
  getFolderContentTypes: (folderId: string) =>
    apiClient.get(`/content-type-definitions/folders/${folderId}/assignments`),
  getAvailableContentTypes: (folderId: string) =>
    apiClient.get(`/content-type-definitions/folders/${folderId}/available`),
  assignToFolder: (folderId: string, data: { contentTypeId: string; isRequired: boolean; isDefault?: boolean; inheritToChildren: boolean; displayOrder?: number }) =>
    apiClient.post(`/content-type-definitions/folders/${folderId}/assign`, data),
  updateFolderAssignment: (folderId: string, contentTypeId: string, data: { isRequired: boolean; isDefault: boolean; inheritToChildren: boolean; displayOrder: number }) =>
    apiClient.put(`/content-type-definitions/folders/${folderId}/assignments/${contentTypeId}`, data),
  setFolderDefault: (folderId: string, contentTypeId: string) =>
    apiClient.put(`/content-type-definitions/folders/${folderId}/default/${contentTypeId}`),
  removeFromFolder: (folderId: string, contentTypeId: string) =>
    apiClient.delete(`/content-type-definitions/folders/${folderId}/unassign/${contentTypeId}`),

  // Effective Content Types (with inheritance)
  getEffectiveContentTypes: (folderId: string) =>
    apiClient.get(`/content-type-definitions/folders/${folderId}/effective`),
  getFolderContentTypeInfo: (folderId: string) =>
    apiClient.get(`/content-type-definitions/folders/${folderId}/info`),

  // Cabinet Content Type Assignments
  getCabinetContentTypes: (cabinetId: string) =>
    apiClient.get(`/content-type-definitions/cabinets/${cabinetId}/assignments`),
  assignToCabinet: (cabinetId: string, data: { contentTypeId: string; isRequired: boolean; isDefault?: boolean; inheritToChildren: boolean; displayOrder?: number }) =>
    apiClient.post(`/content-type-definitions/cabinets/${cabinetId}/assign`, data),
  updateCabinetAssignment: (cabinetId: string, contentTypeId: string, data: { isRequired: boolean; isDefault: boolean; inheritToChildren: boolean; displayOrder: number }) =>
    apiClient.put(`/content-type-definitions/cabinets/${cabinetId}/assignments/${contentTypeId}`, data),
  setCabinetDefault: (cabinetId: string, contentTypeId: string) =>
    apiClient.put(`/content-type-definitions/cabinets/${cabinetId}/default/${contentTypeId}`),
  removeFromCabinet: (cabinetId: string, contentTypeId: string) =>
    apiClient.delete(`/content-type-definitions/cabinets/${cabinetId}/unassign/${contentTypeId}`)
}

// Patterns API
export const patternsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/patterns', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/patterns/${id}`),
  getByType: (patternType: string) =>
    apiClient.get(`/patterns/type/${patternType}`),
  getByFolder: (folderId: string) =>
    apiClient.get(`/patterns/folder/${folderId}`),
  findMatch: (value: string, patternType?: string) =>
    apiClient.post('/patterns/match', { value, patternType }),
  testPattern: (regex: string, testValue: string) =>
    apiClient.post('/patterns/test', { regex, testValue }),
  create: (data: { name: string; regex: string; description?: string; patternType: string; targetFolderId?: string; contentTypeId?: string; classificationId?: string; documentTypeId?: string; priority: number }) =>
    apiClient.post('/patterns', data),
  update: (id: string, data: { name: string; regex: string; description?: string; patternType: string; targetFolderId?: string; contentTypeId?: string; classificationId?: string; documentTypeId?: string; priority: number; isActive: boolean }) =>
    apiClient.put(`/patterns/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/patterns/${id}`)
}

// Retention Policies API
export const retentionPoliciesApi = {
  // Policies
  getAll: (includeInactive = false) =>
    apiClient.get('/retention-policies', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/retention-policies/${id}`),
  getByFolder: (folderId: string) =>
    apiClient.get(`/retention-policies/folder/${folderId}`),
  create: (data: { name: string; description?: string; retentionDays: number; expirationAction: string; notifyBeforeExpiration: boolean; notificationDays: number; folderId?: string; classificationId?: string; documentTypeId?: string; requiresApproval: boolean; inheritToSubfolders: boolean; isLegalHold: boolean }) =>
    apiClient.post('/retention-policies', data),
  update: (id: string, data: { name: string; description?: string; retentionDays: number; expirationAction: string; notifyBeforeExpiration: boolean; notificationDays: number; folderId?: string; classificationId?: string; documentTypeId?: string; requiresApproval: boolean; inheritToSubfolders: boolean; isLegalHold: boolean; isActive: boolean }) =>
    apiClient.put(`/retention-policies/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/retention-policies/${id}`),

  // Document Retention
  getDocumentRetentions: (documentId: string) =>
    apiClient.get(`/retention-policies/documents/${documentId}/retention`),
  getExpiringDocuments: (daysAhead = 30) =>
    apiClient.get('/retention-policies/expiring', { params: { daysAhead } }),
  getPendingReview: () =>
    apiClient.get('/retention-policies/pending-review'),
  applyPolicyToDocument: (documentId: string, policyId: string) =>
    apiClient.post(`/retention-policies/documents/${documentId}/apply/${policyId}`),
  approveRetentionAction: (retentionId: string, notes?: string) =>
    apiClient.post(`/retention-policies/retentions/${retentionId}/approve`, { notes }),
  placeOnHold: (documentId: string, notes?: string) =>
    apiClient.post(`/retention-policies/documents/${documentId}/hold`, { notes }),
  releaseHold: (documentId: string) =>
    apiClient.post(`/retention-policies/documents/${documentId}/release-hold`)
}

// Bookmarks API
export const bookmarksApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/bookmarks', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/bookmarks/${id}`),
  getByPlaceholder: (placeholder: string) =>
    apiClient.get(`/bookmarks/placeholder/${placeholder}`),
  create: (data: { name: string; placeholder: string; description?: string; defaultValue?: string; dataType: string; lookupName?: string; isSystem?: boolean; sortOrder?: number }) =>
    apiClient.post('/bookmarks', data),
  update: (id: string, data: { name: string; placeholder: string; description?: string; defaultValue?: string; dataType: string; lookupName?: string; isSystem?: boolean; sortOrder?: number; isActive: boolean }) =>
    apiClient.put(`/bookmarks/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/bookmarks/${id}`)
}

// Service Endpoints API
export const endpointsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/endpoints', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/endpoints/${id}`),
  getByType: (endpointType: string) =>
    apiClient.get(`/endpoints/type/${endpointType}`),
  create: (data: { name: string; url: string; description?: string; endpointType: string; authType?: string; authConfig?: string; timeoutSeconds?: number; retryCount?: number; headers?: string }) =>
    apiClient.post('/endpoints', data),
  update: (id: string, data: { name: string; url: string; description?: string; endpointType: string; authType?: string; authConfig?: string; timeoutSeconds?: number; retryCount?: number; headers?: string; isActive: boolean }) =>
    apiClient.put(`/endpoints/${id}`, data),
  updateHealthStatus: (id: string, status: string) =>
    apiClient.put(`/endpoints/${id}/health`, { status }),
  delete: (id: string) =>
    apiClient.delete(`/endpoints/${id}`)
}

// Export Configs API
export const exportConfigsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/export-configs', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/export-configs/${id}`),
  getDefault: () =>
    apiClient.get('/export-configs/default'),
  create: (data: { name: string; description?: string; exportFormat: string; includeMetadata?: boolean; includeVersions?: boolean; includeAuditTrail?: boolean; flattenFolders?: boolean; namingPattern?: string; watermarkText?: string; maxFileSizeMB?: number; isDefault?: boolean }) =>
    apiClient.post('/export-configs', data),
  update: (id: string, data: { name: string; description?: string; exportFormat: string; includeMetadata?: boolean; includeVersions?: boolean; includeAuditTrail?: boolean; flattenFolders?: boolean; namingPattern?: string; watermarkText?: string; maxFileSizeMB?: number; isDefault?: boolean; isActive: boolean }) =>
    apiClient.put(`/export-configs/${id}`, data),
  setDefault: (id: string) =>
    apiClient.put(`/export-configs/${id}/set-default`),
  delete: (id: string) =>
    apiClient.delete(`/export-configs/${id}`)
}

// Naming Conventions API
export const namingConventionsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/naming-conventions', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/naming-conventions/${id}`),
  getByFolder: (folderId: string) =>
    apiClient.get(`/naming-conventions/folder/${folderId}`),
  getApplicable: (params: { folderId?: string; documentTypeId?: string; appliesTo: string }) =>
    apiClient.get('/naming-conventions/applicable', { params }),
  generateName: (id: string, values: Record<string, string>) =>
    apiClient.post(`/naming-conventions/${id}/generate`, values),
  create: (data: { name: string; description?: string; pattern: string; appliesTo: string; folderId?: string; documentTypeId?: string; isRequired?: boolean; autoGenerate?: boolean; separator?: string; sortOrder?: number }) =>
    apiClient.post('/naming-conventions', data),
  update: (id: string, data: { name: string; description?: string; pattern: string; appliesTo: string; folderId?: string; documentTypeId?: string; isRequired?: boolean; autoGenerate?: boolean; separator?: string; sortOrder?: number; isActive: boolean }) =>
    apiClient.put(`/naming-conventions/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/naming-conventions/${id}`)
}

// Organization Templates API
export const organizationTemplatesApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/organization-templates', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/organization-templates/${id}`),
  getDefault: () =>
    apiClient.get('/organization-templates/default'),
  create: (data: { name: string; description?: string; structure: string; defaultPermissions?: string; includeContentTypes?: boolean; isDefault?: boolean }) =>
    apiClient.post('/organization-templates', data),
  update: (id: string, data: { name: string; description?: string; structure: string; defaultPermissions?: string; includeContentTypes?: boolean; isDefault?: boolean; isActive: boolean }) =>
    apiClient.put(`/organization-templates/${id}`, data),
  setDefault: (id: string) =>
    apiClient.put(`/organization-templates/${id}/set-default`),
  delete: (id: string) =>
    apiClient.delete(`/organization-templates/${id}`)
}

// Permission Levels API
export const permissionLevelsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/permission-levels', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/permission-levels/${id}`),
  getByLevel: (level: number) =>
    apiClient.get(`/permission-levels/level/${level}`),
  create: (data: { name: string; description?: string; level: number; color?: string; icon?: string; canRead?: boolean; canWrite?: boolean; canDelete?: boolean; canAdmin?: boolean; canShare?: boolean; canExport?: boolean; isSystem?: boolean; sortOrder?: number }) =>
    apiClient.post('/permission-levels', data),
  update: (id: string, data: { name: string; description?: string; level: number; color?: string; icon?: string; canRead?: boolean; canWrite?: boolean; canDelete?: boolean; canAdmin?: boolean; canShare?: boolean; canExport?: boolean; isSystem?: boolean; sortOrder?: number; isActive: boolean }) =>
    apiClient.put(`/permission-levels/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/permission-levels/${id}`)
}

// Purposes API
export const purposesApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/purposes', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/purposes/${id}`),
  getByType: (purposeType: string) =>
    apiClient.get(`/purposes/type/${purposeType}`),
  getDefault: (purposeType: string) =>
    apiClient.get(`/purposes/default/${purposeType}`),
  create: (data: { name: string; description?: string; purposeType: string; requiresJustification?: boolean; requiresApproval?: boolean; isDefault?: boolean; sortOrder?: number }) =>
    apiClient.post('/purposes', data),
  update: (id: string, data: { name: string; description?: string; purposeType: string; requiresJustification?: boolean; requiresApproval?: boolean; isDefault?: boolean; sortOrder?: number; isActive: boolean }) =>
    apiClient.put(`/purposes/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/purposes/${id}`)
}

// Scan Configs API
export const scanConfigsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/scan-configs', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/scan-configs/${id}`),
  getDefault: () =>
    apiClient.get('/scan-configs/default'),
  create: (data: { name: string; description?: string; resolution?: number; colorMode?: string; outputFormat?: string; enableOCR?: boolean; ocrLanguage?: string; autoDeskew?: boolean; autoCrop?: boolean; removeBlankPages?: boolean; compressionQuality?: number; targetFolderId?: string; isDefault?: boolean }) =>
    apiClient.post('/scan-configs', data),
  update: (id: string, data: { name: string; description?: string; resolution?: number; colorMode?: string; outputFormat?: string; enableOCR?: boolean; ocrLanguage?: string; autoDeskew?: boolean; autoCrop?: boolean; removeBlankPages?: boolean; compressionQuality?: number; targetFolderId?: string; isDefault?: boolean; isActive: boolean }) =>
    apiClient.put(`/scan-configs/${id}`, data),
  setDefault: (id: string) =>
    apiClient.put(`/scan-configs/${id}/set-default`),
  delete: (id: string) =>
    apiClient.delete(`/scan-configs/${id}`)
}

// Scan Process API
export const scanApi = {
  process: (
    images: File[],
    settings: {
      scanConfigId?: string
      targetFolderId: string
      documentName: string
      description?: string
      pages: { fileIndex: number; rotationDegrees: number }[]
      enableOCR: boolean
      ocrLanguage: string
      autoDeskew: boolean
      autoCrop: boolean
      compressionQuality: number
    },
    onProgress?: (progress: number) => void
  ) => {
    const formData = new FormData()
    images.forEach((image) => formData.append('images', image))
    if (settings.scanConfigId) formData.append('scanConfigId', settings.scanConfigId)
    formData.append('targetFolderId', settings.targetFolderId)
    formData.append('documentName', settings.documentName)
    if (settings.description) formData.append('description', settings.description)
    formData.append('enableOCR', String(settings.enableOCR))
    formData.append('ocrLanguage', settings.ocrLanguage)
    formData.append('autoDeskew', String(settings.autoDeskew))
    formData.append('autoCrop', String(settings.autoCrop))
    formData.append('compressionQuality', String(settings.compressionQuality))
    settings.pages.forEach((page, i) => {
      formData.append(`pages[${i}].fileIndex`, String(page.fileIndex))
      formData.append(`pages[${i}].rotationDegrees`, String(page.rotationDegrees))
    })
    return apiClient.post('/scan/process', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 300000,
      onUploadProgress: (e) => {
        if (e.total && onProgress) {
          onProgress(Math.round((e.loaded * 100) / e.total))
        }
      }
    })
  }
}

// Scanner Agent API (local TWAIN/WIA agent on localhost)
const SCAN_AGENT_URL = 'http://localhost:18181'

export const scanAgentApi = {
  checkStatus: () =>
    axios.get(`${SCAN_AGENT_URL}/api/status`, { timeout: 2000 }),
  getScanners: (driver?: string) =>
    axios.get(`${SCAN_AGENT_URL}/api/scanners`, { params: { driver }, timeout: 10000 }),
  scan: (params: {
    scannerId: string
    driver?: string
    dpi?: number
    colorMode?: string
    pageSize?: string
    paperSource?: string
    duplex?: boolean
  }) =>
    axios.post(`${SCAN_AGENT_URL}/api/scan`, params, { timeout: 120000 })
}

// =============================================
// Document Annotations API
// =============================================
export const documentAnnotationsApi = {
  getByDocument: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/annotations`),
  save: (documentId: string, data: { pages: { pageNumber: number; annotationData: string }[] }) =>
    apiClient.post(`/documents/${documentId}/annotations`, data),
  delete: (documentId: string, annotationId: string) =>
    apiClient.delete(`/documents/${documentId}/annotations/${annotationId}`),
  deleteAll: (documentId: string) =>
    apiClient.delete(`/documents/${documentId}/annotations`),
  getCount: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/annotations/count`)
}

// PDF Page Management API
export const pdfPagesApi = {
  getPageCount: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/pages/count`),

  reorganize: (
    documentId: string,
    manifest: {
      pages: { source: string; originalPage?: number; fileIndex?: number; uploadPageNumber?: number }[]
      comment?: string
    },
    files?: File[],
    onProgress?: (progress: number) => void
  ) => {
    const formData = new FormData()
    formData.append('manifest', JSON.stringify(manifest))
    if (files) {
      files.forEach(file => formData.append('files', file))
    }
    return apiClient.post(`/documents/${documentId}/pages/reorganize`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 300000,
      onUploadProgress: (e) => {
        if (e.total && onProgress) {
          onProgress(Math.round((e.loaded * 100) / e.total))
        }
      }
    })
  }
}

// Saved Signatures API
export const savedSignaturesApi = {
  getAll: () =>
    apiClient.get('/signatures'),
  create: (data: { name: string; signatureData: string; signatureType: string; isDefault: boolean }) =>
    apiClient.post('/signatures', data),
  delete: (id: string) =>
    apiClient.delete(`/signatures/${id}`),
  setDefault: (id: string) =>
    apiClient.put(`/signatures/${id}/set-default`)
}

// Search Configs API
export const searchConfigsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/search-configs', { params: { includeInactive } }),
  getGlobal: () =>
    apiClient.get('/search-configs/global'),
  getByUser: (userId: string) =>
    apiClient.get(`/search-configs/user/${userId}`),
  getById: (id: string) =>
    apiClient.get(`/search-configs/${id}`),
  getDefault: () =>
    apiClient.get('/search-configs/default'),
  create: (data: { name: string; description?: string; searchType?: string; defaultFields?: string; filters?: string; includeContent?: boolean; includeMetadata?: boolean; includeVersions?: boolean; fuzzyMatch?: boolean; maxResults?: number; sortField?: string; sortDirection?: string; isGlobal?: boolean; isDefault?: boolean }) =>
    apiClient.post('/search-configs', data),
  update: (id: string, data: { name: string; description?: string; searchType?: string; defaultFields?: string; filters?: string; includeContent?: boolean; includeMetadata?: boolean; includeVersions?: boolean; fuzzyMatch?: boolean; maxResults?: number; sortField?: string; sortDirection?: string; isGlobal?: boolean; isDefault?: boolean; isActive: boolean }) =>
    apiClient.put(`/search-configs/${id}`, data),
  setDefault: (id: string) =>
    apiClient.put(`/search-configs/${id}/set-default`),
  delete: (id: string) =>
    apiClient.delete(`/search-configs/${id}`)
}

// =============================================
// Document Features API
// =============================================

// Document Comments API
export const documentCommentsApi = {
  getByDocument: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/comments`),
  getById: (documentId: string, commentId: string) =>
    apiClient.get(`/documents/${documentId}/comments/${commentId}`),
  getReplies: (documentId: string, commentId: string) =>
    apiClient.get(`/documents/${documentId}/comments/${commentId}/replies`),
  getCount: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/comments/count`),
  create: (documentId: string, data: { content: string; parentCommentId?: string }) =>
    apiClient.post(`/documents/${documentId}/comments`, { ...data, documentId }),
  update: (documentId: string, commentId: string, data: { content: string }) =>
    apiClient.put(`/documents/${documentId}/comments/${commentId}`, data),
  delete: (documentId: string, commentId: string) =>
    apiClient.delete(`/documents/${documentId}/comments/${commentId}`)
}

// Document Attachments API
function passwordHeaders(password?: string) {
  return password ? { 'X-Document-Password': password } : {}
}

export const documentAttachmentsApi = {
  getByDocument: (documentId: string, password?: string) =>
    apiClient.get(`/documents/${documentId}/attachments`, { headers: passwordHeaders(password) }),
  getById: (documentId: string, attachmentId: string, password?: string) =>
    apiClient.get(`/documents/${documentId}/attachments/${attachmentId}`, { headers: passwordHeaders(password) }),
  getCount: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/attachments/count`),
  download: (documentId: string, attachmentId: string, password?: string) =>
    apiClient.get(`/documents/${documentId}/attachments/${attachmentId}/download`, {
      responseType: 'blob',
      headers: passwordHeaders(password)
    }),
  upload: (documentId: string, file: File, description?: string, password?: string) => {
    const formData = new FormData()
    formData.append('file', file)
    if (description) formData.append('description', description)
    return apiClient.post(`/documents/${documentId}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data', ...passwordHeaders(password) }
    })
  },
  delete: (documentId: string, attachmentId: string, password?: string) =>
    apiClient.delete(`/documents/${documentId}/attachments/${attachmentId}`, { headers: passwordHeaders(password) })
}

// Document Links API
export const documentLinksApi = {
  getByDocument: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/links`),
  getIncoming: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/links/incoming`),
  getById: (documentId: string, linkId: string) =>
    apiClient.get(`/documents/${documentId}/links/${linkId}`),
  getCount: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/links/count`),
  create: (documentId: string, data: { targetDocumentId: string; linkType?: string; description?: string }) =>
    apiClient.post(`/documents/${documentId}/links`, { ...data, sourceDocumentId: documentId }),
  update: (documentId: string, linkId: string, data: { linkType: string; description?: string }) =>
    apiClient.put(`/documents/${documentId}/links/${linkId}`, data),
  delete: (documentId: string, linkId: string) =>
    apiClient.delete(`/documents/${documentId}/links/${linkId}`)
}

// Reports API
export const reportsApi = {
  getStatistics: () =>
    apiClient.get('/reports/statistics'),
  getMonthlyGrowth: (year?: number) =>
    apiClient.get('/reports/monthly-growth', { params: { year } }),
  getDocumentTypes: () =>
    apiClient.get('/reports/document-types'),
  getRecentActivity: (take = 10) =>
    apiClient.get('/reports/recent-activity', { params: { take } })
}

// Document Passwords API
export const documentPasswordsApi = {
  getStatus: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/password/status`),
  hasPassword: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/password/check`),
  getHint: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/password/hint`),
  setPassword: (documentId: string, data: { password: string; hint?: string; expiresAt?: string }) =>
    apiClient.post(`/documents/${documentId}/password`, { ...data, documentId }),
  validatePassword: (documentId: string, password: string) =>
    apiClient.post(`/documents/${documentId}/password/validate`, { documentId, password }),
  changePassword: (documentId: string, data: { currentPassword: string; newPassword: string; hint?: string }) =>
    apiClient.put(`/documents/${documentId}/password`, { ...data, documentId }),
  removePassword: (documentId: string) =>
    apiClient.delete(`/documents/${documentId}/password`)
}

// =============================================
// Folder Structure Templates API
// =============================================
export const folderTemplatesApi = {
  // Template CRUD
  getAll: (includeInactive = false) =>
    apiClient.get('/folder-templates', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/folder-templates/${id}`),
  getDefault: () =>
    apiClient.get('/folder-templates/default'),
  getByCategory: (category: string) =>
    apiClient.get(`/folder-templates/category/${category}`),
  getCategories: () =>
    apiClient.get('/folder-templates/categories'),
  create: (data: { name: string; description?: string; category?: string; icon?: string; isDefault?: boolean; nodes?: any[] }) =>
    apiClient.post('/folder-templates', data),
  update: (id: string, data: { name: string; description?: string; category?: string; icon?: string; isDefault?: boolean; isActive?: boolean }) =>
    apiClient.put(`/folder-templates/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/folder-templates/${id}`),
  duplicate: (id: string, newName: string) =>
    apiClient.post(`/folder-templates/${id}/duplicate`, { newName }),
  getUsageHistory: (id: string) =>
    apiClient.get(`/folder-templates/${id}/usage`),

  // Node management
  addNode: (templateId: string, data: { parentNodeId?: string; name: string; description?: string; contentTypeId?: string; sortOrder?: number; breakContentTypeInheritance?: boolean; children?: any[] }) =>
    apiClient.post(`/folder-templates/${templateId}/nodes`, data),
  updateNode: (nodeId: string, data: { parentNodeId?: string; name: string; description?: string; contentTypeId?: string; sortOrder?: number; breakContentTypeInheritance?: boolean }) =>
    apiClient.put(`/folder-templates/nodes/${nodeId}`, data),
  deleteNode: (nodeId: string) =>
    apiClient.delete(`/folder-templates/nodes/${nodeId}`),

  // Application
  applyToFolder: (folderId: string, data: { templateId: string; namePrefix?: string; overwriteExisting?: boolean }) =>
    apiClient.post(`/folders/${folderId}/apply-template`, data),
  previewApplication: (folderId: string, templateId: string) =>
    apiClient.post(`/folders/${folderId}/preview-template`, { templateId }),
  applyToCabinet: (cabinetId: string, data: { templateId: string; namePrefix?: string; overwriteExisting?: boolean }) =>
    apiClient.post(`/cabinets/${cabinetId}/apply-template`, data),
  previewCabinetApplication: (cabinetId: string, templateId: string) =>
    apiClient.post(`/cabinets/${cabinetId}/preview-template`, { templateId })
}

// =============================================
// Role Permission Matrix API
// =============================================
export const rolePermissionsApi = {
  // System Actions
  getAllActions: (includeInactive = false) =>
    apiClient.get('/role-permissions/actions', { params: { includeInactive } }),
  getActionsByCategory: (category: string) =>
    apiClient.get(`/role-permissions/actions/category/${category}`),
  getCategories: () =>
    apiClient.get('/role-permissions/actions/categories'),

  // Role Permissions
  getMatrix: () =>
    apiClient.get('/role-permissions/matrix'),
  getRolePermissions: (roleId: string) =>
    apiClient.get(`/role-permissions/roles/${roleId}`),
  setRolePermissions: (roleId: string, actionIds: string[]) =>
    apiClient.put(`/role-permissions/roles/${roleId}`, { actionIds }),

  // User Permissions
  getMyPermissions: () =>
    apiClient.get('/role-permissions/my-permissions'),
  getMyAllowedActions: () =>
    apiClient.get('/role-permissions/my-actions'),
  checkPermission: (actionCode: string) =>
    apiClient.get(`/role-permissions/check/${actionCode}`),
  getUserPermissions: (userId: string) =>
    apiClient.get(`/role-permissions/users/${userId}`)
}

// =============================================
// Workflow Statuses API
// =============================================
export const workflowStatusesApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/workflow-statuses', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/workflow-statuses/${id}`),
  create: (data: { name: string; color: string; icon?: string; description?: string; sortOrder: number }) =>
    apiClient.post('/workflow-statuses', data),
  update: (id: string, data: { name: string; color: string; icon?: string; description?: string; sortOrder: number; isActive: boolean }) =>
    apiClient.put(`/workflow-statuses/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/workflow-statuses/${id}`)
}

// =============================================
// Privacy Levels API
// =============================================
export const privacyLevelsApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/privacy-levels', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/privacy-levels/${id}`),
  create: (data: { name: string; level: number; color?: string; description?: string }) =>
    apiClient.post('/privacy-levels', data),
  update: (id: string, data: { name: string; level: number; color?: string; description?: string; isActive: boolean }) =>
    apiClient.put(`/privacy-levels/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/privacy-levels/${id}`)
}

// =============================================
// Disposal API (ISO 15489)
// =============================================
export const disposalApi = {
  getPending: () =>
    apiClient.get('/disposal/pending'),
  getUpcoming: (daysAhead = 30) =>
    apiClient.get('/disposal/upcoming', { params: { daysAhead } }),
  initiate: (documentId: string, data: { reason?: string; legalBasis?: string; disposalMethod?: string; requiresApproval?: boolean }) =>
    apiClient.post(`/disposal/documents/${documentId}/initiate`, data),
  execute: (documentId: string, method = 'HardDelete') =>
    apiClient.post(`/disposal/documents/${documentId}/execute`, null, { params: { method } }),
  getCertificates: (fromDate?: string, toDate?: string) =>
    apiClient.get('/disposal/certificates', { params: { fromDate, toDate } }),
  getCertificate: (id: string) =>
    apiClient.get(`/disposal/certificates/${id}`),
  getCertificateByDocument: (documentId: string) =>
    apiClient.get(`/disposal/certificates/document/${documentId}`),
  processScheduled: () =>
    apiClient.post('/disposal/process-scheduled'),
  // Batch disposal with multi-level approval
  initiateBatch: (data: { documentIds: string[]; disposalMethod?: string; reason?: string; legalBasis?: string; batchReference?: string }) =>
    apiClient.post('/disposal/batch', data),
  getRequests: (params?: { status?: string; page?: number; pageSize?: number }) =>
    apiClient.get('/disposal/requests', { params }),
  getRequest: (id: string) =>
    apiClient.get(`/disposal/requests/${id}`),
  submitApproval: (id: string, data: { decision: string; comments?: string }) =>
    apiClient.post(`/disposal/requests/${id}/approve`, data),
  executeBatch: (id: string) =>
    apiClient.post(`/disposal/requests/${id}/execute`)
}

// =============================================
// Legal Holds API (ISO 15489)
// =============================================
export const legalHoldsApi = {
  getAll: (activeOnly = false) =>
    apiClient.get('/legalholds', { params: { activeOnly } }),
  getById: (id: string) =>
    apiClient.get(`/legalholds/${id}`),
  create: (data: { name: string; description?: string; caseReference?: string; requestedBy?: string; requestedAt?: string; effectiveFrom?: string; effectiveUntil?: string; notes?: string; initialDocumentIds?: string[] }) =>
    apiClient.post('/legalholds', data),
  update: (id: string, data: { name?: string; description?: string; caseReference?: string; requestedBy?: string; effectiveUntil?: string; notes?: string }) =>
    apiClient.put(`/legalholds/${id}`, data),
  addDocuments: (id: string, data: { documentIds: string[]; notes?: string }) =>
    apiClient.post(`/legalholds/${id}/documents`, data),
  getDocuments: (id: string) =>
    apiClient.get(`/legalholds/${id}/documents`),
  removeDocument: (id: string, documentId: string) =>
    apiClient.delete(`/legalholds/${id}/documents/${documentId}`),
  getDocumentHolds: (documentId: string) =>
    apiClient.get(`/legalholds/document/${documentId}`),
  release: (id: string, reason: string) =>
    apiClient.post(`/legalholds/${id}/release`, { reason })
}

// =============================================
// Integrity Verification API (ISO 27001)
// =============================================
export const integrityApi = {
  verifyDocument: (documentId: string) =>
    apiClient.post(`/integrity/documents/${documentId}/verify`),
  verifyVersion: (documentId: string, versionNumber: number) =>
    apiClient.post(`/integrity/documents/${documentId}/versions/${versionNumber}/verify`),
  getHistory: (documentId: string) =>
    apiClient.get(`/integrity/documents/${documentId}/history`),
  batchVerify: (batchSize = 100) =>
    apiClient.post('/integrity/batch-verify', null, { params: { batchSize } })
}

// =============================================
// Document State Machine API (NCAR Governance)
// =============================================
export const documentStateApi = {
  transition: (documentId: string, data: { targetState: string; reason?: string }) =>
    apiClient.post(`/document-state/${documentId}/transition`, data),
  getAllowedTransitions: (documentId: string) =>
    apiClient.get(`/document-state/${documentId}/allowed-transitions`),
  getHistory: (documentId: string) =>
    apiClient.get(`/document-state/${documentId}/history`),
  placeOnHold: (documentId: string, data: { legalHoldId: string; reason?: string }) =>
    apiClient.post(`/document-state/${documentId}/hold`, data),
  releaseFromHold: (documentId: string) =>
    apiClient.post(`/document-state/${documentId}/release-hold`)
}

// =============================================
// Physical Archive API
// =============================================
export const physicalLocationsApi = {
  getAll: () => apiClient.get('/physical-locations'),
  getById: (id: string) => apiClient.get(`/physical-locations/${id}`),
  getChildren: (id: string) => apiClient.get(`/physical-locations/${id}/children`),
  getCapacity: (id: string) => apiClient.get(`/physical-locations/${id}/capacity`),
  create: (data: any) => apiClient.post('/physical-locations', data),
  update: (id: string, data: any) => apiClient.put(`/physical-locations/${id}`, data),
  delete: (id: string) => apiClient.delete(`/physical-locations/${id}`)
}

export const physicalItemsApi = {
  getAll: (params?: { locationId?: string; itemType?: string; page?: number; pageSize?: number }) =>
    apiClient.get('/physical-items', { params }),
  getById: (id: string) => apiClient.get(`/physical-items/${id}`),
  getByBarcode: (barcode: string) => apiClient.get(`/physical-items/barcode/${barcode}`),
  create: (data: any) => apiClient.post('/physical-items', data),
  update: (id: string, data: any) => apiClient.put(`/physical-items/${id}`, data),
  delete: (id: string) => apiClient.delete(`/physical-items/${id}`),
  move: (id: string, targetLocationId: string) =>
    apiClient.post(`/physical-items/${id}/move`, { targetLocationId }),
  updateCondition: (id: string, data: { condition: string; notes?: string }) =>
    apiClient.put(`/physical-items/${id}/condition`, data),
  getCustody: (id: string) => apiClient.get(`/physical-items/${id}/custody`),
  transferCustody: (id: string, data: any) =>
    apiClient.post(`/physical-items/${id}/custody/transfer`, data),
  acknowledgeCustody: (id: string, transferId: string) =>
    apiClient.post(`/physical-items/${id}/custody/${transferId}/acknowledge`)
}

export const accessionsApi = {
  getAll: (params?: { status?: string; page?: number; pageSize?: number }) =>
    apiClient.get('/accessions', { params }),
  getById: (id: string) => apiClient.get(`/accessions/${id}`),
  create: (data: any) => apiClient.post('/accessions', data),
  submit: (id: string) => apiClient.post(`/accessions/${id}/submit`),
  review: (id: string, data: { notes?: string }) =>
    apiClient.post(`/accessions/${id}/review`, data),
  accept: (id: string, data: { notes?: string }) =>
    apiClient.post(`/accessions/${id}/accept`, data),
  reject: (id: string, data: { reason: string }) =>
    apiClient.post(`/accessions/${id}/reject`, data),
  transfer: (id: string) => apiClient.post(`/accessions/${id}/transfer`),
  addItem: (id: string, data: any) => apiClient.post(`/accessions/${id}/items`, data),
  removeItem: (id: string, itemId: string) =>
    apiClient.delete(`/accessions/${id}/items/${itemId}`)
}

export const circulationApi = {
  getAll: (params?: { status?: string; page?: number; pageSize?: number }) =>
    apiClient.get('/circulation', { params }),
  getOverdue: () => apiClient.get('/circulation/overdue'),
  getHistory: (physicalItemId: string) =>
    apiClient.get(`/circulation/history/${physicalItemId}`),
  checkout: (data: { physicalItemId: string; borrowerId: string; borrowerStructureId?: string; purpose?: string; dueDate: string }) =>
    apiClient.post('/circulation/checkout', data),
  return: (id: string, data?: { condition?: string; notes?: string }) =>
    apiClient.post(`/circulation/${id}/return`, data),
  renew: (id: string, data: { newDueDate: string }) =>
    apiClient.post(`/circulation/${id}/renew`, data),
  reportLost: (id: string, data?: { notes?: string }) =>
    apiClient.post(`/circulation/${id}/report-lost`, data)
}

// =============================================
// Enterprise Search API
// =============================================
export const searchApi = {
  searchDocuments: (data: any) =>
    apiClient.post('/search/documents', data),
  searchAll: (data: any) =>
    apiClient.post('/search/all', data),
  reindex: () =>
    apiClient.post('/search/reindex'),
  getHealth: () =>
    apiClient.get('/search/health')
}

// =============================================
// Access Review API
// =============================================
export const accessReviewApi = {
  getCampaigns: () => apiClient.get('/access-review/campaigns'),
  getCampaign: (id: string) => apiClient.get(`/access-review/campaigns/${id}`),
  createCampaign: (data: { name: string; description?: string; dueDate: string; reviewerId?: string }) =>
    apiClient.post('/access-review/campaigns', data),
  getEntries: (campaignId: string) =>
    apiClient.get(`/access-review/campaigns/${campaignId}/entries`),
  submitDecision: (entryId: string, data: { decision: string; comments?: string }) =>
    apiClient.post(`/access-review/entries/${entryId}/decide`, data),
  getStalePermissions: (inactiveDays = 90) =>
    apiClient.get('/access-review/stale-permissions', { params: { inactiveDays } })
}

// =============================================
// System Health API
// =============================================
export const systemHealthApi = {
  getHealth: () => apiClient.get('/system-health'),
  getJobHistory: (params?: { jobName?: string; page?: number; pageSize?: number }) =>
    apiClient.get('/system-health/jobs', { params })
}

// =============================================
// Preservation API (ISO 14721 OAIS)
// =============================================
export const preservationApi = {
  getDocumentPreservation: (documentId: string) =>
    apiClient.get(`/preservation/documents/${documentId}`),
  getSummary: () =>
    apiClient.get('/preservation/summary'),
  getApprovedFormats: () =>
    apiClient.get('/preservation/formats')
}

// =============================================
// Retention Dashboard API
// =============================================
export const retentionDashboardApi = {
  getDashboard: () =>
    apiClient.get('/retention-dashboard'),
  getRecentActions: (take = 20) =>
    apiClient.get('/retention-dashboard/actions', { params: { take } }),
  getJobHistory: (take = 10) =>
    apiClient.get('/retention-dashboard/jobs', { params: { take } })
}
