-- =============================================
-- DMS-Modern NCAR Enterprise Migration Script
-- Adds new tables and columns for state machine,
-- physical archive, disposal, access review,
-- search indexing, and background jobs
-- =============================================
-- Target: INTALIO-PKA1\SQL2022, Database: DMS
-- Generated: 2026-02-16
-- =============================================

USE [DMS];
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

PRINT '=== Phase 1: ALTER existing tables ==='

-- -----------------------------------------------
-- 1a. Documents → add state tracking + OCR columns
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'PreviousState')
    ALTER TABLE [Documents] ADD [PreviousState] INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'StateChangedAt')
    ALTER TABLE [Documents] ADD [StateChangedAt] DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'StateChangedBy')
    ALTER TABLE [Documents] ADD [StateChangedBy] UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'OcrText')
    ALTER TABLE [Documents] ADD [OcrText] NVARCHAR(MAX) NULL;
GO

-- Add index on State column if not exists
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Documents') AND name = 'IX_Documents_State')
    CREATE INDEX [IX_Documents_State] ON [Documents] ([State]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Documents') AND name = 'IX_Documents_State_RetentionPolicyId')
    CREATE INDEX [IX_Documents_State_RetentionPolicyId] ON [Documents] ([State], [RetentionPolicyId]);
GO

-- -----------------------------------------------
-- 1b. RetentionPolicies → add advanced retention fields
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('RetentionPolicies') AND name = 'RetentionBasis')
    ALTER TABLE [RetentionPolicies] ADD [RetentionBasis] NVARCHAR(100) NOT NULL DEFAULT 'CreationDate';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('RetentionPolicies') AND name = 'SuspendDuringLegalHold')
    ALTER TABLE [RetentionPolicies] ADD [SuspendDuringLegalHold] BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('RetentionPolicies') AND name = 'RecalculateOnClassificationChange')
    ALTER TABLE [RetentionPolicies] ADD [RecalculateOnClassificationChange] BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('RetentionPolicies') AND name = 'DisposalApprovalLevels')
    ALTER TABLE [RetentionPolicies] ADD [DisposalApprovalLevels] INT NOT NULL DEFAULT 1;
GO

-- -----------------------------------------------
-- 1c. Classifications → add governance fields
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Classifications') AND name = 'DefaultRetentionPolicyId')
    ALTER TABLE [Classifications] ADD [DefaultRetentionPolicyId] UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Classifications') AND name = 'DefaultPrivacyLevelId')
    ALTER TABLE [Classifications] ADD [DefaultPrivacyLevelId] UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Classifications') AND name = 'ConfidentialityLevel')
    ALTER TABLE [Classifications] ADD [ConfidentialityLevel] NVARCHAR(50) NULL;
GO

-- -----------------------------------------------
-- 1d. DocumentRetentions → add suspension/trigger fields
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DocumentRetentions') AND name = 'SuspendedDays')
    ALTER TABLE [DocumentRetentions] ADD [SuspendedDays] INT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DocumentRetentions') AND name = 'SuspendedAt')
    ALTER TABLE [DocumentRetentions] ADD [SuspendedAt] DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DocumentRetentions') AND name = 'TriggerEventId')
    ALTER TABLE [DocumentRetentions] ADD [TriggerEventId] UNIQUEIDENTIFIER NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('DocumentRetentions') AND name = 'OriginalExpirationDate')
    ALTER TABLE [DocumentRetentions] ADD [OriginalExpirationDate] DATETIME2 NULL;
GO

PRINT '=== Phase 2: CREATE new tables ==='

-- -----------------------------------------------
-- 2a. StateTransitionRules
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('StateTransitionRules') AND type = 'U')
BEGIN
    CREATE TABLE [StateTransitionRules] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [FromState] INT NOT NULL,
        [ToState] INT NOT NULL,
        [RequiredRole] NVARCHAR(50) NULL,
        [Description] NVARCHAR(500) NULL,
        [RequiresClassification] BIT NOT NULL DEFAULT 0,
        [RequiresRetentionPolicy] BIT NOT NULL DEFAULT 0,
        [RequiresApproval] BIT NOT NULL DEFAULT 0,
        [RequiresIntegrityCheck] BIT NOT NULL DEFAULT 0,
        [MakesImmutable] BIT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_StateTransitionRules] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_StateTransitionRules_FromTo_Active]
        ON [StateTransitionRules] ([FromState], [ToState])
        WHERE [IsActive] = 1;

    PRINT 'Created StateTransitionRules'
