using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class CabinetRepository : ICabinetRepository
{
    private readonly DmsDbContext _context;

    public CabinetRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Cabinet?> GetByIdAsync(Guid id)
    {
        return await _context.Cabinets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Cabinet>> GetAllAsync()
    {
        return await _context.Cabinets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cabinet>> GetActiveAsync()
    {
        return await _context.Cabinets
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Cabinet>> SearchAsync(string? name)
    {
        var query = _context.Cabinets.AsNoTracking();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(c => c.Name.Contains(name));

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Guid> CreateAsync(Cabinet entity)
    {
        entity.Id = Guid.NewGuid();
        _context.Cabinets.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Cabinet entity)
    {
        _context.Cabinets.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.Cabinets
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsActive, false)
                .SetProperty(c => c.ModifiedAt, DateTime.UtcNow));
        return affected > 0;
    }
}
