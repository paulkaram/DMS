using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class RetentionPolicyRepository : IRetentionPolicyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RetentionPolicyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    #region Retention Policies

    public async Task<IEnumerable<RetentionPolicy>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT rp.*,
                    f.Name as FolderName,
                    c.Name as ClassificationName,
                    dt.Name as DocumentTypeName
                    FROM RetentionPolicies rp
                    LEFT JOIN Folders f ON rp.FolderId = f.Id
                    LEFT JOIN Classifications c ON rp.ClassificationId = c.Id
                    LEFT JOIN DocumentTypes dt ON rp.DocumentTypeId = dt.Id
                    WHERE (@IncludeInactive = 1 OR rp.IsActive = 1)
                    ORDER BY rp.Name";
        return await connection.QueryAsync<RetentionPolicy>(sql, new { IncludeInactive = includeInactive });
    }

    public async Task<RetentionPolicy?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT rp.*,
                    f.Name as FolderName,
                    c.Name as ClassificationName,
                    dt.Name as DocumentTypeName
                    FROM RetentionPolicies rp
                    LEFT JOIN Folders f ON rp.FolderId = f.Id
                    LEFT JOIN Classifications c ON rp.ClassificationId = c.Id
                    LEFT JOIN DocumentTypes dt ON rp.DocumentTypeId = dt.Id
                    WHERE rp.Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<RetentionPolicy>(sql, new { Id = id });
    }

    public async Task<IEnumerable<RetentionPolicy>> GetByFolderAsync(Guid folderId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT * FROM RetentionPolicies
                    WHERE FolderId = @FolderId AND IsActive = 1
                    ORDER BY Name";
        return await connection.QueryAsync<RetentionPolicy>(sql, new { FolderId = folderId });
    }

    public async Task<RetentionPolicy?> GetApplicablePolicyAsync(Guid? folderId, Guid? classificationId, Guid? documentTypeId)
    {
        using var connection = _connectionFactory.CreateConnection();
        // Find the most specific applicable policy
        var sql = @"SELECT TOP 1 * FROM RetentionPolicies
                    WHERE IsActive = 1
                    AND (
                        (FolderId = @FolderId) OR
                        (ClassificationId = @ClassificationId) OR
                        (DocumentTypeId = @DocumentTypeId) OR
                        (FolderId IS NULL AND ClassificationId IS NULL AND DocumentTypeId IS NULL)
                    )
                    ORDER BY
                        CASE WHEN FolderId = @FolderId THEN 0 ELSE 1 END,
                        CASE WHEN ClassificationId = @ClassificationId THEN 0 ELSE 1 END,
                        CASE WHEN DocumentTypeId = @DocumentTypeId THEN 0 ELSE 1 END";

        return await connection.QueryFirstOrDefaultAsync<RetentionPolicy>(sql, new { FolderId = folderId, ClassificationId = classificationId, DocumentTypeId = documentTypeId });
    }

    public async Task<Guid> CreateAsync(RetentionPolicy policy)
    {
        using var connection = _connectionFactory.CreateConnection();
        policy.Id = Guid.NewGuid();
        policy.CreatedAt = DateTime.UtcNow;

        var sql = @"INSERT INTO RetentionPolicies
                    (Id, Name, Description, RetentionDays, ExpirationAction, NotifyBeforeExpiration,
                     NotificationDays, FolderId, ClassificationId, DocumentTypeId, RequiresApproval,
                     InheritToSubfolders, IsLegalHold, IsActive, CreatedBy, CreatedAt)
                    VALUES (@Id, @Name, @Description, @RetentionDays, @ExpirationAction, @NotifyBeforeExpiration,
                            @NotificationDays, @FolderId, @ClassificationId, @DocumentTypeId, @RequiresApproval,
                            @InheritToSubfolders, @IsLegalHold, @IsActive, @CreatedBy, @CreatedAt)";

        await connection.ExecuteAsync(sql, policy);
        return policy.Id;
    }

    public async Task<bool> UpdateAsync(RetentionPolicy policy)
    {
        using var connection = _connectionFactory.CreateConnection();
        policy.ModifiedAt = DateTime.UtcNow;

        var sql = @"UPDATE RetentionPolicies SET
                    Name = @Name, Description = @Description, RetentionDays = @RetentionDays,
                    ExpirationAction = @ExpirationAction, NotifyBeforeExpiration = @NotifyBeforeExpiration,
                    NotificationDays = @NotificationDays, FolderId = @FolderId, ClassificationId = @ClassificationId,
                    DocumentTypeId = @DocumentTypeId, RequiresApproval = @RequiresApproval,
                    InheritToSubfolders = @InheritToSubfolders, IsLegalHold = @IsLegalHold, IsActive = @IsActive,
                    ModifiedBy = @ModifiedBy, ModifiedAt = @ModifiedAt
                    WHERE Id = @Id";

        return await connection.ExecuteAsync(sql, policy) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "UPDATE RetentionPolicies SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id",
            new { Id = id, Now = DateTime.UtcNow }) > 0;
    }

    #endregion

    #region Document Retention

    public async Task<IEnumerable<DocumentRetention>> GetDocumentRetentionsAsync(Guid documentId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT dr.*, d.Name as DocumentName, rp.Name as PolicyName
                    FROM DocumentRetentions dr
                    INNER JOIN Documents d ON dr.DocumentId = d.Id
                    INNER JOIN RetentionPolicies rp ON dr.PolicyId = rp.Id
                    WHERE dr.DocumentId = @DocumentId";
        return await connection.QueryAsync<DocumentRetention>(sql, new { DocumentId = documentId });
    }

    public async Task<IEnumerable<DocumentRetention>> GetExpiringDocumentsAsync(int daysAhead = 30)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT dr.*, d.Name as DocumentName, rp.Name as PolicyName
                    FROM DocumentRetentions dr
                    INNER JOIN Documents d ON dr.DocumentId = d.Id
                    INNER JOIN RetentionPolicies rp ON dr.PolicyId = rp.Id
                    WHERE dr.Status = 'Active'
                    AND dr.ExpirationDate IS NOT NULL
                    AND dr.ExpirationDate <= DATEADD(day, @DaysAhead, GETUTCDATE())
                    ORDER BY dr.ExpirationDate";
        return await connection.QueryAsync<DocumentRetention>(sql, new { DaysAhead = daysAhead });
    }

    public async Task<IEnumerable<DocumentRetention>> GetPendingReviewAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"SELECT dr.*, d.Name as DocumentName, rp.Name as PolicyName
                    FROM DocumentRetentions dr
                    INNER JOIN Documents d ON dr.DocumentId = d.Id
                    INNER JOIN RetentionPolicies rp ON dr.PolicyId = rp.Id
                    WHERE dr.Status = 'PendingReview'
                    ORDER BY dr.ExpirationDate";
        return await connection.QueryAsync<DocumentRetention>(sql);
    }

    public async Task<Guid> CreateDocumentRetentionAsync(DocumentRetention retention)
    {
        using var connection = _connectionFactory.CreateConnection();
        retention.Id = Guid.NewGuid();
        retention.CreatedAt = DateTime.UtcNow;

        var sql = @"INSERT INTO DocumentRetentions
                    (Id, DocumentId, PolicyId, RetentionStartDate, ExpirationDate, Status,
                     NotificationSent, Notes, CreatedAt)
                    VALUES (@Id, @DocumentId, @PolicyId, @RetentionStartDate, @ExpirationDate, @Status,
                            @NotificationSent, @Notes, @CreatedAt)";

        await connection.ExecuteAsync(sql, retention);
        return retention.Id;
    }

    public async Task<bool> UpdateDocumentRetentionAsync(DocumentRetention retention)
    {
        using var connection = _connectionFactory.CreateConnection();
        retention.ModifiedAt = DateTime.UtcNow;

        var sql = @"UPDATE DocumentRetentions SET
                    Status = @Status, NotificationSent = @NotificationSent, ActionDate = @ActionDate,
                    ApprovedBy = @ApprovedBy, Notes = @Notes, ModifiedAt = @ModifiedAt
                    WHERE Id = @Id";

        return await connection.ExecuteAsync(sql, retention) > 0;
    }

    public async Task<bool> ApplyPolicyToDocumentAsync(Guid documentId, Guid policyId, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // Get the policy to calculate expiration
        var policy = await GetByIdAsync(policyId);
        if (policy == null) return false;

        var retention = new DocumentRetention
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            PolicyId = policyId,
            RetentionStartDate = DateTime.UtcNow,
            ExpirationDate = policy.RetentionDays > 0
                ? DateTime.UtcNow.AddDays(policy.RetentionDays)
                : null,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        var sql = @"INSERT INTO DocumentRetentions
                    (Id, DocumentId, PolicyId, RetentionStartDate, ExpirationDate, Status, CreatedAt)
                    VALUES (@Id, @DocumentId, @PolicyId, @RetentionStartDate, @ExpirationDate, @Status, @CreatedAt)";

        return await connection.ExecuteAsync(sql, retention) > 0;
    }

    public async Task<bool> ApproveRetentionActionAsync(Guid retentionId, Guid userId, string? notes = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"UPDATE DocumentRetentions SET
                    Status = 'Approved', ApprovedBy = @UserId, ActionDate = @Now, Notes = @Notes, ModifiedAt = @Now
                    WHERE Id = @Id AND Status = 'PendingReview'";

        return await connection.ExecuteAsync(sql, new { Id = retentionId, UserId = userId, Notes = notes, Now = DateTime.UtcNow }) > 0;
    }

    public async Task<bool> PlaceOnHoldAsync(Guid documentId, Guid userId, string? notes = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"UPDATE DocumentRetentions SET
                    Status = 'OnHold', Notes = CONCAT(Notes, ' | Legal hold placed by ', @UserId, ' on ', @Now, ': ', @Notes),
                    ModifiedAt = @Now
                    WHERE DocumentId = @DocumentId AND Status IN ('Active', 'PendingReview')";

        return await connection.ExecuteAsync(sql, new { DocumentId = documentId, UserId = userId, Notes = notes, Now = DateTime.UtcNow }) > 0;
    }

    public async Task<bool> ReleaseHoldAsync(Guid documentId, Guid userId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"UPDATE DocumentRetentions SET
                    Status = 'Active', Notes = CONCAT(Notes, ' | Hold released by ', @UserId, ' on ', @Now),
                    ModifiedAt = @Now
                    WHERE DocumentId = @DocumentId AND Status = 'OnHold'";

        return await connection.ExecuteAsync(sql, new { DocumentId = documentId, UserId = userId, Now = DateTime.UtcNow }) > 0;
    }

    #endregion
}
