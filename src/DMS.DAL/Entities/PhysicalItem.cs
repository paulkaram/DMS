using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public enum PhysicalItemType
{
    Document = 1,
    File = 2,
    Box = 3,
    Volume = 4,
    Map = 5,
    Photograph = 6,
    AudioTape = 7,
    VideoTape = 8,
    Microfilm = 9,
    DigitalMedia = 10
}

public enum ItemCondition
{
    Good = 1,
    Fair = 2,
    Poor = 3,
    Damaged = 4,
    Destroyed = 5
}

public enum CirculationStatus
{
    Available = 0,
    CheckedOut = 1,
    InTransit = 2,
    Overdue = 3,
    Returned = 4,
    Lost = 5
}

/// <summary>
/// Physical archival item with barcode, location, condition, and digital link.
/// </summary>
public class PhysicalItem : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeFormat { get; set; } = "Code128";
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public PhysicalItemType ItemType { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? DigitalDocumentId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? RetentionPolicyId { get; set; }
    public Guid? OwnerStructureId { get; set; }
    public ItemCondition Condition { get; set; } = ItemCondition.Good;
    public DateTime? ItemDate { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public int? PageCount { get; set; }
    public string? Dimensions { get; set; }
    public CirculationStatus CirculationStatus { get; set; } = CirculationStatus.Available;
    public bool IsOnLegalHold { get; set; }
    public Guid? LegalHoldId { get; set; }
    public DateTime? LastConditionAssessment { get; set; }
    public string? ConditionNotes { get; set; }
    public string? CustomMetadata { get; set; }

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
