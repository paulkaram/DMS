using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/documents/{documentId}/annotations")]
[Authorize]
public class DocumentAnnotationsController : ControllerBase
{
    private readonly IDocumentAnnotationService _annotationService;
    private readonly IPermissionService _permissionService;

    public DocumentAnnotationsController(
        IDocumentAnnotationService annotationService,
        IPermissionService permissionService)
    {
        _annotationService = annotationService;
        _permissionService = permissionService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    private async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        if (IsAdmin()) return true;
        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentAnnotationDto>>> GetAnnotations(Guid documentId)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this document");

        var annotations = await _annotationService.GetByDocumentIdAsync(documentId);
        return Ok(annotations);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetAnnotationCount(Guid documentId)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this document");

        var count = await _annotationService.GetCountAsync(documentId);
        return Ok(count);
    }

    [HttpPost]
    public async Task<ActionResult<List<DocumentAnnotationDto>>> SaveAnnotations(Guid documentId, [FromBody] SaveAnnotationsRequest request)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Write))
            return Forbid("You don't have write permission on this document");

        request.DocumentId = documentId;
        var result = await _annotationService.SaveAnnotationsAsync(request, userId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAnnotation(Guid documentId, Guid id)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Write))
            return Forbid("You don't have write permission on this document");

        var result = await _annotationService.DeleteAsync(id, userId);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteAllAnnotations(Guid documentId)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Write))
            return Forbid("You don't have write permission on this document");

        await _annotationService.DeleteAllByDocumentAsync(documentId, userId);
        return NoContent();
    }
}

[ApiController]
[Route("api/signatures")]
[Authorize]
public class SavedSignaturesController : ControllerBase
{
    private readonly ISavedSignatureService _signatureService;

    public SavedSignaturesController(ISavedSignatureService signatureService)
    {
        _signatureService = signatureService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SavedSignatureDto>>> GetMySignatures()
    {
        var signatures = await _signatureService.GetByUserIdAsync(GetCurrentUserId());
        return Ok(signatures);
    }

    [HttpPost]
    public async Task<ActionResult<SavedSignatureDto>> CreateSignature([FromBody] CreateSignatureRequest request)
    {
        var signature = await _signatureService.AddAsync(request, GetCurrentUserId());
        return Ok(signature);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteSignature(Guid id)
    {
        var result = await _signatureService.DeleteAsync(id, GetCurrentUserId());
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPut("{id}/set-default")]
    public async Task<ActionResult> SetDefault(Guid id)
    {
        var result = await _signatureService.SetDefaultAsync(id, GetCurrentUserId());
        if (!result) return NotFound();
        return NoContent();
    }
}
