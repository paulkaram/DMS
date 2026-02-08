-- =============================================
-- Migration: 008_Checkout_Enhancement
-- Description: ISO 15489 Compliant Checkout System with Metadata Versioning
-- Features:
--   - Major/Minor versioning (SharePoint-like)
--   - Metadata versioning per document version
--   - Working copy for draft changes during checkout
--   - Version comparison support
--   - Proper checkout enforcement
-- =============================================

USE DMS;
GO

PRINT 'Starting Migration 008: Checkout Enhancement';
GO

-- =============================================
-- 1. EXTEND DOCUMENTVERSIONS TABLE
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'VersionType')
BEGIN
    PRINT 'Adding VersionType column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD VersionType NVARCHAR(10) NOT NULL DEFAULT 'Minor';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'VersionLabel')
BEGIN
    PRINT 'Adding VersionLabel column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD VersionLabel NVARCHAR(20) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'MajorVersion')
BEGIN
    PRINT 'Adding MajorVersion column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD MajorVersion INT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'MinorVersion')
BEGIN
    PRINT 'Adding MinorVersion column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD MinorVersion INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'IsContentChanged')
BEGIN
    PRINT 'Adding IsContentChanged column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD IsContentChanged BIT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'IsMetadataChanged')
BEGIN
    PRINT 'Adding IsMetadataChanged column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD IsMetadataChanged BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'PreviousVersionId')
BEGIN
    PRINT 'Adding PreviousVersionId column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD PreviousVersionId UNIQUEIDENTIFIER NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DocumentVersions') AND name = 'ChangeDescription')
BEGIN
    PRINT 'Adding ChangeDescription column to DocumentVersions...';
    ALTER TABLE DocumentVersions ADD ChangeDescription NVARCHAR(MAX) NULL;
END
GO

-- Add foreign key for version chain (if not exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DocumentVersions_Previous')
BEGIN
    PRINT 'Adding FK_DocumentVersions_Previous constraint...';
    ALTER TABLE DocumentVersions ADD CONSTRAINT FK_DocumentVersions_Previous
        FOREIGN KEY (PreviousVersionId) REFERENCES DocumentVersions(Id);
END
GO

-- Index for version queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentVersions_MajorMinor' AND object_id = OBJECT_ID('DocumentVersions'))
BEGIN
    PRINT 'Creating IX_DocumentVersions_MajorMinor index...';
    CREATE INDEX IX_DocumentVersions_MajorMinor ON DocumentVersions(DocumentId, MajorVersion DESC, MinorVersion DESC);
END
GO

-- =============================================
-- 2. EXTEND DOCUMENTS TABLE
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'CurrentMajorVersion')
BEGIN
    PRINT 'Adding CurrentMajorVersion column to Documents...';
    ALTER TABLE Documents ADD CurrentMajorVersion INT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'CurrentMinorVersion')
BEGIN
    PRINT 'Adding CurrentMinorVersion column to Documents...';
    ALTER TABLE Documents ADD CurrentMinorVersion INT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'CurrentVersionId')
BEGIN
    PRINT 'Adding CurrentVersionId column to Documents...';
    ALTER TABLE Documents ADD CurrentVersionId UNIQUEIDENTIFIER NULL;
END
GO

