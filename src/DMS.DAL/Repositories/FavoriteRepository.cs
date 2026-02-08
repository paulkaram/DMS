using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FavoriteRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Favorite>> GetByUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Favorite>(
            "SELECT * FROM Favorites WHERE UserId = @UserId ORDER BY CreatedAt DESC",
            new { UserId = userId });
    }

    public async Task<Favorite?> GetAsync(Guid userId, int nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Favorite>(
            "SELECT * FROM Favorites WHERE UserId = @UserId AND NodeType = @NodeType AND NodeId = @NodeId",
            new { UserId = userId, NodeType = nodeType, NodeId = nodeId });
    }

    public async Task<bool> IsFavoriteAsync(Guid userId, int nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Favorites WHERE UserId = @UserId AND NodeType = @NodeType AND NodeId = @NodeId",
            new { UserId = userId, NodeType = nodeType, NodeId = nodeId });
        return count > 0;
    }

    public async Task<Guid> AddAsync(Favorite entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Favorites (Id, UserId, NodeType, NodeId, CreatedAt)
            VALUES (@Id, @UserId, @NodeType, @NodeId, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> RemoveAsync(Guid userId, int nodeType, Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM Favorites WHERE UserId = @UserId AND NodeType = @NodeType AND NodeId = @NodeId",
            new { UserId = userId, NodeType = nodeType, NodeId = nodeId });
        return affected > 0;
    }
}
