namespace DMS.BL.DTOs;

public class WorkflowStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateWorkflowStatusRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}

public class UpdateWorkflowStatusRequest
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
