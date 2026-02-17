using System.Security.Cryptography;
using System.Text;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Http;

namespace DMS.BL.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ActivityLogService(
        IActivityLogRepository activityLogRepository,
        IUserRepository userRepository,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _activityLogRepository = activityLogRepository;
        _userRepository = userRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResult<List<ActivityLogDto>>> GetByNodeAsync(string nodeType, Guid nodeId, int skip = 0, int take = 50)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult<List<ActivityLogDto>>.Fail("Invalid node type");

        var logs = await _activityLogRepository.GetByNodeAsync(parsedNodeType, nodeId, skip, take);
        return ServiceResult<List<ActivityLogDto>>.Ok(logs.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<ActivityLogDto>>> GetByUserAsync(Guid userId, int skip = 0, int take = 50)
    {
        var logs = await _activityLogRepository.GetByUserAsync(userId, skip, take);
        return ServiceResult<List<ActivityLogDto>>.Ok(logs.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<ActivityLogDto>>> GetRecentAsync(int take = 100)
    {
        var logs = await _activityLogRepository.GetRecentAsync(take);
        return ServiceResult<List<ActivityLogDto>>.Ok(logs.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<PagedResultDto<ActivityLogDto>>> GetByNodePagedAsync(string nodeType, Guid nodeId, int page = 1, int pageSize = 50)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult<PagedResultDto<ActivityLogDto>>.Fail("Invalid node type");

        var result = await _activityLogRepository.GetByNodePagedAsync(parsedNodeType, nodeId, page, pageSize);
        return ServiceResult<PagedResultDto<ActivityLogDto>>.Ok(new PagedResultDto<ActivityLogDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }

    public async Task<ServiceResult<PagedResultDto<ActivityLogDto>>> GetByUserPagedAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        var result = await _activityLogRepository.GetByUserPagedAsync(userId, page, pageSize);
        return ServiceResult<PagedResultDto<ActivityLogDto>>.Ok(new PagedResultDto<ActivityLogDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }

    public async Task<ServiceResult<PagedResultDto<ActivityLogDto>>> GetRecentPagedAsync(int page = 1, int pageSize = 50)
    {
        var result = await _activityLogRepository.GetRecentPagedAsync(page, pageSize);
        return ServiceResult<PagedResultDto<ActivityLogDto>>.Ok(new PagedResultDto<ActivityLogDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        });
    }

    public async Task LogActivityAsync(string action, string? nodeType, Guid? nodeId, string? nodeName, string? details, Guid? userId, string? userName, string? ipAddress)
    {
        await LogActivityWithContextAsync(action, nodeType, nodeId, nodeName, details, userId, userName, ipAddress, null);
    }

    public async Task LogActivityWithContextAsync(string action, string? nodeType, Guid? nodeId, string? nodeName, string? details, Guid? userId, string? userName, string? ipAddress, string? userAgent)
    {
        NodeType? parsedNodeType = null;
        if (!string.IsNullOrEmpty(nodeType) && Enum.TryParse<NodeType>(nodeType, true, out var parsed))
            parsedNodeType = parsed;

        // Look up user name if not provided but userId is
        var resolvedUserName = userName;
        if (string.IsNullOrEmpty(resolvedUserName) && userId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(userId.Value);
            resolvedUserName = user?.DisplayName ?? user?.Username ?? "Unknown User";
        }

        // Auto-capture IP address and UserAgent from HttpContext if not explicitly provided
        var resolvedIpAddress = ipAddress;
        var resolvedUserAgent = userAgent;
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            if (string.IsNullOrEmpty(resolvedIpAddress))
            {
                // Check X-Forwarded-For first (reverse proxy), then RemoteIpAddress
                resolvedIpAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                    ?? httpContext.Connection.RemoteIpAddress?.ToString();
            }
            if (string.IsNullOrEmpty(resolvedUserAgent))
            {
                resolvedUserAgent = httpContext.Request.Headers["User-Agent"].ToString();
            }
        }

        // Get previous entry hash for chain
        var previousHash = await _activityLogRepository.GetLastEntryHashAsync();

        var log = new ActivityLog
        {
            Action = action,
            NodeType = parsedNodeType,
            NodeId = nodeId,
            NodeName = nodeName,
            Details = details,
            UserId = userId,
            UserName = resolvedUserName,
            IpAddress = resolvedIpAddress,
            UserAgent = resolvedUserAgent,
            DeviceType = ParseDeviceType(resolvedUserAgent),
            PreviousEntryHash = previousHash
        };

        // Compute hash chain entry: SHA-256(PreviousHash + Action + NodeId + UserId + CreatedAt)
        log.CreatedAt = DateTime.Now;
        log.EntryHash = ComputeEntryHash(log, previousHash);

        await _activityLogRepository.CreateAsync(log);
    }

    public async Task<ServiceResult<List<ActivityLogDto>>> SearchAsync(AuditExportQueryDto query)
    {
        var logs = await _activityLogRepository.SearchAsync(
            query.DateFrom, query.DateTo, query.Action, query.NodeType, query.UserId);
        return ServiceResult<List<ActivityLogDto>>.Ok(logs.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<byte[]>> ExportToCsvAsync(AuditExportQueryDto query)
    {
        var logs = await _activityLogRepository.SearchAsync(
            query.DateFrom, query.DateTo, query.Action, query.NodeType, query.UserId);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Action,NodeType,NodeId,NodeName,Details,UserId,UserName,IpAddress,DeviceType,EntryHash,CreatedAt");

        foreach (var log in logs)
        {
            sb.AppendLine($"{log.Id},{Escape(log.Action)},{log.NodeType},{log.NodeId},{Escape(log.NodeName)},{Escape(log.Details)},{log.UserId},{Escape(log.UserName)},{log.IpAddress},{log.DeviceType},{log.EntryHash},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return ServiceResult<byte[]>.Ok(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public async Task<ServiceResult<AuditChainVerificationResult>> VerifyAuditChainAsync(DateTime? from, DateTime? to)
    {
        var logs = await _activityLogRepository.SearchAsync(from, to, null, null, null);
        var logList = logs.OrderBy(l => l.CreatedAt).ToList();

        var result = new AuditChainVerificationResult
        {
            TotalEntries = logList.Count,
            VerifiedAt = DateTime.Now
        };

        string? previousHash = null;
        foreach (var log in logList)
        {
            if (log.PreviousEntryHash != previousHash && previousHash != null)
            {
                result.BrokenLinks++;
            }

            var expectedHash = ComputeEntryHash(log, log.PreviousEntryHash);
            if (log.EntryHash != expectedHash)
            {
                result.TamperedEntries++;
            }
            else
            {
                result.ValidEntries++;
            }

            previousHash = log.EntryHash;
        }

        result.IsValid = result.BrokenLinks == 0 && result.TamperedEntries == 0;
        return ServiceResult<AuditChainVerificationResult>.Ok(result);
    }

    private static string ComputeEntryHash(ActivityLog log, string? previousHash)
    {
        var payload = $"{previousHash}|{log.Action}|{log.NodeId}|{log.UserId}|{log.CreatedAt:O}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string? ParseDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return null;
        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
            return "Mobile";
        if (ua.Contains("tablet") || ua.Contains("ipad"))
            return "Tablet";
        if (ua.Contains("postman") || ua.Contains("swagger") || ua.Contains("curl") || !ua.Contains("mozilla"))
            return "API";
        return "Desktop";
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static ActivityLogDto MapToDto(ActivityLog log)
    {
        return new ActivityLogDto
        {
            Id = log.Id,
            Action = log.Action,
            NodeType = log.NodeType?.ToString(),
            NodeId = log.NodeId,
            NodeName = log.NodeName,
            Details = log.Details,
            UserId = log.UserId,
            UserName = log.UserName,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            DeviceType = log.DeviceType,
            EntryHash = log.EntryHash,
            CreatedAt = log.CreatedAt
        };
    }
}
