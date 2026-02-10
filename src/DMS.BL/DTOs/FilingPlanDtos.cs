namespace DMS.BL.DTOs;

public class CreateFilingPlanRequest
{
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Pattern { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? DocumentTypeId { get; set; }
}

public class UpdateFilingPlanRequest : CreateFilingPlanRequest
{
    public bool IsActive { get; set; } = true;
}
