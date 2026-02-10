namespace DMS.BL.DTOs;

public class DashboardStatisticsDto
{
    public int TotalCabinets { get; set; }
    public int TotalFolders { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalUsers { get; set; }
    public int DocumentsThisMonth { get; set; }
    public int DocumentsThisYear { get; set; }
    public long TotalStorageUsed { get; set; }
    public int MyCheckoutsCount { get; set; }
    public List<ContentTypeStatDto> ContentTypeDistribution { get; set; } = new();
}

public class ContentTypeStatDto
{
    public string ContentType { get; set; } = string.Empty;
    public int Count { get; set; }
    public long TotalSize { get; set; }
}

public class RecentDocumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FolderName { get; set; }
    public string? Extension { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
}
