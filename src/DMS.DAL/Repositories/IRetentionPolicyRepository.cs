using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IRetentionPolicyRepository
{
    // Retention Policies
    Task<IEnumerable<RetentionPolicy>> GetAllAsync(bool includeInactive = false);
    Task<RetentionPolicy?> GetByIdAsync(Guid id);
    Task<IEnumerable<RetentionPolicy>> GetByFolderAsync(Guid folderId);
    Task<RetentionPolicy?> GetApplicablePolicyAsync(Guid? folderId, Guid? classificationId, Guid? documentTypeId);
    Task<Guid> CreateAsync(RetentionPolicy policy);
    Task<bool> UpdateAsync(RetentionPolicy policy);
    Task<bool> DeleteAsync(Guid id);

    // Document Retention
    Task<IEnumerable<DocumentRetention>> GetDocumentRetentionsAsync(Guid documentId);
    Task<IEnumerable<DocumentRetention>> GetExpiringDocumentsAsync(int daysAhead = 30);
    Task<IEnumerable<DocumentRetention>> GetPendingReviewAsync();
    Task<(List<DocumentRetention> Items, int TotalCount)> GetExpiringDocumentsPaginatedAsync(int daysAhead, int page, int pageSize);
    Task<(List<DocumentRetention> Items, int TotalCount)> GetPendingReviewPaginatedAsync(int page, int pageSize);
    Task<Guid> CreateDocumentRetentionAsync(DocumentRetention retention);
    Task<bool> UpdateDocumentRetentionAsync(DocumentRetention retention);
    Task<bool> ApplyPolicyToDocumentAsync(Guid documentId, Guid policyId, Guid userId);
    Task<bool> ApproveRetentionActionAsync(Guid retentionId, Guid userId, string? notes = null);
    Task<bool> PlaceOnHoldAsync(Guid documentId, Guid userId, string? notes = null);
    Task<bool> ReleaseHoldAsync(Guid documentId, Guid userId);
}
