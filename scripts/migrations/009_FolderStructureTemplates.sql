-- =============================================
-- Migration: 009_FolderStructureTemplates
-- Description: Add folder structure templates (patterns) for automated folder creation
-- Date: 2026-02-05
-- =============================================

USE DMS;
GO

PRINT 'Starting Folder Structure Templates Migration...';
GO

-- =============================================
-- PHASE 1: FolderTemplates Table
-- Master table for folder structure templates
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FolderTemplates')
BEGIN
    CREATE TABLE FolderTemplates (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        Category NVARCHAR(100) NULL,
        Icon NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDefault BIT NOT NULL DEFAULT 0,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedBy UNIQUEIDENTIFIER NULL,
        ModifiedAt DATETIME2 NULL,

        CONSTRAINT FK_FolderTemplates_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );

    CREATE INDEX IX_FolderTemplates_Name ON FolderTemplates(Name);
    CREATE INDEX IX_FolderTemplates_Category ON FolderTemplates(Category) WHERE Category IS NOT NULL;
    CREATE INDEX IX_FolderTemplates_IsActive ON FolderTemplates(IsActive);

    -- Only one default template allowed
    CREATE UNIQUE INDEX IX_FolderTemplates_Default ON FolderTemplates(IsDefault) WHERE IsDefault = 1;

    PRINT 'Created FolderTemplates table';
END
GO

