namespace DMS.DAL.Entities;

public class ApprovalRequest
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? WorkflowId { get; set; }
    public Guid RequestedBy { get; set; }
    public int Status { get; set; } // 0=Pending, 1=Approved, 2=Rejected, 3=Cancelled
    public DateTime? DueDate { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public string? DocumentName { get; set; }
    public string? RequestedByName { get; set; }
    public string? WorkflowName { get; set; }
    public List<ApprovalAction>? Actions { get; set; }
}

public class ApprovalAction
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public Guid? StepId { get; set; }
    public Guid ApproverId { get; set; }
    public int Action { get; set; } // 1=Approved, 2=Rejected, 3=Returned for revision
    public string? Comments { get; set; }
    public DateTime ActionDate { get; set; }

    // Navigation properties
    public string? ApproverName { get; set; }
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

public enum ApprovalActionType
{
    Approved = 1,
    Rejected = 2,
    ReturnedForRevision = 3
}
