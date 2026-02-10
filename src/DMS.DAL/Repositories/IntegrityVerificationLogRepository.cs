using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class IntegrityVerificationLogRepository : IIntegrityVerificationLogRepository
{
    private readonly DmsDbContext _context;

    public IntegrityVerificationLogRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrityVerificationLog?> GetByIdAsync(Guid id) =>
        await _context.IntegrityVerificationLogs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<IEnumerable<IntegrityVerificationLog>> GetByDocumentIdAsync(Guid documentId) =>
        await _context.IntegrityVerificationLogs.AsNoTracking()
            .Where(l => l.DocumentId == documentId)
            .OrderByDescending(l => l.VerifiedAt)
            .ToListAsync();

    public async Task<IEnumerable<IntegrityVerificationLog>> GetFailuresAsync(DateTime? since = null)
    {
        var query = _context.IntegrityVerificationLogs.AsNoTracking()
            .Where(l => !l.IsValid);

        if (since.HasValue)
        {
            query = query.Where(l => l.VerifiedAt >= since.Value);
        }

        return await query.OrderByDescending(l => l.VerifiedAt).ToListAsync();
    }

    public async Task<Guid> CreateAsync(IntegrityVerificationLog entity)
    {
        entity.Id = Guid.NewGuid();

        _context.IntegrityVerificationLogs.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }
}
