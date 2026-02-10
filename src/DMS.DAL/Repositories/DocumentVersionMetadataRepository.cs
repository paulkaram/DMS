using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentVersionMetadataRepository : IDocumentVersionMetadataRepository
{
    private readonly DmsDbContext _context;

    public DocumentVersionMetadataRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentVersionMetadata>> GetByVersionIdAsync(Guid versionId)
    {
        return await _context.DocumentVersionMetadata
            .AsNoTracking()
            .Where(m => m.DocumentVersionId == versionId)
            .OrderBy(m => m.FieldName)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentVersionMetadata>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentVersionMetadata
            .AsNoTracking()
            .Where(m => m.DocumentId == documentId)
            .OrderBy(m => m.DocumentVersionId)
            .ThenBy(m => m.FieldName)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, List<DocumentVersionMetadata>>> GetMetadataForVersionsAsync(
        IEnumerable<Guid> versionIds)
    {
        var idList = versionIds.ToList();
        if (!idList.Any())
            return new Dictionary<Guid, List<DocumentVersionMetadata>>();

        var results = await _context.DocumentVersionMetadata
            .AsNoTracking()
            .Where(m => idList.Contains(m.DocumentVersionId))
            .OrderBy(m => m.FieldName)
            .ToListAsync();

        return results.GroupBy(m => m.DocumentVersionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task SaveVersionMetadataAsync(Guid versionId, Guid documentId,
        IEnumerable<DocumentVersionMetadata> metadata)
    {
        var metadataList = metadata.ToList();
        if (!metadataList.Any())
            return;

        foreach (var item in metadataList)
        {
            item.Id = Guid.NewGuid();
            item.DocumentVersionId = versionId;
            item.DocumentId = documentId;
            item.CreatedAt = DateTime.UtcNow;
        }

        _context.DocumentVersionMetadata.AddRange(metadataList);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SnapshotCurrentMetadataToVersionAsync(Guid documentId, Guid versionId)
    {
        // Copy current document metadata to version metadata snapshot using raw SQL
        // This is more efficient than loading all metadata into memory and re-inserting
        var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO DocumentVersionMetadata (
                Id, DocumentVersionId, DocumentId, ContentTypeId, FieldId, FieldName,
                Value, NumericValue, DateValue, CreatedAt)
            SELECT
                NEWID(), {versionId}, DocumentId, ContentTypeId, FieldId, FieldName,
                Value, NumericValue, DateValue, GETUTCDATE()
            FROM DocumentMetadata
            WHERE DocumentId = {documentId}");

        return affected > 0;
    }
}
