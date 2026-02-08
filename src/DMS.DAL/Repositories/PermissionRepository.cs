using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using System.Data;

namespace DMS.DAL.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Permission?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Permission>(
            "SELECT * FROM Permissions WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Permission>> GetByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Permission>(
            "SELECT * FROM Permissions WHERE NodeType = @NodeType AND NodeId = @NodeId",
            new { NodeType = (int)nodeType, NodeId = nodeId });
    }

    public async Task<IEnumerable<Permission>> GetByPrincipalAsync(PrincipalType principalType, Guid principalId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Permission>(
            "SELECT * FROM Permissions WHERE PrincipalType = @PrincipalType AND PrincipalId = @PrincipalId",
            new { PrincipalType = (int)principalType, PrincipalId = principalId });
    }

    public async Task<IEnumerable<Permission>> GetByNodeWithPrincipalNamesAsync(NodeType nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        // This returns permissions with principal names denormalized
        var sql = @"
            SELECT p.*,
                CASE p.PrincipalType
                    WHEN 1 THEN u.DisplayName
                    WHEN 2 THEN r.Name
                    WHEN 3 THEN s.Name
                END AS PrincipalName
            FROM Permissions p
            LEFT JOIN Users u ON p.PrincipalType = 1 AND p.PrincipalId = u.Id
            LEFT JOIN Roles r ON p.PrincipalType = 2 AND p.PrincipalId = r.Id
            LEFT JOIN Structures s ON p.PrincipalType = 3 AND p.PrincipalId = s.Id
            WHERE p.NodeType = @NodeType AND p.NodeId = @NodeId
            ORDER BY p.PrincipalType, p.PermissionLevel DESC";

        return await connection.QueryAsync<Permission>(sql,
            new { NodeType = (int)nodeType, NodeId = nodeId });
    }

    public async Task<PermissionLevel> GetEffectivePermissionAsync(Guid userId, NodeType nodeType, Guid nodeId)
    {
        var (level, _, _) = await GetEffectivePermissionWithSourceAsync(userId, nodeType, nodeId);
        return level;
    }

    public async Task<(PermissionLevel Level, string SourceType, Guid? SourceNodeId)> GetEffectivePermissionWithSourceAsync(
        Guid userId, NodeType nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Use the stored procedure for permission resolution
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@NodeType", (int)nodeType);
        parameters.Add("@NodeId", nodeId);
        parameters.Add("@EffectiveLevel", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("@SourceType", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        parameters.Add("@SourceNodeId", dbType: DbType.Guid, direction: ParameterDirection.Output);

        await connection.ExecuteAsync("sp_GetEffectivePermission", parameters, commandType: CommandType.StoredProcedure);

        var level = (PermissionLevel)(parameters.Get<int?>("@EffectiveLevel") ?? 0);
        var sourceType = parameters.Get<string>("@SourceType") ?? "None";
        var sourceNodeId = parameters.Get<Guid?>("@SourceNodeId");

        return (level, sourceType, sourceNodeId);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, NodeType nodeType, Guid nodeId, PermissionLevel requiredLevel)
    {
        var effectiveLevel = await GetEffectivePermissionAsync(userId, nodeType, nodeId);
        return ((int)effectiveLevel & (int)requiredLevel) == (int)requiredLevel;
    }

    public async Task<IEnumerable<(PrincipalType Type, Guid Id)>> GetUserPrincipalsAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        var results = new List<(PrincipalType, Guid)>();

        // User themselves
        results.Add((PrincipalType.User, userId));

        // User's roles
        var roles = await connection.QueryAsync<Guid>(
            "SELECT RoleId FROM UserRoles WHERE UserId = @UserId",
            new { UserId = userId });
        results.AddRange(roles.Select(r => (PrincipalType.Role, r)));

        // User's structures
        var structures = await connection.QueryAsync<Guid>(@"
            SELECT StructureId FROM StructureMembers
            WHERE UserId = @UserId AND (EndDate IS NULL OR EndDate > GETDATE())",
            new { UserId = userId });
        results.AddRange(structures.Select(s => (PrincipalType.Structure, s)));

        return results;
    }

    public async Task<Guid> CreateAsync(Permission entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Permissions (Id, NodeType, NodeId, PrincipalType, PrincipalId, PermissionLevel, IsInherited, IncludeChildStructures, ExpiresAt, GrantedReason, GrantedBy, CreatedBy, CreatedAt)
            VALUES (@Id, @NodeType, @NodeId, @PrincipalType, @PrincipalId, @PermissionLevel, @IsInherited, @IncludeChildStructures, @ExpiresAt, @GrantedReason, @GrantedBy, @CreatedBy, @CreatedAt)",
            new
            {
                entity.Id,
                NodeType = (int)entity.NodeType,
                entity.NodeId,
                PrincipalType = (int)entity.PrincipalType,
                entity.PrincipalId,
                PermissionLevel = (int)entity.PermissionLevel,
                entity.IsInherited,
                entity.IncludeChildStructures,
                entity.ExpiresAt,
                entity.GrantedReason,
                entity.GrantedBy,
                entity.CreatedBy,
                entity.CreatedAt
            });

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Permission entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Permissions SET
                PermissionLevel = @PermissionLevel,
                IncludeChildStructures = @IncludeChildStructures,
                ExpiresAt = @ExpiresAt,
                GrantedReason = @GrantedReason
            WHERE Id = @Id",
            new
            {
                entity.Id,
                PermissionLevel = (int)entity.PermissionLevel,
                entity.IncludeChildStructures,
                entity.ExpiresAt,
                entity.GrantedReason
            });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM Permissions WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<bool> DeleteByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM Permissions WHERE NodeType = @NodeType AND NodeId = @NodeId AND IsInherited = 0",
            new { NodeType = (int)nodeType, NodeId = nodeId });
        return affected > 0;
    }

    public async Task<int> DeleteExpiredPermissionsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM Permissions WHERE ExpiresAt IS NOT NULL AND ExpiresAt < GETUTCDATE()");
    }
}
