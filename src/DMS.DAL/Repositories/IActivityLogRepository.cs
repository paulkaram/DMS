using DMS.DAL.DTOs;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IActivityLogRepository
{
    Task<IEnumerable<ActivityLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int skip = 0, int take = 50);
    Task<IEnumerable<ActivityLog>> GetByUserAsync(Guid userId, int skip = 0, int take = 50);
    Task<IEnumerable<ActivityLog>> GetRecentAsync(int take = 100);
    Task<PagedResult<ActivityLog>> GetByNodePagedAsync(NodeType nodeType, Guid nodeId, int page = 1, int pageSize = 50);
    Task<PagedResult<ActivityLog>> GetByUserPagedAsync(Guid userId, int page = 1, int pageSize = 50);
    Task<PagedResult<ActivityLog>> GetRecentPagedAsync(int page = 1, int pageSize = 50);
    Task<string?> GetLastEntryHashAsync();
    Task<IEnumerable<ActivityLog>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, string? action, string? nodeType, Guid? userId);
    Task<Guid> CreateAsync(ActivityLog entity);
}
