using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentLinkRepository
{
    Task<IEnumerable<DocumentLink>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DocumentLink>> GetIncomingLinksAsync(Guid documentId);
    Task<DocumentLink?> GetByIdAsync(Guid id);
    Task<DocumentLink?> GetExistingLinkAsync(Guid sourceDocumentId, Guid targetDocumentId);
    Task<Guid> AddAsync(DocumentLink link);
    Task<bool> UpdateAsync(DocumentLink link);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetLinkCountAsync(Guid documentId);
}
