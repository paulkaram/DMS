using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CabinetsController : ControllerBase
{
    private readonly ICabinetService _cabinetService;
    private readonly IPermissionService _permissionService;
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IFolderTemplateService _templateService;

    public CabinetsController(
        ICabinetService cabinetService,
        IPermissionService permissionService,
        IRolePermissionService rolePermissionService,
        IFolderTemplateService templateService)
    {
        _cabinetService = cabinetService;
        _permissionService = permissionService;
        _rolePermissionService = rolePermissionService;
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var userId = GetCurrentUserId();

        var result = string.IsNullOrEmpty(search)
            ? await _cabinetService.GetAllAsync()
            : await _cabinetService.SearchAsync(search);

        if (!result.Success) return BadRequest(result.Errors);

        // Filter cabinets based on user's read permission
        var accessibleCabinets = new List<CabinetDto>();
        foreach (var cabinet in result.Data!)
        {
            if (await HasPermissionAsync(userId, "Cabinet", cabinet.Id, (int)PermissionLevel.Read))
                accessibleCabinets.Add(cabinet);
        }
        return Ok(accessibleCabinets);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check read permission on cabinet
        if (!await HasPermissionAsync(userId, "Cabinet", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this cabinet");

        var result = await _cabinetService.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCabinetDto dto)
    {
        var userId = GetCurrentUserId();

        // Check role-based permission for creating cabinets
        var canCreate = await _rolePermissionService.UserHasPermissionAsync(userId, "cabinet.create");
        if (!canCreate.Success || !canCreate.Data)
            return Forbid("You don't have permission to create cabinets");

        var result = await _cabinetService.CreateAsync(dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCabinetDto dto)
    {
        var userId = GetCurrentUserId();

        // Check role-based permission for managing cabinets
        var canManage = await _rolePermissionService.UserHasPermissionAsync(userId, "cabinet.manage");
        if (!canManage.Success || !canManage.Data)
            return Forbid("You don't have permission to manage cabinets");

        // Also check node-level admin permission
        if (!await HasPermissionAsync(userId, "Cabinet", id, (int)PermissionLevel.Admin))
            return Forbid("You don't have permission to update this cabinet");

        var result = await _cabinetService.UpdateAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check role-based permission for deleting cabinets
        var canDelete = await _rolePermissionService.UserHasPermissionAsync(userId, "cabinet.delete");
        if (!canDelete.Success || !canDelete.Data)
            return Forbid("You don't have permission to delete cabinets");

        // Also check node-level permission
        if (!await HasPermissionAsync(userId, "Cabinet", id, (int)PermissionLevel.Delete))
            return Forbid("You don't have permission to delete this cabinet");

        var result = await _cabinetService.DeleteAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    /// <summary>
    /// Apply a folder template to create a folder structure in a cabinet
    /// </summary>
    [HttpPost("{id:guid}/apply-template")]
    public async Task<IActionResult> ApplyTemplate(Guid id, [FromBody] ApplyTemplateDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write permission on cabinet
        if (!await HasPermissionAsync(userId, "Cabinet", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to create folders in this cabinet");

        var result = await _templateService.ApplyTemplateToCabinetAsync(id, dto, userId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Preview applying a folder template to a cabinet
    /// </summary>
    [HttpPost("{id:guid}/preview-template")]
    public async Task<IActionResult> PreviewTemplate(Guid id, [FromBody] CabinetPreviewTemplateRequest request)
    {
        var userId = GetCurrentUserId();

        // Check read permission on cabinet
        if (!await HasPermissionAsync(userId, "Cabinet", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this cabinet");

        var result = await _templateService.PreviewTemplateToCabinetAsync(id, request.TemplateId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("Administrator");

    private async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        // Admin users bypass permission checks - they have full access to everything
        if (IsAdmin())
            return true;

        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }
}

public class CabinetPreviewTemplateRequest
{
    public Guid TemplateId { get; set; }
}
