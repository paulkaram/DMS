namespace DMS.BL.DTOs;

public class CreateRetentionPolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RetentionDays { get; set; }
    public string ExpirationAction { get; set; } = "Review";
    public bool NotifyBeforeExpiration { get; set; } = true;
    public int NotificationDays { get; set; } = 30;
    public Guid? FolderId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool InheritToSubfolders { get; set; } = true;
    public bool IsLegalHold { get; set; } = false;
}

public class UpdateRetentionPolicyRequest : CreateRetentionPolicyRequest
{
    public bool IsActive { get; set; } = true;
}

public class ApproveRetentionRequest
{
    public string? Notes { get; set; }
}

public class HoldRequest
{
    public string? Notes { get; set; }
}
