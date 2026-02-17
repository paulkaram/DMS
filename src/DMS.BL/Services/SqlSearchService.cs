using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

/// <summary>
/// SQL-based fallback search service. Used when OpenSearch is not configured.
/// Queries SQL Server directly with LIKE/CONTAINS for basic full-text search.
/// </summary>
public class SqlSearchService : ISearchService
{
    private readonly DmsDbContext _context;
    private readonly ILogger<SqlSearchService> _logger;

    public SqlSearchService(DmsDbContext context, ILogger<SqlSearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<SearchResultDto>> SearchDocumentsAsync(SearchDocumentsRequest request, Guid userId)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var query = _context.Documents.AsNoTracking()
            .Where(d => d.State != DocumentState.Disposed);

        // Text search on name, description, OCR text
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = request.Query.Trim().ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(searchTerm) ||
                (d.Description != null && d.Description.ToLower().Contains(searchTerm)) ||
                (d.OcrText != null && d.OcrText.ToLower().Contains(searchTerm)));
        }

        // Facet filters
        if (request.ClassificationId.HasValue)
            query = query.Where(d => d.ClassificationId == request.ClassificationId.Value);
        if (request.DocumentTypeId.HasValue)
            query = query.Where(d => d.DocumentTypeId == request.DocumentTypeId.Value);
        if (!string.IsNullOrWhiteSpace(request.State) &&
            Enum.TryParse<DocumentState>(request.State, true, out var state))
            query = query.Where(d => d.State == state);
        if (!string.IsNullOrWhiteSpace(request.Extension))
            query = query.Where(d => d.Extension == request.Extension);
        if (request.DateFrom.HasValue)
            query = query.Where(d => d.CreatedAt >= request.DateFrom.Value);
        if (request.DateTo.HasValue)
            query = query.Where(d => d.CreatedAt <= request.DateTo.Value);

        var totalCount = await query.CountAsync();

        // Sorting
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            "date" => request.SortDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            "size" => request.SortDescending ? query.OrderByDescending(d => d.Size) : query.OrderBy(d => d.Size),
            _ => query.OrderByDescending(d => d.CreatedAt) // relevance fallback: newest first
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new SearchResultItemDto
            {
                Id = d.Id,
                EntityType = "Document",
                Name = d.Name,
                Description = d.Description,
                Extension = d.Extension,
                Size = d.Size,
                State = d.State.ToString(),
                CreatedAt = d.CreatedAt,
                Score = 1.0
            })
            .ToListAsync();

        sw.Stop();

        // Build basic facets from the full result set
        var facets = await BuildFacetsAsync(request);

        return ServiceResult<SearchResultDto>.Ok(new SearchResultDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            Facets = facets,
            ElapsedMs = sw.Elapsed.TotalMilliseconds
        });
    }

    public async Task<ServiceResult<SearchResultDto>> SearchAllAsync(SearchDocumentsRequest request, Guid userId)
    {
        // For SQL fallback, just search documents
        return await SearchDocumentsAsync(request, userId);
    }

    public Task IndexDocumentAsync(Guid documentId)
    {
        // No-op for SQL search - data is already in the database
        return Task.CompletedTask;
    }

    public Task DeleteDocumentFromIndexAsync(Guid documentId)
    {
        // No-op for SQL search
        return Task.CompletedTask;
    }

    public async Task QueueForIndexingAsync(Guid entityId, string entityType, string operation)
    {
        // Still enqueue so the system tracks what would be indexed
        var entry = new SearchIndexQueue
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Operation = Enum.TryParse<SearchIndexOperation>(operation, true, out var op)
                ? op : SearchIndexOperation.Index,
            QueuedAt = DateTime.Now,
            ProcessedAt = DateTime.Now // Mark as processed immediately for SQL mode
        };
        _context.SearchIndexQueue.Add(entry);
        await _context.SaveChangesAsync();
    }

    public Task<int> ProcessIndexQueueAsync(int batchSize, CancellationToken cancellationToken)
    {
        // No-op for SQL search
        return Task.FromResult(0);
    }

    public Task<ServiceResult> FullReindexAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ServiceResult.Ok("SQL search does not require reindexing"));
    }

    public async Task<SearchHealthDto> GetHealthAsync()
    {
        var pendingCount = await _context.SearchIndexQueue
            .CountAsync(q => q.ProcessedAt == null);

        var docCount = await _context.Documents.CountAsync();

        return new SearchHealthDto
        {
            IsAvailable = true,
            Provider = "SQL",
            ClusterName = "SQL Server",
            ClusterStatus = "green",
            DocumentCount = docCount,
            PendingIndexCount = pendingCount
        };
    }

    private async Task<List<FacetGroupDto>> BuildFacetsAsync(SearchDocumentsRequest request)
    {
        var facets = new List<FacetGroupDto>();

        // Extension facets
        var extensionFacets = await _context.Documents.AsNoTracking()
            .Where(d => d.State != DocumentState.Disposed && d.Extension != null)
            .GroupBy(d => d.Extension)
            .Select(g => new FacetValueDto { Value = g.Key!, Count = g.Count() })
            .OrderByDescending(f => f.Count)
            .Take(10)
            .ToListAsync();
        if (extensionFacets.Count > 0)
            facets.Add(new FacetGroupDto { Field = "extension", Values = extensionFacets });

        // State facets
        var stateFacets = await _context.Documents.AsNoTracking()
            .Where(d => d.State != DocumentState.Disposed)
            .GroupBy(d => d.State)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .ToListAsync();
        if (stateFacets.Count > 0)
            facets.Add(new FacetGroupDto
            {
                Field = "state",
                Values = stateFacets.Select(s => new FacetValueDto
                {
                    Value = s.State.ToString(),
                    Count = s.Count
                }).ToList()
            });

        return facets;
    }
}
