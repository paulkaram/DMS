namespace DMS.BL.DTOs;

/// <summary>
/// DTO for legal hold display.
/// </summary>
public class LegalHoldDto
{
    public Guid Id { get; set; }
    public string HoldNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CaseReference { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    public Guid AppliedBy { get; set; }
    public string? AppliedByName { get; set; }
    public DateTime AppliedAt { get; set; }
    public Guid? ReleasedBy { get; set; }
    public string? ReleasedByName { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string? ReleaseReason { get; set; }
    public string? Notes { get; set; }
    public int DocumentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a legal hold.
/// </summary>
public class CreateLegalHoldDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CaseReference { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    public string? Notes { get; set; }
    public List<Guid>? InitialDocumentIds { get; set; }
}

/// <summary>
/// DTO for updating a legal hold.
/// </summary>
public class UpdateLegalHoldDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CaseReference { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for documents under legal hold.
/// </summary>
public class LegalHoldDocumentDto
{
    public Guid Id { get; set; }
    public Guid LegalHoldId { get; set; }
    public string HoldName { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
    public Guid AddedBy { get; set; }
    public string? AddedByName { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedBy { get; set; }
    public string? Notes { get; set; }
}

public class AddDocumentsToHoldDto
{
    public List<Guid> DocumentIds { get; set; } = new();
    public string? Notes { get; set; }
}

public class ReleaseHoldDto
{
    public string Reason { get; set; } = string.Empty;
}
