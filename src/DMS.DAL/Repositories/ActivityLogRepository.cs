using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly DmsDbContext _context;

    public ActivityLogRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ActivityLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int skip = 0, int take = 50)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Join(_context.Users,
                a => a.UserId,
                u => u.Id,
                (a, u) => new { Activity = a, User = u })
            .Select(x => new ActivityLog
            {
                Id = x.Activity.Id,
                Action = x.Activity.Action,
                NodeType = x.Activity.NodeType,
                NodeId = x.Activity.NodeId,
                NodeName = x.Activity.NodeName,
                Details = x.Activity.Details,
                UserId = x.Activity.UserId,
                UserName = x.Activity.UserName ?? x.User.DisplayName ?? x.User.Username,
                IpAddress = x.Activity.IpAddress,
                CreatedAt = x.Activity.CreatedAt
            })
            .Where(a => a.NodeType == nodeType && a.NodeId == nodeId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetByUserAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Join(_context.Users,
                a => a.UserId,
                u => u.Id,
                (a, u) => new { Activity = a, User = u })
            .Where(x => x.Activity.UserId == userId)
            .Select(x => new ActivityLog
            {
                Id = x.Activity.Id,
                Action = x.Activity.Action,
                NodeType = x.Activity.NodeType,
                NodeId = x.Activity.NodeId,
                NodeName = x.Activity.NodeName,
                Details = x.Activity.Details,
                UserId = x.Activity.UserId,
                UserName = x.Activity.UserName ?? x.User.DisplayName ?? x.User.Username,
                IpAddress = x.Activity.IpAddress,
                CreatedAt = x.Activity.CreatedAt
            })
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetRecentAsync(int take = 100)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .Join(_context.Users,
                a => a.UserId,
                u => u.Id,
                (a, u) => new { Activity = a, User = u })
            .Select(x => new ActivityLog
            {
                Id = x.Activity.Id,
                Action = x.Activity.Action,
                NodeType = x.Activity.NodeType,
                NodeId = x.Activity.NodeId,
                NodeName = x.Activity.NodeName,
                Details = x.Activity.Details,
                UserId = x.Activity.UserId,
                UserName = x.Activity.UserName ?? x.User.DisplayName ?? x.User.Username,
                IpAddress = x.Activity.IpAddress,
                CreatedAt = x.Activity.CreatedAt
            })
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ActivityLog entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        _context.ActivityLogs.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }
}
