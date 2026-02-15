using DMS.BL.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class ReportsService : IReportsService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly ICabinetRepository _cabinetRepository;
    private readonly IFolderRepository _folderRepository;

    private static readonly string[] MonthNames =
        { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    public ReportsService(
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

    public async Task<ServiceResult<ReportStatisticsDto>> GetStatisticsAsync()
    {
        var allDocuments = await GetAllDocumentsAsync();

        var totalDocuments = allDocuments.Count;
        var totalSizeBytes = allDocuments.Sum(d => d.Size);

        var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var documentsThisMonth = allDocuments.Count(d => d.CreatedAt >= thisMonthStart);
        var documentsLastMonth = allDocuments.Count(d => d.CreatedAt >= lastMonthStart && d.CreatedAt < thisMonthStart);

        var cabinets = await _cabinetRepository.GetAllAsync();
        var totalFolders = 0;
        foreach (var cabinet in cabinets)
        {
            var folders = await _folderRepository.GetByCabinetIdAsync(cabinet.Id);
            totalFolders += folders.Count();
        }

        var users = await _userRepository.GetAllAsync();
        var totalUsers = users.Count();

        var documentsGrowth = documentsLastMonth > 0
            ? (int)Math.Round(((double)(documentsThisMonth - documentsLastMonth) / documentsLastMonth) * 100)
            : documentsThisMonth > 0 ? 100 : 0;

        var storageUsedGB = Math.Round((double)totalSizeBytes / (1024 * 1024 * 1024), 1);
        var storageCapacityPercent = (int)Math.Min(Math.Round(storageUsedGB / AppConstants.StorageCapacityGB * 100), 100);

        return ServiceResult<ReportStatisticsDto>.Ok(new ReportStatisticsDto
        {
            TotalDocuments = totalDocuments,
            DocumentsGrowth = documentsGrowth,
            StorageUsedGB = storageUsedGB,
            StorageCapacityPercent = storageCapacityPercent,
            ActiveWorkflows = 0,
            PendingWorkflows = 0,
            TotalUsers = totalUsers,
            OnlineUsers = 1
        });
    }

    public async Task<ServiceResult<List<MonthlyGrowthDto>>> GetMonthlyGrowthAsync(int? year = null)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var allDocuments = await GetAllDocumentsAsync();

        var months = new List<MonthlyGrowthDto>();
        for (int month = 1; month <= 12; month++)
        {
            var monthStart = new DateTime(targetYear, month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var prevYearMonthStart = new DateTime(targetYear - 1, month, 1);
            var prevYearMonthEnd = prevYearMonthStart.AddMonths(1);

            months.Add(new MonthlyGrowthDto
            {
                Month = MonthNames[month - 1],
                CurrentYear = allDocuments.Count(d => d.CreatedAt >= monthStart && d.CreatedAt < monthEnd),
                PreviousYear = allDocuments.Count(d => d.CreatedAt >= prevYearMonthStart && d.CreatedAt < prevYearMonthEnd)
            });
        }

        if (targetYear == DateTime.Now.Year)
            months = months.Take(DateTime.Now.Month).ToList();

        return ServiceResult<List<MonthlyGrowthDto>>.Ok(months);
    }

    public async Task<ServiceResult<List<DocumentTypeDistributionDto>>> GetDocumentTypesAsync()
    {
        var allDocuments = await GetAllDocumentsAsync();
        var totalDocs = allDocuments.Count;

        if (totalDocs == 0)
        {
            return ServiceResult<List<DocumentTypeDistributionDto>>.Ok(new List<DocumentTypeDistributionDto>
            {
                new() { Type = "No Documents", Percentage = 100, Color = "slate" }
            });
        }

        var result = allDocuments
            .GroupBy(d => GetDocumentCategory(d.ContentType ?? d.Extension))
            .Select(g => new DocumentTypeDistributionDto
            {
                Type = g.Key,
                Percentage = (int)Math.Round((double)g.Count() / totalDocs * 100),
                Color = GetColorForCategory(g.Key)
            })
            .OrderByDescending(g => g.Percentage)
            .ToList();

        return ServiceResult<List<DocumentTypeDistributionDto>>.Ok(result);
    }

    public async Task<ServiceResult<List<RecentActivityDto>>> GetRecentActivityAsync(int take = 10)
    {
        var logs = await _activityLogRepository.GetRecentAsync(take);

        var userIds = logs
            .Where(l => l.UserId.HasValue && string.IsNullOrEmpty(l.UserName))
            .Select(l => l.UserId!.Value)
            .Distinct()
            .ToList();

        var userNames = new Dictionary<Guid, string>();
        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user != null)
                userNames[userId] = user.DisplayName ?? user.Username ?? "Unknown User";
        }

        var result = logs.Select(log =>
        {
            var userName = log.UserName;
            if (string.IsNullOrEmpty(userName) && log.UserId.HasValue && userNames.TryGetValue(log.UserId.Value, out var lookupName))
                userName = lookupName;
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

        return ServiceResult<List<RecentActivityDto>>.Ok(result);
    }

    private async Task<List<DAL.Entities.Document>> GetAllDocumentsAsync()
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
        return allDocuments;
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

    private static string GetColorForCategory(string category) => category switch
    {
        "PDF Documents" => "teal",
        "Word / Office" => "navy",
        _ => "slate"
    };

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
        var today = DateTime.Now.Date;
        var yesterday = today.AddDays(-1);

        if (date.Date == today)
            return $"Today, {date:h:mm tt}";
        if (date.Date == yesterday)
            return $"Yesterday, {date:h:mm tt}";
        return date.ToString("MMM d, h:mm tt");
    }

    private static class AppConstants
    {
        public const double StorageCapacityGB = 1024.0;
    }
}
