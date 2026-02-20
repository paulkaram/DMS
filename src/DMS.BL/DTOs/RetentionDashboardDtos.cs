namespace DMS.BL.DTOs;

public class RetentionDashboardDto
{
    public int TotalDocumentsUnderRetention { get; set; }
    public int ActiveRetentions { get; set; }
    public int PendingReview { get; set; }
    public int ExpiringSoon30 { get; set; }
    public int ExpiringSoon7 { get; set; }
    public int OnHold { get; set; }
    public int Archived { get; set; }
    public int Disposed { get; set; }
    public int AwaitingTrigger { get; set; }
    public List<RetentionActionDto> RecentActions { get; set; } = new();
    public List<RetentionPolicySummaryDto> RetentionsByPolicy { get; set; } = new();
    public List<BackgroundJobDto> BackgroundJobs { get; set; } = new();
    public List<UpcomingExpirationDto> UpcomingExpirations { get; set; } = new();
    public List<ExpirationTimelineDto> ExpirationTimeline { get; set; } = new();
}

public class RetentionActionDto
{
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool IsSystemAction { get; set; }
    public string? Notes { get; set; }
}

public class RetentionPolicySummaryDto
{
    public Guid PolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string ExpirationAction { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int ActiveCount { get; set; }
    public int ExpiredCount { get; set; }
    public int OnHoldCount { get; set; }
}

public class BackgroundJobDto
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsFailed { get; set; }
    public double? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UpcomingExpirationDto
{
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; }
    public int DaysRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ExpirationTimelineDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
