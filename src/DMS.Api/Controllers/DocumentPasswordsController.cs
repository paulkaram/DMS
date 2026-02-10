using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Route("api/documents/{documentId}/password")]
[Authorize]
public class DocumentPasswordsController : BaseApiController
{
    private readonly IDocumentPasswordService _passwordService;

    public DocumentPasswordsController(IDocumentPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

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
        var result = await _passwordService.SetPasswordAsync(request, GetCurrentUserId());
        if (!result) return BadRequest(ErrorMessages.FailedToSetPassword);
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
        var result = await _passwordService.ChangePasswordAsync(request, GetCurrentUserId());
        if (!result) return BadRequest(ErrorMessages.InvalidPasswordOrChangeFailed);
        return Ok();
    }

    [HttpDelete]
    public async Task<ActionResult> RemovePassword(Guid documentId)
    {
        var result = await _passwordService.RemovePasswordAsync(documentId, GetCurrentUserId());
        if (!result) return NotFound();
        return NoContent();
    }
}
