using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentLinkRepository : IDocumentLinkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentLinkRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DocumentLink>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentLink>(@"
            SELECT l.*,
                   sd.Name as SourceDocumentName,
                   td.Name as TargetDocumentName,
                   u.DisplayName as CreatedByName
            FROM DocumentLinks l
            LEFT JOIN Documents sd ON l.SourceDocumentId = sd.Id
            LEFT JOIN Documents td ON l.TargetDocumentId = td.Id
            LEFT JOIN Users u ON l.CreatedBy = u.Id
            WHERE l.SourceDocumentId = @DocumentId
            ORDER BY l.CreatedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentLink>> GetIncomingLinksAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentLink>(@"
            SELECT l.*,
                   sd.Name as SourceDocumentName,
                   td.Name as TargetDocumentName,
                   u.DisplayName as CreatedByName
            FROM DocumentLinks l
            LEFT JOIN Documents sd ON l.SourceDocumentId = sd.Id
            LEFT JOIN Documents td ON l.TargetDocumentId = td.Id
            LEFT JOIN Users u ON l.CreatedBy = u.Id
            WHERE l.TargetDocumentId = @DocumentId
            ORDER BY l.CreatedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<DocumentLink?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentLink>(@"
            SELECT l.*,
                   sd.Name as SourceDocumentName,
                   td.Name as TargetDocumentName,
                   u.DisplayName as CreatedByName
            FROM DocumentLinks l
            LEFT JOIN Documents sd ON l.SourceDocumentId = sd.Id
            LEFT JOIN Documents td ON l.TargetDocumentId = td.Id
            LEFT JOIN Users u ON l.CreatedBy = u.Id
            WHERE l.Id = @Id",
            new { Id = id });
    }

    public async Task<DocumentLink?> GetExistingLinkAsync(Guid sourceDocumentId, Guid targetDocumentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentLink>(
            "SELECT * FROM DocumentLinks WHERE SourceDocumentId = @SourceDocumentId AND TargetDocumentId = @TargetDocumentId",
            new { SourceDocumentId = sourceDocumentId, TargetDocumentId = targetDocumentId });
    }

    public async Task<Guid> AddAsync(DocumentLink link)
    {
        link.Id = Guid.NewGuid();
        link.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentLinks (Id, SourceDocumentId, TargetDocumentId, LinkType, Description, CreatedBy, CreatedAt)
            VALUES (@Id, @SourceDocumentId, @TargetDocumentId, @LinkType, @Description, @CreatedBy, @CreatedAt)",
            link);

        return link.Id;
    }

    public async Task<bool> UpdateAsync(DocumentLink link)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentLinks
            SET LinkType = @LinkType, Description = @Description
            WHERE Id = @Id",
            link);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM DocumentLinks WHERE Id = @Id",
            new { Id = id });

        return affected > 0;
    }

    public async Task<int> GetLinkCountAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM DocumentLinks
            WHERE SourceDocumentId = @DocumentId OR TargetDocumentId = @DocumentId",
            new { DocumentId = documentId });
    }
}
