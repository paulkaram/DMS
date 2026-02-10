using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ActivityLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ActivityLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int skip = 0, int take = 50)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ActivityLog>(@"
            SELECT a.*, COALESCE(a.UserName, u.DisplayName, u.Username) AS UserName
            FROM ActivityLogs a
            LEFT JOIN Users u ON a.UserId = u.Id
            WHERE a.NodeType = @NodeType AND a.NodeId = @NodeId
            ORDER BY a.CreatedAt DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { NodeType = (int)nodeType, NodeId = nodeId, Skip = skip, Take = take });
    }

    public async Task<IEnumerable<ActivityLog>> GetByUserAsync(Guid userId, int skip = 0, int take = 50)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ActivityLog>(@"
            SELECT a.*, COALESCE(a.UserName, u.DisplayName, u.Username) AS UserName
            FROM ActivityLogs a
            LEFT JOIN Users u ON a.UserId = u.Id
            WHERE a.UserId = @UserId
            ORDER BY a.CreatedAt DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            new { UserId = userId, Skip = skip, Take = take });
    }

    public async Task<IEnumerable<ActivityLog>> GetRecentAsync(int take = 100)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ActivityLog>(@"
            SELECT TOP (@Take) a.*, COALESCE(a.UserName, u.DisplayName, u.Username) AS UserName
            FROM ActivityLogs a
            LEFT JOIN Users u ON a.UserId = u.Id
            ORDER BY a.CreatedAt DESC",
            new { Take = take });
    }

    public async Task<Guid> CreateAsync(ActivityLog entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO ActivityLogs (Id, Action, NodeType, NodeId, NodeName, Details, UserId, UserName, IpAddress, CreatedAt)
            VALUES (@Id, @Action, @NodeType, @NodeId, @NodeName, @Details, @UserId, @UserName, @IpAddress, @CreatedAt)",
            new
            {
                entity.Id,
                entity.Action,
                NodeType = entity.NodeType.HasValue ? (int?)entity.NodeType : null,
                entity.NodeId,
                entity.NodeName,
                entity.Details,
                entity.UserId,
                entity.UserName,
                entity.IpAddress,
                entity.CreatedAt
            });

        return entity.Id;
    }
}
