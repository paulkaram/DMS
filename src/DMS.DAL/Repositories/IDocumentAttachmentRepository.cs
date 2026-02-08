using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentAttachmentRepository
{
    Task<IEnumerable<DocumentAttachment>> GetByDocumentIdAsync(Guid documentId);
    Task<DocumentAttachment?> GetByIdAsync(Guid id);
    Task<Guid> AddAsync(DocumentAttachment attachment);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetAttachmentCountAsync(Guid documentId);
    Task<long> GetTotalSizeAsync(Guid documentId);
}
