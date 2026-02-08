using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class FolderRepository : IFolderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FolderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Folder?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Folder>(
            "SELECT * FROM Folders WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<Folder>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Folder>(
            "SELECT * FROM Folders WHERE IsActive = 1 ORDER BY Path, Name");
    }

    public async Task<IEnumerable<Folder>> GetByCabinetIdAsync(Guid cabinetId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Folder>(
            "SELECT * FROM Folders WHERE CabinetId = @CabinetId AND IsActive = 1 ORDER BY Path, Name",
            new { CabinetId = cabinetId });
    }

    public async Task<IEnumerable<Folder>> GetByParentIdAsync(Guid? parentId, Guid cabinetId)
    {
        using var connection = _connectionFactory.CreateConnection();
        if (parentId == null)
        {
            return await connection.QueryAsync<Folder>(
                "SELECT * FROM Folders WHERE CabinetId = @CabinetId AND ParentFolderId IS NULL AND IsActive = 1 ORDER BY Name",
                new { CabinetId = cabinetId });
        }
        return await connection.QueryAsync<Folder>(
            "SELECT * FROM Folders WHERE ParentFolderId = @ParentId AND IsActive = 1 ORDER BY Name",
            new { ParentId = parentId });
    }

    public async Task<IEnumerable<Folder>> GetTreeAsync(Guid cabinetId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Folder>(@"
            WITH FolderTree AS (
                SELECT *, 0 AS Level
                FROM Folders
                WHERE CabinetId = @CabinetId AND ParentFolderId IS NULL AND IsActive = 1
                UNION ALL
                SELECT f.*, ft.Level + 1
                FROM Folders f
                INNER JOIN FolderTree ft ON f.ParentFolderId = ft.Id
                WHERE f.IsActive = 1
            )
            SELECT * FROM FolderTree ORDER BY Path, Name",
            new { CabinetId = cabinetId });
    }

    public async Task<IEnumerable<Folder>> SearchAsync(string? name, Guid? cabinetId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM Folders WHERE IsActive = 1";
        if (!string.IsNullOrEmpty(name))
            sql += " AND Name LIKE @Name";
        if (cabinetId.HasValue)
            sql += " AND CabinetId = @CabinetId";
        sql += " ORDER BY Path, Name";

        return await connection.QueryAsync<Folder>(sql, new { Name = $"%{name}%", CabinetId = cabinetId });
    }

    public async Task<string> GetPathAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var folder = await GetByIdAsync(folderId);
        if (folder == null) return string.Empty;

        var path = folder.Name;
        var parentId = folder.ParentFolderId;

        while (parentId.HasValue)
        {
            var parent = await connection.QueryFirstOrDefaultAsync<Folder>(
                "SELECT * FROM Folders WHERE Id = @Id", new { Id = parentId });
            if (parent == null) break;
            path = parent.Name + "/" + path;
            parentId = parent.ParentFolderId;
        }

        return path;
    }

    public async Task UpdatePathsAsync(Guid folderId, string newPath)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            UPDATE Folders SET Path = @Path, ModifiedAt = @ModifiedAt WHERE Id = @Id",
            new { Id = folderId, Path = newPath, ModifiedAt = DateTime.UtcNow });
    }

    public async Task<Guid> CreateAsync(Folder entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.Path = await GetPathAsync(entity.ParentFolderId ?? Guid.Empty);
        if (!string.IsNullOrEmpty(entity.Path))
            entity.Path += "/" + entity.Name;
        else
            entity.Path = entity.Name;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO Folders (Id, CabinetId, ParentFolderId, Name, Description, Path, BreakInheritance, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @CabinetId, @ParentFolderId, @Name, @Description, @Path, @BreakInheritance, @IsActive, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Folder entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE Folders
            SET Name = @Name, Description = @Description, ParentFolderId = @ParentFolderId,
                Path = @Path, BreakInheritance = @BreakInheritance, ModifiedBy = @ModifiedBy, ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            entity);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE Folders SET IsActive = 0, ModifiedAt = @ModifiedAt WHERE Id = @Id",
            new { Id = id, ModifiedAt = DateTime.UtcNow });

        return affected > 0;
    }
}
