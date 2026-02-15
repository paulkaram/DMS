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
            .OrderByDescending(rb => rb.DeletedAt)
            .Select(rb => new RecycleBinItem
            {
                Id = rb.Id,
                NodeType = rb.NodeType,
                NodeId = rb.NodeId,
                NodeName = rb.NodeName,
                OriginalPath = rb.OriginalPath,
                OriginalParentId = rb.OriginalParentId,
                DeletedBy = rb.DeletedBy,
                DeletedAt = rb.DeletedAt,
                ExpiresAt = rb.ExpiresAt,
                Metadata = rb.Metadata,
                DeletedByUserName = _context.Users
                    .Where(u => u.Id == rb.DeletedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<RecycleBinItem>> GetAllAsync(int? nodeType = null)
    {
        var query = _context.RecycleBinItems.AsNoTracking().AsQueryable();

        if (nodeType.HasValue)
            query = query.Where(rb => rb.NodeType == nodeType.Value);

        return await query
            .OrderByDescending(rb => rb.DeletedAt)
            .Select(rb => new RecycleBinItem
            {
                Id = rb.Id,
                NodeType = rb.NodeType,
                NodeId = rb.NodeId,
                NodeName = rb.NodeName,
                OriginalPath = rb.OriginalPath,
                OriginalParentId = rb.OriginalParentId,
                DeletedBy = rb.DeletedBy,
                DeletedAt = rb.DeletedAt,
                ExpiresAt = rb.ExpiresAt,
                Metadata = rb.Metadata,
                DeletedByUserName = _context.Users
                    .Where(u => u.Id == rb.DeletedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
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
            .OrderByDescending(rb => rb.DeletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rb => new RecycleBinItem
            {
                Id = rb.Id,
                NodeType = rb.NodeType,
                NodeId = rb.NodeId,
                NodeName = rb.NodeName,
                OriginalPath = rb.OriginalPath,
                OriginalParentId = rb.OriginalParentId,
                DeletedBy = rb.DeletedBy,
                DeletedAt = rb.DeletedAt,
                ExpiresAt = rb.ExpiresAt,
                Metadata = rb.Metadata,
                DeletedByUserName = _context.Users
                    .Where(u => u.Id == rb.DeletedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
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
            .OrderByDescending(rb => rb.DeletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rb => new RecycleBinItem
            {
                Id = rb.Id,
                NodeType = rb.NodeType,
                NodeId = rb.NodeId,
                NodeName = rb.NodeName,
                OriginalPath = rb.OriginalPath,
                OriginalParentId = rb.OriginalParentId,
                DeletedBy = rb.DeletedBy,
                DeletedAt = rb.DeletedAt,
                ExpiresAt = rb.ExpiresAt,
                Metadata = rb.Metadata,
                DeletedByUserName = _context.Users
                    .Where(u => u.Id == rb.DeletedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<RecycleBinItem?> GetByIdAsync(Guid id)
    {
        return await _context.RecycleBinItems
            .AsNoTracking()
            .Where(rb => rb.Id == id)
            .Select(rb => new RecycleBinItem
            {
                Id = rb.Id,
                NodeType = rb.NodeType,
                NodeId = rb.NodeId,
                NodeName = rb.NodeName,
                OriginalPath = rb.OriginalPath,
                OriginalParentId = rb.OriginalParentId,
                DeletedBy = rb.DeletedBy,
                DeletedAt = rb.DeletedAt,
                ExpiresAt = rb.ExpiresAt,
                Metadata = rb.Metadata,
                DeletedByUserName = _context.Users
                    .Where(u => u.Id == rb.DeletedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
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
