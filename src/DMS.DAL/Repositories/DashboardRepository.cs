using Dapper;
using DMS.DAL.Data;

namespace DMS.DAL.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetCabinetCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Cabinets WHERE IsActive = 1");
    }

    public async Task<int> GetFolderCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Folders WHERE IsActive = 1");
    }

    public async Task<int> GetDocumentCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Documents WHERE IsActive = 1");
    }

    public async Task<int> GetUserCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Users WHERE IsActive = 1");
    }

    public async Task<int> GetDocumentsThisMonthAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Documents WHERE IsActive = 1 AND CreatedAt >= @StartOfMonth",
            new { StartOfMonth = startOfMonth });
    }

    public async Task<int> GetDocumentsThisYearAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var startOfYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Documents WHERE IsActive = 1 AND CreatedAt >= @StartOfYear",
            new { StartOfYear = startOfYear });
    }

    public async Task<long> GetTotalStorageUsedAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(
            "SELECT ISNULL(SUM(Size), 0) FROM Documents WHERE IsActive = 1");
    }

    public async Task<int> GetCheckedOutCountAsync(Guid? userId = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT COUNT(*) FROM Documents WHERE IsActive = 1 AND IsCheckedOut = 1";
        if (userId.HasValue)
            sql += " AND CheckedOutBy = @UserId";
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<ContentTypeStat>> GetContentTypeDistributionAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ContentTypeStat>(@"
            SELECT ISNULL(ContentType, 'Unknown') AS ContentType, COUNT(*) AS Count, SUM(Size) AS TotalSize
            FROM Documents WHERE IsActive = 1
            GROUP BY ContentType
            ORDER BY Count DESC");
    }

    public async Task<IEnumerable<RecentDocument>> GetRecentDocumentsAsync(int take = 10)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<RecentDocument>(@"
            SELECT TOP (@Take) d.Id, d.Name, f.Name AS FolderName, d.Extension, d.CreatedAt, u.DisplayName AS CreatedByName
            FROM Documents d
            LEFT JOIN Folders f ON d.FolderId = f.Id
            LEFT JOIN Users u ON d.CreatedBy = u.Id
            WHERE d.IsActive = 1
            ORDER BY d.CreatedAt DESC", new { Take = take });
    }

    public async Task<IEnumerable<MonthlyStat>> GetMonthlyDocumentStatsAsync(int months = 12)
    {
        using var connection = _connectionFactory.CreateConnection();
        var startDate = DateTime.UtcNow.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1);

        return await connection.QueryAsync<MonthlyStat>(@"
            SELECT YEAR(CreatedAt) AS Year, MONTH(CreatedAt) AS Month, COUNT(*) AS Count
            FROM Documents
            WHERE IsActive = 1 AND CreatedAt >= @StartDate
            GROUP BY YEAR(CreatedAt), MONTH(CreatedAt)
            ORDER BY Year, Month", new { StartDate = startDate });
    }
}
