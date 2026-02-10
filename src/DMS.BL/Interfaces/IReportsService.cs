using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IReportsService
{
    Task<ServiceResult<ReportStatisticsDto>> GetStatisticsAsync();
    Task<ServiceResult<List<MonthlyGrowthDto>>> GetMonthlyGrowthAsync(int? year = null);
    Task<ServiceResult<List<DocumentTypeDistributionDto>>> GetDocumentTypesAsync();
    Task<ServiceResult<List<RecentActivityDto>>> GetRecentActivityAsync(int take = 10);
}
