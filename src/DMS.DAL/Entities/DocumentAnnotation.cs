namespace DMS.DAL.Entities;

public class DocumentAnnotation
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public string AnnotationData { get; set; } = string.Empty;
    public int VersionNumber { get; set; } = 1;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation/display properties
    public string? CreatedByName { get; set; }
}

public class SavedSignature
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SignatureData { get; set; } = string.Empty;
    public string SignatureType { get; set; } = "drawn";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}
