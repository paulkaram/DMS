using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IRolePermissionService
{
    // System Actions
    Task<ServiceResult<List<SystemActionDto>>> GetAllActionsAsync(bool includeInactive = false);
    Task<ServiceResult<List<SystemActionDto>>> GetActionsByCategoryAsync(string category);
    Task<ServiceResult<List<string>>> GetCategoriesAsync();

    // Role Permissions
    Task<ServiceResult<RolePermissionMatrixDto>> GetRolePermissionsAsync(Guid roleId);
    Task<ServiceResult<List<RolePermissionMatrixDto>>> GetAllRolePermissionsAsync();
    Task<ServiceResult> SetRolePermissionsAsync(Guid roleId, SetRolePermissionsDto dto, Guid grantedBy);

    // Permission Checks
    Task<ServiceResult<bool>> UserHasPermissionAsync(Guid userId, string actionCode);
    Task<ServiceResult<UserPermissionsDto>> GetUserPermissionsAsync(Guid userId);
    Task<ServiceResult<List<string>>> GetUserAllowedActionsAsync(Guid userId);
}
