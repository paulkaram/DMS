using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentAnnotationRepository : IDocumentAnnotationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentAnnotationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DocumentAnnotation>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentAnnotation>(@"
            SELECT a.*, u.DisplayName as CreatedByName
            FROM DocumentAnnotations a
            LEFT JOIN Users u ON a.CreatedBy = u.Id
            WHERE a.DocumentId = @DocumentId AND a.IsDeleted = 0
            ORDER BY a.PageNumber",
            new { DocumentId = documentId });
    }

    public async Task<DocumentAnnotation?> GetByDocumentAndPageAsync(Guid documentId, int pageNumber)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentAnnotation>(@"
            SELECT a.*, u.DisplayName as CreatedByName
            FROM DocumentAnnotations a
            LEFT JOIN Users u ON a.CreatedBy = u.Id
            WHERE a.DocumentId = @DocumentId AND a.PageNumber = @PageNumber AND a.IsDeleted = 0",
            new { DocumentId = documentId, PageNumber = pageNumber });
    }

    public async Task<DocumentAnnotation?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentAnnotation>(@"
            SELECT a.*, u.DisplayName as CreatedByName
            FROM DocumentAnnotations a
            LEFT JOIN Users u ON a.CreatedBy = u.Id
            WHERE a.Id = @Id",
            new { Id = id });
    }

    public async Task<Guid> UpsertAsync(DocumentAnnotation annotation)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Check if annotation exists for this document+page
        var existing = await connection.QueryFirstOrDefaultAsync<DocumentAnnotation>(
            "SELECT Id, VersionNumber FROM DocumentAnnotations WHERE DocumentId = @DocumentId AND PageNumber = @PageNumber AND IsDeleted = 0",
            new { annotation.DocumentId, annotation.PageNumber });

        if (existing != null)
        {
            // Update existing
            await connection.ExecuteAsync(@"
                UPDATE DocumentAnnotations
                SET AnnotationData = @AnnotationData,
                    VersionNumber = @VersionNumber,
                    ModifiedBy = @ModifiedBy,
                    ModifiedAt = @ModifiedAt
                WHERE Id = @Id",
                new
                {
                    Id = existing.Id,
                    annotation.AnnotationData,
                    VersionNumber = existing.VersionNumber + 1,
                    ModifiedBy = annotation.CreatedBy,
                    ModifiedAt = DateTime.UtcNow
                });
            return existing.Id;
        }
        else
        {
            // Insert new
            annotation.Id = Guid.NewGuid();
            annotation.CreatedAt = DateTime.UtcNow;
            annotation.IsDeleted = false;
            annotation.VersionNumber = 1;

            await connection.ExecuteAsync(@"
                INSERT INTO DocumentAnnotations (Id, DocumentId, PageNumber, AnnotationData, VersionNumber, CreatedBy, CreatedAt, IsDeleted)
                VALUES (@Id, @DocumentId, @PageNumber, @AnnotationData, @VersionNumber, @CreatedBy, @CreatedAt, @IsDeleted)",
                annotation);
            return annotation.Id;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentAnnotations
            SET IsDeleted = 1, ModifiedBy = @DeletedBy, ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            new { Id = id, DeletedBy = deletedBy, ModifiedAt = DateTime.UtcNow });
        return affected > 0;
    }

    public async Task<bool> DeleteAllByDocumentAsync(Guid documentId, Guid deletedBy)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentAnnotations
            SET IsDeleted = 1, ModifiedBy = @DeletedBy, ModifiedAt = @ModifiedAt
            WHERE DocumentId = @DocumentId AND IsDeleted = 0",
            new { DocumentId = documentId, DeletedBy = deletedBy, ModifiedAt = DateTime.UtcNow });
        return affected > 0;
    }

    public async Task<int> GetCountAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM DocumentAnnotations WHERE DocumentId = @DocumentId AND IsDeleted = 0",
            new { DocumentId = documentId });
    }
}

public class SavedSignatureRepository : ISavedSignatureRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SavedSignatureRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<SavedSignature>> GetByUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<SavedSignature>(
            "SELECT * FROM SavedSignatures WHERE UserId = @UserId ORDER BY IsDefault DESC, CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<SavedSignature?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<SavedSignature>(
            "SELECT * FROM SavedSignatures WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<Guid> AddAsync(SavedSignature signature)
    {
        signature.Id = Guid.NewGuid();
        signature.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO SavedSignatures (Id, UserId, Name, SignatureData, SignatureType, IsDefault, CreatedAt)
            VALUES (@Id, @UserId, @Name, @SignatureData, @SignatureType, @IsDefault, @CreatedAt)",
            signature);
        return signature.Id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM SavedSignatures WHERE Id = @Id",
            new { Id = id });
        return affected > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid userId, Guid signatureId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Clear existing default
        await connection.ExecuteAsync(
            "UPDATE SavedSignatures SET IsDefault = 0 WHERE UserId = @UserId",
            new { UserId = userId });

        // Set new default
        var affected = await connection.ExecuteAsync(
            "UPDATE SavedSignatures SET IsDefault = 1 WHERE Id = @Id AND UserId = @UserId",
            new { Id = signatureId, UserId = userId });

        return affected > 0;
    }
}
