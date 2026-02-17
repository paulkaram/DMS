namespace DMS.BL.DTOs;

public class SystemHealthDto
{
    public bool IsHealthy { get; set; }
    public DatabaseHealthDto Database { get; set; } = new();
    public SearchHealthDto? Search { get; set; }
    public StorageHealthDto Storage { get; set; } = new();
    public List<JobExecutionSummaryDto> RecentJobs { get; set; } = new();
}

public class DatabaseHealthDto
{
    public bool IsAvailable { get; set; }
    public string? ServerVersion { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveLegalHolds { get; set; }
}

public class StorageHealthDto
{
    public string BasePath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long AvailableBytes { get; set; }
    public double UsagePercent { get; set; }
}

public class JobExecutionSummaryDto
{
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double? DurationMs { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsFailed { get; set; }
    public string? ErrorMessage { get; set; }
}

public class JobHistoryRequest
{
    public string? JobName { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
