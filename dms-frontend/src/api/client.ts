import axios, { type AxiosInstance, type AxiosError } from 'axios'
import type { ApiError } from '@/types'

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
  create: (data: { cabinetId: string; parentFolderId?: string; name: string; description?: string }) =>
    apiClient.post('/folders', data),
  update: (id: string, data: { name: string; description?: string; breakInheritance: boolean }) =>
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
  getById: (id: string) =>
    apiClient.get(`/documents/${id}`),
  getVersions: (id: string) =>
    apiClient.get(`/documents/${id}/versions`),
  download: (id: string, version?: number) =>
    apiClient.get(`/documents/${id}/download`, {
      params: { version },
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
  saveWorkingCopy: (id: string, data: {
    name?: string
    description?: string
    classificationId?: string
    importanceId?: string
    documentTypeId?: string
    metadata?: { fieldId: string; fieldName: string; value?: string; numericValue?: number; dateValue?: string }[]
  }) => apiClient.put(`/documents/${id}/working-copy`, data),
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
  getPreviewInfo: (id: string, version?: number) =>
    apiClient.get(`/documents/${id}/preview`, { params: { version } }),

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
  getRecent: (take = 100) =>
    apiClient.get('/activitylogs', { params: { take } }),
  getByNode: (nodeType: string, nodeId: string, skip = 0, take = 50) =>
    apiClient.get(`/activitylogs/by-node/${nodeType}/${nodeId}`, { params: { skip, take } }),
  getByUser: (userId: string, skip = 0, take = 50) =>
    apiClient.get(`/activitylogs/by-user/${userId}`, { params: { skip, take } }),
  getMyActivity: (skip = 0, take = 50) =>
    apiClient.get('/activitylogs/my-activity', { params: { skip, take } })
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
  search: (query: string) =>
    apiClient.get('/users/search', { params: { query } }),
  getById: (id: string) =>
    apiClient.get(`/users/${id}`),
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

// Delegations API
export const delegationsApi = {
  getMyDelegations: () =>
    apiClient.get('/delegations'),
  getDelegationsToMe: () =>
    apiClient.get('/delegations/to-me'),
  getById: (id: string) =>
    apiClient.get(`/delegations/${id}`),
  create: (data: { toUserId: string; roleId?: string; startDate: string; endDate?: string }) =>
    apiClient.post('/delegations', data),
  update: (id: string, data: { startDate: string; endDate?: string; isActive: boolean }) =>
    apiClient.put(`/delegations/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/delegations/${id}`)
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

  // Delegation operations
  createDelegation: (data: {
    delegateId: string
    nodeType: string
    nodeId: string
    permissionLevel: number
    startDate: string
    endDate: string
    reason?: string
  }) => apiClient.post('/permissions/delegations', data),
  revokeDelegation: (id: string) =>
    apiClient.delete(`/permissions/delegations/${id}`),
  getMyDelegations: () =>
    apiClient.get('/permissions/delegations/my'),
  getDelegationsToMe: () =>
    apiClient.get('/permissions/delegations/to-me'),

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
    apiClient.get('/dashboard/my-checkouts')
}

// Reference Data API
export const referenceDataApi = {
  // Classifications
  getClassifications: (language?: string) =>
    apiClient.get('/classifications', { params: { language } }),
  getClassification: (id: string) =>
    apiClient.get(`/classifications/${id}`),
  createClassification: (data: { name: string; description?: string; language?: string }) =>
    apiClient.post('/classifications', data),
  updateClassification: (id: string, data: { name: string; description?: string; language?: string; isActive: boolean }) =>
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
  getLookupItems: (name: string, language?: string) =>
    apiClient.get(`/lookups/${name}`, { params: { language } })
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
    apiClient.delete(`/shares/${id}`)
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

// Vacations API
export const vacationsApi = {
  getMyVacations: () =>
    apiClient.get('/vacations'),
  getActiveVacation: () =>
    apiClient.get('/vacations/active'),
  getAllActive: () =>
    apiClient.get('/vacations/all-active'),
  getById: (id: string) =>
    apiClient.get(`/vacations/${id}`),
  create: (data: { delegateToUserId?: string; startDate: string; endDate: string; message?: string; autoReply: boolean }) =>
    apiClient.post('/vacations', data),
  update: (id: string, data: { delegateToUserId?: string; startDate: string; endDate: string; message?: string; autoReply: boolean; isActive: boolean }) =>
    apiClient.put(`/vacations/${id}`, data),
  delete: (id: string) =>
    apiClient.delete(`/vacations/${id}`),
  cancel: (id: string) =>
    apiClient.post(`/vacations/${id}/cancel`)
}

// Approvals API
export const approvalsApi = {
  // Workflows
  getWorkflows: () =>
    apiClient.get('/approvals/workflows'),
  getWorkflow: (id: string) =>
    apiClient.get(`/approvals/workflows/${id}`),
  createWorkflow: (data: { name: string; description?: string; folderId?: string; requiredApprovers: number; isSequential: boolean; steps?: { stepOrder: number; approverUserId?: string; approverRoleId?: string; isRequired: boolean }[] }) =>
    apiClient.post('/approvals/workflows', data),
  updateWorkflow: (id: string, data: { name: string; description?: string; folderId?: string; requiredApprovers: number; isSequential: boolean }) =>
    apiClient.put(`/approvals/workflows/${id}`, data),
  deleteWorkflow: (id: string) =>
    apiClient.delete(`/approvals/workflows/${id}`),

  // Requests
  getPendingRequests: () =>
    apiClient.get('/approvals/requests/pending'),
  getMyRequests: () =>
    apiClient.get('/approvals/requests/my'),
  submitAction: (requestId: string, data: { action: number; comments?: string }) =>
    apiClient.post(`/approvals/requests/${requestId}/action`, data),
  cancelRequest: (requestId: string) =>
    apiClient.post(`/approvals/requests/${requestId}/cancel`)
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
    apiClient.post(`/approvals/requests/${requestId}/cancel`)
}

// Content Type Definitions API (Form Builder)
export const contentTypeDefinitionsApi = {
  // Content Type Definitions
  getAll: (includeInactive = false) =>
    apiClient.get('/content-type-definitions', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/content-type-definitions/${id}`),
  create: (data: { name: string; description?: string; icon?: string; color?: string; category?: string; allowOnFolders: boolean; allowOnDocuments: boolean; isRequired: boolean; isSystemDefault?: boolean; sortOrder?: number }) =>
    apiClient.post('/content-type-definitions', data),
  update: (id: string, data: { name: string; description?: string; icon?: string; color?: string; category?: string; allowOnFolders: boolean; allowOnDocuments: boolean; isRequired: boolean; isSystemDefault?: boolean; isActive: boolean; sortOrder?: number }) =>
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

// Cases API
export const casesApi = {
  getAll: (includeInactive = false) =>
    apiClient.get('/cases', { params: { includeInactive } }),
  getById: (id: string) =>
    apiClient.get(`/cases/${id}`),
  getByCaseNumber: (caseNumber: string) =>
    apiClient.get(`/cases/number/${caseNumber}`),
  getByStatus: (status: string) =>
    apiClient.get(`/cases/status/${status}`),
  getByAssignee: (userId: string) =>
    apiClient.get(`/cases/assignee/${userId}`),
  create: (data: { caseNumber: string; title: string; description?: string; status?: string; priority?: string; assignedToUserId?: string; folderId?: string; dueDate?: string }) =>
    apiClient.post('/cases', data),
  update: (id: string, data: { caseNumber: string; title: string; description?: string; status?: string; priority?: string; assignedToUserId?: string; folderId?: string; dueDate?: string; closedDate?: string; isActive: boolean }) =>
    apiClient.put(`/cases/${id}`, data),
  updateStatus: (id: string, status: string) =>
    apiClient.put(`/cases/${id}/status`, { status }),
  delete: (id: string) =>
    apiClient.delete(`/cases/${id}`)
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
export const documentAttachmentsApi = {
  getByDocument: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/attachments`),
  getById: (documentId: string, attachmentId: string) =>
    apiClient.get(`/documents/${documentId}/attachments/${attachmentId}`),
  getCount: (documentId: string) =>
    apiClient.get(`/documents/${documentId}/attachments/count`),
  download: (documentId: string, attachmentId: string) =>
    apiClient.get(`/documents/${documentId}/attachments/${attachmentId}/download`, {
      responseType: 'blob'
    }),
  upload: (documentId: string, file: File, description?: string) => {
    const formData = new FormData()
    formData.append('file', file)
    if (description) formData.append('description', description)
    return apiClient.post(`/documents/${documentId}/attachments`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  },
  delete: (documentId: string, attachmentId: string) =>
    apiClient.delete(`/documents/${documentId}/attachments/${attachmentId}`)
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
