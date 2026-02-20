using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public interface IBackgroundJobRepository
{
    Task<Guid> CreateAsync(BackgroundJobExecution execution);
    Task<bool> UpdateAsync(BackgroundJobExecution execution);
    Task<List<BackgroundJobExecution>> GetRecentByJobNameAsync(string jobName, int take = 10);
    Task<List<BackgroundJobExecution>> GetAllRecentAsync(int take = 20);
    Task<Dictionary<string, int>> GetRetentionStatusSummaryAsync();
    Task<int> GetRetentionCountByStatusAsync(string status);
    Task<int> GetTotalDocumentsUnderRetentionAsync();
}

public class BackgroundJobRepository : IBackgroundJobRepository
{
    private readonly DmsDbContext _context;

    public BackgroundJobRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateAsync(BackgroundJobExecution execution)
    {
        if (execution.Id == Guid.Empty)
            execution.Id = Guid.NewGuid();

        _context.BackgroundJobExecutions.Add(execution);
        await _context.SaveChangesAsync();
        return execution.Id;
    }

    public async Task<bool> UpdateAsync(BackgroundJobExecution execution)
    {
        var existing = await _context.BackgroundJobExecutions.FindAsync(execution.Id);
        if (existing == null) return false;

        existing.Status = execution.Status;
        existing.CompletedAt = execution.CompletedAt;
        existing.ItemsProcessed = execution.ItemsProcessed;
        existing.ItemsFailed = execution.ItemsFailed;
        existing.ErrorMessage = execution.ErrorMessage;
        existing.DurationMs = execution.DurationMs;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<BackgroundJobExecution>> GetRecentByJobNameAsync(string jobName, int take = 10)
    {
        return await _context.BackgroundJobExecutions
            .AsNoTracking()
            .Where(j => j.JobName == jobName)
            .OrderByDescending(j => j.StartedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<List<BackgroundJobExecution>> GetAllRecentAsync(int take = 20)
    {
        return await _context.BackgroundJobExecutions
            .AsNoTracking()
            .OrderByDescending(j => j.StartedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetRetentionStatusSummaryAsync()
    {
        return await _context.DocumentRetentions
            .AsNoTracking()
            .GroupBy(dr => dr.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    public async Task<int> GetRetentionCountByStatusAsync(string status)
    {
        return await _context.DocumentRetentions
            .AsNoTracking()
            .CountAsync(dr => dr.Status == status);
    }

    public async Task<int> GetTotalDocumentsUnderRetentionAsync()
    {
        return await _context.DocumentRetentions
            .AsNoTracking()
            .CountAsync();
    }
}
