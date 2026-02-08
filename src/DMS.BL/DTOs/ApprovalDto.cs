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
    public List<ApprovalWorkflowStepDto>? Steps { get; set; }
}

public class ApprovalWorkflowStepDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public Guid? ApproverUserId { get; set; }
    public Guid? ApproverRoleId { get; set; }
    public bool IsRequired { get; set; }
    public string? ApproverUserName { get; set; }
    public string? ApproverRoleName { get; set; }
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
    public string StatusName => Status switch
    {
        0 => "Pending",
        1 => "Approved",
        2 => "Rejected",
        3 => "Cancelled",
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
    public string ActionName => Action switch
    {
        1 => "Approved",
        2 => "Rejected",
        3 => "Returned for Revision",
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
    public List<CreateWorkflowStepRequest>? Steps { get; set; }
}

public class CreateWorkflowStepRequest
{
    public int StepOrder { get; set; }
    public Guid? ApproverUserId { get; set; }
    public Guid? ApproverRoleId { get; set; }
    public bool IsRequired { get; set; } = true;
}
