namespace DMS.BL.DTOs;

// --- Search Request/Response DTOs ---

public class SearchDocumentsRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    // Facet filters
    public Guid? ClassificationId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? State { get; set; }
    public string? Extension { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    // Sorting
    public string SortBy { get; set; } = "relevance"; // relevance, name, date, size
    public bool SortDescending { get; set; } = true;

    // search_after token for deep pagination
    public string? SearchAfterToken { get; set; }
}

public class SearchResultDto
{
    public List<SearchResultItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? SearchAfterToken { get; set; }
    public List<FacetGroupDto> Facets { get; set; } = new();
    public double ElapsedMs { get; set; }
}

public class SearchResultItemDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = "Document";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; }
    public string? State { get; set; }
    public string? ClassificationName { get; set; }
    public string? DocumentTypeName { get; set; }
    public string? FolderPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public List<string> Highlights { get; set; } = new();
    public double Score { get; set; }
}

public class FacetGroupDto
{
    public string Field { get; set; } = string.Empty;
    public List<FacetValueDto> Values { get; set; } = new();
}

public class FacetValueDto
{
    public string Value { get; set; } = string.Empty;
    public string? Label { get; set; }
    public int Count { get; set; }
}

public class SearchHealthDto
{
    public bool IsAvailable { get; set; }
    public string Provider { get; set; } = string.Empty; // "OpenSearch" or "SQL"
    public string? ClusterName { get; set; }
    public string? ClusterStatus { get; set; }
    public int DocumentCount { get; set; }
    public int PendingIndexCount { get; set; }
}

// --- Document Index Model (what gets indexed in OpenSearch) ---

public class DocumentIndexModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; }
    public string? ContentType { get; set; }
    public string? State { get; set; }
    public string? OcrText { get; set; }

    // Classification & type
    public Guid? ClassificationId { get; set; }
    public string? ClassificationName { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public string? DocumentTypeName { get; set; }

    // Location
    public Guid FolderId { get; set; }
    public string? FolderPath { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Security fields for permission-aware filtering
    public List<Guid> PermittedUserIds { get; set; } = new();
    public List<Guid> PermittedRoleIds { get; set; } = new();
    public List<Guid> PermittedStructureIds { get; set; } = new();

    // Privacy level
    public int? PrivacyLevelId { get; set; }
}
