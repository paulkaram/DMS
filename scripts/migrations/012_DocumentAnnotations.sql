-- =============================================
-- Migration 012: Document Annotations & Saved Signatures
-- Adds support for PDF annotation (freehand, highlight, redaction, text, signature)
-- =============================================

-- Document Annotations table (one row per document+page, stores Fabric.js JSON)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentAnnotations')
BEGIN
    CREATE TABLE DocumentAnnotations (
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        DocumentId UNIQUEIDENTIFIER NOT NULL,
        PageNumber INT NOT NULL,
        AnnotationData NVARCHAR(MAX) NOT NULL, -- Fabric.js JSON serialization
        VersionNumber INT NOT NULL DEFAULT 1,
        CreatedBy UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedBy UNIQUEIDENTIFIER NULL,
        ModifiedAt DATETIME2 NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,

        CONSTRAINT FK_DocumentAnnotations_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(Id),
        CONSTRAINT FK_DocumentAnnotations_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
        CONSTRAINT UQ_DocumentAnnotations_DocPage UNIQUE (DocumentId, PageNumber)
    );

    CREATE INDEX IX_DocumentAnnotations_DocumentId ON DocumentAnnotations(DocumentId) WHERE IsDeleted = 0;

    PRINT 'Created DocumentAnnotations table';
END
GO

-- Saved Signatures table (user's reusable signatures)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedSignatures')
BEGIN
    CREATE TABLE SavedSignatures (
        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        SignatureData NVARCHAR(MAX) NOT NULL, -- Base64 data URL or Fabric.js JSON
        SignatureType NVARCHAR(20) NOT NULL DEFAULT 'drawn', -- 'drawn' or 'typed'
        IsDefault BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_SavedSignatures_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
    );

    CREATE INDEX IX_SavedSignatures_UserId ON SavedSignatures(UserId);

    PRINT 'Created SavedSignatures table';
END
GO
