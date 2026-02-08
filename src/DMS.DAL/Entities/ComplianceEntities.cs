namespace DMS.DAL.Entities;

/// <summary>
/// ISO 15489 compliant disposal certificate for audit trail of permanent deletions.
/// Provides proof of compliant document destruction.
/// </summary>
public class DisposalCertificate
{
    public Guid Id { get; set; }

    /// <summary>
    /// Unique certificate number for reference.
    /// </summary>
    public string CertificateNumber { get; set; } = string.Empty;

    /// <summary>
    /// ID of the disposed document (retained for audit even after document deletion).
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Name of the disposed document at time of disposal.
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Full path/location of document at time of disposal.
    /// </summary>
    public string? DocumentPath { get; set; }

    /// <summary>
    /// Document classification at time of disposal.
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Retention policy that triggered disposal.
    /// </summary>
    public Guid? RetentionPolicyId { get; set; }

    /// <summary>
    /// Name of retention policy.
    /// </summary>
    public string? RetentionPolicyName { get; set; }

    /// <summary>
    /// Document creation date.
    /// </summary>
    public DateTime DocumentCreatedAt { get; set; }

    /// <summary>
    /// Date retention period started.
    /// </summary>
    public DateTime? RetentionStartDate { get; set; }

    /// <summary>
    /// Calculated expiration date.
    /// </summary>
    public DateTime? RetentionExpirationDate { get; set; }

    /// <summary>
    /// Disposal method: SoftDelete, HardDelete, CryptographicErasure, PhysicalDestruction.
    /// </summary>
    public string DisposalMethod { get; set; } = "HardDelete";

    /// <summary>
    /// Disposal date and time.
    /// </summary>
    public DateTime DisposedAt { get; set; }

    /// <summary>
    /// User who executed the disposal.
    /// </summary>
    public Guid DisposedBy { get; set; }

    /// <summary>
    /// Name of user who executed disposal.
    /// </summary>
    public string? DisposedByName { get; set; }

    /// <summary>
    /// User who approved the disposal (if approval required).
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    /// <summary>
    /// Name of approver.
    /// </summary>
    public string? ApprovedByName { get; set; }

    /// <summary>
    /// Date of approval.
    /// </summary>
    public DateTime? ApprovedAt { get; set; }

    /// <summary>
    /// Legal basis or authority for disposal.
    /// </summary>
    public string? LegalBasis { get; set; }

    /// <summary>
    /// Additional notes or justification.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// SHA-256 hash of document content at time of disposal (proof of what was destroyed).
    /// </summary>
    public string? ContentHashAtDisposal { get; set; }

    /// <summary>
    /// File size at time of disposal.
    /// </summary>
    public long FileSizeAtDisposal { get; set; }

    /// <summary>
    /// Number of versions disposed.
    /// </summary>
    public int VersionsDisposed { get; set; }

    /// <summary>
    /// Digital signature of certificate for tamper evidence.
    /// </summary>
    public string? CertificateSignature { get; set; }

    /// <summary>
    /// Whether disposal was verified complete.
    /// </summary>
    public bool DisposalVerified { get; set; }

    /// <summary>
    /// Verification timestamp.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// ISO 15489 Legal Hold entity for preserving documents during litigation or investigation.
/// </summary>
public class LegalHold
{
    public Guid Id { get; set; }

    /// <summary>
    /// Legal hold/matter reference number.
    /// </summary>
    public string HoldNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name/title of the legal matter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the legal matter or investigation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Case or matter reference (e.g., court case number).
    /// </summary>
    public string? CaseReference { get; set; }

    /// <summary>
    /// Legal counsel or authority requesting the hold.
    /// </summary>
    public string? RequestedBy { get; set; }

    /// <summary>
    /// Date hold was requested.
    /// </summary>
    public DateTime? RequestedAt { get; set; }

    /// <summary>
    /// Status: Active, Released, Expired.
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Date hold becomes effective.
    /// </summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>
    /// Date hold expires (null = indefinite).
    /// </summary>
    public DateTime? EffectiveUntil { get; set; }

    /// <summary>
    /// User who applied the hold.
    /// </summary>
    public Guid AppliedBy { get; set; }

    /// <summary>
    /// Date hold was applied in system.
    /// </summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>
    /// User who released the hold.
    /// </summary>
    public Guid? ReleasedBy { get; set; }

    /// <summary>
    /// Date hold was released.
    /// </summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>
    /// Reason for release.
    /// </summary>
    public string? ReleaseReason { get; set; }

