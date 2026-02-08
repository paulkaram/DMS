using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IUserRepository _userRepository;

    public ActivityLogService(IActivityLogRepository activityLogRepository, IUserRepository userRepository)
    {
        _activityLogRepository = activityLogRepository;
        _userRepository = userRepository;
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

    public async Task LogActivityAsync(string action, string? nodeType, Guid? nodeId, string? nodeName, string? details, Guid? userId, string? userName, string? ipAddress)
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

        var log = new ActivityLog
        {
            Action = action,
            NodeType = parsedNodeType,
            NodeId = nodeId,
            NodeName = nodeName,
            Details = details,
            UserId = userId,
            UserName = resolvedUserName,
            IpAddress = ipAddress
        };

        await _activityLogRepository.CreateAsync(log);
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
            CreatedAt = log.CreatedAt
        };
    }
}
