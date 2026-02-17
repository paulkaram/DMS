using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DisposalRequestRepository : IDisposalRequestRepository
{
    private readonly DmsDbContext _context;

    public DisposalRequestRepository(DmsDbContext context) => _context = context;

    public async Task<DisposalRequest?> GetByIdAsync(Guid id)
    {
        return await _context.DisposalRequests.FindAsync(id);
    }

    public async Task<DisposalRequest?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.DisposalRequests
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<(List<DisposalRequest> Items, int TotalCount)> GetPaginatedAsync(
        DisposalRequestStatus? status, int page, int pageSize)
    {
        var query = _context.DisposalRequests.AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Guid> CreateAsync(DisposalRequest request)
    {
        request.Id = Guid.NewGuid();
        _context.DisposalRequests.Add(request);
        await _context.SaveChangesAsync();
        return request.Id;
    }

    public async Task UpdateAsync(DisposalRequest request)
    {
        _context.DisposalRequests.Update(request);
        await _context.SaveChangesAsync();
    }

    public async Task<string> GenerateRequestNumberAsync()
    {
        var year = DateTime.Now.Year;
        var count = await _context.DisposalRequests
            .CountAsync(r => r.RequestedAt.Year == year);
        return $"DSP-{year}-{(count + 1):D4}";
    }

    public async Task AddDocumentsAsync(Guid requestId, IEnumerable<Guid> documentIds)
    {
        foreach (var docId in documentIds)
        {
            _context.DisposalRequestDocuments.Add(new DisposalRequestDocument
            {
                Id = Guid.NewGuid(),
                DisposalRequestId = requestId,
                DocumentId = docId,
                Status = DisposalRequestDocumentStatus.Pending
            });
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<DisposalRequestDocument>> GetDocumentsAsync(Guid requestId)
    {
        return await _context.DisposalRequestDocuments
            .Where(d => d.DisposalRequestId == requestId)
            .ToListAsync();
    }

    public async Task UpdateDocumentStatusAsync(Guid requestDocId, DisposalRequestDocumentStatus status,
        Guid? certificateId = null, string? error = null)
    {
        var doc = await _context.DisposalRequestDocuments.FindAsync(requestDocId);
        if (doc != null)
        {
            doc.Status = status;
            doc.CertificateId = certificateId;
            doc.ErrorMessage = error;
            await _context.SaveChangesAsync();
        }
    }

    public async Task AddApprovalAsync(DisposalApproval approval)
    {
        approval.Id = Guid.NewGuid();
        _context.DisposalApprovals.Add(approval);
        await _context.SaveChangesAsync();
    }

    public async Task<List<DisposalApproval>> GetApprovalsAsync(Guid requestId)
    {
        return await _context.DisposalApprovals
            .Where(a => a.DisposalRequestId == requestId)
            .OrderBy(a => a.ApprovalLevel)
            .ToListAsync();
    }
}
