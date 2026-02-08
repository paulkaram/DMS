namespace DMS.DAL.Entities;

public class ApprovalWorkflow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? FolderId { get; set; }
    public int RequiredApprovers { get; set; } = 1;
    public bool IsSequential { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public string? FolderName { get; set; }
    public List<ApprovalWorkflowStep>? Steps { get; set; }
}

public class ApprovalWorkflowStep
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int StepOrder { get; set; }
    public Guid? ApproverUserId { get; set; }
    public Guid? ApproverRoleId { get; set; }
    public bool IsRequired { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public string? ApproverUserName { get; set; }
    public string? ApproverRoleName { get; set; }
}
