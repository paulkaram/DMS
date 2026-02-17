using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface ISearchService
{
    /// <summary>
    /// Full-text search documents with security filtering and facets.
    /// </summary>
    Task<ServiceResult<SearchResultDto>> SearchDocumentsAsync(SearchDocumentsRequest request, Guid userId);

    /// <summary>
    /// Hybrid search across digital documents and physical items.
    /// </summary>
    Task<ServiceResult<SearchResultDto>> SearchAllAsync(SearchDocumentsRequest request, Guid userId);

    /// <summary>
    /// Index or re-index a single document in the search engine.
    /// </summary>
    Task IndexDocumentAsync(Guid documentId);

    /// <summary>
    /// Remove a document from the search index.
    /// </summary>
    Task DeleteDocumentFromIndexAsync(Guid documentId);

    /// <summary>
    /// Queue a document for indexing (via background job).
    /// </summary>
    Task QueueForIndexingAsync(Guid entityId, string entityType, string operation);

    /// <summary>
    /// Process pending items in the search index queue.
    /// </summary>
    Task<int> ProcessIndexQueueAsync(int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// Full reindex of all documents. Creates new index, bulk inserts, swaps alias.
    /// </summary>
    Task<ServiceResult> FullReindexAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get search engine health/status.
    /// </summary>
    Task<SearchHealthDto> GetHealthAsync();
}

/// <summary>
/// Configuration for OpenSearch connection.
/// </summary>
public class OpenSearchConfig
{
    public const string SectionName = "OpenSearch";
    public string Urls { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "dms_documents";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
