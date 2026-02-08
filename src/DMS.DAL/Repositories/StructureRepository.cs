using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class StructureRepository : IStructureRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public StructureRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Structure?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Structure>(
            "SELECT * FROM Structures WHERE Id = @Id", new { Id = id });
    }

    public async Task<Structure?> GetByCodeAsync(string code)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Structure>(
            "SELECT * FROM Structures WHERE Code = @Code", new { Code = code });
    }

    public async Task<IEnumerable<Structure>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = includeInactive
            ? "SELECT * FROM Structures ORDER BY [Level], SortOrder, Name"
            : "SELECT * FROM Structures WHERE IsActive = 1 ORDER BY [Level], SortOrder, Name";
        return await connection.QueryAsync<Structure>(sql);
    }

    public async Task<Guid> CreateAsync(Structure entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        // Calculate level based on parent
        if (entity.ParentId.HasValue)
        {
            var parent = await GetByIdAsync(entity.ParentId.Value);
            entity.Level = (parent?.Level ?? 0) + 1;
            entity.Path = $"{parent?.Path}{entity.Id}/";
        }
        else
        {
            entity.Level = 0;
            entity.Path = $"/{entity.Id}/";
        }

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Structures (Id, ParentId, Name, NameAr, Code, StructureType, [Path], [Level], ManagerId, IsActive, SortOrder, Description, CreatedBy, CreatedAt)
            VALUES (@Id, @ParentId, @Name, @NameAr, @Code, @StructureType, @Path, @Level, @ManagerId, @IsActive, @SortOrder, @Description, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Structure entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Structures SET
                ParentId = @ParentId,
                Name = @Name,
                NameAr = @NameAr,
                Code = @Code,
                StructureType = @StructureType,
                ManagerId = @ManagerId,
                IsActive = @IsActive,
                SortOrder = @SortOrder,
                Description = @Description,
                ModifiedBy = @ModifiedBy,
                ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            entity);

        // Update paths if parent changed
        if (affected > 0)
            await UpdatePathsAsync(entity.Id);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        // Soft delete - mark as inactive
        var affected = await connection.ExecuteAsync(
            "UPDATE Structures SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id",
            new { Id = id, Now = DateTime.UtcNow });
        return affected > 0;
    }

    public async Task<IEnumerable<Structure>> GetChildrenAsync(Guid parentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Structure>(
            "SELECT * FROM Structures WHERE ParentId = @ParentId AND IsActive = 1 ORDER BY SortOrder, Name",
            new { ParentId = parentId });
    }

    public async Task<IEnumerable<Structure>> GetDescendantsAsync(Guid parentId)
    {
        var parent = await GetByIdAsync(parentId);
        if (parent == null) return Enumerable.Empty<Structure>();

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Structure>(
            "SELECT * FROM Structures WHERE [Path] LIKE @PathPattern AND Id <> @ParentId AND IsActive = 1 ORDER BY [Level], SortOrder, Name",
            new { PathPattern = $"{parent.Path}%", ParentId = parentId });
    }

    public async Task<IEnumerable<Structure>> GetAncestorsAsync(Guid structureId)
    {
        var structure = await GetByIdAsync(structureId);
        if (structure == null || string.IsNullOrEmpty(structure.Path))
            return Enumerable.Empty<Structure>();

        // Parse path to get ancestor IDs
        var ancestorIds = structure.Path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .Where(id => id != structureId)
            .ToList();

        if (!ancestorIds.Any()) return Enumerable.Empty<Structure>();

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Structure>(
            "SELECT * FROM Structures WHERE Id IN @Ids ORDER BY [Level]",
            new { Ids = ancestorIds });
    }

    public async Task<IEnumerable<Structure>> GetRootStructuresAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Structure>(
            "SELECT * FROM Structures WHERE ParentId IS NULL AND IsActive = 1 ORDER BY SortOrder, Name");
    }

    public async Task<IEnumerable<Structure>> GetByTypeAsync(StructureType type)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Structure>(
            "SELECT * FROM Structures WHERE StructureType = @Type AND IsActive = 1 ORDER BY [Level], SortOrder, Name",
            new { Type = (int)type });
    }

    // Member management
    public async Task<IEnumerable<StructureMember>> GetMembersAsync(Guid structureId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<StructureMember>(@"
            SELECT sm.*, u.DisplayName, u.Username, u.Email
            FROM StructureMembers sm
            INNER JOIN Users u ON sm.UserId = u.Id
            WHERE sm.StructureId = @StructureId
              AND (sm.EndDate IS NULL OR sm.EndDate > GETDATE())
            ORDER BY sm.IsPrimary DESC, u.DisplayName",
            new { StructureId = structureId });
    }

    public async Task<IEnumerable<Structure>> GetUserStructuresAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Structure>(@"
            SELECT s.*
            FROM Structures s
            INNER JOIN StructureMembers sm ON s.Id = sm.StructureId
            WHERE sm.UserId = @UserId
              AND (sm.EndDate IS NULL OR sm.EndDate > GETDATE())
              AND s.IsActive = 1
            ORDER BY sm.IsPrimary DESC, s.Name",
            new { UserId = userId });
    }

    public async Task<Structure?> GetUserPrimaryStructureAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Structure>(@"
            SELECT s.*
            FROM Structures s
            INNER JOIN StructureMembers sm ON s.Id = sm.StructureId
            WHERE sm.UserId = @UserId AND sm.IsPrimary = 1
              AND (sm.EndDate IS NULL OR sm.EndDate > GETDATE())
              AND s.IsActive = 1",
            new { UserId = userId });
    }

    public async Task<Guid> AddMemberAsync(StructureMember member)
    {
        member.Id = Guid.NewGuid();
        member.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();

        // If this is primary, clear other primary flags
        if (member.IsPrimary)
        {
            await connection.ExecuteAsync(
                "UPDATE StructureMembers SET IsPrimary = 0 WHERE UserId = @UserId",
                new { member.UserId });
        }

        await connection.ExecuteAsync(@"
            INSERT INTO StructureMembers (Id, StructureId, UserId, IsPrimary, Position, StartDate, EndDate, CreatedBy, CreatedAt)
            VALUES (@Id, @StructureId, @UserId, @IsPrimary, @Position, @StartDate, @EndDate, @CreatedBy, @CreatedAt)",
            member);

        return member.Id;
    }

    public async Task<bool> RemoveMemberAsync(Guid structureId, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        // Soft delete by setting end date
        var affected = await connection.ExecuteAsync(@"
            UPDATE StructureMembers
            SET EndDate = @Now
            WHERE StructureId = @StructureId AND UserId = @UserId AND EndDate IS NULL",
            new { StructureId = structureId, UserId = userId, Now = DateTime.UtcNow });
        return affected > 0;
    }

    public async Task<bool> SetPrimaryStructureAsync(Guid userId, Guid structureId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Clear all primary flags for user
            await connection.ExecuteAsync(
                "UPDATE StructureMembers SET IsPrimary = 0 WHERE UserId = @UserId",
                new { UserId = userId }, transaction);

            // Set new primary
            var affected = await connection.ExecuteAsync(
                "UPDATE StructureMembers SET IsPrimary = 1 WHERE UserId = @UserId AND StructureId = @StructureId",
                new { UserId = userId, StructureId = structureId }, transaction);

            transaction.Commit();
            return affected > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdatePathsAsync(Guid structureId)
    {
        var structure = await GetByIdAsync(structureId);
        if (structure == null) return;

        // Recalculate path
        string newPath;
        int newLevel;

        if (structure.ParentId.HasValue)
        {
            var parent = await GetByIdAsync(structure.ParentId.Value);
            newPath = $"{parent?.Path}{structure.Id}/";
            newLevel = (parent?.Level ?? 0) + 1;
        }
        else
        {
            newPath = $"/{structure.Id}/";
            newLevel = 0;
        }

        using var connection = _connectionFactory.CreateConnection();

        // Update this structure
        await connection.ExecuteAsync(
            "UPDATE Structures SET [Path] = @Path, [Level] = @Level WHERE Id = @Id",
            new { Path = newPath, Level = newLevel, Id = structureId });

        // Update all descendants
        var descendants = await GetDescendantsAsync(structureId);
        foreach (var descendant in descendants)
        {
            await UpdatePathsAsync(descendant.Id);
        }
    }
}

