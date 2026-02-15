namespace DMS.BL.DTOs;

public class StructureDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Type { get; set; } = string.Empty;  // Ministry, Department, Division, Section, Unit
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? Path { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; }
    public int? SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int ChildCount { get; set; }
}

public class CreateStructureDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int? SortOrder { get; set; }
}

public class UpdateStructureDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }
}

public class StructureMemberDto
{
    public Guid Id { get; set; }
    public Guid StructureId { get; set; }
    public string? StructureName { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserDisplayName { get; set; }
    public string? Position { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class AddStructureMemberDto
{
    public Guid UserId { get; set; }
    public string? Position { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime? EndDate { get; set; }
}

public class StructureTreeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
    public List<StructureTreeDto> Children { get; set; } = new();
}
