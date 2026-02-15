namespace DMS.DAL.Repositories;

public interface IDashboardRepository
{
    Task<int> GetCabinetCountAsync();
    Task<int> GetFolderCountAsync();
    Task<int> GetDocumentCountAsync();
    Task<int> GetUserCountAsync();
    Task<int> GetDocumentsThisMonthAsync();
    Task<int> GetDocumentsThisYearAsync();
    Task<long> GetTotalStorageUsedAsync();
    Task<int> GetCheckedOutCountAsync(Guid? userId = null);
    Task<IEnumerable<ContentTypeStat>> GetContentTypeDistributionAsync();
    Task<IEnumerable<RecentDocument>> GetRecentDocumentsAsync(int take = 10, int? userPrivacyLevel = null);
    Task<IEnumerable<MonthlyStat>> GetMonthlyDocumentStatsAsync(int months = 12);
    Task<int> GetExpiredDocumentCountAsync();
    Task<int> GetExpiringSoonCountAsync(int days = 7);
    Task<IEnumerable<ExpiredDocument>> GetExpiredDocumentsAsync(int take = 5, int? userPrivacyLevel = null);
    Task<int> GetPendingApprovalCountAsync(Guid userId);
}

public class ContentTypeStat
{
    public string ContentType { get; set; } = string.Empty;
    public int Count { get; set; }
    public long TotalSize { get; set; }
}

public class RecentDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FolderName { get; set; }
    public string? Extension { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
}

public class MonthlyStat
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}

public class ExpiredDocument
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? CreatedByName { get; set; }
}
