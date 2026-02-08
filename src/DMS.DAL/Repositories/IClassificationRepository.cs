using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IClassificationRepository
{
    Task<Classification?> GetByIdAsync(Guid id);
    Task<IEnumerable<Classification>> GetAllAsync(string? language = null);
    Task<IEnumerable<Classification>> SearchAsync(string? name, string? language = null);
    Task<Guid> CreateAsync(Classification entity);
    Task<bool> UpdateAsync(Classification entity);
    Task<bool> DeleteAsync(Guid id);
}

public interface IImportanceRepository
{
    Task<Importance?> GetByIdAsync(Guid id);
    Task<IEnumerable<Importance>> GetAllAsync(string? language = null);
    Task<Guid> CreateAsync(Importance entity);
    Task<bool> UpdateAsync(Importance entity);
    Task<bool> DeleteAsync(Guid id);
}

public interface IDocumentTypeRepository
{
    Task<DocumentType?> GetByIdAsync(Guid id);
    Task<IEnumerable<DocumentType>> GetAllAsync(string? language = null);
    Task<IEnumerable<DocumentType>> SearchAsync(string? name, string? language = null);
    Task<Guid> CreateAsync(DocumentType entity);
    Task<bool> UpdateAsync(DocumentType entity);
    Task<bool> DeleteAsync(Guid id);
}

public interface ILookupRepository
{
    Task<Lookup?> GetByIdAsync(Guid id);
    Task<Lookup?> GetByNameAsync(string name);
    Task<IEnumerable<Lookup>> GetAllAsync();
    Task<IEnumerable<LookupItem>> GetItemsByLookupIdAsync(Guid lookupId, string? language = null);
    Task<IEnumerable<LookupItem>> GetItemsByLookupNameAsync(string lookupName, string? language = null);
    Task<Guid> CreateAsync(Lookup entity);
    Task<Guid> CreateItemAsync(LookupItem entity);
}
