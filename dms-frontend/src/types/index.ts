// Cabinet types
export interface Cabinet {
  id: string
  name: string
  description?: string
  breakInheritance: boolean
  createdAt: string
  modifiedAt?: string
}

export interface CreateCabinet {
  name: string
  description?: string
}

export interface UpdateCabinet {
  name: string
  description?: string
  breakInheritance: boolean
}

// Folder types
export interface Folder {
  id: string
  cabinetId: string
  parentFolderId?: string
  name: string
  description?: string
  path?: string
  breakInheritance: boolean
  accessMode?: number
  privacyLevelId?: string
  privacyLevelName?: string
  privacyLevelColor?: string
  privacyLevelValue?: number
  createdAt: string
  modifiedAt?: string
  children?: Folder[]
}

export interface CreateFolder {
  cabinetId: string
  parentFolderId?: string
  name: string
  description?: string
  accessMode?: number
  privacyLevelId?: string
}

export interface UpdateFolder {
  name: string
  description?: string
  breakInheritance: boolean
  accessMode?: number
  privacyLevelId?: string
}

// Document types
export interface Document {
  id: string
  folderId: string
  name: string
  description?: string
  extension?: string
  contentType?: string
  size: number
  currentVersion: number
  currentMajorVersion?: number
  currentMinorVersion?: number
  currentVersionId?: string
  isCheckedOut: boolean
  checkedOutBy?: string
  checkedOutByName?: string
  checkedOutAt?: string
  classificationId?: string
  importanceId?: string
  documentTypeId?: string
  contentTypeId?: string
  contentTypeName?: string
  createdBy?: string
  createdByName?: string
  createdAt: string
  modifiedAt?: string
  hasPassword?: boolean
  isShortcut?: boolean
  shortcutId?: string
  attachmentCount?: number
  approvalStatus?: number | null
  state?: string
  expiryDate?: string | null
  privacyLevelId?: string
  privacyLevelName?: string
  privacyLevelColor?: string
  privacyLevelValue?: number
}

// Document Shortcut
export interface DocumentShortcut {
  id: string
  documentId: string
  folderId: string
  documentName?: string
  folderName?: string
  folderPath?: string
  createdAt: string
}

export interface DocumentVersion {
  id: string
  documentId: string
  versionNumber: number
  size: number
  comment?: string
  createdBy?: string
  createdAt: string
  // ISO 15489 Major/Minor versioning
  versionType: string
  versionLabel?: string
  majorVersion: number
  minorVersion: number
  isContentChanged: boolean
  isMetadataChanged: boolean
  changeDescription?: string
  contentType?: string
  originalFileName?: string
  integrityHash?: string
}

// ISO 15489 Checkout Types
export enum CheckInType {
  Minor = 0,
  Major = 1,
  Overwrite = 2
}

export interface CheckInDto {
  comment?: string
  checkInType: CheckInType
  keepCheckedOut: boolean
  changeDescription?: string
}

export interface WorkingCopy {
  id: string
  documentId: string
  checkedOutBy: string
  checkedOutByName?: string
  checkedOutAt: string
  hasDraftFile: boolean
  draftFileName?: string
  draftSize?: number
  draftContentType?: string
  draftName?: string
  draftDescription?: string
  draftClassificationId?: string
  draftImportanceId?: string
  draftDocumentTypeId?: string
  draftExpiryDate?: string
  draftExpiryDateChanged?: boolean
  draftPrivacyLevelId?: string
  draftPrivacyLevelChanged?: boolean
  draftMetadata?: WorkingCopyMetadataItem[]
  lastModifiedAt?: string
  autoSaveEnabled: boolean
  hasUnsavedChanges: boolean
}

export interface WorkingCopyMetadataItem {
  fieldId: string
  fieldName: string
  value?: string
  numericValue?: number
  dateValue?: string
}

export interface SaveWorkingCopy {
  name?: string
  description?: string
  classificationId?: string
  importanceId?: string
  documentTypeId?: string
  expiryDate?: string | null
  expiryDateChanged?: boolean
  privacyLevelId?: string | null
  privacyLevelChanged?: boolean
  metadata?: WorkingCopyMetadataItem[]
}

export interface VersionComparison {
  documentId: string
  sourceVersion: VersionSummary
  targetVersion: VersionSummary
  contentChanged: boolean
  metadataChanged: boolean
  sizeDifference: number
  metadataDifferences: MetadataDiffItem[]
}

export interface VersionSummary {
  versionId: string
  versionLabel: string
  majorVersion: number
  minorVersion: number
  versionType: string
  size: number
  integrityHash?: string
  contentType?: string
  comment?: string
  createdBy?: string
  createdByName?: string
  createdAt: string
}

export interface MetadataDiffItem {
  fieldId: string
  fieldName: string
  displayName: string
  oldValue?: string
  newValue?: string
  diffType: DiffType
}

export enum DiffType {
  Unchanged = 0,
  Added = 1,
  Removed = 2,
  Modified = 3
}

export interface RestoreVersion {
  comment?: string
  restoreContent: boolean
  restoreMetadata: boolean
}

export interface CreateDocument {
  folderId: string
  name: string
  description?: string
  classificationId?: string
  importanceId?: string
  documentTypeId?: string
  privacyLevelId?: string
}

export interface UpdateDocument {
  name: string
  description?: string
  classificationId?: string
  importanceId?: string
  documentTypeId?: string
}

// Activity Log types
export interface ActivityLog {
  id: string
  action: string
  nodeType?: string
  nodeId?: string
  nodeName?: string
  details?: string
  userId?: string
  userName?: string
  ipAddress?: string
  userAgent?: string
  createdAt: string
}

