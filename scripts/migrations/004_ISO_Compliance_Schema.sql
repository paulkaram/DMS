-- ============================================================================
-- ISO COMPLIANCE SCHEMA MIGRATION
-- Version: 004
-- Standards: ISO 15489, ISO 27001, ISO 14721 (OAIS), ISO 23081
-- ============================================================================

-- ============================================================================
-- PART 1: Document Table Extensions
-- ============================================================================

-- ISO 27001: Integrity verification fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'IntegrityHash')
BEGIN
    ALTER TABLE Documents ADD IntegrityHash NVARCHAR(128) NULL;
    PRINT 'Added Documents.IntegrityHash';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'HashAlgorithm')
BEGIN
    ALTER TABLE Documents ADD HashAlgorithm NVARCHAR(20) NULL DEFAULT 'SHA256';
    PRINT 'Added Documents.HashAlgorithm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'IntegrityVerifiedAt')
BEGIN
    ALTER TABLE Documents ADD IntegrityVerifiedAt DATETIME2 NULL;
    PRINT 'Added Documents.IntegrityVerifiedAt';
END

-- ISO 15489: Retention and legal hold fields
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'RetentionPolicyId')
BEGIN
    ALTER TABLE Documents ADD RetentionPolicyId UNIQUEIDENTIFIER NULL;
    PRINT 'Added Documents.RetentionPolicyId';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'IsOnLegalHold')
BEGIN
    ALTER TABLE Documents ADD IsOnLegalHold BIT NOT NULL DEFAULT 0;
    PRINT 'Added Documents.IsOnLegalHold';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'LegalHoldId')
BEGIN
    ALTER TABLE Documents ADD LegalHoldId UNIQUEIDENTIFIER NULL;
    PRINT 'Added Documents.LegalHoldId';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'LegalHoldAppliedAt')
BEGIN
    ALTER TABLE Documents ADD LegalHoldAppliedAt DATETIME2 NULL;
    PRINT 'Added Documents.LegalHoldAppliedAt';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'LegalHoldAppliedBy')
BEGIN
    ALTER TABLE Documents ADD LegalHoldAppliedBy UNIQUEIDENTIFIER NULL;
    PRINT 'Added Documents.LegalHoldAppliedBy';
END

-- ISO 15489: Content categorization (original vs derivative)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'IsOriginalRecord')
BEGIN
    ALTER TABLE Documents ADD IsOriginalRecord BIT NOT NULL DEFAULT 1;
    PRINT 'Added Documents.IsOriginalRecord';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'SourceDocumentId')
BEGIN
    ALTER TABLE Documents ADD SourceDocumentId UNIQUEIDENTIFIER NULL;
    PRINT 'Added Documents.SourceDocumentId';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'ContentCategory')
BEGIN
    ALTER TABLE Documents ADD ContentCategory NVARCHAR(50) NOT NULL DEFAULT 'Original';
    PRINT 'Added Documents.ContentCategory';
END

-- ============================================================================
-- PART 2: DocumentVersions Table Extensions
-- ============================================================================

-- ISO 27001: Integrity verification fields per version
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'IntegrityHash')
BEGIN
    ALTER TABLE DocumentVersions ADD IntegrityHash NVARCHAR(128) NULL;
    PRINT 'Added DocumentVersions.IntegrityHash';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'HashAlgorithm')
BEGIN
    ALTER TABLE DocumentVersions ADD HashAlgorithm NVARCHAR(20) NULL DEFAULT 'SHA256';
    PRINT 'Added DocumentVersions.HashAlgorithm';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'IntegrityVerifiedAt')
BEGIN
    ALTER TABLE DocumentVersions ADD IntegrityVerifiedAt DATETIME2 NULL;
    PRINT 'Added DocumentVersions.IntegrityVerifiedAt';
END

-- ISO 23081: Additional metadata per version
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'ContentType')
BEGIN
    ALTER TABLE DocumentVersions ADD ContentType NVARCHAR(255) NULL;
    PRINT 'Added DocumentVersions.ContentType';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'OriginalFileName')
BEGIN
    ALTER TABLE DocumentVersions ADD OriginalFileName NVARCHAR(500) NULL;
    PRINT 'Added DocumentVersions.OriginalFileName';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'IsOriginalRecord')
