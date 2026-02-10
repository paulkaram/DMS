using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

/// <summary>
/// ISO 15489 compliant disposal management endpoints.
/// </summary>
[Authorize]
public class DisposalController : BaseApiController
{
    private readonly IDisposalService _disposalService;
    private readonly ILogger<DisposalController> _logger;

    public DisposalController(IDisposalService disposalService, ILogger<DisposalController> logger)
    {
        _disposalService = disposalService;
        _logger = logger;
    }

    /// <summary>
    /// Gets documents pending disposal (retention expired).
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> GetPendingDisposals()
    {
        var result = await _disposalService.GetPendingDisposalsAsync();

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets documents approaching retention expiration.
    /// </summary>
    [HttpGet("upcoming")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> GetUpcomingDisposals([FromQuery] int daysAhead = 30)
    {
        var result = await _disposalService.GetUpcomingDisposalsAsync(daysAhead);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Initiates disposal of a document.
    /// </summary>
    [HttpPost("documents/{documentId}/initiate")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> InitiateDisposal(Guid documentId, [FromBody] InitiateDisposalDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _disposalService.InitiateDisposalAsync(documentId, dto, userId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Executes disposal of a document and generates certificate.
    /// </summary>
    [HttpPost("documents/{documentId}/execute")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ExecuteDisposal(Guid documentId, [FromQuery] string method = "HardDelete")
    {
        var userId = GetCurrentUserId();
        var result = await _disposalService.ExecuteDisposalAsync(documentId, userId, method);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a disposal certificate by ID.
    /// </summary>
    [HttpGet("certificates/{id}")]
    public async Task<IActionResult> GetCertificate(Guid id)
    {
        var result = await _disposalService.GetCertificateAsync(id);

        if (!result.Success)
            return NotFound(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets disposal certificate for a document.
    /// </summary>
    [HttpGet("certificates/document/{documentId}")]
    public async Task<IActionResult> GetCertificateByDocument(Guid documentId)
    {
        var result = await _disposalService.GetCertificateByDocumentAsync(documentId);

        if (!result.Success)
            return NotFound(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets all disposal certificates with optional date filtering.
    /// </summary>
    [HttpGet("certificates")]
    [Authorize(Roles = "Administrator,Records,Auditor")]
    public async Task<IActionResult> GetCertificates([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var result = await _disposalService.GetCertificatesAsync(fromDate, toDate);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Processes scheduled disposals (for background job).
    /// </summary>
    [HttpPost("process-scheduled")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ProcessScheduledDisposals()
    {
        var userId = GetCurrentUserId();
        var result = await _disposalService.ProcessScheduledDisposalsAsync(userId);
        return Ok(result);
    }
}
