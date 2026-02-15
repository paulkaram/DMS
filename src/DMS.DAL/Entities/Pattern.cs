using DMS.DAL.Data;

namespace DMS.DAL.Entities;

/// <summary>
/// Defines regex patterns for document matching, validation, and auto-filing
/// </summary>
public class Pattern : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Regex { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Pattern type: Naming, Filing, Validation, Search
    /// </summary>
    public string PatternType { get; set; } = "Naming";

    /// <summary>
    /// Target folder for auto-filing patterns
    /// </summary>
    public Guid? TargetFolderId { get; set; }

    /// <summary>
    /// Content type to apply when pattern matches
    /// </summary>
    public Guid? ContentTypeId { get; set; }

    /// <summary>
    /// Classification to apply when pattern matches
    /// </summary>
    public Guid? ClassificationId { get; set; }

    /// <summary>
    /// Document type to apply when pattern matches
    /// </summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>
    /// Priority for pattern matching (lower = higher priority)
    /// </summary>
    public int Priority { get; set; } = 100;

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation properties for display
    public string? TargetFolderName { get; set; }
    public string? ContentTypeName { get; set; }
    public string? ClassificationName { get; set; }
    public string? DocumentTypeName { get; set; }
}