// Document Lifecycle States (ISO 15489)
export const DocumentStates = {
  Draft: 'Draft',
  Active: 'Active',
  Record: 'Record',
  Archived: 'Archived',
  Disposed: 'Disposed'
} as const

export type DocumentState = typeof DocumentStates[keyof typeof DocumentStates]

export interface TransitionStateRequest {
  targetState: string
  reason?: string
}

// Tree Node type
export interface TreeNode {
  id: string
  name: string
  type: 'cabinet' | 'folder'
  parentId?: string
  children?: TreeNode[]
  isExpanded?: boolean
  isLoading?: boolean
  accessMode?: number
}

// Reference data types
export interface Classification {
  id: string
  name: string
  description?: string
  parentId?: string
  level: number
  code?: string
  fullPath?: string
  confidentialityLevel?: string
  defaultRetentionPolicyId?: string
  defaultPrivacyLevelId?: string
  requiresDisposalApproval: boolean
  sortOrder: number
  isActive: boolean
  language?: string
  children?: Classification[]
}

export interface Importance {
  id: string
  name: string
  level: number
  color?: string
}

export interface DocumentType {
  id: string
  name: string
  description?: string
}

// Privacy Level types
export interface PrivacyLevel {
  id: string
  name: string
  level: number
  color?: string
  description?: string
  isActive: boolean
}

// User types
export interface User {
  id: string
  username: string
  email?: string
  firstName?: string
  lastName?: string
  displayName?: string
  privacyLevel?: number
  roles?: Role[]
}

// Auth types
export interface LoginRequest {
  username: string
  password: string
}

export interface AuthResponse {
  token: string
  refreshToken?: string
  expiresAt: string
  user: User
}

// Permission types
export interface Permission {
  id: string
  nodeType: string
  nodeId: string
  principalType: string
  principalId: string
  principalName?: string
  permissionLevel: number
  isInherited: boolean
  createdAt: string
}

export interface CreatePermission {
  nodeType: string
  nodeId: string
  principalType: string
  principalId: string
  permissionLevel: number
}

// Role type
export interface Role {
  id: string
  name: string
  description?: string
}

// Role Permission Matrix types
export interface SystemAction {
  id: string
  code: string
  name: string
  description?: string
  category: string
  sortOrder: number
  isActive: boolean
}

export interface RolePermissionMatrix {
  roleId: string
  roleName: string
  roleDescription?: string
  allowedActionCodes: string[]
  allowedActions: SystemAction[]
}

export interface UserPermissions {
  userId: string
  username: string
  roles: string[]
  allowedActionCodes: string[]
}

// Dashboard types
export interface DashboardStatistics {
  totalCabinets: number
  totalFolders: number
  totalDocuments: number
  totalUsers: number
  documentsThisMonth: number
  documentsThisYear: number
  myCheckoutsCount: number
  pendingApprovalsCount: number
  expiredDocumentsCount: number
  expiringSoonCount: number
}

export interface RecentDocument {
  id: string
  name: string
  extension?: string
  folderName?: string
  createdAt: string
  createdByName?: string
}

export interface ExpiredDocument {
  id: string
  name: string
  extension?: string
  expiryDate: string
  createdByName?: string
}

// Lookup Item
export interface LookupItem {
  id: string
  lookupId: string
  value: string
  displayText: string
  sortOrder: number
}

// API Response types
export interface ApiError {
  message: string
  errors?: string[]
}

// Permission Levels
export const PermissionLevels = {
  Read: 1,
  Write: 2,
  Delete: 4,
  Admin: 8
} as const

export type PermissionLevel = typeof PermissionLevels[keyof typeof PermissionLevels]

// Favorite types
export interface FavoriteItem {
  id: string
  nodeType: number
  nodeId: string
  name: string
  description?: string
  path?: string
  parentFolderId?: string
  cabinetId?: string
  createdAt: string
  favoritedAt: string
}

export interface ToggleFavoriteRequest {
  nodeType: number
  nodeId: string
}

// Share types
export interface SharedDocument {
  shareId: string
  documentId: string
  documentName: string
  extension?: string
  size: number
  permissionLevel: number
  sharedByUserName?: string
  sharedAt: string
  expiresAt?: string
  message?: string
  hasPassword?: boolean
}

export interface MySharedItem {
  shareId: string
  documentId: string
  documentName: string
  extension?: string
  sharedWithUserName: string
  permissionLevel: number
  sharedAt: string
  expiresAt?: string
  hasPassword?: boolean
  isLinkShare?: boolean
}

export interface DocumentShare {
  id: string
  documentId: string
  sharedWithUserId?: string
  sharedByUserId: string
  permissionLevel: number
  expiresAt?: string
  message?: string
  isActive: boolean
  createdAt: string
  isLinkShare?: boolean
  shareToken?: string
  documentName?: string
  sharedWithUserName?: string
  sharedByUserName?: string
}

export interface ShareDocumentRequest {
  documentId: string
  sharedWithUserId: string
  permissionLevel: number
  expiresAt?: string
  message?: string
}

export interface LinkShare {
  id: string
  documentId: string
  shareToken: string
  permissionLevel: number
  expiresAt?: string
  isActive: boolean
  createdAt: string
  sharedByUserName?: string
}

// Recycle Bin types
export interface RecycleBinItem {
  id: string
  nodeType: number
  nodeId: string
  nodeName: string
  originalPath?: string
  originalParentId?: string
  deletedBy: string
  deletedAt: string
  expiresAt?: string
  deletedByUserName?: string
  nodeTypeName: string
}

export interface RestoreRequest {
  id: string
  restoreToFolderId?: string
}



// Workflow Status types
export interface WorkflowStatus {
  id: string
  name: string
  color: string
  icon?: string
  description?: string
  sortOrder: number
  isActive: boolean
  createdAt: string
}

