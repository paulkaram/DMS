using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public enum AccessReviewStatus
{
    Open = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum AccessReviewDecision
{
    Pending = 0,
    Approved = 1,
    Revoked = 2,
    Modified = 3
}

/// <summary>
/// Access review campaign for periodic permission auditing.
/// </summary>
public class AccessReviewCampaign : IAuditable
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AccessReviewStatus Status { get; set; } = AccessReviewStatus.Open;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public Guid? ReviewerId { get; set; }
    public int TotalEntries { get; set; }
    public int CompletedEntries { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}

/// <summary>
/// Individual permission entry in an access review campaign.
/// </summary>
public class AccessReviewEntry
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid UserId { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public int PermissionLevel { get; set; }
    public string? PermissionSource { get; set; }
    public AccessReviewDecision Decision { get; set; } = AccessReviewDecision.Pending;
    public string? Comments { get; set; }
    public Guid? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
}
