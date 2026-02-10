namespace DMS.BL.DTOs;

public class FolderTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedByName { get; set; }
    public int UsageCount { get; set; }
    public List<FolderTemplateNodeDto> Nodes { get; set; } = new();
}

public class FolderTemplateNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentNodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ContentTypeId { get; set; }
    public string? ContentTypeName { get; set; }
    public int SortOrder { get; set; }
    public bool BreakContentTypeInheritance { get; set; }
    public List<FolderTemplateNodeDto> Children { get; set; } = new();
}

public class CreateFolderTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }
    public List<CreateTemplateNodeDto> Nodes { get; set; } = new();
}

public class UpdateFolderTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateTemplateNodeDto
{
    public Guid? ParentNodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ContentTypeId { get; set; }
    public int SortOrder { get; set; }
    public bool BreakContentTypeInheritance { get; set; }
    public List<CreateTemplateNodeDto> Children { get; set; } = new();
}

public class UpdateTemplateNodeDto
{
    public Guid? ParentNodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ContentTypeId { get; set; }
    public int SortOrder { get; set; }
    public bool BreakContentTypeInheritance { get; set; }
}

public class ApplyTemplateDto
{
    public Guid TemplateId { get; set; }
    public string? NamePrefix { get; set; }
    public bool OverwriteExisting { get; set; } = false;
}

public class ApplyTemplateResultDto
{
    public bool Success { get; set; }
    public int FoldersCreated { get; set; }
    public int FoldersSkipped { get; set; }
    public List<string> CreatedFolderPaths { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class FolderTemplateUsageDto
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string? TemplateName { get; set; }
    public Guid? RootFolderId { get; set; }
    public string? FolderName { get; set; }
    public Guid CabinetId { get; set; }
    public Guid AppliedBy { get; set; }
    public string? AppliedByName { get; set; }
    public DateTime AppliedAt { get; set; }
    public int FoldersCreated { get; set; }
}

public class DuplicateTemplateRequest
{
    public string NewName { get; set; } = string.Empty;
}
