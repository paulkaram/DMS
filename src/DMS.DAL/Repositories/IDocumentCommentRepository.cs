using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentCommentRepository
{
    Task<IEnumerable<DocumentComment>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DocumentComment>> GetRepliesAsync(Guid parentCommentId);
    Task<DocumentComment?> GetByIdAsync(Guid id);
    Task<Guid> AddAsync(DocumentComment comment);
    Task<bool> UpdateAsync(DocumentComment comment);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    Task<int> GetCommentCountAsync(Guid documentId);
}
