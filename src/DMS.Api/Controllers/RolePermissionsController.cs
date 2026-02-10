using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

/// <summary>
/// Role-based permission matrix management.
/// </summary>
[Route("api/role-permissions")]
[Authorize]
public class RolePermissionsController : BaseApiController
{
    private readonly IRolePermissionService _rolePermissionService;

    public RolePermissionsController(IRolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    #region System Actions

    /// <summary>
    /// Get all system actions.
    /// </summary>
    [HttpGet("actions")]
    public async Task<IActionResult> GetAllActions([FromQuery] bool includeInactive = false)
    {
        var result = await _rolePermissionService.GetAllActionsAsync(includeInactive);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get actions by category.
    /// </summary>
    [HttpGet("actions/category/{category}")]
    public async Task<IActionResult> GetActionsByCategory(string category)
    {
        var result = await _rolePermissionService.GetActionsByCategoryAsync(category);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get all action categories.
    /// </summary>
    [HttpGet("actions/categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _rolePermissionService.GetCategoriesAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    #endregion

    #region Role Permissions

    /// <summary>
    /// Get permission matrix for all roles.
    /// </summary>
    [HttpGet("matrix")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetAllRolePermissions()
    {
        var result = await _rolePermissionService.GetAllRolePermissionsAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get permissions for a specific role.
    /// </summary>
    [HttpGet("roles/{roleId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        var result = await _rolePermissionService.GetRolePermissionsAsync(roleId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Set permissions for a role.
    /// </summary>
    [HttpPut("roles/{roleId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> SetRolePermissions(Guid roleId, [FromBody] SetRolePermissionsDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _rolePermissionService.SetRolePermissionsAsync(roleId, dto, userId);
        return result.Success ? Ok(new { message = result.Message }) : BadRequest(result.Errors);
    }

    #endregion

    #region User Permissions

    /// <summary>
    /// Get current user's permissions.
    /// </summary>
    [HttpGet("my-permissions")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var userId = GetCurrentUserId();
        var result = await _rolePermissionService.GetUserPermissionsAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get current user's allowed action codes.
    /// </summary>
    [HttpGet("my-actions")]
    public async Task<IActionResult> GetMyAllowedActions()
    {
        var userId = GetCurrentUserId();
        var result = await _rolePermissionService.GetUserAllowedActionsAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Check if current user has a specific permission.
    /// </summary>
    [HttpGet("check/{actionCode}")]
    public async Task<IActionResult> CheckPermission(string actionCode)
    {
        var userId = GetCurrentUserId();
        var result = await _rolePermissionService.UserHasPermissionAsync(userId, actionCode);
        return result.Success ? Ok(new { hasPermission = result.Data }) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get permissions for a specific user (admin only).
    /// </summary>
    [HttpGet("users/{userId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetUserPermissions(Guid userId)
    {
        var result = await _rolePermissionService.GetUserPermissionsAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    #endregion
}
