using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public enum LocationType
{
    Site = 1,
    Building = 2,
    Floor = 3,
    Room = 4,
    Rack = 5,
    Shelf = 6,
    Box = 7,
    File = 8
}

/// <summary>
/// Physical storage location hierarchy (self-referencing tree, like Structure).
/// </summary>
public class PhysicalLocation : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Code { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public string? Path { get; set; }
    public int Level { get; set; }
    public int? Capacity { get; set; }
    public int CurrentCount { get; set; }
    public string? EnvironmentalConditions { get; set; }
    public string? Coordinates { get; set; }
    public string? SecurityLevel { get; set; }
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
