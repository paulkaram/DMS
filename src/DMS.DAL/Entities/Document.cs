namespace DMS.DAL.Entities;

/// <summary>
/// Represents a document in the DMS with ISO 15489/23081 compliant metadata.
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Extension { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string? StoragePath { get; set; }
    public int CurrentVersion { get; set; } = 1;

    /// <summary>
    /// Current major version number (e.g., 1, 2, 3).
    /// </summary>
    public int CurrentMajorVersion { get; set; } = 1;

    /// <summary>
    /// Current minor version number (e.g., 0, 1, 2).
    /// </summary>
    public int CurrentMinorVersion { get; set; } = 0;

    /// <summary>
    /// Reference to the current version record.
    /// </summary>
    public Guid? CurrentVersionId { get; set; }

    public bool IsCheckedOut { get; set; }
    public Guid? CheckedOutBy { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? ImportanceId { get; set; }
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Reference to the content type (metadata schema) used by this document.
    /// This links to ContentTypes table (renamed from ContentTypeDefinitions).
    /// </summary>
    public Guid? ContentTypeId { get; set; }

    // ISO 15489/27001: Integrity verification
    /// <summary>
    /// SHA-256 cryptographic hash of the current version's content for integrity verification.
    /// </summary>
    public string? IntegrityHash { get; set; }

    /// <summary>
    /// Algorithm used for integrity hash (e.g., "SHA256").
    /// </summary>
    public string? HashAlgorithm { get; set; }

    /// <summary>
    /// Timestamp of last successful integrity verification.
    /// </summary>
    public DateTime? IntegrityVerifiedAt { get; set; }

    // ISO 15489: Retention and holds
    /// <summary>
    /// Associated retention policy ID.
    /// </summary>
    public Guid? RetentionPolicyId { get; set; }

    /// <summary>
    /// Whether document is under legal hold (prevents deletion).
    /// </summary>
    public bool IsOnLegalHold { get; set; } = false;

    /// <summary>
    /// ID of the legal hold matter/case.
    /// </summary>
    public Guid? LegalHoldId { get; set; }

    /// <summary>
    /// Date when legal hold was applied.
    /// </summary>
    public DateTime? LegalHoldAppliedAt { get; set; }

    /// <summary>
    /// User who applied the legal hold.
    /// </summary>
    public Guid? LegalHoldAppliedBy { get; set; }

    // ISO 15489: Content categorization
    /// <summary>
    /// Whether this is an original authoritative record (true) or a derivative (false).
    /// </summary>
    public bool IsOriginalRecord { get; set; } = true;

    /// <summary>
    /// For derivatives: ID of the source document this was derived from.
    /// </summary>
    public Guid? SourceDocumentId { get; set; }

    /// <summary>
    /// Content category: Original, OCRText, Thumbnail, Preview, Rendition, Migration.
    /// </summary>
    public string ContentCategory { get; set; } = "Original";

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Document with resolved user/content type names from JOIN queries.
/// </summary>
public class DocumentWithNames : Document
{
    public string? CreatedByName { get; set; }
    public string? CheckedOutByName { get; set; }
    public string? ContentTypeName { get; set; }
}
