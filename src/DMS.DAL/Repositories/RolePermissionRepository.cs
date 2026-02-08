using Dapper;
using DMS.DAL.Entities;
using Microsoft.Data.SqlClient;

namespace DMS.DAL.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly string _connectionString;

    public RolePermissionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    #region System Actions

    public async Task<IEnumerable<SystemAction>> GetAllActionsAsync(bool includeInactive = false)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = includeInactive
            ? "SELECT * FROM SystemActions ORDER BY Category, SortOrder"
            : "SELECT * FROM SystemActions WHERE IsActive = 1 ORDER BY Category, SortOrder";
        return await connection.QueryAsync<SystemAction>(sql);
    }

    public async Task<SystemAction?> GetActionByIdAsync(Guid id)
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<SystemAction>(
            "SELECT * FROM SystemActions WHERE Id = @Id", new { Id = id });
    }

    public async Task<SystemAction?> GetActionByCodeAsync(string code)
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<SystemAction>(
            "SELECT * FROM SystemActions WHERE Code = @Code", new { Code = code });
    }

    public async Task<IEnumerable<SystemAction>> GetActionsByCategoryAsync(string category)
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<SystemAction>(
            "SELECT * FROM SystemActions WHERE Category = @Category AND IsActive = 1 ORDER BY SortOrder",
            new { Category = category });
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<string>(
            "SELECT DISTINCT Category FROM SystemActions WHERE IsActive = 1 ORDER BY Category");
    }

    #endregion

    #region Role Action Permissions

    public async Task<IEnumerable<RoleActionPermission>> GetPermissionsByRoleAsync(Guid roleId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT rap.*, r.Name AS RoleName, sa.Code AS ActionCode, sa.Name AS ActionName, sa.Category AS ActionCategory
            FROM RoleActionPermissions rap
            INNER JOIN Roles r ON rap.RoleId = r.Id
            INNER JOIN SystemActions sa ON rap.ActionId = sa.Id
            WHERE rap.RoleId = @RoleId AND rap.IsAllowed = 1
            ORDER BY sa.Category, sa.SortOrder";
        return await connection.QueryAsync<RoleActionPermission>(sql, new { RoleId = roleId });
    }

    public async Task<IEnumerable<RoleActionPermission>> GetAllPermissionsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT rap.*, r.Name AS RoleName, sa.Code AS ActionCode, sa.Name AS ActionName, sa.Category AS ActionCategory
            FROM RoleActionPermissions rap
            INNER JOIN Roles r ON rap.RoleId = r.Id
            INNER JOIN SystemActions sa ON rap.ActionId = sa.Id
            WHERE rap.IsAllowed = 1
            ORDER BY r.Name, sa.Category, sa.SortOrder";
        return await connection.QueryAsync<RoleActionPermission>(sql);
    }

    public async Task<bool> HasPermissionAsync(Guid roleId, string actionCode)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT COUNT(1) FROM RoleActionPermissions rap
            INNER JOIN SystemActions sa ON rap.ActionId = sa.Id
            WHERE rap.RoleId = @RoleId AND sa.Code = @ActionCode AND rap.IsAllowed = 1";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { RoleId = roleId, ActionCode = actionCode });
        return count > 0;
    }

    public async Task<bool> UserHasPermissionAsync(Guid userId, string actionCode)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT COUNT(1) FROM RoleActionPermissions rap
            INNER JOIN SystemActions sa ON rap.ActionId = sa.Id
            INNER JOIN UserRoles ur ON rap.RoleId = ur.RoleId
            WHERE ur.UserId = @UserId AND sa.Code = @ActionCode AND rap.IsAllowed = 1";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, ActionCode = actionCode });
        return count > 0;
    }

    public async Task<IEnumerable<string>> GetUserAllowedActionsAsync(Guid userId)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            SELECT DISTINCT sa.Code FROM RoleActionPermissions rap
            INNER JOIN SystemActions sa ON rap.ActionId = sa.Id
            INNER JOIN UserRoles ur ON rap.RoleId = ur.RoleId
            WHERE ur.UserId = @UserId AND rap.IsAllowed = 1 AND sa.IsActive = 1";
        return await connection.QueryAsync<string>(sql, new { UserId = userId });
    }

    #endregion

    #region Permission Management

    public async Task<bool> GrantPermissionAsync(Guid roleId, Guid actionId, Guid? grantedBy = null)
    {
        using var connection = new SqlConnection(_connectionString);
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM RoleActionPermissions WHERE RoleId = @RoleId AND ActionId = @ActionId)
                INSERT INTO RoleActionPermissions (RoleId, ActionId, IsAllowed, GrantedBy, GrantedAt)
                VALUES (@RoleId, @ActionId, 1, @GrantedBy, GETUTCDATE())
            ELSE
                UPDATE RoleActionPermissions SET IsAllowed = 1, GrantedBy = @GrantedBy, GrantedAt = GETUTCDATE()
                WHERE RoleId = @RoleId AND ActionId = @ActionId";
        var rows = await connection.ExecuteAsync(sql, new { RoleId = roleId, ActionId = actionId, GrantedBy = grantedBy });
        return rows > 0;
    }

    public async Task<bool> RevokePermissionAsync(Guid roleId, Guid actionId)
    {
        using var connection = new SqlConnection(_connectionString);
        var rows = await connection.ExecuteAsync(
            "DELETE FROM RoleActionPermissions WHERE RoleId = @RoleId AND ActionId = @ActionId",
            new { RoleId = roleId, ActionId = actionId });
        return rows > 0;
    }

    public async Task<bool> SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> actionIds, Guid? grantedBy = null)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Remove all existing permissions for this role
            await connection.ExecuteAsync(
                "DELETE FROM RoleActionPermissions WHERE RoleId = @RoleId",
                new { RoleId = roleId }, transaction);

            // Add new permissions
            foreach (var actionId in actionIds)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO RoleActionPermissions (RoleId, ActionId, IsAllowed, GrantedBy, GrantedAt)
                      VALUES (@RoleId, @ActionId, 1, @GrantedBy, GETUTCDATE())",
                    new { RoleId = roleId, ActionId = actionId, GrantedBy = grantedBy }, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }

    #endregion
}
