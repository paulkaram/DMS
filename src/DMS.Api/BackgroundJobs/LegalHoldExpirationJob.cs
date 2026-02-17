using DMS.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Daily job that checks for expired legal holds and releases them.
/// </summary>
public class LegalHoldExpirationJob : RecurringJobService
{
    public LegalHoldExpirationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<LegalHoldExpirationJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromHours(configuration.GetValue("BackgroundJobs:LegalHoldCheckIntervalHours", 24)),
            "LegalHoldExpiration")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<DmsDbContext>();
        var logger = services.GetRequiredService<ILogger<LegalHoldExpirationJob>>();

        var now = DateTime.Now;

        // Find expired holds
        var expiredHolds = await context.LegalHolds
            .Where(h => h.Status == "Active" && h.EffectiveUntil != null && h.EffectiveUntil < now)
            .ToListAsync(cancellationToken);

        foreach (var hold in expiredHolds)
        {
            hold.Status = "Expired";
            hold.ModifiedAt = now;

            logger.LogInformation("Legal hold '{Name}' ({Id}) expired", hold.Name, hold.Id);
        }

        if (expiredHolds.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Legal hold expiration check: {Count} holds expired", expiredHolds.Count);
        }
    }
}
