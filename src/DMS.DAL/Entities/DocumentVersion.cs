namespace DMS.DAL.Entities;

/// <summary>
/// Represents an immutable version of a document per ISO 14721 (OAIS) preservation requirements.
/// </summary>
public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string? StoragePath { get; set; }
    public long Size { get; set; }
    public string? Comment { get; set; }

    // ISO 15489/27001: Integrity verification per version
    /// <summary>
    /// SHA-256 cryptographic hash of this version's content.
    /// </summary>
    public string? IntegrityHash { get; set; }

    /// <summary>
    /// Algorithm used for integrity hash (e.g., "SHA256").
    /// </summary>
    public string? HashAlgorithm { get; set; }

    /// <summary>
    /// Timestamp of last successful integrity verification for this version.
    /// </summary>
    public DateTime? IntegrityVerifiedAt { get; set; }

    // ISO 15489: Content categorization
    /// <summary>
    /// Content type/MIME type for this specific version.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Original filename when this version was uploaded.
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Whether this version is an original record or derivative.
    /// </summary>
    public bool IsOriginalRecord { get; set; } = true;

    /// <summary>
    /// Content category: Original, OCRText, Thumbnail, Preview, Rendition, Migration.
    /// </summary>
    public string ContentCategory { get; set; } = "Original";

    // ISO 15489: Major/Minor Versioning (SharePoint-like)

    /// <summary>
    /// Version type: "Major" or "Minor".
    /// Major versions (1.0, 2.0) are published releases.
    /// Minor versions (1.1, 1.2) are draft updates.
    /// </summary>
    public string VersionType { get; set; } = "Minor";

    /// <summary>
    /// Human-readable version label (e.g., "1.0", "1.1", "2.0").
    /// </summary>
    public string? VersionLabel { get; set; }

    /// <summary>
    /// Major version number component.
    /// </summary>
    public int MajorVersion { get; set; } = 1;

    /// <summary>
    /// Minor version number component.
    /// </summary>
    public int MinorVersion { get; set; } = 0;

    /// <summary>
    /// Whether the file content changed in this version.
    /// </summary>
    public bool IsContentChanged { get; set; } = true;

    /// <summary>
    /// Whether metadata changed in this version.
    /// </summary>
    public bool IsMetadataChanged { get; set; } = false;

    /// <summary>
    /// Reference to the previous version (for version chain).
    /// </summary>
    public Guid? PreviousVersionId { get; set; }

    /// <summary>
    /// Description of changes made in this version.
    /// </summary>
    public string? ChangeDescription { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
