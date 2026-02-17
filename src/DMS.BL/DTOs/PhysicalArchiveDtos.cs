namespace DMS.BL.DTOs;

// --- Physical Location DTOs ---

public class PhysicalLocationDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Code { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string? Path { get; set; }
    public int Level { get; set; }
    public int? Capacity { get; set; }
    public int CurrentCount { get; set; }
    public string? EnvironmentalConditions { get; set; }
    public string? Coordinates { get; set; }
    public string? SecurityLevel { get; set; }
    public int SortOrder { get; set; }
    public List<PhysicalLocationDto> Children { get; set; } = new();
}

public class CreatePhysicalLocationDto
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Code { get; set; } = string.Empty;
    public string LocationType { get; set; } = "Room";
    public int? Capacity { get; set; }
    public string? EnvironmentalConditions { get; set; }
    public string? Coordinates { get; set; }
    public string? SecurityLevel { get; set; }
    public int SortOrder { get; set; }
}

// --- Physical Item DTOs ---

public class PhysicalItemDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeFormat { get; set; } = "Code128";
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? DigitalDocumentId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? RetentionPolicyId { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime? ItemDate { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public int? PageCount { get; set; }
    public string? Dimensions { get; set; }
    public string CirculationStatus { get; set; } = string.Empty;
    public bool IsOnLegalHold { get; set; }
    public DateTime? LastConditionAssessment { get; set; }
    public string? ConditionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePhysicalItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string ItemType { get; set; } = "Document";
    public Guid? LocationId { get; set; }
    public Guid? DigitalDocumentId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? RetentionPolicyId { get; set; }
    public string Condition { get; set; } = "Good";
    public DateTime? ItemDate { get; set; }
    public DateTime? DateRangeStart { get; set; }
    public DateTime? DateRangeEnd { get; set; }
    public int? PageCount { get; set; }
    public string? Dimensions { get; set; }
}

public class MovePhysicalItemDto
{
    public Guid NewLocationId { get; set; }
}

public class UpdateConditionDto
{
    public string Condition { get; set; } = "Good";
    public string? Notes { get; set; }
}

// --- Accession DTOs ---

public class AccessionRequestDto
{
    public Guid Id { get; set; }
    public string AccessionNumber { get; set; } = string.Empty;
    public Guid? SourceStructureId { get; set; }
    public Guid? TargetLocationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTime? RecordsDateFrom { get; set; }
    public DateTime? RecordsDateTo { get; set; }
    public DateTime? RequestedTransferDate { get; set; }
    public DateTime? ActualTransferDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AccessionRequestItemDto> Items { get; set; } = new();
}

public class CreateAccessionRequestDto
{
    public Guid? SourceStructureId { get; set; }
    public Guid? TargetLocationId { get; set; }
    public DateTime? RecordsDateFrom { get; set; }
    public DateTime? RecordsDateTo { get; set; }
    public DateTime? RequestedTransferDate { get; set; }
}

public class AccessionRequestItemDto
{
    public Guid Id { get; set; }
    public Guid? PhysicalItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
}

public class AddAccessionItemDto
{
    public Guid? PhysicalItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Document";
    public Guid? ClassificationId { get; set; }
    public string? Notes { get; set; }
}

public class ReviewAccessionDto
{
    public string? Notes { get; set; }
}

// --- Circulation DTOs ---

public class CirculationRecordDto
{
    public Guid Id { get; set; }
    public Guid PhysicalItemId { get; set; }
    public string? PhysicalItemTitle { get; set; }
    public string? PhysicalItemBarcode { get; set; }
    public Guid BorrowerId { get; set; }
    public string? BorrowerName { get; set; }
    public string? Purpose { get; set; }
    public DateTime CheckedOutAt { get; set; }
    public DateTime DueDate { get; set; }
    public int RenewalCount { get; set; }
    public int MaxRenewals { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string ConditionAtCheckout { get; set; } = string.Empty;
    public string? ConditionAtReturn { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CheckOutPhysicalItemDto
{
    public Guid PhysicalItemId { get; set; }
    public Guid BorrowerId { get; set; }
    public Guid? BorrowerStructureId { get; set; }
    public string? Purpose { get; set; }
    public int DueDays { get; set; } = 14;
}

public class ReturnPhysicalItemDto
{
    public string ConditionAtReturn { get; set; } = "Good";
    public string? ConditionNotes { get; set; }
}

// --- Custody Transfer DTOs ---

public class CustodyTransferDto
{
    public Guid Id { get; set; }
    public Guid PhysicalItemId { get; set; }
    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public string TransferType { get; set; } = string.Empty;
    public string? ConditionAtTransfer { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime TransferredAt { get; set; }
    public string? EntryHash { get; set; }
}

public class CreateCustodyTransferDto
{
    public Guid? ToUserId { get; set; }
    public Guid? ToLocationId { get; set; }
    public string TransferType { get; set; } = "InternalMove";
    public string? ConditionAtTransfer { get; set; }
}
