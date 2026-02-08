using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class LegalHoldRepository : ILegalHoldRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LegalHoldRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LegalHold?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<LegalHold>(
            "SELECT * FROM LegalHolds WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<IEnumerable<LegalHold>> GetAllAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LegalHold>(
            "SELECT * FROM LegalHolds ORDER BY CreatedAt DESC");
    }

    public async Task<IEnumerable<LegalHold>> GetActiveAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LegalHold>(
            "SELECT * FROM LegalHolds WHERE Status = 'Active' AND IsActive = 1 ORDER BY CreatedAt DESC");
    }

    public async Task<Guid> CreateAsync(LegalHold entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO LegalHolds
                (Id, HoldNumber, Name, Description, CaseReference, RequestedBy, RequestedAt,
                 Status, EffectiveFrom, EffectiveUntil, AppliedBy, AppliedAt, ReleasedBy,
                 ReleasedAt, ReleaseReason, Notes, IsActive, CreatedAt)
            VALUES
                (@Id, @HoldNumber, @Name, @Description, @CaseReference, @RequestedBy, @RequestedAt,
                 @Status, @EffectiveFrom, @EffectiveUntil, @AppliedBy, @AppliedAt, @ReleasedBy,
                 @ReleasedAt, @ReleaseReason, @Notes, @IsActive, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(LegalHold entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE LegalHolds
            SET Name = @Name,
                Description = @Description,
                CaseReference = @CaseReference,
                RequestedBy = @RequestedBy,
                RequestedAt = @RequestedAt,
                Status = @Status,
                EffectiveUntil = @EffectiveUntil,
                ReleasedBy = @ReleasedBy,
                ReleasedAt = @ReleasedAt,
                ReleaseReason = @ReleaseReason,
                Notes = @Notes,
                IsActive = @IsActive,
                ModifiedAt = @ModifiedAt
            WHERE Id = @Id",
            entity);

        return affected > 0;
    }
}

public class LegalHoldDocumentRepository : ILegalHoldDocumentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LegalHoldDocumentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LegalHoldDocument?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<LegalHoldDocument>(
            "SELECT * FROM LegalHoldDocuments WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<IEnumerable<LegalHoldDocument>> GetByHoldIdAsync(Guid holdId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LegalHoldDocument>(@"
            SELECT lhd.*, d.Name as DocumentName, lh.Name as HoldName
            FROM LegalHoldDocuments lhd
            JOIN Documents d ON lhd.DocumentId = d.Id
            JOIN LegalHolds lh ON lhd.LegalHoldId = lh.Id
            WHERE lhd.LegalHoldId = @HoldId
            ORDER BY lhd.AddedAt DESC",
            new { HoldId = holdId });
    }

    public async Task<IEnumerable<LegalHoldDocument>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LegalHoldDocument>(@"
            SELECT lhd.*, d.Name as DocumentName, lh.Name as HoldName
            FROM LegalHoldDocuments lhd
            JOIN Documents d ON lhd.DocumentId = d.Id
            JOIN LegalHolds lh ON lhd.LegalHoldId = lh.Id
            WHERE lhd.DocumentId = @DocumentId
            ORDER BY lhd.AddedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<LegalHoldDocument>> GetActiveByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<LegalHoldDocument>(@"
            SELECT lhd.*, d.Name as DocumentName, lh.Name as HoldName
            FROM LegalHoldDocuments lhd
            JOIN Documents d ON lhd.DocumentId = d.Id
            JOIN LegalHolds lh ON lhd.LegalHoldId = lh.Id
            WHERE lhd.DocumentId = @DocumentId
              AND lhd.ReleasedAt IS NULL
              AND lh.Status = 'Active'
            ORDER BY lhd.AddedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<bool> IsDocumentOnHoldAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1)
            FROM LegalHoldDocuments lhd
            JOIN LegalHolds lh ON lhd.LegalHoldId = lh.Id
            WHERE lhd.DocumentId = @DocumentId
              AND lhd.ReleasedAt IS NULL
              AND lh.Status = 'Active'",
            new { DocumentId = documentId });

        return count > 0;
    }

    public async Task<Guid> CreateAsync(LegalHoldDocument entity)
    {
        entity.Id = Guid.NewGuid();

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO LegalHoldDocuments
                (Id, LegalHoldId, DocumentId, AddedAt, AddedBy, ReleasedAt, ReleasedBy, Notes)
            VALUES
                (@Id, @LegalHoldId, @DocumentId, @AddedAt, @AddedBy, @ReleasedAt, @ReleasedBy, @Notes)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(LegalHoldDocument entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE LegalHoldDocuments
            SET ReleasedAt = @ReleasedAt,
                ReleasedBy = @ReleasedBy,
                Notes = @Notes
            WHERE Id = @Id",
            entity);

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(
            "DELETE FROM LegalHoldDocuments WHERE Id = @Id",
            new { Id = id });

        return affected > 0;
    }
}