END
GO

-- -----------------------------------------------
-- 2b. StateTransitionLogs
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('StateTransitionLogs') AND type = 'U')
BEGIN
    CREATE TABLE [StateTransitionLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [DocumentId] UNIQUEIDENTIFIER NOT NULL,
        [FromState] INT NOT NULL,
        [ToState] INT NOT NULL,
        [TransitionedBy] UNIQUEIDENTIFIER NOT NULL,
        [TransitionedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [Reason] NVARCHAR(1000) NULL,
        [RuleId] UNIQUEIDENTIFIER NULL,
        [IsSystemAction] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [PK_StateTransitionLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StateTransitionLogs_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_StateTransitionLogs_DocumentId] ON [StateTransitionLogs] ([DocumentId]);
    CREATE INDEX [IX_StateTransitionLogs_TransitionedAt] ON [StateTransitionLogs] ([TransitionedAt]);

    PRINT 'Created StateTransitionLogs'
END
GO

-- -----------------------------------------------
-- 2c. RetentionTriggerEvents
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('RetentionTriggerEvents') AND type = 'U')
BEGIN
    CREATE TABLE [RetentionTriggerEvents] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [RetentionPolicyId] UNIQUEIDENTIFIER NOT NULL,
        [TriggerType] INT NOT NULL DEFAULT 0,
        [TriggerFieldName] NVARCHAR(200) NULL,
        [TriggerFieldValue] NVARCHAR(500) NULL,
        [Description] NVARCHAR(500) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_RetentionTriggerEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RetentionTriggerEvents_RetentionPolicies] FOREIGN KEY ([RetentionPolicyId]) REFERENCES [RetentionPolicies]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_RetentionTriggerEvents_RetentionPolicyId] ON [RetentionTriggerEvents] ([RetentionPolicyId]);

    PRINT 'Created RetentionTriggerEvents'
END
GO

-- -----------------------------------------------
-- 2d. RetentionTriggerLogs
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('RetentionTriggerLogs') AND type = 'U')
BEGIN
    CREATE TABLE [RetentionTriggerLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [DocumentId] UNIQUEIDENTIFIER NOT NULL,
        [RetentionPolicyId] UNIQUEIDENTIFIER NOT NULL,
        [TriggerEventId] UNIQUEIDENTIFIER NULL,
        [TriggerType] INT NOT NULL DEFAULT 0,
        [TriggeredAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [TriggeredBy] UNIQUEIDENTIFIER NULL,
        [NewExpirationDate] DATETIME2 NULL,
        [PreviousExpirationDate] DATETIME2 NULL,
        CONSTRAINT [PK_RetentionTriggerLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RetentionTriggerLogs_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RetentionTriggerLogs_RetentionPolicies] FOREIGN KEY ([RetentionPolicyId]) REFERENCES [RetentionPolicies]([Id])
    );

    CREATE INDEX [IX_RetentionTriggerLogs_DocumentId] ON [RetentionTriggerLogs] ([DocumentId]);
    CREATE INDEX [IX_RetentionTriggerLogs_RetentionPolicyId] ON [RetentionTriggerLogs] ([RetentionPolicyId]);

    PRINT 'Created RetentionTriggerLogs'
END
GO

-- -----------------------------------------------
-- 2e. DisposalRequests
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('DisposalRequests') AND type = 'U')
BEGIN
    CREATE TABLE [DisposalRequests] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [RequestNumber] NVARCHAR(50) NOT NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [BatchReference] NVARCHAR(100) NULL,
        [DisposalMethod] NVARCHAR(50) NOT NULL DEFAULT 'HardDelete',
        [Reason] NVARCHAR(1000) NULL,
        [LegalBasis] NVARCHAR(500) NULL,
        [RequiredApprovalLevels] INT NOT NULL DEFAULT 1,
        [CurrentApprovalLevel] INT NOT NULL DEFAULT 0,
        [RequestedBy] UNIQUEIDENTIFIER NOT NULL,
        [RequestedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ExecutedAt] DATETIME2 NULL,
        [ExecutedBy] UNIQUEIDENTIFIER NULL,
        [CertificateId] UNIQUEIDENTIFIER NULL,
        [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] UNIQUEIDENTIFIER NULL,
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_DisposalRequests] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_DisposalRequests_RequestNumber] ON [DisposalRequests] ([RequestNumber]);
    CREATE INDEX [IX_DisposalRequests_Status] ON [DisposalRequests] ([Status]);
    CREATE INDEX [IX_DisposalRequests_RequestedAt] ON [DisposalRequests] ([RequestedAt]);

    PRINT 'Created DisposalRequests'
