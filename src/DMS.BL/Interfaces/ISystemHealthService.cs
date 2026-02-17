using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface ISystemHealthService
{
    Task<SystemHealthDto> GetSystemHealthAsync();
    Task<List<JobExecutionSummaryDto>> GetJobHistoryAsync(string? jobName, int page, int pageSize);
    Task<Guid> RecordJobStartAsync(string jobName);
    Task RecordJobCompletionAsync(Guid executionId, int itemsProcessed, int itemsFailed);
    Task RecordJobFailureAsync(Guid executionId, string errorMessage);
}
