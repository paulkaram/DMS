namespace DMS.BL.DTOs;

/// <summary>
/// DTO for comparing two document versions (content and metadata differences).
/// </summary>
public class VersionComparisonDto
{
    public Guid DocumentId { get; set; }
    public VersionSummaryDto SourceVersion { get; set; } = null!;
    public VersionSummaryDto TargetVersion { get; set; } = null!;
    public bool ContentChanged { get; set; }
    public bool MetadataChanged { get; set; }
    public long SizeDifference { get; set; }
    public List<MetadataDiffItem> MetadataDifferences { get; set; } = new();
}

/// <summary>
/// Summary information about a version for comparison display.
/// </summary>
public class VersionSummaryDto
{
    public Guid VersionId { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public string VersionType { get; set; } = "Minor";
    public long Size { get; set; }
    public string? IntegrityHash { get; set; }
    public string? ContentType { get; set; }
    public string? Comment { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Individual metadata field difference between versions.
/// </summary>
public class MetadataDiffItem
{
    public Guid FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DiffType DiffType { get; set; }
}

/// <summary>
/// Type of change detected in metadata comparison.
/// </summary>
public enum DiffType
{
    Unchanged = 0,
    Added = 1,
    Removed = 2,
    Modified = 3
}