END
GO

-- -----------------------------------------------
-- 2f. DisposalRequestDocuments
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('DisposalRequestDocuments') AND type = 'U')
BEGIN
    CREATE TABLE [DisposalRequestDocuments] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [DisposalRequestId] UNIQUEIDENTIFIER NOT NULL,
        [DocumentId] UNIQUEIDENTIFIER NOT NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [CertificateId] UNIQUEIDENTIFIER NULL,
        [ErrorMessage] NVARCHAR(500) NULL,
        CONSTRAINT [PK_DisposalRequestDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DisposalRequestDocuments_DisposalRequests] FOREIGN KEY ([DisposalRequestId]) REFERENCES [DisposalRequests]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DisposalRequestDocuments_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id])
    );

    CREATE INDEX [IX_DisposalRequestDocuments_DisposalRequestId] ON [DisposalRequestDocuments] ([DisposalRequestId]);
    CREATE INDEX [IX_DisposalRequestDocuments_DocumentId] ON [DisposalRequestDocuments] ([DocumentId]);

    PRINT 'Created DisposalRequestDocuments'
END
GO

-- -----------------------------------------------
-- 2g. DisposalApprovals
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('DisposalApprovals') AND type = 'U')
BEGIN
    CREATE TABLE [DisposalApprovals] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [DisposalRequestId] UNIQUEIDENTIFIER NOT NULL,
        [ApprovalLevel] INT NOT NULL,
        [ApproverId] UNIQUEIDENTIFIER NOT NULL,
        [Decision] INT NOT NULL DEFAULT 0,
        [Comments] NVARCHAR(1000) NULL,
        [DecisionAt] DATETIME2 NULL,
        CONSTRAINT [PK_DisposalApprovals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DisposalApprovals_DisposalRequests] FOREIGN KEY ([DisposalRequestId]) REFERENCES [DisposalRequests]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_DisposalApprovals_DisposalRequestId] ON [DisposalApprovals] ([DisposalRequestId]);
    CREATE INDEX [IX_DisposalApprovals_RequestLevel] ON [DisposalApprovals] ([DisposalRequestId], [ApprovalLevel]);

    PRINT 'Created DisposalApprovals'
END
GO

-- -----------------------------------------------
-- 2h. PhysicalLocations
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('PhysicalLocations') AND type = 'U')
BEGIN
    CREATE TABLE [PhysicalLocations] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [Name] NVARCHAR(200) NOT NULL,
        [NameAr] NVARCHAR(200) NULL,
        [Code] NVARCHAR(100) NOT NULL,
        [LocationType] INT NOT NULL DEFAULT 0,
        [Path] NVARCHAR(1000) NULL,
        [Level] INT NOT NULL DEFAULT 0,
        [ParentId] UNIQUEIDENTIFIER NULL,
        [Capacity] INT NOT NULL DEFAULT 0,
        [CurrentCount] INT NOT NULL DEFAULT 0,
        [EnvironmentalConditions] NVARCHAR(500) NULL,
        [Coordinates] NVARCHAR(100) NULL,
        [SecurityLevel] NVARCHAR(50) NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] UNIQUEIDENTIFIER NULL,
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_PhysicalLocations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhysicalLocations_Parent] FOREIGN KEY ([ParentId]) REFERENCES [PhysicalLocations]([Id])
    );

    CREATE UNIQUE INDEX [IX_PhysicalLocations_Code] ON [PhysicalLocations] ([Code]);
    CREATE INDEX [IX_PhysicalLocations_ParentId] ON [PhysicalLocations] ([ParentId]);
    CREATE INDEX [IX_PhysicalLocations_LocationType] ON [PhysicalLocations] ([LocationType]);

    PRINT 'Created PhysicalLocations'
