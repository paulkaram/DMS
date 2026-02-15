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
            .Select(s => new DocumentShare
            {
                Id = s.Id,
                DocumentId = s.DocumentId,
                SharedWithUserId = s.SharedWithUserId,
                SharedByUserId = s.SharedByUserId,
                PermissionLevel = s.PermissionLevel,
                ExpiresAt = s.ExpiresAt,
                Message = s.Message,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                DocumentName = _context.Documents
                    .Where(d => d.Id == s.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                SharedWithUserName = _context.Users
                    .Where(u => u.Id == s.SharedWithUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                SharedByUserName = _context.Users
                    .Where(u => u.Id == s.SharedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentShare>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentShares
            .AsNoTracking()
            .Where(s => s.DocumentId == documentId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new DocumentShare
            {
                Id = s.Id,
                DocumentId = s.DocumentId,
                SharedWithUserId = s.SharedWithUserId,
                SharedByUserId = s.SharedByUserId,
                PermissionLevel = s.PermissionLevel,
                ExpiresAt = s.ExpiresAt,
                Message = s.Message,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                DocumentName = _context.Documents
                    .Where(d => d.Id == s.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                SharedWithUserName = _context.Users
                    .Where(u => u.Id == s.SharedWithUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                SharedByUserName = _context.Users
                    .Where(u => u.Id == s.SharedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentShare>> GetSharedWithUserAsync(Guid userId)
    {
        return await _context.DocumentShares
            .AsNoTracking()
            .Where(s => s.SharedWithUserId == userId
                && (s.ExpiresAt == null || s.ExpiresAt > DateTime.Now))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new DocumentShare
            {
                Id = s.Id,
                DocumentId = s.DocumentId,
                SharedWithUserId = s.SharedWithUserId,
                SharedByUserId = s.SharedByUserId,
                PermissionLevel = s.PermissionLevel,
                ExpiresAt = s.ExpiresAt,
                Message = s.Message,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                DocumentName = _context.Documents
                    .Where(d => d.Id == s.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                SharedWithUserName = _context.Users
                    .Where(u => u.Id == s.SharedWithUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                SharedByUserName = _context.Users
                    .Where(u => u.Id == s.SharedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentShare>> GetSharedByUserAsync(Guid userId)
    {
        return await _context.DocumentShares
            .AsNoTracking()
            .Where(s => s.SharedByUserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new DocumentShare
            {
                Id = s.Id,
                DocumentId = s.DocumentId,
                SharedWithUserId = s.SharedWithUserId,
                SharedByUserId = s.SharedByUserId,
                PermissionLevel = s.PermissionLevel,
                ExpiresAt = s.ExpiresAt,
                Message = s.Message,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                DocumentName = _context.Documents
                    .Where(d => d.Id == s.DocumentId)
                    .Select(d => d.Name)
                    .FirstOrDefault(),
                SharedWithUserName = _context.Users
                    .Where(u => u.Id == s.SharedWithUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault(),
                SharedByUserName = _context.Users
                    .Where(u => u.Id == s.SharedByUserId)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
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
