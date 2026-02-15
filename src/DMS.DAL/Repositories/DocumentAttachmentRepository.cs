using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentAttachmentRepository : IDocumentAttachmentRepository
{
    private readonly DmsDbContext _context;

    public DocumentAttachmentRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentAttachment>> GetByDocumentIdAsync(Guid documentId) =>
        await _context.DocumentAttachments.AsNoTracking()
            .Where(a => a.DocumentId == documentId)
            .Join(_context.Users.AsNoTracking(), a => a.CreatedBy, u => u.Id,
                (a, u) => new DocumentAttachment
                {
                    Id = a.Id,
                    DocumentId = a.DocumentId,
                    FileName = a.FileName,
                    Description = a.Description,
                    ContentType = a.ContentType,
                    Size = a.Size,
                    StoragePath = a.StoragePath,
                    CreatedBy = a.CreatedBy,
                    CreatedAt = a.CreatedAt,
                    CreatedByName = u.DisplayName
                })
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<DocumentAttachment?> GetByIdAsync(Guid id) =>
        await _context.DocumentAttachments.AsNoTracking()
            .Where(a => a.Id == id)
            .Join(_context.Users.AsNoTracking(), a => a.CreatedBy, u => u.Id,
                (a, u) => new DocumentAttachment
                {
                    Id = a.Id,
                    DocumentId = a.DocumentId,
                    FileName = a.FileName,
                    Description = a.Description,
                    ContentType = a.ContentType,
                    Size = a.Size,
                    StoragePath = a.StoragePath,
                    CreatedBy = a.CreatedBy,
                    CreatedAt = a.CreatedAt,
                    CreatedByName = u.DisplayName
                })
            .FirstOrDefaultAsync();

    public async Task<Guid> AddAsync(DocumentAttachment attachment)
    {
        attachment.Id = Guid.NewGuid();
        attachment.CreatedAt = DateTime.Now;

        _context.DocumentAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return attachment.Id;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _context.DocumentAttachments.Where(a => a.Id == id)
            .ExecuteDeleteAsync() > 0;

    public async Task<int> GetAttachmentCountAsync(Guid documentId) =>
        await _context.DocumentAttachments.AsNoTracking()
            .CountAsync(a => a.DocumentId == documentId);

    public async Task<long> GetTotalSizeAsync(Guid documentId) =>
        await _context.DocumentAttachments.AsNoTracking()
            .Where(a => a.DocumentId == documentId)
            .SumAsync(a => a.Size);
}
