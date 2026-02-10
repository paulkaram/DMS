using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentAnnotationRepository : IDocumentAnnotationRepository
{
    private readonly DmsDbContext _context;

    public DocumentAnnotationRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentAnnotation>> GetByDocumentIdAsync(Guid documentId) =>
        await _context.DocumentAnnotations.AsNoTracking()
            .Where(a => a.DocumentId == documentId && !a.IsDeleted)
            .GroupJoin(_context.Users.AsNoTracking(), a => a.CreatedBy, u => u.Id, (a, users) => new { a, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new DocumentAnnotation
            {
                Id = x.a.Id,
                DocumentId = x.a.DocumentId,
                PageNumber = x.a.PageNumber,
                AnnotationData = x.a.AnnotationData,
                VersionNumber = x.a.VersionNumber,
                CreatedBy = x.a.CreatedBy,
                CreatedAt = x.a.CreatedAt,
                ModifiedBy = x.a.ModifiedBy,
                ModifiedAt = x.a.ModifiedAt,
                IsDeleted = x.a.IsDeleted,
                CreatedByName = u != null ? u.DisplayName : null
            })
            .OrderBy(a => a.PageNumber)
            .ToListAsync();

    public async Task<DocumentAnnotation?> GetByDocumentAndPageAsync(Guid documentId, int pageNumber) =>
        await _context.DocumentAnnotations.AsNoTracking()
            .Where(a => a.DocumentId == documentId && a.PageNumber == pageNumber && !a.IsDeleted)
            .GroupJoin(_context.Users.AsNoTracking(), a => a.CreatedBy, u => u.Id, (a, users) => new { a, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new DocumentAnnotation
            {
                Id = x.a.Id,
                DocumentId = x.a.DocumentId,
                PageNumber = x.a.PageNumber,
                AnnotationData = x.a.AnnotationData,
                VersionNumber = x.a.VersionNumber,
                CreatedBy = x.a.CreatedBy,
                CreatedAt = x.a.CreatedAt,
                ModifiedBy = x.a.ModifiedBy,
                ModifiedAt = x.a.ModifiedAt,
                IsDeleted = x.a.IsDeleted,
                CreatedByName = u != null ? u.DisplayName : null
            })
            .FirstOrDefaultAsync();

    public async Task<DocumentAnnotation?> GetByIdAsync(Guid id) =>
        await _context.DocumentAnnotations.AsNoTracking()
            .Where(a => a.Id == id)
            .GroupJoin(_context.Users.AsNoTracking(), a => a.CreatedBy, u => u.Id, (a, users) => new { a, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new DocumentAnnotation
            {
                Id = x.a.Id,
                DocumentId = x.a.DocumentId,
                PageNumber = x.a.PageNumber,
                AnnotationData = x.a.AnnotationData,
                VersionNumber = x.a.VersionNumber,
                CreatedBy = x.a.CreatedBy,
                CreatedAt = x.a.CreatedAt,
                ModifiedBy = x.a.ModifiedBy,
                ModifiedAt = x.a.ModifiedAt,
                IsDeleted = x.a.IsDeleted,
                CreatedByName = u != null ? u.DisplayName : null
            })
            .FirstOrDefaultAsync();

    public async Task<Guid> UpsertAsync(DocumentAnnotation annotation)
    {
        // Check if annotation exists for this document+page
        var existing = await _context.DocumentAnnotations
            .Where(a => a.DocumentId == annotation.DocumentId && a.PageNumber == annotation.PageNumber && !a.IsDeleted)
            .Select(a => new { a.Id, a.VersionNumber })
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            // Update existing
            await _context.DocumentAnnotations
                .Where(a => a.Id == existing.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.AnnotationData, annotation.AnnotationData)
                    .SetProperty(a => a.VersionNumber, existing.VersionNumber + 1)
                    .SetProperty(a => a.ModifiedBy, annotation.CreatedBy)
                    .SetProperty(a => a.ModifiedAt, DateTime.UtcNow));
            return existing.Id;
        }
        else
        {
            // Insert new
            annotation.Id = Guid.NewGuid();
            annotation.CreatedAt = DateTime.UtcNow;
            annotation.IsDeleted = false;
            annotation.VersionNumber = 1;

            _context.DocumentAnnotations.Add(annotation);
            await _context.SaveChangesAsync();
            return annotation.Id;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy) =>
        await _context.DocumentAnnotations
            .Where(a => a.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.IsDeleted, true)
                .SetProperty(a => a.ModifiedBy, deletedBy)
                .SetProperty(a => a.ModifiedAt, DateTime.UtcNow)) > 0;

    public async Task<bool> DeleteAllByDocumentAsync(Guid documentId, Guid deletedBy) =>
        await _context.DocumentAnnotations
            .Where(a => a.DocumentId == documentId && !a.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.IsDeleted, true)
                .SetProperty(a => a.ModifiedBy, deletedBy)
                .SetProperty(a => a.ModifiedAt, DateTime.UtcNow)) > 0;

    public async Task<int> GetCountAsync(Guid documentId) =>
        await _context.DocumentAnnotations.AsNoTracking()
            .CountAsync(a => a.DocumentId == documentId && !a.IsDeleted);
}

public class SavedSignatureRepository : ISavedSignatureRepository
{
    private readonly DmsDbContext _context;

    public SavedSignatureRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SavedSignature>> GetByUserIdAsync(Guid userId) =>
        await _context.SavedSignatures.AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.IsDefault)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task<SavedSignature?> GetByIdAsync(Guid id) =>
        await _context.SavedSignatures.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Guid> AddAsync(SavedSignature signature)
    {
        signature.Id = Guid.NewGuid();
        signature.CreatedAt = DateTime.UtcNow;

        _context.SavedSignatures.Add(signature);
        await _context.SaveChangesAsync();

        return signature.Id;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _context.SavedSignatures
            .Where(s => s.Id == id)
            .ExecuteDeleteAsync() > 0;

    public async Task<bool> SetDefaultAsync(Guid userId, Guid signatureId)
    {
        // Clear existing default
        await _context.SavedSignatures
            .Where(s => s.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsDefault, false));

        // Set new default
        return await _context.SavedSignatures
            .Where(s => s.Id == signatureId && s.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsDefault, true)) > 0;
    }
}
