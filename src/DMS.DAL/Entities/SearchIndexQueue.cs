using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public enum SearchIndexOperation
{
    Index = 0,
    Update = 1,
    Delete = 2
}

/// <summary>
/// Queue for pending search index operations. Background job dequeues and pushes to OpenSearch.
/// </summary>
public class SearchIndexQueue
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty; // "Document", "PhysicalItem"
    public Guid EntityId { get; set; }
    public SearchIndexOperation Operation { get; set; }
    public DateTime QueuedAt { get; set; } = DateTime.Now;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
