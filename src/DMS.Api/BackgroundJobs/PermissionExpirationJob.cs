using DMS.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Hourly job that expires temporary permissions and delegations.
/// </summary>
public class PermissionExpirationJob : RecurringJobService
{
    public PermissionExpirationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<PermissionExpirationJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromHours(configuration.GetValue("BackgroundJobs:PermissionExpirationIntervalHours", 1)),
            "PermissionExpiration")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<DmsDbContext>();
        var logger = services.GetRequiredService<ILogger<PermissionExpirationJob>>();

        var now = DateTime.Now;

        // Expire temporary permissions (remove expired grants)
        var expiredPermissions = await context.Permissions
            .Where(p => p.ExpiresAt != null && p.ExpiresAt < now)
            .ToListAsync(cancellationToken);

        context.Permissions.RemoveRange(expiredPermissions);

        // Expire delegations
        var expiredDelegations = await context.PermissionDelegations
            .Where(d => d.EndDate < now && d.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var delegation in expiredDelegations)
        {
            delegation.IsActive = false;
        }

        var total = expiredPermissions.Count + expiredDelegations.Count;
        if (total > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Permission expiration: {Permissions} permissions, {Delegations} delegations expired",
                expiredPermissions.Count, expiredDelegations.Count);
        }
    }
}
