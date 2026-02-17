namespace DMS.BL.DTOs;

/// <summary>
/// DTO for disposal certificate display.
/// </summary>
public class DisposalCertificateDto
{
    public Guid Id { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? DocumentPath { get; set; }
    public string? Classification { get; set; }
    public Guid? RetentionPolicyId { get; set; }
    public string? RetentionPolicyName { get; set; }
    public DateTime DocumentCreatedAt { get; set; }
    public DateTime? RetentionStartDate { get; set; }
    public DateTime? RetentionExpirationDate { get; set; }
    public string DisposalMethod { get; set; } = string.Empty;
    public DateTime DisposedAt { get; set; }
    public Guid DisposedBy { get; set; }
    public string? DisposedByName { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? LegalBasis { get; set; }
    public string? Notes { get; set; }
    public string? ContentHashAtDisposal { get; set; }
    public long FileSizeAtDisposal { get; set; }
    public int VersionsDisposed { get; set; }
    public bool DisposalVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for initiating disposal.
/// </summary>
public class InitiateDisposalDto
{
    public string? Reason { get; set; }
    public string? LegalBasis { get; set; }
    public string DisposalMethod { get; set; } = "HardDelete";
    public bool RequiresApproval { get; set; } = true;
}

/// <summary>
/// DTO for disposal request.
/// </summary>
public class DisposalRequestDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string DisposalMethod { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? LegalBasis { get; set; }
    public Guid RequestedBy { get; set; }
    public string? RequestedByName { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
}

/// <summary>
/// DTO for document disposal status.
/// </summary>
public class DocumentDisposalStatusDto
{
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? FolderPath { get; set; }
    public Guid? RetentionPolicyId { get; set; }
    public string? RetentionPolicyName { get; set; }
    public DateTime DocumentCreatedAt { get; set; }
    public DateTime? RetentionStartDate { get; set; }
    public DateTime? RetentionExpirationDate { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsExpired { get; set; }
    public bool IsOnLegalHold { get; set; }
    public string? ExpirationAction { get; set; }
    public bool RequiresApproval { get; set; }
}

/// <summary>
/// Result of batch disposal processing.
/// </summary>
public class DisposalBatchResult
{
    public int TotalPending { get; set; }
    public int ProcessedCount { get; set; }
    public int DisposedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
}

// --- Batch Disposal DTOs ---

/// <summary>
/// DTO for initiating a batch disposal request.
/// </summary>
public class InitiateBatchDisposalDto
{
    public List<Guid> DocumentIds { get; set; } = new();
    public string DisposalMethod { get; set; } = "HardDelete";
    public string? Reason { get; set; }
    public string? LegalBasis { get; set; }
    public string? BatchReference { get; set; }
}

/// <summary>
/// DTO for submitting a disposal approval decision.
/// </summary>
public class SubmitDisposalApprovalDto
{
    public string Decision { get; set; } = "Approved";
    public string? Comments { get; set; }
}

/// <summary>
/// Full disposal request detail DTO with documents and approvals.
/// </summary>
public class DisposalRequestDetailDto
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BatchReference { get; set; }
    public string DisposalMethod { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? LegalBasis { get; set; }
    public int RequiredApprovalLevels { get; set; }
    public int CurrentApprovalLevel { get; set; }
    public Guid RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public List<DisposalRequestDocumentDto> Documents { get; set; } = new();
    public List<DisposalApprovalDto> Approvals { get; set; } = new();
}

public class DisposalRequestDocumentDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string? DocumentName { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? CertificateId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class DisposalApprovalDto
{
    public Guid Id { get; set; }
    public int ApprovalLevel { get; set; }
    public Guid ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecisionAt { get; set; }
}
