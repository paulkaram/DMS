using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class ClassificationRepository : IClassificationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ClassificationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Classification?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Classification>(
            "SELECT * FROM Classifications WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Classification>> GetAllAsync(string? language = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM Classifications WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(language))
            sql += " AND (Language = @Language OR Language IS NULL)";
        sql += " ORDER BY SortOrder, Name";
        return await connection.QueryAsync<Classification>(sql, new { Language = language });
    }

    public async Task<IEnumerable<Classification>> SearchAsync(string? name, string? language = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM Classifications WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(name))
            sql += " AND Name LIKE @Name";
        if (!string.IsNullOrEmpty(language))
            sql += " AND (Language = @Language OR Language IS NULL)";
        sql += " ORDER BY SortOrder, Name";
        return await connection.QueryAsync<Classification>(sql, new { Name = $"%{name}%", Language = language });
    }

    public async Task<Guid> CreateAsync(Classification entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Classifications (Id, Name, Description, Language, SortOrder, IsActive, CreatedAt)
            VALUES (@Id, @Name, @Description, @Language, @SortOrder, @IsActive, @CreatedAt)", entity);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Classification entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Classifications SET Name = @Name, Description = @Description,
            Language = @Language, SortOrder = @SortOrder, IsActive = @IsActive WHERE Id = @Id", entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE Classifications SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}

public class ImportanceRepository : IImportanceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ImportanceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Importance?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Importance>(
            "SELECT * FROM Importances WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Importance>> GetAllAsync(string? language = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM Importances WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(language))
            sql += " AND (Language = @Language OR Language IS NULL)";
        sql += " ORDER BY Level";
        return await connection.QueryAsync<Importance>(sql, new { Language = language });
    }

    public async Task<Guid> CreateAsync(Importance entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Importances (Id, Name, Level, Color, Language, IsActive, CreatedAt)
            VALUES (@Id, @Name, @Level, @Color, @Language, @IsActive, @CreatedAt)", entity);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Importance entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Importances SET Name = @Name, Level = @Level, Color = @Color,
            Language = @Language, IsActive = @IsActive WHERE Id = @Id", entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE Importances SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}

public class DocumentTypeRepository : IDocumentTypeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentTypeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DocumentType?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentType>(
            "SELECT * FROM DocumentTypes WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<DocumentType>> GetAllAsync(string? language = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM DocumentTypes WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(language))
            sql += " AND (Language = @Language OR Language IS NULL)";
        sql += " ORDER BY Name";
        return await connection.QueryAsync<DocumentType>(sql, new { Language = language });
    }

    public async Task<IEnumerable<DocumentType>> SearchAsync(string? name, string? language = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM DocumentTypes WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(name))
            sql += " AND Name LIKE @Name";
        if (!string.IsNullOrEmpty(language))
            sql += " AND (Language = @Language OR Language IS NULL)";
        sql += " ORDER BY Name";
        return await connection.QueryAsync<DocumentType>(sql, new { Name = $"%{name}%", Language = language });
    }

    public async Task<Guid> CreateAsync(DocumentType entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DocumentTypes (Id, Name, Description, Language, IsActive, CreatedAt)
            VALUES (@Id, @Name, @Description, @Language, @IsActive, @CreatedAt)", entity);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DocumentType entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentTypes SET Name = @Name, Description = @Description,
            Language = @Language, IsActive = @IsActive WHERE Id = @Id", entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE DocumentTypes SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}

public class LookupRepository : ILookupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LookupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Lookup?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Lookup>(
            "SELECT * FROM Lookups WHERE Id = @Id", new { Id = id });
    }

    public async Task<Lookup?> GetByNameAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Lookup>(
            "SELECT * FROM Lookups WHERE Name = @Name AND IsActive = 1", new { Name = name });
    }

    public async Task<IEnumerable<Lookup>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Lookup>(
            "SELECT * FROM Lookups WHERE IsActive = 1 ORDER BY Name");
    }

    public async Task<IEnumerable<LookupItem>> GetItemsByLookupIdAsync(Guid lookupId, string? language = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM LookupItems WHERE LookupId = @LookupId AND IsActive = 1";
        if (!string.IsNullOrEmpty(language))
            sql += " AND (Language = @Language OR Language IS NULL)";
        sql += " ORDER BY SortOrder, DisplayText";
        return await connection.QueryAsync<LookupItem>(sql, new { LookupId = lookupId, Language = language });
    }

    public async Task<IEnumerable<LookupItem>> GetItemsByLookupNameAsync(string lookupName, string? language = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT li.* FROM LookupItems li
            INNER JOIN Lookups l ON li.LookupId = l.Id
            WHERE l.Name = @LookupName AND l.IsActive = 1 AND li.IsActive = 1";
        if (!string.IsNullOrEmpty(language))
            sql += " AND (li.Language = @Language OR li.Language IS NULL)";
        sql += " ORDER BY li.SortOrder, li.DisplayText";
        return await connection.QueryAsync<LookupItem>(sql, new { LookupName = lookupName, Language = language });
    }

    public async Task<Guid> CreateAsync(Lookup entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Lookups (Id, Name, Description, IsActive, CreatedAt)
            VALUES (@Id, @Name, @Description, @IsActive, @CreatedAt)", entity);
        return entity.Id;
    }

    public async Task<Guid> CreateItemAsync(LookupItem entity)
    {
        entity.Id = Guid.NewGuid();
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO LookupItems (Id, LookupId, Value, DisplayText, Language, SortOrder, IsActive)
            VALUES (@Id, @LookupId, @Value, @DisplayText, @Language, @SortOrder, @IsActive)", entity);
        return entity.Id;
    }
}
