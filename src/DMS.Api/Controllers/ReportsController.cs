using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly ICabinetRepository _cabinetRepository;
    private readonly IFolderRepository _folderRepository;

    public ReportsController(
        IDocumentRepository documentRepository,
        IUserRepository userRepository,
        IActivityLogRepository activityLogRepository,
        ICabinetRepository cabinetRepository,
        IFolderRepository folderRepository)
    {
        _documentRepository = documentRepository;
        _userRepository = userRepository;
        _activityLogRepository = activityLogRepository;
        _cabinetRepository = cabinetRepository;
        _folderRepository = folderRepository;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ReportStatisticsDto>> GetStatistics()
    {
        var cabinets = await _cabinetRepository.GetAllAsync();
        var totalCabinets = cabinets.Count();

        int totalFolders = 0;
        int totalDocuments = 0;
        long totalSizeBytes = 0;
        int documentsThisMonth = 0;
        int documentsLastMonth = 0;

        foreach (var cabinet in cabinets)
        {
            var folders = await _folderRepository.GetByCabinetIdAsync(cabinet.Id);
            totalFolders += folders.Count();

            foreach (var folder in folders)
            {
                var documents = await _documentRepository.GetByFolderIdAsync(folder.Id);
                var docsList = documents.ToList();
                totalDocuments += docsList.Count;
                totalSizeBytes += docsList.Sum(d => d.Size);

                var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var lastMonthStart = thisMonthStart.AddMonths(-1);

                documentsThisMonth += docsList.Count(d => d.CreatedAt >= thisMonthStart);
                documentsLastMonth += docsList.Count(d => d.CreatedAt >= lastMonthStart && d.CreatedAt < thisMonthStart);
            }
        }

        var users = await _userRepository.GetAllAsync();
        var totalUsers = users.Count();

        // Calculate growth percentage
        var documentsGrowth = documentsLastMonth > 0
            ? (int)Math.Round(((double)(documentsThisMonth - documentsLastMonth) / documentsLastMonth) * 100)
            : documentsThisMonth > 0 ? 100 : 0;

        // Storage in GB (assuming 1TB capacity for percentage calculation)
        var storageUsedGB = Math.Round((double)totalSizeBytes / (1024 * 1024 * 1024), 1);
        var storageCapacityPercent = (int)Math.Min(Math.Round(storageUsedGB / 1024 * 100), 100); // Assuming 1TB capacity

        return Ok(new ReportStatisticsDto
        {
            TotalDocuments = totalDocuments,
            DocumentsGrowth = documentsGrowth,
            StorageUsedGB = storageUsedGB,
            StorageCapacityPercent = storageCapacityPercent,
            ActiveWorkflows = 0, // TODO: Implement when workflows are ready
            PendingWorkflows = 0,
            TotalUsers = totalUsers,
            OnlineUsers = 1 // Current user at minimum
        });
    }

    [HttpGet("monthly-growth")]
    public async Task<ActionResult<IEnumerable<MonthlyGrowthDto>>> GetMonthlyGrowth([FromQuery] int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var months = new List<MonthlyGrowthDto>();
        var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        // Get all documents once
        var allDocuments = new List<DAL.Entities.Document>();
        var cabinets = await _cabinetRepository.GetAllAsync();
        foreach (var cabinet in cabinets)
        {
            var folders = await _folderRepository.GetByCabinetIdAsync(cabinet.Id);
            foreach (var folder in folders)
            {
                var documents = await _documentRepository.GetByFolderIdAsync(folder.Id);
                allDocuments.AddRange(documents);
            }
        }

        for (int month = 1; month <= 12; month++)
        {
            var monthStart = new DateTime(targetYear, month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var prevYearMonthStart = new DateTime(targetYear - 1, month, 1);
            var prevYearMonthEnd = prevYearMonthStart.AddMonths(1);

            var currentYearCount = allDocuments.Count(d => d.CreatedAt >= monthStart && d.CreatedAt < monthEnd);
            var previousYearCount = allDocuments.Count(d => d.CreatedAt >= prevYearMonthStart && d.CreatedAt < prevYearMonthEnd);

            months.Add(new MonthlyGrowthDto
            {
                Month = monthNames[month - 1],
                CurrentYear = currentYearCount,
                PreviousYear = previousYearCount
            });
        }

        // Only return months up to current month if current year
        if (targetYear == DateTime.UtcNow.Year)
        {
            months = months.Take(DateTime.UtcNow.Month).ToList();
        }

        return Ok(months);
    }

    [HttpGet("document-types")]
    public async Task<ActionResult<IEnumerable<DocumentTypeDistributionDto>>> GetDocumentTypes()
    {
        var allDocuments = new List<DAL.Entities.Document>();
        var cabinets = await _cabinetRepository.GetAllAsync();
        foreach (var cabinet in cabinets)
        {
            var folders = await _folderRepository.GetByCabinetIdAsync(cabinet.Id);
            foreach (var folder in folders)
            {
                var documents = await _documentRepository.GetByFolderIdAsync(folder.Id);
                allDocuments.AddRange(documents);
            }
        }

        var totalDocs = allDocuments.Count;
        if (totalDocs == 0)
        {
            return Ok(new List<DocumentTypeDistributionDto>
            {
                new() { Type = "No Documents", Percentage = 100, Color = "slate" }
            });
        }

        // Group by file extension
        var groups = allDocuments
            .GroupBy(d => GetDocumentCategory(d.ContentType ?? d.Extension))
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var result = groups.Select(g => new DocumentTypeDistributionDto
        {
            Type = g.Category,
            Percentage = (int)Math.Round((double)g.Count / totalDocs * 100),
            Color = GetColorForCategory(g.Category)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivity([FromQuery] int take = 10)
    {
        var logs = await _activityLogRepository.GetRecentAsync(take);

        // Get all unique user IDs that need name lookup
        var userIds = logs
            .Where(l => l.UserId.HasValue && string.IsNullOrEmpty(l.UserName))
            .Select(l => l.UserId!.Value)
            .Distinct()
            .ToList();

        // Lookup user names
        var userNames = new Dictionary<Guid, string>();
        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
            {
                userNames[userId] = user.DisplayName ?? user.Username ?? "Unknown User";
            }
        }

        var result = logs.Select(log =>
        {
            // Get username from log, or lookup from dictionary, or default
            var userName = log.UserName;
            if (string.IsNullOrEmpty(userName) && log.UserId.HasValue && userNames.TryGetValue(log.UserId.Value, out var lookupName))
            {
                userName = lookupName;
            }
            userName ??= "Unknown User";

            return new RecentActivityDto
            {
                Id = log.Id.ToString(),
                UserName = userName,
                UserInitials = GetInitials(userName),
                Action = GetActionLabel(log.Action),
                ActionType = GetActionType(log.Action),
                TargetName = log.NodeName ?? log.Details ?? "Unknown",
                CreatedAt = FormatDate(log.CreatedAt),
                IpAddress = log.IpAddress ?? "N/A"
            };
        }).ToList();

        return Ok(result);
    }

    private static string GetDocumentCategory(string? contentTypeOrExtension)
    {
        if (string.IsNullOrEmpty(contentTypeOrExtension)) return "Other";

        var lower = contentTypeOrExtension.ToLower();

        if (lower.Contains("pdf")) return "PDF Documents";
        if (lower.Contains("word") || lower.Contains("doc") || lower.Contains("docx") ||
            lower.Contains("excel") || lower.Contains("xls") || lower.Contains("xlsx") ||
            lower.Contains("powerpoint") || lower.Contains("ppt") || lower.Contains("pptx") ||
            lower.Contains("office")) return "Word / Office";
        if (lower.Contains("image") || lower.Contains("jpg") || lower.Contains("jpeg") ||
            lower.Contains("png") || lower.Contains("gif") || lower.Contains("bmp") ||
            lower.Contains("video") || lower.Contains("audio") || lower.Contains("mp3") ||
            lower.Contains("mp4")) return "Media & Other";

        return "Media & Other";
    }

    private static string GetColorForCategory(string category)
    {
        return category switch
        {
            "PDF Documents" => "teal",
            "Word / Office" => "navy",
            _ => "slate"
        };
    }

    private static string GetInitials(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "??";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[1][0]}".ToUpper();
        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }

    private static string GetActionLabel(string? action)
    {
        if (string.IsNullOrEmpty(action)) return "ACTION";
        return action.ToUpper() switch
        {
            "DOCUMENTCREATED" or "CREATED" or "UPLOAD" => "UPLOAD",
            "DOCUMENTUPDATED" or "UPDATED" or "EDIT" => "EDIT",
            "DOCUMENTDELETED" or "DELETED" or "DELETE" => "DELETE",
            "DOCUMENTDOWNLOADED" or "DOWNLOAD" => "DOWNLOAD",
            "LOGIN" or "USERLOGGEDIN" => "LOGIN",
            "LOGOUT" or "USERLOGGEDOUT" => "LOGOUT",
            "CHECKOUT" or "DOCUMENTCHECKEDOUT" => "CHECKOUT",
            "CHECKIN" or "DOCUMENTCHECKEDIN" => "CHECKIN",
            _ => action.Length > 10 ? action[..10].ToUpper() : action.ToUpper()
        };
    }

    private static string GetActionType(string? action)
    {
        if (string.IsNullOrEmpty(action)) return "other";
        var lower = action.ToLower();
        if (lower.Contains("create") || lower.Contains("upload")) return "upload";
        if (lower.Contains("update") || lower.Contains("edit") || lower.Contains("checkin")) return "edit";
        if (lower.Contains("delete")) return "delete";
        if (lower.Contains("download")) return "download";
        if (lower.Contains("login")) return "login";
        if (lower.Contains("checkout")) return "checkout";
        return "other";
    }

    private static string FormatDate(DateTime date)
    {
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);

        if (date.Date == today)
            return $"Today, {date:h:mm tt}";
        if (date.Date == yesterday)
            return $"Yesterday, {date:h:mm tt}";
        return date.ToString("MMM d, h:mm tt");
    }
}

// DTOs
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
