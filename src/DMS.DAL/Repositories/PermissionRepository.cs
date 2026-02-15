using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace DMS.DAL.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly DmsDbContext _context;

    public PermissionRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(Guid id)
    {
        return await _context.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Permission>> GetByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        return await _context.Permissions
            .AsNoTracking()
            .Where(p => p.NodeType == nodeType && p.NodeId == nodeId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetByPrincipalAsync(PrincipalType principalType, Guid principalId)
    {
        return await _context.Permissions
            .AsNoTracking()
            .Where(p => p.PrincipalType == principalType && p.PrincipalId == principalId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetByNodeWithPrincipalNamesAsync(NodeType nodeType, Guid nodeId)
    {
        // Use raw SQL to get permissions with denormalized principal names from JOINs.
        // The PrincipalName column is not mapped on the Permission entity (Dapper silently dropped it),
        // so we keep the same behavior: return permissions ordered by PrincipalType and PermissionLevel.
        return await _context.Permissions
            .AsNoTracking()
            .Where(p => p.NodeType == nodeType && p.NodeId == nodeId)
            .OrderBy(p => p.PrincipalType)
            .ThenByDescending(p => p.PermissionLevel)
            .ToListAsync();
    }

    public async Task<PermissionLevel> GetEffectivePermissionAsync(Guid userId, NodeType nodeType, Guid nodeId)
    {
        var (level, _, _) = await GetEffectivePermissionWithSourceAsync(userId, nodeType, nodeId);
        return level;
    }

    public async Task<(PermissionLevel Level, string SourceType, Guid? SourceNodeId)> GetEffectivePermissionWithSourceAsync(
        Guid userId, NodeType nodeType, Guid nodeId)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "sp_GetEffectivePermission";
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.Add(new SqlParameter("@UserId", userId));
        command.Parameters.Add(new SqlParameter("@NodeType", (int)nodeType));
        command.Parameters.Add(new SqlParameter("@NodeId", nodeId));

        var effectiveLevelParam = new SqlParameter("@EffectiveLevel", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(effectiveLevelParam);

        var sourceTypeParam = new SqlParameter("@SourceType", SqlDbType.NVarChar, 50)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(sourceTypeParam);

        var sourceNodeIdParam = new SqlParameter("@SourceNodeId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(sourceNodeIdParam);

        await command.ExecuteNonQueryAsync();

        var level = (PermissionLevel)(effectiveLevelParam.Value is DBNull ? 0 : (int)effectiveLevelParam.Value);
        var sourceType = sourceTypeParam.Value is DBNull ? "None" : (string)sourceTypeParam.Value;
        var sourceNodeId = sourceNodeIdParam.Value is DBNull ? (Guid?)null : (Guid)sourceNodeIdParam.Value;

        return (level, sourceType, sourceNodeId);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, NodeType nodeType, Guid nodeId, PermissionLevel requiredLevel)
    {
        var effectiveLevel = await GetEffectivePermissionAsync(userId, nodeType, nodeId);
        return ((int)effectiveLevel & (int)requiredLevel) == (int)requiredLevel;
    }

    public async Task<IEnumerable<(PrincipalType Type, Guid Id)>> GetUserPrincipalsAsync(Guid userId)
    {
        var results = new List<(PrincipalType, Guid)>();

        // User themselves
        results.Add((PrincipalType.User, userId));

        // User's roles
        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();
        results.AddRange(roles.Select(r => (PrincipalType.Role, r)));

        // User's structures
        var structures = await _context.StructureMembers
            .AsNoTracking()
            .Where(sm => sm.UserId == userId
                && (sm.EndDate == null || sm.EndDate > DateTime.UtcNow))
            .Select(sm => sm.StructureId)
            .ToListAsync();
        results.AddRange(structures.Select(s => (PrincipalType.Structure, s)));

        return results;
    }

    public async Task<Guid> CreateAsync(Permission entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        _context.Permissions.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Permission entity)
    {
        var existing = await _context.Permissions.FindAsync(entity.Id);
        if (existing == null) return false;

        existing.PermissionLevel = entity.PermissionLevel;
        existing.IncludeChildStructures = entity.IncludeChildStructures;
        existing.ExpiresAt = entity.ExpiresAt;
        existing.GrantedReason = entity.GrantedReason;

        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.Permissions
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        var affected = await _context.Permissions
            .Where(p => p.NodeType == nodeType && p.NodeId == nodeId && !p.IsInherited)
            .ExecuteDeleteAsync();
        return affected > 0;
    }

    public async Task<int> DeleteExpiredPermissionsAsync()
    {
        return await _context.Permissions
            .Where(p => p.ExpiresAt != null && p.ExpiresAt < DateTime.UtcNow)
            .ExecuteDeleteAsync();
    }
}
