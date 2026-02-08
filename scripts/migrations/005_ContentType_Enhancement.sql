-- =============================================
-- Migration: 005_ContentType_Enhancement
-- Description: Unify content type system - rename tables, add defaults, cabinet support
-- Date: 2024
-- =============================================

USE DMSModern;
GO

PRINT 'Starting Content Type Enhancement Migration...';
GO

-- =============================================
-- PHASE 1: Rename ContentTypes to FileTypes (MIME type management)
-- =============================================

-- Step 1.1: Drop existing foreign key constraints that reference ContentTypes
-- Note: ContentTypes table is for file extensions, not referenced by FK in current schema

-- Step 1.2: Rename the table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentTypes')
BEGIN
    EXEC sp_rename 'ContentTypes', 'FileTypes';
    PRINT 'Renamed ContentTypes table to FileTypes';
END
GO

-- =============================================
-- PHASE 2: Rename ContentTypeDefinitions to ContentTypes
-- This is the actual content type system for metadata schemas
-- =============================================

-- Step 2.1: Drop FK constraints that reference ContentTypeDefinitions
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__ContentTy__Conte__XXXXX')
BEGIN
    -- Note: Exact constraint names may vary, using dynamic drop
    PRINT 'Dropping foreign key constraints...';
END

-- Step 2.2: Drop indexes on referencing tables
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ContentTypeFields_ContentTypeId')
    DROP INDEX IX_ContentTypeFields_ContentTypeId ON ContentTypeFields;

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DocumentMetadata_ContentTypeId')
    DROP INDEX IX_DocumentMetadata_ContentTypeId ON DocumentMetadata;

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FolderContentTypeAssignments_ContentTypeId')
    DROP INDEX IX_FolderContentTypeAssignments_ContentTypeId ON FolderContentTypeAssignments;
GO

-- Step 2.3: Drop FK constraints from dependent tables
DECLARE @sql NVARCHAR(MAX) = '';

-- Drop FK from ContentTypeFields
SELECT @sql = @sql + 'ALTER TABLE ContentTypeFields DROP CONSTRAINT ' + fk.name + '; '
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
WHERE t.name = 'ContentTypeFields' AND fk.name LIKE '%ContentTypeDefinitions%';
IF @sql <> '' EXEC sp_executesql @sql;

SET @sql = '';
-- Drop FK from DocumentMetadata
SELECT @sql = @sql + 'ALTER TABLE DocumentMetadata DROP CONSTRAINT ' + fk.name + '; '
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
WHERE t.name = 'DocumentMetadata' AND fk.name LIKE '%ContentTypeDefinitions%';
IF @sql <> '' EXEC sp_executesql @sql;

SET @sql = '';
-- Drop FK from FolderContentTypeAssignments
SELECT @sql = @sql + 'ALTER TABLE FolderContentTypeAssignments DROP CONSTRAINT ' + fk.name + '; '
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
WHERE t.name = 'FolderContentTypeAssignments' AND fk.name LIKE '%ContentTypeDefinitions%';
IF @sql <> '' EXEC sp_executesql @sql;

SET @sql = '';
-- Drop FK from Patterns
SELECT @sql = @sql + 'ALTER TABLE Patterns DROP CONSTRAINT ' + fk.name + '; '
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
WHERE t.name = 'Patterns' AND fk.name LIKE '%ContentTypeDefinitions%';
IF @sql <> '' EXEC sp_executesql @sql;
GO

-- Step 2.4: Rename the table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentTypeDefinitions')
BEGIN
    EXEC sp_rename 'ContentTypeDefinitions', 'ContentTypes_New';
    PRINT 'Renamed ContentTypeDefinitions to ContentTypes_New (temporary)';
END
GO

-- Step 2.5: Now that FileTypes exists (renamed from old ContentTypes), rename ContentTypes_New to ContentTypes
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentTypes_New')
BEGIN
    EXEC sp_rename 'ContentTypes_New', 'ContentTypes';
    PRINT 'Renamed ContentTypes_New to ContentTypes';
END
GO

-- Step 2.6: Re-add FK constraints with new table name
ALTER TABLE ContentTypeFields
ADD CONSTRAINT FK_ContentTypeFields_ContentTypes
FOREIGN KEY (ContentTypeId) REFERENCES ContentTypes(Id);

ALTER TABLE DocumentMetadata
ADD CONSTRAINT FK_DocumentMetadata_ContentTypes
FOREIGN KEY (ContentTypeId) REFERENCES ContentTypes(Id);

ALTER TABLE FolderContentTypeAssignments
ADD CONSTRAINT FK_FolderContentTypeAssignments_ContentTypes
FOREIGN KEY (ContentTypeId) REFERENCES ContentTypes(Id);

ALTER TABLE Patterns
ADD CONSTRAINT FK_Patterns_ContentTypes
FOREIGN KEY (ContentTypeId) REFERENCES ContentTypes(Id);
GO

-- Step 2.7: Re-create indexes
CREATE INDEX IX_ContentTypeFields_ContentTypeId ON ContentTypeFields(ContentTypeId);
CREATE INDEX IX_DocumentMetadata_ContentTypeId ON DocumentMetadata(ContentTypeId);
CREATE INDEX IX_FolderContentTypeAssignments_ContentTypeId ON FolderContentTypeAssignments(ContentTypeId);
GO