BEGIN
    ALTER TABLE DocumentVersions ADD IsOriginalRecord BIT NOT NULL DEFAULT 1;
    PRINT 'Added DocumentVersions.IsOriginalRecord';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'ContentCategory')
BEGIN
    ALTER TABLE DocumentVersions ADD ContentCategory NVARCHAR(50) NOT NULL DEFAULT 'Original';
    PRINT 'Added DocumentVersions.ContentCategory';
END

-- ============================================================================
-- PART 3: Legal Hold Tables (ISO 15489)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('LegalHolds') AND type = 'U')
BEGIN
    CREATE TABLE LegalHolds (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        HoldNumber NVARCHAR(50) NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        CaseReference NVARCHAR(255) NULL,
        RequestedBy NVARCHAR(255) NULL,
        RequestedAt DATETIME2 NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Active',
        EffectiveFrom DATETIME2 NOT NULL,
        EffectiveUntil DATETIME2 NULL,
        AppliedBy UNIQUEIDENTIFIER NOT NULL,
        AppliedAt DATETIME2 NOT NULL,
        ReleasedBy UNIQUEIDENTIFIER NULL,
        ReleasedAt DATETIME2 NULL,
        ReleaseReason NVARCHAR(MAX) NULL,
        Notes NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2 NULL
    );

    CREATE INDEX IX_LegalHolds_Status ON LegalHolds(Status);
    CREATE INDEX IX_LegalHolds_HoldNumber ON LegalHolds(HoldNumber);
    PRINT 'Created LegalHolds table';
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('LegalHoldDocuments') AND type = 'U')
BEGIN
    CREATE TABLE LegalHoldDocuments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        LegalHoldId UNIQUEIDENTIFIER NOT NULL,
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        AddedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        AddedBy UNIQUEIDENTIFIER NOT NULL,
        ReleasedAt DATETIME2 NULL,
        ReleasedBy UNIQUEIDENTIFIER NULL,
        Notes NVARCHAR(MAX) NULL,

        CONSTRAINT FK_LegalHoldDocuments_LegalHold FOREIGN KEY (LegalHoldId) REFERENCES LegalHolds(Id),
        CONSTRAINT FK_LegalHoldDocuments_Document FOREIGN KEY (DocumentId) REFERENCES Documents(Id)
    );

    CREATE INDEX IX_LegalHoldDocuments_LegalHoldId ON LegalHoldDocuments(LegalHoldId);
    CREATE INDEX IX_LegalHoldDocuments_DocumentId ON LegalHoldDocuments(DocumentId);
    CREATE INDEX IX_LegalHoldDocuments_Active ON LegalHoldDocuments(DocumentId, ReleasedAt) WHERE ReleasedAt IS NULL;
    PRINT 'Created LegalHoldDocuments table';
END

