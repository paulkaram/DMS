using DMS.Api.Constants;
using DMS.BL.Interfaces;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Periodic job that identifies and auto-discards stale document checkouts
/// older than the configured threshold (default 24 hours).
/// </summary>
public class StaleCheckoutCleanupJob : RecurringJobService
{
    private readonly int _staleHours;

    public StaleCheckoutCleanupJob(
        IServiceScopeFactory scopeFactory,
        ILogger<StaleCheckoutCleanupJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromHours(configuration.GetValue("BackgroundJobs:StaleCheckoutIntervalHours", 6)),
            "StaleCheckoutCleanup")
    {
        _staleHours = configuration.GetValue("BackgroundJobs:StaleCheckoutThresholdHours", AppConstants.DefaultStaleCheckoutHours);
    }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var documentService = services.GetRequiredService<IDocumentService>();
        var logger = services.GetRequiredService<ILogger<StaleCheckoutCleanupJob>>();

        var staleResult = await documentService.GetStaleCheckoutsAsync(_staleHours);
        if (!staleResult.Success || staleResult.Data == null || staleResult.Data.Count == 0)
        {
            logger.LogInformation("No stale checkouts found (threshold: {Hours}h)", _staleHours);
            return;
        }

        logger.LogInformation("Found {Count} stale checkouts older than {Hours}h", staleResult.Data.Count, _staleHours);

        var discardedCount = 0;
        foreach (var checkout in staleResult.Data)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var result = await documentService.ForceDiscardCheckOutAsync(
                    checkout.DocumentId,
                    Guid.Empty, // System action
                    $"Auto-discarded: checkout stale for over {_staleHours} hours");

                if (result.Success)
                {
                    discardedCount++;
                    logger.LogInformation("Auto-discarded stale checkout for document {DocId}", checkout.DocumentId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error discarding stale checkout for document {DocId}", checkout.DocumentId);
            }
        }

        logger.LogInformation("Stale checkout cleanup completed: {Discarded}/{Total} discarded", discardedCount, staleResult.Data.Count);
    }
}
