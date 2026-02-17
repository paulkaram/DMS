using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Daily job that flags overdue physical item loans and updates circulation status.
/// </summary>
public class OverdueCirculationJob : RecurringJobService
{
    public OverdueCirculationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OverdueCirculationJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromHours(configuration.GetValue("BackgroundJobs:OverdueCheckIntervalHours", 24)),
            "OverdueCirculation")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<DmsDbContext>();
        var logger = services.GetRequiredService<ILogger<OverdueCirculationJob>>();

        var now = DateTime.Now;

        // Find active circulation records past their due date
        var overdueRecords = await context.CirculationRecords
            .Where(c => c.Status == CirculationRecordStatus.Active && c.DueDate < now)
            .ToListAsync(cancellationToken);

        foreach (var record in overdueRecords)
        {
            record.Status = CirculationRecordStatus.Overdue;

            // Update the physical item's circulation status
            var item = await context.PhysicalItems.FindAsync(record.PhysicalItemId);
            if (item != null)
                item.CirculationStatus = CirculationStatus.Overdue;
        }

        if (overdueRecords.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Overdue circulation check: {Count} items flagged as overdue", overdueRecords.Count);
        }
    }
}