-- ============================================================================
-- PART 4: Disposal Certificate Table (ISO 15489)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('DisposalCertificates') AND type = 'U')
BEGIN
    CREATE TABLE DisposalCertificates (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CertificateNumber NVARCHAR(50) NOT NULL,
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        DocumentName NVARCHAR(500) NOT NULL,
        DocumentPath NVARCHAR(1000) NULL,
        Classification NVARCHAR(255) NULL,
        RetentionPolicyId UNIQUEIDENTIFIER NULL,
        RetentionPolicyName NVARCHAR(255) NULL,
        DocumentCreatedAt DATETIME2 NOT NULL,
        RetentionStartDate DATETIME2 NULL,
        RetentionExpirationDate DATETIME2 NULL,
        DisposalMethod NVARCHAR(50) NOT NULL,
        DisposedAt DATETIME2 NOT NULL,
        DisposedBy UNIQUEIDENTIFIER NOT NULL,
        DisposedByName NVARCHAR(255) NULL,
        ApprovedBy UNIQUEIDENTIFIER NULL,
        ApprovedByName NVARCHAR(255) NULL,
        ApprovedAt DATETIME2 NULL,
        LegalBasis NVARCHAR(MAX) NULL,
        Notes NVARCHAR(MAX) NULL,
        ContentHashAtDisposal NVARCHAR(128) NULL,
        FileSizeAtDisposal BIGINT NOT NULL DEFAULT 0,
        VersionsDisposed INT NOT NULL DEFAULT 1,
        CertificateSignature NVARCHAR(MAX) NULL,
        DisposalVerified BIT NOT NULL DEFAULT 0,
        VerifiedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE UNIQUE INDEX IX_DisposalCertificates_CertificateNumber ON DisposalCertificates(CertificateNumber);
    CREATE INDEX IX_DisposalCertificates_DocumentId ON DisposalCertificates(DocumentId);
    CREATE INDEX IX_DisposalCertificates_DisposedAt ON DisposalCertificates(DisposedAt);
    PRINT 'Created DisposalCertificates table';
END

-- ============================================================================
-- PART 5: Integrity Verification Log Table (ISO 14721 OAIS Fixity)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('IntegrityVerificationLogs') AND type = 'U')
BEGIN
    CREATE TABLE IntegrityVerificationLogs (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        VersionNumber INT NULL,
        ExpectedHash NVARCHAR(128) NOT NULL,
        ComputedHash NVARCHAR(128) NOT NULL,
        HashAlgorithm NVARCHAR(20) NOT NULL DEFAULT 'SHA256',
        IsValid BIT NOT NULL,
        VerifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        VerificationType NVARCHAR(20) NOT NULL DEFAULT 'Scheduled',
        VerifiedBy UNIQUEIDENTIFIER NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ActionTaken NVARCHAR(50) NULL,

        CONSTRAINT FK_IntegrityVerificationLogs_Document FOREIGN KEY (DocumentId) REFERENCES Documents(Id)
    );

    CREATE INDEX IX_IntegrityVerificationLogs_DocumentId ON IntegrityVerificationLogs(DocumentId);
    CREATE INDEX IX_IntegrityVerificationLogs_VerifiedAt ON IntegrityVerificationLogs(VerifiedAt);
    CREATE INDEX IX_IntegrityVerificationLogs_IsValid ON IntegrityVerificationLogs(IsValid) WHERE IsValid = 0;
    PRINT 'Created IntegrityVerificationLogs table';
END

-- ============================================================================
-- PART 6: Preservation Metadata Table (ISO 14721 OAIS)
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('PreservationMetadata') AND type = 'U')
BEGIN
    CREATE TABLE PreservationMetadata (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        VersionNumber INT NOT NULL,
        FormatIdentifier NVARCHAR(100) NULL,
        FormatName NVARCHAR(255) NULL,
        FormatVersion NVARCHAR(50) NULL,
        FormatRegistry NVARCHAR(100) NULL,
        IsPreservationFormat BIT NOT NULL DEFAULT 0,
        MigrationTargetFormat NVARCHAR(255) NULL,
        IdentifiedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IdentificationTool NVARCHAR(255) NULL,
        CreatingApplication NVARCHAR(255) NULL,
        EnvironmentRequirements NVARCHAR(MAX) NULL,
        SignificantProperties NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2 NULL,

        CONSTRAINT FK_PreservationMetadata_Document FOREIGN KEY (DocumentId) REFERENCES Documents(Id)
    );

    CREATE INDEX IX_PreservationMetadata_DocumentId ON PreservationMetadata(DocumentId);
    CREATE INDEX IX_PreservationMetadata_FormatIdentifier ON PreservationMetadata(FormatIdentifier);
    PRINT 'Created PreservationMetadata table';
END

-- ============================================================================
-- PART 7: Create Indexes for Performance
-- ============================================================================

-- Index for finding documents on legal hold
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Documents_IsOnLegalHold' AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE INDEX IX_Documents_IsOnLegalHold ON Documents(IsOnLegalHold) WHERE IsOnLegalHold = 1;
    PRINT 'Created IX_Documents_IsOnLegalHold index';
END

-- Index for finding documents by retention policy
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Documents_RetentionPolicyId' AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE INDEX IX_Documents_RetentionPolicyId ON Documents(RetentionPolicyId) WHERE RetentionPolicyId IS NOT NULL;
    PRINT 'Created IX_Documents_RetentionPolicyId index';
END

-- Index for finding documents needing integrity verification
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Documents_IntegrityVerifiedAt' AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE INDEX IX_Documents_IntegrityVerifiedAt ON Documents(IntegrityVerifiedAt);
    PRINT 'Created IX_Documents_IntegrityVerifiedAt index';
END

-- ============================================================================
-- MIGRATION COMPLETE
-- ============================================================================

PRINT '============================================================================';
PRINT 'ISO Compliance Schema Migration Complete';
PRINT 'Standards Implemented: ISO 15489, ISO 27001, ISO 14721, ISO 23081';
PRINT '============================================================================';
