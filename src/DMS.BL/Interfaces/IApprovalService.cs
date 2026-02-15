using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IApprovalService
{
    // Workflows
    Task<IEnumerable<ApprovalWorkflowDto>> GetWorkflowsAsync();
    Task<ApprovalWorkflowDto?> GetWorkflowByIdAsync(Guid id);
    Task<Guid> CreateWorkflowAsync(Guid createdBy, CreateWorkflowRequest request);
    Task<bool> UpdateWorkflowAsync(Guid id, CreateWorkflowRequest request);
    Task<bool> DeleteWorkflowAsync(Guid id);
    Task TryAutoTriggerWorkflowAsync(Guid documentId, Guid folderId, Guid uploadedBy);

    // Requests
    Task<IEnumerable<ApprovalRequestDto>> GetPendingRequestsForUserAsync(Guid userId);
    Task<IEnumerable<ApprovalRequestDto>> GetMyRequestsAsync(Guid userId);
    Task<IEnumerable<ApprovalRequestDto>> GetDocumentRequestsAsync(Guid documentId);
    Task<ApprovalRequestDto?> GetRequestByIdAsync(Guid id);
    Task<Guid> CreateRequestAsync(Guid requestedBy, CreateApprovalRequestDto request);
    Task<bool> SubmitActionAsync(Guid requestId, Guid approverId, SubmitApprovalActionDto action);
    Task<bool> CancelRequestAsync(Guid requestId);
    Task<bool> ResubmitRequestAsync(Guid requestId, Guid userId);
    Task<Dictionary<Guid, int>> GetApprovalStatusBulkAsync(IEnumerable<Guid> documentIds);
    Task<bool> IsApproverForDocumentAsync(Guid documentId, Guid userId);
}
