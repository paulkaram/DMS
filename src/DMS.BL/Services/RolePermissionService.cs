using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class RolePermissionService : IRolePermissionService
{
    private readonly IRolePermissionRepository _rolePermissionRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRepository _userRepo;

    public RolePermissionService(
        IRolePermissionRepository rolePermissionRepo,
        IRoleRepository roleRepo,
        IUserRepository userRepo)
    {
        _rolePermissionRepo = rolePermissionRepo;
        _roleRepo = roleRepo;
        _userRepo = userRepo;
    }

    #region System Actions

    public async Task<ServiceResult<List<SystemActionDto>>> GetAllActionsAsync(bool includeInactive = false)
    {
        var actions = await _rolePermissionRepo.GetAllActionsAsync(includeInactive);
        return ServiceResult<List<SystemActionDto>>.Ok(actions.Select(MapActionToDto).ToList());
    }

    public async Task<ServiceResult<List<SystemActionDto>>> GetActionsByCategoryAsync(string category)
    {
        var actions = await _rolePermissionRepo.GetActionsByCategoryAsync(category);
        return ServiceResult<List<SystemActionDto>>.Ok(actions.Select(MapActionToDto).ToList());
    }

    public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
    {
        var categories = await _rolePermissionRepo.GetCategoriesAsync();
        return ServiceResult<List<string>>.Ok(categories.ToList());
    }

    #endregion

    #region Role Permissions

    public async Task<ServiceResult<RolePermissionMatrixDto>> GetRolePermissionsAsync(Guid roleId)
    {
        var role = await _roleRepo.GetByIdAsync(roleId);
        if (role == null)
            return ServiceResult<RolePermissionMatrixDto>.Fail("Role not found");

        var permissions = await _rolePermissionRepo.GetPermissionsByRoleAsync(roleId);
        var allActions = await _rolePermissionRepo.GetAllActionsAsync();

        var dto = new RolePermissionMatrixDto
        {
            RoleId = role.Id,
            RoleName = role.Name,
            RoleDescription = role.Description,
            AllowedActionCodes = permissions.Select(p => p.ActionCode!).ToList(),
            AllowedActions = permissions
                .Select(p => allActions.FirstOrDefault(a => a.Id == p.ActionId))
                .Where(a => a != null)
                .Select(a => MapActionToDto(a!))
                .ToList()
        };

        return ServiceResult<RolePermissionMatrixDto>.Ok(dto);
    }

    public async Task<ServiceResult<List<RolePermissionMatrixDto>>> GetAllRolePermissionsAsync()
    {
        var roles = await _roleRepo.GetAllAsync();
        var allPermissions = await _rolePermissionRepo.GetAllPermissionsAsync();
        var allActions = await _rolePermissionRepo.GetAllActionsAsync();

        var result = new List<RolePermissionMatrixDto>();

        foreach (var role in roles)
        {
            var rolePerms = allPermissions.Where(p => p.RoleId == role.Id).ToList();
            result.Add(new RolePermissionMatrixDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                RoleDescription = role.Description,
                AllowedActionCodes = rolePerms.Select(p => p.ActionCode!).ToList(),
                AllowedActions = rolePerms
                    .Select(p => allActions.FirstOrDefault(a => a.Id == p.ActionId))
                    .Where(a => a != null)
                    .Select(a => MapActionToDto(a!))
                    .ToList()
            });
        }

        return ServiceResult<List<RolePermissionMatrixDto>>.Ok(result);
    }

    public async Task<ServiceResult> SetRolePermissionsAsync(Guid roleId, SetRolePermissionsDto dto, Guid grantedBy)
    {
        var role = await _roleRepo.GetByIdAsync(roleId);
        if (role == null)
            return ServiceResult.Fail("Role not found");

        var success = await _rolePermissionRepo.SetRolePermissionsAsync(roleId, dto.ActionIds, grantedBy);

        return success
            ? ServiceResult.Ok("Permissions updated successfully")
            : ServiceResult.Fail("Failed to update permissions");
    }

    #endregion

    #region Permission Checks

    public async Task<ServiceResult<bool>> UserHasPermissionAsync(Guid userId, string actionCode)
    {
        var hasPermission = await _rolePermissionRepo.UserHasPermissionAsync(userId, actionCode);
        return ServiceResult<bool>.Ok(hasPermission);
    }

    public async Task<ServiceResult<UserPermissionsDto>> GetUserPermissionsAsync(Guid userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ServiceResult<UserPermissionsDto>.Fail("User not found");

        var roles = await _roleRepo.GetByUserIdAsync(userId);
        var allowedActions = await _rolePermissionRepo.GetUserAllowedActionsAsync(userId);

        var dto = new UserPermissionsDto
        {
            UserId = userId,
            Username = user.Username,
            Roles = roles.Select(r => r.Name).ToList(),
            AllowedActionCodes = allowedActions.ToList()
        };

        return ServiceResult<UserPermissionsDto>.Ok(dto);
    }

    public async Task<ServiceResult<List<string>>> GetUserAllowedActionsAsync(Guid userId)
    {
        var allowedActions = await _rolePermissionRepo.GetUserAllowedActionsAsync(userId);
        return ServiceResult<List<string>>.Ok(allowedActions.ToList());
    }

    #endregion

    #region Mapping

    private static SystemActionDto MapActionToDto(SystemAction action)
    {
        return new SystemActionDto
        {
            Id = action.Id,
            Code = action.Code,
            Name = action.Name,
            Description = action.Description,
            Category = action.Category,
            SortOrder = action.SortOrder,
            IsActive = action.IsActive
        };
    }

    #endregion
}
