using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public enum DisposalRequestStatus
{
    Pending = 0,
    PendingApproval = 1,
    PartiallyApproved = 2,
    Approved = 3,
    Rejected = 4,
    Executed = 5,
    Cancelled = 6
}

public enum DisposalApprovalDecision
{
    Approved = 0,
    Rejected = 1,
    ReturnedForReview = 2
}

/// <summary>
/// A disposal request for one or more documents, supporting multi-level approval.
/// </summary>
public class DisposalRequest : IAuditable
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public DisposalRequestStatus Status { get; set; } = DisposalRequestStatus.Pending;
    public string? BatchReference { get; set; }
    public string DisposalMethod { get; set; } = "HardDelete";
    public string? Reason { get; set; }
    public string? LegalBasis { get; set; }
    public int RequiredApprovalLevels { get; set; } = 1;
    public int CurrentApprovalLevel { get; set; } = 0;
    public Guid RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public Guid? ExecutedBy { get; set; }
    public Guid? CertificateId { get; set; }

    // IAuditable
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public enum DisposalRequestDocumentStatus
{
    Pending = 0,
    Disposed = 1,
    Skipped = 2,
    Error = 3
}

/// <summary>
/// Links a disposal request to individual documents for batch disposal.
/// </summary>
public class DisposalRequestDocument
{
    public Guid Id { get; set; }
    public Guid DisposalRequestId { get; set; }
    public Guid DocumentId { get; set; }
    public DisposalRequestDocumentStatus Status { get; set; } = DisposalRequestDocumentStatus.Pending;
    public Guid? CertificateId { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation (for display)
    public string? DocumentName { get; set; }
}

/// <summary>
/// Multi-level approval for a disposal request.
/// </summary>
public class DisposalApproval
{
    public Guid Id { get; set; }
    public Guid DisposalRequestId { get; set; }
    public int ApprovalLevel { get; set; }
    public Guid ApproverId { get; set; }
    public DisposalApprovalDecision Decision { get; set; }
    public string? Comments { get; set; }
    public DateTime DecisionAt { get; set; }
}
