namespace DMS.DAL.DTOs;

/// <summary>
/// Represents a content type field definition
/// </summary>
public class ContentTypeFieldDto
{
    public Guid Id { get; set; }
    public Guid ContentTypeId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FieldType { get; set; } = "Text";
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool ShowInList { get; set; }
    public bool IsSearchable { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string? LookupName { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
    public string? GroupName { get; set; }
    public int ColumnSpan { get; set; }
}

/// <summary>
/// Represents an effective content type for a folder after inheritance calculation.
/// Includes information about where the content type came from (direct, inherited, cabinet).
/// </summary>
public class EffectiveContentTypeDto
{
    public Guid ContentTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }

    /// <summary>
    /// Whether documents must use this content type
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether this is the default content type for uploads
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Display order in the selection dropdown
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Source of this assignment: "Direct", "Inherited", "Cabinet"
    /// </summary>
    public string Source { get; set; } = "Direct";

    /// <summary>
    /// Name of the source (parent folder name or cabinet name if inherited)
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// ID of the source entity (folder ID or cabinet ID)
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Content type fields for rendering the metadata form
    /// </summary>
    public List<ContentTypeFieldDto>? Fields { get; set; }
}

/// <summary>
/// Summary of content type information for a folder
/// </summary>
public class FolderContentTypeInfoDto
{
    public Guid FolderId { get; set; }
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// Whether any content type is marked as required
    /// </summary>
    public bool HasRequiredContentTypes { get; set; }

    /// <summary>
    /// Whether any content types are configured for this folder
    /// </summary>
    public bool HasContentTypes { get; set; }

    /// <summary>
    /// The default content type ID (if any)
    /// </summary>
    public Guid? DefaultContentTypeId { get; set; }

    /// <summary>
    /// The default content type name (if any)
    /// </summary>
    public string? DefaultContentTypeName { get; set; }

    /// <summary>
    /// Total number of available content types
    /// </summary>
    public int TotalContentTypes { get; set; }

    /// <summary>
    /// Whether this folder breaks content type inheritance
    /// </summary>
    public bool BreaksInheritance { get; set; }

    /// <summary>
    /// All effective content types for this folder
    /// </summary>
    public List<EffectiveContentTypeDto> ContentTypes { get; set; } = new();
}
