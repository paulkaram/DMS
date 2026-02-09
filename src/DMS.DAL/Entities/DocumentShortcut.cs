namespace DMS.DAL.Entities;

public class DocumentShortcut
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid FolderId { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public string? DocumentName { get; set; }
    public string? FolderName { get; set; }
    public string? FolderPath { get; set; }
}
