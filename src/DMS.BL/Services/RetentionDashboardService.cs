using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class RetentionDashboardService : IRetentionDashboardService
{
    private readonly IRetentionPolicyRepository _retentionRepo;
    private readonly IBackgroundJobRepository _jobRepo;
    private readonly ILogger<RetentionDashboardService> _logger;

    public RetentionDashboardService(
        IRetentionPolicyRepository retentionRepo,
        IBackgroundJobRepository jobRepo,
        ILogger<RetentionDashboardService> logger)
    {
        _retentionRepo = retentionRepo;
        _jobRepo = jobRepo;
        _logger = logger;
    }

    public async Task<ServiceResult<RetentionDashboardDto>> GetDashboardAsync()
    {
        try
        {
            var statusSummary = await _jobRepo.GetRetentionStatusSummaryAsync();
            var totalUnderRetention = await _jobRepo.GetTotalDocumentsUnderRetentionAsync();

            // Get expiring documents for 7 and 30 day windows
            var expiring30 = await _retentionRepo.GetExpiringDocumentsAsync(30);
            var expiring7 = expiring30.Where(dr => dr.ExpirationDate.HasValue &&
                dr.ExpirationDate.Value <= DateTime.Now.AddDays(7) &&
                dr.ExpirationDate.Value > DateTime.Now).ToList();
            var expiring30Only = expiring30.Where(dr => dr.ExpirationDate.HasValue &&
                dr.ExpirationDate.Value > DateTime.Now).ToList();

            // Upcoming expirations (sorted by nearest)
            var upcomingExpirations = expiring30Only
                .OrderBy(dr => dr.ExpirationDate)
                .Take(20)
                .Select(dr => new UpcomingExpirationDto
                {
                    DocumentId = dr.DocumentId,
                    DocumentName = dr.DocumentName ?? "Unknown",
                    PolicyName = dr.PolicyName ?? "Unknown",
                    ExpirationDate = dr.ExpirationDate!.Value,
                    DaysRemaining = Math.Max(0, (int)(dr.ExpirationDate!.Value - DateTime.Now).TotalDays),
                    Status = dr.Status
                })
                .ToList();

            // Expiration timeline (next 90 days, daily buckets)
            var allExpiring90 = await _retentionRepo.GetExpiringDocumentsAsync(90);
            var today = DateTime.Now.Date;
            var timeline = Enumerable.Range(0, 90)
                .Select(d => today.AddDays(d))
                .Select(date => new ExpirationTimelineDto
                {
                    Date = date,
                    Count = allExpiring90.Count(dr =>
                        dr.ExpirationDate.HasValue &&
                        dr.ExpirationDate.Value.Date == date)
                })
                .Where(t => t.Count > 0)
                .ToList();

            // Retention by policy
            var allPolicies = await _retentionRepo.GetAllAsync();
            var retentionsByPolicy = new List<RetentionPolicySummaryDto>();
            foreach (var policy in allPolicies)
            {
                // Count all document retentions for this policy from the status summary approach
                // We need per-policy stats, so we'll query the expiring docs
                var policyRetentions = allExpiring90.Where(dr => dr.PolicyId == policy.Id).ToList();
                var allForPolicy = (await _retentionRepo.GetExpiringDocumentsAsync(36500)) // large window to get all
                    .Where(dr => dr.PolicyId == policy.Id)
                    .ToList();

                // Also get pending review for this policy
                var pendingForPolicy = (await _retentionRepo.GetPendingReviewAsync())
                    .Where(dr => dr.PolicyId == policy.Id)
                    .ToList();

                // We'll approximate total from status summary; for per-policy we do a simpler calculation
                retentionsByPolicy.Add(new RetentionPolicySummaryDto
                {
                    PolicyId = policy.Id,
                    PolicyName = policy.Name,
                    ExpirationAction = policy.ExpirationAction,
                    TotalDocuments = allForPolicy.Count + pendingForPolicy.Count,
                    ActiveCount = allForPolicy.Count,
                    ExpiredCount = pendingForPolicy.Count,
                    OnHoldCount = 0 // would need separate query
                });
            }

            // Background jobs
            var recentJobs = await _jobRepo.GetAllRecentAsync(15);
            var jobDtos = recentJobs.Select(j => new BackgroundJobDto
            {
                Id = j.Id,
                JobName = j.JobName,
                Status = j.Status,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                ItemsProcessed = j.ItemsProcessed,
                ItemsFailed = j.ItemsFailed,
                DurationMs = j.DurationMs,
                ErrorMessage = j.ErrorMessage
            }).ToList();

            // Recent actions (from document retentions that have been modified recently)
            var recentActions = await GetRecentActionsInternalAsync(10);

            // Count retentions with no expiration date (awaiting trigger events)
            statusSummary.TryGetValue("Active", out var activeCount);
            statusSummary.TryGetValue("PendingReview", out var pendingCount);
            statusSummary.TryGetValue("OnHold", out var onHoldCount);
            statusSummary.TryGetValue("Archived", out var archivedCount);
            statusSummary.TryGetValue("Deleted", out var disposedCount);

            var dashboard = new RetentionDashboardDto
            {
                TotalDocumentsUnderRetention = totalUnderRetention,
                ActiveRetentions = activeCount,
                PendingReview = pendingCount,
                ExpiringSoon30 = expiring30Only.Count,
                ExpiringSoon7 = expiring7.Count,
                OnHold = onHoldCount,
                Archived = archivedCount,
                Disposed = disposedCount,
                AwaitingTrigger = 0, // Event-based retentions without expiration
                RecentActions = recentActions,
                RetentionsByPolicy = retentionsByPolicy,
                BackgroundJobs = jobDtos,
                UpcomingExpirations = upcomingExpirations,
                ExpirationTimeline = timeline
            };

            return ServiceResult<RetentionDashboardDto>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load retention dashboard");
            return ServiceResult<RetentionDashboardDto>.Fail("Failed to load retention dashboard data");
        }
    }

    public async Task<ServiceResult<List<RetentionActionDto>>> GetRecentActionsAsync(int take = 20)
    {
        try
        {
            var actions = await GetRecentActionsInternalAsync(take);
            return ServiceResult<List<RetentionActionDto>>.Ok(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent retention actions");
            return ServiceResult<List<RetentionActionDto>>.Fail("Failed to load recent actions");
        }
    }

    public async Task<ServiceResult<List<BackgroundJobDto>>> GetJobHistoryAsync(int take = 10)
    {
        try
        {
            var jobs = await _jobRepo.GetAllRecentAsync(take);
            var dtos = jobs.Select(j => new BackgroundJobDto
            {
                Id = j.Id,
                JobName = j.JobName,
                Status = j.Status,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                ItemsProcessed = j.ItemsProcessed,
                ItemsFailed = j.ItemsFailed,
                DurationMs = j.DurationMs,
                ErrorMessage = j.ErrorMessage
            }).ToList();

            return ServiceResult<List<BackgroundJobDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load job history");
            return ServiceResult<List<BackgroundJobDto>>.Fail("Failed to load job history");
        }
    }

    private async Task<List<RetentionActionDto>> GetRecentActionsInternalAsync(int take)
    {
        // Get recently modified retentions (PendingReview, Archived, Deleted, OnHold)
        var pendingReview = await _retentionRepo.GetPendingReviewAsync();
        var actions = pendingReview
            .Where(dr => dr.ModifiedAt.HasValue)
            .OrderByDescending(dr => dr.ModifiedAt)
            .Take(take)
            .Select(dr => new RetentionActionDto
            {
                DocumentId = dr.DocumentId,
                DocumentName = dr.DocumentName ?? "Unknown",
                PolicyName = dr.PolicyName ?? "Unknown",
                Action = dr.Status switch
                {
                    "PendingReview" => "Flagged for Review",
                    "Archived" => "Auto-Archived",
                    "Deleted" => "Auto-Deleted",
                    "OnHold" => "Placed on Hold",
                    "Approved" => "Approved",
                    _ => dr.Status
                },
                Timestamp = dr.ModifiedAt ?? dr.CreatedAt,
                IsSystemAction = true,
                Notes = dr.Notes
            })
            .ToList();

        return actions;
    }
}
