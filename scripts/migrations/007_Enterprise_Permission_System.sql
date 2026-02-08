-- =============================================
-- Migration: 007_Enterprise_Permission_System
-- Description: Enterprise-grade permission system for governmental DMS
-- Features:
--   - Organizational structures (Ministry, Department, Division, Section)
--   - Multi-principal permissions (User, Role, Structure)
--   - Permission inheritance with break capability
--   - Effective permissions caching
--   - Complete audit trail
--   - Temporary/expiring permissions
-- =============================================

USE DMS;
GO

-- =============================================
-- 1. ORGANIZATIONAL STRUCTURES
-- =============================================

-- Structure Types Enum Reference:
-- 1 = Ministry/Organization
-- 2 = Department
-- 3 = Division
-- 4 = Section
-- 5 = Unit

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Structures')
BEGIN
    CREATE TABLE Structures (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ParentId UNIQUEIDENTIFIER NULL,
        Name NVARCHAR(200) NOT NULL,
        NameAr NVARCHAR(200) NULL, -- Arabic name for government entities
        Code NVARCHAR(50) NULL, -- Unique code like "MOF-FIN-001"
        StructureType INT NOT NULL DEFAULT 2, -- 1=Ministry, 2=Dept, 3=Div, 4=Section, 5=Unit
        [Path] NVARCHAR(1000) NULL, -- Materialized path: /id1/id2/id3/
        [Level] INT NOT NULL DEFAULT 0, -- Depth in hierarchy
        ManagerId UNIQUEIDENTIFIER NULL, -- Structure manager/head
        IsActive BIT NOT NULL DEFAULT 1,
        SortOrder INT NOT NULL DEFAULT 0,
        Description NVARCHAR(500) NULL,
        CreatedBy UNIQUEIDENTIFIER NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedBy UNIQUEIDENTIFIER NULL,
        ModifiedAt DATETIME2 NULL,

        CONSTRAINT FK_Structures_Parent FOREIGN KEY (ParentId) REFERENCES Structures(Id),
        CONSTRAINT FK_Structures_Manager FOREIGN KEY (ManagerId) REFERENCES Users(Id)
    );

    CREATE INDEX IX_Structures_ParentId ON Structures(ParentId);
    CREATE INDEX IX_Structures_Path ON Structures([Path]);
    CREATE INDEX IX_Structures_Code ON Structures(Code) WHERE Code IS NOT NULL;
    CREATE INDEX IX_Structures_Type ON Structures(StructureType);

    PRINT 'Created Structures table';
END
GO

-- =============================================
-- 2. STRUCTURE MEMBERS (Users in Structures)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StructureMembers')
BEGIN
    CREATE TABLE StructureMembers (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        StructureId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        IsPrimary BIT NOT NULL DEFAULT 0, -- User's primary/home structure
        Position NVARCHAR(200) NULL, -- User's position/title in this structure
        StartDate DATE NULL, -- When user joined this structure
        EndDate DATE NULL, -- When user left (NULL = still active)
        CreatedBy UNIQUEIDENTIFIER NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_StructureMembers_Structure FOREIGN KEY (StructureId) REFERENCES Structures(Id) ON DELETE CASCADE,
        CONSTRAINT FK_StructureMembers_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        CONSTRAINT UQ_StructureMember UNIQUE (StructureId, UserId)
    );

    CREATE INDEX IX_StructureMembers_StructureId ON StructureMembers(StructureId);
    CREATE INDEX IX_StructureMembers_UserId ON StructureMembers(UserId);
    CREATE INDEX IX_StructureMembers_Primary ON StructureMembers(UserId) WHERE IsPrimary = 1;

    PRINT 'Created StructureMembers table';
END
GO

-- =============================================
-- 3. ENHANCE PERMISSIONS TABLE
-- =============================================

