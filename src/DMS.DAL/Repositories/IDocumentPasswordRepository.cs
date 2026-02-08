using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentPasswordRepository
{
    Task<DocumentPassword?> GetByDocumentIdAsync(Guid documentId);
    Task<DocumentPassword?> GetByIdAsync(Guid id);
    Task<Guid> AddAsync(DocumentPassword password);
    Task<bool> UpdateAsync(DocumentPassword password);
    Task<bool> DeleteAsync(Guid documentId);
    Task<bool> HasPasswordAsync(Guid documentId);
    Task<bool> ValidatePasswordAsync(Guid documentId, string passwordHash);
    Task<Dictionary<Guid, bool>> GetPasswordStatusBulkAsync(List<Guid> documentIds);
}
