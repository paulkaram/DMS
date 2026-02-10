using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class BookmarkRepository : IBookmarkRepository
{
    private readonly DmsDbContext _context;

    public BookmarkRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<Bookmark>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.Bookmarks.IgnoreQueryFilters()
            : _context.Bookmarks.AsQueryable();

        return await query
            .AsNoTracking()
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<Bookmark?> GetByIdAsync(Guid id)
    {
        return await _context.Bookmarks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Bookmark?> GetByPlaceholderAsync(string placeholder)
    {
        return await _context.Bookmarks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Placeholder == placeholder);
    }

    public async Task<Guid> CreateAsync(Bookmark bookmark)
    {
        bookmark.Id = Guid.NewGuid();
        bookmark.CreatedAt = DateTime.UtcNow;

        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();
        return bookmark.Id;
    }

    public async Task<bool> UpdateAsync(Bookmark bookmark)
    {
        bookmark.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.Bookmarks.FindAsync(bookmark.Id);
        if (existing == null) return false;

        existing.Name = bookmark.Name;
        existing.Placeholder = bookmark.Placeholder;
        existing.Description = bookmark.Description;
        existing.DefaultValue = bookmark.DefaultValue;
        existing.DataType = bookmark.DataType;
        existing.LookupName = bookmark.LookupName;
        existing.IsSystem = bookmark.IsSystem;
        existing.IsActive = bookmark.IsActive;
        existing.SortOrder = bookmark.SortOrder;
        existing.ModifiedBy = bookmark.ModifiedBy;
        existing.ModifiedAt = bookmark.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.Bookmarks
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.IsActive, false)
                .SetProperty(b => b.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class CaseRepository : ICaseRepository
{
    private readonly DmsDbContext _context;

    public CaseRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<Case>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.Cases.IgnoreQueryFilters()
            : _context.Cases.AsQueryable();

        return await query
            .AsNoTracking()
            .GroupJoin(_context.Users.AsNoTracking(), c => c.AssignedToUserId, u => u.Id, (c, users) => new { c, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.c, u })
            .GroupJoin(_context.Folders.AsNoTracking(), x => x.c.FolderId, f => f.Id, (x, folders) => new { x.c, x.u, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new Case
            {
                Id = x.c.Id,
                CaseNumber = x.c.CaseNumber,
                Title = x.c.Title,
                Description = x.c.Description,
                Status = x.c.Status,
                Priority = x.c.Priority,
                AssignedToUserId = x.c.AssignedToUserId,
                FolderId = x.c.FolderId,
                DueDate = x.c.DueDate,
                ClosedDate = x.c.ClosedDate,
                IsActive = x.c.IsActive,
                CreatedBy = x.c.CreatedBy,
                CreatedAt = x.c.CreatedAt,
                ModifiedBy = x.c.ModifiedBy,
                ModifiedAt = x.c.ModifiedAt,
                AssignedToUserName = x.u != null ? x.u.DisplayName : null,
                FolderName = f != null ? f.Name : null
            })
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Case?> GetByIdAsync(Guid id)
    {
        return await _context.Cases
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.Id == id)
            .GroupJoin(_context.Users.AsNoTracking(), c => c.AssignedToUserId, u => u.Id, (c, users) => new { c, users })
            .SelectMany(x => x.users.DefaultIfEmpty(), (x, u) => new { x.c, u })
            .GroupJoin(_context.Folders.AsNoTracking(), x => x.c.FolderId, f => f.Id, (x, folders) => new { x.c, x.u, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new Case
            {
                Id = x.c.Id,
                CaseNumber = x.c.CaseNumber,
                Title = x.c.Title,
                Description = x.c.Description,
                Status = x.c.Status,
                Priority = x.c.Priority,
                AssignedToUserId = x.c.AssignedToUserId,
                FolderId = x.c.FolderId,
                DueDate = x.c.DueDate,
                ClosedDate = x.c.ClosedDate,
                IsActive = x.c.IsActive,
                CreatedBy = x.c.CreatedBy,
                CreatedAt = x.c.CreatedAt,
                ModifiedBy = x.c.ModifiedBy,
                ModifiedAt = x.c.ModifiedAt,
                AssignedToUserName = x.u != null ? x.u.DisplayName : null,
                FolderName = f != null ? f.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<Case?> GetByCaseNumberAsync(string caseNumber)
    {
        return await _context.Cases
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CaseNumber == caseNumber);
    }

    public async Task<IEnumerable<Case>> GetByStatusAsync(string status)
    {
        return await _context.Cases
            .AsNoTracking()
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Case>> GetByAssigneeAsync(Guid userId)
    {
        return await _context.Cases
            .AsNoTracking()
            .Where(c => c.AssignedToUserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(Case caseEntity)
    {
        caseEntity.Id = Guid.NewGuid();
        caseEntity.CreatedAt = DateTime.UtcNow;

        _context.Cases.Add(caseEntity);
        await _context.SaveChangesAsync();
        return caseEntity.Id;
    }

    public async Task<bool> UpdateAsync(Case caseEntity)
    {
        caseEntity.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.Cases.FindAsync(caseEntity.Id);
        if (existing == null) return false;

        existing.CaseNumber = caseEntity.CaseNumber;
        existing.Title = caseEntity.Title;
        existing.Description = caseEntity.Description;
        existing.Status = caseEntity.Status;
        existing.Priority = caseEntity.Priority;
        existing.AssignedToUserId = caseEntity.AssignedToUserId;
        existing.FolderId = caseEntity.FolderId;
        existing.DueDate = caseEntity.DueDate;
        existing.ClosedDate = caseEntity.ClosedDate;
        existing.IsActive = caseEntity.IsActive;
        existing.ModifiedBy = caseEntity.ModifiedBy;
        existing.ModifiedAt = caseEntity.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, Guid userId)
    {
        var now = DateTime.UtcNow;
        var closedDate = status == "Closed" || status == "Archived" ? now : (DateTime?)null;

        return await _context.Cases
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Status, status)
                .SetProperty(c => c.ClosedDate, closedDate)
                .SetProperty(c => c.ModifiedBy, userId)
                .SetProperty(c => c.ModifiedAt, now)) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.Cases
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.IsActive, false)
                .SetProperty(c => c.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class EndpointRepository : IEndpointRepository
{
    private readonly DmsDbContext _context;

    public EndpointRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<ServiceEndpoint>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.ServiceEndpoints.IgnoreQueryFilters()
            : _context.ServiceEndpoints.AsQueryable();

        return await query
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<ServiceEndpoint?> GetByIdAsync(Guid id)
    {
        return await _context.ServiceEndpoints
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<ServiceEndpoint>> GetByTypeAsync(string endpointType)
    {
        return await _context.ServiceEndpoints
            .AsNoTracking()
            .Where(e => e.EndpointType == endpointType)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(ServiceEndpoint endpoint)
    {
        endpoint.Id = Guid.NewGuid();
        endpoint.CreatedAt = DateTime.UtcNow;

        _context.ServiceEndpoints.Add(endpoint);
        await _context.SaveChangesAsync();
        return endpoint.Id;
    }

    public async Task<bool> UpdateAsync(ServiceEndpoint endpoint)
    {
        endpoint.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.ServiceEndpoints.FindAsync(endpoint.Id);
        if (existing == null) return false;

        existing.Name = endpoint.Name;
        existing.Url = endpoint.Url;
        existing.Description = endpoint.Description;
        existing.EndpointType = endpoint.EndpointType;
        existing.AuthType = endpoint.AuthType;
        existing.AuthConfig = endpoint.AuthConfig;
        existing.TimeoutSeconds = endpoint.TimeoutSeconds;
        existing.RetryCount = endpoint.RetryCount;
        existing.Headers = endpoint.Headers;
        existing.IsActive = endpoint.IsActive;
        existing.ModifiedBy = endpoint.ModifiedBy;
        existing.ModifiedAt = endpoint.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateHealthStatusAsync(Guid id, string status)
    {
        return await _context.ServiceEndpoints
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.LastHealthCheck, DateTime.UtcNow)
                .SetProperty(e => e.LastHealthStatus, status)) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.ServiceEndpoints
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsActive, false)
                .SetProperty(e => e.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class ExportConfigRepository : IExportConfigRepository
{
    private readonly DmsDbContext _context;

    public ExportConfigRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<ExportConfig>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.ExportConfigs.IgnoreQueryFilters()
            : _context.ExportConfigs.AsQueryable();

        return await query
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    public async Task<ExportConfig?> GetByIdAsync(Guid id)
    {
        return await _context.ExportConfigs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<ExportConfig?> GetDefaultAsync()
    {
        return await _context.ExportConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IsDefault);
    }

    public async Task<Guid> CreateAsync(ExportConfig config)
    {
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;

        _context.ExportConfigs.Add(config);
        await _context.SaveChangesAsync();
        return config.Id;
    }

    public async Task<bool> UpdateAsync(ExportConfig config)
    {
        config.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.ExportConfigs.FindAsync(config.Id);
        if (existing == null) return false;

        existing.Name = config.Name;
        existing.Description = config.Description;
        existing.ExportFormat = config.ExportFormat;
        existing.IncludeMetadata = config.IncludeMetadata;
        existing.IncludeVersions = config.IncludeVersions;
        existing.IncludeAuditTrail = config.IncludeAuditTrail;
        existing.FlattenFolders = config.FlattenFolders;
        existing.NamingPattern = config.NamingPattern;
        existing.WatermarkText = config.WatermarkText;
        existing.MaxFileSizeMB = config.MaxFileSizeMB;
        existing.IsDefault = config.IsDefault;
        existing.IsActive = config.IsActive;
        existing.ModifiedBy = config.ModifiedBy;
        existing.ModifiedAt = config.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.ExportConfigs
                    .IgnoreQueryFilters()
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
                await _context.ExportConfigs
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, true));
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.ExportConfigs
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.IsActive, false)
                .SetProperty(e => e.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class NamingConventionRepository : INamingConventionRepository
{
    private readonly DmsDbContext _context;

    public NamingConventionRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<NamingConvention>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.NamingConventions.IgnoreQueryFilters()
            : _context.NamingConventions.AsQueryable();

        return await query
            .AsNoTracking()
            .GroupJoin(_context.Folders.AsNoTracking(), nc => nc.FolderId, f => f.Id, (nc, folders) => new { nc, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.nc, f })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.nc.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.nc, x.f, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new NamingConvention
            {
                Id = x.nc.Id,
                Name = x.nc.Name,
                Description = x.nc.Description,
                Pattern = x.nc.Pattern,
                AppliesTo = x.nc.AppliesTo,
                FolderId = x.nc.FolderId,
                DocumentTypeId = x.nc.DocumentTypeId,
                IsRequired = x.nc.IsRequired,
                AutoGenerate = x.nc.AutoGenerate,
                Separator = x.nc.Separator,
                IsActive = x.nc.IsActive,
                SortOrder = x.nc.SortOrder,
                CreatedBy = x.nc.CreatedBy,
                CreatedAt = x.nc.CreatedAt,
                ModifiedBy = x.nc.ModifiedBy,
                ModifiedAt = x.nc.ModifiedAt,
                FolderName = x.f != null ? x.f.Name : null,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .OrderBy(nc => nc.SortOrder)
            .ThenBy(nc => nc.Name)
            .ToListAsync();
    }

    public async Task<NamingConvention?> GetByIdAsync(Guid id)
    {
        return await _context.NamingConventions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(nc => nc.Id == id)
            .GroupJoin(_context.Folders.AsNoTracking(), nc => nc.FolderId, f => f.Id, (nc, folders) => new { nc, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.nc, f })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.nc.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.nc, x.f, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new NamingConvention
            {
                Id = x.nc.Id,
                Name = x.nc.Name,
                Description = x.nc.Description,
                Pattern = x.nc.Pattern,
                AppliesTo = x.nc.AppliesTo,
                FolderId = x.nc.FolderId,
                DocumentTypeId = x.nc.DocumentTypeId,
                IsRequired = x.nc.IsRequired,
                AutoGenerate = x.nc.AutoGenerate,
                Separator = x.nc.Separator,
                IsActive = x.nc.IsActive,
                SortOrder = x.nc.SortOrder,
                CreatedBy = x.nc.CreatedBy,
                CreatedAt = x.nc.CreatedAt,
                ModifiedBy = x.nc.ModifiedBy,
                ModifiedAt = x.nc.ModifiedAt,
                FolderName = x.f != null ? x.f.Name : null,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<NamingConvention>> GetByFolderAsync(Guid folderId)
    {
        return await _context.NamingConventions
            .AsNoTracking()
            .Where(nc => nc.FolderId == folderId)
            .OrderBy(nc => nc.SortOrder)
            .ToListAsync();
    }

    public async Task<NamingConvention?> GetApplicableAsync(Guid? folderId, Guid? documentTypeId, string appliesTo)
    {
        return await _context.NamingConventions
            .AsNoTracking()
            .Where(nc => (nc.AppliesTo == appliesTo || nc.AppliesTo == "Both")
                && (nc.FolderId == folderId || nc.FolderId == null)
                && (nc.DocumentTypeId == documentTypeId || nc.DocumentTypeId == null))
            .OrderBy(nc => nc.FolderId == folderId ? 0 : 1)
            .ThenBy(nc => nc.DocumentTypeId == documentTypeId ? 0 : 1)
            .ThenBy(nc => nc.SortOrder)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid> CreateAsync(NamingConvention convention)
    {
        convention.Id = Guid.NewGuid();
        convention.CreatedAt = DateTime.UtcNow;

        _context.NamingConventions.Add(convention);
        await _context.SaveChangesAsync();
        return convention.Id;
    }

    public async Task<bool> UpdateAsync(NamingConvention convention)
    {
        convention.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.NamingConventions.FindAsync(convention.Id);
        if (existing == null) return false;

        existing.Name = convention.Name;
        existing.Description = convention.Description;
        existing.Pattern = convention.Pattern;
        existing.AppliesTo = convention.AppliesTo;
        existing.FolderId = convention.FolderId;
        existing.DocumentTypeId = convention.DocumentTypeId;
        existing.IsRequired = convention.IsRequired;
        existing.AutoGenerate = convention.AutoGenerate;
        existing.Separator = convention.Separator;
        existing.IsActive = convention.IsActive;
        existing.SortOrder = convention.SortOrder;
        existing.ModifiedBy = convention.ModifiedBy;
        existing.ModifiedAt = convention.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.NamingConventions
            .Where(nc => nc.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(nc => nc.IsActive, false)
                .SetProperty(nc => nc.ModifiedAt, DateTime.UtcNow)) > 0;
    }

    public Task<string> GenerateNameAsync(Guid conventionId, Dictionary<string, string> values)
    {
        // This would need to be implemented with actual pattern replacement logic
        return Task.FromResult(string.Empty);
    }
}

public class OrganizationTemplateRepository : IOrganizationTemplateRepository
{
    private readonly DmsDbContext _context;

    public OrganizationTemplateRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<OrganizationTemplate>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.OrganizationTemplates.IgnoreQueryFilters()
            : _context.OrganizationTemplates.AsQueryable();

        return await query
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<OrganizationTemplate?> GetByIdAsync(Guid id)
    {
        return await _context.OrganizationTemplates
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<OrganizationTemplate?> GetDefaultAsync()
    {
        return await _context.OrganizationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsDefault);
    }

    public async Task<Guid> CreateAsync(OrganizationTemplate template)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;

        _context.OrganizationTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template.Id;
    }

    public async Task<bool> UpdateAsync(OrganizationTemplate template)
    {
        template.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.OrganizationTemplates.FindAsync(template.Id);
        if (existing == null) return false;

        existing.Name = template.Name;
        existing.Description = template.Description;
        existing.Structure = template.Structure;
        existing.DefaultPermissions = template.DefaultPermissions;
        existing.IncludeContentTypes = template.IncludeContentTypes;
        existing.IsDefault = template.IsDefault;
        existing.IsActive = template.IsActive;
        existing.ModifiedBy = template.ModifiedBy;
        existing.ModifiedAt = template.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.OrganizationTemplates
                    .IgnoreQueryFilters()
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
                await _context.OrganizationTemplates
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, true));
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.OrganizationTemplates
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsActive, false)
                .SetProperty(t => t.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class PermissionLevelDefinitionRepository : IPermissionLevelDefinitionRepository
{
    private readonly DmsDbContext _context;

    public PermissionLevelDefinitionRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<PermissionLevelDefinition>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.PermissionLevelDefinitions.IgnoreQueryFilters()
            : _context.PermissionLevelDefinitions.AsQueryable();

        return await query
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Level)
            .ToListAsync();
    }

    public async Task<PermissionLevelDefinition?> GetByIdAsync(Guid id)
    {
        return await _context.PermissionLevelDefinitions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PermissionLevelDefinition?> GetByLevelAsync(int level)
    {
        return await _context.PermissionLevelDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Level == level);
    }

    public async Task<Guid> CreateAsync(PermissionLevelDefinition definition)
    {
        definition.Id = Guid.NewGuid();
        definition.CreatedAt = DateTime.UtcNow;

        _context.PermissionLevelDefinitions.Add(definition);
        await _context.SaveChangesAsync();
        return definition.Id;
    }

    public async Task<bool> UpdateAsync(PermissionLevelDefinition definition)
    {
        definition.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.PermissionLevelDefinitions.FindAsync(definition.Id);
        if (existing == null) return false;

        existing.Name = definition.Name;
        existing.Description = definition.Description;
        existing.Level = definition.Level;
        existing.Color = definition.Color;
        existing.Icon = definition.Icon;
        existing.CanRead = definition.CanRead;
        existing.CanWrite = definition.CanWrite;
        existing.CanDelete = definition.CanDelete;
        existing.CanAdmin = definition.CanAdmin;
        existing.CanShare = definition.CanShare;
        existing.CanExport = definition.CanExport;
        existing.IsSystem = definition.IsSystem;
        existing.IsActive = definition.IsActive;
        existing.SortOrder = definition.SortOrder;
        existing.ModifiedBy = definition.ModifiedBy;
        existing.ModifiedAt = definition.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.PermissionLevelDefinitions
            .Where(p => p.Id == id && !p.IsSystem)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class PurposeRepository : IPurposeRepository
{
    private readonly DmsDbContext _context;

    public PurposeRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<Purpose>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.Purposes.IgnoreQueryFilters()
            : _context.Purposes.AsQueryable();

        return await query
            .AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Purpose?> GetByIdAsync(Guid id)
    {
        return await _context.Purposes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Purpose>> GetByTypeAsync(string purposeType)
    {
        return await _context.Purposes
            .AsNoTracking()
            .Where(p => p.PurposeType == purposeType)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<Purpose?> GetDefaultAsync(string purposeType)
    {
        return await _context.Purposes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PurposeType == purposeType && p.IsDefault);
    }

    public async Task<Guid> CreateAsync(Purpose purpose)
    {
        purpose.Id = Guid.NewGuid();
        purpose.CreatedAt = DateTime.UtcNow;

        _context.Purposes.Add(purpose);
        await _context.SaveChangesAsync();
        return purpose.Id;
    }

    public async Task<bool> UpdateAsync(Purpose purpose)
    {
        purpose.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.Purposes.FindAsync(purpose.Id);
        if (existing == null) return false;

        existing.Name = purpose.Name;
        existing.Description = purpose.Description;
        existing.PurposeType = purpose.PurposeType;
        existing.RequiresJustification = purpose.RequiresJustification;
        existing.RequiresApproval = purpose.RequiresApproval;
        existing.IsDefault = purpose.IsDefault;
        existing.IsActive = purpose.IsActive;
        existing.SortOrder = purpose.SortOrder;
        existing.ModifiedBy = purpose.ModifiedBy;
        existing.ModifiedAt = purpose.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.Purposes
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class ScanConfigRepository : IScanConfigRepository
{
    private readonly DmsDbContext _context;

    public ScanConfigRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<ScanConfig>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.ScanConfigs.IgnoreQueryFilters()
            : _context.ScanConfigs.AsQueryable();

        return await query
            .AsNoTracking()
            .GroupJoin(_context.Folders.AsNoTracking(), sc => sc.TargetFolderId, f => f.Id, (sc, folders) => new { sc, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new ScanConfig
            {
                Id = x.sc.Id,
                Name = x.sc.Name,
                Description = x.sc.Description,
                Resolution = x.sc.Resolution,
                ColorMode = x.sc.ColorMode,
                OutputFormat = x.sc.OutputFormat,
                EnableOCR = x.sc.EnableOCR,
                OcrLanguage = x.sc.OcrLanguage,
                AutoDeskew = x.sc.AutoDeskew,
                AutoCrop = x.sc.AutoCrop,
                RemoveBlankPages = x.sc.RemoveBlankPages,
                CompressionQuality = x.sc.CompressionQuality,
                TargetFolderId = x.sc.TargetFolderId,
                IsDefault = x.sc.IsDefault,
                IsActive = x.sc.IsActive,
                CreatedBy = x.sc.CreatedBy,
                CreatedAt = x.sc.CreatedAt,
                ModifiedBy = x.sc.ModifiedBy,
                ModifiedAt = x.sc.ModifiedAt,
                TargetFolderName = f != null ? f.Name : null
            })
            .OrderBy(sc => sc.Name)
            .ToListAsync();
    }

    public async Task<ScanConfig?> GetByIdAsync(Guid id)
    {
        return await _context.ScanConfigs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(sc => sc.Id == id)
            .GroupJoin(_context.Folders.AsNoTracking(), sc => sc.TargetFolderId, f => f.Id, (sc, folders) => new { sc, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new ScanConfig
            {
                Id = x.sc.Id,
                Name = x.sc.Name,
                Description = x.sc.Description,
                Resolution = x.sc.Resolution,
                ColorMode = x.sc.ColorMode,
                OutputFormat = x.sc.OutputFormat,
                EnableOCR = x.sc.EnableOCR,
                OcrLanguage = x.sc.OcrLanguage,
                AutoDeskew = x.sc.AutoDeskew,
                AutoCrop = x.sc.AutoCrop,
                RemoveBlankPages = x.sc.RemoveBlankPages,
                CompressionQuality = x.sc.CompressionQuality,
                TargetFolderId = x.sc.TargetFolderId,
                IsDefault = x.sc.IsDefault,
                IsActive = x.sc.IsActive,
                CreatedBy = x.sc.CreatedBy,
                CreatedAt = x.sc.CreatedAt,
                ModifiedBy = x.sc.ModifiedBy,
                ModifiedAt = x.sc.ModifiedAt,
                TargetFolderName = f != null ? f.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ScanConfig?> GetDefaultAsync()
    {
        return await _context.ScanConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.IsDefault);
    }

    public async Task<Guid> CreateAsync(ScanConfig config)
    {
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;

        _context.ScanConfigs.Add(config);
        await _context.SaveChangesAsync();
        return config.Id;
    }

    public async Task<bool> UpdateAsync(ScanConfig config)
    {
        config.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.ScanConfigs.FindAsync(config.Id);
        if (existing == null) return false;

        existing.Name = config.Name;
        existing.Description = config.Description;
        existing.Resolution = config.Resolution;
        existing.ColorMode = config.ColorMode;
        existing.OutputFormat = config.OutputFormat;
        existing.EnableOCR = config.EnableOCR;
        existing.OcrLanguage = config.OcrLanguage;
        existing.AutoDeskew = config.AutoDeskew;
        existing.AutoCrop = config.AutoCrop;
        existing.RemoveBlankPages = config.RemoveBlankPages;
        existing.CompressionQuality = config.CompressionQuality;
        existing.TargetFolderId = config.TargetFolderId;
        existing.IsDefault = config.IsDefault;
        existing.IsActive = config.IsActive;
        existing.ModifiedBy = config.ModifiedBy;
        existing.ModifiedAt = config.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.ScanConfigs
                    .IgnoreQueryFilters()
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
                await _context.ScanConfigs
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, true));
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.ScanConfigs
            .Where(sc => sc.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(sc => sc.IsActive, false)
                .SetProperty(sc => sc.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}

public class SearchConfigRepository : ISearchConfigRepository
{
    private readonly DmsDbContext _context;

    public SearchConfigRepository(DmsDbContext context) => _context = context;

    public async Task<IEnumerable<SearchConfig>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.SearchConfigs.IgnoreQueryFilters()
            : _context.SearchConfigs.AsQueryable();

        return await query
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<SearchConfig>> GetGlobalAsync()
    {
        return await _context.SearchConfigs
            .AsNoTracking()
            .Where(s => s.IsGlobal)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<SearchConfig>> GetByUserAsync(Guid userId)
    {
        return await _context.SearchConfigs
            .AsNoTracking()
            .Where(s => s.IsGlobal || s.CreatedBy == userId)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<SearchConfig?> GetByIdAsync(Guid id)
    {
        return await _context.SearchConfigs
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SearchConfig?> GetDefaultAsync()
    {
        return await _context.SearchConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IsDefault);
    }

    public async Task<Guid> CreateAsync(SearchConfig config)
    {
        config.Id = Guid.NewGuid();
        config.CreatedAt = DateTime.UtcNow;

        _context.SearchConfigs.Add(config);
        await _context.SaveChangesAsync();
        return config.Id;
    }

    public async Task<bool> UpdateAsync(SearchConfig config)
    {
        config.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.SearchConfigs.FindAsync(config.Id);
        if (existing == null) return false;

        existing.Name = config.Name;
        existing.Description = config.Description;
        existing.SearchType = config.SearchType;
        existing.DefaultFields = config.DefaultFields;
        existing.Filters = config.Filters;
        existing.IncludeContent = config.IncludeContent;
        existing.IncludeMetadata = config.IncludeMetadata;
        existing.IncludeVersions = config.IncludeVersions;
        existing.FuzzyMatch = config.FuzzyMatch;
        existing.MaxResults = config.MaxResults;
        existing.SortField = config.SortField;
        existing.SortDirection = config.SortDirection;
        existing.IsGlobal = config.IsGlobal;
        existing.IsDefault = config.IsDefault;
        existing.IsActive = config.IsActive;
        existing.ModifiedBy = config.ModifiedBy;
        existing.ModifiedAt = config.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> SetDefaultAsync(Guid id)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SearchConfigs
                    .IgnoreQueryFilters()
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
                await _context.SearchConfigs
                    .IgnoreQueryFilters()
                    .Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, true));
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        });
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.SearchConfigs
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.ModifiedAt, DateTime.UtcNow)) > 0;
    }
}
