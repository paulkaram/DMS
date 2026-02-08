using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class FilingPlanRepository : IFilingPlanRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FilingPlanRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<FilingPlan>> GetByFolderAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<FilingPlan>(@"
            SELECT fp.*, f.Name as FolderName, c.Name as ClassificationName, dt.Name as DocumentTypeName
            FROM FilingPlans fp
            LEFT JOIN Folders f ON fp.FolderId = f.Id
            LEFT JOIN Classifications c ON fp.ClassificationId = c.Id
            LEFT JOIN DocumentTypes dt ON fp.DocumentTypeId = dt.Id
            WHERE fp.FolderId = @FolderId AND fp.IsActive = 1
            ORDER BY fp.Name",
            new { FolderId = folderId });
    }

    public async Task<FilingPlan?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<FilingPlan>(@"
            SELECT fp.*, f.Name as FolderName, c.Name as ClassificationName, dt.Name as DocumentTypeName
            FROM FilingPlans fp
            LEFT JOIN Folders f ON fp.FolderId = f.Id
            LEFT JOIN Classifications c ON fp.ClassificationId = c.Id
            LEFT JOIN DocumentTypes dt ON fp.DocumentTypeId = dt.Id
            WHERE fp.Id = @Id",
            new { Id = id });
    }

    public async Task<Guid> CreateAsync(FilingPlan entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO FilingPlans (Id, FolderId, Name, Description, Pattern,
                ClassificationId, DocumentTypeId, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @FolderId, @Name, @Description, @Pattern,
                @ClassificationId, @DocumentTypeId, @IsActive, @CreatedBy, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(FilingPlan entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE FilingPlans
            SET Name = @Name, Description = @Description, Pattern = @Pattern,
                ClassificationId = @ClassificationId, DocumentTypeId = @DocumentTypeId,
                IsActive = @IsActive
            WHERE Id = @Id",
            entity);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE FilingPlans SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return affected > 0;
    }
}
