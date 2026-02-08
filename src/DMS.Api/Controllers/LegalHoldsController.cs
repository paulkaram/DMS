using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

/// <summary>
/// ISO 15489 compliant legal hold management endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LegalHoldsController : ControllerBase
{
    private readonly ILegalHoldService _legalHoldService;
    private readonly ILogger<LegalHoldsController> _logger;

    public LegalHoldsController(ILegalHoldService legalHoldService, ILogger<LegalHoldsController> logger)
    {
        _legalHoldService = legalHoldService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all legal holds.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var result = activeOnly
            ? await _legalHoldService.GetActiveHoldsAsync()
            : await _legalHoldService.GetAllHoldsAsync();

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Gets a legal hold by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _legalHoldService.GetByIdAsync(id);

        if (!result.Success)
            return NotFound(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new legal hold.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Legal")]
    public async Task<IActionResult> Create([FromBody] CreateLegalHoldDto dto)
    {
        var userId = GetUserId();
        var result = await _legalHoldService.CreateHoldAsync(dto, userId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates a legal hold.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Legal")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLegalHoldDto dto)
    {
        var userId = GetUserId();
        var result = await _legalHoldService.UpdateHoldAsync(id, dto, userId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Adds documents to a legal hold.
    /// </summary>
    [HttpPost("{id}/documents")]
    [Authorize(Roles = "Admin,Legal")]
    public async Task<IActionResult> AddDocuments(Guid id, [FromBody] AddDocumentsToHoldDto dto)
    {
        var userId = GetUserId();
        var result = await _legalHoldService.AddDocumentsToHoldAsync(id, dto.DocumentIds, userId, dto.Notes);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Gets documents under a legal hold.
    /// </summary>
    [HttpGet("{id}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        var result = await _legalHoldService.GetHoldDocumentsAsync(id);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Removes a document from a legal hold.
    /// </summary>
    [HttpDelete("{id}/documents/{documentId}")]
    [Authorize(Roles = "Admin,Legal")]
    public async Task<IActionResult> RemoveDocument(Guid id, Guid documentId)
    {
        var userId = GetUserId();
        var result = await _legalHoldService.RemoveDocumentFromHoldAsync(id, documentId, userId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Gets all legal holds affecting a document.
    /// </summary>
    [HttpGet("document/{documentId}")]
    public async Task<IActionResult> GetDocumentHolds(Guid documentId)
    {
        var result = await _legalHoldService.GetDocumentHoldsAsync(documentId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Data);
    }

    /// <summary>
    /// Releases a legal hold.
    /// </summary>
    [HttpPost("{id}/release")]
    [Authorize(Roles = "Admin,Legal")]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReleaseHoldDto dto)
    {
        var userId = GetUserId();
        var result = await _legalHoldService.ReleaseHoldAsync(id, userId, dto.Reason);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }
}

public class AddDocumentsToHoldDto
{
    public List<Guid> DocumentIds { get; set; } = new();
    public string? Notes { get; set; }
}

public class ReleaseHoldDto
{
    public string Reason { get; set; } = string.Empty;
}
