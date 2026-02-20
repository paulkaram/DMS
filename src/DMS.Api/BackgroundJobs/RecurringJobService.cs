using System.Diagnostics;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Base class for recurring background jobs using .NET built-in BackgroundService.
/// Provides scoped DI resolution, configurable intervals, error logging,
/// and automatic BackgroundJobExecution tracking.
/// </summary>
public abstract class RecurringJobService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly TimeSpan _interval;
    private readonly string _jobName;

    protected RecurringJobService(
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        TimeSpan interval,
        string jobName)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval;
        _jobName = jobName;
    }

    /// <summary>
    /// Items processed during the current execution. Set by subclasses.
    /// </summary>
    protected int ItemsProcessed { get; set; }

    /// <summary>
    /// Items that failed during the current execution. Set by subclasses.
    /// </summary>
    protected int ItemsFailed { get; set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background job '{JobName}' started with interval {Interval}", _jobName, _interval);

        // Delay initial execution to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Background job '{JobName}' executing", _jobName);

                // Reset counters
                ItemsProcessed = 0;
                ItemsFailed = 0;

                using var scope = _scopeFactory.CreateScope();

                // Record execution start
                var execution = new BackgroundJobExecution
                {
                    Id = Guid.NewGuid(),
                    JobName = _jobName,
                    Status = "Running",
                    StartedAt = DateTime.Now
                };

                await RecordExecutionStartAsync(scope.ServiceProvider, execution);

                var sw = Stopwatch.StartNew();
                try
                {
                    await ExecuteJobAsync(scope.ServiceProvider, stoppingToken);
                    sw.Stop();

                    execution.Status = "Completed";
                    execution.CompletedAt = DateTime.Now;
                    execution.DurationMs = sw.Elapsed.TotalMilliseconds;
                    execution.ItemsProcessed = ItemsProcessed;
                    execution.ItemsFailed = ItemsFailed;

                    _logger.LogInformation(
                        "Background job '{JobName}' completed: {Processed} processed, {Failed} failed in {Duration}ms",
                        _jobName, ItemsProcessed, ItemsFailed, sw.Elapsed.TotalMilliseconds);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    sw.Stop();
                    execution.Status = "Failed";
                    execution.CompletedAt = DateTime.Now;
                    execution.DurationMs = sw.Elapsed.TotalMilliseconds;
                    execution.ItemsProcessed = ItemsProcessed;
                    execution.ItemsFailed = ItemsFailed;
                    execution.ErrorMessage = ex.Message;

                    _logger.LogError(ex, "Background job '{JobName}' failed", _jobName);
                }

                await RecordExecutionEndAsync(scope.ServiceProvider, execution);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — not an error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job '{JobName}' execution tracking failed", _jobName);
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Background job '{JobName}' stopped", _jobName);
    }

    private async Task RecordExecutionStartAsync(IServiceProvider services, BackgroundJobExecution execution)
    {
        try
        {
            var repo = services.GetRequiredService<IBackgroundJobRepository>();
            await repo.CreateAsync(execution);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record execution start for '{JobName}'", _jobName);
        }
    }

    private async Task RecordExecutionEndAsync(IServiceProvider services, BackgroundJobExecution execution)
    {
        try
        {
            var repo = services.GetRequiredService<IBackgroundJobRepository>();
            await repo.UpdateAsync(execution);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record execution end for '{JobName}'", _jobName);
        }
    }

    /// <summary>
    /// Override this to implement the job logic. A fresh DI scope is provided per execution.
    /// Set ItemsProcessed and ItemsFailed to track metrics.
    /// </summary>
    protected abstract Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken);
}