public class EffectivePermissionRepository : IEffectivePermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EffectivePermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<EffectivePermission?> GetAsync(NodeType nodeType, Guid nodeId, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EffectivePermission>(@"
            SELECT * FROM EffectivePermissions
            WHERE NodeType = @NodeType AND NodeId = @NodeId AND UserId = @UserId
              AND (ExpiresAt IS NULL OR ExpiresAt > GETUTCDATE())",
            new { NodeType = (int)nodeType, NodeId = nodeId, UserId = userId });
    }

    public async Task<IEnumerable<EffectivePermission>> GetByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<EffectivePermission>(@"
            SELECT * FROM EffectivePermissions
            WHERE NodeType = @NodeType AND NodeId = @NodeId",
            new { NodeType = (int)nodeType, NodeId = nodeId });
    }

    public async Task<IEnumerable<EffectivePermission>> GetByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<EffectivePermission>(
            "SELECT * FROM EffectivePermissions WHERE UserId = @UserId",
            new { UserId = userId });
    }

    public async Task<Guid> UpsertAsync(EffectivePermission entity)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Try to find existing
        var existing = await GetAsync(entity.NodeType, entity.NodeId, entity.UserId);

        if (existing != null)
        {
            entity.Id = existing.Id;
            entity.CalculatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(@"
                UPDATE EffectivePermissions SET
                    EffectiveLevel = @EffectiveLevel,
                    SourceType = @SourceType,
                    SourcePermissionId = @SourcePermissionId,
                    SourceNodeType = @SourceNodeType,
                    SourceNodeId = @SourceNodeId,
                    InheritancePath = @InheritancePath,
                    CalculatedAt = @CalculatedAt,
                    ExpiresAt = @ExpiresAt
                WHERE Id = @Id",
                new
                {
                    entity.Id,
                    EffectiveLevel = (int)entity.EffectiveLevel,
                    entity.SourceType,
                    entity.SourcePermissionId,
                    SourceNodeType = entity.SourceNodeType.HasValue ? (int?)entity.SourceNodeType : null,
                    entity.SourceNodeId,
                    entity.InheritancePath,
                    entity.CalculatedAt,
                    entity.ExpiresAt
                });

            return entity.Id;
        }

        entity.Id = Guid.NewGuid();
        entity.CalculatedAt = DateTime.UtcNow;

        await connection.ExecuteAsync(@"
            INSERT INTO EffectivePermissions (Id, NodeType, NodeId, UserId, EffectiveLevel, SourceType, SourcePermissionId, SourceNodeType, SourceNodeId, InheritancePath, CalculatedAt, ExpiresAt)
            VALUES (@Id, @NodeType, @NodeId, @UserId, @EffectiveLevel, @SourceType, @SourcePermissionId, @SourceNodeType, @SourceNodeId, @InheritancePath, @CalculatedAt, @ExpiresAt)",
            new
            {
                entity.Id,
                NodeType = (int)entity.NodeType,
                entity.NodeId,
                entity.UserId,
                EffectiveLevel = (int)entity.EffectiveLevel,
                entity.SourceType,
                entity.SourcePermissionId,
                SourceNodeType = entity.SourceNodeType.HasValue ? (int?)entity.SourceNodeType : null,
                entity.SourceNodeId,
                entity.InheritancePath,
                entity.CalculatedAt,
                entity.ExpiresAt
            });

        return entity.Id;
    }

    public async Task InvalidateByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM EffectivePermissions WHERE NodeType = @NodeType AND NodeId = @NodeId",
            new { NodeType = (int)nodeType, NodeId = nodeId });
    }

    public async Task InvalidateByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM EffectivePermissions WHERE UserId = @UserId",
            new { UserId = userId });
    }

    public async Task InvalidateByPrincipalAsync(PrincipalType principalType, Guid principalId)
    {
        using var connection = _connectionFactory.CreateConnection();

        if (principalType == PrincipalType.User)
        {
            await InvalidateByUserAsync(principalId);
        }
        else if (principalType == PrincipalType.Role)
        {
            // Invalidate all users in this role
            await connection.ExecuteAsync(@"
                DELETE FROM EffectivePermissions
                WHERE UserId IN (SELECT UserId FROM UserRoles WHERE RoleId = @RoleId)",
                new { RoleId = principalId });
        }
        else if (principalType == PrincipalType.Structure)
        {
            // Invalidate all users in this structure
            await connection.ExecuteAsync(@"
                DELETE FROM EffectivePermissions
                WHERE UserId IN (SELECT UserId FROM StructureMembers WHERE StructureId = @StructureId AND (EndDate IS NULL OR EndDate > GETDATE()))",
                new { StructureId = principalId });
        }
    }

    public async Task CleanupExpiredAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM EffectivePermissions WHERE ExpiresAt < GETUTCDATE()");
    }
}

