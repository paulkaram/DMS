using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentShortcutRepository : IDocumentShortcutRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentShortcutRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DocumentShortcut>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentShortcut>(@"
            SELECT ds.*, d.Name as DocumentName, f.Name as FolderName, f.Path as FolderPath
            FROM DocumentShortcuts ds
            LEFT JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Folders f ON ds.FolderId = f.Id
            WHERE ds.DocumentId = @DocumentId
            ORDER BY f.Name",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentShortcut>> GetByFolderIdAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentShortcut>(@"
            SELECT ds.*, d.Name as DocumentName, f.Name as FolderName, f.Path as FolderPath
            FROM DocumentShortcuts ds
            LEFT JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Folders f ON ds.FolderId = f.Id
            WHERE ds.FolderId = @FolderId
            ORDER BY d.Name",
            new { FolderId = folderId });
    }

    public async Task<DocumentShortcut?> GetByDocumentAndFolderAsync(Guid documentId, Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentShortcut>(
            "SELECT * FROM DocumentShortcuts WHERE DocumentId = @DocumentId AND FolderId = @FolderId",
            new { DocumentId = documentId, FolderId = folderId });
    }

    public async Task<Guid> CreateAsync(DocumentShortcut entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentShortcuts (Id, DocumentId, FolderId, CreatedBy, CreatedAt)
            VALUES (@Id, @DocumentId, @FolderId, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM DocumentShortcuts WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<int> DeleteAllByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "DELETE FROM DocumentShortcuts WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId });
    }
}
