using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username AND IsActive = 1", new { Username = username });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<User>(
            "SELECT * FROM Users WHERE IsActive = 1 ORDER BY DisplayName, Username");
    }

    public async Task<IEnumerable<User>> SearchAsync(string? search)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM Users WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(search))
            sql += " AND (Username LIKE @Search OR Email LIKE @Search OR DisplayName LIKE @Search)";
        sql += " ORDER BY DisplayName, Username";
        return await connection.QueryAsync<User>(sql, new { Search = $"%{search}%" });
    }

    public async Task<Guid> CreateAsync(User entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Users (Id, Username, Email, FirstName, LastName, DisplayName, IsActive, CreatedAt)
            VALUES (@Id, @Username, @Email, @FirstName, @LastName, @DisplayName, @IsActive, @CreatedAt)", entity);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(User entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Users SET Email = @Email, FirstName = @FirstName, LastName = @LastName,
            DisplayName = @DisplayName, IsActive = @IsActive, ModifiedAt = @ModifiedAt WHERE Id = @Id", entity);
        return affected > 0;
    }

    public async Task<bool> UpdateLastLoginAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE Users SET LastLoginAt = @LastLoginAt WHERE Id = @Id",
            new { Id = id, LastLoginAt = DateTime.UtcNow });
        return affected > 0;
    }
}

public class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Role>(
            "SELECT * FROM Roles WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Role>(
            "SELECT * FROM Roles WHERE IsActive = 1 ORDER BY Name");
    }

    public async Task<IEnumerable<Role>> GetByUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Role>(@"
            SELECT r.* FROM Roles r
            INNER JOIN UserRoles ur ON r.Id = ur.RoleId
            WHERE ur.UserId = @UserId AND r.IsActive = 1
            ORDER BY r.Name", new { UserId = userId });
    }

    public async Task<Guid> CreateAsync(Role entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Roles (Id, Name, Description, IsActive, CreatedAt)
            VALUES (@Id, @Name, @Description, @IsActive, @CreatedAt)", entity);
        return entity.Id;
    }

    public async Task<bool> AddUserToRoleAsync(Guid userId, Guid roleId)
    {
        using var connection = _connectionFactory.CreateConnection();
        try
        {
            await connection.ExecuteAsync(@"
                INSERT INTO UserRoles (Id, UserId, RoleId, CreatedAt)
                VALUES (@Id, @UserId, @RoleId, @CreatedAt)",
                new { Id = Guid.NewGuid(), UserId = userId, RoleId = roleId, CreatedAt = DateTime.UtcNow });
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> RemoveUserFromRoleAsync(Guid userId, Guid roleId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId",
            new { UserId = userId, RoleId = roleId });
        return affected > 0;
    }
}

public class DelegationRepository : IDelegationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DelegationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Delegation?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Delegation>(
            "SELECT * FROM Delegations WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Delegation>> GetByFromUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Delegation>(
            "SELECT * FROM Delegations WHERE FromUserId = @UserId AND IsActive = 1 ORDER BY StartDate DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<Delegation>> GetByToUserIdAsync(Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Delegation>(
            "SELECT * FROM Delegations WHERE ToUserId = @UserId AND IsActive = 1 ORDER BY StartDate DESC",
            new { UserId = userId });
    }

    public async Task<IEnumerable<Delegation>> GetActiveAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Delegation>(@"
            SELECT * FROM Delegations WHERE IsActive = 1
            AND StartDate <= @Now AND (EndDate IS NULL OR EndDate >= @Now)
            ORDER BY StartDate DESC", new { Now = DateTime.UtcNow });
    }

    public async Task<Guid> CreateAsync(Delegation entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Delegations (Id, FromUserId, ToUserId, RoleId, StartDate, EndDate, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @FromUserId, @ToUserId, @RoleId, @StartDate, @EndDate, @IsActive, @CreatedBy, @CreatedAt)", entity);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Delegation entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Delegations SET ToUserId = @ToUserId, RoleId = @RoleId,
            StartDate = @StartDate, EndDate = @EndDate, IsActive = @IsActive WHERE Id = @Id", entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE Delegations SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
