using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentAttachmentRepository : IDocumentAttachmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentAttachmentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DocumentAttachment>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentAttachment>(@"
            SELECT a.*, u.DisplayName as CreatedByName
            FROM DocumentAttachments a
            LEFT JOIN Users u ON a.CreatedBy = u.Id
            WHERE a.DocumentId = @DocumentId
            ORDER BY a.CreatedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<DocumentAttachment?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentAttachment>(@"
            SELECT a.*, u.DisplayName as CreatedByName
            FROM DocumentAttachments a
            LEFT JOIN Users u ON a.CreatedBy = u.Id
            WHERE a.Id = @Id",
            new { Id = id });
    }

    public async Task<Guid> AddAsync(DocumentAttachment attachment)
    {
        attachment.Id = Guid.NewGuid();
        attachment.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentAttachments (Id, DocumentId, FileName, Description, ContentType, Size, StoragePath, CreatedBy, CreatedAt)
            VALUES (@Id, @DocumentId, @FileName, @Description, @ContentType, @Size, @StoragePath, @CreatedBy, @CreatedAt)",
            attachment);

        return attachment.Id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM DocumentAttachments WHERE Id = @Id",
            new { Id = id });

        return affected > 0;
    }

    public async Task<int> GetAttachmentCountAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM DocumentAttachments WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId });
    }

    public async Task<long> GetTotalSizeAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            "SELECT ISNULL(SUM(Size), 0) FROM DocumentAttachments WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId });
    }
}
