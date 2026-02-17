using DMS.DAL.DTOs;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly AuditDbContext _context;

    public ActivityLogRepository(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ActivityLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int skip = 0, int take = 50)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
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
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLog>> GetRecentAsync(int take = 100)
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<PagedResult<ActivityLog>> GetByNodePagedAsync(NodeType nodeType, Guid nodeId, int page = 1, int pageSize = 50)
    {
        pageSize = Math.Min(pageSize, 200);
        var baseQuery = _context.ActivityLogs.AsNoTracking()
            .Where(a => a.NodeType == nodeType && a.NodeId == nodeId);
        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<ActivityLog> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }

    public async Task<PagedResult<ActivityLog>> GetByUserPagedAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        pageSize = Math.Min(pageSize, 200);
        var baseQuery = _context.ActivityLogs.AsNoTracking()
            .Where(a => a.UserId == userId);
        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<ActivityLog> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }

    public async Task<PagedResult<ActivityLog>> GetRecentPagedAsync(int page = 1, int pageSize = 50)
    {
        pageSize = Math.Min(pageSize, 200);
        var baseQuery = _context.ActivityLogs.AsNoTracking();
        var totalCount = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<ActivityLog> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }

    public async Task<string?> GetLastEntryHashAsync()
    {
        return await _context.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.EntryHash)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<ActivityLog>> SearchAsync(DateTime? dateFrom, DateTime? dateTo, string? action, string? nodeType, Guid? userId)
    {
        var query = _context.ActivityLogs.AsNoTracking().AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(a => a.CreatedAt <= dateTo.Value);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);
        if (!string.IsNullOrEmpty(nodeType) && Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            query = query.Where(a => a.NodeType == parsedNodeType);
        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(10000)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ActivityLog entity)
    {
        entity.Id = Guid.NewGuid();
        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.Now;

        _context.ActivityLogs.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }
}
