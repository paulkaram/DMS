using DMS.BL.Interfaces;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Weekly job that verifies SHA-256 integrity of documents not checked in the last 30 days.
/// Leverages existing IntegrityService.RunScheduledVerificationAsync().
/// </summary>
public class IntegrityVerificationJob : RecurringJobService
{
    public IntegrityVerificationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<IntegrityVerificationJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromHours(configuration.GetValue("BackgroundJobs:IntegrityCheckIntervalHours", 168)),
            "IntegrityVerification")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var integrityService = services.GetRequiredService<IIntegrityService>();
        var logger = services.GetRequiredService<ILogger<IntegrityVerificationJob>>();

        var result = await integrityService.RunScheduledVerificationAsync(batchSize: 100);

        logger.LogInformation(
            "Integrity verification completed: {Total} documents checked, {Passed} passed, {Failed} failed, {Skipped} skipped",
            result.TotalDocuments, result.PassedCount, result.FailedCount, result.SkippedCount);

        if (result.FailedCount > 0)
        {
            logger.LogWarning("Integrity verification found {Count} documents with hash mismatches!", result.FailedCount);
        }
    }
}
