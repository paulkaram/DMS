namespace DMS.BL.DTOs;

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? NodeType { get; set; }
    public Guid? NodeId { get; set; }
    public string? NodeName { get; set; }
    public string? Details { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceType { get; set; }
    public string? EntryHash { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogQueryDto
{
    public Guid? NodeId { get; set; }
    public string? NodeType { get; set; }
    public Guid? UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class AuditExportQueryDto
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Action { get; set; }
    public string? NodeType { get; set; }
    public Guid? UserId { get; set; }
}

public class AuditChainVerificationResult
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int TamperedEntries { get; set; }
    public int BrokenLinks { get; set; }
    public bool IsValid { get; set; }
    public DateTime VerifiedAt { get; set; }
}
