namespace DMS.DAL.Entities;

/// <summary>
/// Represents draft changes during checkout.
/// Enables "save draft" and "discard checkout" functionality per ISO 15489.
/// All edits during checkout are stored here until check-in or discard.
/// </summary>
public class DocumentWorkingCopy
{
    public Guid Id { get; set; }

    /// <summary>
    /// The document being edited.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// User who has the document checked out.
    /// </summary>
    public Guid CheckedOutBy { get; set; }

    /// <summary>
    /// When the checkout was initiated.
    /// </summary>
    public DateTime CheckedOutAt { get; set; }

    // Draft content (null if no file replacement)

    /// <summary>
    /// Storage path for the draft file content.
    /// Null if user hasn't uploaded a replacement file.
    /// </summary>
    public string? DraftStoragePath { get; set; }

    /// <summary>
    /// Size of the draft file in bytes.
    /// </summary>
    public long? DraftSize { get; set; }

    /// <summary>
    /// MIME type of the draft file.
    /// </summary>
    public string? DraftContentType { get; set; }

    /// <summary>
    /// Original filename of the draft upload.
    /// </summary>
    public string? DraftOriginalFileName { get; set; }

    /// <summary>
    /// Integrity hash of the draft file.
    /// </summary>
    public string? DraftIntegrityHash { get; set; }

    // Draft metadata (JSON serialized for flexibility during editing)

    /// <summary>
    /// JSON-serialized metadata changes.
    /// Format: [{ "fieldId": "...", "fieldName": "...", "value": "..." }, ...]
    /// </summary>
    public string? DraftMetadataJson { get; set; }

    // Draft document properties

    /// <summary>
    /// Draft document name (if changed).
    /// </summary>
    public string? DraftName { get; set; }

    /// <summary>
    /// Draft document description (if changed).
    /// </summary>
    public string? DraftDescription { get; set; }

    /// <summary>
    /// Draft classification ID (if changed).
    /// </summary>
    public Guid? DraftClassificationId { get; set; }

    /// <summary>
    /// Draft importance ID (if changed).
    /// </summary>
    public Guid? DraftImportanceId { get; set; }

    /// <summary>
    /// Draft document type ID (if changed).
    /// </summary>
    public Guid? DraftDocumentTypeId { get; set; }

    // Tracking

    /// <summary>
    /// When the working copy was last modified.
    /// </summary>
    public DateTime? LastModifiedAt { get; set; }

    /// <summary>
    /// Whether auto-save is enabled for this working copy.
    /// </summary>
    public bool AutoSaveEnabled { get; set; } = true;
}
