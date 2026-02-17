namespace DMS.DAL.Entities;

/// <summary>
/// Tracks execution history of all background jobs for monitoring and diagnostics.
/// </summary>
public class BackgroundJobExecution
{
    public Guid Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = "Running"; // Running, Completed, Failed
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsFailed { get; set; }
    public string? ErrorMessage { get; set; }
    public double? DurationMs { get; set; }
}
