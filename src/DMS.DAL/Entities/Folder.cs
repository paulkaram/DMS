namespace DMS.DAL.Entities;

public class Folder
{
    public Guid Id { get; set; }
    public Guid CabinetId { get; set; }
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
    public bool BreakInheritance { get; set; }

    /// <summary>
    /// If true, this folder does not inherit content types from parent folders or cabinet
    /// </summary>
    public bool BreakContentTypeInheritance { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
