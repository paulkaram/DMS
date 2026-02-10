using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class PermissionsController : BaseApiController
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    #region Basic Permission Operations

    /// <summary>
    /// Get all permissions for a node (cabinet, folder, or document)
    /// </summary>
    [HttpGet("{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> GetNodePermissions(string nodeType, Guid nodeId)
    {
        var result = await _permissionService.GetNodePermissionsAsync(nodeType, nodeId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Grant a permission to a principal (user, role, or structure)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GrantPermission([FromBody] CreatePermissionDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.GrantPermissionAsync(dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetNodePermissions), new { nodeType = dto.NodeType, nodeId = dto.NodeId }, result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Update an existing permission
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePermission(Guid id, [FromBody] UpdatePermissionDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.UpdatePermissionAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Revoke a permission
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> RevokePermission(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.RevokePermissionAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    #endregion

    #region Permission Checking

    /// <summary>
    /// Check if current user has a specific permission level on a node
    /// </summary>
    [HttpGet("check/{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> CheckPermission(string nodeType, Guid nodeId, [FromQuery] int level = 1)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, level);
        return result.Success ? Ok(new { hasPermission = result.Data }) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get effective permission details for current user on a node
    /// </summary>
    [HttpGet("effective/{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> GetEffectivePermission(string nodeType, Guid nodeId)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.GetEffectivePermissionAsync(userId, nodeType, nodeId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get current user's permission level on a node (simple integer response)
    /// </summary>
    [HttpGet("my-level/{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> GetMyPermissionLevel(string nodeType, Guid nodeId)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.GetMyPermissionLevelAsync(userId, nodeType, nodeId);
        return result.Success ? Ok(new { level = result.Data }) : BadRequest(result.Errors);
    }

    #endregion

    #region Inheritance Management

    /// <summary>
    /// Break permission inheritance on a node
    /// </summary>
    [HttpPost("{nodeType}/{nodeId:guid}/break-inheritance")]
    public async Task<IActionResult> BreakInheritance(string nodeType, Guid nodeId, [FromQuery] bool copyPermissions = true)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.BreakInheritanceAsync(nodeType, nodeId, userId, copyPermissions);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Restore permission inheritance on a node
    /// </summary>
    [HttpPost("{nodeType}/{nodeId:guid}/restore-inheritance")]
    public async Task<IActionResult> RestoreInheritance(string nodeType, Guid nodeId)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.RestoreInheritanceAsync(nodeType, nodeId, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    #endregion

    #region Delegation Operations

    /// <summary>
    /// Create a permission delegation
    /// </summary>
    [HttpPost("delegations")]
    public async Task<IActionResult> CreateDelegation([FromBody] CreatePermissionDelegationDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.CreateDelegationAsync(dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetMyDelegations), result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Revoke a permission delegation
    /// </summary>
    [HttpDelete("delegations/{id:guid}")]
    public async Task<IActionResult> RevokeDelegation(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.RevokeDelegationAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get delegations created by current user
    /// </summary>
    [HttpGet("delegations/my")]
    public async Task<IActionResult> GetMyDelegations()
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.GetMyDelegationsAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get delegations where current user is the delegate
    /// </summary>
    [HttpGet("delegations/to-me")]
    public async Task<IActionResult> GetDelegationsToMe()
    {
        var userId = GetCurrentUserId();
        var result = await _permissionService.GetDelegationsToMeAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    #endregion

    #region Audit Trail

    /// <summary>
    /// Get permission audit log for a node
    /// </summary>
    [HttpGet("audit/{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> GetNodeAudit(string nodeType, Guid nodeId, [FromQuery] int take = 100)
    {
        var result = await _permissionService.GetNodePermissionAuditAsync(nodeType, nodeId, take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get permission audit log for a principal
    /// </summary>
    [HttpGet("audit/principal/{principalType}/{principalId:guid}")]
    public async Task<IActionResult> GetPrincipalAudit(string principalType, Guid principalId, [FromQuery] int take = 100)
    {
        var result = await _permissionService.GetPrincipalPermissionAuditAsync(principalType, principalId, take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    #endregion

    #region Cache Management

    /// <summary>
    /// Invalidate permission cache for a node (admin only)
    /// </summary>
    [HttpPost("cache/invalidate/{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> InvalidateCache(string nodeType, Guid nodeId)
    {
        await _permissionService.InvalidatePermissionCacheAsync(nodeType, nodeId);
        return Ok("Cache invalidated");
    }

    /// <summary>
    /// Invalidate permission cache for a user (admin only)
    /// </summary>
    [HttpPost("cache/invalidate/user/{userId:guid}")]
    public async Task<IActionResult> InvalidateUserCache(Guid userId)
    {
        await _permissionService.InvalidateUserPermissionCacheAsync(userId);
        return Ok("User cache invalidated");
    }

    #endregion
}
