namespace DMS.DAL.Entities;

public class FolderLink
{
    public Guid Id { get; set; }
    public Guid SourceFolderId { get; set; }
    public Guid TargetFolderId { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public string? SourceFolderName { get; set; }
    public string? TargetFolderName { get; set; }
    public string? TargetFolderPath { get; set; }
}
