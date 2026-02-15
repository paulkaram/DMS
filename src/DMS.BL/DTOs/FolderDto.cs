namespace DMS.BL.DTOs;

public class FolderDto
{
    public Guid Id { get; set; }
    public Guid CabinetId { get; set; }
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
    public bool BreakInheritance { get; set; }
    public int AccessMode { get; set; }
    public Guid? PrivacyLevelId { get; set; }
    public string? PrivacyLevelName { get; set; }
    public string? PrivacyLevelColor { get; set; }
    public int? PrivacyLevelValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public List<FolderDto> Children { get; set; } = new();
}

public class CreateFolderDto
{
    public Guid CabinetId { get; set; }
    public Guid? ParentFolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AccessMode { get; set; } = 0;
    public Guid? PrivacyLevelId { get; set; }
}

public class UpdateFolderDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool BreakInheritance { get; set; }
    public int AccessMode { get; set; }
    public Guid? PrivacyLevelId { get; set; }
}

public class MoveFolderDto
{
    public Guid? NewParentFolderId { get; set; }
    public Guid? NewCabinetId { get; set; }
}

public class PreviewTemplateRequest
{
    public Guid TemplateId { get; set; }
}
