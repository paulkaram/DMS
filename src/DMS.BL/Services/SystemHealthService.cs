using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class SystemHealthService : ISystemHealthService
{
    private readonly DmsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemHealthService> _logger;

    public SystemHealthService(
        DmsDbContext context,
        IConfiguration configuration,
        ILogger<SystemHealthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SystemHealthDto> GetSystemHealthAsync()
    {
        var health = new SystemHealthDto();

        // Database health
        try
        {
            health.Database.IsAvailable = await _context.Database.CanConnectAsync();
            health.Database.TotalDocuments = await _context.Documents.CountAsync();
            health.Database.TotalUsers = await _context.Users.CountAsync();
            health.Database.ActiveLegalHolds = await _context.LegalHolds
                .CountAsync(h => h.Status == "Active");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            health.Database.IsAvailable = false;
        }

        // Storage health
        try
        {
            var basePath = _configuration["Storage:BasePath"] ?? "D:\\Storage\\DMS";
            health.Storage.BasePath = basePath;

            if (Directory.Exists(basePath))
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(basePath)!);
                health.Storage.TotalBytes = driveInfo.TotalSize;
                health.Storage.AvailableBytes = driveInfo.AvailableFreeSpace;
                health.Storage.UsedBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
                health.Storage.UsagePercent = driveInfo.TotalSize > 0
                    ? Math.Round((double)health.Storage.UsedBytes / driveInfo.TotalSize * 100, 1)
                    : 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
        }

        // Recent job executions
        health.RecentJobs = await _context.BackgroundJobExecutions
            .OrderByDescending(j => j.StartedAt)
            .Take(20)
            .Select(j => new JobExecutionSummaryDto
            {
                JobName = j.JobName,
                Status = j.Status,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                DurationMs = j.DurationMs,
                ItemsProcessed = j.ItemsProcessed,
                ItemsFailed = j.ItemsFailed,
                ErrorMessage = j.ErrorMessage
            })
            .ToListAsync();

        health.IsHealthy = health.Database.IsAvailable;
        return health;
    }

    public async Task<List<JobExecutionSummaryDto>> GetJobHistoryAsync(string? jobName, int page, int pageSize)
    {
        var query = _context.BackgroundJobExecutions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(jobName))
            query = query.Where(j => j.JobName == jobName);

        return await query
            .OrderByDescending(j => j.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobExecutionSummaryDto
            {
                JobName = j.JobName,
                Status = j.Status,
                StartedAt = j.StartedAt,
                CompletedAt = j.CompletedAt,
                DurationMs = j.DurationMs,
                ItemsProcessed = j.ItemsProcessed,
                ItemsFailed = j.ItemsFailed,
                ErrorMessage = j.ErrorMessage
            })
            .ToListAsync();
    }

    public async Task<Guid> RecordJobStartAsync(string jobName)
    {
        var execution = new BackgroundJobExecution
        {
            Id = Guid.NewGuid(),
            JobName = jobName,
            Status = "Running",
            StartedAt = DateTime.Now
        };

        _context.BackgroundJobExecutions.Add(execution);
        await _context.SaveChangesAsync();
        return execution.Id;
    }

    public async Task RecordJobCompletionAsync(Guid executionId, int itemsProcessed, int itemsFailed)
    {
        var execution = await _context.BackgroundJobExecutions.FindAsync(executionId);
        if (execution == null) return;

        execution.Status = "Completed";
        execution.CompletedAt = DateTime.Now;
        execution.ItemsProcessed = itemsProcessed;
        execution.ItemsFailed = itemsFailed;
        execution.DurationMs = (DateTime.Now - execution.StartedAt).TotalMilliseconds;
        await _context.SaveChangesAsync();
    }

    public async Task RecordJobFailureAsync(Guid executionId, string errorMessage)
    {
        var execution = await _context.BackgroundJobExecutions.FindAsync(executionId);
        if (execution == null) return;

        execution.Status = "Failed";
        execution.CompletedAt = DateTime.Now;
        execution.ErrorMessage = errorMessage;
        execution.DurationMs = (DateTime.Now - execution.StartedAt).TotalMilliseconds;
        await _context.SaveChangesAsync();
    }
}
