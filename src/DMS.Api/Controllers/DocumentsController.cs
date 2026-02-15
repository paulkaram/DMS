using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class DocumentsController : BaseApiController
{
    private readonly IDocumentService _documentService;
    private readonly IBulkOperationService _bulkOperationService;
    private readonly IPreviewService _previewService;
    private readonly IFolderService _folderService;
    private readonly IApprovalService _approvalService;

    public DocumentsController(
        IDocumentService documentService,
        IBulkOperationService bulkOperationService,
        IPreviewService previewService,
        IFolderService folderService,
        IApprovalService approvalService)
    {
        _documentService = documentService;
        _bulkOperationService = bulkOperationService;
        _previewService = previewService;
        _folderService = folderService;
        _approvalService = approvalService;
    }

    /// <summary>
    /// Checks if the current user has sufficient privacy level to access a document's folder.
    /// Returns true if access is allowed, false if blocked.
    /// </summary>
    private async Task<bool> HasFolderPrivacyAccessAsync(Guid folderId)
    {
        var privacyLevel = GetCurrentUserPrivacyLevel();
        if (privacyLevel == null) return true; // Admin bypasses

        var folderResult = await _folderService.GetByIdAsync(folderId);
        if (!folderResult.Success) return true; // Folder not found, allow (document may be orphaned)

        var folder = folderResult.Data!;
        if (folder.PrivacyLevelValue == null) return true; // No privacy restriction

        return folder.PrivacyLevelValue.Value <= privacyLevel.Value;
    }

    /// <summary>
    /// Checks if the current user has sufficient privacy level to access a document.
    /// Checks both the document's own privacy level and the folder's privacy level.
    /// </summary>
    private async Task<bool> HasDocumentPrivacyAccessAsync(DocumentDto doc)
    {
        var privacyLevel = GetCurrentUserPrivacyLevel();
        if (privacyLevel == null) return true; // Admin bypasses

        // Check document-level privacy
        if (doc.PrivacyLevelValue != null && doc.PrivacyLevelValue.Value > privacyLevel.Value)
            return false;

        // Check folder-level privacy
        return await HasFolderPrivacyAccessAsync(doc.FolderId);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? folderId, [FromQuery] string? search,
        [FromQuery] Guid? classificationId, [FromQuery] Guid? documentTypeId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        var userId = GetCurrentUserId();
        var privacyLevel = GetCurrentUserPrivacyLevel();
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);

        // If searching within a folder, check read permission on folder
        if (folderId.HasValue)
        {
            if (!await HasPermissionAsync(userId, "Folder", folderId.Value, (int)PermissionLevel.Read))
                return Forbid(ErrorMessages.Permissions.ViewFolder);
        }

        var result = await _documentService.SearchPaginatedAsync(search, folderId, classificationId, documentTypeId, page, pageSize, privacyLevel);
        if (!result.Success) return BadRequest(result.Errors);

        var pagedResult = result.Data!;

        // Private folder filtering: non-admin users only see their own documents
        if (folderId.HasValue)
        {
            var folderResult = await _folderService.GetByIdAsync(folderId.Value);
            if (folderResult.Success && folderResult.Data!.AccessMode == 1)
            {
                if (!IsAdmin() && !await HasPermissionAsync(userId, "Folder", folderId.Value, (int)PermissionLevel.Admin))
                {
                    pagedResult.Items = pagedResult.Items.Where(d => d.CreatedBy == userId).ToList();
                }
            }
        }

        // Filter documents based on user's read permission
        var accessibleDocs = new List<DocumentDto>();
        foreach (var doc in pagedResult.Items)
        {
            if (await HasPermissionAsync(userId, "Document", doc.Id, (int)PermissionLevel.Read))
                accessibleDocs.Add(doc);
        }
        pagedResult.Items = accessibleDocs;

        // Filter out documents pending/rejected approval (only creator and admins can see them)
        if (!IsAdmin())
        {
            pagedResult.Items = pagedResult.Items.Where(doc =>
                doc.ApprovalStatus == null ||
                doc.ApprovalStatus == (int)ApprovalStatus.Approved ||
                doc.ApprovalStatus == (int)ApprovalStatus.Cancelled ||
                doc.CreatedBy == userId
            ).ToList();
        }

        // Filter out expired documents (only creator and admins can see them)
        if (!IsAdmin())
        {
            pagedResult.Items = pagedResult.Items.Where(doc =>
                doc.ExpiryDate == null ||
                doc.ExpiryDate > DateTime.Now ||
                doc.CreatedBy == userId
            ).ToList();
        }

        return Ok(pagedResult);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check read permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.ViewDocument);

        var result = await _documentService.GetByIdAsync(id);
        if (!result.Success) return NotFound(result.Errors);

        var doc = result.Data!;

        // Check privacy level on document and folder
        if (!await HasDocumentPrivacyAccessAsync(doc))
            return NotFound(new[] { ErrorMessages.Permissions.InsufficientPrivacyLevel });

        // Block access to pending/rejected documents for non-creators/non-admins/non-approvers
        if (!IsAdmin() && doc.CreatedBy != userId &&
            doc.ApprovalStatus is (int)ApprovalStatus.Pending or (int)ApprovalStatus.Rejected or (int)ApprovalStatus.ReturnedForRevision)
        {
            // Allow assigned approvers to view the document
            if (!await _approvalService.IsApproverForDocumentAsync(id, userId))
                return NotFound(new[] { ErrorMessages.Permissions.DocumentPendingApproval });
        }

        // Block access to expired documents for non-creators/non-admins
        if (!IsAdmin() && doc.CreatedBy != userId &&
            doc.ExpiryDate.HasValue && doc.ExpiryDate.Value <= DateTime.Now)
        {
            return NotFound(new[] { ErrorMessages.Permissions.DocumentExpired });
        }

        return Ok(doc);
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check read permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.ViewDocument);

        var result = await _documentService.GetVersionsAsync(id);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] int? version)
    {
        var userId = GetCurrentUserId();

        // Check read permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.DownloadDocument);

        var docResult = await _documentService.GetByIdAsync(id);
        if (!docResult.Success)
            return NotFound(docResult.Errors);

        var doc = docResult.Data!;

        // Check privacy level on document and folder
        if (!await HasDocumentPrivacyAccessAsync(doc))
            return NotFound(new[] { ErrorMessages.Permissions.InsufficientPrivacyLevel });

        // Block download of pending/rejected documents for non-creators/non-admins/non-approvers
        if (!IsAdmin() && doc.CreatedBy != userId &&
            doc.ApprovalStatus is (int)ApprovalStatus.Pending or (int)ApprovalStatus.Rejected or (int)ApprovalStatus.ReturnedForRevision)
        {
            if (!await _approvalService.IsApproverForDocumentAsync(id, userId))
                return NotFound(new[] { ErrorMessages.Permissions.DocumentPendingApproval });
        }

        // Block download of expired documents for non-creators/non-admins
        if (!IsAdmin() && doc.CreatedBy != userId &&
            doc.ExpiryDate.HasValue && doc.ExpiryDate.Value <= DateTime.Now)
        {
            return NotFound(new[] { ErrorMessages.Permissions.DocumentExpired });
        }

        var result = await _documentService.DownloadAsync(id, version);
        if (!result.Success)
            return NotFound(result.Errors);

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
            return BadRequest(new[] { ErrorMessages.FileRequired });

        var userId = GetCurrentUserId();

        // Check write permission on target folder
        if (!await HasPermissionAsync(userId, "Folder", dto.FolderId, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.UploadDocumentsToFolder);

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
            return Forbid(ErrorMessages.Permissions.EditDocument);

        var result = await _documentService.UpdateMetadataAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}/content")]
    public async Task<IActionResult> UpdateContent(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new[] { ErrorMessages.FileRequired });

        var userId = GetCurrentUserId();

        // Check write permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.EditDocument);

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
            return Forbid(ErrorMessages.Permissions.CheckOutDocument);

        // Check privacy level on document and folder
        var docResult = await _documentService.GetByIdAsync(id);
        if (docResult.Success && !await HasDocumentPrivacyAccessAsync(docResult.Data!))
            return NotFound(new[] { ErrorMessages.Permissions.InsufficientPrivacyLevel });

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
            return Forbid(ErrorMessages.Permissions.CheckInDocument);

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
            return Forbid(ErrorMessages.Permissions.DiscardCheckout);

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
            return Forbid(ErrorMessages.Permissions.ViewWorkingCopy);

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
            return Forbid(ErrorMessages.Permissions.EditDocument);

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
            return BadRequest(new[] { ErrorMessages.FileRequired });

        var userId = GetCurrentUserId();

        // Check write permission
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.EditDocument);

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
            return Forbid(ErrorMessages.Permissions.ViewDocument);

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
            return Forbid(ErrorMessages.Permissions.RestoreVersion);

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
            return Forbid(ErrorMessages.Permissions.MoveDocument);

        // Check write permission on destination folder
        if (!await HasPermissionAsync(userId, "Folder", dto.NewFolderId, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.MoveDocumentsToFolder);

        // Check privacy level on destination folder
        if (!await HasFolderPrivacyAccessAsync(dto.NewFolderId))
            return NotFound(new[] { ErrorMessages.Permissions.InsufficientPrivacyLevel });

        var result = await _documentService.MoveAsync(id, dto, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    [HttpPost("{id:guid}/copy")]
    public async Task<IActionResult> Copy(Guid id, [FromBody] CopyDocumentDto dto)
    {
        var userId = GetCurrentUserId();

        // Check read permission on source document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Read))
            return Forbid(ErrorMessages.Permissions.CopyDocument);

        // Check write permission on destination folder
        if (!await HasPermissionAsync(userId, "Folder", dto.TargetFolderId, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.CopyDocumentsToFolder);

        // Check privacy level on destination folder
        if (!await HasFolderPrivacyAccessAsync(dto.TargetFolderId))
            return NotFound(new[] { ErrorMessages.Permissions.InsufficientPrivacyLevel });

        var result = await _documentService.CopyAsync(id, dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();

        // Check delete permission on document
        if (!await HasPermissionAsync(userId, "Document", id, (int)PermissionLevel.Delete))
            return Forbid(ErrorMessages.Permissions.DeleteDocument);

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
            return Forbid(ErrorMessages.Permissions.PreviewDocument);

        // Check privacy level and approval status for preview
        var docResult = await _documentService.GetByIdAsync(id);
        if (docResult.Success)
        {
            var doc = docResult.Data!;

            // Check privacy level on document and folder
            if (!await HasDocumentPrivacyAccessAsync(doc))
                return NotFound(new[] { ErrorMessages.Permissions.InsufficientPrivacyLevel });

            if (!IsAdmin() && doc.CreatedBy != userId &&
                doc.ApprovalStatus is (int)ApprovalStatus.Pending or (int)ApprovalStatus.Rejected or (int)ApprovalStatus.ReturnedForRevision)
            {
                if (!await _approvalService.IsApproverForDocumentAsync(id, userId))
                    return NotFound(new[] { ErrorMessages.Permissions.DocumentPendingApproval });
            }

            // Block preview of expired documents for non-creators/non-admins
            if (!IsAdmin() && doc.CreatedBy != userId &&
                doc.ExpiryDate.HasValue && doc.ExpiryDate.Value <= DateTime.Now)
            {
                return NotFound(new[] { ErrorMessages.Permissions.DocumentExpired });
            }
        }

        var result = await _previewService.GetPreviewInfoAsync(id, version);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    // Bulk operation endpoints
    [HttpPost("bulk/delete")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        if (request.DocumentIds == null || request.DocumentIds.Count == 0)
            return BadRequest(new[] { ErrorMessages.NoDocumentsSpecified });

        var userId = GetCurrentUserId();
        var result = await _bulkOperationService.BulkDeleteAsync(request.DocumentIds, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("bulk/move")]
    public async Task<IActionResult> BulkMove([FromBody] BulkMoveRequest request)
    {
        if (request.DocumentIds == null || request.DocumentIds.Count == 0)
            return BadRequest(new[] { ErrorMessages.NoDocumentsSpecified });

        var userId = GetCurrentUserId();
        var result = await _bulkOperationService.BulkMoveAsync(request.DocumentIds, request.TargetFolderId, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("bulk/download")]
    public async Task<IActionResult> BulkDownload([FromBody] BulkDownloadRequest request)
    {
        if (request.DocumentIds == null || request.DocumentIds.Count == 0)
            return BadRequest(new[] { ErrorMessages.NoDocumentsSpecified });

        var userId = GetCurrentUserId();
        var result = await _bulkOperationService.BulkDownloadAsync(request.DocumentIds, userId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return File(result.Data!, "application/zip", $"documents-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
    }
}
