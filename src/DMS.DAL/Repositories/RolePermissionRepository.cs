using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly DmsDbContext _context;

    public RolePermissionRepository(DmsDbContext context)
    {
        _context = context;
    }

    #region System Actions

    public async Task<IEnumerable<SystemAction>> GetAllActionsAsync(bool includeInactive = false)
    {
        var query = _context.SystemActions.AsNoTracking();

        if (!includeInactive)
            query = query.Where(sa => sa.IsActive);

        return await query
            .OrderBy(sa => sa.Category)
            .ThenBy(sa => sa.SortOrder)
            .ToListAsync();
    }

    public async Task<SystemAction?> GetActionByIdAsync(Guid id)
    {
        return await _context.SystemActions
            .AsNoTracking()
            .FirstOrDefaultAsync(sa => sa.Id == id);
    }

    public async Task<SystemAction?> GetActionByCodeAsync(string code)
    {
        return await _context.SystemActions
            .AsNoTracking()
            .FirstOrDefaultAsync(sa => sa.Code == code);
    }

    public async Task<IEnumerable<SystemAction>> GetActionsByCategoryAsync(string category)
    {
        return await _context.SystemActions
            .AsNoTracking()
            .Where(sa => sa.Category == category && sa.IsActive)
            .OrderBy(sa => sa.SortOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        return await _context.SystemActions
            .AsNoTracking()
            .Where(sa => sa.IsActive)
            .Select(sa => sa.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    #endregion

    #region Role Action Permissions

    public async Task<IEnumerable<RoleActionPermission>> GetPermissionsByRoleAsync(Guid roleId)
    {
        var permissions = await _context.RoleActionPermissions
            .AsNoTracking()
            .Where(rap => rap.RoleId == roleId && rap.IsAllowed)
            .ToListAsync();

        // Populate navigation properties from separate queries
        var roleIds = permissions.Select(p => p.RoleId).Distinct().ToList();
        var actionIds = permissions.Select(p => p.ActionId).Distinct().ToList();

        var roles = await _context.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        var actions = await _context.SystemActions.AsNoTracking()
            .Where(sa => actionIds.Contains(sa.Id))
            .ToDictionaryAsync(sa => sa.Id, sa => new { sa.Code, sa.Name, sa.Category, sa.SortOrder });

        foreach (var p in permissions)
        {
            if (roles.TryGetValue(p.RoleId, out var roleName))
                p.RoleName = roleName;
            if (actions.TryGetValue(p.ActionId, out var action))
            {
                p.ActionCode = action.Code;
                p.ActionName = action.Name;
                p.ActionCategory = action.Category;
            }
        }

        return permissions
            .OrderBy(p => p.ActionCategory)
            .ThenBy(p => actions.TryGetValue(p.ActionId, out var a) ? a.SortOrder : 0);
    }

    public async Task<IEnumerable<RoleActionPermission>> GetAllPermissionsAsync()
    {
        var permissions = await _context.RoleActionPermissions
            .AsNoTracking()
            .Where(rap => rap.IsAllowed)
            .ToListAsync();

        // Populate navigation properties from separate queries
        var roleIds = permissions.Select(p => p.RoleId).Distinct().ToList();
        var actionIds = permissions.Select(p => p.ActionId).Distinct().ToList();

        var roles = await _context.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        var actions = await _context.SystemActions.AsNoTracking()
            .Where(sa => actionIds.Contains(sa.Id))
            .ToDictionaryAsync(sa => sa.Id, sa => new { sa.Code, sa.Name, sa.Category, sa.SortOrder });

        foreach (var p in permissions)
        {
            if (roles.TryGetValue(p.RoleId, out var roleName))
                p.RoleName = roleName;
            if (actions.TryGetValue(p.ActionId, out var action))
            {
                p.ActionCode = action.Code;
                p.ActionName = action.Name;
                p.ActionCategory = action.Category;
            }
        }

        return permissions
            .OrderBy(p => p.RoleName)
            .ThenBy(p => p.ActionCategory)
            .ThenBy(p => actions.TryGetValue(p.ActionId, out var a) ? a.SortOrder : 0);
    }

    public async Task<bool> HasPermissionAsync(Guid roleId, string actionCode)
    {
        return await _context.RoleActionPermissions
            .AsNoTracking()
            .AnyAsync(rap => rap.RoleId == roleId
                && rap.IsAllowed
                && _context.SystemActions.Any(sa => sa.Id == rap.ActionId && sa.Code == actionCode));
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string actionCode)
    {
        return await _context.RoleActionPermissions
            .AsNoTracking()
            .AnyAsync(rap => rap.IsAllowed
                && _context.UserRoles.Any(ur => ur.UserId == userId && ur.RoleId == rap.RoleId)
                && _context.SystemActions.Any(sa => sa.Id == rap.ActionId && sa.Code == actionCode));
    }

    public async Task<IEnumerable<string>> GetUserAllowedActionsAsync(Guid userId)
    {
        var userRoleIds = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var allowedActionIds = await _context.RoleActionPermissions
            .AsNoTracking()
            .Where(rap => userRoleIds.Contains(rap.RoleId) && rap.IsAllowed)
            .Select(rap => rap.ActionId)
            .Distinct()
            .ToListAsync();

        return await _context.SystemActions
            .AsNoTracking()
            .Where(sa => allowedActionIds.Contains(sa.Id) && sa.IsActive)
            .Select(sa => sa.Code)
            .Distinct()
            .ToListAsync();
    }

    #endregion

    #region Permission Management

    public async Task<bool> GrantPermissionAsync(Guid roleId, Guid actionId, Guid? grantedBy = null)
    {
        var existing = await _context.RoleActionPermissions
            .FirstOrDefaultAsync(rap => rap.RoleId == roleId && rap.ActionId == actionId);

        if (existing != null)
        {
            existing.IsAllowed = true;
            existing.GrantedBy = grantedBy;
            existing.GrantedAt = DateTime.UtcNow;
        }
        else
        {
            _context.RoleActionPermissions.Add(new RoleActionPermission
            {
                RoleId = roleId,
                ActionId = actionId,
                IsAllowed = true,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.UtcNow
            });
        }

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> RevokePermissionAsync(Guid roleId, Guid actionId)
    {
        return await _context.RoleActionPermissions
            .Where(rap => rap.RoleId == roleId && rap.ActionId == actionId)
            .ExecuteDeleteAsync() > 0;
    }

    public async Task<bool> SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> actionIds, Guid? grantedBy = null)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Remove all existing permissions for this role
                await _context.RoleActionPermissions
                    .Where(rap => rap.RoleId == roleId)
                    .ExecuteDeleteAsync();

                // Add new permissions
                var now = DateTime.UtcNow;
                foreach (var actionId in actionIds)
                {
                    _context.RoleActionPermissions.Add(new RoleActionPermission
                    {
                        RoleId = roleId,
                        ActionId = actionId,
                        IsAllowed = true,
                        GrantedBy = grantedBy,
                        GrantedAt = now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        });
    }

    #endregion
}
