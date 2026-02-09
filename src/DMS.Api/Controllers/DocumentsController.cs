using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly IBulkOperationService _bulkOperationService;
    private readonly IPreviewService _previewService;
    private readonly IPermissionService _permissionService;
    private readonly IFolderService _folderService;

    public DocumentsController(
        IDocumentService documentService,
        IBulkOperationService bulkOperationService,
        IPreviewService previewService,
        IPermissionService permissionService,
        IFolderService folderService)
    {
        _documentService = documentService;
        _bulkOperationService = bulkOperationService;
        _previewService = previewService;
        _permissionService = permissionService;
        _folderService = folderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? folderId, [FromQuery] string? search, [FromQuery] Guid? classificationId, [FromQuery] Guid? documentTypeId)
    {
        var userId = GetCurrentUserId();

        // If searching within a folder, check read permission on folder
        if (folderId.HasValue)
        {
            if (!await HasPermissionAsync(userId, "Folder", folderId.Value, (int)PermissionLevel.Read))
                return Forbid("You don't have permission to view this folder");
        }

        var result = await _documentService.SearchAsync(search, folderId, classificationId, documentTypeId);
        if (!result.Success) return BadRequest(result.Errors);

        var docs = result.Data!;

        // Private folder filtering: non-admin users only see their own documents
        if (folderId.HasValue)
        {
            var folderResult = await _folderService.GetByIdAsync(folderId.Value);
            if (folderResult.Success && folderResult.Data!.AccessMode == 1)
            {
                if (!IsAdmin() && !await HasPermissionAsync(userId, "Folder", folderId.Value, (int)PermissionLevel.Admin))
                {
                    docs = docs.Where(d => d.CreatedBy == userId).ToList();
                }
            }
        }

        // Filter documents based on user's read permission
        var accessibleDocs = new List<DocumentDto>();
        foreach (var doc in docs)
        {
            if (await HasPermissionAsync(userId, "Document", doc.Id, (int)PermissionLevel.Read))
                accessibleDocs.Add(doc);
        }
        return Ok(accessibleDocs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check read permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this document");

        var result = await _documentService.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check read permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this document");

        var result = await _documentService.GetVersionsAsync(id);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] int? version)
    {
        var userId = GetCurrentUserId();

        // Check read permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to download this document");

        var docResult = await _documentService.GetByIdAsync(id);
        if (!docResult.Success)
            return NotFound(docResult.Errors);

        var result = await _documentService.DownloadAsync(id, version);
        if (!result.Success)
            return NotFound(result.Errors);

        var doc = docResult.Data!;
        return File(result.Data!, doc.ContentType ?? "application/octet-stream", doc.Name + doc.Extension);
    }

    [HttpGet("my-checkouts")]
    public async Task<IActionResult> GetMyCheckouts()
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetCheckedOutByUserAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("my-documents")]
    public async Task<IActionResult> GetMyDocuments([FromQuery] int take = 50)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetCreatedByUserAsync(userId, take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateDocumentDto dto, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new[] { "File is required" });

        var userId = GetCurrentUserId();

        // Check write permission on target folder
        if (!await HasPermissionAsync(userId, "Folder", dto.FolderId, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to upload documents to this folder");

        using var stream = file.OpenReadStream();
        var result = await _documentService.CreateAsync(dto, stream, file.FileName, file.ContentType, userId);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMetadata(Guid id, [FromBody] UpdateDocumentDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to edit this document");

        var result = await _documentService.UpdateMetadataAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}/content")]
    public async Task<IActionResult> UpdateContent(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new[] { "File is required" });

        var userId = GetCurrentUserId();

        // Check write permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to edit this document");

        using var stream = file.OpenReadStream();
        var result = await _documentService.UpdateContentAsync(id, stream, file.FileName, file.ContentType, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("{id:guid}/checkout")]
    public async Task<IActionResult> CheckOut(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check write permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to check out this document");

        var result = await _documentService.CheckOutAsync(id, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Check in a document with optional file. Supports Major/Minor versioning.
    /// File is optional - allows metadata-only check-ins.
    /// </summary>
    [HttpPost("{id:guid}/checkin")]
    public async Task<IActionResult> CheckIn(Guid id, [FromForm] CheckInDto dto, IFormFile? file = null)
    {
        var userId = GetCurrentUserId();

        // Check write permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to check in this document");

        Stream? stream = null;
        string? fileName = null;
        string? contentType = null;

        if (file != null && file.Length > 0)
        {
            stream = file.OpenReadStream();
            fileName = file.FileName;
            contentType = file.ContentType;
        }

        try
        {
            var result = await _documentService.CheckInAsync(id, stream, fileName, contentType, dto, userId);
            return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
        }
        finally
        {
            stream?.Dispose();
        }
    }

    [HttpPost("{id:guid}/discard-checkout")]
    public async Task<IActionResult> DiscardCheckout(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check write permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to discard this checkout");

        var result = await _documentService.DiscardCheckOutAsync(id, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Admin-only: Force discard another user's checkout.
    /// </summary>
    [HttpPost("{id:guid}/force-discard-checkout")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ForceDiscardCheckout(Guid id, [FromBody] ForceDiscardCheckoutRequest request)
    {
        var adminUserId = GetCurrentUserId();
        var result = await _documentService.ForceDiscardCheckOutAsync(id, adminUserId, request.Reason ?? "Admin forced discard");
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    #region Working Copy Management

    /// <summary>
    /// Gets the current working copy (draft state) for a checked-out document.
    /// </summary>
    [HttpGet("{id:guid}/working-copy")]
    public async Task<IActionResult> GetWorkingCopy(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check write permission (only those who can edit can view working copy)
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to view this working copy");

        var result = await _documentService.GetWorkingCopyAsync(id, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Saves draft metadata/properties to the working copy.
    /// </summary>
    [HttpPut("{id:guid}/working-copy")]
    public async Task<IActionResult> SaveWorkingCopy(Guid id, [FromBody] SaveWorkingCopyDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write permission
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to edit this document");

        var result = await _documentService.SaveWorkingCopyAsync(id, dto, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Saves draft file content to the working copy.
    /// </summary>
    [HttpPut("{id:guid}/working-copy/content")]
    public async Task<IActionResult> SaveWorkingCopyContent(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new[] { "File is required" });

        var userId = GetCurrentUserId();

        // Check write permission
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to edit this document");

        using var stream = file.OpenReadStream();
        var result = await _documentService.SaveWorkingCopyContentAsync(id, stream, file.FileName, file.ContentType, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    #endregion

    #region Version Comparison and Restore

    /// <summary>
    /// Compares two versions showing content and metadata differences.
    /// </summary>
    [HttpGet("{id:guid}/versions/compare")]
    public async Task<IActionResult> CompareVersions(Guid id, [FromQuery] Guid source, [FromQuery] Guid target)
    {
        var userId = GetCurrentUserId();

        // Check read permission
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this document");

        var result = await _documentService.CompareVersionsAsync(id, source, target);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Restores a previous version (creates a new major version).
    /// </summary>
    [HttpPost("{id:guid}/versions/{versionId:guid}/restore")]
    public async Task<IActionResult> RestoreVersion(Guid id, Guid versionId, [FromBody] RestoreVersionDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write permission
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to restore versions of this document");

        var result = await _documentService.RestoreVersionAsync(id, versionId, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    #endregion

    #region Admin Checkout Management

    /// <summary>
    /// Admin-only: Gets stale checkouts for cleanup.
    /// </summary>
    [HttpGet("stale-checkouts")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> GetStaleCheckouts([FromQuery] int staleHours = 24)
    {
        var result = await _documentService.GetStaleCheckoutsAsync(staleHours);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    #endregion

    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MoveDocumentDto dto)
    {
        var userId = GetCurrentUserId();

        // Check write+delete permission on document (need to remove from current location)
        if (!await HasPermissionAsync(userId, "Document", id, (int)(PermissionLevel.Write | PermissionLevel.Delete)))
            return Forbid("You don't have permission to move this document");

        // Check write permission on destination folder
        if (!await HasPermissionAsync(userId, "Folder", dto.NewFolderId, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to move documents to this folder");

        var result = await _documentService.MoveAsync(id, dto, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    [HttpPost("{id:guid}/copy")]
    public async Task<IActionResult> Copy(Guid id, [FromBody] CopyDocumentDto dto)
    {
        var userId = GetCurrentUserId();

        // Check read permission on source document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to copy this document");

        // Check write permission on destination folder
        if (!await HasPermissionAsync(userId, "Folder", dto.TargetFolderId, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to copy documents to this folder");

        var result = await _documentService.CopyAsync(id, dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check delete permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Delete))
            return Forbid("You don't have permission to delete this document");

        var result = await _documentService.DeleteAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    // Preview endpoint
    [HttpGet("{id:guid}/preview")]
    public async Task<IActionResult> GetPreviewInfo(Guid id, [FromQuery] int? version)
    {
        var userId = GetCurrentUserId();

        // Check read permission
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to preview this document");

        var result = await _previewService.GetPreviewInfoAsync(id, version);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    // Bulk operation endpoints
    [HttpPost("bulk/delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        if (request.DocumentIds == null || request.DocumentIds.Count == 0)
            return BadRequest(new[] { "No documents specified" });

        var userId = GetCurrentUserId();
        var result = await _bulkOperationService.BulkDeleteAsync(request.DocumentIds, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("bulk/move")]
    public async Task<IActionResult> BulkMove([FromBody] BulkMoveRequest request)
    {
        if (request.DocumentIds == null || request.DocumentIds.Count == 0)
            return BadRequest(new[] { "No documents specified" });

        var userId = GetCurrentUserId();
        var result = await _bulkOperationService.BulkMoveAsync(request.DocumentIds, request.TargetFolderId, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("bulk/download")]
    public async Task<IActionResult> BulkDownload([FromBody] BulkDownloadRequest request)
    {
        if (request.DocumentIds == null || request.DocumentIds.Count == 0)
            return BadRequest(new[] { "No documents specified" });

        var userId = GetCurrentUserId();
        var result = await _bulkOperationService.BulkDownloadAsync(request.DocumentIds, userId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return File(result.Data!, "application/zip", $"documents-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    private async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        // Admin users bypass permission checks - they have full access to everything
        if (IsAdmin())
            return true;

        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }
}
