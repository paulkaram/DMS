using DMS.DAL.Data;

namespace DMS.DAL.Entities;

/// <summary>
/// Data-driven state transition rules for the document lifecycle state machine.
/// Replaces hardcoded switch expression with configurable rules.
/// </summary>
public class StateTransitionRule : ISoftDeletable
{
    public Guid Id { get; set; }
    public DocumentState FromState { get; set; }
    public DocumentState ToState { get; set; }

    /// <summary>
    /// Role required to perform this transition (null = any authenticated user).
    /// </summary>
    public string? RequiredRole { get; set; }

    /// <summary>
    /// Whether an approval workflow must be completed before transition.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Whether the document must have a classification assigned.
    /// </summary>
    public bool RequiresClassification { get; set; }

    /// <summary>
    /// Whether the document must have a retention policy assigned.
    /// </summary>
    public bool RequiresRetentionPolicy { get; set; }

    /// <summary>
    /// Whether an integrity check must pass before transition.
    /// </summary>
    public bool RequiresIntegrityCheck { get; set; }

    /// <summary>
    /// Whether the target state makes the document immutable (no edits/deletes).
    /// </summary>
    public bool MakesImmutable { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Immutable audit log of every document state transition.
/// </summary>
public class StateTransitionLog
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public DocumentState FromState { get; set; }
    public DocumentState ToState { get; set; }
    public Guid TransitionedBy { get; set; }
    public DateTime TransitionedAt { get; set; }
    public string? Reason { get; set; }
    public Guid? RuleId { get; set; }
    public bool IsSystemAction { get; set; }
}
