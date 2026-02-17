using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentShareRepository
{
    Task<DocumentShare?> GetByIdAsync(Guid id);
    Task<IEnumerable<DocumentShare>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DocumentShare>> GetSharedWithUserAsync(Guid userId);
    Task<IEnumerable<DocumentShare>> GetSharedByUserAsync(Guid userId);
    Task<Guid> CreateAsync(DocumentShare entity);
    Task<bool> UpdateAsync(DocumentShare entity);
    Task<bool> DeleteAsync(Guid id);

    // Link sharing
    Task<DocumentShare?> GetByShareTokenAsync(string shareToken);
    Task<DocumentShare?> GetActiveLinkShareByDocumentAsync(Guid documentId);
}
