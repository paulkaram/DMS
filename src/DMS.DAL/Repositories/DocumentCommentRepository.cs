using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentCommentRepository : IDocumentCommentRepository
{
    private readonly DmsDbContext _context;

    public DocumentCommentRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentComment>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentComments
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId && c.ParentCommentId == null && !c.IsDeleted)
            .GroupJoin(_context.Users.AsNoTracking(), c => c.CreatedBy, u => u.Id, (c, users) => new { c, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.c, u })
            .GroupJoin(
                _context.DocumentComments.AsNoTracking().Where(r => !r.IsDeleted),
                x => x.c.Id, r => r.ParentCommentId,
                (x, replies) => new { x.c, x.u, ReplyCount = replies.Count() })
            .OrderByDescending(x => x.c.CreatedAt)
            .Select(x => new DocumentComment
            {
                Id = x.c.Id,
                DocumentId = x.c.DocumentId,
                ParentCommentId = x.c.ParentCommentId,
                Content = x.c.Content,
                CreatedBy = x.c.CreatedBy,
                CreatedAt = x.c.CreatedAt,
                ModifiedBy = x.c.ModifiedBy,
                ModifiedAt = x.c.ModifiedAt,
                IsDeleted = x.c.IsDeleted,
                CreatedByName = x.u != null ? x.u.DisplayName : null,
                ReplyCount = x.ReplyCount
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentComment>> GetRepliesAsync(Guid parentCommentId)
    {
        return await _context.DocumentComments
            .AsNoTracking()
            .Where(c => c.ParentCommentId == parentCommentId && !c.IsDeleted)
            .GroupJoin(_context.Users.AsNoTracking(), c => c.CreatedBy, u => u.Id, (c, users) => new { c, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.c, u })
            .OrderBy(x => x.c.CreatedAt)
            .Select(x => new DocumentComment
            {
                Id = x.c.Id,
                DocumentId = x.c.DocumentId,
                ParentCommentId = x.c.ParentCommentId,
                Content = x.c.Content,
                CreatedBy = x.c.CreatedBy,
                CreatedAt = x.c.CreatedAt,
                ModifiedBy = x.c.ModifiedBy,
                ModifiedAt = x.c.ModifiedAt,
                IsDeleted = x.c.IsDeleted,
                CreatedByName = x.u != null ? x.u.DisplayName : null
            })
            .ToListAsync();
    }

    public async Task<DocumentComment?> GetByIdAsync(Guid id)
    {
        return await _context.DocumentComments
            .AsNoTracking()
            .Where(c => c.Id == id)
            .GroupJoin(_context.Users.AsNoTracking(), c => c.CreatedBy, u => u.Id, (c, users) => new { c, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.c, u })
            .Select(x => new DocumentComment
            {
                Id = x.c.Id,
                DocumentId = x.c.DocumentId,
                ParentCommentId = x.c.ParentCommentId,
                Content = x.c.Content,
                CreatedBy = x.c.CreatedBy,
                CreatedAt = x.c.CreatedAt,
                ModifiedBy = x.c.ModifiedBy,
                ModifiedAt = x.c.ModifiedAt,
                IsDeleted = x.c.IsDeleted,
                CreatedByName = x.u != null ? x.u.DisplayName : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> AddAsync(DocumentComment comment)
    {
        comment.Id = Guid.NewGuid();
        comment.CreatedAt = DateTime.Now;
        comment.IsDeleted = false;

        _context.DocumentComments.Add(comment);
        await _context.SaveChangesAsync();

        return comment.Id;
    }

    public async Task<bool> UpdateAsync(DocumentComment comment)
    {
        var existing = await _context.DocumentComments
            .FirstOrDefaultAsync(c => c.Id == comment.Id && !c.IsDeleted);
        if (existing == null) return false;

        existing.Content = comment.Content;
        existing.ModifiedBy = comment.ModifiedBy;
        existing.ModifiedAt = DateTime.Now;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
    {
        var entity = await _context.DocumentComments.FindAsync(id);
        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.ModifiedBy = deletedBy;
        entity.ModifiedAt = DateTime.Now;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<int> GetCommentCountAsync(Guid documentId)
    {
        return await _context.DocumentComments
            .AsNoTracking()
            .CountAsync(c => c.DocumentId == documentId && !c.IsDeleted);
    }
}
