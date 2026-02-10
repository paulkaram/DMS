using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class DocumentShortcutsController : BaseApiController
{
    private readonly IDocumentShortcutService _shortcutService;

    public DocumentShortcutsController(IDocumentShortcutService shortcutService)
    {
        _shortcutService = shortcutService;
    }

    [HttpGet("by-document/{documentId:guid}")]
    public async Task<IActionResult> GetByDocument(Guid documentId)
    {
        var userId = GetCurrentUserId();

        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.ViewDocument);

        var result = await _shortcutService.GetShortcutsByDocumentAsync(documentId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDocumentShortcutDto dto)
    {
        var userId = GetCurrentUserId();

        // Check read permission on source document
        if (!await HasPermissionAsync(userId, "Document", dto.DocumentId, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.ReadDocument);

        // Check write permission on target folder
        if (!await HasPermissionAsync(userId, "Folder", dto.FolderId, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.WriteToFolder);

        var result = await _shortcutService.CreateShortcutAsync(dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _shortcutService.RemoveShortcutAsync(id);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }
}
