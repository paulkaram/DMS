using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentVersionMetadataRepository : IDocumentVersionMetadataRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentVersionMetadataRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DocumentVersionMetadata>> GetByVersionIdAsync(Guid versionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentVersionMetadata>(
            "SELECT * FROM DocumentVersionMetadata WHERE DocumentVersionId = @VersionId ORDER BY FieldName",
            new { VersionId = versionId });
    }

    public async Task<IEnumerable<DocumentVersionMetadata>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentVersionMetadata>(
            "SELECT * FROM DocumentVersionMetadata WHERE DocumentId = @DocumentId ORDER BY DocumentVersionId, FieldName",
            new { DocumentId = documentId });
    }

    public async Task<Dictionary<Guid, List<DocumentVersionMetadata>>> GetMetadataForVersionsAsync(
        IEnumerable<Guid> versionIds)
    {
        var idList = versionIds.ToList();
        if (!idList.Any())
            return new Dictionary<Guid, List<DocumentVersionMetadata>>();

        using var connection = _connectionFactory.CreateConnection();
        var results = await connection.QueryAsync<DocumentVersionMetadata>(
            "SELECT * FROM DocumentVersionMetadata WHERE DocumentVersionId IN @VersionIds ORDER BY FieldName",
            new { VersionIds = idList });

        return results.GroupBy(m => m.DocumentVersionId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task SaveVersionMetadataAsync(Guid versionId, Guid documentId,
        IEnumerable<DocumentVersionMetadata> metadata)
    {
        var metadataList = metadata.ToList();
        if (!metadataList.Any())
            return;

        using var connection = _connectionFactory.CreateConnection();

        foreach (var item in metadataList)
        {
            item.Id = Guid.NewGuid();
            item.DocumentVersionId = versionId;
            item.DocumentId = documentId;
            item.CreatedAt = DateTime.UtcNow;
        }

        await connection.ExecuteAsync(@"
            INSERT INTO DocumentVersionMetadata (
                Id, DocumentVersionId, DocumentId, ContentTypeId, FieldId, FieldName,
                Value, NumericValue, DateValue, CreatedAt)
            VALUES (
                @Id, @DocumentVersionId, @DocumentId, @ContentTypeId, @FieldId, @FieldName,
                @Value, @NumericValue, @DateValue, @CreatedAt)",
            metadataList);
    }

    public async Task<bool> SnapshotCurrentMetadataToVersionAsync(Guid documentId, Guid versionId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Copy current document metadata to version metadata snapshot
        var affected = await connection.ExecuteAsync(@"
            INSERT INTO DocumentVersionMetadata (
                Id, DocumentVersionId, DocumentId, ContentTypeId, FieldId, FieldName,
                Value, NumericValue, DateValue, CreatedAt)
            SELECT
                NEWID(), @VersionId, DocumentId, ContentTypeId, FieldId, FieldName,
                Value, NumericValue, DateValue, GETUTCDATE()
            FROM DocumentMetadata
            WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId, VersionId = versionId });

        return affected > 0;
    }
}
