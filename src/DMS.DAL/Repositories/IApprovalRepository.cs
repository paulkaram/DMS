using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IApprovalWorkflowRepository
{
    Task<ApprovalWorkflow?> GetByIdAsync(Guid id);
    Task<IEnumerable<ApprovalWorkflow>> GetAllAsync();
    Task<ApprovalWorkflow?> GetByFolderIdAsync(Guid folderId);
    Task<ApprovalWorkflow?> GetActiveByFolderChainAsync(Guid folderId, string triggerType);
    Task<IEnumerable<ApprovalWorkflowStep>> GetStepsAsync(Guid workflowId);
    Task<Guid> CreateAsync(ApprovalWorkflow entity);
    Task<bool> UpdateAsync(ApprovalWorkflow entity);
    Task<bool> DeleteAsync(Guid id);
    Task<Guid> AddStepAsync(ApprovalWorkflowStep step);
    Task<bool> RemoveStepAsync(Guid stepId);
    Task ReplaceStepsAsync(Guid workflowId, IEnumerable<ApprovalWorkflowStep> newSteps);
}

public interface IApprovalRequestRepository
{
    Task<ApprovalRequest?> GetByIdAsync(Guid id);
    Task<IEnumerable<ApprovalRequest>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<ApprovalRequest>> GetPendingForUserAsync(Guid userId);
    Task<IEnumerable<ApprovalRequest>> GetByRequestedByAsync(Guid userId);
    Task<IEnumerable<ApprovalRequest>> GetAllAsync(int? status = null);
    Task<IEnumerable<ApprovalAction>> GetActionsAsync(Guid requestId);
    Task<Guid> CreateAsync(ApprovalRequest entity);
    Task<bool> UpdateStatusAsync(Guid id, int status, DateTime? completedAt = null);
    Task<Guid> AddActionAsync(ApprovalAction action);
    Task<Dictionary<Guid, int>> GetLatestStatusByDocumentIdsAsync(IEnumerable<Guid> documentIds);
    Task<bool> IsApproverForDocumentAsync(Guid documentId, Guid userId);
}
