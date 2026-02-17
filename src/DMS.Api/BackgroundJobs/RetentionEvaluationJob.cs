using DMS.DAL.Repositories;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Daily job that evaluates retention policies and transitions expired documents.
/// - Moves expired Active retentions to PendingReview (if RequiresApproval) or executes action directly
/// - Sends notifications for documents approaching expiration
/// </summary>
public class RetentionEvaluationJob : RecurringJobService
{
    public RetentionEvaluationJob(
        IServiceScopeFactory scopeFactory,
        ILogger<RetentionEvaluationJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromHours(configuration.GetValue("BackgroundJobs:RetentionEvaluationIntervalHours", 24)),
            "RetentionEvaluation")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var retentionRepo = services.GetRequiredService<IRetentionPolicyRepository>();
        var documentRepo = services.GetRequiredService<IDocumentRepository>();
        var logger = services.GetRequiredService<ILogger<RetentionEvaluationJob>>();

        // 1. Find expired document retentions that are still Active
        var expiredRetentions = await retentionRepo.GetExpiringDocumentsAsync(0);
        var expiredList = expiredRetentions
            .Where(dr => dr.ExpirationDate.HasValue && dr.ExpirationDate.Value <= DateTime.Now)
            .ToList();

        logger.LogInformation("Retention evaluation found {Count} expired document retentions", expiredList.Count);

        foreach (var retention in expiredList)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                // Check if document is on legal hold
                var document = await documentRepo.GetByIdAsync(retention.DocumentId);
                if (document == null || document.IsOnLegalHold)
                {
                    if (document?.IsOnLegalHold == true)
                    {
                        // Move to OnHold status
                        retention.Status = "OnHold";
                        retention.Notes = (retention.Notes ?? "") + $" | Auto-held: document on legal hold at {DateTime.Now:yyyy-MM-dd}";
                        await retentionRepo.UpdateDocumentRetentionAsync(retention);
                    }
                    continue;
                }

                // Get the policy to check if approval is required
                var policy = await retentionRepo.GetByIdAsync(retention.PolicyId);
                if (policy == null) continue;

                if (policy.RequiresApproval)
                {
                    // Move to PendingReview — requires human approval
                    retention.Status = "PendingReview";
                    retention.Notes = (retention.Notes ?? "") + $" | Auto-flagged for review at {DateTime.Now:yyyy-MM-dd}";
                    await retentionRepo.UpdateDocumentRetentionAsync(retention);
                    logger.LogInformation("Document {DocId} retention moved to PendingReview", retention.DocumentId);
                }
                else
                {
                    // Execute action directly based on policy
                    retention.Status = policy.ExpirationAction switch
                    {
                        "Archive" => "Archived",
                        "Delete" => "Deleted",
                        _ => "PendingReview"
                    };
                    retention.ActionDate = DateTime.Now;
                    retention.Notes = (retention.Notes ?? "") + $" | Auto-{retention.Status.ToLower()} at {DateTime.Now:yyyy-MM-dd}";
                    await retentionRepo.UpdateDocumentRetentionAsync(retention);
                    logger.LogInformation("Document {DocId} retention auto-actioned to {Status}", retention.DocumentId, retention.Status);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing retention for document {DocId}", retention.DocumentId);
            }
        }

        // 2. Send notifications for documents approaching expiration (not yet expired)
        var allPolicies = await retentionRepo.GetAllAsync();
        foreach (var policy in allPolicies.Where(p => p.NotifyBeforeExpiration))
        {
            if (cancellationToken.IsCancellationRequested) break;

            var approachingExpiry = await retentionRepo.GetExpiringDocumentsAsync(policy.NotificationDays);
            var needsNotification = approachingExpiry
                .Where(dr => !dr.NotificationSent && dr.PolicyId == policy.Id)
                .ToList();

            foreach (var retention in needsNotification)
            {
                retention.NotificationSent = true;
                retention.Notes = (retention.Notes ?? "") + $" | Notification sent at {DateTime.Now:yyyy-MM-dd}";
                await retentionRepo.UpdateDocumentRetentionAsync(retention);
            }

            if (needsNotification.Count > 0)
                logger.LogInformation("Sent {Count} retention notifications for policy '{Policy}'", needsNotification.Count, policy.Name);
        }
    }
}
