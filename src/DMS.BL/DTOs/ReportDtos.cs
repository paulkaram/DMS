namespace DMS.BL.DTOs;

public class ReportStatisticsDto
{
    public int TotalDocuments { get; set; }
    public int DocumentsGrowth { get; set; }
    public double StorageUsedGB { get; set; }
    public int StorageCapacityPercent { get; set; }
    public int ActiveWorkflows { get; set; }
    public int PendingWorkflows { get; set; }
    public int TotalUsers { get; set; }
    public int OnlineUsers { get; set; }
}

public class MonthlyGrowthDto
{
    public string Month { get; set; } = string.Empty;
    public int CurrentYear { get; set; }
    public int PreviousYear { get; set; }
}

public class DocumentTypeDistributionDto
{
    public string Type { get; set; } = string.Empty;
    public int Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class RecentActivityDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserInitials { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
