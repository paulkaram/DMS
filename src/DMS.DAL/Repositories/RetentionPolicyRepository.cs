using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class RetentionPolicyRepository : IRetentionPolicyRepository
{
    private readonly DmsDbContext _context;

    public RetentionPolicyRepository(DmsDbContext context)
    {
        _context = context;
    }

    #region Retention Policies

    public async Task<IEnumerable<RetentionPolicy>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.RetentionPolicies.IgnoreQueryFilters()
            : _context.RetentionPolicies.AsQueryable();

        return await query
            .AsNoTracking()
            .GroupJoin(_context.Folders.AsNoTracking(), rp => rp.FolderId, f => f.Id, (rp, folders) => new { rp, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.rp, f })
            .GroupJoin(_context.Classifications.AsNoTracking(), x => x.rp.ClassificationId, c => c.Id, (x, cs) => new { x.rp, x.f, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.rp, x.f, c })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.rp.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.rp, x.f, x.c, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new RetentionPolicy
            {
                Id = x.rp.Id,
                Name = x.rp.Name,
                Description = x.rp.Description,
                RetentionDays = x.rp.RetentionDays,
                ExpirationAction = x.rp.ExpirationAction,
                NotifyBeforeExpiration = x.rp.NotifyBeforeExpiration,
                NotificationDays = x.rp.NotificationDays,
                FolderId = x.rp.FolderId,
                ClassificationId = x.rp.ClassificationId,
                DocumentTypeId = x.rp.DocumentTypeId,
                RequiresApproval = x.rp.RequiresApproval,
                InheritToSubfolders = x.rp.InheritToSubfolders,
                IsLegalHold = x.rp.IsLegalHold,
                IsActive = x.rp.IsActive,
                CreatedBy = x.rp.CreatedBy,
                CreatedAt = x.rp.CreatedAt,
                ModifiedBy = x.rp.ModifiedBy,
                ModifiedAt = x.rp.ModifiedAt,
                FolderName = x.f != null ? x.f.Name : null,
                ClassificationName = x.c != null ? x.c.Name : null,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .OrderBy(rp => rp.Name)
            .ToListAsync();
    }

    public async Task<RetentionPolicy?> GetByIdAsync(Guid id)
    {
        return await _context.RetentionPolicies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(rp => rp.Id == id)
            .GroupJoin(_context.Folders.AsNoTracking(), rp => rp.FolderId, f => f.Id, (rp, folders) => new { rp, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.rp, f })
            .GroupJoin(_context.Classifications.AsNoTracking(), x => x.rp.ClassificationId, c => c.Id, (x, cs) => new { x.rp, x.f, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.rp, x.f, c })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.rp.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.rp, x.f, x.c, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new RetentionPolicy
            {
                Id = x.rp.Id,
                Name = x.rp.Name,
                Description = x.rp.Description,
                RetentionDays = x.rp.RetentionDays,
                ExpirationAction = x.rp.ExpirationAction,
                NotifyBeforeExpiration = x.rp.NotifyBeforeExpiration,
                NotificationDays = x.rp.NotificationDays,
                FolderId = x.rp.FolderId,
                ClassificationId = x.rp.ClassificationId,
                DocumentTypeId = x.rp.DocumentTypeId,
                RequiresApproval = x.rp.RequiresApproval,
                InheritToSubfolders = x.rp.InheritToSubfolders,
                IsLegalHold = x.rp.IsLegalHold,
                IsActive = x.rp.IsActive,
                CreatedBy = x.rp.CreatedBy,
                CreatedAt = x.rp.CreatedAt,
                ModifiedBy = x.rp.ModifiedBy,
                ModifiedAt = x.rp.ModifiedAt,
                FolderName = x.f != null ? x.f.Name : null,
                ClassificationName = x.c != null ? x.c.Name : null,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<RetentionPolicy>> GetByFolderAsync(Guid folderId)
    {
        return await _context.RetentionPolicies
            .AsNoTracking()
            .Where(rp => rp.FolderId == folderId)
            .OrderBy(rp => rp.Name)
            .ToListAsync();
    }

    public async Task<RetentionPolicy?> GetApplicablePolicyAsync(Guid? folderId, Guid? classificationId, Guid? documentTypeId)
    {
        // Find the most specific applicable policy
        return await _context.RetentionPolicies
            .AsNoTracking()
            .Where(rp =>
                rp.FolderId == folderId ||
                rp.ClassificationId == classificationId ||
                rp.DocumentTypeId == documentTypeId ||
                (rp.FolderId == null && rp.ClassificationId == null && rp.DocumentTypeId == null))
            .OrderBy(rp => rp.FolderId == folderId ? 0 : 1)
            .ThenBy(rp => rp.ClassificationId == classificationId ? 0 : 1)
            .ThenBy(rp => rp.DocumentTypeId == documentTypeId ? 0 : 1)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(RetentionPolicy policy)
    {
        policy.Id = Guid.NewGuid();
        policy.CreatedAt = DateTime.UtcNow;

        _context.RetentionPolicies.Add(policy);
        await _context.SaveChangesAsync();
        return policy.Id;
    }

    public async Task<bool> UpdateAsync(RetentionPolicy policy)
    {
        policy.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.RetentionPolicies.FindAsync(policy.Id);
        if (existing == null) return false;

        existing.Name = policy.Name;
        existing.Description = policy.Description;
        existing.RetentionDays = policy.RetentionDays;
        existing.ExpirationAction = policy.ExpirationAction;
        existing.NotifyBeforeExpiration = policy.NotifyBeforeExpiration;
        existing.NotificationDays = policy.NotificationDays;
        existing.FolderId = policy.FolderId;
        existing.ClassificationId = policy.ClassificationId;
        existing.DocumentTypeId = policy.DocumentTypeId;
        existing.RequiresApproval = policy.RequiresApproval;
        existing.InheritToSubfolders = policy.InheritToSubfolders;
        existing.IsLegalHold = policy.IsLegalHold;
        existing.IsActive = policy.IsActive;
        existing.ModifiedBy = policy.ModifiedBy;
        existing.ModifiedAt = policy.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.RetentionPolicies
            .Where(rp => rp.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(rp => rp.IsActive, false)
                .SetProperty(rp => rp.ModifiedAt, DateTime.UtcNow)) > 0;
    }

    #endregion

    #region Document Retention

    public async Task<IEnumerable<DocumentRetention>> GetDocumentRetentionsAsync(Guid documentId)
    {
        return await _context.DocumentRetentions
            .AsNoTracking()
            .Where(dr => dr.DocumentId == documentId)
            .Join(_context.Documents.AsNoTracking(), dr => dr.DocumentId, d => d.Id, (dr, d) => new { dr, d })
            .Join(_context.RetentionPolicies.IgnoreQueryFilters().AsNoTracking(), x => x.dr.PolicyId, rp => rp.Id, (x, rp) => new DocumentRetention
            {
                Id = x.dr.Id,
                DocumentId = x.dr.DocumentId,
                PolicyId = x.dr.PolicyId,
                RetentionStartDate = x.dr.RetentionStartDate,
                ExpirationDate = x.dr.ExpirationDate,
                Status = x.dr.Status,
                NotificationSent = x.dr.NotificationSent,
                ActionDate = x.dr.ActionDate,
                ApprovedBy = x.dr.ApprovedBy,
                Notes = x.dr.Notes,
                CreatedAt = x.dr.CreatedAt,
                ModifiedAt = x.dr.ModifiedAt,
                DocumentName = x.d.Name,
                PolicyName = rp.Name
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentRetention>> GetExpiringDocumentsAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await _context.DocumentRetentions
            .AsNoTracking()
            .Where(dr => dr.Status == "Active"
                && dr.ExpirationDate != null
                && dr.ExpirationDate <= cutoffDate)
            .Join(_context.Documents.AsNoTracking(), dr => dr.DocumentId, d => d.Id, (dr, d) => new { dr, d })
            .Join(_context.RetentionPolicies.IgnoreQueryFilters().AsNoTracking(), x => x.dr.PolicyId, rp => rp.Id, (x, rp) => new DocumentRetention
            {
                Id = x.dr.Id,
                DocumentId = x.dr.DocumentId,
                PolicyId = x.dr.PolicyId,
                RetentionStartDate = x.dr.RetentionStartDate,
                ExpirationDate = x.dr.ExpirationDate,
                Status = x.dr.Status,
                NotificationSent = x.dr.NotificationSent,
                ActionDate = x.dr.ActionDate,
                ApprovedBy = x.dr.ApprovedBy,
                Notes = x.dr.Notes,
                CreatedAt = x.dr.CreatedAt,
                ModifiedAt = x.dr.ModifiedAt,
                DocumentName = x.d.Name,
                PolicyName = rp.Name
            })
            .OrderBy(dr => dr.ExpirationDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentRetention>> GetPendingReviewAsync()
    {
        return await _context.DocumentRetentions
            .AsNoTracking()
            .Where(dr => dr.Status == "PendingReview")
            .Join(_context.Documents.AsNoTracking(), dr => dr.DocumentId, d => d.Id, (dr, d) => new { dr, d })
            .Join(_context.RetentionPolicies.IgnoreQueryFilters().AsNoTracking(), x => x.dr.PolicyId, rp => rp.Id, (x, rp) => new DocumentRetention
            {
                Id = x.dr.Id,
                DocumentId = x.dr.DocumentId,
                PolicyId = x.dr.PolicyId,
                RetentionStartDate = x.dr.RetentionStartDate,
                ExpirationDate = x.dr.ExpirationDate,
                Status = x.dr.Status,
                NotificationSent = x.dr.NotificationSent,
                ActionDate = x.dr.ActionDate,
                ApprovedBy = x.dr.ApprovedBy,
                Notes = x.dr.Notes,
                CreatedAt = x.dr.CreatedAt,
                ModifiedAt = x.dr.ModifiedAt,
                DocumentName = x.d.Name,
                PolicyName = rp.Name
            })
            .OrderBy(dr => dr.ExpirationDate)
            .ToListAsync();
    }

    public async Task<(List<DocumentRetention> Items, int TotalCount)> GetExpiringDocumentsPaginatedAsync(int daysAhead, int page, int pageSize)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        var query = _context.DocumentRetentions
            .AsNoTracking()
            .Where(dr => dr.Status == "Active"
                && dr.ExpirationDate != null
                && dr.ExpirationDate <= cutoffDate);

        var totalCount = await query.CountAsync();

        var items = await query
            .Join(_context.Documents.AsNoTracking(), dr => dr.DocumentId, d => d.Id, (dr, d) => new { dr, d })
            .Join(_context.RetentionPolicies.IgnoreQueryFilters().AsNoTracking(), x => x.dr.PolicyId, rp => rp.Id, (x, rp) => new DocumentRetention
            {
                Id = x.dr.Id,
                DocumentId = x.dr.DocumentId,
                PolicyId = x.dr.PolicyId,
                RetentionStartDate = x.dr.RetentionStartDate,
                ExpirationDate = x.dr.ExpirationDate,
                Status = x.dr.Status,
                NotificationSent = x.dr.NotificationSent,
                ActionDate = x.dr.ActionDate,
                ApprovedBy = x.dr.ApprovedBy,
                Notes = x.dr.Notes,
                CreatedAt = x.dr.CreatedAt,
                ModifiedAt = x.dr.ModifiedAt,
                DocumentName = x.d.Name,
                PolicyName = rp.Name
            })
            .OrderBy(dr => dr.ExpirationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<DocumentRetention> Items, int TotalCount)> GetPendingReviewPaginatedAsync(int page, int pageSize)
    {
        var query = _context.DocumentRetentions
            .AsNoTracking()
            .Where(dr => dr.Status == "PendingReview");

        var totalCount = await query.CountAsync();

        var items = await query
            .Join(_context.Documents.AsNoTracking(), dr => dr.DocumentId, d => d.Id, (dr, d) => new { dr, d })
            .Join(_context.RetentionPolicies.IgnoreQueryFilters().AsNoTracking(), x => x.dr.PolicyId, rp => rp.Id, (x, rp) => new DocumentRetention
            {
                Id = x.dr.Id,
                DocumentId = x.dr.DocumentId,
                PolicyId = x.dr.PolicyId,
                RetentionStartDate = x.dr.RetentionStartDate,
                ExpirationDate = x.dr.ExpirationDate,
                Status = x.dr.Status,
                NotificationSent = x.dr.NotificationSent,
                ActionDate = x.dr.ActionDate,
                ApprovedBy = x.dr.ApprovedBy,
                Notes = x.dr.Notes,
                CreatedAt = x.dr.CreatedAt,
                ModifiedAt = x.dr.ModifiedAt,
                DocumentName = x.d.Name,
                PolicyName = rp.Name
            })
            .OrderBy(dr => dr.ExpirationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Guid> CreateDocumentRetentionAsync(DocumentRetention retention)
    {
        retention.Id = Guid.NewGuid();
        retention.CreatedAt = DateTime.UtcNow;

        _context.DocumentRetentions.Add(retention);
        await _context.SaveChangesAsync();
        return retention.Id;
    }

    public async Task<bool> UpdateDocumentRetentionAsync(DocumentRetention retention)
    {
        retention.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.DocumentRetentions.FindAsync(retention.Id);
        if (existing == null) return false;

        existing.Status = retention.Status;
        existing.NotificationSent = retention.NotificationSent;
        existing.ActionDate = retention.ActionDate;
        existing.ApprovedBy = retention.ApprovedBy;
        existing.Notes = retention.Notes;
        existing.ModifiedAt = retention.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ApplyPolicyToDocumentAsync(Guid documentId, Guid policyId, Guid userId)
    {
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

        _context.DocumentRetentions.Add(retention);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ApproveRetentionActionAsync(Guid retentionId, Guid userId, string? notes = null)
    {
        var now = DateTime.UtcNow;
        return await _context.DocumentRetentions
            .Where(dr => dr.Id == retentionId && dr.Status == "PendingReview")
            .ExecuteUpdateAsync(s => s
                .SetProperty(dr => dr.Status, "Approved")
                .SetProperty(dr => dr.ApprovedBy, userId)
                .SetProperty(dr => dr.ActionDate, now)
                .SetProperty(dr => dr.Notes, notes)
                .SetProperty(dr => dr.ModifiedAt, now)) > 0;
    }

    public async Task<bool> PlaceOnHoldAsync(Guid documentId, Guid userId, string? notes = null)
    {
        var now = DateTime.UtcNow;
        var holdNote = $" | Legal hold placed by {userId} on {now}: {notes}";

        var affected = await _context.DocumentRetentions
            .Where(dr => dr.DocumentId == documentId && (dr.Status == "Active" || dr.Status == "PendingReview"))
            .ExecuteUpdateAsync(s => s
                .SetProperty(dr => dr.Status, "OnHold")
                .SetProperty(dr => dr.Notes, dr => (dr.Notes ?? "") + holdNote)
                .SetProperty(dr => dr.ModifiedAt, now));

        return affected > 0;
    }

    public async Task<bool> ReleaseHoldAsync(Guid documentId, Guid userId)
    {
        var now = DateTime.UtcNow;
        var releaseNote = $" | Hold released by {userId} on {now}";

        var affected = await _context.DocumentRetentions
            .Where(dr => dr.DocumentId == documentId && dr.Status == "OnHold")
            .ExecuteUpdateAsync(s => s
                .SetProperty(dr => dr.Status, "Active")
                .SetProperty(dr => dr.Notes, dr => (dr.Notes ?? "") + releaseNote)
                .SetProperty(dr => dr.ModifiedAt, now));

        return affected > 0;
    }

    #endregion
}
