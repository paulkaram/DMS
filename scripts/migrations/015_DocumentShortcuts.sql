-- 015_DocumentShortcuts.sql
-- Document Shortcuts: junction table allowing a document to appear in multiple folders

CREATE TABLE DocumentShortcuts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DocumentId UNIQUEIDENTIFIER NOT NULL,
    FolderId UNIQUEIDENTIFIER NOT NULL,
    CreatedBy UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_DocumentShortcuts_Documents FOREIGN KEY (DocumentId) REFERENCES Documents(Id),
    CONSTRAINT FK_DocumentShortcuts_Folders FOREIGN KEY (FolderId) REFERENCES Folders(Id),
    CONSTRAINT FK_DocumentShortcuts_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id),
    CONSTRAINT UQ_DocumentShortcuts_DocumentFolder UNIQUE (DocumentId, FolderId)
);

CREATE INDEX IX_DocumentShortcuts_FolderId ON DocumentShortcuts(FolderId);
CREATE INDEX IX_DocumentShortcuts_DocumentId ON DocumentShortcuts(DocumentId);
