namespace DMS.DAL.Entities;

/// <summary>
/// Trigger types for event-based retention.
/// </summary>
public enum RetentionTriggerType
{
    DocumentCreated = 0,
    DeclaredRecord = 1,
    MetadataFieldChanged = 2,
    ExternalEvent = 3,
    ContractClosed = 4,
    CaseResolved = 5
}

/// <summary>
/// Defines event-based triggers for retention policies.
/// When the specified event occurs, the retention countdown begins.
/// </summary>
public class RetentionTriggerEvent
{
    public Guid Id { get; set; }
    public Guid RetentionPolicyId { get; set; }
    public RetentionTriggerType TriggerType { get; set; }

    /// <summary>
    /// For MetadataFieldChanged triggers: the field name to watch.
    /// </summary>
    public string? TriggerFieldName { get; set; }

    /// <summary>
    /// For MetadataFieldChanged triggers: the field value that activates the trigger.
    /// </summary>
    public string? TriggerFieldValue { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Records when a retention trigger fires for a specific document.
/// </summary>
public class RetentionTriggerLog
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid RetentionPolicyId { get; set; }
    public Guid? TriggerEventId { get; set; }
    public RetentionTriggerType TriggerType { get; set; }
    public DateTime TriggeredAt { get; set; }
    public Guid? TriggeredBy { get; set; }
    public DateTime? NewExpirationDate { get; set; }
    public DateTime? PreviousExpirationDate { get; set; }
}
