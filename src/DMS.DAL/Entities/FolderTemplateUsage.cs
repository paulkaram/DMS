namespace DMS.DAL.Entities;

/// <summary>
/// Tracks when and where folder templates have been applied
/// </summary>
public class FolderTemplateUsage
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? RootFolderId { get; set; }
    public Guid CabinetId { get; set; }
    public Guid AppliedBy { get; set; }
    public DateTime AppliedAt { get; set; }
    public int FoldersCreated { get; set; }

    // Navigation properties (populated by repository)
    public string? TemplateName { get; set; }
    public string? FolderName { get; set; }
    public string? AppliedByName { get; set; }
}
