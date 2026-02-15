using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class LegalHoldRepository : ILegalHoldRepository
{
    private readonly DmsDbContext _context;

    public LegalHoldRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<LegalHold?> GetByIdAsync(Guid id)
    {
        return await _context.LegalHolds
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(lh => lh.Id == id);
    }

    public async Task<IEnumerable<LegalHold>> GetAllAsync()
    {
        return await _context.LegalHolds
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(lh => lh.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LegalHold>> GetActiveAsync()
    {
        return await _context.LegalHolds
            .AsNoTracking()
            .Where(lh => lh.Status == "Active")
            .OrderByDescending(lh => lh.CreatedAt)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(LegalHold entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.LegalHolds.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(LegalHold entity)
    {
        entity.ModifiedAt = DateTime.Now;

        var existing = await _context.LegalHolds
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(lh => lh.Id == entity.Id);
        if (existing == null) return false;

        existing.Name = entity.Name;
        existing.Description = entity.Description;
        existing.CaseReference = entity.CaseReference;
        existing.RequestedBy = entity.RequestedBy;
        existing.RequestedAt = entity.RequestedAt;
        existing.Status = entity.Status;
        existing.EffectiveUntil = entity.EffectiveUntil;
        existing.ReleasedBy = entity.ReleasedBy;
        existing.ReleasedAt = entity.ReleasedAt;
        existing.ReleaseReason = entity.ReleaseReason;
        existing.Notes = entity.Notes;
        existing.IsActive = entity.IsActive;
        existing.ModifiedAt = entity.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }
}

public class LegalHoldDocumentRepository : ILegalHoldDocumentRepository
{
    private readonly DmsDbContext _context;

    public LegalHoldDocumentRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<LegalHoldDocument?> GetByIdAsync(Guid id)
    {
        return await _context.LegalHoldDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(lhd => lhd.Id == id);
    }

    public async Task<IEnumerable<LegalHoldDocument>> GetByHoldIdAsync(Guid holdId)
    {
        return await _context.LegalHoldDocuments
            .AsNoTracking()
            .Where(lhd => lhd.LegalHoldId == holdId)
            .Join(_context.Documents.AsNoTracking(), lhd => lhd.DocumentId, d => d.Id, (lhd, d) => new { lhd, d })
            .Join(_context.LegalHolds.IgnoreQueryFilters().AsNoTracking(), x => x.lhd.LegalHoldId, lh => lh.Id, (x, lh) => new LegalHoldDocument
            {
                Id = x.lhd.Id,
                LegalHoldId = x.lhd.LegalHoldId,
                DocumentId = x.lhd.DocumentId,
                AddedAt = x.lhd.AddedAt,
                AddedBy = x.lhd.AddedBy,
                ReleasedAt = x.lhd.ReleasedAt,
                ReleasedBy = x.lhd.ReleasedBy,
                Notes = x.lhd.Notes,
                DocumentName = x.d.Name,
                HoldName = lh.Name
            })
            .OrderByDescending(lhd => lhd.AddedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LegalHoldDocument>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.LegalHoldDocuments
            .AsNoTracking()
            .Where(lhd => lhd.DocumentId == documentId)
            .Join(_context.Documents.AsNoTracking(), lhd => lhd.DocumentId, d => d.Id, (lhd, d) => new { lhd, d })
            .Join(_context.LegalHolds.IgnoreQueryFilters().AsNoTracking(), x => x.lhd.LegalHoldId, lh => lh.Id, (x, lh) => new LegalHoldDocument
            {
                Id = x.lhd.Id,
                LegalHoldId = x.lhd.LegalHoldId,
                DocumentId = x.lhd.DocumentId,
                AddedAt = x.lhd.AddedAt,
                AddedBy = x.lhd.AddedBy,
                ReleasedAt = x.lhd.ReleasedAt,
                ReleasedBy = x.lhd.ReleasedBy,
                Notes = x.lhd.Notes,
                DocumentName = x.d.Name,
                HoldName = lh.Name
            })
            .OrderByDescending(lhd => lhd.AddedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<LegalHoldDocument>> GetActiveByDocumentIdAsync(Guid documentId)
    {
        return await _context.LegalHoldDocuments
            .AsNoTracking()
            .Where(lhd => lhd.DocumentId == documentId && lhd.ReleasedAt == null)
            .Join(_context.Documents.AsNoTracking(), lhd => lhd.DocumentId, d => d.Id, (lhd, d) => new { lhd, d })
            .Join(_context.LegalHolds.AsNoTracking(), x => x.lhd.LegalHoldId, lh => lh.Id, (x, lh) => new { x.lhd, x.d, lh })
            .Where(x => x.lh.Status == "Active")
            .Select(x => new LegalHoldDocument
            {
                Id = x.lhd.Id,
                LegalHoldId = x.lhd.LegalHoldId,
                DocumentId = x.lhd.DocumentId,
                AddedAt = x.lhd.AddedAt,
                AddedBy = x.lhd.AddedBy,
                ReleasedAt = x.lhd.ReleasedAt,
                ReleasedBy = x.lhd.ReleasedBy,
                Notes = x.lhd.Notes,
                DocumentName = x.d.Name,
                HoldName = x.lh.Name
            })
            .OrderByDescending(lhd => lhd.AddedAt)
            .ToListAsync();
    }

    public async Task<bool> IsDocumentOnHoldAsync(Guid documentId)
    {
        return await _context.LegalHoldDocuments
            .AsNoTracking()
            .Where(lhd => lhd.DocumentId == documentId && lhd.ReleasedAt == null)
            .Join(_context.LegalHolds.AsNoTracking(), lhd => lhd.LegalHoldId, lh => lh.Id, (lhd, lh) => lh)
            .Where(lh => lh.Status == "Active")
            .AnyAsync();
    }

    public async Task<Guid> CreateAsync(LegalHoldDocument entity)
    {
        entity.Id = Guid.NewGuid();

        _context.LegalHoldDocuments.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(LegalHoldDocument entity)
    {
        var existing = await _context.LegalHoldDocuments.FindAsync(entity.Id);
        if (existing == null) return false;

        existing.ReleasedAt = entity.ReleasedAt;
        existing.ReleasedBy = entity.ReleasedBy;
        existing.Notes = entity.Notes;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.LegalHoldDocuments
            .Where(lhd => lhd.Id == id)
            .ExecuteDeleteAsync() > 0;
    }
}
