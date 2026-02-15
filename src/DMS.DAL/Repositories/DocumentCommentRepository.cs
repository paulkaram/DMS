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
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new DocumentComment
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                ParentCommentId = c.ParentCommentId,
                Content = c.Content,
                CreatedBy = c.CreatedBy,
                CreatedAt = c.CreatedAt,
                ModifiedBy = c.ModifiedBy,
                ModifiedAt = c.ModifiedAt,
                IsDeleted = c.IsDeleted,
                CreatedByName = _context.Users
                    .Where(u => u.Id == c.CreatedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                ReplyCount = _context.DocumentComments
                    .Count(r => r.ParentCommentId == c.Id && !r.IsDeleted)
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentComment>> GetRepliesAsync(Guid parentCommentId)
    {
        return await _context.DocumentComments
            .AsNoTracking()
            .Where(c => c.ParentCommentId == parentCommentId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new DocumentComment
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                ParentCommentId = c.ParentCommentId,
                Content = c.Content,
                CreatedBy = c.CreatedBy,
                CreatedAt = c.CreatedAt,
                ModifiedBy = c.ModifiedBy,
                ModifiedAt = c.ModifiedAt,
                IsDeleted = c.IsDeleted,
                CreatedByName = _context.Users
                    .Where(u => u.Id == c.CreatedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<DocumentComment?> GetByIdAsync(Guid id)
    {
        return await _context.DocumentComments
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new DocumentComment
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                ParentCommentId = c.ParentCommentId,
                Content = c.Content,
                CreatedBy = c.CreatedBy,
                CreatedAt = c.CreatedAt,
                ModifiedBy = c.ModifiedBy,
                ModifiedAt = c.ModifiedAt,
                IsDeleted = c.IsDeleted,
                CreatedByName = _context.Users
                    .Where(u => u.Id == c.CreatedBy)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
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