END
GO

-- -----------------------------------------------
-- 2i. PhysicalItems
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('PhysicalItems') AND type = 'U')
BEGIN
    CREATE TABLE [PhysicalItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [Barcode] NVARCHAR(100) NOT NULL,
        [BarcodeFormat] NVARCHAR(20) NOT NULL DEFAULT 'Code128',
        [Title] NVARCHAR(500) NOT NULL,
        [TitleAr] NVARCHAR(500) NULL,
        [Description] NVARCHAR(2000) NULL,
        [ItemType] INT NOT NULL DEFAULT 0,
        [LocationId] UNIQUEIDENTIFIER NOT NULL,
        [DigitalDocumentId] UNIQUEIDENTIFIER NULL,
        [ClassificationId] UNIQUEIDENTIFIER NULL,
        [RetentionPolicyId] UNIQUEIDENTIFIER NULL,
        [OwnerStructureId] UNIQUEIDENTIFIER NULL,
        [Condition] INT NOT NULL DEFAULT 0,
        [ItemDate] DATETIME2 NULL,
        [DateRangeStart] DATETIME2 NULL,
        [DateRangeEnd] DATETIME2 NULL,
        [PageCount] INT NULL,
        [Dimensions] NVARCHAR(100) NULL,
        [CirculationStatus] INT NOT NULL DEFAULT 0,
        [IsOnLegalHold] BIT NOT NULL DEFAULT 0,
        [LegalHoldId] UNIQUEIDENTIFIER NULL,
        [LastConditionAssessment] DATETIME2 NULL,
        [ConditionNotes] NVARCHAR(1000) NULL,
        [CustomMetadata] NVARCHAR(MAX) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] UNIQUEIDENTIFIER NULL,
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_PhysicalItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhysicalItems_Locations] FOREIGN KEY ([LocationId]) REFERENCES [PhysicalLocations]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhysicalItems_Documents] FOREIGN KEY ([DigitalDocumentId]) REFERENCES [Documents]([Id]) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX [IX_PhysicalItems_Barcode] ON [PhysicalItems] ([Barcode]);
    CREATE INDEX [IX_PhysicalItems_LocationId] ON [PhysicalItems] ([LocationId]);
    CREATE INDEX [IX_PhysicalItems_DigitalDocumentId] ON [PhysicalItems] ([DigitalDocumentId]);
    CREATE INDEX [IX_PhysicalItems_CirculationStatus] ON [PhysicalItems] ([CirculationStatus]);

    PRINT 'Created PhysicalItems'
END
GO

