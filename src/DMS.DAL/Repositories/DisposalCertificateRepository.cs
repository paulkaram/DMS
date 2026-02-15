using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DisposalCertificateRepository : IDisposalCertificateRepository
{
    private readonly DmsDbContext _context;

    public DisposalCertificateRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<DisposalCertificate?> GetByIdAsync(Guid id)
    {
        return await _context.DisposalCertificates
            .AsNoTracking()
            .FirstOrDefaultAsync(dc => dc.Id == id);
    }

    public async Task<DisposalCertificate?> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DisposalCertificates
            .AsNoTracking()
            .Where(dc => dc.DocumentId == documentId)
            .OrderByDescending(dc => dc.DisposedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DisposalCertificate>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.DisposalCertificates.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(dc => dc.DisposedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(dc => dc.DisposedAt <= toDate.Value);

        return await query
            .OrderByDescending(dc => dc.DisposedAt)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(DisposalCertificate entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.DisposalCertificates.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DisposalCertificate entity)
    {
        var existing = await _context.DisposalCertificates.FindAsync(entity.Id);
        if (existing == null) return false;

        existing.DisposalVerified = entity.DisposalVerified;
        existing.VerifiedAt = entity.VerifiedAt;
        existing.CertificateSignature = entity.CertificateSignature;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<string> GenerateCertificateNumberAsync()
    {
        // Generate format: DC-YYYYMMDD-XXXX (where XXXX is sequential for the day)
        var today = DateTime.Now.ToString("yyyyMMdd");
        var pattern = $"DC-{today}%";

        var count = await _context.DisposalCertificates
            .AsNoTracking()
            .CountAsync(dc => EF.Functions.Like(dc.CertificateNumber, pattern));

        return $"DC-{today}-{(count + 1):D4}";
    }
}
