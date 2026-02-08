using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class RecycleBinRepository : IRecycleBinRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RecycleBinRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<RecycleBinItem>> GetByUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<RecycleBinItem>(@"
            SELECT rb.*, u.DisplayName as DeletedByUserName
            FROM RecycleBin rb
            LEFT JOIN Users u ON rb.DeletedBy = u.Id
            WHERE rb.DeletedBy = @UserId
            ORDER BY rb.DeletedAt DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<RecycleBinItem>> GetAllAsync(int? nodeType = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT rb.*, u.DisplayName as DeletedByUserName
            FROM RecycleBin rb
            LEFT JOIN Users u ON rb.DeletedBy = u.Id
            WHERE 1=1";

        if (nodeType.HasValue)
            sql += " AND rb.NodeType = @NodeType";

        sql += " ORDER BY rb.DeletedAt DESC";

        return await connection.QueryAsync<RecycleBinItem>(sql, new { NodeType = nodeType });
    }

    public async Task<RecycleBinItem?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<RecycleBinItem>(@"
            SELECT rb.*, u.DisplayName as DeletedByUserName
            FROM RecycleBin rb
            LEFT JOIN Users u ON rb.DeletedBy = u.Id
            WHERE rb.Id = @Id",
            new { Id = id });
    }

    public async Task<Guid> AddAsync(RecycleBinItem entity)
    {
        entity.Id = Guid.NewGuid();
        entity.DeletedAt = DateTime.UtcNow;
        entity.ExpiresAt = DateTime.UtcNow.AddDays(30); // 30 days retention

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO RecycleBin (Id, NodeType, NodeId, NodeName, OriginalPath,
                OriginalParentId, DeletedBy, DeletedAt, ExpiresAt, Metadata)
            VALUES (@Id, @NodeType, @NodeId, @NodeName, @OriginalPath,
                @OriginalParentId, @DeletedBy, @DeletedAt, @ExpiresAt, @Metadata)",
            entity);

        return entity.Id;
    }

    public async Task<bool> RemoveAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM RecycleBin WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }

    public async Task<bool> PurgeExpiredAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM RecycleBin WHERE ExpiresAt < GETUTCDATE()");
        return affected > 0;
    }
}
