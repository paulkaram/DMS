using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentShareRepository : IDocumentShareRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentShareRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DocumentShare?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentShare>(@"
            SELECT ds.*, d.Name as DocumentName,
                   sw.DisplayName as SharedWithUserName, sb.DisplayName as SharedByUserName
            FROM DocumentShares ds
            LEFT JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Users sw ON ds.SharedWithUserId = sw.Id
            LEFT JOIN Users sb ON ds.SharedByUserId = sb.Id
            WHERE ds.Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<DocumentShare>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentShare>(@"
            SELECT ds.*, d.Name as DocumentName,
                   sw.DisplayName as SharedWithUserName, sb.DisplayName as SharedByUserName
            FROM DocumentShares ds
            LEFT JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Users sw ON ds.SharedWithUserId = sw.Id
            LEFT JOIN Users sb ON ds.SharedByUserId = sb.Id
            WHERE ds.DocumentId = @DocumentId AND ds.IsActive = 1
            ORDER BY ds.CreatedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentShare>> GetSharedWithUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentShare>(@"
            SELECT ds.*, d.Name as DocumentName,
                   sw.DisplayName as SharedWithUserName, sb.DisplayName as SharedByUserName
            FROM DocumentShares ds
            LEFT JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Users sw ON ds.SharedWithUserId = sw.Id
            LEFT JOIN Users sb ON ds.SharedByUserId = sb.Id
            WHERE ds.SharedWithUserId = @UserId AND ds.IsActive = 1
            AND (ds.ExpiresAt IS NULL OR ds.ExpiresAt > GETUTCDATE())
            ORDER BY ds.CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<DocumentShare>> GetSharedByUserAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentShare>(@"
            SELECT ds.*, d.Name as DocumentName,
                   sw.DisplayName as SharedWithUserName, sb.DisplayName as SharedByUserName
            FROM DocumentShares ds
            LEFT JOIN Documents d ON ds.DocumentId = d.Id
            LEFT JOIN Users sw ON ds.SharedWithUserId = sw.Id
            LEFT JOIN Users sb ON ds.SharedByUserId = sb.Id
            WHERE ds.SharedByUserId = @UserId AND ds.IsActive = 1
            ORDER BY ds.CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<Guid> CreateAsync(DocumentShare entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentShares (Id, DocumentId, SharedWithUserId, SharedByUserId,
                PermissionLevel, ExpiresAt, Message, IsActive, CreatedAt)
            VALUES (@Id, @DocumentId, @SharedWithUserId, @SharedByUserId,
                @PermissionLevel, @ExpiresAt, @Message, @IsActive, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DocumentShare entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentShares
            SET PermissionLevel = @PermissionLevel, ExpiresAt = @ExpiresAt,
                Message = @Message, IsActive = @IsActive
            WHERE Id = @Id",
            entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE DocumentShares SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