PRINT 'Completed table renames and FK updates';
GO

-- =============================================
-- PHASE 3: Add IsDefault and DisplayOrder to FolderContentTypeAssignments
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FolderContentTypeAssignments') AND name = 'IsDefault')
BEGIN
    ALTER TABLE FolderContentTypeAssignments
    ADD IsDefault BIT NOT NULL DEFAULT 0;
    PRINT 'Added IsDefault column to FolderContentTypeAssignments';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FolderContentTypeAssignments') AND name = 'DisplayOrder')
BEGIN
    ALTER TABLE FolderContentTypeAssignments
    ADD DisplayOrder INT NOT NULL DEFAULT 0;
    PRINT 'Added DisplayOrder column to FolderContentTypeAssignments';
END
GO

-- Create filtered unique index: only one default per folder
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FolderContentType_Default')
BEGIN
    CREATE UNIQUE INDEX IX_FolderContentType_Default
    ON FolderContentTypeAssignments(FolderId)
    WHERE IsDefault = 1;
    PRINT 'Created unique index for default content type per folder';
END
GO

-- =============================================
-- PHASE 4: Create CabinetContentTypeAssignments table
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CabinetContentTypeAssignments')
BEGIN
    CREATE TABLE CabinetContentTypeAssignments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        CabinetId UNIQUEIDENTIFIER NOT NULL,
        ContentTypeId UNIQUEIDENTIFIER NOT NULL,
        IsRequired BIT NOT NULL DEFAULT 0,
        IsDefault BIT NOT NULL DEFAULT 0,
        InheritToChildren BIT NOT NULL DEFAULT 1,
        DisplayOrder INT NOT NULL DEFAULT 0,
        CreatedBy UNIQUEIDENTIFIER,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_CabinetContentType_Cabinets FOREIGN KEY (CabinetId) REFERENCES Cabinets(Id) ON DELETE CASCADE,
        CONSTRAINT FK_CabinetContentType_ContentTypes FOREIGN KEY (ContentTypeId) REFERENCES ContentTypes(Id),
        CONSTRAINT UQ_Cabinet_ContentType UNIQUE (CabinetId, ContentTypeId)
    );

    CREATE INDEX IX_CabinetContentType_CabinetId ON CabinetContentTypeAssignments(CabinetId);
    CREATE INDEX IX_CabinetContentType_ContentTypeId ON CabinetContentTypeAssignments(ContentTypeId);

    -- Only one default per cabinet
    CREATE UNIQUE INDEX IX_CabinetContentType_Default
    ON CabinetContentTypeAssignments(CabinetId)
    WHERE IsDefault = 1;

    PRINT 'Created CabinetContentTypeAssignments table';
END
GO

-- =============================================
-- PHASE 5: Add ContentTypeId to Documents table
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'ContentTypeId')
BEGIN
    ALTER TABLE Documents
    ADD ContentTypeId UNIQUEIDENTIFIER NULL;

    ALTER TABLE Documents
    ADD CONSTRAINT FK_Documents_ContentTypes
    FOREIGN KEY (ContentTypeId) REFERENCES ContentTypes(Id);

    CREATE INDEX IX_Documents_ContentTypeId ON Documents(ContentTypeId);

    PRINT 'Added ContentTypeId column to Documents';
END
GO

-- =============================================
-- PHASE 6: Add BreakContentTypeInheritance to Folders
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Folders') AND name = 'BreakContentTypeInheritance')
BEGIN
    ALTER TABLE Folders
    ADD BreakContentTypeInheritance BIT NOT NULL DEFAULT 0;
    PRINT 'Added BreakContentTypeInheritance column to Folders';
END
GO

-- =============================================
-- PHASE 7: Add Category column to ContentTypes for grouping
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ContentTypes') AND name = 'Category')
BEGIN
    ALTER TABLE ContentTypes
    ADD Category NVARCHAR(100) NULL;
    PRINT 'Added Category column to ContentTypes';
END
GO

-- =============================================
-- VERIFICATION
-- =============================================

PRINT '';
PRINT '=== Migration Verification ===';

-- Check FileTypes table
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FileTypes')
    PRINT 'FileTypes table exists (renamed from old ContentTypes)';
ELSE
    PRINT 'WARNING: FileTypes table does not exist!';

-- Check ContentTypes table (new)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentTypes')
    PRINT 'ContentTypes table exists (renamed from ContentTypeDefinitions)';
ELSE
    PRINT 'WARNING: ContentTypes table does not exist!';

-- Check CabinetContentTypeAssignments
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CabinetContentTypeAssignments')
    PRINT 'CabinetContentTypeAssignments table exists';
ELSE
    PRINT 'WARNING: CabinetContentTypeAssignments table does not exist!';

-- Check new columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('FolderContentTypeAssignments') AND name = 'IsDefault')
    PRINT 'FolderContentTypeAssignments.IsDefault column exists';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'ContentTypeId')
    PRINT 'Documents.ContentTypeId column exists';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Folders') AND name = 'BreakContentTypeInheritance')
    PRINT 'Folders.BreakContentTypeInheritance column exists';

PRINT '';
PRINT 'Content Type Enhancement Migration completed successfully!';
GO
