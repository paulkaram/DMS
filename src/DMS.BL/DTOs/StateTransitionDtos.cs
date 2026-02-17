namespace DMS.BL.DTOs;

public class StateTransitionRequestDto
{
    public string TargetState { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class PlaceOnHoldDto
{
    public Guid LegalHoldId { get; set; }
    public string? Reason { get; set; }
}

public class ReleaseFromHoldDto
{
    public string? Reason { get; set; }
}

public class AllowedTransitionDto
{
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresApproval { get; set; }
    public bool RequiresClassification { get; set; }
    public bool RequiresRetentionPolicy { get; set; }
}

public class StateTransitionLogDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public Guid TransitionedBy { get; set; }
    public string? TransitionedByName { get; set; }
    public DateTime TransitionedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsSystemAction { get; set; }
}
