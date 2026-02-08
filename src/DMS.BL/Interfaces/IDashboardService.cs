using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDashboardService
{
    Task<ServiceResult<DashboardStatisticsDto>> GetStatisticsAsync();
    Task<ServiceResult<List<RecentDocumentDto>>> GetRecentDocumentsAsync(int take = 10);
    Task<ServiceResult<List<DocumentDto>>> GetMyCheckedOutDocumentsAsync(Guid userId);
}
