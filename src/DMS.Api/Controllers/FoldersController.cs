using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : ControllerBase
{
    private readonly IFolderService _folderService;
    private readonly IPermissionService _permissionService;
    private readonly IFolderTemplateService _templateService;

    public FoldersController(
        IFolderService folderService,
        IPermissionService permissionService,
        IFolderTemplateService templateService)
    {
        _folderService = folderService;
        _permissionService = permissionService;
        _templateService = templateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? cabinetId, [FromQuery] Guid? parentId, [FromQuery] string? search)
    {
        var userId = GetCurrentUserId();

        if (!string.IsNullOrEmpty(search))
        {
            var searchResult = await _folderService.SearchAsync(search, cabinetId);
            if (!searchResult.Success) return BadRequest(searchResult.Errors);

            // Filter results based on user's read permission
            var accessibleFolders = new List<FolderDto>();
            foreach (var folder in searchResult.Data!)
            {
                if (await HasPermissionAsync(userId, "Folder", folder.Id, (int)PermissionLevel.Read))
                    accessibleFolders.Add(folder);
            }
            return Ok(accessibleFolders);
        }

        if (cabinetId.HasValue)
        {
            // Check read permission on parent (folder or cabinet)
            if (parentId.HasValue)
            {
                if (!await HasPermissionAsync(userId, "Folder", parentId.Value, (int)PermissionLevel.Read))
                    return Forbid("You don't have permission to view this folder");
            }
            else
            {
                if (!await HasPermissionAsync(userId, "Cabinet", cabinetId.Value, (int)PermissionLevel.Read))
                    return Forbid("You don't have permission to view this cabinet");
            }

            var result = await _folderService.GetByParentIdAsync(parentId, cabinetId.Value);
            if (!result.Success) return BadRequest(result.Errors);

            // Filter child folders based on user's read permission
            var accessibleFolders = new List<FolderDto>();
            foreach (var folder in result.Data!)
            {
                if (await HasPermissionAsync(userId, "Folder", folder.Id, (int)PermissionLevel.Read))
                    accessibleFolders.Add(folder);
            }
            return Ok(accessibleFolders);
        }

        return BadRequest(new[] { "CabinetId is required" });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check read permission
        if (!await HasPermissionAsync(userId, "Folder", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this folder");

        var result = await _folderService.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpGet("tree/{cabinetId:guid}")]
    public async Task<IActionResult> GetTree(Guid cabinetId)
    {
        var userId = GetCurrentUserId();

        // Check read permission on cabinet
        if (!await HasPermissionAsync(userId, "Cabinet", cabinetId, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this cabinet");

        var result = await _folderService.GetTreeAsync(cabinetId);
        if (!result.Success) return BadRequest(result.Errors);

        // Filter tree based on user's read permission
        var accessibleFolders = new List<FolderDto>();
        foreach (var folder in result.Data!)
        {
            if (await HasPermissionAsync(userId, "Folder", folder.Id, (int)PermissionLevel.Read))
                accessibleFolders.Add(folder);
        }
        return Ok(accessibleFolders);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFolderDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write permission on parent (folder or cabinet)
        if (dto.ParentFolderId.HasValue)
        {
            if (!await HasPermissionAsync(userId, "Folder", dto.ParentFolderId.Value, (int)PermissionLevel.Write))
                return Forbid("You don't have permission to create folders here");
        }
        else
        {
            if (!await HasPermissionAsync(userId, "Cabinet", dto.CabinetId, (int)PermissionLevel.Write))
                return Forbid("You don't have permission to create folders in this cabinet");
        }

        var result = await _folderService.CreateAsync(dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFolderDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write permission on folder
        if (!await HasPermissionAsync(userId, "Folder", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to edit this folder");

        var result = await _folderService.UpdateAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveFolderDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write+delete permission on source folder (need to remove from current location)
        if (!await HasPermissionAsync(userId, "Folder", id, (int)(PermissionLevel.Write | PermissionLevel.Delete)))
            return Forbid("You don't have permission to move this folder");

        // Check write permission on destination
        if (dto.NewParentFolderId.HasValue)
        {
            if (!await HasPermissionAsync(userId, "Folder", dto.NewParentFolderId.Value, (int)PermissionLevel.Write))
                return Forbid("You don't have permission to move folders to this destination");
        }
        else if (dto.NewCabinetId.HasValue)
        {
            if (!await HasPermissionAsync(userId, "Cabinet", dto.NewCabinetId.Value, (int)PermissionLevel.Write))
                return Forbid("You don't have permission to move folders to this cabinet");
        }

        var result = await _folderService.MoveAsync(id, dto, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check delete permission on folder
        if (!await HasPermissionAsync(userId, "Folder", id, (int)PermissionLevel.Delete))
            return Forbid("You don't have permission to delete this folder");

        var result = await _folderService.DeleteAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    /// <summary>
    /// Apply a folder template to create a folder structure
    /// </summary>
    [HttpPost("{id:guid}/apply-template")]
    public async Task<IActionResult> ApplyTemplate(Guid id, [FromBody] ApplyTemplateDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write permission on folder
        if (!await HasPermissionAsync(userId, "Folder", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to create folders here");

        var result = await _templateService.ApplyTemplateAsync(id, dto, userId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Preview what folders would be created from a template
    /// </summary>
    [HttpPost("{id:guid}/preview-template")]
    public async Task<IActionResult> PreviewTemplate(Guid id, [FromBody] PreviewTemplateRequest request)
    {
        var userId = GetCurrentUserId();

        // Check read permission on folder
        if (!await HasPermissionAsync(userId, "Folder", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this folder");

        var result = await _templateService.PreviewTemplateAsync(id, request.TemplateId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    private async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        // Admin users bypass permission checks - they have full access to everything
        if (IsAdmin())
            return true;

        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }
}

public class PreviewTemplateRequest
{
    public Guid TemplateId { get; set; }
}
