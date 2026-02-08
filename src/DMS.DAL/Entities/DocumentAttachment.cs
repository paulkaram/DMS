namespace DMS.DAL.Entities;

public class DocumentAttachment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string? StoragePath { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation/display properties
    public string? CreatedByName { get; set; }
}
