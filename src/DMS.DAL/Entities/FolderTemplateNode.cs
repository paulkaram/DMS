namespace DMS.DAL.Entities;

/// <summary>
/// Represents a folder node within a folder template hierarchy
/// </summary>
public class FolderTemplateNode
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid? ParentNodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ContentTypeId { get; set; }
    public int SortOrder { get; set; }
    public bool BreakContentTypeInheritance { get; set; }

    // Navigation properties (populated by repository)
    public string? ContentTypeName { get; set; }
    public List<FolderTemplateNode> Children { get; set; } = new();
}
