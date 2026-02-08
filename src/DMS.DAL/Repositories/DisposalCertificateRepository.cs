using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class DisposalCertificateRepository : IDisposalCertificateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DisposalCertificateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DisposalCertificate?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DisposalCertificate>(
            "SELECT * FROM DisposalCertificates WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<DisposalCertificate?> GetByDocumentIdAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<DisposalCertificate>(
            "SELECT * FROM DisposalCertificates WHERE DocumentId = @DocumentId ORDER BY DisposedAt DESC",
            new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DisposalCertificate>> GetAllAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var connection = _connectionFactory.CreateConnection();

        var sql = "SELECT * FROM DisposalCertificates WHERE 1=1";
        if (fromDate.HasValue)
            sql += " AND DisposedAt >= @FromDate";
        if (toDate.HasValue)
            sql += " AND DisposedAt <= @ToDate";
        sql += " ORDER BY DisposedAt DESC";

        return await connection.QueryAsync<DisposalCertificate>(sql, new { FromDate = fromDate, ToDate = toDate });
    }

    public async Task<Guid> CreateAsync(DisposalCertificate entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(@"
            INSERT INTO DisposalCertificates
                (Id, CertificateNumber, DocumentId, DocumentName, DocumentPath, Classification,
                 RetentionPolicyId, RetentionPolicyName, DocumentCreatedAt, RetentionStartDate,
                 RetentionExpirationDate, DisposalMethod, DisposedAt, DisposedBy, DisposedByName,
                 ApprovedBy, ApprovedByName, ApprovedAt, LegalBasis, Notes, ContentHashAtDisposal,
                 FileSizeAtDisposal, VersionsDisposed, CertificateSignature, DisposalVerified,
                 VerifiedAt, CreatedAt)
            VALUES
                (@Id, @CertificateNumber, @DocumentId, @DocumentName, @DocumentPath, @Classification,
                 @RetentionPolicyId, @RetentionPolicyName, @DocumentCreatedAt, @RetentionStartDate,
                 @RetentionExpirationDate, @DisposalMethod, @DisposedAt, @DisposedBy, @DisposedByName,
                 @ApprovedBy, @ApprovedByName, @ApprovedAt, @LegalBasis, @Notes, @ContentHashAtDisposal,
                 @FileSizeAtDisposal, @VersionsDisposed, @CertificateSignature, @DisposalVerified,
                 @VerifiedAt, @CreatedAt)",
            entity);

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(DisposalCertificate entity)
    {
        using var connection = _connectionFactory.CreateConnection();
        var affected = await connection.ExecuteAsync(@"
            UPDATE DisposalCertificates
            SET DisposalVerified = @DisposalVerified,
                VerifiedAt = @VerifiedAt,
                CertificateSignature = @CertificateSignature
            WHERE Id = @Id",
            entity);

        return affected > 0;
    }

    public async Task<string> GenerateCertificateNumberAsync()
    {
        using var connection = _connectionFactory.CreateConnection();

        // Generate format: DC-YYYYMMDD-XXXX (where XXXX is sequential for the day)
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM DisposalCertificates WHERE CertificateNumber LIKE @Pattern",
            new { Pattern = $"DC-{today}%" });

        return $"DC-{today}-{(count + 1):D4}";
    }
}
