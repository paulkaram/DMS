using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class ClassificationRepository : IClassificationRepository
{
    private readonly DmsDbContext _context;

    public ClassificationRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Classification?> GetByIdAsync(Guid id)
    {
        return await _context.Classifications
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Classification>> GetAllAsync(string? language = null)
    {
        var query = _context.Classifications
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(language))
            query = query.Where(c => c.Language == language || c.Language == null);

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Classification>> SearchAsync(string? name, string? language = null)
    {
        var query = _context.Classifications
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(name))
            query = query.Where(c => c.Name.Contains(name));

        if (!string.IsNullOrEmpty(language))
            query = query.Where(c => c.Language == language || c.Language == null);

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Classification?> GetByCodeAsync(string code)
    {
        return await _context.Classifications
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
    }

    public async Task<IEnumerable<Classification>> GetChildrenAsync(Guid? parentId)
    {
        return await _context.Classifications
            .AsNoTracking()
            .Where(c => c.IsActive && c.ParentId == parentId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Classification>> GetTreeAsync(string? language = null)
    {
        var query = _context.Classifications
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(language))
            query = query.Where(c => c.Language == language || c.Language == null);

        return await query
            .OrderBy(c => c.Level)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(Classification entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        _context.Classifications.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Classification entity)
    {
        _context.Classifications.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.Classifications
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsActive, false));
        return affected > 0;
    }
}

public class ImportanceRepository : IImportanceRepository
{
    private readonly DmsDbContext _context;

    public ImportanceRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Importance?> GetByIdAsync(Guid id)
    {
        return await _context.Importances
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Importance>> GetAllAsync(string? language = null)
    {
        var query = _context.Importances
            .AsNoTracking()
            .Where(i => i.IsActive);

        if (!string.IsNullOrEmpty(language))
            query = query.Where(i => i.Language == language || i.Language == null);

        return await query
            .OrderBy(i => i.Level)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(Importance entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        _context.Importances.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Importance entity)
    {
        _context.Importances.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.Importances
            .Where(i => i.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.IsActive, false));
        return affected > 0;
    }
}

public class DocumentTypeRepository : IDocumentTypeRepository
{
    private readonly DmsDbContext _context;

    public DocumentTypeRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentType?> GetByIdAsync(Guid id)
    {
        return await _context.DocumentTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<DocumentType>> GetAllAsync(string? language = null)
    {
        var query = _context.DocumentTypes
            .AsNoTracking()
            .Where(d => d.IsActive);

        if (!string.IsNullOrEmpty(language))
            query = query.Where(d => d.Language == language || d.Language == null);

        return await query
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentType>> SearchAsync(string? name, string? language = null)
    {
        var query = _context.DocumentTypes
            .AsNoTracking()
            .Where(d => d.IsActive);

        if (!string.IsNullOrEmpty(name))
            query = query.Where(d => d.Name.Contains(name));

        if (!string.IsNullOrEmpty(language))
            query = query.Where(d => d.Language == language || d.Language == null);

        return await query
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(DocumentType entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        _context.DocumentTypes.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DocumentType entity)
    {
        _context.DocumentTypes.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.DocumentTypes
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.IsActive, false));
        return affected > 0;
    }
}

public class LookupRepository : ILookupRepository
{
    private readonly DmsDbContext _context;

    public LookupRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Lookup?> GetByIdAsync(Guid id)
    {
        return await _context.Lookups
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Lookup?> GetByNameAsync(string name)
    {
        return await _context.Lookups
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Name == name && l.IsActive);
    }

    public async Task<IEnumerable<Lookup>> GetAllAsync()
    {
        return await _context.Lookups
            .AsNoTracking()
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<LookupItem>> GetItemsByLookupIdAsync(Guid lookupId, string? language = null)
    {
        var query = _context.LookupItems
            .AsNoTracking()
            .Where(li => li.LookupId == lookupId && li.IsActive);

        if (!string.IsNullOrEmpty(language))
            query = query.Where(li => li.Language == language || li.Language == null);

        return await query
            .OrderBy(li => li.SortOrder)
            .ThenBy(li => li.DisplayText)
            .ToListAsync();
    }

    public async Task<IEnumerable<LookupItem>> GetItemsByLookupNameAsync(string lookupName, string? language = null)
    {
        var query = _context.LookupItems
            .AsNoTracking()
            .Join(
                _context.Lookups.Where(l => l.Name == lookupName && l.IsActive),
                li => li.LookupId,
                l => l.Id,
                (li, l) => li)
            .Where(li => li.IsActive);

        if (!string.IsNullOrEmpty(language))
            query = query.Where(li => li.Language == language || li.Language == null);

        return await query
            .OrderBy(li => li.SortOrder)
            .ThenBy(li => li.DisplayText)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(Lookup entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        _context.Lookups.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Lookup entity)
    {
        _context.Lookups.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.Lookups
            .Where(l => l.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.IsActive, false));
        return affected > 0;
    }

    public async Task<LookupItem?> GetItemByIdAsync(Guid id)
    {
        return await _context.LookupItems
            .AsNoTracking()
            .FirstOrDefaultAsync(li => li.Id == id);
    }

    public async Task<Guid> CreateItemAsync(LookupItem entity)
    {
        entity.Id = Guid.NewGuid();
        _context.LookupItems.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateItemAsync(LookupItem entity)
    {
        _context.LookupItems.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteItemAsync(Guid id)
    {
        var affected = await _context.LookupItems
            .Where(li => li.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(li => li.IsActive, false));
        return affected > 0;
    }
}
