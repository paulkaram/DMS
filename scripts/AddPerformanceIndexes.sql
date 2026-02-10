SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Performance Indexes for DMS 10M-Scale
-- Generated from EF Core HasIndex() configurations

-- Documents (5 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Documents_FolderId' AND object_id = OBJECT_ID('Documents'))
    CREATE INDEX IX_Documents_FolderId ON Documents(FolderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Documents_CreatedBy' AND object_id = OBJECT_ID('Documents'))
    CREATE INDEX IX_Documents_CreatedBy ON Documents(CreatedBy);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Documents_FolderId_IsActive' AND object_id = OBJECT_ID('Documents'))
    CREATE INDEX IX_Documents_FolderId_IsActive ON Documents(FolderId, IsActive);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Documents_CreatedBy_CreatedAt' AND object_id = OBJECT_ID('Documents'))
    CREATE INDEX IX_Documents_CreatedBy_CreatedAt ON Documents(CreatedBy ASC, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Documents_Name' AND object_id = OBJECT_ID('Documents'))
    CREATE INDEX IX_Documents_Name ON Documents(Name);

-- Folders (3 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Folders_CabinetId' AND object_id = OBJECT_ID('Folders'))
    CREATE INDEX IX_Folders_CabinetId ON Folders(CabinetId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Folders_ParentFolderId' AND object_id = OBJECT_ID('Folders'))
    CREATE INDEX IX_Folders_ParentFolderId ON Folders(ParentFolderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Folders_CabinetId_ParentFolderId' AND object_id = OBJECT_ID('Folders'))
    CREATE INDEX IX_Folders_CabinetId_ParentFolderId ON Folders(CabinetId, ParentFolderId);

-- Permissions (3 indexes - CRITICAL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_NodeType_NodeId' AND object_id = OBJECT_ID('Permissions'))
    CREATE INDEX IX_Permissions_NodeType_NodeId ON Permissions(NodeType, NodeId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_PrincipalType_PrincipalId' AND object_id = OBJECT_ID('Permissions'))
    CREATE INDEX IX_Permissions_PrincipalType_PrincipalId ON Permissions(PrincipalType, PrincipalId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Permissions_NodeType_NodeId_PrincipalType_PrincipalId' AND object_id = OBJECT_ID('Permissions'))
    CREATE INDEX IX_Permissions_NodeType_NodeId_PrincipalType_PrincipalId ON Permissions(NodeType, NodeId, PrincipalType, PrincipalId);

-- EffectivePermissions (2 indexes - CRITICAL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EffectivePermissions_NodeType_NodeId_UserId' AND object_id = OBJECT_ID('EffectivePermissions'))
    CREATE INDEX IX_EffectivePermissions_NodeType_NodeId_UserId ON EffectivePermissions(NodeType, NodeId, UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EffectivePermissions_UserId' AND object_id = OBJECT_ID('EffectivePermissions'))
    CREATE INDEX IX_EffectivePermissions_UserId ON EffectivePermissions(UserId);

-- Structures (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Structures_ParentId' AND object_id = OBJECT_ID('Structures'))
    CREATE INDEX IX_Structures_ParentId ON Structures(ParentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Structures_StructureType' AND object_id = OBJECT_ID('Structures'))
    CREATE INDEX IX_Structures_StructureType ON Structures(StructureType);

-- StructureMembers (3 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StructureMembers_UserId' AND object_id = OBJECT_ID('StructureMembers'))
    CREATE INDEX IX_StructureMembers_UserId ON StructureMembers(UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StructureMembers_StructureId' AND object_id = OBJECT_ID('StructureMembers'))
    CREATE INDEX IX_StructureMembers_StructureId ON StructureMembers(StructureId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StructureMembers_UserId_StructureId' AND object_id = OBJECT_ID('StructureMembers'))
    CREATE INDEX IX_StructureMembers_UserId_StructureId ON StructureMembers(UserId, StructureId);

-- PermissionDelegations (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PermissionDelegations_DelegatorId' AND object_id = OBJECT_ID('PermissionDelegations'))
    CREATE INDEX IX_PermissionDelegations_DelegatorId ON PermissionDelegations(DelegatorId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PermissionDelegations_DelegateId' AND object_id = OBJECT_ID('PermissionDelegations'))
    CREATE INDEX IX_PermissionDelegations_DelegateId ON PermissionDelegations(DelegateId);

-- ActivityLogs (3 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogs_CreatedAt' AND object_id = OBJECT_ID('ActivityLogs'))
    CREATE INDEX IX_ActivityLogs_CreatedAt ON ActivityLogs(CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogs_UserId_CreatedAt' AND object_id = OBJECT_ID('ActivityLogs'))
    CREATE INDEX IX_ActivityLogs_UserId_CreatedAt ON ActivityLogs(UserId ASC, CreatedAt DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ActivityLogs_NodeType_NodeId' AND object_id = OBJECT_ID('ActivityLogs'))
    CREATE INDEX IX_ActivityLogs_NodeType_NodeId ON ActivityLogs(NodeType, NodeId);

-- RecycleBin (3 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecycleBin_DeletedBy' AND object_id = OBJECT_ID('RecycleBin'))
    CREATE INDEX IX_RecycleBin_DeletedBy ON RecycleBin(DeletedBy);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecycleBin_ExpiresAt' AND object_id = OBJECT_ID('RecycleBin'))
    CREATE INDEX IX_RecycleBin_ExpiresAt ON RecycleBin(ExpiresAt);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecycleBin_DeletedBy_DeletedAt' AND object_id = OBJECT_ID('RecycleBin'))
    CREATE INDEX IX_RecycleBin_DeletedBy_DeletedAt ON RecycleBin(DeletedBy ASC, DeletedAt DESC);

-- DocumentVersions (1 index)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentVersions_DocumentId' AND object_id = OBJECT_ID('DocumentVersions'))
    CREATE INDEX IX_DocumentVersions_DocumentId ON DocumentVersions(DocumentId);

-- DocumentRetentions (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentRetentions_DocumentId' AND object_id = OBJECT_ID('DocumentRetentions'))
    CREATE INDEX IX_DocumentRetentions_DocumentId ON DocumentRetentions(DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentRetentions_Status_ExpirationDate' AND object_id = OBJECT_ID('DocumentRetentions'))
    CREATE INDEX IX_DocumentRetentions_Status_ExpirationDate ON DocumentRetentions(Status, ExpirationDate);

-- DocumentShortcuts (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentShortcuts_FolderId' AND object_id = OBJECT_ID('DocumentShortcuts'))
    CREATE INDEX IX_DocumentShortcuts_FolderId ON DocumentShortcuts(FolderId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentShortcuts_DocumentId' AND object_id = OBJECT_ID('DocumentShortcuts'))
    CREATE INDEX IX_DocumentShortcuts_DocumentId ON DocumentShortcuts(DocumentId);

-- Favorites (2 indexes, 1 unique)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Favorites_UserId' AND object_id = OBJECT_ID('Favorites'))
    CREATE INDEX IX_Favorites_UserId ON Favorites(UserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Favorites_UserId_NodeType_NodeId' AND object_id = OBJECT_ID('Favorites'))
    CREATE UNIQUE INDEX IX_Favorites_UserId_NodeType_NodeId ON Favorites(UserId, NodeType, NodeId);

-- DocumentShares (2 indexes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentShares_DocumentId' AND object_id = OBJECT_ID('DocumentShares'))
    CREATE INDEX IX_DocumentShares_DocumentId ON DocumentShares(DocumentId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DocumentShares_SharedWithUserId' AND object_id = OBJECT_ID('DocumentShares'))
    CREATE INDEX IX_DocumentShares_SharedWithUserId ON DocumentShares(SharedWithUserId);

PRINT 'Performance indexes applied successfully (35 indexes across 12 tables)';
