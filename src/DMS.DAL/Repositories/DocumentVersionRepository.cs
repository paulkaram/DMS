using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentVersionRepository : IDocumentVersionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentVersionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DocumentVersion?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentVersion>(
            "SELECT * FROM DocumentVersions WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<DocumentVersion>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentVersion>(
            "SELECT * FROM DocumentVersions WHERE DocumentId = @DocumentId ORDER BY VersionNumber DESC",
            new { DocumentId = documentId });
    }

    public async Task<DocumentVersion?> GetLatestVersionAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentVersion>(
            "SELECT TOP 1 * FROM DocumentVersions WHERE DocumentId = @DocumentId ORDER BY VersionNumber DESC",
            new { DocumentId = documentId });
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

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentVersions (
                Id, DocumentId, VersionNumber, StoragePath, Size, Comment,
                IntegrityHash, HashAlgorithm, IntegrityVerifiedAt,
                ContentType, OriginalFileName, IsOriginalRecord, ContentCategory,
                VersionType, VersionLabel, MajorVersion, MinorVersion,
                IsContentChanged, IsMetadataChanged, PreviousVersionId, ChangeDescription,
                CreatedBy, CreatedAt)
            VALUES (
                @Id, @DocumentId, @VersionNumber, @StoragePath, @Size, @Comment,
                @IntegrityHash, @HashAlgorithm, @IntegrityVerifiedAt,
                @ContentType, @OriginalFileName, @IsOriginalRecord, @ContentCategory,
                @VersionType, @VersionLabel, @MajorVersion, @MinorVersion,
                @IsContentChanged, @IsMetadataChanged, @PreviousVersionId, @ChangeDescription,
                @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DocumentVersion entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentVersions
            SET IntegrityHash = @IntegrityHash,
                HashAlgorithm = @HashAlgorithm,
                IntegrityVerifiedAt = @IntegrityVerifiedAt,
                Comment = @Comment,
                ChangeDescription = @ChangeDescription
            WHERE Id = @Id",
            entity);

        return affected > 0;
    }

    public async Task<DocumentVersion?> GetByVersionNumberAsync(Guid documentId, int majorVersion, int minorVersion)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentVersion>(@"
            SELECT * FROM DocumentVersions
            WHERE DocumentId = @DocumentId
              AND MajorVersion = @MajorVersion
              AND MinorVersion = @MinorVersion",
            new { DocumentId = documentId, MajorVersion = majorVersion, MinorVersion = minorVersion });
    }

    public async Task<DocumentVersion?> GetLatestMajorVersionAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentVersion>(@"
            SELECT TOP 1 * FROM DocumentVersions
            WHERE DocumentId = @DocumentId
              AND VersionType = 'Major'
            ORDER BY MajorVersion DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentVersion>> GetMajorVersionsAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentVersion>(@"
            SELECT * FROM DocumentVersions
            WHERE DocumentId = @DocumentId
              AND VersionType = 'Major'
            ORDER BY MajorVersion DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentVersion>> GetMinorVersionsAsync(Guid documentId, int majorVersion)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentVersion>(@"
            SELECT * FROM DocumentVersions
            WHERE DocumentId = @DocumentId
              AND MajorVersion = @MajorVersion
            ORDER BY MinorVersion DESC",
            new { DocumentId = documentId, MajorVersion = majorVersion });
    }
}
