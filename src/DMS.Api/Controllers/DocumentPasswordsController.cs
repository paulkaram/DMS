using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/documents/{documentId}/password")]
[Authorize]
public class DocumentPasswordsController : ControllerBase
{
    private readonly IDocumentPasswordService _passwordService;

    public DocumentPasswordsController(IDocumentPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet("status")]
    public async Task<ActionResult<DocumentPasswordDto>> GetPasswordStatus(Guid documentId)
    {
        var password = await _passwordService.GetByDocumentIdAsync(documentId);
        if (password == null)
        {
            return Ok(new DocumentPasswordDto { DocumentId = documentId, HasPassword = false });
        }
        return Ok(password);
    }

    [HttpGet("check")]
    public async Task<ActionResult<bool>> HasPassword(Guid documentId)
    {
        var hasPassword = await _passwordService.HasPasswordAsync(documentId);
        return Ok(hasPassword);
    }

    [HttpGet("hint")]
    [AllowAnonymous]
    public async Task<ActionResult<string?>> GetHint(Guid documentId)
    {
        var hint = await _passwordService.GetHintAsync(documentId);
        return Ok(hint);
    }

    [HttpPost]
    public async Task<ActionResult> SetPassword(Guid documentId, [FromBody] SetPasswordRequest request)
    {
        request.DocumentId = documentId;
        var result = await _passwordService.SetPasswordAsync(request, GetUserId());
        if (!result) return BadRequest("Failed to set password");
        return Ok();
    }

    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> ValidatePassword(Guid documentId, [FromBody] ValidatePasswordRequest request)
    {
        request.DocumentId = documentId;
        var isValid = await _passwordService.ValidatePasswordAsync(request);
        return Ok(isValid);
    }

    [HttpPut]
    public async Task<ActionResult> ChangePassword(Guid documentId, [FromBody] ChangePasswordRequest request)
    {
        request.DocumentId = documentId;
        var result = await _passwordService.ChangePasswordAsync(request, GetUserId());
        if (!result) return BadRequest("Invalid current password or failed to change password");
        return Ok();
    }

    [HttpDelete]
    public async Task<ActionResult> RemovePassword(Guid documentId)
    {
        var result = await _passwordService.RemovePasswordAsync(documentId, GetUserId());
        if (!result) return NotFound();
        return NoContent();
    }
}