-- =============================================
-- PHASE 2: FolderTemplateNodes Table
-- Defines the folder hierarchy within a template (self-referencing for parent-child)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FolderTemplateNodes')
BEGIN
    CREATE TABLE FolderTemplateNodes (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TemplateId UNIQUEIDENTIFIER NOT NULL,
        ParentNodeId UNIQUEIDENTIFIER NULL,
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        ContentTypeId UNIQUEIDENTIFIER NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        BreakContentTypeInheritance BIT NOT NULL DEFAULT 0,

        CONSTRAINT FK_TemplateNodes_Template FOREIGN KEY (TemplateId)
            REFERENCES FolderTemplates(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TemplateNodes_Parent FOREIGN KEY (ParentNodeId)
            REFERENCES FolderTemplateNodes(Id),
        CONSTRAINT FK_TemplateNodes_ContentType FOREIGN KEY (ContentTypeId)
            REFERENCES ContentTypes(Id)
    );

    CREATE INDEX IX_TemplateNodes_TemplateId ON FolderTemplateNodes(TemplateId);
    CREATE INDEX IX_TemplateNodes_ParentNodeId ON FolderTemplateNodes(ParentNodeId) WHERE ParentNodeId IS NOT NULL;
    CREATE INDEX IX_TemplateNodes_SortOrder ON FolderTemplateNodes(TemplateId, SortOrder);

    PRINT 'Created FolderTemplateNodes table';
END
GO

-- =============================================
-- PHASE 3: FolderTemplateUsage Table
-- Tracks which folders were created from which templates (for analytics/auditing)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FolderTemplateUsage')
BEGIN
    CREATE TABLE FolderTemplateUsage (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        TemplateId UNIQUEIDENTIFIER NOT NULL,
        RootFolderId UNIQUEIDENTIFIER NOT NULL,
        CabinetId UNIQUEIDENTIFIER NOT NULL,
        AppliedBy UNIQUEIDENTIFIER NOT NULL,
        AppliedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        FoldersCreated INT NOT NULL DEFAULT 0,

        CONSTRAINT FK_TemplateUsage_Template FOREIGN KEY (TemplateId)
            REFERENCES FolderTemplates(Id),
        CONSTRAINT FK_TemplateUsage_Folder FOREIGN KEY (RootFolderId)
            REFERENCES Folders(Id) ON DELETE CASCADE,
        CONSTRAINT FK_TemplateUsage_Cabinet FOREIGN KEY (CabinetId)
            REFERENCES Cabinets(Id),
        CONSTRAINT FK_TemplateUsage_AppliedBy FOREIGN KEY (AppliedBy)
            REFERENCES Users(Id)
    );

    CREATE INDEX IX_TemplateUsage_TemplateId ON FolderTemplateUsage(TemplateId);
    CREATE INDEX IX_TemplateUsage_RootFolderId ON FolderTemplateUsage(RootFolderId);
    CREATE INDEX IX_TemplateUsage_AppliedAt ON FolderTemplateUsage(AppliedAt DESC);

    PRINT 'Created FolderTemplateUsage table';
END
GO

-- =============================================
-- PHASE 4: Sample Data (Optional - comment out for production)
-- =============================================

-- Insert sample template for demonstration
DECLARE @AdminUserId UNIQUEIDENTIFIER;
SELECT TOP 1 @AdminUserId = u.Id
FROM Users u
INNER JOIN UserRoles ur ON u.Id = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.Id
WHERE r.Name = 'Admin';

IF @AdminUserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM FolderTemplates WHERE Name = 'Project Folder')
BEGIN
    DECLARE @TemplateId UNIQUEIDENTIFIER = NEWID();
    DECLARE @DocsNodeId UNIQUEIDENTIFIER = NEWID();
    DECLARE @ContractsNodeId UNIQUEIDENTIFIER = NEWID();
    DECLARE @InvoicesNodeId UNIQUEIDENTIFIER = NEWID();
    DECLARE @ReportsNodeId UNIQUEIDENTIFIER = NEWID();
    DECLARE @MonthlyNodeId UNIQUEIDENTIFIER = NEWID();
    DECLARE @AnnualNodeId UNIQUEIDENTIFIER = NEWID();
    DECLARE @CorrespondenceNodeId UNIQUEIDENTIFIER = NEWID();

    -- Create template
    INSERT INTO FolderTemplates (Id, Name, Description, Category, Icon, IsActive, IsDefault, CreatedBy)
    VALUES (@TemplateId, 'Project Folder', 'Standard project folder structure with documents, contracts, invoices, and reports', 'Project', 'folder_special', 1, 0, @AdminUserId);

    -- Create root-level nodes
    INSERT INTO FolderTemplateNodes (Id, TemplateId, ParentNodeId, Name, Description, ContentTypeId, SortOrder)
    VALUES
        (@DocsNodeId, @TemplateId, NULL, 'Documents', 'General project documents', NULL, 1),
        (@ContractsNodeId, @TemplateId, NULL, 'Contracts', 'Contract documents', NULL, 2),
        (@InvoicesNodeId, @TemplateId, NULL, 'Invoices', 'Invoice documents', NULL, 3),
        (@ReportsNodeId, @TemplateId, NULL, 'Reports', 'Project reports', NULL, 4),
        (@CorrespondenceNodeId, @TemplateId, NULL, 'Correspondence', 'Letters and communication', NULL, 5);

    -- Create child nodes under Reports
    INSERT INTO FolderTemplateNodes (Id, TemplateId, ParentNodeId, Name, Description, ContentTypeId, SortOrder)
    VALUES
        (@MonthlyNodeId, @TemplateId, @ReportsNodeId, 'Monthly', 'Monthly reports', NULL, 1),
        (@AnnualNodeId, @TemplateId, @ReportsNodeId, 'Annual', 'Annual reports', NULL, 2);

    PRINT 'Created sample Project Folder template';
END
GO

-- =============================================
-- VERIFICATION
-- =============================================

PRINT '';
PRINT '=== Migration Verification ===';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FolderTemplates')
    PRINT 'FolderTemplates table exists';
ELSE
    PRINT 'WARNING: FolderTemplates table does not exist!';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FolderTemplateNodes')
    PRINT 'FolderTemplateNodes table exists';
ELSE
    PRINT 'WARNING: FolderTemplateNodes table does not exist!';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'FolderTemplateUsage')
    PRINT 'FolderTemplateUsage table exists';
ELSE
    PRINT 'WARNING: FolderTemplateUsage table does not exist!';

-- Show sample template if created
SELECT 'Templates Created:' AS Info, COUNT(*) AS Count FROM FolderTemplates;
SELECT 'Template Nodes Created:' AS Info, COUNT(*) AS Count FROM FolderTemplateNodes;

PRINT '';
PRINT 'Folder Structure Templates Migration completed successfully!';
GO
