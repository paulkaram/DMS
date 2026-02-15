using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class ApprovalService : IApprovalService
{
    private readonly IApprovalWorkflowRepository _workflowRepository;
    private readonly IApprovalRequestRepository _requestRepository;

    public ApprovalService(
        IApprovalWorkflowRepository workflowRepository,
        IApprovalRequestRepository requestRepository)
    {
        _workflowRepository = workflowRepository;
        _requestRepository = requestRepository;
    }

    // Workflow methods
    public async Task<IEnumerable<ApprovalWorkflowDto>> GetWorkflowsAsync()
    {
        var workflows = await _workflowRepository.GetAllAsync();
        return workflows.Select(MapWorkflowToDto);
    }

    public async Task<ApprovalWorkflowDto?> GetWorkflowByIdAsync(Guid id)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id);
        return workflow != null ? MapWorkflowToDto(workflow) : null;
    }

    public async Task<Guid> CreateWorkflowAsync(Guid createdBy, CreateWorkflowRequest request)
    {
        var workflow = new ApprovalWorkflow
        {
            Name = request.Name,
            Description = request.Description,
            FolderId = request.FolderId,
            RequiredApprovers = request.RequiredApprovers,
            IsSequential = request.IsSequential,
            IsActive = true,
            CreatedBy = createdBy,
            DesignerData = request.DesignerData,
            TriggerType = request.TriggerType,
            InheritToSubfolders = request.InheritToSubfolders
        };

        var workflowId = await _workflowRepository.CreateAsync(workflow);

        // Add steps if provided
        if (request.Steps != null)
        {
            foreach (var step in request.Steps)
            {
                await _workflowRepository.AddStepAsync(new ApprovalWorkflowStep
                {
                    WorkflowId = workflowId,
                    StepOrder = step.StepOrder,
                    ApproverUserId = step.ApproverUserId,
                    ApproverRoleId = step.ApproverRoleId,
                    ApproverStructureId = step.ApproverStructureId,
                    AssignToManager = step.AssignToManager,
                    IsRequired = step.IsRequired,
                    StatusId = step.StatusId
                });
            }
        }

        return workflowId;
    }

    public async Task<bool> UpdateWorkflowAsync(Guid id, CreateWorkflowRequest request)
    {
        var workflow = await _workflowRepository.GetByIdAsync(id);
        if (workflow == null) return false;

        workflow.Name = request.Name;
        workflow.Description = request.Description;
        workflow.FolderId = request.FolderId;
        workflow.RequiredApprovers = request.RequiredApprovers;
        workflow.IsSequential = request.IsSequential;
        if (request.DesignerData != null)
            workflow.DesignerData = request.DesignerData;
        workflow.TriggerType = request.TriggerType;
        workflow.InheritToSubfolders = request.InheritToSubfolders;

        var updated = await _workflowRepository.UpdateAsync(workflow);

        // Replace steps if provided
        if (request.Steps != null)
        {
            var newSteps = request.Steps.Select(s => new ApprovalWorkflowStep
            {
                StepOrder = s.StepOrder,
                ApproverUserId = s.ApproverUserId,
                ApproverRoleId = s.ApproverRoleId,
                ApproverStructureId = s.ApproverStructureId,
                AssignToManager = s.AssignToManager,
                IsRequired = s.IsRequired,
                StatusId = s.StatusId
            });
            await _workflowRepository.ReplaceStepsAsync(id, newSteps);
        }

        return updated;
    }

    public async Task<bool> DeleteWorkflowAsync(Guid id)
    {
        return await _workflowRepository.DeleteAsync(id);
    }

    // Request methods
    public async Task<IEnumerable<ApprovalRequestDto>> GetPendingRequestsForUserAsync(Guid userId)
    {
        var requests = await _requestRepository.GetPendingForUserAsync(userId);
        return requests.Select(MapRequestToDto);
    }

    public async Task<IEnumerable<ApprovalRequestDto>> GetMyRequestsAsync(Guid userId)
    {
        var requests = await _requestRepository.GetByRequestedByAsync(userId);
        return requests.Select(MapRequestToDto);
    }

    public async Task<IEnumerable<ApprovalRequestDto>> GetDocumentRequestsAsync(Guid documentId)
    {
        var requests = await _requestRepository.GetByDocumentIdAsync(documentId);
        return requests.Select(MapRequestToDto);
    }

    public async Task<ApprovalRequestDto?> GetRequestByIdAsync(Guid id)
    {
        var request = await _requestRepository.GetByIdAsync(id);
        return request != null ? MapRequestToDto(request) : null;
    }

    public async Task<Guid> CreateRequestAsync(Guid requestedBy, CreateApprovalRequestDto request)
    {
        var approvalRequest = new ApprovalRequest
        {
            DocumentId = request.DocumentId,
            WorkflowId = request.WorkflowId,
            RequestedBy = requestedBy,
            Status = (int)ApprovalStatus.Pending,
            DueDate = request.DueDate,
            Comments = request.Comments
        };

        return await _requestRepository.CreateAsync(approvalRequest);
    }

    public async Task<bool> SubmitActionAsync(Guid requestId, Guid approverId, SubmitApprovalActionDto actionDto)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null || request.Status != (int)ApprovalStatus.Pending)
            return false;

        // Add the action
        await _requestRepository.AddActionAsync(new ApprovalAction
        {
            RequestId = requestId,
            ApproverId = approverId,
            Action = actionDto.Action,
            Comments = actionDto.Comments
        });

        // Update request status based on action
        if (actionDto.Action == (int)ApprovalActionType.Rejected)
        {
            await _requestRepository.UpdateStatusAsync(requestId, (int)ApprovalStatus.Rejected);
        }
        else if (actionDto.Action == (int)ApprovalActionType.ReturnedForRevision)
        {
            await _requestRepository.UpdateStatusAsync(requestId, (int)ApprovalStatus.ReturnedForRevision);
        }
        else if (actionDto.Action == (int)ApprovalActionType.Approved)
        {
            // Check if all required approvers have approved
            var workflow = request.WorkflowId.HasValue
                ? await _workflowRepository.GetByIdAsync(request.WorkflowId.Value)
                : null;

            var actions = await _requestRepository.GetActionsAsync(requestId);
            var approveCount = actions.Count(a => a.Action == (int)ApprovalActionType.Approved);

            var requiredApprovers = workflow?.RequiredApprovers ?? 1;
            if (approveCount >= requiredApprovers)
            {
                await _requestRepository.UpdateStatusAsync(requestId, (int)ApprovalStatus.Approved);
            }
        }

        return true;
    }

    public async Task<bool> CancelRequestAsync(Guid requestId)
    {
        return await _requestRepository.UpdateStatusAsync(requestId, (int)ApprovalStatus.Cancelled);
    }

    public async Task<bool> ResubmitRequestAsync(Guid requestId, Guid userId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null) return false;

        // Only the original requester can resubmit
        if (request.RequestedBy != userId) return false;

        // Only returned-for-revision or rejected requests can be resubmitted
        if (request.Status != (int)ApprovalStatus.ReturnedForRevision &&
            request.Status != (int)ApprovalStatus.Rejected)
            return false;

        return await _requestRepository.UpdateStatusAsync(requestId, (int)ApprovalStatus.Pending);
    }

    public async Task TryAutoTriggerWorkflowAsync(Guid documentId, Guid folderId, Guid uploadedBy)
    {
        var workflow = await _workflowRepository.GetActiveByFolderChainAsync(folderId, "OnUpload");
        if (workflow == null) return;

        var approvalRequest = new ApprovalRequest
        {
            DocumentId = documentId,
            WorkflowId = workflow.Id,
            RequestedBy = uploadedBy,
            Status = (int)ApprovalStatus.Pending,
            Comments = $"Auto-triggered by workflow '{workflow.Name}' on upload"
        };

        await _requestRepository.CreateAsync(approvalRequest);
    }

    public async Task<Dictionary<Guid, int>> GetApprovalStatusBulkAsync(IEnumerable<Guid> documentIds)
    {
        return await _requestRepository.GetLatestStatusByDocumentIdsAsync(documentIds);
    }

    public async Task<bool> IsApproverForDocumentAsync(Guid documentId, Guid userId)
    {
        return await _requestRepository.IsApproverForDocumentAsync(documentId, userId);
    }

    private static ApprovalWorkflowDto MapWorkflowToDto(ApprovalWorkflow workflow)
    {
        return new ApprovalWorkflowDto
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Description = workflow.Description,
            FolderId = workflow.FolderId,
            RequiredApprovers = workflow.RequiredApprovers,
            IsSequential = workflow.IsSequential,
            IsActive = workflow.IsActive,
            CreatedAt = workflow.CreatedAt,
            FolderName = workflow.FolderName,
            DesignerData = workflow.DesignerData,
            TriggerType = workflow.TriggerType,
            InheritToSubfolders = workflow.InheritToSubfolders,
            Steps = workflow.Steps?.Select(s => new ApprovalWorkflowStepDto
            {
                Id = s.Id,
                WorkflowId = s.WorkflowId,
                StepOrder = s.StepOrder,
                ApproverUserId = s.ApproverUserId,
                ApproverRoleId = s.ApproverRoleId,
                ApproverStructureId = s.ApproverStructureId,
                AssignToManager = s.AssignToManager,
                IsRequired = s.IsRequired,
                StatusId = s.StatusId,
                ApproverUserName = s.ApproverUserName,
                ApproverRoleName = s.ApproverRoleName,
                ApproverStructureName = s.ApproverStructureName,
                StatusName = s.StatusName,
                StatusColor = s.StatusColor
            }).ToList()
        };
    }

    private static ApprovalRequestDto MapRequestToDto(ApprovalRequest request)
    {
        return new ApprovalRequestDto
        {
            Id = request.Id,
            DocumentId = request.DocumentId,
            WorkflowId = request.WorkflowId,
            RequestedBy = request.RequestedBy,
            Status = request.Status,
            DueDate = request.DueDate,
            Comments = request.Comments,
            CreatedAt = request.CreatedAt,
            CompletedAt = request.CompletedAt,
            DocumentName = request.DocumentName,
            RequestedByName = request.RequestedByName,
            WorkflowName = request.WorkflowName,
            Actions = request.Actions?.Select(a => new ApprovalActionDto
            {
                Id = a.Id,
                RequestId = a.RequestId,
                StepId = a.StepId,
                ApproverId = a.ApproverId,
                Action = a.Action,
                Comments = a.Comments,
                ActionDate = a.ActionDate,
                ApproverName = a.ApproverName
            }).ToList()
        };
    }
}