export interface CreateWorkflowStatus {
  name: string
  color: string
  icon?: string
  description?: string
  sortOrder: number
}

export interface UpdateWorkflowStatus {
  name: string
  color: string
  icon?: string
  description?: string
  sortOrder: number
  isActive: boolean
}

// Approval types
export interface ApprovalWorkflow {
  id: string
  name: string
  description?: string
  folderId?: string
  requiredApprovers: number
  isSequential: boolean
  isActive: boolean
  createdAt: string
  folderName?: string
  designerData?: string
  triggerType: string
  inheritToSubfolders: boolean
  steps?: ApprovalWorkflowStep[]
}

export interface ApprovalWorkflowStep {
  id: string
  workflowId: string
  stepOrder: number
  approverUserId?: string
  approverRoleId?: string
  approverStructureId?: string
  assignToManager: boolean
  isRequired: boolean
  statusId?: string
  approverUserName?: string
  approverRoleName?: string
  approverStructureName?: string
  statusName?: string
  statusColor?: string
}

export interface ApprovalRequest {
  id: string
  documentId: string
  workflowId?: string
  requestedBy: string
  status: number
  dueDate?: string
  comments?: string
  createdAt: string
  completedAt?: string
  documentName?: string
  requestedByName?: string
  workflowName?: string
  statusName: string
  actions?: ApprovalAction[]
}

export interface ApprovalAction {
  id: string
  requestId: string
  stepId?: string
  approverId: string
  action: number
  comments?: string
  actionDate: string
  approverName?: string
  actionName: string
}

export interface CreateApprovalRequest {
  documentId: string
  workflowId?: string
  dueDate?: string
  comments?: string
}

export interface SubmitApprovalAction {
  action: number
  comments?: string
}

export interface CreateWorkflowRequest {
  name: string
  description?: string
  folderId?: string
  requiredApprovers: number
  isSequential: boolean
  triggerType?: string
  inheritToSubfolders?: boolean
  steps?: CreateWorkflowStep[]
}

export interface CreateWorkflowStep {
  stepOrder: number
  approverUserId?: string
  approverRoleId?: string
  approverStructureId?: string
  assignToManager: boolean
  isRequired: boolean
  statusId?: string
}

// Node types
export const NodeTypes = {
  Cabinet: 1,
  Folder: 2,
  Document: 3
} as const

// Approval status
export const ApprovalStatus = {
  Pending: 0,
  Approved: 1,
  Rejected: 2,
  Cancelled: 3,
  ReturnedForRevision: 4
} as const

// Approval action types
export const ApprovalActionTypes = {
  Approved: 1,
  Rejected: 2,
  ReturnedForRevision: 3
} as const

// File Type (MIME type / extension configuration)
// Note: Renamed from ContentType to avoid confusion with ContentTypeDefinition (metadata schema)
export interface FileType {
  id: string
  extension: string
  mimeType: string
  displayName?: string
  icon?: string
  allowPreview: boolean
  allowThumbnail: boolean
  maxFileSizeMB: number
  isActive: boolean
  createdAt: string
}

// Legacy alias for backwards compatibility
export type ContentType = FileType

// Folder Link
export interface FolderLink {
  id: string
  sourceFolderId: string
  targetFolderId: string
  createdBy?: string
  createdAt: string
  sourceFolderName?: string
  targetFolderName?: string
  targetFolderPath?: string
}

// Filing Plan
export interface FilingPlan {
  id: string
  folderId: string
  name: string
  description?: string
  pattern?: string
  classificationId?: string
  documentTypeId?: string
  isActive: boolean
  createdBy?: string
  createdAt: string
  folderName?: string
  classificationName?: string
  documentTypeName?: string
}

// Content Type Definition (Form Builder)
export interface ContentTypeDefinition {
  id: string
  name: string
  description?: string
  icon?: string
  color?: string
  category?: string // Category for retention policy classification
  allowOnFolders: boolean
  allowOnDocuments: boolean
  isRequired: boolean
  isSystemDefault: boolean // If true, this is the fallback content type when no specific type is assigned
  defaultClassificationId?: string
  isActive: boolean
  sortOrder: number
  createdAt: string
  modifiedAt?: string
  fieldCount?: number
  fields?: ContentTypeField[]
}

export interface ContentTypeField {
  id: string
  contentTypeId: string
  fieldName: string
  displayName: string
  description?: string
  fieldType: string
  isRequired: boolean
  isReadOnly: boolean
  showInList: boolean
  isSearchable: boolean
  defaultValue?: string
  validationRules?: string
  lookupName?: string
  options?: string
  groupName?: string
  columnSpan: number
  sortOrder: number
  isActive: boolean
  createdAt: string
}

export interface DocumentMetadata {
  id: string
  documentId: string
  contentTypeId: string
  fieldId: string
  fieldName: string
  value?: string
  numericValue?: number
  dateValue?: string
  createdAt: string
  modifiedAt?: string
}

export interface FolderContentTypeAssignment {
  id: string
  folderId: string
  contentTypeId: string
  isRequired: boolean
  isDefault: boolean
  inheritToChildren: boolean
  displayOrder: number
  createdAt: string
  contentTypeName?: string
  contentTypeDescription?: string
  contentTypeIcon?: string
  contentTypeColor?: string
}

export interface CabinetContentTypeAssignment {
  id: string
  cabinetId: string
  contentTypeId: string
  isRequired: boolean
  isDefault: boolean
  inheritToChildren: boolean
  displayOrder: number
  createdAt: string
  contentTypeName?: string
  contentTypeDescription?: string
  contentTypeIcon?: string
  contentTypeColor?: string
}

