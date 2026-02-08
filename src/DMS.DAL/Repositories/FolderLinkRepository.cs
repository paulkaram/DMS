using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class FolderLinkRepository : IFolderLinkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FolderLinkRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<FolderLink>> GetBySourceFolderAsync(Guid sourceFolderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<FolderLink>(@"
            SELECT fl.*, sf.Name as SourceFolderName, tf.Name as TargetFolderName, tf.Path as TargetFolderPath
            FROM FolderLinks fl
            LEFT JOIN Folders sf ON fl.SourceFolderId = sf.Id
            LEFT JOIN Folders tf ON fl.TargetFolderId = tf.Id
            WHERE fl.SourceFolderId = @SourceFolderId
            ORDER BY tf.Name",
            new { SourceFolderId = sourceFolderId });
    }

    public async Task<IEnumerable<FolderLink>> GetByTargetFolderAsync(Guid targetFolderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<FolderLink>(@"
            SELECT fl.*, sf.Name as SourceFolderName, tf.Name as TargetFolderName, tf.Path as TargetFolderPath
            FROM FolderLinks fl
            LEFT JOIN Folders sf ON fl.SourceFolderId = sf.Id
            LEFT JOIN Folders tf ON fl.TargetFolderId = tf.Id
            WHERE fl.TargetFolderId = @TargetFolderId
            ORDER BY sf.Name",
            new { TargetFolderId = targetFolderId });
    }

    public async Task<Guid> CreateAsync(FolderLink entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO FolderLinks (Id, SourceFolderId, TargetFolderId, CreatedBy, CreatedAt)
            VALUES (@Id, @SourceFolderId, @TargetFolderId, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM FolderLinks WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