    /// <summary>
    /// Notes and audit trail.
    /// </summary>
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Junction table linking legal holds to documents.
/// </summary>
public class LegalHoldDocument
{
    public Guid Id { get; set; }
    public Guid LegalHoldId { get; set; }
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Date document was placed under this hold.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// User who added document to hold.
    /// </summary>
    public Guid AddedBy { get; set; }

    /// <summary>
    /// Date document was released from this specific hold.
    /// </summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>
    /// User who released document from hold.
    /// </summary>
    public Guid? ReleasedBy { get; set; }

    /// <summary>
    /// Notes about why document was included.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public string? DocumentName { get; set; }
    public string? HoldName { get; set; }
}

/// <summary>
/// ISO 14721 (OAIS) Fixity verification log for periodic integrity checks.
/// </summary>
public class IntegrityVerificationLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// Document or version being verified.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Specific version verified (null = current version).
    /// </summary>
    public int? VersionNumber { get; set; }

    /// <summary>
    /// Expected hash from metadata.
    /// </summary>
    public string ExpectedHash { get; set; } = string.Empty;

    /// <summary>
    /// Computed hash during verification.
    /// </summary>
    public string ComputedHash { get; set; } = string.Empty;

    /// <summary>
    /// Hash algorithm used.
    /// </summary>
    public string HashAlgorithm { get; set; } = "SHA256";

    /// <summary>
    /// Whether verification passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Verification timestamp.
    /// </summary>
    public DateTime VerifiedAt { get; set; }

    /// <summary>
    /// Type: Manual, Scheduled, OnAccess, OnDownload.
    /// </summary>
    public string VerificationType { get; set; } = "Scheduled";

    /// <summary>
    /// User who initiated verification (null for scheduled).
    /// </summary>
    public Guid? VerifiedBy { get; set; }

    /// <summary>
    /// Error message if verification failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Action taken on failure: None, Quarantined, Restored, Notified.
    /// </summary>
    public string? ActionTaken { get; set; }
}

/// <summary>
/// ISO 14721 (OAIS) Preservation metadata for long-term digital preservation.
/// </summary>
public class PreservationMetadata
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }

    /// <summary>
    /// Original file format (PRONOM PUID if available).
    /// </summary>
    public string? FormatIdentifier { get; set; }

    /// <summary>
    /// Format name (e.g., "PDF/A-1b").
    /// </summary>
    public string? FormatName { get; set; }

    /// <summary>
    /// Format version.
    /// </summary>
    public string? FormatVersion { get; set; }

    /// <summary>
    /// Format registry (e.g., "PRONOM").
    /// </summary>
    public string? FormatRegistry { get; set; }

    /// <summary>
    /// Whether format is suitable for long-term preservation.
    /// </summary>
    public bool IsPreservationFormat { get; set; }

    /// <summary>
    /// Recommended migration target format if current is at risk.
    /// </summary>
    public string? MigrationTargetFormat { get; set; }

    /// <summary>
    /// Date format was identified.
    /// </summary>
    public DateTime IdentifiedAt { get; set; }

    /// <summary>
    /// Tool used for format identification.
    /// </summary>
    public string? IdentificationTool { get; set; }

    /// <summary>
    /// Creating application (if known).
    /// </summary>
    public string? CreatingApplication { get; set; }

    /// <summary>
    /// Original environment requirements.
    /// </summary>
    public string? EnvironmentRequirements { get; set; }

    /// <summary>
    /// Significant properties to preserve.
    /// </summary>
    public string? SignificantProperties { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Content category constants for ISO 15489 original vs derivative distinction.
/// </summary>
public static class ContentCategories
{
    public const string Original = "Original";
    public const string OCRText = "OCRText";
    public const string Thumbnail = "Thumbnail";
    public const string Preview = "Preview";
    public const string Rendition = "Rendition";
    public const string Migration = "Migration";
    public const string Redacted = "Redacted";
}

/// <summary>
/// Disposal method constants.
/// </summary>
public static class DisposalMethods
{
    public const string SoftDelete = "SoftDelete";
    public const string HardDelete = "HardDelete";
    public const string CryptographicErasure = "CryptographicErasure";
    public const string PhysicalDestruction = "PhysicalDestruction";
}

/// <summary>
/// Legal hold status constants.
/// </summary>
public static class LegalHoldStatus
{
    public const string Active = "Active";
    public const string Released = "Released";
    public const string Expired = "Expired";
}
