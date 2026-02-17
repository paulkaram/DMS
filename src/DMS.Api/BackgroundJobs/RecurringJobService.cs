namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Base class for recurring background jobs using .NET built-in BackgroundService.
/// Provides scoped DI resolution, configurable intervals, and error logging.
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

                using var scope = _scopeFactory.CreateScope();
                await ExecuteJobAsync(scope.ServiceProvider, stoppingToken);

                _logger.LogInformation("Background job '{JobName}' completed successfully", _jobName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — not an error
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job '{JobName}' failed", _jobName);
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Background job '{JobName}' stopped", _jobName);
    }

    /// <summary>
    /// Override this to implement the job logic. A fresh DI scope is provided per execution.
    /// </summary>
    protected abstract Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken);
}
