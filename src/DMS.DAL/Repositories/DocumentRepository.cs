using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Document>(
            "SELECT * FROM Documents WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Document>(
            "SELECT * FROM Documents WHERE IsActive = 1 ORDER BY Name");
    }

    public async Task<IEnumerable<Document>> GetByFolderIdAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Document>(
            "SELECT * FROM Documents WHERE FolderId = @FolderId AND IsActive = 1 ORDER BY Name",
            new { FolderId = folderId });
    }

    public async Task<IEnumerable<DocumentWithNames>> GetByFolderIdWithNamesAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT d.*,
                   u.DisplayName AS CreatedByName,
                   co.DisplayName AS CheckedOutByName,
                   ct.Name AS ContentTypeName,
                   CAST(0 AS BIT) AS IsShortcut,
                   CAST(NULL AS UNIQUEIDENTIFIER) AS ShortcutId,
                   (SELECT COUNT(*) FROM DocumentAttachments da WHERE da.DocumentId = d.Id) AS AttachmentCount
            FROM Documents d
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            LEFT JOIN Users co ON d.CheckedOutBy = co.Id
            LEFT JOIN ContentTypeDefinitions ct ON d.ContentTypeId = ct.Id
            WHERE d.FolderId = @FolderId AND d.IsActive = 1

            UNION ALL

            SELECT d.*,
                   u.DisplayName AS CreatedByName,
                   co.DisplayName AS CheckedOutByName,
                   ct.Name AS ContentTypeName,
                   CAST(1 AS BIT) AS IsShortcut,
                   ds.Id AS ShortcutId,
                   (SELECT COUNT(*) FROM DocumentAttachments da WHERE da.DocumentId = d.Id) AS AttachmentCount
            FROM DocumentShortcuts ds
            INNER JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            LEFT JOIN Users co ON d.CheckedOutBy = co.Id
            LEFT JOIN ContentTypeDefinitions ct ON d.ContentTypeId = ct.Id
            WHERE ds.FolderId = @FolderId AND d.IsActive = 1

            ORDER BY Name";
        return await connection.QueryAsync<DocumentWithNames>(sql, new { FolderId = folderId });
    }

    public async Task<IEnumerable<Document>> SearchAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM Documents WHERE IsActive = 1";

        if (!string.IsNullOrEmpty(name))
            sql += " AND Name LIKE @Name";
        if (folderId.HasValue)
            sql += " AND FolderId = @FolderId";
        if (classificationId.HasValue)
            sql += " AND ClassificationId = @ClassificationId";
        if (documentTypeId.HasValue)
            sql += " AND DocumentTypeId = @DocumentTypeId";

        sql += " ORDER BY Name";

        return await connection.QueryAsync<Document>(sql, new
        {
            Name = $"%{name}%",
            FolderId = folderId,
            ClassificationId = classificationId,
            DocumentTypeId = documentTypeId
        });
    }

    public async Task<IEnumerable<DocumentWithNames>> SearchWithNamesAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT d.*,
                   u.DisplayName AS CreatedByName,
                   co.DisplayName AS CheckedOutByName,
                   ct.Name AS ContentTypeName,
                   CAST(0 AS BIT) AS IsShortcut,
                   CAST(NULL AS UNIQUEIDENTIFIER) AS ShortcutId,
                   (SELECT COUNT(*) FROM DocumentAttachments da WHERE da.DocumentId = d.Id) AS AttachmentCount
            FROM Documents d
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            LEFT JOIN Users co ON d.CheckedOutBy = co.Id
            LEFT JOIN ContentTypeDefinitions ct ON d.ContentTypeId = ct.Id
            WHERE d.IsActive = 1";

        if (!string.IsNullOrEmpty(name))
            sql += " AND d.Name LIKE @Name";
        if (folderId.HasValue)
            sql += " AND d.FolderId = @FolderId";
        if (classificationId.HasValue)
            sql += " AND d.ClassificationId = @ClassificationId";
        if (documentTypeId.HasValue)
            sql += " AND d.DocumentTypeId = @DocumentTypeId";

        // Include shortcuts in the folder
        if (folderId.HasValue)
        {
            sql += @"
            UNION ALL
            SELECT d.*,
                   u.DisplayName AS CreatedByName,
                   co.DisplayName AS CheckedOutByName,
                   ct.Name AS ContentTypeName,
                   CAST(1 AS BIT) AS IsShortcut,
                   ds.Id AS ShortcutId,
                   (SELECT COUNT(*) FROM DocumentAttachments da WHERE da.DocumentId = d.Id) AS AttachmentCount
            FROM DocumentShortcuts ds
            INNER JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            LEFT JOIN Users co ON d.CheckedOutBy = co.Id
            LEFT JOIN ContentTypeDefinitions ct ON d.ContentTypeId = ct.Id
            WHERE ds.FolderId = @FolderId AND d.IsActive = 1";

            if (!string.IsNullOrEmpty(name))
                sql += " AND d.Name LIKE @Name";
            if (classificationId.HasValue)
                sql += " AND d.ClassificationId = @ClassificationId";
            if (documentTypeId.HasValue)
                sql += " AND d.DocumentTypeId = @DocumentTypeId";
        }

        sql += " ORDER BY Name";

        return await connection.QueryAsync<DocumentWithNames>(sql, new
        {
            Name = $"%{name}%",
            FolderId = folderId,
            ClassificationId = classificationId,
            DocumentTypeId = documentTypeId
        });
    }

    public async Task<IEnumerable<Document>> GetCheckedOutByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Document>(
            "SELECT * FROM Documents WHERE CheckedOutBy = @UserId AND IsCheckedOut = 1 AND IsActive = 1 ORDER BY Name",
            new { UserId = userId });
    }

    public async Task<IEnumerable<DocumentWithNames>> GetCheckedOutByUserWithNamesAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT d.*,
                   u.DisplayName AS CreatedByName,
                   co.DisplayName AS CheckedOutByName,
                   ct.Name AS ContentTypeName
            FROM Documents d
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            LEFT JOIN Users co ON d.CheckedOutBy = co.Id
            LEFT JOIN ContentTypeDefinitions ct ON d.ContentTypeId = ct.Id
            WHERE d.CheckedOutBy = @UserId AND d.IsCheckedOut = 1 AND d.IsActive = 1
            ORDER BY d.Name";
        return await connection.QueryAsync<DocumentWithNames>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<Document>> GetCreatedByUserAsync(Guid userId, int take = 50)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Document>(
            "SELECT TOP (@Take) * FROM Documents WHERE CreatedBy = @UserId AND IsActive = 1 ORDER BY CreatedAt DESC",
            new { UserId = userId, Take = take });
    }

    public async Task<IEnumerable<DocumentWithNames>> GetCreatedByUserWithNamesAsync(Guid userId, int take = 50)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT TOP (@Take) d.*,
                   u.DisplayName AS CreatedByName,
                   co.DisplayName AS CheckedOutByName,
                   ct.Name AS ContentTypeName
            FROM Documents d
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            LEFT JOIN Users co ON d.CheckedOutBy = co.Id
            LEFT JOIN ContentTypeDefinitions ct ON d.ContentTypeId = ct.Id
            WHERE d.CreatedBy = @UserId AND d.IsActive = 1
            ORDER BY d.CreatedAt DESC";
        return await connection.QueryAsync<DocumentWithNames>(sql, new { UserId = userId, Take = take });
    }

    public async Task<bool> CheckOutAsync(Guid id, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Documents
            SET IsCheckedOut = 1, CheckedOutBy = @UserId, CheckedOutAt = @CheckedOutAt
            WHERE Id = @Id AND IsCheckedOut = 0",
            new { Id = id, UserId = userId, CheckedOutAt = DateTime.UtcNow });

        return affected > 0;
    }

    public async Task<bool> CheckInAsync(Guid id, Guid userId, string? comment)
    {
        using var connection = _connectionFactory.CreateConnection();
        var document = await GetByIdAsync(id);
        if (document == null || !document.IsCheckedOut || document.CheckedOutBy != userId)
            return false;

        var newVersion = document.CurrentVersion + 1;

        await connection.ExecuteAsync(@"
            UPDATE Documents
            SET IsCheckedOut = 0, CheckedOutBy = NULL, CheckedOutAt = NULL,
                CurrentVersion = @NewVersion, ModifiedBy = @UserId, ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            new { Id = id, NewVersion = newVersion, UserId = userId, ModifiedAt = DateTime.UtcNow });

        return true;
    }

    public async Task<bool> DiscardCheckOutAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Documents
            SET IsCheckedOut = 0, CheckedOutBy = NULL, CheckedOutAt = NULL
            WHERE Id = @Id AND IsCheckedOut = 1",
            new { Id = id });

        return affected > 0;
    }

    public async Task<Document?> GetWithVersionsAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Document>(
            "SELECT * FROM Documents WHERE Id = @Id", new { Id = id });
    }

    public async Task<Guid> CreateAsync(Document entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Documents (
                Id, FolderId, Name, Description, Extension, ContentType, Size, StoragePath,
                CurrentVersion, IsCheckedOut, ClassificationId, ImportanceId, DocumentTypeId,
                IntegrityHash, HashAlgorithm, IntegrityVerifiedAt,
                RetentionPolicyId, IsOnLegalHold, LegalHoldId, LegalHoldAppliedAt, LegalHoldAppliedBy,
                IsOriginalRecord, SourceDocumentId, ContentCategory,
                IsActive, CreatedBy, CreatedAt)
            VALUES (
                @Id, @FolderId, @Name, @Description, @Extension, @ContentType, @Size, @StoragePath,
                @CurrentVersion, @IsCheckedOut, @ClassificationId, @ImportanceId, @DocumentTypeId,
                @IntegrityHash, @HashAlgorithm, @IntegrityVerifiedAt,
                @RetentionPolicyId, @IsOnLegalHold, @LegalHoldId, @LegalHoldAppliedAt, @LegalHoldAppliedBy,
                @IsOriginalRecord, @SourceDocumentId, @ContentCategory,
                @IsActive, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Document entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Documents
            SET FolderId = @FolderId,
                Name = @Name,
                Description = @Description,
                Extension = @Extension,
                ContentType = @ContentType,
                Size = @Size,
                StoragePath = @StoragePath,
                CurrentVersion = @CurrentVersion,
                CurrentMajorVersion = @CurrentMajorVersion,
                CurrentMinorVersion = @CurrentMinorVersion,
                CurrentVersionId = @CurrentVersionId,
                IsCheckedOut = @IsCheckedOut,
                CheckedOutBy = @CheckedOutBy,
                CheckedOutAt = @CheckedOutAt,
                ClassificationId = @ClassificationId,
                ImportanceId = @ImportanceId,
                DocumentTypeId = @DocumentTypeId,
                IntegrityHash = @IntegrityHash,
                HashAlgorithm = @HashAlgorithm,
                IntegrityVerifiedAt = @IntegrityVerifiedAt,
                RetentionPolicyId = @RetentionPolicyId,
                IsOnLegalHold = @IsOnLegalHold,
                LegalHoldId = @LegalHoldId,
                LegalHoldAppliedAt = @LegalHoldAppliedAt,
                LegalHoldAppliedBy = @LegalHoldAppliedBy,
                IsOriginalRecord = @IsOriginalRecord,
                SourceDocumentId = @SourceDocumentId,
                ContentCategory = @ContentCategory,
                ModifiedBy = @ModifiedBy,
                ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            entity);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE Documents SET IsActive = 0, ModifiedAt = @ModifiedAt WHERE Id = @Id",
            new { Id = id, ModifiedAt = DateTime.UtcNow });

        return affected > 0;
    }
}
