using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class CabinetRepository : ICabinetRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CabinetRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Cabinet?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Cabinet>(
            "SELECT * FROM Cabinets WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Cabinet>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Cabinet>(
            "SELECT * FROM Cabinets ORDER BY Name");
    }

    public async Task<IEnumerable<Cabinet>> GetActiveAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Cabinet>(
            "SELECT * FROM Cabinets WHERE IsActive = 1 ORDER BY Name");
    }

    public async Task<IEnumerable<Cabinet>> SearchAsync(string? name)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM Cabinets WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(name))
        {
            sql += " AND Name LIKE @Name";
        }
        sql += " ORDER BY Name";
        return await connection.QueryAsync<Cabinet>(sql, new { Name = $"%{name}%" });
    }

    public async Task<Guid> CreateAsync(Cabinet entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Cabinets (Id, Name, Description, BreakInheritance, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @Name, @Description, @BreakInheritance, @IsActive, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Cabinet entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Cabinets
            SET Name = @Name, Description = @Description, BreakInheritance = @BreakInheritance,
                IsActive = @IsActive, ModifiedBy = @ModifiedBy, ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            entity);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE Cabinets SET IsActive = 0, ModifiedAt = @ModifiedAt WHERE Id = @Id",
            new { Id = id, ModifiedAt = DateTime.UtcNow });

        return affected > 0;
    }
}
