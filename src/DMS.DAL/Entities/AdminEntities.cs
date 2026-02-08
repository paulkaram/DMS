namespace DMS.DAL.Entities;

/// <summary>
/// Bookmark templates for document generation
/// </summary>
public class Bookmark
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Placeholder { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultValue { get; set; }
    public string DataType { get; set; } = "Text"; // Text, Date, Number, User, Lookup
    public string? LookupName { get; set; }
    public bool IsSystem { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Case management for grouping related documents
/// </summary>
public class Case
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Open"; // Open, InProgress, OnHold, Closed, Archived
    public string? Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? FolderId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation
    public string? AssignedToUserName { get; set; }
    public string? FolderName { get; set; }
}

/// <summary>
/// External service endpoints configuration
/// </summary>
public class ServiceEndpoint
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EndpointType { get; set; } = "REST"; // REST, SOAP, GraphQL, WebSocket
    public string? AuthType { get; set; } // None, Basic, Bearer, OAuth, ApiKey
    public string? AuthConfig { get; set; } // JSON config for auth
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
    public string? Headers { get; set; } // JSON headers
    public bool IsActive { get; set; } = true;
    public DateTime? LastHealthCheck { get; set; }
    public string? LastHealthStatus { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Export configuration profiles
/// </summary>
public class ExportConfig
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ExportFormat { get; set; } = "PDF"; // PDF, ZIP, CSV, XML
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeVersions { get; set; } = false;
    public bool IncludeAuditTrail { get; set; } = false;
    public bool FlattenFolders { get; set; } = false;
    public string? NamingPattern { get; set; }
    public string? WatermarkText { get; set; }
    public int? MaxFileSizeMB { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Naming conventions for documents and folders
/// </summary>
public class NamingConvention
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Pattern { get; set; } = string.Empty; // Pattern with placeholders like {date}, {type}, {seq}
    public string AppliesTo { get; set; } = "Document"; // Document, Folder, Both
    public Guid? FolderId { get; set; } // If null, applies globally
    public Guid? DocumentTypeId { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool AutoGenerate { get; set; } = false;
    public string? Separator { get; set; } = "-";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation
    public string? FolderName { get; set; }
    public string? DocumentTypeName { get; set; }
}

/// <summary>
/// Organization folder templates for standard structures
/// </summary>
public class OrganizationTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Structure { get; set; } = "[]"; // JSON array of folder structure
    public string? DefaultPermissions { get; set; } // JSON permissions config
    public bool IncludeContentTypes { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Permission level definitions
/// </summary>
public class PermissionLevelDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Level { get; set; } // Bitmask value: 1=Read, 2=Write, 4=Delete, 8=Admin, etc.
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = false;
    public bool CanDelete { get; set; } = false;
    public bool CanAdmin { get; set; } = false;
    public bool CanShare { get; set; } = false;
    public bool CanExport { get; set; } = false;
    public bool IsSystem { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Purpose/reason for document actions (for compliance)
/// </summary>
public class Purpose
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PurposeType { get; set; } = "General"; // General, Access, Download, Print, Share, Export
    public bool RequiresJustification { get; set; } = false;
    public bool RequiresApproval { get; set; } = false;
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Scan/OCR configuration profiles
/// </summary>
public class ScanConfig
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Resolution { get; set; } = 300; // DPI
    public string ColorMode { get; set; } = "Color"; // Color, Grayscale, BlackWhite
    public string OutputFormat { get; set; } = "PDF"; // PDF, TIFF, PNG, JPEG
    public bool EnableOCR { get; set; } = true;
    public string OcrLanguage { get; set; } = "eng";
    public bool AutoDeskew { get; set; } = true;
    public bool AutoCrop { get; set; } = true;
    public bool RemoveBlankPages { get; set; } = false;
    public int? CompressionQuality { get; set; } = 85;
    public Guid? TargetFolderId { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation
    public string? TargetFolderName { get; set; }
}

/// <summary>
/// Search configuration and saved searches
/// </summary>
public class SearchConfig
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SearchType { get; set; } = "FullText"; // FullText, Metadata, Combined
    public string? DefaultFields { get; set; } // JSON array of searchable fields
    public string? Filters { get; set; } // JSON default filters
    public bool IncludeContent { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeVersions { get; set; } = false;
    public bool FuzzyMatch { get; set; } = false;
    public int MaxResults { get; set; } = 100;
    public string? SortField { get; set; }
    public string SortDirection { get; set; } = "desc";
    public bool IsGlobal { get; set; } = false; // Available to all users
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
