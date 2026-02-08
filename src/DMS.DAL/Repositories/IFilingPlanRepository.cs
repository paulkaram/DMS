using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IFilingPlanRepository
{
    Task<IEnumerable<FilingPlan>> GetByFolderAsync(Guid folderId);
    Task<FilingPlan?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(FilingPlan entity);
    Task<bool> UpdateAsync(FilingPlan entity);
    Task<bool> DeleteAsync(Guid id);
}
