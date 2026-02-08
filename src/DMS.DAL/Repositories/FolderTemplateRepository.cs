using Dapper;
using DMS.DAL.Data;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public class FolderTemplateRepository : IFolderTemplateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public FolderTemplateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    #region Template CRUD

    public async Task<IEnumerable<FolderTemplate>> GetAllAsync(bool includeInactive = false)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.*,
                   u.DisplayName AS CreatedByName,
                   (SELECT COUNT(*) FROM FolderTemplateUsage WHERE TemplateId = t.Id) AS UsageCount
            FROM FolderTemplates t
            LEFT JOIN Users u ON t.CreatedBy = u.Id
            WHERE @IncludeInactive = 1 OR t.IsActive = 1
            ORDER BY t.Category, t.Name";

        return await connection.QueryAsync<FolderTemplate>(sql, new { IncludeInactive = includeInactive });
    }

    public async Task<FolderTemplate?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.*,
                   u.DisplayName AS CreatedByName,
                   (SELECT COUNT(*) FROM FolderTemplateUsage WHERE TemplateId = t.Id) AS UsageCount
            FROM FolderTemplates t
            LEFT JOIN Users u ON t.CreatedBy = u.Id
            WHERE t.Id = @Id";

        return await connection.QueryFirstOrDefaultAsync<FolderTemplate>(sql, new { Id = id });
    }

    public async Task<FolderTemplate?> GetByIdWithNodesAsync(Guid id)
    {
        var template = await GetByIdAsync(id);
        if (template == null) return null;

        var nodes = await GetNodesByTemplateIdAsync(id);
        template.Nodes = BuildNodeTree(nodes.ToList());

        return template;
    }

    public async Task<IEnumerable<FolderTemplate>> GetByCategoryAsync(string category)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.*,
                   u.DisplayName AS CreatedByName,
                   (SELECT COUNT(*) FROM FolderTemplateUsage WHERE TemplateId = t.Id) AS UsageCount
            FROM FolderTemplates t
            LEFT JOIN Users u ON t.CreatedBy = u.Id
            WHERE t.Category = @Category AND t.IsActive = 1
            ORDER BY t.Name";

        return await connection.QueryAsync<FolderTemplate>(sql, new { Category = category });
    }

    public async Task<FolderTemplate?> GetDefaultAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT t.*,
                   u.DisplayName AS CreatedByName,
                   (SELECT COUNT(*) FROM FolderTemplateUsage WHERE TemplateId = t.Id) AS UsageCount
            FROM FolderTemplates t
            LEFT JOIN Users u ON t.CreatedBy = u.Id
            WHERE t.IsDefault = 1 AND t.IsActive = 1";

        var template = await connection.QueryFirstOrDefaultAsync<FolderTemplate>(sql);
        if (template != null)
        {
            var nodes = await GetNodesByTemplateIdAsync(template.Id);
            template.Nodes = BuildNodeTree(nodes.ToList());
        }
        return template;
    }

    public async Task<Guid> CreateAsync(FolderTemplate template)
    {
        using var connection = _connectionFactory.CreateConnection();
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;

        // If setting as default, clear other defaults first
        if (template.IsDefault)
        {
            await connection.ExecuteAsync(
                "UPDATE FolderTemplates SET IsDefault = 0 WHERE IsDefault = 1");
        }

        var sql = @"
            INSERT INTO FolderTemplates
                (Id, Name, Description, Category, Icon, IsActive, IsDefault, CreatedBy, CreatedAt)
            VALUES
                (@Id, @Name, @Description, @Category, @Icon, @IsActive, @IsDefault, @CreatedBy, @CreatedAt)";

        await connection.ExecuteAsync(sql, template);
        return template.Id;
    }

    public async Task<bool> UpdateAsync(FolderTemplate template)
    {
        using var connection = _connectionFactory.CreateConnection();
        template.ModifiedAt = DateTime.UtcNow;

        // If setting as default, clear other defaults first
        if (template.IsDefault)
        {
            await connection.ExecuteAsync(
                "UPDATE FolderTemplates SET IsDefault = 0 WHERE IsDefault = 1 AND Id != @Id",
                new { template.Id });
        }

        var sql = @"
            UPDATE FolderTemplates SET
                Name = @Name,
                Description = @Description,
                Category = @Category,
                Icon = @Icon,
                IsActive = @IsActive,
                IsDefault = @IsDefault,
                ModifiedBy = @ModifiedBy,
                ModifiedAt = @ModifiedAt
            WHERE Id = @Id";

        return await connection.ExecuteAsync(sql, template) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        // Soft delete
        return await connection.ExecuteAsync(
            "UPDATE FolderTemplates SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id",
            new { Id = id, Now = DateTime.UtcNow }) > 0;
    }

    #endregion

    #region Node Management

    public async Task<IEnumerable<FolderTemplateNode>> GetNodesByTemplateIdAsync(Guid templateId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT n.*,
                   ct.DisplayName AS ContentTypeName
            FROM FolderTemplateNodes n
            LEFT JOIN ContentTypes ct ON n.ContentTypeId = ct.Id
            WHERE n.TemplateId = @TemplateId
            ORDER BY n.SortOrder";

        return await connection.QueryAsync<FolderTemplateNode>(sql, new { TemplateId = templateId });
    }

    public async Task<Guid> CreateNodeAsync(FolderTemplateNode node)
    {
        using var connection = _connectionFactory.CreateConnection();
        node.Id = Guid.NewGuid();

        var sql = @"
            INSERT INTO FolderTemplateNodes
                (Id, TemplateId, ParentNodeId, Name, Description, ContentTypeId, SortOrder, BreakContentTypeInheritance)
            VALUES
                (@Id, @TemplateId, @ParentNodeId, @Name, @Description, @ContentTypeId, @SortOrder, @BreakContentTypeInheritance)";

        await connection.ExecuteAsync(sql, node);
        return node.Id;
    }

    public async Task<bool> UpdateNodeAsync(FolderTemplateNode node)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            UPDATE FolderTemplateNodes SET
                ParentNodeId = @ParentNodeId,
                Name = @Name,
                Description = @Description,
                ContentTypeId = @ContentTypeId,
                SortOrder = @SortOrder,
                BreakContentTypeInheritance = @BreakContentTypeInheritance
            WHERE Id = @Id";

        return await connection.ExecuteAsync(sql, node) > 0;
    }

    public async Task<bool> DeleteNodeAsync(Guid nodeId)
    {
        using var connection = _connectionFactory.CreateConnection();

        // First, update children to point to the deleted node's parent
        var node = await connection.QueryFirstOrDefaultAsync<FolderTemplateNode>(
            "SELECT * FROM FolderTemplateNodes WHERE Id = @Id", new { Id = nodeId });

        if (node != null)
        {
            await connection.ExecuteAsync(
                "UPDATE FolderTemplateNodes SET ParentNodeId = @NewParentId WHERE ParentNodeId = @OldParentId",
                new { NewParentId = node.ParentNodeId, OldParentId = nodeId });
        }

        return await connection.ExecuteAsync(
            "DELETE FROM FolderTemplateNodes WHERE Id = @Id", new { Id = nodeId }) > 0;
    }

    public async Task DeleteAllNodesByTemplateIdAsync(Guid templateId)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            "DELETE FROM FolderTemplateNodes WHERE TemplateId = @TemplateId",
            new { TemplateId = templateId });
    }

    #endregion

    #region Usage Tracking

    public async Task<Guid> RecordUsageAsync(FolderTemplateUsage usage)
    {
        using var connection = _connectionFactory.CreateConnection();
        usage.Id = Guid.NewGuid();
        usage.AppliedAt = DateTime.UtcNow;

        var sql = @"
            INSERT INTO FolderTemplateUsage
                (Id, TemplateId, RootFolderId, CabinetId, AppliedBy, AppliedAt, FoldersCreated)
            VALUES
                (@Id, @TemplateId, @RootFolderId, @CabinetId, @AppliedBy, @AppliedAt, @FoldersCreated)";

        await connection.ExecuteAsync(sql, usage);
        return usage.Id;
    }

    public async Task<IEnumerable<FolderTemplateUsage>> GetUsageByTemplateIdAsync(Guid templateId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = @"
            SELECT u.*,
                   t.Name AS TemplateName,
                   f.Name AS FolderName,
                   usr.DisplayName AS AppliedByName
            FROM FolderTemplateUsage u
            LEFT JOIN FolderTemplates t ON u.TemplateId = t.Id
            LEFT JOIN Folders f ON u.RootFolderId = f.Id
            LEFT JOIN Users usr ON u.AppliedBy = usr.Id
            WHERE u.TemplateId = @TemplateId
            ORDER BY u.AppliedAt DESC";

        return await connection.QueryAsync<FolderTemplateUsage>(sql, new { TemplateId = templateId });
    }

    public async Task<int> GetUsageCountAsync(Guid templateId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM FolderTemplateUsage WHERE TemplateId = @TemplateId",
            new { TemplateId = templateId });
    }

    #endregion

    #region Utilities

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<string>(
            "SELECT DISTINCT Category FROM FolderTemplates WHERE Category IS NOT NULL AND IsActive = 1 ORDER BY Category");
    }

    private List<FolderTemplateNode> BuildNodeTree(List<FolderTemplateNode> flatNodes)
    {
        var lookup = flatNodes.ToDictionary(n => n.Id);
        var rootNodes = new List<FolderTemplateNode>();

        foreach (var node in flatNodes)
        {
            if (node.ParentNodeId == null)
            {
                rootNodes.Add(node);
            }
            else if (lookup.TryGetValue(node.ParentNodeId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
        }

        // Sort children at each level
        SortNodesRecursively(rootNodes);

        return rootNodes;
    }

    private void SortNodesRecursively(List<FolderTemplateNode> nodes)
    {
        nodes.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        foreach (var node in nodes)
        {
            SortNodesRecursively(node.Children);
        }
    }

    #endregion
}
