using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IRecycleBinRepository
{
    Task<IEnumerable<RecycleBinItem>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<RecycleBinItem>> GetAllAsync(int? nodeType = null);
    Task<(List<RecycleBinItem> Items, int TotalCount)> GetByUserIdPaginatedAsync(Guid userId, int page, int pageSize);
    Task<(List<RecycleBinItem> Items, int TotalCount)> GetAllPaginatedAsync(int? nodeType, int page, int pageSize);
    Task<RecycleBinItem?> GetByIdAsync(Guid id);
    Task<Guid> AddAsync(RecycleBinItem entity);
    Task<bool> RemoveAsync(Guid id);
    Task<bool> PurgeExpiredAsync();
}