-- Add new columns to Permissions table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'ExpiresAt')
BEGIN
    ALTER TABLE Permissions ADD ExpiresAt DATETIME2 NULL;
    PRINT 'Added ExpiresAt column to Permissions';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'IncludeChildStructures')
BEGIN
    ALTER TABLE Permissions ADD IncludeChildStructures BIT NOT NULL DEFAULT 1;
    PRINT 'Added IncludeChildStructures column to Permissions';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'GrantedReason')
BEGIN
    ALTER TABLE Permissions ADD GrantedReason NVARCHAR(500) NULL;
    PRINT 'Added GrantedReason column to Permissions';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Permissions') AND name = 'GrantedBy')
BEGIN
    ALTER TABLE Permissions ADD GrantedBy UNIQUEIDENTIFIER NULL;
    PRINT 'Added GrantedBy column to Permissions';
END
GO

-- Update PrincipalType to support Structure (add value 3)
-- PrincipalType: 1=User, 2=Role, 3=Structure
-- No schema change needed, just documentation

-- =============================================
-- 4. EFFECTIVE PERMISSIONS CACHE
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EffectivePermissions')
BEGIN
    CREATE TABLE EffectivePermissions (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        NodeType INT NOT NULL, -- 1=Cabinet, 2=Folder, 3=Document
        NodeId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        EffectiveLevel INT NOT NULL, -- Combined permission flags

        -- Source tracking for debugging/display
        SourceType NVARCHAR(50) NOT NULL, -- 'Direct', 'Role', 'Structure', 'Inherited'
        SourcePermissionId UNIQUEIDENTIFIER NULL, -- Which permission entry granted this
        SourceNodeType INT NULL, -- Where inherited from (if inherited)
        SourceNodeId UNIQUEIDENTIFIER NULL, -- Where inherited from (if inherited)
        InheritancePath NVARCHAR(1000) NULL, -- Full path for debugging: Cabinet > Folder > ...

        CalculatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ExpiresAt DATETIME2 NULL, -- When this cache entry should be recalculated

        CONSTRAINT FK_EffectivePermissions_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );

    -- Primary lookup index
    CREATE UNIQUE INDEX IX_EffectivePermissions_Lookup
        ON EffectivePermissions(NodeType, NodeId, UserId);

    -- For cache invalidation by node
    CREATE INDEX IX_EffectivePermissions_Node
        ON EffectivePermissions(NodeType, NodeId);

    -- For cache invalidation by user
    CREATE INDEX IX_EffectivePermissions_User
        ON EffectivePermissions(UserId);

    -- For cleanup of expired entries
    CREATE INDEX IX_EffectivePermissions_Expires
        ON EffectivePermissions(ExpiresAt) WHERE ExpiresAt IS NOT NULL;

    PRINT 'Created EffectivePermissions table';
END
GO

-- =============================================
-- 5. PERMISSION AUDIT LOG
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PermissionAuditLog')
BEGIN
    CREATE TABLE PermissionAuditLog (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),

        -- Action details
        Action NVARCHAR(50) NOT NULL, -- 'Grant', 'Revoke', 'Modify', 'BreakInheritance', 'RestoreInheritance'

        -- Target node
        NodeType INT NOT NULL,
        NodeId UNIQUEIDENTIFIER NOT NULL,
        NodeName NVARCHAR(255) NULL, -- Denormalized for historical record

        -- Principal affected
        PrincipalType INT NOT NULL, -- 1=User, 2=Role, 3=Structure
        PrincipalId UNIQUEIDENTIFIER NOT NULL,
        PrincipalName NVARCHAR(255) NULL, -- Denormalized for historical record

        -- Permission change details
        OldPermissionLevel INT NULL,
        NewPermissionLevel INT NULL,

        -- Context
        Reason NVARCHAR(500) NULL,

        -- Who performed the action
        PerformedBy UNIQUEIDENTIFIER NOT NULL,
        PerformedByName NVARCHAR(255) NULL, -- Denormalized
        PerformedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Additional context
        IPAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        SessionId NVARCHAR(100) NULL,

        -- For compliance
        IsSystemAction BIT NOT NULL DEFAULT 0 -- True if automated (inheritance, etc.)
    );

    CREATE INDEX IX_PermissionAuditLog_Node ON PermissionAuditLog(NodeType, NodeId);
    CREATE INDEX IX_PermissionAuditLog_Principal ON PermissionAuditLog(PrincipalType, PrincipalId);
    CREATE INDEX IX_PermissionAuditLog_PerformedBy ON PermissionAuditLog(PerformedBy);
    CREATE INDEX IX_PermissionAuditLog_PerformedAt ON PermissionAuditLog(PerformedAt DESC);
    CREATE INDEX IX_PermissionAuditLog_Action ON PermissionAuditLog(Action);

    PRINT 'Created PermissionAuditLog table';
