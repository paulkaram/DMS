using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDashboardService
{
    Task<ServiceResult<DashboardStatisticsDto>> GetStatisticsAsync(Guid userId);
    Task<ServiceResult<List<RecentDocumentDto>>> GetRecentDocumentsAsync(int take = 10, int? userPrivacyLevel = null);
    Task<ServiceResult<List<DocumentDto>>> GetMyCheckedOutDocumentsAsync(Guid userId);
    Task<ServiceResult<List<ExpiredDocumentDto>>> GetExpiredDocumentsAsync(int take = 5, int? userPrivacyLevel = null);
}
