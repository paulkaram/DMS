namespace DMS.DAL.Entities;

public class FilingPlan
{
    public Guid Id { get; set; }
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Pattern { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public string? FolderName { get; set; }
    public string? ClassificationName { get; set; }
    public string? DocumentTypeName { get; set; }
}