// Effective content type (after inheritance calculation)
export interface EffectiveContentType {
  contentTypeId: string
  name: string
  description?: string
  icon?: string
  color?: string
  category?: string
  isRequired: boolean
  isDefault: boolean
  displayOrder: number
  source: 'Direct' | 'Inherited' | 'Cabinet' | 'SystemDefault'
  sourceName?: string
  sourceId?: string
  fields?: ContentTypeField[]
}

// Folder content type info summary
export interface FolderContentTypeInfo {
  folderId: string
  folderName: string
  hasRequiredContentTypes: boolean
  hasContentTypes: boolean
  defaultContentTypeId?: string
  defaultContentTypeName?: string
  totalContentTypes: number
  breaksInheritance: boolean
  contentTypes: EffectiveContentType[]
}

export interface CreateContentTypeDefinition {
  name: string
  description?: string
  icon?: string
  color?: string
  category?: string // Category for retention policy classification
  allowOnFolders: boolean
  allowOnDocuments: boolean
  isRequired: boolean
  isSystemDefault?: boolean // If true, this is the fallback content type when no specific type is assigned
  sortOrder?: number
}

export interface UpdateContentTypeDefinition extends CreateContentTypeDefinition {
  isActive: boolean
}

export interface CreateContentTypeField {
  fieldName: string
  displayName: string
  description?: string
  fieldType: string
  isRequired: boolean
  isReadOnly: boolean
  showInList: boolean
  isSearchable: boolean
  defaultValue?: string
  validationRules?: string
  lookupName?: string
  options?: string
  groupName?: string
  columnSpan: number
}

export interface UpdateContentTypeField extends CreateContentTypeField {
  sortOrder: number
  isActive: boolean
}

export interface SaveMetadataRequest {
  fieldId: string
  fieldName: string
  value?: string
  numericValue?: number
  dateValue?: string
}

// Field types enum
export const FieldTypes = {
  Text: 'Text',
  TextArea: 'TextArea',
  Number: 'Number',
  Decimal: 'Decimal',
  Date: 'Date',
  DateTime: 'DateTime',
  Boolean: 'Boolean',
  Dropdown: 'Dropdown',
  MultiSelect: 'MultiSelect',
  User: 'User',
  Lookup: 'Lookup'
} as const

export type FieldType = typeof FieldTypes[keyof typeof FieldTypes]

// Pattern types
export interface Pattern {
  id: string
  name: string
  regex: string
  description?: string
  patternType: string
  targetFolderId?: string
  contentTypeId?: string
  classificationId?: string
  documentTypeId?: string
  priority: number
  isActive: boolean
  createdAt: string
  modifiedAt?: string
  targetFolderName?: string
  contentTypeName?: string
  classificationName?: string
  documentTypeName?: string
}

export const PatternTypes = {
  Naming: 'Naming',
  Filing: 'Filing',
  Validation: 'Validation',
  Search: 'Search'
} as const

// Retention Policy types
export interface RetentionPolicy {
  id: string
  name: string
  description?: string
  retentionDays: number
  expirationAction: string
  notifyBeforeExpiration: boolean
  notificationDays: number
  folderId?: string
  classificationId?: string
  documentTypeId?: string
  requiresApproval: boolean
  inheritToSubfolders: boolean
  isLegalHold: boolean
  isActive: boolean
  retentionBasis?: string
  suspendDuringLegalHold?: boolean
  recalculateOnClassificationChange?: boolean
  disposalApprovalLevels?: number
  createdAt: string
  modifiedAt?: string
  folderName?: string
  classificationName?: string
  documentTypeName?: string
}

export interface DocumentRetention {
  id: string
  documentId: string
  policyId: string
  retentionStartDate: string
  expirationDate?: string
  status: string
  notificationSent: boolean
  actionDate?: string
  approvedBy?: string
  notes?: string
  createdAt: string
  modifiedAt?: string
  documentName?: string
  policyName?: string
}

export const ExpirationActions = {
  Archive: 'Archive',
  Delete: 'Delete',
  Review: 'Review',
  Notify: 'Notify'
} as const

export const RetentionStatuses = {
  Active: 'Active',
  PendingReview: 'PendingReview',
  Approved: 'Approved',
  Archived: 'Archived',
  Deleted: 'Deleted',
  OnHold: 'OnHold'
} as const

// Bookmark types
export interface Bookmark {
  id: string
  name: string
  placeholder: string
  description?: string
  defaultValue?: string
  dataType: string
  lookupName?: string
  isSystem: boolean
  isActive: boolean
  sortOrder: number
  createdAt: string
}

// ServiceEndpoint types
export interface ServiceEndpoint {
  id: string
  name: string
  url: string
  description?: string
  endpointType: string
  authType?: string
  authConfig?: string
  timeoutSeconds: number
  retryCount: number
  headers?: string
  isActive: boolean
  lastHealthCheck?: string
  lastHealthStatus?: string
  createdAt: string
}

// Export Config types
export interface ExportConfig {
  id: string
  name: string
  description?: string
  exportFormat: string
  includeMetadata: boolean
  includeVersions: boolean
  includeAuditTrail: boolean
  flattenFolders: boolean
  namingPattern?: string
  watermarkText?: string
  maxFileSizeMB?: number
  isDefault: boolean
  isActive: boolean
  createdAt: string
}

// Naming Convention types
export interface NamingConvention {
  id: string
  name: string
  description?: string
  pattern: string
  appliesTo: string
  folderId?: string
  documentTypeId?: string
  isRequired: boolean
  autoGenerate: boolean
  separator?: string
  isActive: boolean
  sortOrder: number
  createdAt: string
  folderName?: string
  documentTypeName?: string
}

// Organization Template types
export interface OrganizationTemplate {
  id: string
  name: string
  description?: string
  structure: string
  defaultPermissions?: string
  includeContentTypes: boolean
  isDefault: boolean
  isActive: boolean
  createdAt: string
}

