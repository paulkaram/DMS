using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentVersionRepository : IDocumentVersionRepository
{
    private readonly DmsDbContext _context;

    public DocumentVersionRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentVersion?> GetByIdAsync(Guid id)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<DocumentVersion>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }

    public async Task<DocumentVersion?> GetLatestVersionAsync(Guid documentId)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(DocumentVersion entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        // Set version label if not provided
        if (string.IsNullOrEmpty(entity.VersionLabel))
        {
            entity.VersionLabel = $"{entity.MajorVersion}.{entity.MinorVersion}";
        }

        _context.DocumentVersions.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DocumentVersion entity)
    {
        var affected = await _context.DocumentVersions
            .Where(v => v.Id == entity.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.IntegrityHash, entity.IntegrityHash)
                .SetProperty(v => v.HashAlgorithm, entity.HashAlgorithm)
                .SetProperty(v => v.IntegrityVerifiedAt, entity.IntegrityVerifiedAt)
                .SetProperty(v => v.Comment, entity.Comment)
                .SetProperty(v => v.ChangeDescription, entity.ChangeDescription));

        return affected > 0;
    }

    public async Task<DocumentVersion?> GetByVersionNumberAsync(Guid documentId, int majorVersion, int minorVersion)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.DocumentId == documentId
                && v.MajorVersion == majorVersion
                && v.MinorVersion == minorVersion);
    }

    public async Task<DocumentVersion?> GetLatestMajorVersionAsync(Guid documentId)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .Where(v => v.DocumentId == documentId && v.VersionType == "Major")
            .OrderByDescending(v => v.MajorVersion)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DocumentVersion>> GetMajorVersionsAsync(Guid documentId)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .Where(v => v.DocumentId == documentId && v.VersionType == "Major")
            .OrderByDescending(v => v.MajorVersion)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentVersion>> GetMinorVersionsAsync(Guid documentId, int majorVersion)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .Where(v => v.DocumentId == documentId && v.MajorVersion == majorVersion)
            .OrderByDescending(v => v.MinorVersion)
            .ToListAsync();
    }
}
