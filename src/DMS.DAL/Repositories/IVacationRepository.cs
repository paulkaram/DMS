using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IVacationRepository
{
    Task<Vacation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Vacation>> GetByUserIdAsync(Guid userId);
    Task<Vacation?> GetActiveVacationAsync(Guid userId);
    Task<IEnumerable<Vacation>> GetAllActiveAsync();
    Task<Guid> CreateAsync(Vacation entity);
    Task<bool> UpdateAsync(Vacation entity);
    Task<bool> DeleteAsync(Guid id);
}
