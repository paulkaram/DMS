-- =============================================
-- DMS Database Cleanup Script
-- Removes ALL transactional data while preserving:
--   - Users, Roles, UserRoles
--   - Organization Structures & Members
--   - Reference data (PrivacyLevels, Classifications, Importances, DocumentTypes, ContentTypes, FileTypes, Lookups)
--   - RetentionPolicies (definitions only)
--   - StateTransitionRules (seed data)
--   - System config (PermissionLevelDefinitions, SystemActions, RoleActionPermissions)
--   - Admin config (NamingConventions, ScanConfigs, SearchConfigs, ExportConfigs, ServiceEndpoints, OrganizationTemplates, Patterns)
-- =============================================
-- Target: INTALIO-PKA1\SQL2022, Database: DMS
-- =============================================

USE [DMS];
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

PRINT '=== Starting DMS Database Cleanup ==='
PRINT ''

-- -----------------------------------------------
-- Phase 1: NCAR / Enterprise tables (leaf tables first)
-- -----------------------------------------------
PRINT '--- Phase 1: NCAR & Enterprise tables ---'

DELETE FROM [AccessReviewEntries];
PRINT 'Cleared AccessReviewEntries'

DELETE FROM [AccessReviewCampaigns];
PRINT 'Cleared AccessReviewCampaigns'

DELETE FROM [BackgroundJobExecutions];
PRINT 'Cleared BackgroundJobExecutions'

DELETE FROM [SearchIndexQueue];
PRINT 'Cleared SearchIndexQueue'

DELETE FROM [EncryptionKeyStore];
PRINT 'Cleared EncryptionKeyStore'

DELETE FROM [CustodyTransfers];
PRINT 'Cleared CustodyTransfers'

DELETE FROM [CirculationRecords];
PRINT 'Cleared CirculationRecords'

DELETE FROM [AccessionRequestItems];
PRINT 'Cleared AccessionRequestItems'

DELETE FROM [AccessionRequests];
PRINT 'Cleared AccessionRequests'

DELETE FROM [PhysicalItems];
PRINT 'Cleared PhysicalItems'

DELETE FROM [PhysicalLocations];
PRINT 'Cleared PhysicalLocations'

DELETE FROM [DisposalApprovals];
PRINT 'Cleared DisposalApprovals'

DELETE FROM [DisposalRequestDocuments];
PRINT 'Cleared DisposalRequestDocuments'

DELETE FROM [DisposalRequests];
PRINT 'Cleared DisposalRequests'

DELETE FROM [DisposalCertificates];
PRINT 'Cleared DisposalCertificates'

DELETE FROM [IntegrityVerificationLogs];
PRINT 'Cleared IntegrityVerificationLogs'

DELETE FROM [PreservationMetadata];
PRINT 'Cleared PreservationMetadata'

DELETE FROM [StateTransitionLogs];
PRINT 'Cleared StateTransitionLogs'

DELETE FROM [RetentionTriggerLogs];
PRINT 'Cleared RetentionTriggerLogs'

DELETE FROM [RetentionTriggerEvents];
PRINT 'Cleared RetentionTriggerEvents'

PRINT ''

-- -----------------------------------------------
-- Phase 2: Retention & Compliance
-- -----------------------------------------------
PRINT '--- Phase 2: Retention & Compliance ---'

DELETE FROM [DocumentRetentions];
PRINT 'Cleared DocumentRetentions'

DELETE FROM [LegalHoldDocuments];
PRINT 'Cleared LegalHoldDocuments'

DELETE FROM [LegalHolds];
PRINT 'Cleared LegalHolds'

PRINT ''

-- -----------------------------------------------
-- Phase 3: Approval Workflows
-- -----------------------------------------------
PRINT '--- Phase 3: Approval Workflows ---'

DELETE FROM [ApprovalActions];
PRINT 'Cleared ApprovalActions'

DELETE FROM [WorkflowStatuses];
PRINT 'Cleared WorkflowStatuses'

DELETE FROM [ApprovalRequests];
PRINT 'Cleared ApprovalRequests'

DELETE FROM [ApprovalWorkflowSteps];
PRINT 'Cleared ApprovalWorkflowSteps'

DELETE FROM [ApprovalWorkflows];
PRINT 'Cleared ApprovalWorkflows'

PRINT ''

-- -----------------------------------------------
-- Phase 4: Document features (child tables)
-- -----------------------------------------------
PRINT '--- Phase 4: Document features ---'

DELETE FROM [DocumentAnnotations];
PRINT 'Cleared DocumentAnnotations'

DELETE FROM [SavedSignatures];
PRINT 'Cleared SavedSignatures'

DELETE FROM [DocumentWorkingCopies];
PRINT 'Cleared DocumentWorkingCopies'

DELETE FROM [DocumentShortcuts];
PRINT 'Cleared DocumentShortcuts'

DELETE FROM [DocumentShares];
PRINT 'Cleared DocumentShares'

DELETE FROM [DocumentPasswords];
PRINT 'Cleared DocumentPasswords'

DELETE FROM [DocumentLinks];
PRINT 'Cleared DocumentLinks'

DELETE FROM [DocumentAttachments];
PRINT 'Cleared DocumentAttachments'

