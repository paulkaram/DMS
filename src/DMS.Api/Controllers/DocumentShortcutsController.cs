using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentShortcutsController : ControllerBase
{
    private readonly IDocumentShortcutService _shortcutService;
    private readonly IPermissionService _permissionService;

    public DocumentShortcutsController(
        IDocumentShortcutService shortcutService,
        IPermissionService permissionService)
    {
        _shortcutService = shortcutService;
        _permissionService = permissionService;
    }

    [HttpGet("by-document/{documentId:guid}")]
    public async Task<IActionResult> GetByDocument(Guid documentId)
    {
        var userId = GetCurrentUserId();

        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this document");

        var result = await _shortcutService.GetShortcutsByDocumentAsync(documentId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentShortcutDto dto)
    {
        var userId = GetCurrentUserId();

        // Check read permission on source document
        if (!await HasPermissionAsync(userId, "Document", dto.DocumentId, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to read this document");

        // Check write permission on target folder
        if (!await HasPermissionAsync(userId, "Folder", dto.FolderId, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to write to this folder");

        var result = await _shortcutService.CreateShortcutAsync(dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _shortcutService.RemoveShortcutAsync(id);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    private async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        if (IsAdmin())
            return true;

        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }
}
