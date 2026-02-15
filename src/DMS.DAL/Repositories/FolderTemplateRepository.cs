using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class FolderTemplateRepository : IFolderTemplateRepository
{
    private readonly DmsDbContext _context;

    public FolderTemplateRepository(DmsDbContext context)
    {
        _context = context;
    }

    #region Template CRUD

    public async Task<IEnumerable<FolderTemplate>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.FolderTemplates.IgnoreQueryFilters().AsNoTracking()
            : _context.FolderTemplates.AsNoTracking();

        return await query
            .GroupJoin(_context.Users.AsNoTracking(), t => t.CreatedBy, u => u.Id, (t, users) => new { t, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.t, u })
            .GroupJoin(_context.FolderTemplateUsages.AsNoTracking(), x => x.t.Id, fu => fu.TemplateId, (x, usages) => new { x.t, x.u, UsageCount = usages.Count() })
            .Select(x => new FolderTemplate
            {
                Id = x.t.Id,
                Name = x.t.Name,
                Description = x.t.Description,
                Category = x.t.Category,
                Icon = x.t.Icon,
                IsActive = x.t.IsActive,
                IsDefault = x.t.IsDefault,
                CreatedBy = x.t.CreatedBy,
                CreatedAt = x.t.CreatedAt,
                ModifiedBy = x.t.ModifiedBy,
                ModifiedAt = x.t.ModifiedAt,
                CreatedByName = x.u != null ? x.u.DisplayName : null,
                UsageCount = x.UsageCount
            })
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<FolderTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.FolderTemplates.IgnoreQueryFilters().AsNoTracking()
            .Where(t => t.Id == id)
            .GroupJoin(_context.Users.AsNoTracking(), t => t.CreatedBy, u => u.Id, (t, users) => new { t, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.t, u })
            .GroupJoin(_context.FolderTemplateUsages.AsNoTracking(), x => x.t.Id, fu => fu.TemplateId, (x, usages) => new { x.t, x.u, UsageCount = usages.Count() })
            .Select(x => new FolderTemplate
            {
                Id = x.t.Id,
                Name = x.t.Name,
                Description = x.t.Description,
                Category = x.t.Category,
                Icon = x.t.Icon,
                IsActive = x.t.IsActive,
                IsDefault = x.t.IsDefault,
                CreatedBy = x.t.CreatedBy,
                CreatedAt = x.t.CreatedAt,
                ModifiedBy = x.t.ModifiedBy,
                ModifiedAt = x.t.ModifiedAt,
                CreatedByName = x.u != null ? x.u.DisplayName : null,
                UsageCount = x.UsageCount
            })
            .FirstOrDefaultAsync();
    }

    public async Task<FolderTemplate?> GetByIdWithNodesAsync(Guid id)
    {
        var template = await GetByIdAsync(id);
        if (template == null) return null;

        var nodes = await GetNodesByTemplateIdAsync(id);
        template.Nodes = BuildNodeTree(nodes.ToList());

        return template;
    }

    public async Task<IEnumerable<FolderTemplate>> GetByCategoryAsync(string category) =>
        await _context.FolderTemplates.AsNoTracking()
            .Where(t => t.Category == category)
            .GroupJoin(_context.Users.AsNoTracking(), t => t.CreatedBy, u => u.Id, (t, users) => new { t, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.t, u })
            .GroupJoin(_context.FolderTemplateUsages.AsNoTracking(), x => x.t.Id, fu => fu.TemplateId, (x, usages) => new { x.t, x.u, UsageCount = usages.Count() })
            .Select(x => new FolderTemplate
            {
                Id = x.t.Id,
                Name = x.t.Name,
                Description = x.t.Description,
                Category = x.t.Category,
                Icon = x.t.Icon,
                IsActive = x.t.IsActive,
                IsDefault = x.t.IsDefault,
                CreatedBy = x.t.CreatedBy,
                CreatedAt = x.t.CreatedAt,
                ModifiedBy = x.t.ModifiedBy,
                ModifiedAt = x.t.ModifiedAt,
                CreatedByName = x.u != null ? x.u.DisplayName : null,
                UsageCount = x.UsageCount
            })
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<FolderTemplate?> GetDefaultAsync()
    {
        var template = await _context.FolderTemplates.AsNoTracking()
            .Where(t => t.IsDefault)
            .GroupJoin(_context.Users.AsNoTracking(), t => t.CreatedBy, u => u.Id, (t, users) => new { t, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.t, u })
            .GroupJoin(_context.FolderTemplateUsages.AsNoTracking(), x => x.t.Id, fu => fu.TemplateId, (x, usages) => new { x.t, x.u, UsageCount = usages.Count() })
            .Select(x => new FolderTemplate
            {
                Id = x.t.Id,
                Name = x.t.Name,
                Description = x.t.Description,
                Category = x.t.Category,
                Icon = x.t.Icon,
                IsActive = x.t.IsActive,
                IsDefault = x.t.IsDefault,
                CreatedBy = x.t.CreatedBy,
                CreatedAt = x.t.CreatedAt,
                ModifiedBy = x.t.ModifiedBy,
                ModifiedAt = x.t.ModifiedAt,
                CreatedByName = x.u != null ? x.u.DisplayName : null,
                UsageCount = x.UsageCount
            })
            .FirstOrDefaultAsync();

        if (template != null)
        {
            var nodes = await GetNodesByTemplateIdAsync(template.Id);
            template.Nodes = BuildNodeTree(nodes.ToList());
        }
        return template;
    }

    public async Task<Guid> CreateAsync(FolderTemplate template)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.Now;

        // If setting as default, clear other defaults first
        if (template.IsDefault)
        {
            await _context.FolderTemplates
                .Where(t => t.IsDefault)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.IsDefault, false));
        }

        _context.FolderTemplates.Add(template);
        await _context.SaveChangesAsync();

        return template.Id;
    }

    public async Task<bool> UpdateAsync(FolderTemplate template)
    {
        template.ModifiedAt = DateTime.Now;

        // If setting as default, clear other defaults first
        if (template.IsDefault)
        {
            await _context.FolderTemplates
                .Where(t => t.IsDefault && t.Id != template.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(t => t.IsDefault, false));
        }

        return await _context.FolderTemplates
            .Where(t => t.Id == template.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.Name, template.Name)
                .SetProperty(t => t.Description, template.Description)
                .SetProperty(t => t.Category, template.Category)
                .SetProperty(t => t.Icon, template.Icon)
                .SetProperty(t => t.IsActive, template.IsActive)
                .SetProperty(t => t.IsDefault, template.IsDefault)
                .SetProperty(t => t.ModifiedBy, template.ModifiedBy)
                .SetProperty(t => t.ModifiedAt, template.ModifiedAt)) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        // Soft delete
        await _context.FolderTemplates
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.IsActive, false)
                .SetProperty(t => t.ModifiedAt, DateTime.Now)) > 0;

    #endregion

    #region Node Management

    public async Task<IEnumerable<FolderTemplateNode>> GetNodesByTemplateIdAsync(Guid templateId) =>
        await _context.FolderTemplateNodes.AsNoTracking()
            .Where(n => n.TemplateId == templateId)
            .GroupJoin(_context.ContentTypes.AsNoTracking(), n => n.ContentTypeId, ct => ct.Id, (n, cts) => new { n, cts })
            .SelectMany(x => x.cts.DefaultIfEmpty(), (x, ct) => new FolderTemplateNode
            {
                Id = x.n.Id,
                TemplateId = x.n.TemplateId,
                ParentNodeId = x.n.ParentNodeId,
                Name = x.n.Name,
                Description = x.n.Description,
                ContentTypeId = x.n.ContentTypeId,
                SortOrder = x.n.SortOrder,
                BreakContentTypeInheritance = x.n.BreakContentTypeInheritance,
                ContentTypeName = ct != null ? ct.DisplayName : null
            })
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

    public async Task<Guid> CreateNodeAsync(FolderTemplateNode node)
    {
        node.Id = Guid.NewGuid();

        _context.FolderTemplateNodes.Add(node);
        await _context.SaveChangesAsync();

        return node.Id;
    }

    public async Task<bool> UpdateNodeAsync(FolderTemplateNode node) =>
        await _context.FolderTemplateNodes
            .Where(n => n.Id == node.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.ParentNodeId, node.ParentNodeId)
                .SetProperty(n => n.Name, node.Name)
                .SetProperty(n => n.Description, node.Description)
                .SetProperty(n => n.ContentTypeId, node.ContentTypeId)
                .SetProperty(n => n.SortOrder, node.SortOrder)
                .SetProperty(n => n.BreakContentTypeInheritance, node.BreakContentTypeInheritance)) > 0;

    public async Task<bool> DeleteNodeAsync(Guid nodeId)
    {
        // First, update children to point to the deleted node's parent
        var node = await _context.FolderTemplateNodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId);

        if (node != null)
        {
            await _context.FolderTemplateNodes
                .Where(n => n.ParentNodeId == nodeId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.ParentNodeId, node.ParentNodeId));
        }

        return await _context.FolderTemplateNodes
            .Where(n => n.Id == nodeId)
            .ExecuteDeleteAsync() > 0;
    }

    public async Task DeleteAllNodesByTemplateIdAsync(Guid templateId) =>
        await _context.FolderTemplateNodes
            .Where(n => n.TemplateId == templateId)
            .ExecuteDeleteAsync();

    #endregion

    #region Usage Tracking

    public async Task<Guid> RecordUsageAsync(FolderTemplateUsage usage)
    {
        usage.Id = Guid.NewGuid();
        usage.AppliedAt = DateTime.Now;

        _context.FolderTemplateUsages.Add(usage);
        await _context.SaveChangesAsync();

        return usage.Id;
    }

    public async Task<IEnumerable<FolderTemplateUsage>> GetUsageByTemplateIdAsync(Guid templateId) =>
        await _context.FolderTemplateUsages.AsNoTracking()
            .Where(u => u.TemplateId == templateId)
            .GroupJoin(_context.FolderTemplates.IgnoreQueryFilters().AsNoTracking(), u => u.TemplateId, t => t.Id, (u, templates) => new { u, templates })
            .SelectMany(x => x.templates.DefaultIfEmpty(), (x, t) => new { x.u, t })
            .GroupJoin(_context.Folders.AsNoTracking(), x => x.u.RootFolderId, f => f.Id, (x, folders) => new { x.u, x.t, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.u, x.t, f })
            .GroupJoin(_context.Users.AsNoTracking(), x => x.u.AppliedBy, usr => usr.Id, (x, users) => new { x.u, x.t, x.f, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, usr) => new FolderTemplateUsage
            {
                Id = x.u.Id,
                TemplateId = x.u.TemplateId,
                RootFolderId = x.u.RootFolderId,
                CabinetId = x.u.CabinetId,
                AppliedBy = x.u.AppliedBy,
                AppliedAt = x.u.AppliedAt,
                FoldersCreated = x.u.FoldersCreated,
                TemplateName = x.t != null ? x.t.Name : null,
                FolderName = x.f != null ? x.f.Name : null,
                AppliedByName = usr != null ? usr.DisplayName : null
            })
            .OrderByDescending(u => u.AppliedAt)
            .ToListAsync();

    public async Task<int> GetUsageCountAsync(Guid templateId) =>
        await _context.FolderTemplateUsages.AsNoTracking()
            .CountAsync(u => u.TemplateId == templateId);

    #endregion

    #region Utilities

    public async Task<IEnumerable<string>> GetCategoriesAsync() =>
        await _context.FolderTemplates.AsNoTracking()
            .Where(t => t.Category != null)
            .Select(t => t.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

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
