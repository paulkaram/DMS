namespace DMS.BL.DTOs;

public class PrivacyLevelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
