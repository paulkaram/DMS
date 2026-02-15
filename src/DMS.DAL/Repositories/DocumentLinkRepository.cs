using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentLinkRepository : IDocumentLinkRepository
{
    private readonly DmsDbContext _context;

    public DocumentLinkRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentLink>> GetByDocumentIdAsync(Guid documentId) =>
        await BuildLinkQuery()
            .Where(l => l.SourceDocumentId == documentId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<DocumentLink>> GetIncomingLinksAsync(Guid documentId) =>
        await BuildLinkQuery()
            .Where(l => l.TargetDocumentId == documentId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

    public async Task<DocumentLink?> GetByIdAsync(Guid id) =>
        await BuildLinkQuery()
            .FirstOrDefaultAsync(l => l.Id == id);

    public async Task<DocumentLink?> GetExistingLinkAsync(Guid sourceDocumentId, Guid targetDocumentId) =>
        await _context.DocumentLinks.AsNoTracking()
            .FirstOrDefaultAsync(l => l.SourceDocumentId == sourceDocumentId
                                   && l.TargetDocumentId == targetDocumentId);

    public async Task<Guid> AddAsync(DocumentLink link)
    {
        link.Id = Guid.NewGuid();
        link.CreatedAt = DateTime.Now;

        _context.DocumentLinks.Add(link);
        await _context.SaveChangesAsync();

        return link.Id;
    }

    public async Task<bool> UpdateAsync(DocumentLink link)
    {
        return await _context.DocumentLinks.Where(l => l.Id == link.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.LinkType, link.LinkType)
                .SetProperty(l => l.Description, link.Description)) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _context.DocumentLinks.Where(l => l.Id == id)
            .ExecuteDeleteAsync() > 0;

    public async Task<int> GetLinkCountAsync(Guid documentId) =>
        await _context.DocumentLinks.AsNoTracking()
            .CountAsync(l => l.SourceDocumentId == documentId
                          || l.TargetDocumentId == documentId);

    /// <summary>
    /// Builds the common link query with JOINs for source/target document names and creator name.
    /// </summary>
    private IQueryable<DocumentLink> BuildLinkQuery() =>
        from l in _context.DocumentLinks.AsNoTracking()
        join sd in _context.Documents.AsNoTracking() on l.SourceDocumentId equals sd.Id into sds
        from sd in sds.DefaultIfEmpty()
        join td in _context.Documents.AsNoTracking() on l.TargetDocumentId equals td.Id into tds
        from td in tds.DefaultIfEmpty()
        join u in _context.Users.AsNoTracking() on l.CreatedBy equals u.Id into us
        from u in us.DefaultIfEmpty()
        select new DocumentLink
        {
            Id = l.Id,
            SourceDocumentId = l.SourceDocumentId,
            TargetDocumentId = l.TargetDocumentId,
            LinkType = l.LinkType,
            Description = l.Description,
            CreatedBy = l.CreatedBy,
            CreatedAt = l.CreatedAt,
            SourceDocumentName = sd != null ? sd.Name : null,
            TargetDocumentName = td != null ? td.Name : null,
            CreatedByName = u != null ? u.DisplayName : null
        };
}
