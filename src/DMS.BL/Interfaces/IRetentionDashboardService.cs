using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IRetentionDashboardService
{
    Task<ServiceResult<RetentionDashboardDto>> GetDashboardAsync();
    Task<ServiceResult<List<RetentionActionDto>>> GetRecentActionsAsync(int take = 20);
    Task<ServiceResult<List<BackgroundJobDto>>> GetJobHistoryAsync(int take = 10);
}