// Permission Level Definition types
export interface PermissionLevelDefinition {
  id: string
  name: string
  description?: string
  level: number
  color?: string
  icon?: string
  canRead: boolean
  canWrite: boolean
  canDelete: boolean
  canAdmin: boolean
  canShare: boolean
  canExport: boolean
  isSystem: boolean
  isActive: boolean
  sortOrder: number
  createdAt: string
}

// Purpose types
export interface Purpose {
  id: string
  name: string
  description?: string
  purposeType: string
  requiresJustification: boolean
  requiresApproval: boolean
  isDefault: boolean
  isActive: boolean
  sortOrder: number
  createdAt: string
}

// Scan Config types
export interface ScanConfig {
  id: string
  name: string
  description?: string
  resolution: number
  colorMode: string
  outputFormat: string
  enableOCR: boolean
  ocrLanguage: string
  autoDeskew: boolean
  autoCrop: boolean
  removeBlankPages: boolean
  compressionQuality?: number
  targetFolderId?: string
  isDefault: boolean
  isActive: boolean
  createdAt: string
  targetFolderName?: string
}

// Search Config types
export interface SearchConfig {
  id: string
  name: string
  description?: string
  searchType: string
  defaultFields?: string
  filters?: string
  includeContent: boolean
  includeMetadata: boolean
  includeVersions: boolean
  fuzzyMatch: boolean
  maxResults: number
  sortField?: string
  sortDirection: string
  isGlobal: boolean
  isDefault: boolean
  isActive: boolean
  createdAt: string
}

// Preview types
export interface PreviewInfo {
  type: PreviewType
  contentType: string
  fileName: string
  fileSize: number
  textContent?: string
}

export type PreviewType = 'Pdf' | 'Image' | 'Text' | 'Office' | 'Video' | 'Audio' | 'Unsupported'

// Bulk operation types
export interface BulkOperationResult {
  totalRequested: number
  successCount: number
  failedCount: number
  errors: BulkOperationError[]
}

export interface BulkOperationError {
  documentId: string
  documentName: string
  error: string
}

// Upload types
export interface UploadItem {
  id: string
  file: File
  status: 'pending' | 'uploading' | 'completed' | 'error'
  progress: number
  error?: string
  documentId?: string
}

// Enterprise Permission System Types

// Enhanced Permission with enterprise fields
export interface EnterprisePermission {
  id: string
  nodeType: string
  nodeId: string
  principalType: 'User' | 'Role' | 'Structure'
  principalId: string
  principalName?: string
  permissionLevel: number
  isInherited: boolean
  inheritedFromNodeId?: string
  inheritedFromNodeType?: string
  inheritedFromNodeName?: string
  includeChildStructures: boolean
  expiresAt?: string
  grantedReason?: string
  grantedBy?: string
  grantedByName?: string
  createdAt: string
}

export interface CreateEnterprisePermission {
  nodeType: string
  nodeId: string
  principalType: string
  principalId: string
  permissionLevel: number
  includeChildStructures?: boolean
  expiresAt?: string
  grantedReason?: string
}

export interface UpdateEnterprisePermission {
  permissionLevel: number
  includeChildStructures: boolean
  expiresAt?: string
  grantedReason?: string
}

export interface NodePermissions {
  nodeType: string
  nodeId: string
  nodeName?: string
  breakInheritance: boolean
  permissions: EnterprisePermission[]
}

export interface EffectivePermission {
  userId: string
  userName?: string
  nodeType: string
  nodeId: string
  permissionLevel: number
  sourceType: string
  sourceNodeId?: string
  sourceNodeName?: string
}

// Organizational Structure Types
export interface Structure {
  id: string
  code: string
  name: string
  nameAr?: string
  type: StructureType
  parentId?: string
  parentName?: string
  path?: string
  level: number
  isActive: boolean
  sortOrder?: number
  createdAt: string
  memberCount: number
  childCount: number
}

export type StructureType = 'Ministry' | 'Department' | 'Division' | 'Section' | 'Unit'

export interface CreateStructure {
  code: string
  name: string
  nameAr?: string
  type: string
  parentId?: string
  sortOrder?: number
}

export interface UpdateStructure {
  code: string
  name: string
  nameAr?: string
  type: string
  parentId?: string
  isActive: boolean
  sortOrder?: number
}

export interface StructureMember {
  id: string
  structureId: string
  structureName?: string
  userId: string
  userName?: string
  userDisplayName?: string
  position?: string
  isPrimary: boolean
  startDate: string
  endDate?: string
  isActive: boolean
}

export interface AddStructureMember {
  userId: string
  position?: string
  isPrimary: boolean
  startDate: string
  endDate?: string
}

export interface StructureTree {
  id: string
  code: string
  name: string
  type: string
  isActive: boolean
  memberCount: number
  children: StructureTree[]
}

// Permission Audit Types
export interface PermissionAudit {
  id: string
  action: string
  nodeType: string
  nodeId: string
  nodeName?: string
  principalType: string
  principalId: string
  principalName?: string
  oldLevel?: number
  newLevel?: number
  reason?: string
  performedBy: string
  performedByName?: string
  performedAt: string
}



// Principal types for selector
export interface Principal {
  id: string
  type: 'User' | 'Role' | 'Structure'
  name: string
  displayName?: string
  description?: string
  icon?: string
}

// Folder Structure Template Types
export interface FolderTemplate {
  id: string
  name: string
  description?: string
  category?: string
  icon?: string
  isActive: boolean
  isDefault: boolean
  createdAt: string
  createdByName?: string
  usageCount: number
  nodes: FolderTemplateNode[]
}

