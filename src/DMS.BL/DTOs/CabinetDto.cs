namespace DMS.BL.DTOs;

public class CabinetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool BreakInheritance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

public class CreateCabinetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCabinetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool BreakInheritance { get; set; }
}
