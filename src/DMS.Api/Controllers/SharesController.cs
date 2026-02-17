using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class SharesController : BaseApiController
{
    private readonly IShareService _shareService;

    public SharesController(IShareService shareService)
    {
        _shareService = shareService;
    }

    [HttpGet("shared-with-me")]
    public async Task<ActionResult<IEnumerable<SharedDocumentDto>>> GetSharedWithMe()
    {
        var items = await _shareService.GetSharedWithMeAsync(GetCurrentUserId());
        return Ok(items);
    }

    [HttpGet("my-shared-items")]
    public async Task<ActionResult<IEnumerable<MySharedItemDto>>> GetMySharedItems()
    {
        var items = await _shareService.GetMySharedItemsAsync(GetCurrentUserId());
        return Ok(items);
    }

    [HttpGet("document/{documentId}")]
    public async Task<ActionResult<IEnumerable<DocumentShareDto>>> GetDocumentShares(Guid documentId)
    {
        var shares = await _shareService.GetDocumentSharesAsync(documentId);
        return Ok(shares);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> ShareDocument([FromBody] ShareDocumentRequest request)
    {
        var shareId = await _shareService.ShareDocumentAsync(GetCurrentUserId(), request);
        return Ok(shareId);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateShare(Guid id, [FromBody] UpdateShareRequest request)
    {
        var result = await _shareService.UpdateShareAsync(id, request.PermissionLevel, request.ExpiresAt);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> RevokeShare(Guid id)
    {
        var result = await _shareService.RevokeShareAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPost("{id}/verify-otp")]
    public async Task<IActionResult> VerifyOtp(Guid id, [FromBody] VerifyOtpRequest request)
    {
        var result = await _shareService.VerifyOtpAsync(id, request.OtpCode, GetCurrentUserId());
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    [HttpPost("{id}/resend-otp")]
    public async Task<IActionResult> ResendOtp(Guid id)
    {
        var result = await _shareService.ResendOtpAsync(id, GetCurrentUserId());
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    // Link sharing endpoints

    [HttpPost("link")]
    public async Task<IActionResult> CreateLinkShare([FromBody] CreateLinkShareRequest request)
    {
        var result = await _shareService.CreateLinkShareAsync(GetCurrentUserId(), request);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("link/{documentId:guid}")]
    public async Task<IActionResult> GetLinkShare(Guid documentId)
    {
        var result = await _shareService.GetLinkShareAsync(documentId);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpGet("resolve/{token}")]
    public async Task<IActionResult> ResolveShareToken(string token)
    {
        var result = await _shareService.ValidateShareTokenAsync(token);
        if (!result.Success) return NotFound(result.Errors);

        return Ok(new { documentId = result.Data!.DocumentId, permissionLevel = result.Data.PermissionLevel });
    }
}
