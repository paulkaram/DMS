namespace DMS.DAL.Entities;

public enum CustodyTransferType
{
    Accession = 0,
    Circulation = 1,
    InternalMove = 2,
    Disposal = 3,
    Return = 4
}

/// <summary>
/// Chain of custody record for physical items with tamper-evident hash chain.
/// </summary>
public class CustodyTransfer
{
    public Guid Id { get; set; }
    public Guid PhysicalItemId { get; set; }
    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public CustodyTransferType TransferType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public ItemCondition? ConditionAtTransfer { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime TransferredAt { get; set; }
    public Guid TransferredBy { get; set; }

    /// <summary>
    /// SHA-256 hash of this entry for tamper detection.
    /// </summary>
    public string? EntryHash { get; set; }

    /// <summary>
    /// Hash of the previous custody transfer entry (forms hash chain).
    /// </summary>
    public string? PreviousEntryHash { get; set; }
}
