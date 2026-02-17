using DMS.BL.Interfaces;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// High-frequency job that dequeues pending search index operations and pushes to OpenSearch.
/// Runs every 10 seconds.
/// </summary>
public class SearchIndexingJob : RecurringJobService
{
    public SearchIndexingJob(
        IServiceScopeFactory scopeFactory,
        ILogger<SearchIndexingJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromSeconds(configuration.GetValue("BackgroundJobs:SearchIndexIntervalSeconds", 10)),
            "SearchIndexing")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var searchService = services.GetRequiredService<ISearchService>();
        var logger = services.GetRequiredService<ILogger<SearchIndexingJob>>();

        var processed = await searchService.ProcessIndexQueueAsync(batchSize: 50, cancellationToken);

        if (processed > 0)
            logger.LogInformation("Search indexing job processed {Count} items", processed);
    }
}
