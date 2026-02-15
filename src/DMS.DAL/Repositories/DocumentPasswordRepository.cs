using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentPasswordRepository : IDocumentPasswordRepository
{
    private readonly DmsDbContext _context;

    public DocumentPasswordRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentPassword?> GetByDocumentIdAsync(Guid documentId)
    {
        // Global query filter already handles IsActive == true
        return await _context.DocumentPasswords
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.DocumentId == documentId);
    }

    public async Task<DocumentPassword?> GetByIdAsync(Guid id)
    {
        // IgnoreQueryFilters: original Dapper query didn't filter by IsActive
        return await _context.DocumentPasswords
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Guid> AddAsync(DocumentPassword password)
    {
        password.Id = Guid.NewGuid();
        password.CreatedAt = DateTime.Now;
        password.IsActive = true;

        // Deactivate any existing password (bypass soft-delete filter to deactivate all)
        await _context.DocumentPasswords
            .IgnoreQueryFilters()
            .Where(p => p.DocumentId == password.DocumentId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

        _context.DocumentPasswords.Add(password);
        await _context.SaveChangesAsync();

        return password.Id;
    }

    public async Task<bool> UpdateAsync(DocumentPassword password)
    {
        password.ModifiedAt = DateTime.Now;

        // Global query filter ensures only IsActive == true records are matched
        var affected = await _context.DocumentPasswords
            .Where(p => p.Id == password.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.PasswordHash, password.PasswordHash)
                .SetProperty(p => p.Hint, password.Hint)
                .SetProperty(p => p.ExpiresAt, password.ExpiresAt)
                .SetProperty(p => p.ModifiedBy, password.ModifiedBy)
                .SetProperty(p => p.ModifiedAt, password.ModifiedAt));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid documentId)
    {
        // Soft delete: bypass filter to deactivate all for this document
        var affected = await _context.DocumentPasswords
            .IgnoreQueryFilters()
            .Where(p => p.DocumentId == documentId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

        return affected > 0;
    }

    public async Task<bool> HasPasswordAsync(Guid documentId)
    {
        var now = DateTime.Now;
        // Global query filter handles IsActive == true
        return await _context.DocumentPasswords
            .AsNoTracking()
            .AnyAsync(p => p.DocumentId == documentId
                && (p.ExpiresAt == null || p.ExpiresAt > now));
    }

    public async Task<bool> ValidatePasswordAsync(Guid documentId, string passwordHash)
    {
        var now = DateTime.Now;
        // Global query filter handles IsActive == true
        return await _context.DocumentPasswords
            .AsNoTracking()
            .AnyAsync(p => p.DocumentId == documentId
                && p.PasswordHash == passwordHash
                && (p.ExpiresAt == null || p.ExpiresAt > now));
    }

    public async Task<Dictionary<Guid, bool>> GetPasswordStatusBulkAsync(List<Guid> documentIds)
    {
        if (documentIds.Count == 0)
            return new Dictionary<Guid, bool>();

        var now = DateTime.Now;
        // Global query filter handles IsActive == true
        var documentsWithPassword = await _context.DocumentPasswords
            .AsNoTracking()
            .Where(p => documentIds.Contains(p.DocumentId)
                && (p.ExpiresAt == null || p.ExpiresAt > now))
            .Select(p => p.DocumentId)
            .Distinct()
            .ToListAsync();

        var passwordSet = new HashSet<Guid>(documentsWithPassword);
        return documentIds.ToDictionary(id => id, id => passwordSet.Contains(id));
    }
}