export interface FolderTemplateNode {
  id: string
  parentNodeId?: string
  name: string
  description?: string
  contentTypeId?: string
  contentTypeName?: string
  sortOrder: number
  breakContentTypeInheritance: boolean
  children: FolderTemplateNode[]
}

export interface CreateFolderTemplate {
  name: string
  description?: string
  category?: string
  icon?: string
  isDefault: boolean
  nodes: CreateTemplateNode[]
}

export interface UpdateFolderTemplate {
  name: string
  description?: string
  category?: string
  icon?: string
  isDefault: boolean
  isActive: boolean
}

export interface CreateTemplateNode {
  parentNodeId?: string
  name: string
  description?: string
  contentTypeId?: string
  sortOrder: number
  breakContentTypeInheritance: boolean
  children: CreateTemplateNode[]
}

export interface UpdateTemplateNode {
  parentNodeId?: string
  name: string
  description?: string
  contentTypeId?: string
  sortOrder: number
  breakContentTypeInheritance: boolean
}

export interface ApplyTemplateRequest {
  templateId: string
  namePrefix?: string
  overwriteExisting?: boolean
}

export interface ApplyTemplateResult {
  success: boolean
  foldersCreated: number
  foldersSkipped: number
  createdFolderPaths: string[]
  errors: string[]
}

export interface FolderTemplateUsage {
  id: string
  templateId: string
  templateName?: string
  rootFolderId: string
  folderName?: string
  cabinetId: string
  appliedBy: string
  appliedByName?: string
  appliedAt: string
  foldersCreated: number
}

// Scan to Upload types
export interface ScanPageItem {
  id: string
  file: File
  thumbnailUrl: string
  rotationDegrees: number
  originalIndex: number
}

export interface ScanProcessResult {
  documentId: string
  documentName: string
  pageCount: number
  fileSize: number
  ocrApplied: boolean
  ocrText?: string
}

// =============================================
// PDF Annotation Types
// =============================================
export interface DocumentAnnotation {
  id: string
  documentId: string
  pageNumber: number
  annotationData: string // Fabric.js JSON
  versionNumber: number
  createdBy: string
  createdByName?: string
  createdAt: string
  modifiedBy?: string
  modifiedAt?: string
}

export interface SaveAnnotationsRequest {
  pages: PageAnnotationData[]
}

export interface PageAnnotationData {
  pageNumber: number
  annotationData: string
}

export interface SavedSignature {
  id: string
  name: string
  signatureData: string
  signatureType: 'drawn' | 'typed'
  isDefault: boolean
  createdAt: string
}

export interface CreateSignatureRequest {
  name: string
  signatureData: string
  signatureType: 'drawn' | 'typed'
  isDefault: boolean
}

export type AnnotationTool = 'select' | 'freehand' | 'highlight' | 'redaction' | 'signature' | 'text'

export interface AnnotationToolSettings {
  strokeColor: string
  strokeWidth: number
  highlightColor: string
  fontSize: number
}

// Scanner Agent types (local TWAIN/WIA agent)
export interface ScannerDevice {
  id: string
  name: string
  driver: string
}

export interface ScanAgentStatus {
  version: string
  status: string
}

export interface ScannedPage {
  data: string // base64
  format: string
}

// PDF Page Management types
export interface PageManifestEntry {
  source: 'existing' | 'upload'
  originalPage?: number    // 1-based, for source="existing"
  fileIndex?: number       // 0-based, for source="upload"
  uploadPageNumber?: number
}

export interface PageReorganizeResult {
  documentId: string
  versionLabel: string
  pageCount: number
  fileSize: number
}

export interface ManagedPage {
  id: string
  source: 'existing' | 'upload'
  originalPage?: number
  fileIndex?: number
  uploadPageNumber?: number
  label: string
  thumbnailUrl?: string // blob URL for uploaded image previews
}

// =============================================
// Disposal Types (ISO 15489)
// =============================================
export interface DisposalCertificate {
  id: string
  certificateNumber: string
  documentId: string
  documentName: string
  documentPath?: string
  classification?: string
  retentionPolicyId?: string
  retentionPolicyName?: string
  documentCreatedAt: string
  retentionStartDate?: string
  retentionExpirationDate?: string
  disposalMethod: string
  disposedAt: string
  disposedBy: string
  disposedByName?: string
  approvedBy?: string
  approvedByName?: string
  approvedAt?: string
  legalBasis?: string
  notes?: string
  contentHashAtDisposal?: string
  fileSizeAtDisposal: number
  versionsDisposed: number
  disposalVerified: boolean
  verifiedAt?: string
  createdAt: string
}

export interface DisposalRequest {
  id: string
  documentId: string
  documentName: string
  status: string
  disposalMethod: string
  reason?: string
  legalBasis?: string
  requestedBy: string
  requestedByName?: string
  requestedAt: string
  approvedBy?: string
  approvedByName?: string
  approvedAt?: string
  rejectedBy?: string
  rejectedByName?: string
  rejectedAt?: string
  rejectionReason?: string
}

export interface DocumentDisposalStatus {
  documentId: string
  documentName: string
  folderPath?: string
  retentionPolicyId?: string
  retentionPolicyName?: string
  documentCreatedAt: string
  retentionStartDate?: string
  retentionExpirationDate?: string
  daysUntilExpiration: number
  isExpired: boolean
  isOnLegalHold: boolean
  expirationAction?: string
  requiresApproval: boolean
}

export interface InitiateDisposalRequest {
  reason?: string
  legalBasis?: string
  disposalMethod?: string
  requiresApproval?: boolean
}

export interface DisposalBatchResult {
  totalPending: number
  processedCount: number
  disposedCount: number
  skippedCount: number
  errorCount: number
  errors: string[]
  startedAt: string
  completedAt: string
}

