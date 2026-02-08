using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class IntegrityVerificationLogRepository : IIntegrityVerificationLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IntegrityVerificationLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IntegrityVerificationLog?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<IntegrityVerificationLog>(
            "SELECT * FROM IntegrityVerificationLogs WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<IEnumerable<IntegrityVerificationLog>> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<IntegrityVerificationLog>(
            "SELECT * FROM IntegrityVerificationLogs WHERE DocumentId = @DocumentId ORDER BY VerifiedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<IntegrityVerificationLog>> GetFailuresAsync(DateTime? since = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT * FROM IntegrityVerificationLogs WHERE IsValid = 0";
        if (since.HasValue)
        {
            sql += " AND VerifiedAt >= @Since";
        }
        sql += " ORDER BY VerifiedAt DESC";

        return await connection.QueryAsync<IntegrityVerificationLog>(sql, new { Since = since });
    }

    public async Task<Guid> CreateAsync(IntegrityVerificationLog entity)
    {
        entity.Id = Guid.NewGuid();

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO IntegrityVerificationLogs
                (Id, DocumentId, VersionNumber, ExpectedHash, ComputedHash, HashAlgorithm,
                 IsValid, VerifiedAt, VerificationType, VerifiedBy, ErrorMessage, ActionTaken)
            VALUES
                (@Id, @DocumentId, @VersionNumber, @ExpectedHash, @ComputedHash, @HashAlgorithm,
                 @IsValid, @VerifiedAt, @VerificationType, @VerifiedBy, @ErrorMessage, @ActionTaken)",
            entity);

        return entity.Id;
    }
}
