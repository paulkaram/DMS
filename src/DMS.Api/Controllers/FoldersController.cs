using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class FoldersController : BaseApiController
{
    private readonly IFolderService _folderService;
    private readonly IFolderTemplateService _templateService;

    public FoldersController(
        IFolderService folderService,
        IFolderTemplateService templateService)
    {
        _folderService = folderService;
        _templateService = templateService;
    }

    /// <summary>
    /// Checks if the current user has sufficient privacy level to access a folder.
    /// </summary>
    private async Task<bool> HasPrivacyAccessAsync(Guid folderId)
    {
        var privacyLevel = GetCurrentUserPrivacyLevel();
        if (privacyLevel == null) return true; // Admin bypasses

        var folderResult = await _folderService.GetByIdAsync(folderId);
        if (!folderResult.Success) return true;

        var folder = folderResult.Data!;
        if (folder.PrivacyLevelValue == null) return true;

        return folder.PrivacyLevelValue.Value <= privacyLevel.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? cabinetId, [FromQuery] Guid? parentId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        var userId = GetCurrentUserId();
        var privacyLevel = GetCurrentUserPrivacyLevel();
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);

        if (!string.IsNullOrEmpty(search))
        {
            var searchResult = await _folderService.SearchPaginatedAsync(search, cabinetId, page, pageSize, privacyLevel);
            if (!searchResult.Success) return BadRequest(searchResult.Errors);

            var pagedResult = searchResult.Data!;
            // Filter results based on user's read permission
            var accessibleFolders = new List<FolderDto>();
            foreach (var folder in pagedResult.Items)
            {
                if (await HasPermissionAsync(userId, "Folder", folder.Id, (int)PermissionLevel.Read))
                    accessibleFolders.Add(folder);
            }
            pagedResult.Items = accessibleFolders;
            return Ok(pagedResult);
        }

        if (cabinetId.HasValue)
        {
            // Check read permission on parent (folder or cabinet)
            if (parentId.HasValue)
            {
                if (!await HasPermissionAsync(userId, "Folder", parentId.Value, (int)PermissionLevel.Read))
                    return Forbid(ErrorMessages.Permissions.ViewFolder);
            }
            else
            {
                if (!await HasPermissionAsync(userId, "Cabinet", cabinetId.Value, (int)PermissionLevel.Read))
                    return Forbid(ErrorMessages.Permissions.ViewCabinet);
            }

            var result = await _folderService.GetByParentIdPaginatedAsync(parentId, cabinetId.Value, page, pageSize, privacyLevel);
            if (!result.Success) return BadRequest(result.Errors);

            var pagedResult = result.Data!;
            // Filter child folders based on user's read permission
            var accessibleFolders = new List<FolderDto>();
            foreach (var folder in pagedResult.Items)
            {
                if (await HasPermissionAsync(userId, "Folder", folder.Id, (int)PermissionLevel.Read))
                    accessibleFolders.Add(folder);
            }
            pagedResult.Items = accessibleFolders;
            return Ok(pagedResult);
        }

        return BadRequest(new[] { ErrorMessages.CabinetIdRequired });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check read permission
        if (!await HasPermissionAsync(userId, "Folder", id, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.ViewFolder);

        // Check privacy level
        if (!await HasPrivacyAccessAsync(id))
            return NotFound(new[] { ErrorMessages.Permissions.InsufficientPrivacyLevel });

        var result = await _folderService.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpGet("tree/{cabinetId:guid}")]
    public async Task<IActionResult> GetTree(Guid cabinetId)
    {
        var userId = GetCurrentUserId();
        var privacyLevel = GetCurrentUserPrivacyLevel();

        // Check read permission on cabinet
        if (!await HasPermissionAsync(userId, "Cabinet", cabinetId, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.ViewCabinet);

        var result = await _folderService.GetTreeAsync(cabinetId, privacyLevel);
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
                return Forbid(ErrorMessages.Permissions.CreateFoldersHere);
        }
        else
        {
            if (!await HasPermissionAsync(userId, "Cabinet", dto.CabinetId, (int)PermissionLevel.Write))
                return Forbid(ErrorMessages.Permissions.CreateFoldersInCabinet);
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
            return Forbid(ErrorMessages.Permissions.EditFolder);

        var result = await _folderService.UpdateAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveFolderDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write+delete permission on source folder (need to remove from current location)
        if (!await HasPermissionAsync(userId, "Folder", id, (int)(PermissionLevel.Write | PermissionLevel.Delete)))
            return Forbid(ErrorMessages.Permissions.MoveFolder);

        // Check write permission on destination
        if (dto.NewParentFolderId.HasValue)
        {
            if (!await HasPermissionAsync(userId, "Folder", dto.NewParentFolderId.Value, (int)PermissionLevel.Write))
                return Forbid(ErrorMessages.Permissions.MoveFoldersToDestination);
        }
        else if (dto.NewCabinetId.HasValue)
        {
            if (!await HasPermissionAsync(userId, "Cabinet", dto.NewCabinetId.Value, (int)PermissionLevel.Write))
                return Forbid(ErrorMessages.Permissions.MoveFoldersToCabinet);
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
            return Forbid(ErrorMessages.Permissions.DeleteFolder);

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
            return Forbid(ErrorMessages.Permissions.CreateFoldersHere);

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
            return Forbid(ErrorMessages.Permissions.ViewFolder);

        var result = await _templateService.PreviewTemplateAsync(id, request.TemplateId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }
}