public class PermissionAuditRepository : IPermissionAuditRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionAuditRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> LogAsync(PermissionAuditLog entry)
    {
        entry.Id = Guid.NewGuid();
        entry.PerformedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO PermissionAuditLog (Id, Action, NodeType, NodeId, NodeName, PrincipalType, PrincipalId, PrincipalName, OldPermissionLevel, NewPermissionLevel, Reason, PerformedBy, PerformedByName, PerformedAt, IPAddress, UserAgent, SessionId, IsSystemAction)
            VALUES (@Id, @Action, @NodeType, @NodeId, @NodeName, @PrincipalType, @PrincipalId, @PrincipalName, @OldPermissionLevel, @NewPermissionLevel, @Reason, @PerformedBy, @PerformedByName, @PerformedAt, @IPAddress, @UserAgent, @SessionId, @IsSystemAction)",
            new
            {
                entry.Id,
                entry.Action,
                NodeType = (int)entry.NodeType,
                entry.NodeId,
                entry.NodeName,
                PrincipalType = (int)entry.PrincipalType,
                entry.PrincipalId,
                entry.PrincipalName,
                entry.OldPermissionLevel,
                entry.NewPermissionLevel,
                entry.Reason,
                entry.PerformedBy,
                entry.PerformedByName,
                entry.PerformedAt,
                entry.IPAddress,
                entry.UserAgent,
                entry.SessionId,
                entry.IsSystemAction
            });

        return entry.Id;
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int take = 100)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionAuditLog>(@"
            SELECT TOP (@Take) * FROM PermissionAuditLog
            WHERE NodeType = @NodeType AND NodeId = @NodeId
            ORDER BY PerformedAt DESC",
            new { Take = take, NodeType = (int)nodeType, NodeId = nodeId });
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetByPrincipalAsync(PrincipalType principalType, Guid principalId, int take = 100)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionAuditLog>(@"
            SELECT TOP (@Take) * FROM PermissionAuditLog
            WHERE PrincipalType = @PrincipalType AND PrincipalId = @PrincipalId
            ORDER BY PerformedAt DESC",
            new { Take = take, PrincipalType = (int)principalType, PrincipalId = principalId });
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetByPerformerAsync(Guid userId, int take = 100)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionAuditLog>(@"
            SELECT TOP (@Take) * FROM PermissionAuditLog
            WHERE PerformedBy = @UserId
            ORDER BY PerformedAt DESC",
            new { Take = take, UserId = userId });
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetRecentAsync(int take = 100)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionAuditLog>(@"
            SELECT TOP (@Take) * FROM PermissionAuditLog
            ORDER BY PerformedAt DESC",
            new { Take = take });
    }
}