-- -----------------------------------------------
-- 2j. AccessionRequests
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('AccessionRequests') AND type = 'U')
BEGIN
    CREATE TABLE [AccessionRequests] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [AccessionNumber] NVARCHAR(50) NOT NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [SourceStructureId] UNIQUEIDENTIFIER NULL,
        [TargetLocationId] UNIQUEIDENTIFIER NULL,
        [ItemCount] INT NOT NULL DEFAULT 0,
        [RecordsDateFrom] DATETIME2 NULL,
        [RecordsDateTo] DATETIME2 NULL,
        [RequestedTransferDate] DATETIME2 NULL,
        [ActualTransferDate] DATETIME2 NULL,
        [ReviewedBy] UNIQUEIDENTIFIER NULL,
        [ReviewedAt] DATETIME2 NULL,
        [ReviewNotes] NVARCHAR(1000) NULL,
        [AcceptedBy] UNIQUEIDENTIFIER NULL,
        [AcceptedAt] DATETIME2 NULL,
        [AcceptanceNotes] NVARCHAR(1000) NULL,
        [RejectionReason] NVARCHAR(1000) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] UNIQUEIDENTIFIER NULL,
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_AccessionRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccessionRequests_TargetLocation] FOREIGN KEY ([TargetLocationId]) REFERENCES [PhysicalLocations]([Id])
    );

    CREATE UNIQUE INDEX [IX_AccessionRequests_AccessionNumber] ON [AccessionRequests] ([AccessionNumber]);
    CREATE INDEX [IX_AccessionRequests_Status] ON [AccessionRequests] ([Status]);

    PRINT 'Created AccessionRequests'
END
GO

-- -----------------------------------------------
-- 2k. AccessionRequestItems
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('AccessionRequestItems') AND type = 'U')
BEGIN
    CREATE TABLE [AccessionRequestItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [AccessionRequestId] UNIQUEIDENTIFIER NOT NULL,
        [PhysicalItemId] UNIQUEIDENTIFIER NULL,
        [Title] NVARCHAR(500) NOT NULL,
        [ItemType] INT NOT NULL DEFAULT 0,
        [ClassificationId] UNIQUEIDENTIFIER NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        [Notes] NVARCHAR(1000) NULL,
        CONSTRAINT [PK_AccessionRequestItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccessionRequestItems_AccessionRequests] FOREIGN KEY ([AccessionRequestId]) REFERENCES [AccessionRequests]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_AccessionRequestItems_AccessionRequestId] ON [AccessionRequestItems] ([AccessionRequestId]);

    PRINT 'Created AccessionRequestItems'
END
GO

-- -----------------------------------------------
-- 2l. CirculationRecords
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('CirculationRecords') AND type = 'U')
BEGIN
    CREATE TABLE [CirculationRecords] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [PhysicalItemId] UNIQUEIDENTIFIER NOT NULL,
        [BorrowerId] UNIQUEIDENTIFIER NOT NULL,
        [BorrowerStructureId] UNIQUEIDENTIFIER NULL,
        [Purpose] NVARCHAR(500) NULL,
        [CheckedOutAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [CheckedOutBy] UNIQUEIDENTIFIER NOT NULL,
        [DueDate] DATETIME2 NOT NULL,
        [ReturnedAt] DATETIME2 NULL,
        [ReturnedTo] UNIQUEIDENTIFIER NULL,
        [RenewalCount] INT NOT NULL DEFAULT 0,
        [MaxRenewals] INT NOT NULL DEFAULT 2,
        [ConditionAtCheckout] INT NOT NULL DEFAULT 0,
        [ConditionAtReturn] INT NULL,
        [ConditionNotes] NVARCHAR(1000) NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] UNIQUEIDENTIFIER NULL,
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_CirculationRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CirculationRecords_PhysicalItems] FOREIGN KEY ([PhysicalItemId]) REFERENCES [PhysicalItems]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_CirculationRecords_PhysicalItemId] ON [CirculationRecords] ([PhysicalItemId]);
    CREATE INDEX [IX_CirculationRecords_BorrowerId] ON [CirculationRecords] ([BorrowerId]);
    CREATE INDEX [IX_CirculationRecords_Status] ON [CirculationRecords] ([Status]);
    CREATE INDEX [IX_CirculationRecords_DueDate] ON [CirculationRecords] ([DueDate]);

    PRINT 'Created CirculationRecords'
END
GO

