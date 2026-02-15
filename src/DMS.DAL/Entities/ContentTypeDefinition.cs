using System.Text.Json.Serialization;
using DMS.DAL.Data;

namespace DMS.DAL.Entities;

/// <summary>
/// Defines a content type schema with custom metadata fields.
/// Can be applied to cabinets, folders, or individual documents.
/// Note: This was renamed from ContentTypeDefinition to ContentType in the database.
/// The class keeps the original name for backwards compatibility but maps to ContentTypes table.
/// </summary>
public class ContentTypeDefinition : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }

    /// <summary>
    /// Category for grouping content types (e.g., "Business", "Legal", "HR")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether this content type can be applied to folders
    /// </summary>
    public bool AllowOnFolders { get; set; } = true;

    /// <summary>
    /// Whether this content type can be applied to documents
    /// </summary>
    public bool AllowOnDocuments { get; set; } = true;

    /// <summary>
    /// If true, documents in a folder with this content type must use it
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// If true, this is the system-wide default content type used when no specific content type is assigned.
    /// Only ONE content type can have this flag set to true.
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation
    public List<ContentTypeField>? Fields { get; set; }
}

/// <summary>
/// Defines a field within a content type schema
/// </summary>
public class ContentTypeField
{
    public Guid Id { get; set; }
    public Guid ContentTypeId { get; set; }

    /// <summary>
    /// Internal field name (used for storage key)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Display label for the field
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field description / help text
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Field type: Text, TextArea, Number, Decimal, Date, DateTime, Boolean,
    /// Dropdown, MultiSelect, User, Lookup
    /// </summary>
    public string FieldType { get; set; } = "Text";

    /// <summary>
    /// Is this field required?
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Is this field read-only?
    /// </summary>
    public bool IsReadOnly { get; set; } = false;

    /// <summary>
    /// Is this field visible in list views?
    /// </summary>
    public bool ShowInList { get; set; } = false;

    /// <summary>
    /// Is this field searchable?
    /// </summary>
    public bool IsSearchable { get; set; } = true;

    /// <summary>
    /// Default value (JSON serialized)
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// For Dropdown/MultiSelect: Lookup name or JSON array of options
    /// For Number/Decimal: min,max values
    /// For Text: regex pattern, maxLength
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// For Dropdown/MultiSelect: Reference to Lookup table name
    /// </summary>
    public string? LookupName { get; set; }

    /// <summary>
    /// JSON array of options for Dropdown/MultiSelect when not using Lookup
    /// Format: [{"value": "val1", "label": "Label 1"}, ...]
    /// </summary>
    public string? Options { get; set; }

    /// <summary>
    /// Field display order
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Group name for organizing fields in the form
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// CSS class or column span for layout (1-12 grid)
    /// </summary>
    public int ColumnSpan { get; set; } = 12;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    [JsonIgnore]
    public ContentTypeDefinition? ContentType { get; set; }
}

/// <summary>
/// Stores actual metadata values for a document
/// </summary>
public class DocumentMetadata : IAuditable
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ContentTypeId { get; set; }
    public Guid FieldId { get; set; }

    /// <summary>
    /// Field name (denormalized for query performance)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// The actual value (stored as string, parsed based on field type)
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// For numeric values, stored separately for range queries
    /// </summary>
    public decimal? NumericValue { get; set; }

    /// <summary>
    /// For date values, stored separately for range queries
    /// </summary>
    public DateTime? DateValue { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Associates a content type with a folder (folder-level content type)
/// </summary>
public class FolderContentTypeAssignment
{
    public Guid Id { get; set; }
    public Guid FolderId { get; set; }
    public Guid ContentTypeId { get; set; }

    /// <summary>
    /// If true, all documents in this folder must have this content type
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// If true, this content type is the default selection for uploads
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// If true, child folders inherit this content type
    /// </summary>
    public bool InheritToChildren { get; set; } = true;

    /// <summary>
    /// Display order in the content type selection dropdown
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    [JsonIgnore]
    public ContentTypeDefinition? ContentType { get; set; }

    // Populated by queries (not from DB)
    public string? ContentTypeName { get; set; }
    public string? ContentTypeDescription { get; set; }
    public string? ContentTypeIcon { get; set; }
    public string? ContentTypeColor { get; set; }
}

/// <summary>
/// Associates a content type with a cabinet (cabinet-level content type)
/// Inherited by all folders in the cabinet unless BreakContentTypeInheritance is set
/// </summary>
public class CabinetContentTypeAssignment
{
    public Guid Id { get; set; }
    public Guid CabinetId { get; set; }
    public Guid ContentTypeId { get; set; }

    /// <summary>
    /// If true, all documents in this cabinet must have this content type
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// If true, this content type is the default selection for uploads
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// If true, all folders in this cabinet inherit this content type
    /// </summary>
    public bool InheritToChildren { get; set; } = true;

    /// <summary>
    /// Display order in the content type selection dropdown
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    [JsonIgnore]
    public ContentTypeDefinition? ContentType { get; set; }

    // Populated by queries (not from DB)
    public string? ContentTypeName { get; set; }
    public string? ContentTypeDescription { get; set; }
    public string? ContentTypeIcon { get; set; }
    public string? ContentTypeColor { get; set; }
}