public class PermissionDelegationRepository : IPermissionDelegationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionDelegationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PermissionDelegation?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<PermissionDelegation>(
            "SELECT * FROM PermissionDelegations WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<PermissionDelegation>> GetByDelegatorAsync(Guid delegatorId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionDelegation>(
            "SELECT * FROM PermissionDelegations WHERE DelegatorId = @DelegatorId ORDER BY CreatedAt DESC",
            new { DelegatorId = delegatorId });
    }

    public async Task<IEnumerable<PermissionDelegation>> GetByDelegateAsync(Guid delegateId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionDelegation>(
            "SELECT * FROM PermissionDelegations WHERE DelegateId = @DelegateId ORDER BY CreatedAt DESC",
            new { DelegateId = delegateId });
    }

    public async Task<IEnumerable<PermissionDelegation>> GetActiveByDelegateAsync(Guid delegateId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PermissionDelegation>(@"
            SELECT * FROM PermissionDelegations
            WHERE DelegateId = @DelegateId
              AND IsActive = 1
              AND StartDate <= GETUTCDATE()
              AND EndDate > GETUTCDATE()
            ORDER BY CreatedAt DESC",
            new { DelegateId = delegateId });
    }

    public async Task<Guid> CreateAsync(PermissionDelegation entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO PermissionDelegations (Id, DelegatorId, DelegateId, NodeType, NodeId, PermissionLevel, StartDate, EndDate, IsActive, Reason, CreatedAt)
            VALUES (@Id, @DelegatorId, @DelegateId, @NodeType, @NodeId, @PermissionLevel, @StartDate, @EndDate, @IsActive, @Reason, @CreatedAt)",
            new
            {
                entity.Id,
                entity.DelegatorId,
                entity.DelegateId,
                NodeType = entity.NodeType.HasValue ? (int?)entity.NodeType : null,
                entity.NodeId,
                PermissionLevel = (int)entity.PermissionLevel,
                entity.StartDate,
                entity.EndDate,
                entity.IsActive,
                entity.Reason,
                entity.CreatedAt
            });

        return entity.Id;
    }

    public async Task<bool> RevokeAsync(Guid id, Guid revokedBy)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE PermissionDelegations
            SET IsActive = 0, RevokedAt = @Now, RevokedBy = @RevokedBy
            WHERE Id = @Id",
            new { Id = id, Now = DateTime.UtcNow, RevokedBy = revokedBy });
        return affected > 0;
    }

    public async Task ExpireOldDelegationsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE PermissionDelegations
            SET IsActive = 0
            WHERE IsActive = 1 AND EndDate < GETUTCDATE()");
    }
}
