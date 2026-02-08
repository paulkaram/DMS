SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
    DECLARE @ParentFolderId UNIQUEIDENTIFIER;
    DECLARE @CabinetId UNIQUEIDENTIFIER;

    -- Initialize outputs
    SET @EffectiveLevel = 0;
    SET @SourceType = NULL;
    SET @SourceNodeId = NULL;

    -- Get all user principals
    CREATE TABLE #UserPrincipals (
        PrincipalType INT,
        PrincipalId UNIQUEIDENTIFIER,
        [Priority] INT
    );

    -- Direct user
    INSERT INTO #UserPrincipals VALUES (1, @UserId, 1);

    -- User's roles
    INSERT INTO #UserPrincipals (PrincipalType, PrincipalId, [Priority])
    SELECT 2, RoleId, 2 FROM UserRoles WHERE UserId = @UserId;

    -- User's structures
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
        -- Check for permissions on current node
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

        -- If found, exit
        IF @EffectiveLevel > 0
            BREAK;

        -- Move to parent
        IF @CurrentNodeType = 1 -- Cabinet
        BEGIN
            SELECT @BreakInheritance = BreakInheritance FROM Cabinets WHERE Id = @CurrentNodeId;
            SET @CurrentNodeId = NULL; -- Cabinets have no parent
        END
        ELSE IF @CurrentNodeType = 2 -- Folder
        BEGIN
            SELECT @BreakInheritance = BreakInheritance,
                   @ParentFolderId = ParentFolderId,
                   @CabinetId = CabinetId
            FROM Folders WHERE Id = @CurrentNodeId;

            IF @BreakInheritance = 1
                BREAK;

            IF @ParentFolderId IS NOT NULL
                SET @CurrentNodeId = @ParentFolderId;
            ELSE
            BEGIN
                SET @CurrentNodeId = @CabinetId;
                SET @CurrentNodeType = 1;
            END
        END
        ELSE IF @CurrentNodeType = 3 -- Document
        BEGIN
            SELECT @CurrentNodeId = FolderId FROM Documents WHERE Id = @CurrentNodeId;
            SET @CurrentNodeType = 2;
        END

        SET @Level = @Level + 1;

        -- Safety limit
        IF @Level > 50
            BREAK;
    END

    -- Mark as inherited if source is different from original node
    IF @EffectiveLevel > 0 AND @SourceNodeId <> @NodeId
        SET @SourceType = 'Inherited';

    DROP TABLE #UserPrincipals;
END
GO

PRINT 'Stored procedure sp_GetEffectivePermission created successfully';
GO
