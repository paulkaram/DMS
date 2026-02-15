using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly DmsDbContext _context;

    public FolderRepository(DmsDbContext context)
    {
        _context = context;
    }

    private IQueryable<Folder> ApplyPrivacyFilter(IQueryable<Folder> query, int? userPrivacyLevel)
    {
        if (userPrivacyLevel == null) return query; // null = admin, no filter
        return query.Where(f => f.PrivacyLevelId == null || f.PrivacyLevel!.Level <= userPrivacyLevel.Value);
    }

    public async Task<Folder?> GetByIdAsync(Guid id)
    {
        return await _context.Folders
            .Include(f => f.PrivacyLevel)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<Folder>> GetAllAsync()
    {
        return await _context.Folders
            .Include(f => f.PrivacyLevel)
            .AsNoTracking()
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetByCabinetIdAsync(Guid cabinetId, int? userPrivacyLevel = null)
    {
        var query = _context.Folders
            .Include(f => f.PrivacyLevel)
            .AsNoTracking()
            .Where(f => f.CabinetId == cabinetId);

        query = ApplyPrivacyFilter(query, userPrivacyLevel);

        return await query
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetByParentIdAsync(Guid? parentId, Guid cabinetId, int? userPrivacyLevel = null)
    {
        IQueryable<Folder> query;

        if (parentId == null)
        {
            query = _context.Folders
                .Include(f => f.PrivacyLevel)
                .AsNoTracking()
                .Where(f => f.CabinetId == cabinetId && f.ParentFolderId == null);
        }
        else
        {
            query = _context.Folders
                .Include(f => f.PrivacyLevel)
                .AsNoTracking()
                .Where(f => f.ParentFolderId == parentId);
        }

        query = ApplyPrivacyFilter(query, userPrivacyLevel);

        return await query
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetTreeAsync(Guid cabinetId, int maxResults = 5000, int? userPrivacyLevel = null)
    {
        var query = _context.Folders
            .Include(f => f.PrivacyLevel)
            .AsNoTracking()
            .Where(f => f.CabinetId == cabinetId);

        query = ApplyPrivacyFilter(query, userPrivacyLevel);

        return await query
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> SearchAsync(string? name, Guid? cabinetId, int? userPrivacyLevel = null)
    {
        var query = _context.Folders
            .Include(f => f.PrivacyLevel)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(f => EF.Functions.Like(f.Name, $"%{name}%"));

        if (cabinetId.HasValue)
            query = query.Where(f => f.CabinetId == cabinetId.Value);

        query = ApplyPrivacyFilter(query, userPrivacyLevel);

        return await query
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<(List<Folder> Items, int TotalCount)> SearchPaginatedAsync(string? name, Guid? cabinetId, int page, int pageSize, int? userPrivacyLevel = null)
    {
        var query = _context.Folders
            .Include(f => f.PrivacyLevel)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(f => EF.Functions.Like(f.Name, $"%{name}%"));
        if (cabinetId.HasValue)
            query = query.Where(f => f.CabinetId == cabinetId.Value);

        query = ApplyPrivacyFilter(query, userPrivacyLevel);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Folder> Items, int TotalCount)> GetByParentIdPaginatedAsync(Guid? parentId, Guid cabinetId, int page, int pageSize, int? userPrivacyLevel = null)
    {
        IQueryable<Folder> query;

        if (parentId == null)
        {
            query = _context.Folders
                .Include(f => f.PrivacyLevel)
                .AsNoTracking()
                .Where(f => f.CabinetId == cabinetId && f.ParentFolderId == null);
        }
        else
        {
            query = _context.Folders
                .Include(f => f.PrivacyLevel)
                .AsNoTracking()
                .Where(f => f.ParentFolderId == parentId);
        }

        query = ApplyPrivacyFilter(query, userPrivacyLevel);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<string> GetPathAsync(Guid folderId)
    {
        var folder = await _context.Folders
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == folderId);

        return folder?.Path ?? string.Empty;
    }

    public async Task UpdatePathsAsync(Guid folderId, string newPath)
    {
        await _context.Folders
            .Where(f => f.Id == folderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Path, newPath)
                .SetProperty(f => f.ModifiedAt, DateTime.UtcNow));
    }

    public async Task<Guid> CreateAsync(Folder entity)
    {
        entity.Id = Guid.NewGuid();
        entity.Path = await GetPathAsync(entity.ParentFolderId ?? Guid.Empty);
        if (!string.IsNullOrEmpty(entity.Path))
            entity.Path += "/" + entity.Name;
        else
            entity.Path = entity.Name;

        _context.Folders.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Folder entity)
    {
        _context.Folders.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Soft-delete the folder and all its descendants using a recursive CTE
        var affected = await _context.Database.ExecuteSqlRawAsync(@"
            WITH FolderTree AS (
                SELECT Id FROM Folders WHERE Id = {0} AND IsActive = 1
                UNION ALL
                SELECT f.Id FROM Folders f
                INNER JOIN FolderTree ft ON f.ParentFolderId = ft.Id
                WHERE f.IsActive = 1
            )
            UPDATE Folders SET IsActive = 0, ModifiedAt = GETUTCDATE()
            WHERE Id IN (SELECT Id FROM FolderTree)", id);
        return affected > 0;
    }
}
