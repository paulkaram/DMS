using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IPrivacyLevelRepository
{
    Task<PrivacyLevel?> GetByIdAsync(Guid id);
    Task<IEnumerable<PrivacyLevel>> GetAllAsync(bool includeInactive = false);
    Task<Guid> CreateAsync(PrivacyLevel entity);
    Task<bool> UpdateAsync(PrivacyLevel entity);
    Task<bool> DeleteAsync(Guid id);
}
