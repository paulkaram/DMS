using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentWorkingCopyRepository : IDocumentWorkingCopyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentWorkingCopyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DocumentWorkingCopy?> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentWorkingCopy>(
            "SELECT * FROM DocumentWorkingCopies WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentWorkingCopy>> GetAllByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentWorkingCopy>(
            "SELECT * FROM DocumentWorkingCopies WHERE CheckedOutBy = @UserId ORDER BY CheckedOutAt DESC",
            new { UserId = userId });
    }

    public async Task<Guid> CreateAsync(DocumentWorkingCopy workingCopy)
    {
        workingCopy.Id = Guid.NewGuid();
        workingCopy.CheckedOutAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentWorkingCopies (
                Id, DocumentId, CheckedOutBy, CheckedOutAt,
                DraftStoragePath, DraftSize, DraftContentType, DraftOriginalFileName, DraftIntegrityHash,
                DraftMetadataJson, DraftName, DraftDescription, DraftClassificationId,
                DraftImportanceId, DraftDocumentTypeId, LastModifiedAt, AutoSaveEnabled)
            VALUES (
                @Id, @DocumentId, @CheckedOutBy, @CheckedOutAt,
                @DraftStoragePath, @DraftSize, @DraftContentType, @DraftOriginalFileName, @DraftIntegrityHash,
                @DraftMetadataJson, @DraftName, @DraftDescription, @DraftClassificationId,
                @DraftImportanceId, @DraftDocumentTypeId, @LastModifiedAt, @AutoSaveEnabled)",
            workingCopy);

        return workingCopy.Id;
    }

    public async Task<bool> UpdateAsync(DocumentWorkingCopy workingCopy)
    {
        workingCopy.LastModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentWorkingCopies
            SET DraftStoragePath = @DraftStoragePath,
                DraftSize = @DraftSize,
                DraftContentType = @DraftContentType,
                DraftOriginalFileName = @DraftOriginalFileName,
                DraftIntegrityHash = @DraftIntegrityHash,
                DraftMetadataJson = @DraftMetadataJson,
                DraftName = @DraftName,
                DraftDescription = @DraftDescription,
                DraftClassificationId = @DraftClassificationId,
                DraftImportanceId = @DraftImportanceId,
                DraftDocumentTypeId = @DraftDocumentTypeId,
                LastModifiedAt = @LastModifiedAt,
                AutoSaveEnabled = @AutoSaveEnabled
            WHERE Id = @Id",
            workingCopy);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM DocumentWorkingCopies WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId });

        return affected > 0;
    }

    public async Task<IEnumerable<DocumentWorkingCopy>> GetStaleCheckoutsAsync(int staleHours)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentWorkingCopy>(@"
            SELECT * FROM DocumentWorkingCopies
            WHERE CheckedOutAt < DATEADD(HOUR, -@StaleHours, GETUTCDATE())
            ORDER BY CheckedOutAt",
            new { StaleHours = staleHours });
    }

    public async Task<bool> DeleteAllByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM DocumentWorkingCopies WHERE CheckedOutBy = @UserId",
            new { UserId = userId });

        return affected > 0;
    }
}
