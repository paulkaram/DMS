using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentAnnotationRepository
{
    Task<IEnumerable<DocumentAnnotation>> GetByDocumentIdAsync(Guid documentId);
    Task<DocumentAnnotation?> GetByDocumentAndPageAsync(Guid documentId, int pageNumber);
    Task<DocumentAnnotation?> GetByIdAsync(Guid id);
    Task<Guid> UpsertAsync(DocumentAnnotation annotation);
    Task<bool> DeleteAsync(Guid id, Guid deletedBy);
    Task<bool> DeleteAllByDocumentAsync(Guid documentId, Guid deletedBy);
    Task<int> GetCountAsync(Guid documentId);
}

public interface ISavedSignatureRepository
{
    Task<IEnumerable<SavedSignature>> GetByUserIdAsync(Guid userId);
    Task<SavedSignature?> GetByIdAsync(Guid id);
    Task<Guid> AddAsync(SavedSignature signature);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> SetDefaultAsync(Guid userId, Guid signatureId);
}
