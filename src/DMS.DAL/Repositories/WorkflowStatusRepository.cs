using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class WorkflowStatusRepository : IWorkflowStatusRepository
{
    private readonly DmsDbContext _context;

    public WorkflowStatusRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<WorkflowStatus?> GetByIdAsync(Guid id)
    {
        return await _context.WorkflowStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(ws => ws.Id == id);
    }

    public async Task<IEnumerable<WorkflowStatus>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.WorkflowStatuses.AsNoTracking();

        if (!includeInactive)
            query = query.Where(ws => ws.IsActive);

        return await query
            .OrderBy(ws => ws.SortOrder)
            .ThenBy(ws => ws.Name)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(WorkflowStatus entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.WorkflowStatuses.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(WorkflowStatus entity)
    {
        var existing = await _context.WorkflowStatuses.FindAsync(entity.Id);
        if (existing == null) return false;

        existing.Name = entity.Name;
        existing.Color = entity.Color;
        existing.Icon = entity.Icon;
        existing.Description = entity.Description;
        existing.SortOrder = entity.SortOrder;
        existing.IsActive = entity.IsActive;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.WorkflowStatuses.FindAsync(id);
        if (entity == null) return false;

        _context.WorkflowStatuses.Remove(entity);
        return await _context.SaveChangesAsync() > 0;
    }
}
