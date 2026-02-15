using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class RecycleBinRepository : IRecycleBinRepository
{
    private const int RetentionDays = 30;
    private readonly DmsDbContext _context;

    public RecycleBinRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RecycleBinItem>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RecycleBinItems
            .AsNoTracking()
            .Where(rb => rb.DeletedBy == userId)
            .GroupJoin(_context.Users.AsNoTracking(), rb => rb.DeletedBy, u => u.Id, (rb, users) => new { rb, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.rb, u })
            .OrderByDescending(x => x.rb.DeletedAt)
            .Select(x => new RecycleBinItem
            {
                Id = x.rb.Id,
                NodeType = x.rb.NodeType,
                NodeId = x.rb.NodeId,
                NodeName = x.rb.NodeName,
                OriginalPath = x.rb.OriginalPath,
                OriginalParentId = x.rb.OriginalParentId,
                DeletedBy = x.rb.DeletedBy,
                DeletedAt = x.rb.DeletedAt,
                ExpiresAt = x.rb.ExpiresAt,
                Metadata = x.rb.Metadata,
                DeletedByUserName = x.u != null ? x.u.DisplayName : null
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<RecycleBinItem>> GetAllAsync(int? nodeType = null)
    {
        var query = _context.RecycleBinItems.AsNoTracking().AsQueryable();

        if (nodeType.HasValue)
            query = query.Where(rb => rb.NodeType == nodeType.Value);

        return await query
            .GroupJoin(_context.Users.AsNoTracking(), rb => rb.DeletedBy, u => u.Id, (rb, users) => new { rb, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.rb, u })
            .OrderByDescending(x => x.rb.DeletedAt)
            .Select(x => new RecycleBinItem
            {
                Id = x.rb.Id,
                NodeType = x.rb.NodeType,
                NodeId = x.rb.NodeId,
                NodeName = x.rb.NodeName,
                OriginalPath = x.rb.OriginalPath,
                OriginalParentId = x.rb.OriginalParentId,
                DeletedBy = x.rb.DeletedBy,
                DeletedAt = x.rb.DeletedAt,
                ExpiresAt = x.rb.ExpiresAt,
                Metadata = x.rb.Metadata,
                DeletedByUserName = x.u != null ? x.u.DisplayName : null
            })
            .ToListAsync();
    }

    public async Task<(List<RecycleBinItem> Items, int TotalCount)> GetByUserIdPaginatedAsync(Guid userId, int page, int pageSize)
    {
        var query = _context.RecycleBinItems
            .AsNoTracking()
            .Where(rb => rb.DeletedBy == userId);

        var totalCount = await query.CountAsync();

        var items = await query
            .GroupJoin(_context.Users.AsNoTracking(), rb => rb.DeletedBy, u => u.Id, (rb, users) => new { rb, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.rb, u })
            .OrderByDescending(x => x.rb.DeletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RecycleBinItem
            {
                Id = x.rb.Id,
                NodeType = x.rb.NodeType,
                NodeId = x.rb.NodeId,
                NodeName = x.rb.NodeName,
                OriginalPath = x.rb.OriginalPath,
                OriginalParentId = x.rb.OriginalParentId,
                DeletedBy = x.rb.DeletedBy,
                DeletedAt = x.rb.DeletedAt,
                ExpiresAt = x.rb.ExpiresAt,
                Metadata = x.rb.Metadata,
                DeletedByUserName = x.u != null ? x.u.DisplayName : null
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<RecycleBinItem> Items, int TotalCount)> GetAllPaginatedAsync(int? nodeType, int page, int pageSize)
    {
        var query = _context.RecycleBinItems.AsNoTracking().AsQueryable();

        if (nodeType.HasValue)
            query = query.Where(rb => rb.NodeType == nodeType.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .GroupJoin(_context.Users.AsNoTracking(), rb => rb.DeletedBy, u => u.Id, (rb, users) => new { rb, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.rb, u })
            .OrderByDescending(x => x.rb.DeletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RecycleBinItem
            {
                Id = x.rb.Id,
                NodeType = x.rb.NodeType,
                NodeId = x.rb.NodeId,
                NodeName = x.rb.NodeName,
                OriginalPath = x.rb.OriginalPath,
                OriginalParentId = x.rb.OriginalParentId,
                DeletedBy = x.rb.DeletedBy,
                DeletedAt = x.rb.DeletedAt,
                ExpiresAt = x.rb.ExpiresAt,
                Metadata = x.rb.Metadata,
                DeletedByUserName = x.u != null ? x.u.DisplayName : null
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<RecycleBinItem?> GetByIdAsync(Guid id)
    {
        return await _context.RecycleBinItems
            .AsNoTracking()
            .Where(rb => rb.Id == id)
            .GroupJoin(_context.Users.AsNoTracking(), rb => rb.DeletedBy, u => u.Id, (rb, users) => new { rb, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.rb, u })
            .Select(x => new RecycleBinItem
            {
                Id = x.rb.Id,
                NodeType = x.rb.NodeType,
                NodeId = x.rb.NodeId,
                NodeName = x.rb.NodeName,
                OriginalPath = x.rb.OriginalPath,
                OriginalParentId = x.rb.OriginalParentId,
                DeletedBy = x.rb.DeletedBy,
                DeletedAt = x.rb.DeletedAt,
                ExpiresAt = x.rb.ExpiresAt,
                Metadata = x.rb.Metadata,
                DeletedByUserName = x.u != null ? x.u.DisplayName : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> AddAsync(RecycleBinItem entity)
    {
        entity.Id = Guid.NewGuid();
        entity.DeletedAt = DateTime.Now;
        entity.ExpiresAt = DateTime.Now.AddDays(RetentionDays);

        _context.RecycleBinItems.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        var entity = await _context.RecycleBinItems.FindAsync(id);
        if (entity == null) return false;

        _context.RecycleBinItems.Remove(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> PurgeExpiredAsync()
    {
        var affected = await _context.RecycleBinItems
            .Where(rb => rb.ExpiresAt < DateTime.Now)
            .ExecuteDeleteAsync();

        return affected > 0;
    }
}
