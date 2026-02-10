using DMS.DAL.Data;

namespace DMS.DAL.Entities;

/// <summary>
/// Represents a folder structure template that can be applied to create folder hierarchies
/// </summary>
public class FolderTemplate : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation properties (populated by repository)
    public List<FolderTemplateNode> Nodes { get; set; } = new();
    public string? CreatedByName { get; set; }
    public int UsageCount { get; set; }
}
