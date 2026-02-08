namespace DMS.DAL.Entities;

/// <summary>
/// Defines document retention policies for compliance and records management
/// </summary>
public class RetentionPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Retention period in days (0 = permanent)
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Action to take when retention period expires: Archive, Delete, Review, Notify
    /// </summary>
    public string ExpirationAction { get; set; } = "Review";

    /// <summary>
    /// Whether to notify owners before expiration
    /// </summary>
    public bool NotifyBeforeExpiration { get; set; } = true;

    /// <summary>
    /// Days before expiration to send notification
    /// </summary>
    public int NotificationDays { get; set; } = 30;

    /// <summary>
    /// Folder to apply this policy to (null = can be assigned manually)
    /// </summary>
    public Guid? FolderId { get; set; }

    /// <summary>
    /// Classification that triggers this policy
    /// </summary>
    public Guid? ClassificationId { get; set; }

    /// <summary>
    /// Document type that triggers this policy
    /// </summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Whether documents under this policy require approval before action
    /// </summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>
    /// Whether to inherit policy to subfolders
    /// </summary>
    public bool InheritToSubfolders { get; set; } = true;

    /// <summary>
    /// Whether this is a legal hold (prevents deletion)
    /// </summary>
    public bool IsLegalHold { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation properties for display
    public string? FolderName { get; set; }
    public string? ClassificationName { get; set; }
    public string? DocumentTypeName { get; set; }
}

/// <summary>
/// Tracks retention status for individual documents
/// </summary>
public class DocumentRetention
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid PolicyId { get; set; }

    /// <summary>
    /// Date when retention period started (usually document creation date)
    /// </summary>
    public DateTime RetentionStartDate { get; set; }

    /// <summary>
    /// Calculated expiration date
    /// </summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>
    /// Status: Active, PendingReview, Approved, Archived, Deleted, OnHold
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Whether notification has been sent
    /// </summary>
    public bool NotificationSent { get; set; } = false;

    /// <summary>
    /// Date when action was taken (archived/deleted)
    /// </summary>
    public DateTime? ActionDate { get; set; }

    /// <summary>
    /// User who approved the action
    /// </summary>
    public Guid? ApprovedBy { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedAt { get; set; }

    // Navigation properties
    public string? DocumentName { get; set; }
    public string? PolicyName { get; set; }
}
