-- Migration: Add Document Features (Comments, Attachments, Links, Passwords)
-- Version: 003
-- Date: 2026-02-03

-- =============================================
-- Document Comments Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentComments')
BEGIN
    CREATE TABLE DocumentComments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        ParentCommentId UNIQUEIDENTIFIER NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedBy UNIQUEIDENTIFIER NULL,
        ModifiedAt DATETIME2 NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,

        CONSTRAINT FK_DocumentComments_Document FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentComments_Parent FOREIGN KEY (ParentCommentId) REFERENCES DocumentComments(Id),
        CONSTRAINT FK_DocumentComments_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
        CONSTRAINT FK_DocumentComments_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES Users(Id)
    );

    CREATE INDEX IX_DocumentComments_DocumentId ON DocumentComments(DocumentId);
    CREATE INDEX IX_DocumentComments_ParentCommentId ON DocumentComments(ParentCommentId);
    CREATE INDEX IX_DocumentComments_CreatedBy ON DocumentComments(CreatedBy);

    PRINT 'Created DocumentComments table';
END
GO

-- =============================================
-- Document Attachments Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentAttachments')
BEGIN
    CREATE TABLE DocumentAttachments (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        FileName NVARCHAR(500) NOT NULL,
        Description NVARCHAR(1000) NULL,
        ContentType NVARCHAR(200) NULL,
        Size BIGINT NOT NULL DEFAULT 0,
        StoragePath NVARCHAR(1000) NULL,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DocumentAttachments_Document FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentAttachments_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
    );

    CREATE INDEX IX_DocumentAttachments_DocumentId ON DocumentAttachments(DocumentId);
    CREATE INDEX IX_DocumentAttachments_CreatedBy ON DocumentAttachments(CreatedBy);

    PRINT 'Created DocumentAttachments table';
END
GO

-- =============================================
-- Document Links Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentLinks')
BEGIN
    CREATE TABLE DocumentLinks (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        SourceDocumentId UNIQUEIDENTIFIER NOT NULL,
        TargetDocumentId UNIQUEIDENTIFIER NOT NULL,
        LinkType NVARCHAR(50) NOT NULL DEFAULT 'related', -- related, reference, supersedes, attachment
        Description NVARCHAR(500) NULL,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DocumentLinks_SourceDocument FOREIGN KEY (SourceDocumentId) REFERENCES Documents(Id),
        CONSTRAINT FK_DocumentLinks_TargetDocument FOREIGN KEY (TargetDocumentId) REFERENCES Documents(Id),
        CONSTRAINT FK_DocumentLinks_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
        CONSTRAINT UQ_DocumentLinks_SourceTarget UNIQUE (SourceDocumentId, TargetDocumentId)
    );

    CREATE INDEX IX_DocumentLinks_SourceDocumentId ON DocumentLinks(SourceDocumentId);
    CREATE INDEX IX_DocumentLinks_TargetDocumentId ON DocumentLinks(TargetDocumentId);
    CREATE INDEX IX_DocumentLinks_LinkType ON DocumentLinks(LinkType);

    PRINT 'Created DocumentLinks table';
END
GO

-- =============================================
-- Document Passwords Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentPasswords')
BEGIN
    CREATE TABLE DocumentPasswords (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        Hint NVARCHAR(200) NULL,
        ExpiresAt DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedBy UNIQUEIDENTIFIER NULL,
        ModifiedAt DATETIME2 NULL,

        CONSTRAINT FK_DocumentPasswords_Document FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentPasswords_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
        CONSTRAINT FK_DocumentPasswords_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES Users(Id)
    );

    CREATE INDEX IX_DocumentPasswords_DocumentId ON DocumentPasswords(DocumentId);
    CREATE INDEX IX_DocumentPasswords_IsActive ON DocumentPasswords(IsActive);

    PRINT 'Created DocumentPasswords table';
END
GO

-- =============================================
-- Add HasPassword flag to Documents table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'HasPassword')
BEGIN
    ALTER TABLE Documents ADD HasPassword BIT NOT NULL DEFAULT 0;
    PRINT 'Added HasPassword column to Documents table';
END
GO

-- =============================================
-- Add CommentCount to Documents table (denormalized for performance)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'CommentCount')
BEGIN
    ALTER TABLE Documents ADD CommentCount INT NOT NULL DEFAULT 0;
    PRINT 'Added CommentCount column to Documents table';
END
GO

-- =============================================
-- Add AttachmentCount to Documents table (denormalized for performance)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'AttachmentCount')
BEGIN
    ALTER TABLE Documents ADD AttachmentCount INT NOT NULL DEFAULT 0;
    PRINT 'Added AttachmentCount column to Documents table';
END
GO

-- =============================================
-- Add LinkCount to Documents table (denormalized for performance)
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Documents') AND name = 'LinkCount')
BEGIN
    ALTER TABLE Documents ADD LinkCount INT NOT NULL DEFAULT 0;
    PRINT 'Added LinkCount column to Documents table';
END
GO

PRINT 'Migration 003_AddDocumentFeatures completed successfully';
