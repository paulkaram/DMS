namespace DMS.BL.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Extension { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public int CurrentVersion { get; set; }
    public int CurrentMajorVersion { get; set; }
    public int CurrentMinorVersion { get; set; }
    public bool IsCheckedOut { get; set; }
    public Guid? CheckedOutBy { get; set; }
    public string? CheckedOutByName { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? ImportanceId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public Guid? ContentTypeId { get; set; }
    public string? ContentTypeName { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool HasPassword { get; set; }
}

public class DocumentVersionDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public long Size { get; set; }
    public string? Comment { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // ISO 15489 Major/Minor versioning
    public string VersionType { get; set; } = "Minor";
    public string? VersionLabel { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public bool IsContentChanged { get; set; }
    public bool IsMetadataChanged { get; set; }
    public string? ChangeDescription { get; set; }

    // Content information
    public string? ContentType { get; set; }
    public string? OriginalFileName { get; set; }
    public string? IntegrityHash { get; set; }
}

public class CreateDocumentDto
{
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? ImportanceId { get; set; }
    public Guid? DocumentTypeId { get; set; }
}

public class UpdateDocumentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? ImportanceId { get; set; }
    public Guid? DocumentTypeId { get; set; }
}

/// <summary>
/// DTO for check-in operation following ISO 15489 and SharePoint-like behavior.
/// </summary>
public class CheckInDto
{
    /// <summary>
    /// Optional comment describing changes in this version.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Type of version to create: Minor (1.0→1.1), Major (1.0→2.0), or Overwrite (replace current).
    /// </summary>
    public CheckInType CheckInType { get; set; } = CheckInType.Minor;

    /// <summary>
    /// If true, keeps document checked out after check-in (for continued editing).
    /// </summary>
    public bool KeepCheckedOut { get; set; } = false;

    /// <summary>
    /// Optional description of the changes made in this version.
    /// </summary>
    public string? ChangeDescription { get; set; }
}

/// <summary>
/// Type of check-in operation (SharePoint-style versioning).
/// </summary>
public enum CheckInType
{
    /// <summary>
    /// Create a minor version (1.0 → 1.1). Used for draft updates.
    /// </summary>
    Minor = 0,

    /// <summary>
    /// Create a major version (1.0 → 2.0). Used for published releases.
    /// </summary>
    Major = 1,

    /// <summary>
    /// Overwrite the current version (no new version created).
    /// </summary>
    Overwrite = 2
}

public class MoveDocumentDto
{
    public Guid NewFolderId { get; set; }
}

public class CopyDocumentDto
{
    public Guid TargetFolderId { get; set; }
    public string? NewName { get; set; }
}
