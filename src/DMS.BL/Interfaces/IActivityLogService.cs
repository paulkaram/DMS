using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IActivityLogService
{
    Task<ServiceResult<List<ActivityLogDto>>> GetByNodeAsync(string nodeType, Guid nodeId, int skip = 0, int take = 50);
    Task<ServiceResult<List<ActivityLogDto>>> GetByUserAsync(Guid userId, int skip = 0, int take = 50);
    Task<ServiceResult<List<ActivityLogDto>>> GetRecentAsync(int take = 100);
    Task<ServiceResult<PagedResultDto<ActivityLogDto>>> GetByNodePagedAsync(string nodeType, Guid nodeId, int page = 1, int pageSize = 50);
    Task<ServiceResult<PagedResultDto<ActivityLogDto>>> GetByUserPagedAsync(Guid userId, int page = 1, int pageSize = 50);
    Task<ServiceResult<PagedResultDto<ActivityLogDto>>> GetRecentPagedAsync(int page = 1, int pageSize = 50);
    Task LogActivityAsync(string action, string? nodeType, Guid? nodeId, string? nodeName, string? details, Guid? userId, string? userName, string? ipAddress);
}
