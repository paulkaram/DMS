using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Daily job that sends reminders for open access review campaigns approaching their due date.
/// </summary>
public class AccessReviewReminderJob : RecurringJobService
{
    public AccessReviewReminderJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AccessReviewReminderJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromHours(configuration.GetValue("BackgroundJobs:AccessReviewReminderIntervalHours", 24)),
            "AccessReviewReminder")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<DmsDbContext>();
        var logger = services.GetRequiredService<ILogger<AccessReviewReminderJob>>();

        var now = DateTime.Now;
        var reminderThreshold = now.AddDays(7); // Remind 7 days before due date

        // Find open campaigns approaching their due date
        var dueSoon = await context.AccessReviewCampaigns
            .Where(c => (c.Status == AccessReviewStatus.Open || c.Status == AccessReviewStatus.InProgress)
                        && c.DueDate <= reminderThreshold
                        && c.DueDate > now)
            .ToListAsync(cancellationToken);

        foreach (var campaign in dueSoon)
        {
            var pendingCount = await context.AccessReviewEntries
                .CountAsync(e => e.CampaignId == campaign.Id && e.Decision == AccessReviewDecision.Pending, cancellationToken);

            if (pendingCount > 0)
            {
                logger.LogWarning(
                    "Access review campaign '{Name}' due on {DueDate}: {Pending} entries still pending review",
                    campaign.Name, campaign.DueDate, pendingCount);
            }
        }

        // Auto-close overdue campaigns
        var overdue = await context.AccessReviewCampaigns
            .Where(c => (c.Status == AccessReviewStatus.Open || c.Status == AccessReviewStatus.InProgress)
                        && c.DueDate < now)
            .ToListAsync(cancellationToken);

        foreach (var campaign in overdue)
        {
            campaign.Status = AccessReviewStatus.Completed;
            logger.LogWarning("Access review campaign '{Name}' auto-closed (past due date {DueDate})",
                campaign.Name, campaign.DueDate);
        }

        if (overdue.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}