END
GO

-- =============================================
-- 6. PERMISSION DELEGATION (Optional Feature)
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PermissionDelegations')
BEGIN
    CREATE TABLE PermissionDelegations (
        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        DelegatorId UNIQUEIDENTIFIER NOT NULL, -- User delegating their permissions
        DelegateId UNIQUEIDENTIFIER NOT NULL, -- User receiving delegated permissions

        -- Scope: NULL means all permissions, otherwise specific node
        NodeType INT NULL,
        NodeId UNIQUEIDENTIFIER NULL,

        -- What level is delegated
        PermissionLevel INT NOT NULL,

        -- Validity period
        StartDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        EndDate DATETIME2 NOT NULL, -- Must have an end date

        -- Status
        IsActive BIT NOT NULL DEFAULT 1,
        RevokedAt DATETIME2 NULL,
        RevokedBy UNIQUEIDENTIFIER NULL,

        Reason NVARCHAR(500) NULL,

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_PermissionDelegations_Delegator FOREIGN KEY (DelegatorId) REFERENCES Users(Id),
        CONSTRAINT FK_PermissionDelegations_Delegate FOREIGN KEY (DelegateId) REFERENCES Users(Id)
    );

    CREATE INDEX IX_PermissionDelegations_Delegator ON PermissionDelegations(DelegatorId);
    CREATE INDEX IX_PermissionDelegations_Delegate ON PermissionDelegations(DelegateId);
    CREATE INDEX IX_PermissionDelegations_Active ON PermissionDelegations(DelegateId, IsActive)
        WHERE IsActive = 1;
    CREATE INDEX IX_PermissionDelegations_EndDate ON PermissionDelegations(EndDate)
        WHERE IsActive = 1;

    PRINT 'Created PermissionDelegations table';
END
GO

-- =============================================
-- 7. HELPER VIEWS
-- =============================================

-- View: User's all principals (user + roles + structures)
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_UserPrincipals')
    DROP VIEW vw_UserPrincipals;
GO

CREATE VIEW vw_UserPrincipals AS
SELECT
    u.Id AS UserId,
    1 AS PrincipalType, -- User
    u.Id AS PrincipalId,
    u.DisplayName AS PrincipalName,
    0 AS [Level] -- Direct
FROM Users u
WHERE u.IsActive = 1

UNION ALL

SELECT
    ur.UserId,
    2 AS PrincipalType, -- Role
    ur.RoleId AS PrincipalId,
    r.Name AS PrincipalName,
    1 AS [Level] -- Via role
FROM UserRoles ur
INNER JOIN Roles r ON ur.RoleId = r.Id
WHERE r.IsActive = 1

UNION ALL

SELECT
    sm.UserId,
    3 AS PrincipalType, -- Structure
    sm.StructureId AS PrincipalId,
    s.Name AS PrincipalName,
    2 AS [Level] -- Via structure
FROM StructureMembers sm
INNER JOIN Structures s ON sm.StructureId = s.Id
WHERE s.IsActive = 1
  AND (sm.EndDate IS NULL OR sm.EndDate > GETDATE());
