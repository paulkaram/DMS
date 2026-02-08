using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IContentTypeRepository
{
    Task<IEnumerable<ContentType>> GetAllAsync();
    Task<ContentType?> GetByIdAsync(Guid id);
    Task<ContentType?> GetByExtensionAsync(string extension);
    Task<Guid> CreateAsync(ContentType entity);
    Task<bool> UpdateAsync(ContentType entity);
    Task<bool> DeleteAsync(Guid id);
}
