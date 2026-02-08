using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentCommentRepository : IDocumentCommentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentCommentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<DocumentComment>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentComment>(@"
            SELECT c.*, u.DisplayName as CreatedByName,
                   (SELECT COUNT(*) FROM DocumentComments WHERE ParentCommentId = c.Id AND IsDeleted = 0) as ReplyCount
            FROM DocumentComments c
            LEFT JOIN Users u ON c.CreatedBy = u.Id
            WHERE c.DocumentId = @DocumentId AND c.ParentCommentId IS NULL AND c.IsDeleted = 0
            ORDER BY c.CreatedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentComment>> GetRepliesAsync(Guid parentCommentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DocumentComment>(@"
            SELECT c.*, u.DisplayName as CreatedByName
            FROM DocumentComments c
            LEFT JOIN Users u ON c.CreatedBy = u.Id
            WHERE c.ParentCommentId = @ParentCommentId AND c.IsDeleted = 0
            ORDER BY c.CreatedAt ASC",
            new { ParentCommentId = parentCommentId });
    }

    public async Task<DocumentComment?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentComment>(@"
            SELECT c.*, u.DisplayName as CreatedByName
            FROM DocumentComments c
            LEFT JOIN Users u ON c.CreatedBy = u.Id
            WHERE c.Id = @Id",
            new { Id = id });
    }

    public async Task<Guid> AddAsync(DocumentComment comment)
    {
        comment.Id = Guid.NewGuid();
        comment.CreatedAt = DateTime.UtcNow;
        comment.IsDeleted = false;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentComments (Id, DocumentId, ParentCommentId, Content, CreatedBy, CreatedAt, IsDeleted)
            VALUES (@Id, @DocumentId, @ParentCommentId, @Content, @CreatedBy, @CreatedAt, @IsDeleted)",
            comment);

        return comment.Id;
    }

    public async Task<bool> UpdateAsync(DocumentComment comment)
    {
        comment.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentComments
            SET Content = @Content, ModifiedBy = @ModifiedBy, ModifiedAt = @ModifiedAt
            WHERE Id = @Id AND IsDeleted = 0",
            comment);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentComments
            SET IsDeleted = 1, ModifiedBy = @DeletedBy, ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            new { Id = id, DeletedBy = deletedBy, ModifiedAt = DateTime.UtcNow });

        return affected > 0;
    }

    public async Task<int> GetCommentCountAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM DocumentComments WHERE DocumentId = @DocumentId AND IsDeleted = 0",
            new { DocumentId = documentId });
    }
}