-- -----------------------------------------------
-- 2m. CustodyTransfers
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('CustodyTransfers') AND type = 'U')
BEGIN
    CREATE TABLE [CustodyTransfers] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [PhysicalItemId] UNIQUEIDENTIFIER NOT NULL,
        [FromUserId] UNIQUEIDENTIFIER NULL,
        [ToUserId] UNIQUEIDENTIFIER NULL,
        [FromLocationId] UNIQUEIDENTIFIER NULL,
        [ToLocationId] UNIQUEIDENTIFIER NULL,
        [TransferType] INT NOT NULL DEFAULT 0,
        [ReferenceId] UNIQUEIDENTIFIER NULL,
        [ReferenceType] NVARCHAR(100) NULL,
        [ConditionAtTransfer] INT NOT NULL DEFAULT 0,
        [IsAcknowledged] BIT NOT NULL DEFAULT 0,
        [AcknowledgedAt] DATETIME2 NULL,
        [TransferredAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [TransferredBy] UNIQUEIDENTIFIER NOT NULL,
        [EntryHash] NVARCHAR(128) NULL,
        [PreviousEntryHash] NVARCHAR(128) NULL,
        CONSTRAINT [PK_CustodyTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustodyTransfers_PhysicalItems] FOREIGN KEY ([PhysicalItemId]) REFERENCES [PhysicalItems]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_CustodyTransfers_PhysicalItemId] ON [CustodyTransfers] ([PhysicalItemId]);
    CREATE INDEX [IX_CustodyTransfers_TransferredAt] ON [CustodyTransfers] ([TransferredAt]);

    PRINT 'Created CustodyTransfers'
END
GO

-- -----------------------------------------------
-- 2n. EncryptionKeyStore
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('EncryptionKeyStore') AND type = 'U')
BEGIN
    CREATE TABLE [EncryptionKeyStore] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [DocumentId] UNIQUEIDENTIFIER NOT NULL,
        [KeyVersion] INT NOT NULL DEFAULT 1,
        [WrappedKey] NVARCHAR(1024) NOT NULL,
        [KeyAlgorithm] NVARCHAR(50) NOT NULL DEFAULT 'AES-256-CBC',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_EncryptionKeyStore] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EncryptionKeyStore_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_EncryptionKeyStore_DocumentId_KeyVersion] ON [EncryptionKeyStore] ([DocumentId], [KeyVersion]);

    PRINT 'Created EncryptionKeyStore'
END
GO

-- -----------------------------------------------
-- 2o. AccessReviewCampaigns
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('AccessReviewCampaigns') AND type = 'U')
BEGIN
    CREATE TABLE [AccessReviewCampaigns] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [Status] INT NOT NULL DEFAULT 0,
        [StartDate] DATETIME2 NOT NULL,
        [DueDate] DATETIME2 NOT NULL,
        [ReviewerId] UNIQUEIDENTIFIER NULL,
        [TotalEntries] INT NOT NULL DEFAULT 0,
        [CompletedEntries] INT NOT NULL DEFAULT 0,
        [CreatedBy] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy] UNIQUEIDENTIFIER NULL,
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_AccessReviewCampaigns] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_AccessReviewCampaigns_Status] ON [AccessReviewCampaigns] ([Status]);

    PRINT 'Created AccessReviewCampaigns'
END
GO

-- -----------------------------------------------
-- 2p. AccessReviewEntries
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('AccessReviewEntries') AND type = 'U')
BEGIN
    CREATE TABLE [AccessReviewEntries] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [CampaignId] UNIQUEIDENTIFIER NOT NULL,
        [UserId] UNIQUEIDENTIFIER NOT NULL,
        [NodeType] NVARCHAR(50) NULL,
        [NodeId] UNIQUEIDENTIFIER NOT NULL,
        [PermissionLevel] NVARCHAR(50) NULL,
        [PermissionSource] NVARCHAR(200) NULL,
        [Decision] INT NOT NULL DEFAULT 0,
        [Comments] NVARCHAR(500) NULL,
        [DecidedBy] UNIQUEIDENTIFIER NULL,
        [DecidedAt] DATETIME2 NULL,
        CONSTRAINT [PK_AccessReviewEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccessReviewEntries_Campaigns] FOREIGN KEY ([CampaignId]) REFERENCES [AccessReviewCampaigns]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_AccessReviewEntries_CampaignId] ON [AccessReviewEntries] ([CampaignId]);
    CREATE INDEX [IX_AccessReviewEntries_UserId] ON [AccessReviewEntries] ([UserId]);
    CREATE INDEX [IX_AccessReviewEntries_Decision] ON [AccessReviewEntries] ([Decision]);

    PRINT 'Created AccessReviewEntries'
