using DMS.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly DmsDbContext _context;

    public DashboardRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetCabinetCountAsync() =>
        await _context.Cabinets.AsNoTracking().CountAsync();

    public async Task<int> GetFolderCountAsync() =>
        await _context.Folders.AsNoTracking().CountAsync();

    public async Task<int> GetDocumentCountAsync() =>
        await _context.Documents.AsNoTracking().CountAsync();

    public async Task<int> GetUserCountAsync() =>
        await _context.Users.AsNoTracking().CountAsync(u => u.IsActive);

    public async Task<int> GetDocumentsThisMonthAsync()
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _context.Documents.AsNoTracking()
            .CountAsync(d => d.CreatedAt >= startOfMonth);
    }

    public async Task<int> GetDocumentsThisYearAsync()
    {
        var startOfYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
        return await _context.Documents.AsNoTracking()
            .CountAsync(d => d.CreatedAt >= startOfYear);
    }

    public async Task<long> GetTotalStorageUsedAsync() =>
        await _context.Documents.AsNoTracking()
            .SumAsync(d => d.Size);

    public async Task<int> GetCheckedOutCountAsync(Guid? userId = null)
    {
        var query = _context.Documents.AsNoTracking()
            .Where(d => d.IsCheckedOut);

        if (userId.HasValue)
            query = query.Where(d => d.CheckedOutBy == userId.Value);

        return await query.CountAsync();
    }

    public async Task<IEnumerable<ContentTypeStat>> GetContentTypeDistributionAsync() =>
        await _context.Documents.AsNoTracking()
            .GroupBy(d => d.ContentType ?? "Unknown")
            .Select(g => new ContentTypeStat
            {
                ContentType = g.Key,
                Count = g.Count(),
                TotalSize = g.Sum(d => d.Size)
            })
            .OrderByDescending(s => s.Count)
            .ToListAsync();

    public async Task<IEnumerable<RecentDocument>> GetRecentDocumentsAsync(int take = 10) =>
        await _context.Documents.AsNoTracking()
            .GroupJoin(_context.Folders.AsNoTracking(), d => d.FolderId, f => f.Id, (d, folders) => new { d, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.d, f })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.d.CreatedBy, u => u.Id, (x, users) => new { x.d, x.f, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new RecentDocument
            {
                Id = x.d.Id,
                Name = x.d.Name,
                FolderName = x.f != null ? x.f.Name : null,
                Extension = x.d.Extension,
                CreatedAt = x.d.CreatedAt,
                CreatedByName = u != null ? u.DisplayName : null
            })
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .ToListAsync();

    public async Task<IEnumerable<MonthlyStat>> GetMonthlyDocumentStatsAsync(int months = 12)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        return await _context.Documents.AsNoTracking()
            .Where(d => d.CreatedAt >= startDate)
            .GroupBy(d => new { d.CreatedAt.Year, d.CreatedAt.Month })
            .Select(g => new MonthlyStat
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Count = g.Count()
            })
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ToListAsync();
    }
}
