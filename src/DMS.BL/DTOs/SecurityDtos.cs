namespace DMS.BL.DTOs;

// --- Access Review DTOs ---

public class AccessReviewCampaignDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public Guid? ReviewerId { get; set; }
    public int TotalEntries { get; set; }
    public int CompletedEntries { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAccessReviewCampaignDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public Guid? ReviewerId { get; set; }
}

public class CampaignReviewEntryDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string? NodeName { get; set; }
    public int PermissionLevel { get; set; }
    public string? PermissionSource { get; set; }
    public string Decision { get; set; } = "Pending";
    public string? Comments { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class SubmitAccessReviewDto
{
    public string Decision { get; set; } = "Approved";
    public string? Comments { get; set; }
}

public class StalePermissionDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string? NodeName { get; set; }
    public int PermissionLevel { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int InactiveDays { get; set; }
}

// --- Effective Permissions DTOs ---

public class EffectivePermissionDetailDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public int EffectiveLevel { get; set; }
    public string EffectiveLevelName { get; set; } = string.Empty;
    public List<PermissionSourceDto> Sources { get; set; } = new();
}

public class PermissionSourceDto
{
    public string SourceType { get; set; } = string.Empty;
    public string? SourceName { get; set; }
    public int Level { get; set; }
    public bool IsInherited { get; set; }
    public string? InheritedFrom { get; set; }
}

public class UserAccessReportDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<UserAccessItemDto> AccessItems { get; set; } = new();
}

public class UserAccessItemDto
{
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string? NodeName { get; set; }
    public int EffectiveLevel { get; set; }
    public string EffectiveLevelName { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}