DELETE FROM [DocumentComments];
PRINT 'Cleared DocumentComments'

DELETE FROM [DocumentVersionMetadata];
PRINT 'Cleared DocumentVersionMetadata'

DELETE FROM [DocumentMetadata];
PRINT 'Cleared DocumentMetadata'

PRINT ''

-- -----------------------------------------------
-- Phase 5: Document Versions, then Documents
-- -----------------------------------------------
PRINT '--- Phase 5: Documents ---'

-- Break circular FK: Documents.CurrentVersionId -> DocumentVersions
UPDATE [Documents] SET [CurrentVersionId] = NULL;
PRINT 'Nullified Documents.CurrentVersionId (break circular FK)'

DELETE FROM [DocumentVersions];
PRINT 'Cleared DocumentVersions'

DELETE FROM [Documents];
PRINT 'Cleared Documents'

PRINT ''

-- -----------------------------------------------
-- Phase 6: Folder & Cabinet features
-- -----------------------------------------------
PRINT '--- Phase 6: Folders & Cabinets ---'

DELETE FROM [FolderContentTypeAssignments];
PRINT 'Cleared FolderContentTypeAssignments'

DELETE FROM [CabinetContentTypeAssignments];
PRINT 'Cleared CabinetContentTypeAssignments'

DELETE FROM [FolderLinks];
PRINT 'Cleared FolderLinks'

DELETE FROM [FolderTemplateUsage];
PRINT 'Cleared FolderTemplateUsage'

DELETE FROM [FolderTemplateNodes];
PRINT 'Cleared FolderTemplateNodes'

DELETE FROM [FolderTemplates];
PRINT 'Cleared FolderTemplates'

DELETE FROM [ContentTypeFields];
PRINT 'Cleared ContentTypeFields'

DELETE FROM [ContentTypeDefinitions];
PRINT 'Cleared ContentTypeDefinitions'

PRINT ''

-- -----------------------------------------------
-- Phase 7: Permissions & misc user data
-- -----------------------------------------------
PRINT '--- Phase 7: Permissions & user data ---'

DELETE FROM [PermissionDelegations];
PRINT 'Cleared PermissionDelegations'

DELETE FROM [Delegations];
PRINT 'Cleared Delegations'

DELETE FROM [EffectivePermissions];
PRINT 'Cleared EffectivePermissions'

DELETE FROM [Permissions];
PRINT 'Cleared Permissions'

DELETE FROM [Favorites];
PRINT 'Cleared Favorites'

DELETE FROM [RecycleBin];
PRINT 'Cleared RecycleBin'

DELETE FROM [Bookmarks];
PRINT 'Cleared Bookmarks'

DELETE FROM [Cases];
PRINT 'Cleared Cases'

DELETE FROM [FilingPlans];
PRINT 'Cleared FilingPlans'

DELETE FROM [Vacations];
PRINT 'Cleared Vacations'

DELETE FROM [PermissionAuditLog];
PRINT 'Cleared PermissionAuditLog'

DELETE FROM [ActivityLogs];
PRINT 'Cleared ActivityLogs'

PRINT ''

-- -----------------------------------------------
-- Phase 8: Folders, then Cabinets
-- -----------------------------------------------
PRINT '--- Phase 8: Structure (Folders & Cabinets) ---'

DELETE FROM [Folders];
PRINT 'Cleared Folders'

DELETE FROM [Cabinets];
PRINT 'Cleared Cabinets'

PRINT ''

-- -----------------------------------------------
-- Phase 9: Reset RetentionPolicies new columns to defaults (keep policies)
-- -----------------------------------------------
PRINT '--- Phase 9: Reset retention policy counters ---'

-- Keep retention policies but clear any document-specific state
-- (RetentionPolicies rows are preserved as reference data)

PRINT ''

PRINT '=== DMS Database Cleanup Complete ==='
PRINT ''
PRINT 'Preserved data:'
PRINT '  - Users, Roles, UserRoles'
PRINT '  - Structures, StructureMembers'
PRINT '  - PrivacyLevels, Classifications, Importances'
PRINT '  - DocumentTypes, ContentTypes, FileTypes'
PRINT '  - Lookups, LookupItems'
PRINT '  - RetentionPolicies'
PRINT '  - StateTransitionRules'
PRINT '  - SystemActions, PermissionLevelDefinitions, RoleActionPermissions'
PRINT '  - NamingConventions, ScanConfigs, SearchConfigs, etc.'

COMMIT TRANSACTION;
GO

-- -----------------------------------------------
-- Clean the Audit database too
-- -----------------------------------------------
USE [DMS_Audit];
GO

BEGIN TRANSACTION;

PRINT ''
PRINT '=== Cleaning DMS_Audit Database ==='

IF OBJECT_ID('ActivityLogs', 'U') IS NOT NULL
BEGIN
    DELETE FROM [ActivityLogs];
    PRINT 'Cleared ActivityLogs'
END

IF OBJECT_ID('PermissionAuditLogs', 'U') IS NOT NULL
BEGIN
    DELETE FROM [PermissionAuditLogs];
    PRINT 'Cleared PermissionAuditLogs'
END

PRINT '=== DMS_Audit Cleanup Complete ==='

COMMIT TRANSACTION;
GO
