using System.Text.RegularExpressions;
using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class PatternRepository : IPatternRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PatternRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<Pattern>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT p.*,
                    f.Name as TargetFolderName,
                    ct.Name as ContentTypeName,
                    c.Name as ClassificationName,
                    dt.Name as DocumentTypeName
                    FROM Patterns p
                    LEFT JOIN Folders f ON p.TargetFolderId = f.Id
                    LEFT JOIN ContentTypeDefinitions ct ON p.ContentTypeId = ct.Id
                    LEFT JOIN Classifications c ON p.ClassificationId = c.Id
                    LEFT JOIN DocumentTypes dt ON p.DocumentTypeId = dt.Id
                    WHERE (@IncludeInactive = 1 OR p.IsActive = 1)
                    ORDER BY p.Priority, p.Name";
        return await connection.QueryAsync<Pattern>(sql, new { IncludeInactive = includeInactive });
    }

    public async Task<Pattern?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT p.*,
                    f.Name as TargetFolderName,
                    ct.Name as ContentTypeName,
                    c.Name as ClassificationName,
                    dt.Name as DocumentTypeName
                    FROM Patterns p
                    LEFT JOIN Folders f ON p.TargetFolderId = f.Id
                    LEFT JOIN ContentTypeDefinitions ct ON p.ContentTypeId = ct.Id
                    LEFT JOIN Classifications c ON p.ClassificationId = c.Id
                    LEFT JOIN DocumentTypes dt ON p.DocumentTypeId = dt.Id
                    WHERE p.Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Pattern>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Pattern>> GetByTypeAsync(string patternType)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT p.*, f.Name as TargetFolderName
                    FROM Patterns p
                    LEFT JOIN Folders f ON p.TargetFolderId = f.Id
                    WHERE p.PatternType = @PatternType AND p.IsActive = 1
                    ORDER BY p.Priority, p.Name";
        return await connection.QueryAsync<Pattern>(sql, new { PatternType = patternType });
    }

    public async Task<IEnumerable<Pattern>> GetByFolderAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT * FROM Patterns
                    WHERE TargetFolderId = @FolderId AND IsActive = 1
                    ORDER BY Priority, Name";
        return await connection.QueryAsync<Pattern>(sql, new { FolderId = folderId });
    }

    public async Task<Pattern?> FindMatchingPatternAsync(string value, string? patternType = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT * FROM Patterns
                    WHERE IsActive = 1
                    AND (@PatternType IS NULL OR PatternType = @PatternType)
                    ORDER BY Priority";

        var patterns = await connection.QueryAsync<Pattern>(sql, new { PatternType = patternType });

        foreach (var pattern in patterns)
        {
            try
            {
                if (Regex.IsMatch(value, pattern.Regex))
                {
                    return pattern;
                }
            }
            catch
            {
                // Invalid regex, skip
            }
        }

        return null;
    }

    public async Task<Guid> CreateAsync(Pattern pattern)
    {
        using var connection = _connectionFactory.CreateConnection();
        pattern.Id = Guid.NewGuid();
        pattern.CreatedAt = DateTime.UtcNow;

        var sql = @"INSERT INTO Patterns
                    (Id, Name, Regex, Description, PatternType, TargetFolderId, ContentTypeId,
                     ClassificationId, DocumentTypeId, Priority, IsActive, CreatedBy, CreatedAt)
                    VALUES (@Id, @Name, @Regex, @Description, @PatternType, @TargetFolderId, @ContentTypeId,
                            @ClassificationId, @DocumentTypeId, @Priority, @IsActive, @CreatedBy, @CreatedAt)";

        await connection.ExecuteAsync(sql, pattern);
        return pattern.Id;
    }

    public async Task<bool> UpdateAsync(Pattern pattern)
    {
        using var connection = _connectionFactory.CreateConnection();
        pattern.ModifiedAt = DateTime.UtcNow;

        var sql = @"UPDATE Patterns SET
                    Name = @Name, Regex = @Regex, Description = @Description, PatternType = @PatternType,
                    TargetFolderId = @TargetFolderId, ContentTypeId = @ContentTypeId, ClassificationId = @ClassificationId,
                    DocumentTypeId = @DocumentTypeId, Priority = @Priority, IsActive = @IsActive,
                    ModifiedBy = @ModifiedBy, ModifiedAt = @ModifiedAt
                    WHERE Id = @Id";

        return await connection.ExecuteAsync(sql, pattern) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "UPDATE Patterns SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id",
            new { Id = id, Now = DateTime.UtcNow }) > 0;
    }

    public Task<bool> TestPatternAsync(string regex, string testValue)
    {
        try
        {
            var result = Regex.IsMatch(testValue, regex);
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
