using DMS.BL.DTOs;
using DMS.DAL.Entities;

namespace DMS.BL.Interfaces;

/// <summary>
/// Advanced retention engine with event-based triggers, legal hold suspension,
/// classification-based auto-application, and recalculation support.
/// </summary>
public interface IRetentionEngineService
{
    /// <summary>
    /// Apply a specific retention policy to a document.
    /// </summary>
    Task<ServiceResult> ApplyRetentionAsync(Guid documentId, Guid policyId, Guid userId);

    /// <summary>
    /// Fire a trigger event for a document, starting the retention countdown.
    /// </summary>
    Task<ServiceResult> FireTriggerEventAsync(Guid documentId, RetentionTriggerType triggerType, Guid userId);

    /// <summary>
    /// Recalculate retention when classification changes (applies new default policy).
    /// </summary>
    Task<ServiceResult> RecalculateOnClassificationChangeAsync(Guid documentId, Guid? newClassificationId, Guid userId);

    /// <summary>
    /// Suspend retention countdown (when legal hold is applied).
    /// </summary>
    Task<ServiceResult> SuspendRetentionAsync(Guid documentId, Guid userId);

    /// <summary>
    /// Resume retention countdown (when legal hold is released), extending expiration by suspended days.
    /// </summary>
    Task<ServiceResult> ResumeRetentionAsync(Guid documentId, Guid userId);

    /// <summary>
    /// Auto-apply matching retention policy based on classification/folder hierarchy.
    /// </summary>
    Task<ServiceResult> AutoApplyRetentionAsync(Guid documentId, Guid? classificationId, Guid? folderId);
}
