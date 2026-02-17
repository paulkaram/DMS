using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class RetentionEngineService : IRetentionEngineService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IRetentionPolicyRepository _policyRepository;
    private readonly IClassificationRepository _classificationRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly DmsDbContext _context;
    private readonly ILogger<RetentionEngineService> _logger;

    public RetentionEngineService(
        IDocumentRepository documentRepository,
        IRetentionPolicyRepository policyRepository,
        IClassificationRepository classificationRepository,
        IActivityLogService activityLogService,
        DmsDbContext context,
        ILogger<RetentionEngineService> logger)
    {
        _documentRepository = documentRepository;
        _policyRepository = policyRepository;
        _classificationRepository = classificationRepository;
        _activityLogService = activityLogService;
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult> ApplyRetentionAsync(Guid documentId, Guid policyId, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult.Fail("Document not found");

        var policy = await _policyRepository.GetByIdAsync(policyId);
        if (policy == null)
            return ServiceResult.Fail("Retention policy not found");

        // Calculate expiration based on retention basis
        DateTime startDate;
        if (policy.RetentionBasis == "DeclaredRecord" && document.State >= DocumentState.Record)
            startDate = document.StateChangedAt ?? document.CreatedAt;
        else
            startDate = document.CreatedAt;

        DateTime? expirationDate = policy.RetentionDays > 0
            ? startDate.AddDays(policy.RetentionDays)
            : null; // 0 = permanent

        var retention = new DocumentRetention
        {
            DocumentId = documentId,
            PolicyId = policyId,
            RetentionStartDate = startDate,
            ExpirationDate = expirationDate,
            OriginalExpirationDate = expirationDate,
            Status = "Active"
        };

        // If event-based, don't set expiration yet (waiting for trigger)
        if (policy.RetentionBasis == "EventBased")
        {
            retention.ExpirationDate = null;
            retention.Status = "AwaitingTrigger";
        }

        await _policyRepository.CreateDocumentRetentionAsync(retention);

        document.RetentionPolicyId = policyId;
        await _documentRepository.UpdateAsync(document);

        await _activityLogService.LogActivityAsync(
            "RetentionApplied", "Document", documentId, document.Name,
            $"Retention policy '{policy.Name}' applied. Basis: {policy.RetentionBasis}", userId, null, null);

        return ServiceResult.Ok("Retention policy applied");
    }

    public async Task<ServiceResult> FireTriggerEventAsync(Guid documentId, RetentionTriggerType triggerType, Guid userId)
    {
        var retentions = (await _policyRepository.GetDocumentRetentionsAsync(documentId))
            .Where(r => r.Status == "AwaitingTrigger")
            .ToList();

        if (retentions.Count == 0)
            return ServiceResult.Ok("No pending trigger-based retentions found");

        foreach (var retention in retentions)
        {
            var policy = await _policyRepository.GetByIdAsync(retention.PolicyId);
            if (policy == null) continue;

            // Check if there's a matching trigger event for this policy
            var triggerEvent = await _context.RetentionTriggerEvents
                .FirstOrDefaultAsync(te => te.RetentionPolicyId == policy.Id
                    && te.TriggerType == triggerType
                    && te.IsActive);

            if (triggerEvent == null) continue;

            var previousExpiration = retention.ExpirationDate;
            var startDate = DateTime.Now;
            retention.RetentionStartDate = startDate;
            retention.ExpirationDate = policy.RetentionDays > 0
                ? startDate.AddDays(policy.RetentionDays)
                : null;
            retention.OriginalExpirationDate = retention.ExpirationDate;
            retention.TriggerEventId = triggerEvent.Id;
            retention.Status = "Active";
            retention.ModifiedAt = DateTime.Now;

            await _policyRepository.UpdateDocumentRetentionAsync(retention);

            // Log the trigger
            _context.RetentionTriggerLogs.Add(new RetentionTriggerLog
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                RetentionPolicyId = policy.Id,
                TriggerEventId = triggerEvent.Id,
                TriggerType = triggerType,
                TriggeredAt = DateTime.Now,
                TriggeredBy = userId,
                NewExpirationDate = retention.ExpirationDate,
                PreviousExpirationDate = previousExpiration
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Retention trigger {TriggerType} fired for document {DocumentId}", triggerType, documentId);
        }

        return ServiceResult.Ok("Trigger event processed");
    }

    public async Task<ServiceResult> RecalculateOnClassificationChangeAsync(Guid documentId, Guid? newClassificationId, Guid userId)
    {
        if (!newClassificationId.HasValue)
            return ServiceResult.Ok("No classification to evaluate");

        // Walk up the classification hierarchy looking for a default retention policy
        var classification = await _classificationRepository.GetByIdAsync(newClassificationId.Value);
        while (classification != null)
        {
            if (classification.DefaultRetentionPolicyId.HasValue)
            {
                var result = await ApplyRetentionAsync(documentId, classification.DefaultRetentionPolicyId.Value, userId);
                if (result.Success)
                {
                    _logger.LogInformation(
                        "Retention recalculated for document {DocumentId} from classification {ClassificationId}",
                        documentId, classification.Id);
                }
                return result;
            }

            if (classification.ParentId.HasValue)
                classification = await _classificationRepository.GetByIdAsync(classification.ParentId.Value);
            else
                break;
        }

        return ServiceResult.Ok("No default retention policy found in classification hierarchy");
    }

    public async Task<ServiceResult> SuspendRetentionAsync(Guid documentId, Guid userId)
    {
        var retentions = (await _policyRepository.GetDocumentRetentionsAsync(documentId))
            .Where(r => r.Status == "Active" && r.SuspendedAt == null)
            .ToList();

        foreach (var retention in retentions)
        {
            retention.SuspendedAt = DateTime.Now;
            retention.Status = "OnHold";
            retention.ModifiedAt = DateTime.Now;
            retention.Notes = (retention.Notes ?? "") + $"\nSuspended on {DateTime.Now:yyyy-MM-dd} due to legal hold.";
            await _policyRepository.UpdateDocumentRetentionAsync(retention);
        }

        _logger.LogInformation("Retention suspended for document {DocumentId} ({Count} retentions)", documentId, retentions.Count);
        return ServiceResult.Ok("Retention suspended");
    }

    public async Task<ServiceResult> ResumeRetentionAsync(Guid documentId, Guid userId)
    {
        var retentions = (await _policyRepository.GetDocumentRetentionsAsync(documentId))
            .Where(r => r.Status == "OnHold" && r.SuspendedAt != null)
            .ToList();

        foreach (var retention in retentions)
        {
            var suspendedDays = (int)(DateTime.Now - retention.SuspendedAt!.Value).TotalDays;
            retention.SuspendedDays += suspendedDays;
            retention.SuspendedAt = null;
            retention.Status = "Active";
            retention.ModifiedAt = DateTime.Now;

            // Extend expiration by suspended days
            if (retention.ExpirationDate.HasValue)
            {
                retention.ExpirationDate = retention.ExpirationDate.Value.AddDays(suspendedDays);
            }

            retention.Notes = (retention.Notes ?? "") + $"\nResumed on {DateTime.Now:yyyy-MM-dd}. Extended by {suspendedDays} days.";
            await _policyRepository.UpdateDocumentRetentionAsync(retention);
        }

        _logger.LogInformation("Retention resumed for document {DocumentId} ({Count} retentions)", documentId, retentions.Count);
        return ServiceResult.Ok("Retention resumed");
    }

    public async Task<ServiceResult> AutoApplyRetentionAsync(Guid documentId, Guid? classificationId, Guid? folderId)
    {
        // 1. Try to find an applicable policy by classification/folder/docType
        var policy = await _policyRepository.GetApplicablePolicyAsync(folderId, classificationId, null);
        if (policy != null)
        {
            return await ApplyRetentionAsync(documentId, policy.Id, Guid.Empty);
        }

        // 2. If no direct policy, check classification hierarchy for default
        if (classificationId.HasValue)
        {
            return await RecalculateOnClassificationChangeAsync(documentId, classificationId, Guid.Empty);
        }

        return ServiceResult.Ok("No auto-applicable retention policy found");
    }
}
