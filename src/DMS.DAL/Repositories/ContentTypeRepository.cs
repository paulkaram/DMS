using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class ContentTypeRepository : IContentTypeRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ContentTypeRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<ContentType>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ContentType>(
            "SELECT * FROM ContentTypes WHERE IsActive = 1 ORDER BY Extension");
    }

    public async Task<ContentType?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ContentType>(
            "SELECT * FROM ContentTypes WHERE Id = @Id", new { Id = id });
    }

    public async Task<ContentType?> GetByExtensionAsync(string extension)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<ContentType>(
            "SELECT * FROM ContentTypes WHERE Extension = @Extension AND IsActive = 1",
            new { Extension = extension.ToLower() });
    }

    public async Task<Guid> CreateAsync(ContentType entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.Extension = entity.Extension.ToLower();

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO ContentTypes (Id, Extension, MimeType, DisplayName, Icon,
                AllowPreview, AllowThumbnail, MaxFileSizeMB, IsActive, CreatedAt)
            VALUES (@Id, @Extension, @MimeType, @DisplayName, @Icon,
                @AllowPreview, @AllowThumbnail, @MaxFileSizeMB, @IsActive, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(ContentType entity)
    {
        entity.Extension = entity.Extension.ToLower();

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE ContentTypes
            SET Extension = @Extension, MimeType = @MimeType, DisplayName = @DisplayName,
                Icon = @Icon, AllowPreview = @AllowPreview, AllowThumbnail = @AllowThumbnail,
                MaxFileSizeMB = @MaxFileSizeMB, IsActive = @IsActive
            WHERE Id = @Id",
            entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE ContentTypes SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
