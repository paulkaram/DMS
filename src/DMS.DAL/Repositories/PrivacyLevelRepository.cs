using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class PrivacyLevelRepository : IPrivacyLevelRepository
{
    private readonly DmsDbContext _context;

    public PrivacyLevelRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<PrivacyLevel?> GetByIdAsync(Guid id)
    {
        return await _context.PrivacyLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<PrivacyLevel>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.PrivacyLevels.AsNoTracking();

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await query
            .OrderBy(p => p.Level)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(PrivacyLevel entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        _context.PrivacyLevels.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(PrivacyLevel entity)
    {
        _context.PrivacyLevels.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.PrivacyLevels
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();
        return affected > 0;
    }
}
