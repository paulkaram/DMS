using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class DocumentStateController : BaseApiController
{
    private readonly IDocumentStateService _stateService;

    public DocumentStateController(IDocumentStateService stateService)
    {
        _stateService = stateService;
    }

    /// <summary>
    /// Transition a document to a new lifecycle state.
    /// </summary>
    [HttpPost("{documentId:guid}/transition")]
    [Authorize(Roles = $"{AppConstants.Roles.Administrator},{AppConstants.Roles.Records}")]
    public async Task<IActionResult> TransitionState(Guid documentId, [FromBody] StateTransitionRequestDto dto)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.TransitionDocument);
        var result = await _stateService.TransitionAsync(documentId, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get allowed transitions for a document based on current state and user role.
    /// </summary>
    [HttpGet("{documentId:guid}/allowed-transitions")]
    public async Task<IActionResult> GetAllowedTransitions(Guid documentId)
    {
        var userId = GetCurrentUserId();
        var roles = User.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value);
        var result = await _stateService.GetAllowedTransitionsAsync(documentId, userId, roles);
        return OkOrBadRequest(result);
    }

    /// <summary>
    /// Get the full state transition history for a document.
    /// </summary>
    [HttpGet("{documentId:guid}/history")]
    public async Task<IActionResult> GetTransitionHistory(Guid documentId)
    {
        var result = await _stateService.GetTransitionHistoryAsync(documentId);
        return OkOrBadRequest(result);
    }

    /// <summary>
    /// Place a document on legal hold.
    /// </summary>
    [HttpPost("{documentId:guid}/hold")]
    [Authorize(Roles = $"{AppConstants.Roles.Administrator},{AppConstants.Roles.Records}")]
    public async Task<IActionResult> PlaceOnHold(Guid documentId, [FromBody] PlaceOnHoldDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _stateService.PlaceOnHoldAsync(documentId, dto.LegalHoldId, userId);
        return OkOrBadRequest(result);
    }

    /// <summary>
    /// Release a document from legal hold.
    /// </summary>
    [HttpPost("{documentId:guid}/release-hold")]
    [Authorize(Roles = $"{AppConstants.Roles.Administrator},{AppConstants.Roles.Records}")]
    public async Task<IActionResult> ReleaseFromHold(Guid documentId)
    {
        var userId = GetCurrentUserId();
        var result = await _stateService.ReleaseFromHoldAsync(documentId, userId);
        return OkOrBadRequest(result);
    }
}
