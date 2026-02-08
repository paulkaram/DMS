namespace DMS.DAL.Entities;

public class Lookup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class LookupItem
{
    public Guid Id { get; set; }
    public Guid LookupId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayText { get; set; }
    public string? Language { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
