using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

/// <summary>
/// Repository for managing document working copies during checkout.
/// Working copies store draft changes until check-in or discard.
/// </summary>
public interface IDocumentWorkingCopyRepository
{
    /// <summary>
    /// Gets the working copy for a document.
    /// </summary>
    Task<DocumentWorkingCopy?> GetByDocumentIdAsync(Guid documentId);

    /// <summary>
    /// Gets all working copies for a user (their checked out documents).
    /// </summary>
    Task<IEnumerable<DocumentWorkingCopy>> GetAllByUserAsync(Guid userId);

    /// <summary>
    /// Creates a new working copy (when document is checked out).
    /// </summary>
    Task<Guid> CreateAsync(DocumentWorkingCopy workingCopy);

    /// <summary>
    /// Updates the working copy (saves draft changes).
    /// </summary>
    Task<bool> UpdateAsync(DocumentWorkingCopy workingCopy);

    /// <summary>
    /// Deletes the working copy (on check-in or discard).
    /// </summary>
    Task<bool> DeleteAsync(Guid documentId);

    /// <summary>
    /// Gets all stale checkouts (for admin cleanup).
    /// </summary>
    Task<IEnumerable<DocumentWorkingCopy>> GetStaleCheckoutsAsync(int staleHours);

    /// <summary>
    /// Deletes all working copies for a user (when user is deleted).
    /// </summary>
    Task<bool> DeleteAllByUserAsync(Guid userId);
}
