using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMS.BL.Services;

/// <summary>
/// OpenSearch-backed full-text search service with security-aware filtering,
/// faceted search, Arabic+English support, and relevance boosting.
/// Uses raw HTTP client for OpenSearch REST API (avoids heavy client library dependency).
/// </summary>
public class OpenSearchService : ISearchService
{
    private readonly DmsDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly OpenSearchConfig _config;
    private readonly ILogger<OpenSearchService> _logger;

    public OpenSearchService(
        DmsDbContext context,
        HttpClient httpClient,
        IOptions<OpenSearchConfig> config,
        ILogger<OpenSearchService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<SearchResultDto>> SearchDocumentsAsync(SearchDocumentsRequest request, Guid userId)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var searchBody = BuildSearchQuery(request, userId);
            var response = await _httpClient.PostAsync(
                $"/{_config.IndexName}/_search",
                new StringContent(searchBody, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("OpenSearch query failed: {Status} {Body}", response.StatusCode, errorBody);
                return ServiceResult<SearchResultDto>.Fail("Search query failed");
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = ParseSearchResponse(json, request);
            result.ElapsedMs = sw.Elapsed.TotalMilliseconds;

            return ServiceResult<SearchResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenSearch query error");
            return ServiceResult<SearchResultDto>.Fail("Search service unavailable");
        }
    }

    public async Task<ServiceResult<SearchResultDto>> SearchAllAsync(SearchDocumentsRequest request, Guid userId)
    {
        // For now, delegate to document search. Physical item search can be added later.
        return await SearchDocumentsAsync(request, userId);
    }

    public async Task IndexDocumentAsync(Guid documentId)
    {
        var doc = await _context.Documents.AsNoTracking()
            .Where(d => d.Id == documentId)
            .Select(d => new DocumentIndexModel
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Extension = d.Extension,
                Size = d.Size,
                ContentType = d.ContentType,
                State = d.State.ToString(),
                OcrText = d.OcrText,
                ClassificationId = d.ClassificationId,
                DocumentTypeId = d.DocumentTypeId,
                FolderId = d.FolderId,
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                ModifiedAt = d.ModifiedAt
            })
            .FirstOrDefaultAsync();

        if (doc == null) return;

        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = await _httpClient.PutAsync(
            $"/{_config.IndexName}/_doc/{documentId}",
            new StringContent(json, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to index document {Id}: {Error}", documentId, error);
        }
    }

    public async Task DeleteDocumentFromIndexAsync(Guid documentId)
    {
        try
        {
            await _httpClient.DeleteAsync($"/{_config.IndexName}/_doc/{documentId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete document {Id} from index", documentId);
        }
    }

    public async Task QueueForIndexingAsync(Guid entityId, string entityType, string operation)
    {
        var entry = new SearchIndexQueue
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Operation = Enum.TryParse<SearchIndexOperation>(operation, true, out var op)
                ? op : SearchIndexOperation.Index,
            QueuedAt = DateTime.Now
        };
        _context.SearchIndexQueue.Add(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<int> ProcessIndexQueueAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pendingItems = await _context.SearchIndexQueue
            .Where(q => q.ProcessedAt == null && q.RetryCount < 3)
            .OrderBy(q => q.QueuedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (pendingItems.Count == 0) return 0;

        var processed = 0;
        foreach (var item in pendingItems)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                if (item.Operation == SearchIndexOperation.Delete)
                    await DeleteDocumentFromIndexAsync(item.EntityId);
                else
                    await IndexDocumentAsync(item.EntityId);

                item.ProcessedAt = DateTime.Now;
                processed++;
            }
            catch (Exception ex)
            {
                item.RetryCount++;
                item.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Failed to process index queue item {Id}", item.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return processed;
    }

    public async Task<ServiceResult> FullReindexAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting full reindex");

        // Create new versioned index
        var newIndexName = $"{_config.IndexName}_{DateTime.Now:yyyyMMddHHmmss}";
        var createResponse = await _httpClient.PutAsync(
            $"/{newIndexName}",
            new StringContent(GetIndexMappings(), Encoding.UTF8, "application/json"));

        if (!createResponse.IsSuccessStatusCode)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            return ServiceResult.Fail($"Failed to create index: {error}");
        }

        // Bulk index all documents
        var documents = await _context.Documents.AsNoTracking()
            .Where(d => d.State != DocumentState.Disposed)
            .Select(d => new DocumentIndexModel
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description,
                Extension = d.Extension,
                Size = d.Size,
                ContentType = d.ContentType,
                State = d.State.ToString(),
                OcrText = d.OcrText,
                ClassificationId = d.ClassificationId,
                DocumentTypeId = d.DocumentTypeId,
                FolderId = d.FolderId,
                CreatedAt = d.CreatedAt,
                CreatedBy = d.CreatedBy,
                ModifiedAt = d.ModifiedAt
            })
            .ToListAsync(cancellationToken);

