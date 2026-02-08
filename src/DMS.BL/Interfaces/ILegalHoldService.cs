using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

/// <summary>
/// ISO 15489 compliant legal hold management service.
/// Manages preservation of documents during litigation or investigation.
/// </summary>
public interface ILegalHoldService
{
    /// <summary>
    /// Creates a new legal hold matter.
    /// </summary>
    Task<ServiceResult<LegalHoldDto>> CreateHoldAsync(CreateLegalHoldDto dto, Guid userId);

    /// <summary>
    /// Gets a legal hold by ID.
    /// </summary>
    Task<ServiceResult<LegalHoldDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all active legal holds.
    /// </summary>
    Task<ServiceResult<List<LegalHoldDto>>> GetActiveHoldsAsync();

    /// <summary>
    /// Gets all legal holds (active and released).
    /// </summary>
    Task<ServiceResult<List<LegalHoldDto>>> GetAllHoldsAsync();

    /// <summary>
    /// Adds documents to a legal hold.
    /// </summary>
    Task<ServiceResult> AddDocumentsToHoldAsync(Guid holdId, List<Guid> documentIds, Guid userId, string? notes = null);

    /// <summary>
    /// Removes a document from a legal hold.
    /// </summary>
    Task<ServiceResult> RemoveDocumentFromHoldAsync(Guid holdId, Guid documentId, Guid userId);

    /// <summary>
    /// Gets all documents under a legal hold.
    /// </summary>
    Task<ServiceResult<List<LegalHoldDocumentDto>>> GetHoldDocumentsAsync(Guid holdId);

    /// <summary>
    /// Gets all legal holds affecting a document.
    /// </summary>
    Task<ServiceResult<List<LegalHoldDto>>> GetDocumentHoldsAsync(Guid documentId);

    /// <summary>
    /// Checks if a document is under any legal hold.
    /// </summary>
    Task<bool> IsDocumentOnHoldAsync(Guid documentId);

    /// <summary>
    /// Releases a legal hold (marks it as released, doesn't delete).
    /// </summary>
    Task<ServiceResult> ReleaseHoldAsync(Guid holdId, Guid userId, string reason);

    /// <summary>
    /// Updates a legal hold.
    /// </summary>
    Task<ServiceResult<LegalHoldDto>> UpdateHoldAsync(Guid id, UpdateLegalHoldDto dto, Guid userId);
}
