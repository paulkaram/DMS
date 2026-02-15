using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDocumentService
{
    Task<ServiceResult<DocumentDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<DocumentDto>>> GetByFolderIdAsync(Guid folderId);
    Task<ServiceResult<List<DocumentDto>>> SearchAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId);
    Task<ServiceResult<PagedResultDto<DocumentDto>>> SearchPaginatedAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId, int page, int pageSize, int? userPrivacyLevel = null);
    Task<ServiceResult<List<DocumentDto>>> GetCheckedOutByUserAsync(Guid userId);
    Task<ServiceResult<List<DocumentDto>>> GetCreatedByUserAsync(Guid userId, int take = 50);
    Task<ServiceResult<List<DocumentVersionDto>>> GetVersionsAsync(Guid documentId);
    Task<ServiceResult<DocumentDto>> CreateAsync(CreateDocumentDto dto, Stream fileStream, string fileName, string contentType, Guid userId);
    Task<ServiceResult<DocumentDto>> UpdateMetadataAsync(Guid id, UpdateDocumentDto dto, Guid userId);
    Task<ServiceResult<DocumentDto>> UpdateContentAsync(Guid id, Stream fileStream, string fileName, string contentType, Guid userId);
    Task<ServiceResult<Stream>> DownloadAsync(Guid id, int? version = null);
    Task<ServiceResult> MoveAsync(Guid id, MoveDocumentDto dto, Guid userId);
    Task<ServiceResult<DocumentDto>> CopyAsync(Guid id, CopyDocumentDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id, Guid userId);

    // ISO 15489 Checkout System
    /// <summary>
    /// Checks out a document for exclusive editing. Creates a working copy for draft storage.
    /// </summary>
    Task<ServiceResult> CheckOutAsync(Guid id, Guid userId);

    /// <summary>
    /// Checks in a document with optional file content. Supports Major/Minor versioning.
    /// File is optional - allows metadata-only check-ins.
    /// </summary>
    Task<ServiceResult<DocumentVersionDto>> CheckInAsync(Guid id, Stream? fileStream, string? fileName,
        string? contentType, CheckInDto dto, Guid userId);

    /// <summary>
    /// Discards checkout and deletes all draft changes.
    /// </summary>
    Task<ServiceResult> DiscardCheckOutAsync(Guid id, Guid userId);

    /// <summary>
    /// Admin-only: Force discards another user's checkout.
    /// </summary>
    Task<ServiceResult> ForceDiscardCheckOutAsync(Guid id, Guid adminUserId, string reason);

    // Working Copy Management
    /// <summary>
    /// Gets the current working copy (draft state) for a checked-out document.
    /// </summary>
    Task<ServiceResult<WorkingCopyDto>> GetWorkingCopyAsync(Guid documentId, Guid userId);

    /// <summary>
    /// Saves draft changes to the working copy (auto-save or manual save).
    /// </summary>
    Task<ServiceResult> SaveWorkingCopyAsync(Guid documentId, SaveWorkingCopyDto dto, Guid userId);

    /// <summary>
    /// Saves draft file content to the working copy.
    /// </summary>
    Task<ServiceResult> SaveWorkingCopyContentAsync(Guid documentId, Stream fileStream,
        string fileName, string contentType, Guid userId);

    // Version Comparison
    /// <summary>
    /// Compares two versions showing content and metadata differences.
    /// </summary>
    Task<ServiceResult<VersionComparisonDto>> CompareVersionsAsync(Guid documentId,
        Guid sourceVersionId, Guid targetVersionId);

    /// <summary>
    /// Restores a previous version (creates a new major version).
    /// </summary>
    Task<ServiceResult<DocumentVersionDto>> RestoreVersionAsync(Guid documentId,
        Guid versionId, RestoreVersionDto dto, Guid userId);

    /// <summary>
    /// Gets stale checkouts for admin cleanup.
    /// </summary>
    Task<ServiceResult<List<WorkingCopyDto>>> GetStaleCheckoutsAsync(int staleHours);
}
