using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IRolePermissionRepository
{
    // System Actions
    Task<IEnumerable<SystemAction>> GetAllActionsAsync(bool includeInactive = false);
    Task<SystemAction?> GetActionByIdAsync(Guid id);
    Task<SystemAction?> GetActionByCodeAsync(string code);
    Task<IEnumerable<SystemAction>> GetActionsByCategoryAsync(string category);
    Task<IEnumerable<string>> GetCategoriesAsync();

    // Role Action Permissions
    Task<IEnumerable<RoleActionPermission>> GetPermissionsByRoleAsync(Guid roleId);
    Task<IEnumerable<RoleActionPermission>> GetAllPermissionsAsync();
    Task<bool> HasPermissionAsync(Guid roleId, string actionCode);
    Task<bool> UserHasPermissionAsync(Guid userId, string actionCode);
    Task<IEnumerable<string>> GetUserAllowedActionsAsync(Guid userId);

    // Permission Management
    Task<bool> GrantPermissionAsync(Guid roleId, Guid actionId, Guid? grantedBy = null);
    Task<bool> RevokePermissionAsync(Guid roleId, Guid actionId);
    Task<bool> SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> actionIds, Guid? grantedBy = null);
}
