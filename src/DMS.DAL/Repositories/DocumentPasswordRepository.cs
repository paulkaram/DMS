using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DocumentPasswordRepository : IDocumentPasswordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DocumentPasswordRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DocumentPassword?> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentPassword>(
            "SELECT * FROM DocumentPasswords WHERE DocumentId = @DocumentId AND IsActive = 1",
            new { DocumentId = documentId });
    }

    public async Task<DocumentPassword?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DocumentPassword>(
            "SELECT * FROM DocumentPasswords WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<Guid> AddAsync(DocumentPassword password)
    {
        password.Id = Guid.NewGuid();
        password.CreatedAt = DateTime.UtcNow;
        password.IsActive = true;

        using var connection = _connectionFactory.CreateConnection();

        // Deactivate any existing password
        await connection.ExecuteAsync(
            "UPDATE DocumentPasswords SET IsActive = 0 WHERE DocumentId = @DocumentId",
            new { password.DocumentId });

        await connection.ExecuteAsync(@"
            INSERT INTO DocumentPasswords (Id, DocumentId, PasswordHash, Hint, ExpiresAt, IsActive, CreatedBy, CreatedAt)
            VALUES (@Id, @DocumentId, @PasswordHash, @Hint, @ExpiresAt, @IsActive, @CreatedBy, @CreatedAt)",
            password);

        return password.Id;
    }

    public async Task<bool> UpdateAsync(DocumentPassword password)
    {
        password.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DocumentPasswords
            SET PasswordHash = @PasswordHash, Hint = @Hint, ExpiresAt = @ExpiresAt,
                ModifiedBy = @ModifiedBy, ModifiedAt = @ModifiedAt
            WHERE Id = @Id AND IsActive = 1",
            password);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "UPDATE DocumentPasswords SET IsActive = 0 WHERE DocumentId = @DocumentId",
            new { DocumentId = documentId });

        return affected > 0;
    }

    public async Task<bool> HasPasswordAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM DocumentPasswords
            WHERE DocumentId = @DocumentId AND IsActive = 1
                AND (ExpiresAt IS NULL OR ExpiresAt > @Now)",
            new { DocumentId = documentId, Now = DateTime.UtcNow });

        return count > 0;
    }

    public async Task<bool> ValidatePasswordAsync(Guid documentId, string passwordHash)
    {
        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM DocumentPasswords
            WHERE DocumentId = @DocumentId AND PasswordHash = @PasswordHash AND IsActive = 1
                AND (ExpiresAt IS NULL OR ExpiresAt > @Now)",
            new { DocumentId = documentId, PasswordHash = passwordHash, Now = DateTime.UtcNow });

        return count > 0;
    }

    public async Task<Dictionary<Guid, bool>> GetPasswordStatusBulkAsync(List<Guid> documentIds)
    {
        if (documentIds.Count == 0)
            return new Dictionary<Guid, bool>();

        using var connection = _connectionFactory.CreateConnection();
        var documentsWithPassword = await connection.QueryAsync<Guid>(@"
            SELECT DocumentId FROM DocumentPasswords
            WHERE DocumentId IN @DocumentIds AND IsActive = 1
                AND (ExpiresAt IS NULL OR ExpiresAt > @Now)",
            new { DocumentIds = documentIds, Now = DateTime.UtcNow });

        var passwordSet = new HashSet<Guid>(documentsWithPassword);
        return documentIds.ToDictionary(id => id, id => passwordSet.Contains(id));
    }
}
