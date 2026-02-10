namespace DMS.BL.DTOs;

/// <summary>
/// Represents a content type with its fields for API responses
/// </summary>
public class ContentTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }
    public bool AllowOnFolders { get; set; }
    public bool AllowOnDocuments { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSystemDefault { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int FieldCount { get; set; }
    public List<DMS.DAL.DTOs.ContentTypeFieldDto>? Fields { get; set; }
}

/// <summary>
/// Request to assign a content type to a folder or cabinet
/// </summary>
public class AssignContentTypeRequest
{
    public Guid ContentTypeId { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public bool InheritToChildren { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Request to update a content type assignment
/// </summary>
public class UpdateContentTypeAssignmentRequest
{
    public bool IsRequired { get; set; }
    public bool IsDefault { get; set; }
    public bool InheritToChildren { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Response for a content type assignment operation
/// </summary>
public class ContentTypeAssignmentDto
{
    public Guid Id { get; set; }
    public Guid ContentTypeId { get; set; }
    public string ContentTypeName { get; set; } = string.Empty;
    public string? ContentTypeDescription { get; set; }
    public string? ContentTypeIcon { get; set; }
    public string? ContentTypeColor { get; set; }
    public bool IsRequired { get; set; }
    public bool IsDefault { get; set; }
    public bool InheritToChildren { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateContentTypeRequest
{
    public string Extension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Icon { get; set; }
    public bool AllowPreview { get; set; } = true;
    public bool AllowThumbnail { get; set; } = true;
    public int MaxFileSizeMB { get; set; } = 100;
}

public class UpdateContentTypeRequest : CreateContentTypeRequest
{
    public bool IsActive { get; set; } = true;
}
