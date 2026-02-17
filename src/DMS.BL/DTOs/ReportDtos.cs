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

public class AccessReviewEntryDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PermissionLevel { get; set; } = string.Empty;
    public string PermissionSource { get; set; } = string.Empty;
    public Guid? SourceNodeId { get; set; }
    public string? SourceNodeName { get; set; }
    public DateTime? LastAccessDate { get; set; }
}

public class AccessReviewReportDto
{
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public int TotalUsersWithAccess { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public List<AccessReviewEntryDto> Entries { get; set; } = new();
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
