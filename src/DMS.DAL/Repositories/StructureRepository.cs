using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class StructureRepository : IStructureRepository
{
    private readonly DmsDbContext _context;

    public StructureRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Structure?> GetByIdAsync(Guid id)
    {
        return await _context.Structures
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Structure?> GetByCodeAsync(string code)
    {
        return await _context.Structures
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == code);
    }

    public async Task<IEnumerable<Structure>> GetAllAsync(bool includeInactive = false, int maxResults = 5000)
    {
        var query = _context.Structures.AsNoTracking();

        if (includeInactive)
            query = query.IgnoreQueryFilters();

        return await query
            .OrderBy(s => s.Level)
            .ThenBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(Structure entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

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

        _context.Structures.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Structure entity)
    {
        entity.ModifiedAt = DateTime.Now;

        var existing = await _context.Structures.FindAsync(entity.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(entity);
        var affected = await _context.SaveChangesAsync();

        // Update paths if parent changed
        if (affected > 0)
            await UpdatePathsAsync(entity.Id);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.Structures.FindAsync(id);
        if (entity == null) return false;

        // Soft delete - mark as inactive
        entity.IsActive = false;
        entity.ModifiedAt = DateTime.Now;
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<IEnumerable<Structure>> GetChildrenAsync(Guid parentId)
    {
        return await _context.Structures
            .AsNoTracking()
            .Where(s => s.ParentId == parentId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Structure>> GetDescendantsAsync(Guid parentId, int maxResults = 5000)
    {
        var parent = await GetByIdAsync(parentId);
        if (parent == null) return Enumerable.Empty<Structure>();

        var pathPattern = $"{parent.Path}%";
        return await _context.Structures
            .AsNoTracking()
            .Where(s => EF.Functions.Like(s.Path!, pathPattern) && s.Id != parentId)
            .OrderBy(s => s.Level)
            .ThenBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Take(maxResults)
            .ToListAsync();
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

        return await _context.Structures
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => ancestorIds.Contains(s.Id))
            .OrderBy(s => s.Level)
            .ToListAsync();
    }

    public async Task<IEnumerable<Structure>> GetRootStructuresAsync()
    {
        return await _context.Structures
            .AsNoTracking()
            .Where(s => s.ParentId == null)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Structure>> GetByTypeAsync(StructureType type)
    {
        return await _context.Structures
            .AsNoTracking()
            .Where(s => s.StructureType == type)
            .OrderBy(s => s.Level)
            .ThenBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    // Member management
    public async Task<IEnumerable<StructureMember>> GetMembersAsync(Guid structureId)
    {
        return await _context.StructureMembers
            .AsNoTracking()
            .Include(sm => sm.User)
            .Where(sm => sm.StructureId == structureId
                && (sm.EndDate == null || sm.EndDate > DateTime.Now))
            .OrderByDescending(sm => sm.IsPrimary)
            .ThenBy(sm => sm.User!.DisplayName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Structure>> GetUserStructuresAsync(Guid userId)
    {
        return await _context.Structures
            .AsNoTracking()
            .Where(s => _context.StructureMembers
                .Any(sm => sm.StructureId == s.Id
                    && sm.UserId == userId
                    && (sm.EndDate == null || sm.EndDate > DateTime.Now)))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Structure?> GetUserPrimaryStructureAsync(Guid userId)
    {
        return await _context.Structures
            .AsNoTracking()
            .Where(s => _context.StructureMembers
                .Any(sm => sm.StructureId == s.Id
                    && sm.UserId == userId
                    && sm.IsPrimary
                    && (sm.EndDate == null || sm.EndDate > DateTime.Now)))
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> AddMemberAsync(StructureMember member)
    {
        member.Id = Guid.NewGuid();
        member.CreatedAt = DateTime.Now;

        // If this is primary, clear other primary flags
        if (member.IsPrimary)
        {
            await _context.StructureMembers
                .Where(sm => sm.UserId == member.UserId && sm.IsPrimary)
                .ExecuteUpdateAsync(s => s.SetProperty(sm => sm.IsPrimary, false));
        }

        _context.StructureMembers.Add(member);
        await _context.SaveChangesAsync();

        return member.Id;
    }

    public async Task<bool> RemoveMemberAsync(Guid structureId, Guid userId)
    {
        // Soft delete by setting end date
        var member = await _context.StructureMembers
            .FirstOrDefaultAsync(sm => sm.StructureId == structureId
                && sm.UserId == userId
                && sm.EndDate == null);

        if (member == null) return false;

        member.EndDate = DateTime.Now;
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> SetPrimaryStructureAsync(Guid userId, Guid structureId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Clear all primary flags for user
                await _context.StructureMembers
                    .Where(sm => sm.UserId == userId && sm.IsPrimary)
                    .ExecuteUpdateAsync(s => s.SetProperty(sm => sm.IsPrimary, false));

                // Set new primary
                var targetMember = await _context.StructureMembers
                    .FirstOrDefaultAsync(sm => sm.UserId == userId && sm.StructureId == structureId);

                if (targetMember == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                targetMember.IsPrimary = true;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task UpdatePathsAsync(Guid structureId)
    {
        var structure = await _context.Structures
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == structureId);
        if (structure == null) return;

        // Recalculate root path
        string newPath;
        int newLevel;

        if (structure.ParentId.HasValue)
        {
            var parent = await _context.Structures
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == structure.ParentId.Value);
            newPath = $"{parent?.Path}{structure.Id}/";
            newLevel = (parent?.Level ?? 0) + 1;
        }
        else
        {
            newPath = $"/{structure.Id}/";
            newLevel = 0;
        }

        // Update root structure
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE Structures SET [Path] = {0}, [Level] = {1} WHERE Id = {2}",
            newPath, newLevel, structureId);

        // Load ALL descendants in one query, sorted by Level (top-down)
        var oldPathPrefix = structure.Path ?? $"/{structure.Id}/";
        var descendants = await _context.Structures
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => EF.Functions.Like(s.Path!, $"{oldPathPrefix}%") && s.Id != structureId)
            .OrderBy(s => s.Level)
            .ToListAsync();

        if (descendants.Count == 0) return;

        // Build parent path lookup: start with the root we just updated
        var pathLookup = new Dictionary<Guid, (string Path, int Level)>
        {
            [structureId] = (newPath, newLevel)
        };

        // Iterate top-down, computing each descendant's path from its parent
        foreach (var desc in descendants)
        {
            var parentPath = desc.ParentId.HasValue && pathLookup.TryGetValue(desc.ParentId.Value, out var parentInfo)
                ? parentInfo.Path
                : newPath;
            var parentLevel = desc.ParentId.HasValue && pathLookup.TryGetValue(desc.ParentId.Value, out var parentLevelInfo)
                ? parentLevelInfo.Level
                : newLevel;

            var descPath = $"{parentPath}{desc.Id}/";
            var descLevel = parentLevel + 1;

            pathLookup[desc.Id] = (descPath, descLevel);

            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Structures SET [Path] = {0}, [Level] = {1} WHERE Id = {2}",
                descPath, descLevel, desc.Id);
        }
    }
}

public class EffectivePermissionRepository : IEffectivePermissionRepository
{
    private readonly DmsDbContext _context;

    public EffectivePermissionRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<EffectivePermission?> GetAsync(NodeType nodeType, Guid nodeId, Guid userId)
    {
        return await _context.EffectivePermissions
            .AsNoTracking()
            .FirstOrDefaultAsync(ep => ep.NodeType == nodeType
                && ep.NodeId == nodeId
                && ep.UserId == userId
                && (ep.ExpiresAt == null || ep.ExpiresAt > DateTime.Now));
    }

    public async Task<IEnumerable<EffectivePermission>> GetByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        return await _context.EffectivePermissions
            .AsNoTracking()
            .Where(ep => ep.NodeType == nodeType && ep.NodeId == nodeId)
            .ToListAsync();
    }

    public async Task<IEnumerable<EffectivePermission>> GetByUserAsync(Guid userId)
    {
        return await _context.EffectivePermissions
            .AsNoTracking()
            .Where(ep => ep.UserId == userId)
            .ToListAsync();
    }

    public async Task<Guid> UpsertAsync(EffectivePermission entity)
    {
        // Try to find existing
        var existing = await _context.EffectivePermissions
            .FirstOrDefaultAsync(ep => ep.NodeType == entity.NodeType
                && ep.NodeId == entity.NodeId
                && ep.UserId == entity.UserId
                && (ep.ExpiresAt == null || ep.ExpiresAt > DateTime.Now));

        if (existing != null)
        {
            entity.Id = existing.Id;
            entity.CalculatedAt = DateTime.Now;

            existing.EffectiveLevel = entity.EffectiveLevel;
            existing.SourceType = entity.SourceType;
            existing.SourcePermissionId = entity.SourcePermissionId;
            existing.SourceNodeType = entity.SourceNodeType;
            existing.SourceNodeId = entity.SourceNodeId;
            existing.InheritancePath = entity.InheritancePath;
            existing.CalculatedAt = entity.CalculatedAt;
            existing.ExpiresAt = entity.ExpiresAt;

            await _context.SaveChangesAsync();
            return entity.Id;
        }

        entity.Id = Guid.NewGuid();
        entity.CalculatedAt = DateTime.Now;

        _context.EffectivePermissions.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task InvalidateByNodeAsync(NodeType nodeType, Guid nodeId)
    {
        await _context.EffectivePermissions
            .Where(ep => ep.NodeType == nodeType && ep.NodeId == nodeId)
            .ExecuteDeleteAsync();
    }

    public async Task InvalidateByUserAsync(Guid userId)
    {
        await _context.EffectivePermissions
            .Where(ep => ep.UserId == userId)
            .ExecuteDeleteAsync();
    }

    public async Task InvalidateByPrincipalAsync(PrincipalType principalType, Guid principalId)
    {
        if (principalType == PrincipalType.User)
        {
            await InvalidateByUserAsync(principalId);
        }
        else if (principalType == PrincipalType.Role)
        {
            // Invalidate all users in this role
            var userIds = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.RoleId == principalId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            await _context.EffectivePermissions
                .Where(ep => userIds.Contains(ep.UserId))
                .ExecuteDeleteAsync();
        }
        else if (principalType == PrincipalType.Structure)
        {
            // Invalidate all users in this structure
            var userIds = await _context.StructureMembers
                .AsNoTracking()
                .Where(sm => sm.StructureId == principalId
                    && (sm.EndDate == null || sm.EndDate > DateTime.Now))
                .Select(sm => sm.UserId)
                .ToListAsync();

            await _context.EffectivePermissions
                .Where(ep => userIds.Contains(ep.UserId))
                .ExecuteDeleteAsync();
        }
    }

    public async Task CleanupExpiredAsync()
    {
        await _context.EffectivePermissions
            .Where(ep => ep.ExpiresAt < DateTime.Now)
            .ExecuteDeleteAsync();
    }
}

public class PermissionAuditRepository : IPermissionAuditRepository
{
    private readonly DmsDbContext _context;

    public PermissionAuditRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> LogAsync(PermissionAuditLog entry)
    {
        entry.Id = Guid.NewGuid();
        entry.PerformedAt = DateTime.Now;

        _context.PermissionAuditLogs.Add(entry);
        await _context.SaveChangesAsync();

        return entry.Id;
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int take = 100)
    {
        return await _context.PermissionAuditLogs
            .AsNoTracking()
            .Where(l => l.NodeType == nodeType && l.NodeId == nodeId)
            .OrderByDescending(l => l.PerformedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetByPrincipalAsync(PrincipalType principalType, Guid principalId, int take = 100)
    {
        return await _context.PermissionAuditLogs
            .AsNoTracking()
            .Where(l => l.PrincipalType == principalType && l.PrincipalId == principalId)
            .OrderByDescending(l => l.PerformedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetByPerformerAsync(Guid userId, int take = 100)
    {
        return await _context.PermissionAuditLogs
            .AsNoTracking()
            .Where(l => l.PerformedBy == userId)
            .OrderByDescending(l => l.PerformedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionAuditLog>> GetRecentAsync(int take = 100)
    {
        return await _context.PermissionAuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.PerformedAt)
            .Take(take)
            .ToListAsync();
    }
}

public class PermissionDelegationRepository : IPermissionDelegationRepository
{
    private readonly DmsDbContext _context;

    public PermissionDelegationRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<PermissionDelegation?> GetByIdAsync(Guid id)
    {
        return await _context.PermissionDelegations
            .AsNoTracking()
            .FirstOrDefaultAsync(pd => pd.Id == id);
    }

    public async Task<IEnumerable<PermissionDelegation>> GetByDelegatorAsync(Guid delegatorId)
    {
        return await _context.PermissionDelegations
            .AsNoTracking()
            .Where(pd => pd.DelegatorId == delegatorId)
            .OrderByDescending(pd => pd.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionDelegation>> GetByDelegateAsync(Guid delegateId)
    {
        return await _context.PermissionDelegations
            .AsNoTracking()
            .Where(pd => pd.DelegateId == delegateId)
            .OrderByDescending(pd => pd.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PermissionDelegation>> GetActiveByDelegateAsync(Guid delegateId)
    {
        return await _context.PermissionDelegations
            .AsNoTracking()
            .Where(pd => pd.DelegateId == delegateId
                && pd.IsActive
                && pd.StartDate <= DateTime.Now
                && pd.EndDate > DateTime.Now)
            .OrderByDescending(pd => pd.CreatedAt)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(PermissionDelegation entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.PermissionDelegations.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> RevokeAsync(Guid id, Guid revokedBy)
    {
        var entity = await _context.PermissionDelegations.FindAsync(id);
        if (entity == null) return false;

        entity.IsActive = false;
        entity.RevokedAt = DateTime.Now;
        entity.RevokedBy = revokedBy;

        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task ExpireOldDelegationsAsync()
    {
        await _context.PermissionDelegations
            .Where(pd => pd.IsActive && pd.EndDate < DateTime.Now)
            .ExecuteUpdateAsync(s => s.SetProperty(pd => pd.IsActive, false));
    }
}
