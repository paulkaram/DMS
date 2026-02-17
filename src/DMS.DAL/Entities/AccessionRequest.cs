using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public enum AccessionStatus
{
    Draft = 0,
    Submitted = 1,
    UnderReview = 2,
    Accepted = 3,
    PartiallyAccepted = 4,
    Rejected = 5,
    Transferred = 6
}

/// <summary>
/// Accession/transfer request for physical items into the archive.
/// </summary>
public class AccessionRequest : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public string AccessionNumber { get; set; } = string.Empty;
    public Guid? SourceStructureId { get; set; }
    public Guid? TargetLocationId { get; set; }
    public AccessionStatus Status { get; set; } = AccessionStatus.Draft;
    public int ItemCount { get; set; }
    public DateTime? RecordsDateFrom { get; set; }
    public DateTime? RecordsDateTo { get; set; }
    public DateTime? RequestedTransferDate { get; set; }
    public DateTime? ActualTransferDate { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public Guid? AcceptedBy { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? AcceptanceNotes { get; set; }
    public string? RejectionReason { get; set; }

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Individual item in an accession request.
/// </summary>
public class AccessionRequestItem
{
    public Guid Id { get; set; }
    public Guid AccessionRequestId { get; set; }
    public Guid? PhysicalItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public PhysicalItemType ItemType { get; set; }
    public Guid? ClassificationId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
}
