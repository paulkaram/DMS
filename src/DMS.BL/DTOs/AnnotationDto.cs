namespace DMS.BL.DTOs;

// =============================================
// Document Annotation DTOs
// =============================================
public class DocumentAnnotationDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public string AnnotationData { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class PageAnnotationData
{
    public int PageNumber { get; set; }
    public string AnnotationData { get; set; } = string.Empty;
}

public class SaveAnnotationsRequest
{
    public Guid DocumentId { get; set; }
    public List<PageAnnotationData> Pages { get; set; } = new();
}

// =============================================
// Saved Signature DTOs
// =============================================
public class SavedSignatureDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SignatureData { get; set; } = string.Empty;
    public string SignatureType { get; set; } = "drawn";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSignatureRequest
{
    public string Name { get; set; } = string.Empty;
    public string SignatureData { get; set; } = string.Empty;
    public string SignatureType { get; set; } = "drawn";
    public bool IsDefault { get; set; }
}