GO

PRINT 'Created vw_UserPrincipals view';
GO

-- View: Structure hierarchy with paths
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_StructureHierarchy')
    DROP VIEW vw_StructureHierarchy;
GO

CREATE VIEW vw_StructureHierarchy AS
WITH StructureCTE AS (
    -- Root structures (no parent)
    SELECT
        Id, ParentId, Name, NameAr, Code, StructureType,
        CAST('/' + CAST(Id AS NVARCHAR(36)) + '/' AS NVARCHAR(1000)) AS [Path],
        0 AS [Level],
        CAST(Name AS NVARCHAR(1000)) AS FullPath
    FROM Structures
    WHERE ParentId IS NULL AND IsActive = 1

    UNION ALL

    -- Child structures
    SELECT
        s.Id, s.ParentId, s.Name, s.NameAr, s.Code, s.StructureType,
        CAST(cte.[Path] + CAST(s.Id AS NVARCHAR(36)) + '/' AS NVARCHAR(1000)),
        cte.[Level] + 1,
        CAST(cte.FullPath + ' > ' + s.Name AS NVARCHAR(1000))
    FROM Structures s
    INNER JOIN StructureCTE cte ON s.ParentId = cte.Id
    WHERE s.IsActive = 1
)
SELECT * FROM StructureCTE;
GO

PRINT 'Created vw_StructureHierarchy view';
GO

-- =============================================
-- 8. STORED PROCEDURES FOR PERMISSION RESOLUTION
-- =============================================

-- Procedure: Get effective permission for a user on a node
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetEffectivePermission')
    DROP PROCEDURE sp_GetEffectivePermission;
GO