END
GO

-- -----------------------------------------------
-- 2q. SearchIndexQueue
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('SearchIndexQueue') AND type = 'U')
BEGIN
    CREATE TABLE [SearchIndexQueue] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [EntityType] NVARCHAR(50) NOT NULL,
        [EntityId] UNIQUEIDENTIFIER NOT NULL,
        [Operation] INT NOT NULL DEFAULT 0,
        [QueuedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [ProcessedAt] DATETIME2 NULL,
        [RetryCount] INT NOT NULL DEFAULT 0,
        [ErrorMessage] NVARCHAR(2000) NULL,
        CONSTRAINT [PK_SearchIndexQueue] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_SearchIndexQueue_Unprocessed] ON [SearchIndexQueue] ([QueuedAt]) WHERE [ProcessedAt] IS NULL;
    CREATE INDEX [IX_SearchIndexQueue_Entity] ON [SearchIndexQueue] ([EntityType], [EntityId]);

    PRINT 'Created SearchIndexQueue'
END
GO

-- -----------------------------------------------
-- 2r. BackgroundJobExecutions
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('BackgroundJobExecutions') AND type = 'U')
BEGIN
    CREATE TABLE [BackgroundJobExecutions] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [JobName] NVARCHAR(100) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Running',
        [StartedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        [CompletedAt] DATETIME2 NULL,
        [ItemsProcessed] INT NOT NULL DEFAULT 0,
        [ItemsFailed] INT NOT NULL DEFAULT 0,
        [ErrorMessage] NVARCHAR(4000) NULL,
        [DurationMs] BIGINT NULL,
        CONSTRAINT [PK_BackgroundJobExecutions] PRIMARY KEY ([Id])
    );

    CREATE INDEX [IX_BackgroundJobExecutions_JobName] ON [BackgroundJobExecutions] ([JobName]);
    CREATE INDEX [IX_BackgroundJobExecutions_StartedAt] ON [BackgroundJobExecutions] ([StartedAt]);

    PRINT 'Created BackgroundJobExecutions'
END
GO

PRINT '=== Phase 3: INSERT seed data ==='

-- -----------------------------------------------
-- 3. Seed StateTransitionRules (7 rules)
-- DocumentState enum: Draft=0, Active=1, Record=2, Archived=3, Disposed=4, OnHold=5, PendingDisposal=6, Quarantined=7
-- -----------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [StateTransitionRules])
BEGIN
    INSERT INTO [StateTransitionRules] ([Id], [FromState], [ToState], [RequiredRole], [Description], [RequiresClassification], [RequiresRetentionPolicy], [RequiresApproval], [MakesImmutable], [IsActive])
    VALUES
        (NEWID(), 0, 1, NULL,         'Draft to Active - Document published',                0, 0, 0, 0, 1),
        (NEWID(), 1, 2, 'RecordsManager', 'Active to Record - Declared as official record', 1, 1, 0, 1, 1),
        (NEWID(), 2, 1, 'RecordsManager', 'Record to Active - Undeclare record',            0, 0, 1, 0, 1),
        (NEWID(), 2, 3, 'RecordsManager', 'Record to Archived - Archive record',            0, 1, 0, 1, 1),
        (NEWID(), 3, 6, 'RecordsManager', 'Archived to PendingDisposal - Begin disposal',   0, 0, 1, 0, 1),
        (NEWID(), 6, 4, 'Admin',          'PendingDisposal to Disposed - Execute disposal',  0, 0, 1, 0, 1),
        (NEWID(), 6, 1, 'RecordsManager', 'PendingDisposal to Active - Cancel disposal',    0, 0, 0, 0, 1);

    PRINT 'Seeded 7 state transition rules'
END
GO

COMMIT TRANSACTION;
PRINT '=== Migration complete ==='
GO
