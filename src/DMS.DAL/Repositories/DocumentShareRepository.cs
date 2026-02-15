using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentShareRepository : IDocumentShareRepository
{
    private readonly DmsDbContext _context;

    public DocumentShareRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentShare?> GetByIdAsync(Guid id)
    {
        return await _context.DocumentShares
            .AsNoTracking()
            .Where(s => s.Id == id)
            .GroupJoin(_context.Documents.AsNoTracking(), s => s.DocumentId, d => d.Id, (s, docs) => new { s, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.s, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedWithUserId, u => u.Id, (x, sharedWithUsers) => new { x.s, x.d, sharedWithUsers })
            .SelectMany(x => x.sharedWithUsers.DefaultIfEmpty(), (x, swu) => new { x.s, x.d, swu })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedByUserId, u => u.Id, (x, sharedByUsers) => new { x.s, x.d, x.swu, sharedByUsers })
            .SelectMany(x => x.sharedByUsers.DefaultIfEmpty(), (x, sbu) => new { x.s, x.d, x.swu, sbu })
            .Select(x => new DocumentShare
            {
                Id = x.s.Id,
                DocumentId = x.s.DocumentId,
                SharedWithUserId = x.s.SharedWithUserId,
                SharedByUserId = x.s.SharedByUserId,
                PermissionLevel = x.s.PermissionLevel,
                ExpiresAt = x.s.ExpiresAt,
                Message = x.s.Message,
                IsActive = x.s.IsActive,
                CreatedAt = x.s.CreatedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                SharedWithUserName = x.swu != null ? x.swu.DisplayName : null,
                SharedByUserName = x.sbu != null ? x.sbu.DisplayName : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentShare>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentShares
            .AsNoTracking()
            .Where(s => s.DocumentId == documentId)
            .GroupJoin(_context.Documents.AsNoTracking(), s => s.DocumentId, d => d.Id, (s, docs) => new { s, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.s, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedWithUserId, u => u.Id, (x, sharedWithUsers) => new { x.s, x.d, sharedWithUsers })
            .SelectMany(x => x.sharedWithUsers.DefaultIfEmpty(), (x, swu) => new { x.s, x.d, swu })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedByUserId, u => u.Id, (x, sharedByUsers) => new { x.s, x.d, x.swu, sharedByUsers })
            .SelectMany(x => x.sharedByUsers.DefaultIfEmpty(), (x, sbu) => new { x.s, x.d, x.swu, sbu })
            .OrderByDescending(x => x.s.CreatedAt)
            .Select(x => new DocumentShare
            {
                Id = x.s.Id,
                DocumentId = x.s.DocumentId,
                SharedWithUserId = x.s.SharedWithUserId,
                SharedByUserId = x.s.SharedByUserId,
                PermissionLevel = x.s.PermissionLevel,
                ExpiresAt = x.s.ExpiresAt,
                Message = x.s.Message,
                IsActive = x.s.IsActive,
                CreatedAt = x.s.CreatedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                SharedWithUserName = x.swu != null ? x.swu.DisplayName : null,
                SharedByUserName = x.sbu != null ? x.sbu.DisplayName : null
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentShare>> GetSharedWithUserAsync(Guid userId)
    {
        return await _context.DocumentShares
            .AsNoTracking()
            .Where(s => s.SharedWithUserId == userId
                && (s.ExpiresAt == null || s.ExpiresAt > DateTime.Now))
            .GroupJoin(_context.Documents.AsNoTracking(), s => s.DocumentId, d => d.Id, (s, docs) => new { s, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.s, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedWithUserId, u => u.Id, (x, sharedWithUsers) => new { x.s, x.d, sharedWithUsers })
            .SelectMany(x => x.sharedWithUsers.DefaultIfEmpty(), (x, swu) => new { x.s, x.d, swu })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedByUserId, u => u.Id, (x, sharedByUsers) => new { x.s, x.d, x.swu, sharedByUsers })
            .SelectMany(x => x.sharedByUsers.DefaultIfEmpty(), (x, sbu) => new { x.s, x.d, x.swu, sbu })
            .OrderByDescending(x => x.s.CreatedAt)
            .Select(x => new DocumentShare
            {
                Id = x.s.Id,
                DocumentId = x.s.DocumentId,
                SharedWithUserId = x.s.SharedWithUserId,
                SharedByUserId = x.s.SharedByUserId,
                PermissionLevel = x.s.PermissionLevel,
                ExpiresAt = x.s.ExpiresAt,
                Message = x.s.Message,
                IsActive = x.s.IsActive,
                CreatedAt = x.s.CreatedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                SharedWithUserName = x.swu != null ? x.swu.DisplayName : null,
                SharedByUserName = x.sbu != null ? x.sbu.DisplayName : null
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentShare>> GetSharedByUserAsync(Guid userId)
    {
        return await _context.DocumentShares
            .AsNoTracking()
            .Where(s => s.SharedByUserId == userId)
            .GroupJoin(_context.Documents.AsNoTracking(), s => s.DocumentId, d => d.Id, (s, docs) => new { s, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.s, d })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedWithUserId, u => u.Id, (x, sharedWithUsers) => new { x.s, x.d, sharedWithUsers })
            .SelectMany(x => x.sharedWithUsers.DefaultIfEmpty(), (x, swu) => new { x.s, x.d, swu })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.s.SharedByUserId, u => u.Id, (x, sharedByUsers) => new { x.s, x.d, x.swu, sharedByUsers })
            .SelectMany(x => x.sharedByUsers.DefaultIfEmpty(), (x, sbu) => new { x.s, x.d, x.swu, sbu })
            .OrderByDescending(x => x.s.CreatedAt)
            .Select(x => new DocumentShare
            {
                Id = x.s.Id,
                DocumentId = x.s.DocumentId,
                SharedWithUserId = x.s.SharedWithUserId,
                SharedByUserId = x.s.SharedByUserId,
                PermissionLevel = x.s.PermissionLevel,
                ExpiresAt = x.s.ExpiresAt,
                Message = x.s.Message,
                IsActive = x.s.IsActive,
                CreatedAt = x.s.CreatedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                SharedWithUserName = x.swu != null ? x.swu.DisplayName : null,
                SharedByUserName = x.sbu != null ? x.sbu.DisplayName : null
            })
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(DocumentShare entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.DocumentShares.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DocumentShare entity)
    {
        var existing = await _context.DocumentShares.FindAsync(entity.Id);
        if (existing == null) return false;

        existing.PermissionLevel = entity.PermissionLevel;
        existing.ExpiresAt = entity.ExpiresAt;
        existing.Message = entity.Message;
        existing.IsActive = entity.IsActive;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.DocumentShares.FindAsync(id);
        if (entity == null) return false;

        entity.IsActive = false;
        return await _context.SaveChangesAsync() > 0;
    }
}
