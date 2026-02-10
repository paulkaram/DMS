using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class FilingPlanRepository : IFilingPlanRepository
{
    private readonly DmsDbContext _context;

    public FilingPlanRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FilingPlan>> GetByFolderAsync(Guid folderId) =>
        await _context.FilingPlans.AsNoTracking()
            .Where(fp => fp.FolderId == folderId)
            .GroupJoin(_context.Folders.AsNoTracking(), fp => fp.FolderId, f => f.Id, (fp, fs) => new { fp, fs })
            .SelectMany(x => x.fs.DefaultIfEmpty(), (x, f) => new { x.fp, FolderName = f != null ? f.Name : null })
            .GroupJoin(_context.Classifications.AsNoTracking(), x => x.fp.ClassificationId, c => c.Id, (x, cs) => new { x.fp, x.FolderName, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.fp, x.FolderName, ClassificationName = c != null ? c.Name : null })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.fp.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.fp, x.FolderName, x.ClassificationName, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new FilingPlan
            {
                Id = x.fp.Id,
                FolderId = x.fp.FolderId,
                Name = x.fp.Name,
                Description = x.fp.Description,
                Pattern = x.fp.Pattern,
                ClassificationId = x.fp.ClassificationId,
                DocumentTypeId = x.fp.DocumentTypeId,
                IsActive = x.fp.IsActive,
                CreatedBy = x.fp.CreatedBy,
                CreatedAt = x.fp.CreatedAt,
                FolderName = x.FolderName,
                ClassificationName = x.ClassificationName,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .OrderBy(fp => fp.Name)
            .ToListAsync();

    public async Task<FilingPlan?> GetByIdAsync(Guid id) =>
        await _context.FilingPlans.AsNoTracking()
            .Where(fp => fp.Id == id)
            .GroupJoin(_context.Folders.AsNoTracking(), fp => fp.FolderId, f => f.Id, (fp, fs) => new { fp, fs })
            .SelectMany(x => x.fs.DefaultIfEmpty(), (x, f) => new { x.fp, FolderName = f != null ? f.Name : null })
            .GroupJoin(_context.Classifications.AsNoTracking(), x => x.fp.ClassificationId, c => c.Id, (x, cs) => new { x.fp, x.FolderName, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.fp, x.FolderName, ClassificationName = c != null ? c.Name : null })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.fp.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.fp, x.FolderName, x.ClassificationName, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new FilingPlan
            {
                Id = x.fp.Id,
                FolderId = x.fp.FolderId,
                Name = x.fp.Name,
                Description = x.fp.Description,
                Pattern = x.fp.Pattern,
                ClassificationId = x.fp.ClassificationId,
                DocumentTypeId = x.fp.DocumentTypeId,
                IsActive = x.fp.IsActive,
                CreatedBy = x.fp.CreatedBy,
                CreatedAt = x.fp.CreatedAt,
                FolderName = x.FolderName,
                ClassificationName = x.ClassificationName,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .FirstOrDefaultAsync();

    public async Task<Guid> CreateAsync(FilingPlan entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        _context.FilingPlans.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(FilingPlan entity)
    {
        _context.FilingPlans.Update(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _context.FilingPlans.Where(fp => fp.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(fp => fp.IsActive, false)) > 0;
}
