-- =============================================
-- Migration: 006_Fix_Missing_Columns
-- Description: Add missing columns to ContentTypeDefinitions table
-- Run this script to fix the 'Invalid column name' errors
-- =============================================

USE DMSModern;
GO

PRINT 'Fixing missing columns in ContentTypeDefinitions...';
GO

-- Add Name column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'Name')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD Name NVARCHAR(255) NOT NULL DEFAULT '';
    PRINT 'Added Name column to ContentTypeDefinitions';
END
GO

-- Add SortOrder column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'SortOrder')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD SortOrder INT DEFAULT 0;
    PRINT 'Added SortOrder column to ContentTypeDefinitions';
END
GO

-- Add Description column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'Description')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD Description NVARCHAR(MAX) NULL;
    PRINT 'Added Description column to ContentTypeDefinitions';
END
GO

-- Add Icon column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'Icon')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD Icon NVARCHAR(255) NULL;
    PRINT 'Added Icon column to ContentTypeDefinitions';
END
GO

-- Add Color column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'Color')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD Color NVARCHAR(50) NULL;
    PRINT 'Added Color column to ContentTypeDefinitions';
END
GO

-- Add Category column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'Category')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD Category NVARCHAR(100) NULL;
    PRINT 'Added Category column to ContentTypeDefinitions';
END
GO

-- Add AllowOnFolders column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'AllowOnFolders')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD AllowOnFolders BIT DEFAULT 1;
    PRINT 'Added AllowOnFolders column to ContentTypeDefinitions';
END
GO

-- Add AllowOnDocuments column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'AllowOnDocuments')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD AllowOnDocuments BIT DEFAULT 1;
    PRINT 'Added AllowOnDocuments column to ContentTypeDefinitions';
END
GO

-- Add IsRequired column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'IsRequired')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD IsRequired BIT DEFAULT 0;
    PRINT 'Added IsRequired column to ContentTypeDefinitions';
END
GO

-- Add IsActive column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD IsActive BIT DEFAULT 1;
    PRINT 'Added IsActive column to ContentTypeDefinitions';
END
GO

-- Add CreatedBy column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'CreatedBy')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD CreatedBy UNIQUEIDENTIFIER NULL;
    PRINT 'Added CreatedBy column to ContentTypeDefinitions';
END
GO

-- Add CreatedAt column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'CreatedAt')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD CreatedAt DATETIME2 DEFAULT GETUTCDATE();
    PRINT 'Added CreatedAt column to ContentTypeDefinitions';
END
GO

-- Add ModifiedBy column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'ModifiedBy')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD ModifiedBy UNIQUEIDENTIFIER NULL;
    PRINT 'Added ModifiedBy column to ContentTypeDefinitions';
END
GO

-- Add ModifiedAt column if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ContentTypeDefinitions' AND COLUMN_NAME = 'ModifiedAt')
BEGIN
    ALTER TABLE ContentTypeDefinitions ADD ModifiedAt DATETIME2 NULL;
    PRINT 'Added ModifiedAt column to ContentTypeDefinitions';
END
GO

-- Add IsDefault and DisplayOrder to FolderContentTypeAssignments if missing
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FolderContentTypeAssignments' AND COLUMN_NAME = 'IsDefault')
BEGIN
    ALTER TABLE FolderContentTypeAssignments ADD IsDefault BIT NOT NULL DEFAULT 0;
    PRINT 'Added IsDefault column to FolderContentTypeAssignments';
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'FolderContentTypeAssignments' AND COLUMN_NAME = 'DisplayOrder')
BEGIN
    ALTER TABLE FolderContentTypeAssignments ADD DisplayOrder INT NOT NULL DEFAULT 0;
    PRINT 'Added DisplayOrder column to FolderContentTypeAssignments';
END
GO

-- Create CabinetContentTypeAssignments table if not exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CabinetContentTypeAssignments')
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
        CONSTRAINT UQ_Cabinet_ContentType UNIQUE (CabinetId, ContentTypeId)
    );
    PRINT 'Created CabinetContentTypeAssignments table';
END
GO

PRINT 'Migration completed successfully!';
GO
