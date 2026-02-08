using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IActivityLogRepository
{
    Task<IEnumerable<ActivityLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int skip = 0, int take = 50);
    Task<IEnumerable<ActivityLog>> GetByUserAsync(Guid userId, int skip = 0, int take = 50);
    Task<IEnumerable<ActivityLog>> GetRecentAsync(int take = 100);
    Task<Guid> CreateAsync(ActivityLog entity);
}