CREATE PROCEDURE sp_GetEffectivePermission
    @UserId UNIQUEIDENTIFIER,
    @NodeType INT,
    @NodeId UNIQUEIDENTIFIER,
    @EffectiveLevel INT OUTPUT,
    @SourceType NVARCHAR(50) OUTPUT,
    @SourceNodeId UNIQUEIDENTIFIER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentNodeType INT = @NodeType;
    DECLARE @CurrentNodeId UNIQUEIDENTIFIER = @NodeId;
    DECLARE @Level INT = 0;
    DECLARE @BreakInheritance BIT = 0;
    DECLARE @ParentId UNIQUEIDENTIFIER;

    -- Initialize outputs
    SET @EffectiveLevel = 0;
    SET @SourceType = NULL;
    SET @SourceNodeId = NULL;

    -- Get all user principals (user, roles, structures)
    CREATE TABLE #UserPrincipals (
        PrincipalType INT,
        PrincipalId UNIQUEIDENTIFIER,
        [Priority] INT -- Lower = higher priority
    );

    -- Direct user
    INSERT INTO #UserPrincipals VALUES (1, @UserId, 1);

    -- User's roles
    INSERT INTO #UserPrincipals (PrincipalType, PrincipalId, [Priority])
    SELECT 2, RoleId, 2
    FROM UserRoles WHERE UserId = @UserId;

    -- User's structures (including parent structures for hierarchy)
    INSERT INTO #UserPrincipals (PrincipalType, PrincipalId, [Priority])
    SELECT DISTINCT 3, s.Id, 3
    FROM StructureMembers sm
    INNER JOIN Structures s ON sm.StructureId = s.Id
    WHERE sm.UserId = @UserId
      AND (sm.EndDate IS NULL OR sm.EndDate > GETDATE())
      AND s.IsActive = 1;

    -- Walk up the node hierarchy
    WHILE @CurrentNodeId IS NOT NULL
    BEGIN
        -- Check for direct permissions on current node
        SELECT TOP 1
            @EffectiveLevel = p.PermissionLevel,
            @SourceType = CASE up.PrincipalType
                WHEN 1 THEN 'Direct'
                WHEN 2 THEN 'Role'
                WHEN 3 THEN 'Structure'
            END,
            @SourceNodeId = @CurrentNodeId
        FROM Permissions p
        INNER JOIN #UserPrincipals up ON p.PrincipalType = up.PrincipalType AND p.PrincipalId = up.PrincipalId
        WHERE p.NodeType = @CurrentNodeType
          AND p.NodeId = @CurrentNodeId
          AND (p.ExpiresAt IS NULL OR p.ExpiresAt > GETUTCDATE())
        ORDER BY up.[Priority], p.PermissionLevel DESC;

        -- If we found a permission, we're done (highest priority wins)
        IF @EffectiveLevel > 0
            BREAK;

        -- Check for break inheritance
        IF @CurrentNodeType = 1 -- Cabinet
        BEGIN
            SELECT @BreakInheritance = BreakInheritance FROM Cabinets WHERE Id = @CurrentNodeId;
            SET @ParentId = NULL; -- Cabinets have no parent
        END
        ELSE IF @CurrentNodeType = 2 -- Folder
        BEGIN
            SELECT @BreakInheritance = BreakInheritance, @ParentId = ParentId
            FROM Folders WHERE Id = @CurrentNodeId;

            IF @BreakInheritance = 1
                BREAK;

            -- Move to parent folder or cabinet
            IF @ParentId IS NOT NULL
            BEGIN
                SET @CurrentNodeId = @ParentId;
                -- Still a folder
            END
            ELSE
            BEGIN
                -- Move to cabinet
                SELECT @CurrentNodeId = CabinetId FROM Folders WHERE Id = @CurrentNodeId;
                SET @CurrentNodeType = 1;
            END
        END
        ELSE IF @CurrentNodeType = 3 -- Document
        BEGIN
            -- Move to parent folder
            SELECT @CurrentNodeId = FolderId FROM Documents WHERE Id = @CurrentNodeId;
            SET @CurrentNodeType = 2;
        END

        IF @CurrentNodeId IS NULL
            BREAK;

        SET @Level = @Level + 1;

        -- Safety limit
        IF @Level > 50
            BREAK;
    END

    -- If still no permission found but we traversed, mark as inherited
    IF @EffectiveLevel > 0 AND @SourceNodeId <> @NodeId
        SET @SourceType = 'Inherited';

    DROP TABLE #UserPrincipals;
END
GO

PRINT 'Created sp_GetEffectivePermission procedure';
GO

-- =============================================
-- 9. INDEXES FOR PERFORMANCE
-- =============================================

-- Ensure efficient permission lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Permissions_Lookup')
BEGIN
    CREATE INDEX IX_Permissions_Lookup
        ON Permissions(NodeType, NodeId, PrincipalType, PrincipalId)
        INCLUDE (PermissionLevel, ExpiresAt);
    PRINT 'Created IX_Permissions_Lookup index';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Permissions_Principal')
BEGIN
    CREATE INDEX IX_Permissions_Principal
        ON Permissions(PrincipalType, PrincipalId)
        INCLUDE (NodeType, NodeId, PermissionLevel);
    PRINT 'Created IX_Permissions_Principal index';
END
GO

-- =============================================
-- 10. SEED DATA: Default Structure Types
-- =============================================

-- Insert root organization if none exists
IF NOT EXISTS (SELECT 1 FROM Structures)
BEGIN
    DECLARE @RootId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO Structures (Id, ParentId, Name, NameAr, Code, StructureType, [Path], [Level], SortOrder)
    VALUES
        (@RootId, NULL, 'Organization', N'المنظمة', 'ORG', 1, '/' + CAST(@RootId AS NVARCHAR(36)) + '/', 0, 1);

    PRINT 'Inserted default root organization';
END
GO

PRINT '=========================================';
PRINT 'Migration 007 completed successfully';
PRINT 'Enterprise Permission System is ready';
PRINT '=========================================';
GO
