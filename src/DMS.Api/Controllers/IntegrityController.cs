using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

/// <summary>
/// ISO 27001/15489 compliant integrity verification endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrityController : ControllerBase
{
    private readonly IIntegrityService _integrityService;
    private readonly ILogger<IntegrityController> _logger;

    public IntegrityController(IIntegrityService integrityService, ILogger<IntegrityController> logger)
    {
        _integrityService = integrityService;
        _logger = logger;
    }

    /// <summary>
    /// Verifies integrity of a document's current version.
    /// </summary>
    [HttpPost("documents/{documentId}/verify")]
    public async Task<IActionResult> VerifyDocument(Guid documentId)
    {
        var userId = GetUserId();
        var result = await _integrityService.VerifyDocumentIntegrityAsync(documentId, userId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Verifies integrity of a specific document version.
    /// </summary>
    [HttpPost("documents/{documentId}/versions/{versionNumber}/verify")]
    public async Task<IActionResult> VerifyVersion(Guid documentId, int versionNumber)
    {
        var userId = GetUserId();
        var result = await _integrityService.VerifyVersionIntegrityAsync(documentId, versionNumber, userId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets integrity verification history for a document.
    /// </summary>
    [HttpGet("documents/{documentId}/history")]
    public async Task<IActionResult> GetVerificationHistory(Guid documentId)
    {
        var result = await _integrityService.GetVerificationHistoryAsync(documentId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Runs batch integrity verification (admin only).
    /// </summary>
    [HttpPost("batch-verify")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> RunBatchVerification([FromQuery] int batchSize = 100)
    {
        var userId = GetUserId();
        var result = await _integrityService.RunScheduledVerificationAsync(batchSize, userId);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }
}