// =============================================
// Legal Hold Types (ISO 15489)
// =============================================
export interface LegalHold {
  id: string
  holdNumber: string
  name: string
  description?: string
  caseReference?: string
  requestedBy?: string
  requestedAt?: string
  status: string
  effectiveFrom: string
  effectiveUntil?: string
  appliedBy: string
  appliedByName?: string
  appliedAt: string
  releasedBy?: string
  releasedByName?: string
  releasedAt?: string
  releaseReason?: string
  notes?: string
  documentCount: number
  createdAt: string
}

export interface CreateLegalHold {
  name: string
  description?: string
  caseReference?: string
  requestedBy?: string
  requestedAt?: string
  effectiveFrom?: string
  effectiveUntil?: string
  notes?: string
  initialDocumentIds?: string[]
}

export interface UpdateLegalHold {
  name?: string
  description?: string
  caseReference?: string
  requestedBy?: string
  effectiveUntil?: string
  notes?: string
}

export interface LegalHoldDocument {
  id: string
  legalHoldId: string
  holdName: string
  documentId: string
  documentName: string
  addedAt: string
  addedBy: string
  addedByName?: string
  releasedAt?: string
  releasedBy?: string
  notes?: string
}

// =============================================
// Integrity Verification Types (ISO 27001)
// =============================================
export interface IntegrityVerificationResult {
  isValid: boolean
  expectedHash: string
  computedHash: string
  algorithm: string
  verifiedAt: string
  errorMessage?: string
  documentId?: string
  versionNumber?: number
}

export interface IntegrityVerificationLog {
  id: string
  documentId: string
  versionNumber?: number
  expectedHash: string
  computedHash: string
  hashAlgorithm: string
  isValid: boolean
  verifiedAt: string
  verificationType: string
  verifiedBy?: string
  verifiedByName?: string
  errorMessage?: string
  actionTaken?: string
}

export interface IntegrityBatchResult {
  totalDocuments: number
  verifiedCount: number
  passedCount: number
  failedCount: number
  skippedCount: number
  failures: IntegrityVerificationResult[]
  startedAt: string
  completedAt: string
}

// =============================================
// State Machine Types (NCAR Governance)
// =============================================
export interface StateTransitionRequest {
  targetState: string
  reason?: string
}

export interface AllowedTransition {
  fromState: string
  toState: string
  description?: string
  requiresApproval: boolean
  requiresClassification: boolean
  requiresRetentionPolicy: boolean
}

export interface StateTransitionLog {
  id: string
  documentId: string
  fromState: string
  toState: string
  transitionedBy: string
  transitionedByName?: string
  transitionedAt: string
  reason?: string
  isSystemAction: boolean
}

// =============================================
// Physical Archive Types
// =============================================
export type LocationType = 'Site' | 'Building' | 'Floor' | 'Room' | 'Rack' | 'Shelf' | 'Box' | 'File'
export type PhysicalItemType = 'Document' | 'File' | 'Box' | 'Volume' | 'Map' | 'Photograph' | 'AudioTape' | 'VideoTape' | 'Microfilm' | 'DigitalMedia'
export type ItemCondition = 'Good' | 'Fair' | 'Poor' | 'Damaged' | 'Destroyed'
export type CirculationStatus = 'Available' | 'CheckedOut' | 'InTransit' | 'Overdue' | 'Returned' | 'Lost'

export interface PhysicalLocation {
  id: string
  parentId?: string
  name: string
  nameAr?: string
  code: string
  locationType: LocationType
  path?: string
  level: number
  capacity?: number
  currentCount: number
  environmentalConditions?: string
  coordinates?: string
  securityLevel?: string
  sortOrder: number
  isActive: boolean
  createdAt: string
}

export interface CreatePhysicalLocation {
  parentId?: string
  name: string
  nameAr?: string
  code: string
  locationType: string
  capacity?: number
  environmentalConditions?: string
  coordinates?: string
  securityLevel?: string
  sortOrder?: number
}

export interface PhysicalItem {
  id: string
  barcode: string
  barcodeFormat?: string
  title: string
  titleAr?: string
  description?: string
  itemType: PhysicalItemType
  locationId?: string
  locationName?: string
  digitalDocumentId?: string
  classificationId?: string
  retentionPolicyId?: string
  condition: ItemCondition
  itemDate?: string
  dateRangeStart?: string
  dateRangeEnd?: string
  pageCount?: number
  dimensions?: string
  circulationStatus: CirculationStatus
  isOnLegalHold: boolean
  lastConditionAssessment?: string
  conditionNotes?: string
  isActive: boolean
  createdAt: string
}

export interface CreatePhysicalItem {
  barcode: string
  barcodeFormat?: string
  title: string
  titleAr?: string
  description?: string
  itemType: string
  locationId?: string
  digitalDocumentId?: string
  classificationId?: string
  retentionPolicyId?: string
  condition?: string
  itemDate?: string
  pageCount?: number
  dimensions?: string
}

export interface AccessionRequest {
  id: string
  accessionNumber: string
  sourceStructureId?: string
  sourceStructureName?: string
  targetLocationId?: string
  targetLocationName?: string
  status: string
  itemCount: number
  recordsDateFrom?: string
  recordsDateTo?: string
  requestedTransferDate?: string
  actualTransferDate?: string
  reviewedByName?: string
  reviewNotes?: string
  acceptedByName?: string
  rejectionReason?: string
  createdAt: string
}

export interface CreateAccessionRequest {
  sourceStructureId?: string
  targetLocationId?: string
  recordsDateFrom?: string
  recordsDateTo?: string
  requestedTransferDate?: string
  notes?: string
}

export interface CirculationRecord {
  id: string
  physicalItemId: string
  physicalItemTitle?: string
  physicalItemBarcode?: string
  borrowerId: string
  borrowerName?: string
  purpose?: string
  checkedOutAt: string
  dueDate: string
  renewalCount: number
  maxRenewals: number
  returnedAt?: string
  conditionAtCheckout: string
  conditionAtReturn?: string
  status: string
  createdAt: string
}

