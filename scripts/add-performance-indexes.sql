-- ============================================================================
-- P0 Enterprise Hardening: Performance Indexes
-- Run this script against the DMS database to add missing FK indexes
-- ============================================================================

-- ============================================================================
-- CRITICAL PRIORITY (frequently queried entities)
-- ============================================================================

-- ApprovalRequests
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequests_DocumentId' AND object_id = OBJECT_ID('ApprovalRequests'))
    CREATE NONCLUSTERED INDEX IX_ApprovalRequests_DocumentId ON ApprovalRequests (DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequests_WorkflowId' AND object_id = OBJECT_ID('ApprovalRequests'))
    CREATE NONCLUSTERED INDEX IX_ApprovalRequests_WorkflowId ON ApprovalRequests (WorkflowId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequests_RequestedBy' AND object_id = OBJECT_ID('ApprovalRequests'))
    CREATE NONCLUSTERED INDEX IX_ApprovalRequests_RequestedBy ON ApprovalRequests (RequestedBy);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalRequests_DocumentId_Status' AND object_id = OBJECT_ID('ApprovalRequests'))
    CREATE NONCLUSTERED INDEX IX_ApprovalRequests_DocumentId_Status ON ApprovalRequests (DocumentId, Status);

-- ApprovalActions
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalActions_RequestId' AND object_id = OBJECT_ID('ApprovalActions'))
    CREATE NONCLUSTERED INDEX IX_ApprovalActions_RequestId ON ApprovalActions (RequestId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalActions_ApproverId' AND object_id = OBJECT_ID('ApprovalActions'))
    CREATE NONCLUSTERED INDEX IX_ApprovalActions_ApproverId ON ApprovalActions (ApproverId);

-- ApprovalWorkflows
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalWorkflows_FolderId' AND object_id = OBJECT_ID('ApprovalWorkflows'))
    CREATE NONCLUSTERED INDEX IX_ApprovalWorkflows_FolderId ON ApprovalWorkflows (FolderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalWorkflows_CreatedBy' AND object_id = OBJECT_ID('ApprovalWorkflows'))
    CREATE NONCLUSTERED INDEX IX_ApprovalWorkflows_CreatedBy ON ApprovalWorkflows (CreatedBy);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalWorkflows_FolderId_TriggerType' AND object_id = OBJECT_ID('ApprovalWorkflows'))
    CREATE NONCLUSTERED INDEX IX_ApprovalWorkflows_FolderId_TriggerType ON ApprovalWorkflows (FolderId, TriggerType);

-- ApprovalWorkflowSteps
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ApprovalWorkflowSteps_WorkflowId' AND object_id = OBJECT_ID('ApprovalWorkflowSteps'))
    CREATE NONCLUSTERED INDEX IX_ApprovalWorkflowSteps_WorkflowId ON ApprovalWorkflowSteps (WorkflowId);

-- DocumentComments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentComments_DocumentId' AND object_id = OBJECT_ID('DocumentComments'))
    CREATE NONCLUSTERED INDEX IX_DocumentComments_DocumentId ON DocumentComments (DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentComments_ParentCommentId' AND object_id = OBJECT_ID('DocumentComments'))
    CREATE NONCLUSTERED INDEX IX_DocumentComments_ParentCommentId ON DocumentComments (ParentCommentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentComments_CreatedBy' AND object_id = OBJECT_ID('DocumentComments'))
    CREATE NONCLUSTERED INDEX IX_DocumentComments_CreatedBy ON DocumentComments (CreatedBy);

-- DocumentAttachments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentAttachments_DocumentId' AND object_id = OBJECT_ID('DocumentAttachments'))
    CREATE NONCLUSTERED INDEX IX_DocumentAttachments_DocumentId ON DocumentAttachments (DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentAttachments_CreatedBy' AND object_id = OBJECT_ID('DocumentAttachments'))
    CREATE NONCLUSTERED INDEX IX_DocumentAttachments_CreatedBy ON DocumentAttachments (CreatedBy);

-- DocumentAnnotations
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentAnnotations_DocumentId' AND object_id = OBJECT_ID('DocumentAnnotations'))
    CREATE NONCLUSTERED INDEX IX_DocumentAnnotations_DocumentId ON DocumentAnnotations (DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentAnnotations_CreatedBy' AND object_id = OBJECT_ID('DocumentAnnotations'))
    CREATE NONCLUSTERED INDEX IX_DocumentAnnotations_CreatedBy ON DocumentAnnotations (CreatedBy);

-- DocumentLinks
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentLinks_SourceDocumentId' AND object_id = OBJECT_ID('DocumentLinks'))
    CREATE NONCLUSTERED INDEX IX_DocumentLinks_SourceDocumentId ON DocumentLinks (SourceDocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentLinks_TargetDocumentId' AND object_id = OBJECT_ID('DocumentLinks'))
    CREATE NONCLUSTERED INDEX IX_DocumentLinks_TargetDocumentId ON DocumentLinks (TargetDocumentId);

-- DocumentWorkingCopies
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentWorkingCopies_DocumentId' AND object_id = OBJECT_ID('DocumentWorkingCopies'))
    CREATE NONCLUSTERED INDEX IX_DocumentWorkingCopies_DocumentId ON DocumentWorkingCopies (DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentWorkingCopies_CheckedOutBy' AND object_id = OBJECT_ID('DocumentWorkingCopies'))
    CREATE NONCLUSTERED INDEX IX_DocumentWorkingCopies_CheckedOutBy ON DocumentWorkingCopies (CheckedOutBy);

-- DocumentPasswords
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentPasswords_DocumentId' AND object_id = OBJECT_ID('DocumentPasswords'))
    CREATE NONCLUSTERED INDEX IX_DocumentPasswords_DocumentId ON DocumentPasswords (DocumentId);

-- ContentTypeFields
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ContentTypeFields_ContentTypeId' AND object_id = OBJECT_ID('ContentTypeFields'))
    CREATE NONCLUSTERED INDEX IX_ContentTypeFields_ContentTypeId ON ContentTypeFields (ContentTypeId);

-- DocumentMetadata
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentMetadata_DocumentId' AND object_id = OBJECT_ID('DocumentMetadata'))
    CREATE NONCLUSTERED INDEX IX_DocumentMetadata_DocumentId ON DocumentMetadata (DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentMetadata_DocumentId_ContentTypeId' AND object_id = OBJECT_ID('DocumentMetadata'))
    CREATE NONCLUSTERED INDEX IX_DocumentMetadata_DocumentId_ContentTypeId ON DocumentMetadata (DocumentId, ContentTypeId);

-- FolderContentTypeAssignments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FolderContentTypeAssignments_FolderId' AND object_id = OBJECT_ID('FolderContentTypeAssignments'))
    CREATE NONCLUSTERED INDEX IX_FolderContentTypeAssignments_FolderId ON FolderContentTypeAssignments (FolderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FolderContentTypeAssignments_ContentTypeId' AND object_id = OBJECT_ID('FolderContentTypeAssignments'))
    CREATE NONCLUSTERED INDEX IX_FolderContentTypeAssignments_ContentTypeId ON FolderContentTypeAssignments (ContentTypeId);

-- CabinetContentTypeAssignments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CabinetContentTypeAssignments_CabinetId' AND object_id = OBJECT_ID('CabinetContentTypeAssignments'))
    CREATE NONCLUSTERED INDEX IX_CabinetContentTypeAssignments_CabinetId ON CabinetContentTypeAssignments (CabinetId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CabinetContentTypeAssignments_ContentTypeId' AND object_id = OBJECT_ID('CabinetContentTypeAssignments'))
    CREATE NONCLUSTERED INDEX IX_CabinetContentTypeAssignments_ContentTypeId ON CabinetContentTypeAssignments (ContentTypeId);

-- ============================================================================
-- MEDIUM PRIORITY
-- ============================================================================

-- DisposalCertificates
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DisposalCertificates_RetentionPolicyId' AND object_id = OBJECT_ID('DisposalCertificates'))
    CREATE NONCLUSTERED INDEX IX_DisposalCertificates_RetentionPolicyId ON DisposalCertificates (RetentionPolicyId);

-- LegalHoldDocuments
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LegalHoldDocuments_LegalHoldId' AND object_id = OBJECT_ID('LegalHoldDocuments'))
    CREATE NONCLUSTERED INDEX IX_LegalHoldDocuments_LegalHoldId ON LegalHoldDocuments (LegalHoldId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LegalHoldDocuments_DocumentId' AND object_id = OBJECT_ID('LegalHoldDocuments'))
    CREATE NONCLUSTERED INDEX IX_LegalHoldDocuments_DocumentId ON LegalHoldDocuments (DocumentId);

-- IntegrityVerificationLogs
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IntegrityVerificationLogs_DocumentId' AND object_id = OBJECT_ID('IntegrityVerificationLogs'))
    CREATE NONCLUSTERED INDEX IX_IntegrityVerificationLogs_DocumentId ON IntegrityVerificationLogs (DocumentId);

-- PreservationMetadata
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PreservationMetadata_DocumentId' AND object_id = OBJECT_ID('PreservationMetadata'))
    CREATE NONCLUSTERED INDEX IX_PreservationMetadata_DocumentId ON PreservationMetadata (DocumentId);

-- FolderTemplateNodes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FolderTemplateNodes_TemplateId' AND object_id = OBJECT_ID('FolderTemplateNodes'))
    CREATE NONCLUSTERED INDEX IX_FolderTemplateNodes_TemplateId ON FolderTemplateNodes (TemplateId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FolderTemplateNodes_ParentNodeId' AND object_id = OBJECT_ID('FolderTemplateNodes'))
    CREATE NONCLUSTERED INDEX IX_FolderTemplateNodes_ParentNodeId ON FolderTemplateNodes (ParentNodeId);

-- FolderTemplateUsage
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FolderTemplateUsage_TemplateId' AND object_id = OBJECT_ID('FolderTemplateUsage'))
    CREATE NONCLUSTERED INDEX IX_FolderTemplateUsage_TemplateId ON FolderTemplateUsage (TemplateId);

-- FilingPlans
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FilingPlans_FolderId' AND object_id = OBJECT_ID('FilingPlans'))
    CREATE NONCLUSTERED INDEX IX_FilingPlans_FolderId ON FilingPlans (FolderId);

-- FolderLinks
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FolderLinks_SourceFolderId' AND object_id = OBJECT_ID('FolderLinks'))
    CREATE NONCLUSTERED INDEX IX_FolderLinks_SourceFolderId ON FolderLinks (SourceFolderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FolderLinks_TargetFolderId' AND object_id = OBJECT_ID('FolderLinks'))
    CREATE NONCLUSTERED INDEX IX_FolderLinks_TargetFolderId ON FolderLinks (TargetFolderId);

-- Patterns
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Patterns_TargetFolderId' AND object_id = OBJECT_ID('Patterns'))
    CREATE NONCLUSTERED INDEX IX_Patterns_TargetFolderId ON Patterns (TargetFolderId);

-- RetentionPolicies
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RetentionPolicies_FolderId' AND object_id = OBJECT_ID('RetentionPolicies'))
    CREATE NONCLUSTERED INDEX IX_RetentionPolicies_FolderId ON RetentionPolicies (FolderId);

-- LookupItems
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LookupItems_LookupId' AND object_id = OBJECT_ID('LookupItems'))
    CREATE NONCLUSTERED INDEX IX_LookupItems_LookupId ON LookupItems (LookupId);

-- UserRoles
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_UserId' AND object_id = OBJECT_ID('UserRoles'))
    CREATE NONCLUSTERED INDEX IX_UserRoles_UserId ON UserRoles (UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_RoleId' AND object_id = OBJECT_ID('UserRoles'))
    CREATE NONCLUSTERED INDEX IX_UserRoles_RoleId ON UserRoles (RoleId);

-- RoleActionPermissions
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RoleActionPermissions_RoleId' AND object_id = OBJECT_ID('RoleActionPermissions'))
    CREATE NONCLUSTERED INDEX IX_RoleActionPermissions_RoleId ON RoleActionPermissions (RoleId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RoleActionPermissions_ActionId' AND object_id = OBJECT_ID('RoleActionPermissions'))
    CREATE NONCLUSTERED INDEX IX_RoleActionPermissions_ActionId ON RoleActionPermissions (ActionId);

-- Cases
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cases_FolderId' AND object_id = OBJECT_ID('Cases'))
    CREATE NONCLUSTERED INDEX IX_Cases_FolderId ON Cases (FolderId);

-- NamingConventions
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NamingConventions_FolderId' AND object_id = OBJECT_ID('NamingConventions'))
    CREATE NONCLUSTERED INDEX IX_NamingConventions_FolderId ON NamingConventions (FolderId);

-- ScanConfigs
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ScanConfigs_TargetFolderId' AND object_id = OBJECT_ID('ScanConfigs'))
    CREATE NONCLUSTERED INDEX IX_ScanConfigs_TargetFolderId ON ScanConfigs (TargetFolderId);

PRINT 'All performance indexes created successfully.';
GO