-- Add foreign key for current version (if not exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Documents_CurrentVersion')
BEGIN
    PRINT 'Adding FK_Documents_CurrentVersion constraint...';
    ALTER TABLE Documents ADD CONSTRAINT FK_Documents_CurrentVersion
        FOREIGN KEY (CurrentVersionId) REFERENCES DocumentVersions(Id);
END
GO

-- =============================================
-- 3. CREATE DOCUMENTVERSIONMETADATA TABLE
-- Stores metadata snapshot for each version (enables comparison)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentVersionMetadata')
BEGIN
    PRINT 'Creating DocumentVersionMetadata table...';

    CREATE TABLE DocumentVersionMetadata (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentVersionId UNIQUEIDENTIFIER NOT NULL,
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        ContentTypeId UNIQUEIDENTIFIER NOT NULL,
        FieldId UNIQUEIDENTIFIER NOT NULL,
        FieldName NVARCHAR(100) NOT NULL,
        Value NVARCHAR(MAX) NULL,
        NumericValue DECIMAL(18,6) NULL,
        DateValue DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DocVersionMetadata_Version FOREIGN KEY (DocumentVersionId)
            REFERENCES DocumentVersions(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocVersionMetadata_Document FOREIGN KEY (DocumentId)
            REFERENCES Documents(Id)
    );

    CREATE INDEX IX_DocVersionMetadata_VersionId ON DocumentVersionMetadata(DocumentVersionId);
    CREATE INDEX IX_DocVersionMetadata_DocumentId ON DocumentVersionMetadata(DocumentId);
    CREATE INDEX IX_DocVersionMetadata_FieldId ON DocumentVersionMetadata(FieldId);

    PRINT 'DocumentVersionMetadata table created successfully.';
END
GO

-- =============================================
-- 4. CREATE DOCUMENTWORKINGCOPIES TABLE
-- Stores draft changes during checkout (enables discard)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentWorkingCopies')
BEGIN
    PRINT 'Creating DocumentWorkingCopies table...';

    CREATE TABLE DocumentWorkingCopies (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentId UNIQUEIDENTIFIER NOT NULL UNIQUE,
        CheckedOutBy UNIQUEIDENTIFIER NOT NULL,
        CheckedOutAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Draft content (null if no file replacement)
        DraftStoragePath NVARCHAR(MAX) NULL,
        DraftSize BIGINT NULL,
        DraftContentType NVARCHAR(255) NULL,
        DraftOriginalFileName NVARCHAR(500) NULL,
        DraftIntegrityHash NVARCHAR(128) NULL,

        -- Draft metadata (JSON serialized for flexibility during editing)
        DraftMetadataJson NVARCHAR(MAX) NULL,

        -- Draft document properties
        DraftName NVARCHAR(255) NULL,
        DraftDescription NVARCHAR(MAX) NULL,
        DraftClassificationId UNIQUEIDENTIFIER NULL,
        DraftImportanceId UNIQUEIDENTIFIER NULL,
        DraftDocumentTypeId UNIQUEIDENTIFIER NULL,

        -- Tracking
        LastModifiedAt DATETIME2 NULL,
        AutoSaveEnabled BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_WorkingCopy_Document FOREIGN KEY (DocumentId)
            REFERENCES Documents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_WorkingCopy_User FOREIGN KEY (CheckedOutBy)
            REFERENCES Users(Id)
    );

    CREATE INDEX IX_DocumentWorkingCopies_User ON DocumentWorkingCopies(CheckedOutBy);
    CREATE INDEX IX_DocumentWorkingCopies_Document ON DocumentWorkingCopies(DocumentId);

    PRINT 'DocumentWorkingCopies table created successfully.';
END
GO

-- =============================================
-- 5. UPDATE EXISTING DATA
-- Set version labels for existing versions
-- =============================================

PRINT 'Updating existing document versions with version labels...';

UPDATE DocumentVersions
SET VersionLabel = CAST(VersionNumber AS NVARCHAR(10)) + '.0',
    MajorVersion = VersionNumber,
    MinorVersion = 0,
    VersionType = 'Major'
WHERE VersionLabel IS NULL;
GO

-- Update Documents with CurrentVersionId
PRINT 'Updating Documents with CurrentVersionId...';

UPDATE d
SET d.CurrentVersionId = dv.Id,
    d.CurrentMajorVersion = dv.MajorVersion,
    d.CurrentMinorVersion = dv.MinorVersion
FROM Documents d
INNER JOIN (
    SELECT DocumentId, Id, MajorVersion, MinorVersion,
           ROW_NUMBER() OVER (PARTITION BY DocumentId ORDER BY VersionNumber DESC) as rn
    FROM DocumentVersions
) dv ON d.Id = dv.DocumentId AND dv.rn = 1
WHERE d.CurrentVersionId IS NULL;
GO

-- =============================================
-- 6. ADD ACTIVITY LOG ACTIONS
-- =============================================

-- No table changes needed, just reference for code:
-- 'DraftSaved' - When user saves draft during checkout
-- 'CheckoutForced' - When admin forces checkout release
-- 'MetadataVersioned' - When metadata is snapshotted to version
-- 'VersionCompared' - When versions are compared
-- 'VersionRestored' - When a previous version is restored

PRINT 'Migration 008: Checkout Enhancement completed successfully!';
GO