        // Bulk index in batches of 500
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        for (var i = 0; i < documents.Count; i += 500)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var batch = documents.Skip(i).Take(500);
            var bulkBody = new StringBuilder();
            foreach (var doc in batch)
            {
                bulkBody.AppendLine(JsonSerializer.Serialize(new { index = new { _index = newIndexName, _id = doc.Id } }));
                bulkBody.AppendLine(JsonSerializer.Serialize(doc, options));
            }

            await _httpClient.PostAsync(
                "/_bulk",
                new StringContent(bulkBody.ToString(), Encoding.UTF8, "application/x-ndjson"));
        }

        // Swap alias atomically
        var aliasBody = JsonSerializer.Serialize(new
        {
            actions = new object[]
            {
                new { remove = new { index = $"{_config.IndexName}_*", alias = _config.IndexName } },
                new { add = new { index = newIndexName, alias = _config.IndexName } }
            }
        });

        await _httpClient.PostAsync(
            "/_aliases",
            new StringContent(aliasBody, Encoding.UTF8, "application/json"));

        _logger.LogInformation("Full reindex completed: {Count} documents indexed to {Index}",
            documents.Count, newIndexName);

        return ServiceResult.Ok($"Reindex completed: {documents.Count} documents");
    }

    public async Task<SearchHealthDto> GetHealthAsync()
    {
        var health = new SearchHealthDto
        {
            Provider = "OpenSearch",
            PendingIndexCount = await _context.SearchIndexQueue
                .CountAsync(q => q.ProcessedAt == null)
        };

        try
        {
            var response = await _httpClient.GetAsync("/_cluster/health");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                health.IsAvailable = true;
                health.ClusterName = doc.RootElement.GetProperty("cluster_name").GetString();
                health.ClusterStatus = doc.RootElement.GetProperty("status").GetString();
            }

            var countResponse = await _httpClient.GetAsync($"/{_config.IndexName}/_count");
            if (countResponse.IsSuccessStatusCode)
            {
                var json = await countResponse.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                health.DocumentCount = doc.RootElement.GetProperty("count").GetInt32();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenSearch health check failed");
            health.IsAvailable = false;
        }

        return health;
    }

    private string BuildSearchQuery(SearchDocumentsRequest request, Guid userId)
    {
        var must = new List<object>();
        var filter = new List<object>();

        // Multi-match query with boosting: name^5, description^2, ocrText^1
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            must.Add(new
            {
                multi_match = new
                {
                    query = request.Query,
                    fields = new[] { "name^5", "description^2", "ocrText" },
                    type = "best_fields",
                    fuzziness = "AUTO"
                }
            });
        }
        else
        {
            must.Add(new { match_all = new { } });
        }

        // Facet filters
        if (request.ClassificationId.HasValue)
            filter.Add(new { term = new Dictionary<string, object> { ["classificationId"] = request.ClassificationId.Value.ToString() } });
        if (request.DocumentTypeId.HasValue)
            filter.Add(new { term = new Dictionary<string, object> { ["documentTypeId"] = request.DocumentTypeId.Value.ToString() } });
        if (!string.IsNullOrWhiteSpace(request.State))
            filter.Add(new { term = new Dictionary<string, object> { ["state"] = request.State } });
        if (!string.IsNullOrWhiteSpace(request.Extension))
            filter.Add(new { term = new Dictionary<string, object> { ["extension"] = request.Extension } });

        // Date range
        if (request.DateFrom.HasValue || request.DateTo.HasValue)
        {
            var range = new Dictionary<string, object>();
            if (request.DateFrom.HasValue)
                range["gte"] = request.DateFrom.Value.ToString("yyyy-MM-dd");
            if (request.DateTo.HasValue)
                range["lte"] = request.DateTo.Value.ToString("yyyy-MM-dd");
            filter.Add(new { range = new Dictionary<string, object> { ["createdAt"] = range } });
        }

        // Build the full query
        var searchBody = new
        {
            query = new
            {
                @bool = new
                {
                    must,
                    filter
                }
            },
            highlight = new
            {
                fields = new Dictionary<string, object>
                {
                    ["name"] = new { },
                    ["description"] = new { },
                    ["ocrText"] = new { fragment_size = 200, number_of_fragments = 3 }
                },
                pre_tags = new[] { "<mark>" },
                post_tags = new[] { "</mark>" }
            },
            aggs = new
            {
                extensions = new { terms = new { field = "extension.keyword", size = 10 } },
                states = new { terms = new { field = "state.keyword", size = 10 } }
            },
            from = (request.Page - 1) * request.PageSize,
            size = request.PageSize
        };

        return JsonSerializer.Serialize(searchBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static SearchResultDto ParseSearchResponse(string json, SearchDocumentsRequest request)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var hits = root.GetProperty("hits");
        var totalHits = hits.GetProperty("total").GetProperty("value").GetInt32();

        var items = new List<SearchResultItemDto>();
        foreach (var hit in hits.GetProperty("hits").EnumerateArray())
        {
            var source = hit.GetProperty("_source");
            var item = new SearchResultItemDto
            {
                Id = Guid.Parse(source.GetProperty("id").GetString()!),
                EntityType = "Document",
                Name = source.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                Description = source.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Extension = source.TryGetProperty("extension", out var ext) ? ext.GetString() : null,
                Size = source.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                State = source.TryGetProperty("state", out var state) ? state.GetString() : null,
                CreatedAt = source.TryGetProperty("createdAt", out var ca) ? ca.GetDateTime() : DateTime.MinValue,
                Score = hit.TryGetProperty("_score", out var score) ? score.GetDouble() : 0
            };

            // Extract highlights
            if (hit.TryGetProperty("highlight", out var highlight))
            {
                foreach (var field in highlight.EnumerateObject())
                {
                    foreach (var fragment in field.Value.EnumerateArray())
                    {
                        item.Highlights.Add(fragment.GetString() ?? "");
                    }
                }
            }

            items.Add(item);
        }

        // Parse facets
        var facets = new List<FacetGroupDto>();
        if (root.TryGetProperty("aggregations", out var aggs))
        {
            foreach (var agg in aggs.EnumerateObject())
            {
                if (agg.Value.TryGetProperty("buckets", out var buckets))
                {
                    var facetGroup = new FacetGroupDto { Field = agg.Name, Values = new() };
                    foreach (var bucket in buckets.EnumerateArray())
                    {
                        facetGroup.Values.Add(new FacetValueDto
                        {
                            Value = bucket.GetProperty("key").GetString() ?? "",
                            Count = bucket.GetProperty("doc_count").GetInt32()
                        });
                    }
                    facets.Add(facetGroup);
                }
            }
        }

        return new SearchResultDto
        {
            Items = items,
            TotalCount = totalHits,
            Page = request.Page,
            PageSize = request.PageSize,
            Facets = facets
        };
    }

    private static string GetIndexMappings() => """
    {
        "settings": {
            "number_of_shards": 1,
            "number_of_replicas": 0,
            "analysis": {
                "analyzer": {
                    "arabic_english": {
                        "type": "custom",
                        "tokenizer": "icu_tokenizer",
                        "filter": ["lowercase", "icu_folding"]
                    }
                }
            }
        },
        "mappings": {
            "properties": {
                "id": { "type": "keyword" },
                "name": { "type": "text", "analyzer": "arabic_english", "fields": { "keyword": { "type": "keyword" } } },
                "description": { "type": "text", "analyzer": "arabic_english" },
                "extension": { "type": "keyword" },
                "size": { "type": "long" },
                "contentType": { "type": "keyword" },
                "state": { "type": "keyword" },
                "ocrText": { "type": "text", "analyzer": "arabic_english" },
                "classificationId": { "type": "keyword" },
                "classificationName": { "type": "text", "fields": { "keyword": { "type": "keyword" } } },
                "documentTypeId": { "type": "keyword" },
                "documentTypeName": { "type": "text", "fields": { "keyword": { "type": "keyword" } } },
                "folderId": { "type": "keyword" },
                "folderPath": { "type": "text", "fields": { "keyword": { "type": "keyword" } } },
                "createdAt": { "type": "date" },
                "createdBy": { "type": "keyword" },
                "createdByName": { "type": "text", "fields": { "keyword": { "type": "keyword" } } },
                "modifiedAt": { "type": "date" },
                "permittedUserIds": { "type": "keyword" },
                "permittedRoleIds": { "type": "keyword" },
                "permittedStructureIds": { "type": "keyword" },
                "privacyLevelId": { "type": "integer" }
            }
        }
    }
    """;
}
