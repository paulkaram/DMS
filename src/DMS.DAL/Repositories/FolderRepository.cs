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

    public async Task<Folder?> GetByIdAsync(Guid id)
    {
        return await _context.Folders
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<Folder>> GetAllAsync()
    {
        return await _context.Folders
            .AsNoTracking()
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetByCabinetIdAsync(Guid cabinetId)
    {
        return await _context.Folders
            .AsNoTracking()
            .Where(f => f.CabinetId == cabinetId)
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetByParentIdAsync(Guid? parentId, Guid cabinetId)
    {
        if (parentId == null)
        {
            return await _context.Folders
                .AsNoTracking()
                .Where(f => f.CabinetId == cabinetId && f.ParentFolderId == null)
                .OrderBy(f => f.Name)
                .ToListAsync();
        }

        return await _context.Folders
            .AsNoTracking()
            .Where(f => f.ParentFolderId == parentId)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> GetTreeAsync(Guid cabinetId, int maxResults = 5000)
    {
        return await _context.Folders
            .AsNoTracking()
            .Where(f => f.CabinetId == cabinetId)
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<IEnumerable<Folder>> SearchAsync(string? name, Guid? cabinetId)
    {
        var query = _context.Folders.AsNoTracking();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(f => EF.Functions.Like(f.Name, $"%{name}%"));

        if (cabinetId.HasValue)
            query = query.Where(f => f.CabinetId == cabinetId.Value);

        return await query
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<(List<Folder> Items, int TotalCount)> SearchPaginatedAsync(string? name, Guid? cabinetId, int page, int pageSize)
    {
        var query = _context.Folders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(f => EF.Functions.Like(f.Name, $"%{name}%"));
        if (cabinetId.HasValue)
            query = query.Where(f => f.CabinetId == cabinetId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(f => f.Path)
            .ThenBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Folder> Items, int TotalCount)> GetByParentIdPaginatedAsync(Guid? parentId, Guid cabinetId, int page, int pageSize)
    {
        IQueryable<Folder> query;

        if (parentId == null)
        {
            query = _context.Folders
                .AsNoTracking()
                .Where(f => f.CabinetId == cabinetId && f.ParentFolderId == null);
        }
        else
        {
            query = _context.Folders
                .AsNoTracking()
                .Where(f => f.ParentFolderId == parentId);
        }

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
        var affected = await _context.Folders
            .Where(f => f.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.IsActive, false)
                .SetProperty(f => f.ModifiedAt, DateTime.UtcNow));
        return affected > 0;
    }
}