export interface CheckoutRequest {
  physicalItemId: string
  borrowerId: string
  borrowerStructureId?: string
  purpose?: string
  dueDate: string
}

export interface CustodyTransfer {
  id: string
  physicalItemId: string
  fromUserName?: string
  toUserName?: string
  fromLocationName?: string
  toLocationName?: string
  transferType: string
  conditionAtTransfer: string
  isAcknowledged: boolean
  acknowledgedAt?: string
  transferredAt: string
}

// =============================================
// Search Types
// =============================================
export interface SearchDocumentsRequest {
  query: string
  page?: number
  pageSize?: number
  classificationId?: string
  documentTypeId?: string
  state?: string
  extension?: string
  dateFrom?: string
  dateTo?: string
  sortBy?: string
  sortDescending?: boolean
  searchAfterToken?: string
}

export interface SearchResult {
  items: SearchResultItem[]
  totalCount: number
  page: number
  pageSize: number
  searchAfterToken?: string
  facets: FacetGroup[]
  elapsedMs: number
}

export interface SearchResultItem {
  id: string
  entityType: string
  name: string
  description?: string
  extension?: string
  size: number
  state?: string
  classificationName?: string
  documentTypeName?: string
  folderPath?: string
  createdAt: string
  createdByName?: string
  highlights: string[]
  score: number
}

export interface FacetGroup {
  field: string
  values: FacetValue[]
}

export interface FacetValue {
  value: string
  label?: string
  count: number
}

export interface SearchHealth {
  isAvailable: boolean
  provider: string
  clusterName?: string
  clusterStatus?: string
  documentCount: number
  pendingIndexCount: number
}

// =============================================
// Access Review Types
// =============================================
export interface AccessReviewCampaign {
  id: string
  name: string
  description?: string
  status: string
  startDate: string
  dueDate: string
  reviewerId?: string
  totalEntries: number
  completedEntries: number
  createdAt: string
}

export interface CreateAccessReviewCampaign {
  name: string
  description?: string
  dueDate: string
  reviewerId?: string
}

export interface CampaignReviewEntry {
  id: string
  campaignId: string
  userId: string
  userName?: string
  nodeType: string
  nodeId: string
  nodeName?: string
  permissionLevel: number
  permissionSource?: string
  decision: string
  comments?: string
  decidedAt?: string
}

export interface SubmitAccessReview {
  decision: string
  comments?: string
}

export interface StalePermission {
  userId: string
  userName?: string
  nodeType: string
  nodeId: string
  nodeName?: string
  permissionLevel: number
  lastLoginAt?: string
  inactiveDays: number
}

// =============================================
// System Health Types
// =============================================
export interface SystemHealth {
  isHealthy: boolean
  database: DatabaseHealth
  search?: SearchHealth
  storage: StorageHealth
  recentJobs: JobExecutionSummary[]
}

export interface DatabaseHealth {
  isAvailable: boolean
  serverVersion?: string
  totalDocuments: number
  totalUsers: number
  activeLegalHolds: number
}

export interface StorageHealth {
  basePath: string
  totalBytes: number
  usedBytes: number
  availableBytes: number
  usagePercent: number
}

export interface JobExecutionSummary {
  jobName: string
  status: string
  startedAt: string
  completedAt?: string
  durationMs?: number
  itemsProcessed: number
  itemsFailed: number
  errorMessage?: string
}

// =============================================
// Preservation Types
// =============================================
export interface PreservationMetadata {
  id: string
  documentId: string
  versionNumber: number
  formatName?: string
  formatIdentifier?: string
  formatRegistry?: string
  isPreservationFormat: boolean
  migrationTargetFormat?: string
  identifiedAt: string
  identificationTool?: string
  creatingApplication?: string
}

export interface PreservationSummary {
  totalDocuments: number
  preservationCompliant: number
  needsMigration: number
  pdfACompliant: number
  formatDistribution: FormatDistribution[]
}

export interface FormatDistribution {
  extension: string
  count: number
  isPreservationFormat: boolean
}

export interface PreservationFormat {
  extension: string
  formatName: string
  pronomPuid?: string
  isApprovedForPreservation: boolean
  migrationTargetFormat?: string
  notes?: string
}

// =============================================
// Retention Dashboard Types
// =============================================
export interface RetentionDashboard {
  totalDocumentsUnderRetention: number
  activeRetentions: number
  pendingReview: number
  expiringSoon30: number
  expiringSoon7: number
  onHold: number
  archived: number
  disposed: number
  awaitingTrigger: number
  recentActions: RetentionAction[]
  retentionsByPolicy: RetentionPolicySummary[]
  backgroundJobs: BackgroundJob[]
  upcomingExpirations: UpcomingExpiration[]
  expirationTimeline: ExpirationTimeline[]
}

export interface RetentionAction {
  documentId: string
  documentName: string
  policyName: string
  action: string
  timestamp: string
  isSystemAction: boolean
  notes?: string
}

export interface RetentionPolicySummary {
  policyId: string
  policyName: string
  expirationAction: string
  totalDocuments: number
  activeCount: number
  expiredCount: number
  onHoldCount: number
}

export interface BackgroundJob {
  id: string
  jobName: string
  status: string
  startedAt: string
  completedAt?: string
  itemsProcessed: number
  itemsFailed: number
  durationMs?: number
  errorMessage?: string
}

export interface UpcomingExpiration {
  documentId: string
  documentName: string
  policyName: string
  expirationDate: string
  daysRemaining: number
  status: string
}

export interface ExpirationTimeline {
  date: string
  count: number
}
