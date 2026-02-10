using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class CabinetsController : BaseApiController
{
    private readonly ICabinetService _cabinetService;
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IFolderTemplateService _templateService;

    public CabinetsController(
        ICabinetService cabinetService,
        IRolePermissionService rolePermissionService,
        IFolderTemplateService templateService)
    {
        _cabinetService = cabinetService;
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
            return Forbid(ErrorMessages.Permissions.ViewCabinet);

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
            return Forbid(ErrorMessages.Permissions.CreateCabinet);

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
            return Forbid(ErrorMessages.Permissions.ManageCabinet);

        // Also check node-level admin permission
        if (!await HasPermissionAsync(userId, "Cabinet", id, (int)PermissionLevel.Admin))
            return Forbid(ErrorMessages.Permissions.UpdateCabinet);

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
            return Forbid(ErrorMessages.Permissions.DeleteCabinet);

        // Also check node-level permission
        if (!await HasPermissionAsync(userId, "Cabinet", id, (int)PermissionLevel.Delete))
            return Forbid(ErrorMessages.Permissions.DeleteThisCabinet);

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
            return Forbid(ErrorMessages.Permissions.CreateFoldersInCabinet);

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
            return Forbid(ErrorMessages.Permissions.ViewCabinet);

        var result = await _templateService.PreviewTemplateToCabinetAsync(id, request.TemplateId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }
}
