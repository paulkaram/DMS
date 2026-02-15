using DMS.DAL.Entities;

namespace DMS.BL.DTOs;

public class ApprovalWorkflowDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? FolderId { get; set; }
    public int RequiredApprovers { get; set; }
    public bool IsSequential { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? FolderName { get; set; }
    public string? DesignerData { get; set; }
    public string TriggerType { get; set; } = "Manual";
    public bool InheritToSubfolders { get; set; } = true;
    public List<ApprovalWorkflowStepDto>? Steps { get; set; }
}

public class ApprovalWorkflowStepDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public Guid? ApproverUserId { get; set; }
    public Guid? ApproverRoleId { get; set; }
    public Guid? ApproverStructureId { get; set; }
    public bool AssignToManager { get; set; }
    public bool IsRequired { get; set; }
    public Guid? StatusId { get; set; }
    public string? ApproverUserName { get; set; }
    public string? ApproverRoleName { get; set; }
    public string? ApproverStructureName { get; set; }
    public string? StatusName { get; set; }
    public string? StatusColor { get; set; }
}

public class ApprovalRequestDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? WorkflowId { get; set; }
    public Guid RequestedBy { get; set; }
    public int Status { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? DocumentName { get; set; }
    public string? RequestedByName { get; set; }
    public string? WorkflowName { get; set; }
    public string StatusName => (ApprovalStatus)Status switch
    {
        ApprovalStatus.Pending => "Pending",
        ApprovalStatus.Approved => "Approved",
        ApprovalStatus.Rejected => "Rejected",
        ApprovalStatus.Cancelled => "Cancelled",
        ApprovalStatus.ReturnedForRevision => "Returned for Revision",
        _ => "Unknown"
    };
    public List<ApprovalActionDto>? Actions { get; set; }
}

public class ApprovalActionDto
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid? StepId { get; set; }
    public Guid ApproverId { get; set; }
    public int Action { get; set; }
    public string? Comments { get; set; }
    public DateTime ActionDate { get; set; }
    public string? ApproverName { get; set; }
    public string ActionName => (ApprovalActionType)Action switch
    {
        ApprovalActionType.Approved => "Approved",
        ApprovalActionType.Rejected => "Rejected",
        ApprovalActionType.ReturnedForRevision => "Returned for Revision",
        _ => "Unknown"
    };
}

public class CreateApprovalRequestDto
{
    public Guid DocumentId { get; set; }
    public Guid? WorkflowId { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Comments { get; set; }
}

public class SubmitApprovalActionDto
{
    public int Action { get; set; } // 1=Approve, 2=Reject, 3=Return for revision
    public string? Comments { get; set; }
}

public class CreateWorkflowRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? FolderId { get; set; }
    public int RequiredApprovers { get; set; } = 1;
    public bool IsSequential { get; set; }
    public string? DesignerData { get; set; }
    public string TriggerType { get; set; } = "Manual";
    public bool InheritToSubfolders { get; set; } = true;
    public List<CreateWorkflowStepRequest>? Steps { get; set; }
}

public class CreateWorkflowStepRequest
{
    public int StepOrder { get; set; }
    public Guid? ApproverUserId { get; set; }
    public Guid? ApproverRoleId { get; set; }
    public Guid? ApproverStructureId { get; set; }
    public bool AssignToManager { get; set; }
    public bool IsRequired { get; set; } = true;
    public Guid? StatusId { get; set; }
}
