using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public enum CirculationRecordStatus
{
    Active = 0,
    Returned = 1,
    Overdue = 2,
    Lost = 3,
    Damaged = 4
}

/// <summary>
/// Tracks checkout/return of physical archival items.
/// </summary>
public class CirculationRecord : IAuditable
{
    public Guid Id { get; set; }
    public Guid PhysicalItemId { get; set; }
    public Guid BorrowerId { get; set; }
    public Guid? BorrowerStructureId { get; set; }
    public string? Purpose { get; set; }
    public DateTime CheckedOutAt { get; set; }
    public Guid CheckedOutBy { get; set; }
    public DateTime DueDate { get; set; }
    public int RenewalCount { get; set; }
    public int MaxRenewals { get; set; } = 2;
    public DateTime? ReturnedAt { get; set; }
    public Guid? ReturnedTo { get; set; }
    public ItemCondition ConditionAtCheckout { get; set; }
    public ItemCondition? ConditionAtReturn { get; set; }
    public string? ConditionNotes { get; set; }
    public CirculationRecordStatus Status { get; set; } = CirculationRecordStatus.Active;

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
