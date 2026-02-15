namespace DMS.BL.DTOs;

/// <summary>
/// DTO representing the current working copy (draft) state during checkout.
/// </summary>
public class WorkingCopyDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid CheckedOutBy { get; set; }
    public string? CheckedOutByName { get; set; }
    public DateTime CheckedOutAt { get; set; }

    // Draft file information
    public bool HasDraftFile { get; set; }
    public string? DraftFileName { get; set; }
    public long? DraftSize { get; set; }
    public string? DraftContentType { get; set; }

    // Draft property changes
    public string? DraftName { get; set; }
    public string? DraftDescription { get; set; }
    public Guid? DraftClassificationId { get; set; }
    public Guid? DraftImportanceId { get; set; }
    public Guid? DraftDocumentTypeId { get; set; }
    public DateTime? DraftExpiryDate { get; set; }
    public bool DraftExpiryDateChanged { get; set; }
    public Guid? DraftPrivacyLevelId { get; set; }
    public bool DraftPrivacyLevelChanged { get; set; }

    // Draft metadata (parsed from JSON)
    public List<WorkingCopyMetadataItem>? DraftMetadata { get; set; }

    // Status
    public DateTime? LastModifiedAt { get; set; }
    public bool AutoSaveEnabled { get; set; }
    public bool HasUnsavedChanges { get; set; }
}

/// <summary>
/// Individual metadata field in a working copy.
/// </summary>
public class WorkingCopyMetadataItem
{
    public Guid FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
}

/// <summary>
/// DTO for saving draft changes to a working copy.
/// </summary>
public class SaveWorkingCopyDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? ImportanceId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool? ExpiryDateChanged { get; set; }
    public Guid? PrivacyLevelId { get; set; }
    public bool? PrivacyLevelChanged { get; set; }
    public List<WorkingCopyMetadataItem>? Metadata { get; set; }
}

/// <summary>
/// DTO for checkout operation options.
/// </summary>
public class CheckOutDto
{
    /// <summary>
    /// Optional: Force checkout even if document is checked out by another user (admin only).
    /// </summary>
    public bool ForceCheckout { get; set; } = false;

    /// <summary>
    /// Optional comment explaining why the document is being checked out.
    /// </summary>
    public string? CheckoutReason { get; set; }
}

/// <summary>
/// DTO for version restoration operation.
/// </summary>
public class RestoreVersionDto
{
    /// <summary>
    /// Comment explaining why this version is being restored.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Whether to restore file content from the version.
    /// </summary>
    public bool RestoreContent { get; set; } = true;

    /// <summary>
    /// Whether to restore metadata from the version.
    /// </summary>
    public bool RestoreMetadata { get; set; } = true;
}

/// <summary>
/// DTO for admin force-discard checkout operation.
/// </summary>
public class ForceDiscardCheckoutRequest
{
    /// <summary>
    /// Reason for force discarding the checkout (required for audit trail).
    /// </summary>
    public string? Reason { get; set; }
}
