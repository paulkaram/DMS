using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class VacationRepository : IVacationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public VacationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Vacation?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Vacation>(@"
            SELECT v.*, u.DisplayName as UserName, d.DisplayName as DelegateToUserName
            FROM Vacations v
            LEFT JOIN Users u ON v.UserId = u.Id
            LEFT JOIN Users d ON v.DelegateToUserId = d.Id
            WHERE v.Id = @Id",
            new { Id = id });
    }

    public async Task<IEnumerable<Vacation>> GetByUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Vacation>(@"
            SELECT v.*, u.DisplayName as UserName, d.DisplayName as DelegateToUserName
            FROM Vacations v
            LEFT JOIN Users u ON v.UserId = u.Id
            LEFT JOIN Users d ON v.DelegateToUserId = d.Id
            WHERE v.UserId = @UserId
            ORDER BY v.StartDate DESC",
            new { UserId = userId });
    }

    public async Task<Vacation?> GetActiveVacationAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Vacation>(@"
            SELECT v.*, u.DisplayName as UserName, d.DisplayName as DelegateToUserName
            FROM Vacations v
            LEFT JOIN Users u ON v.UserId = u.Id
            LEFT JOIN Users d ON v.DelegateToUserId = d.Id
            WHERE v.UserId = @UserId AND v.IsActive = 1
            AND GETUTCDATE() BETWEEN v.StartDate AND v.EndDate",
            new { UserId = userId });
    }

    public async Task<IEnumerable<Vacation>> GetAllActiveAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Vacation>(@"
            SELECT v.*, u.DisplayName as UserName, d.DisplayName as DelegateToUserName
            FROM Vacations v
            LEFT JOIN Users u ON v.UserId = u.Id
            LEFT JOIN Users d ON v.DelegateToUserId = d.Id
            WHERE v.IsActive = 1 AND GETUTCDATE() BETWEEN v.StartDate AND v.EndDate
            ORDER BY v.StartDate");
    }

    public async Task<Guid> CreateAsync(Vacation entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Vacations (Id, UserId, DelegateToUserId, StartDate, EndDate,
                Message, AutoReply, IsActive, CreatedAt)
            VALUES (@Id, @UserId, @DelegateToUserId, @StartDate, @EndDate,
                @Message, @AutoReply, @IsActive, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Vacation entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Vacations
            SET DelegateToUserId = @DelegateToUserId, StartDate = @StartDate,
                EndDate = @EndDate, Message = @Message, AutoReply = @AutoReply,
                IsActive = @IsActive, ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM Vacations WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
