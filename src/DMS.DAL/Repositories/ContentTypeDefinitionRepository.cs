using DMS.DAL.Data;
using DMS.DAL.DTOs;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class ContentTypeDefinitionRepository : IContentTypeDefinitionRepository
{
    private readonly DmsDbContext _context;

    public ContentTypeDefinitionRepository(DmsDbContext context)
    {
        _context = context;
    }

    #region Content Type Definitions

    public async Task<IEnumerable<ContentTypeDefinition>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.ContentTypeDefinitions.AsNoTracking();

        if (includeInactive)
            query = query.IgnoreQueryFilters();

        return await query
            .OrderBy(ct => ct.SortOrder)
            .ThenBy(ct => ct.Name)
            .ToListAsync();
    }

    public async Task<ContentTypeDefinition?> GetByIdAsync(Guid id)
    {
        return await _context.ContentTypeDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Id == id);
    }

    public async Task<ContentTypeDefinition?> GetByIdWithFieldsAsync(Guid id)
    {
        return await _context.ContentTypeDefinitions
            .AsNoTracking()
            .Include(ct => ct.Fields!.Where(f => f.IsActive).OrderBy(f => f.SortOrder))
            .FirstOrDefaultAsync(ct => ct.Id == id);
    }

    public async Task<ContentTypeDefinition?> GetByNameAsync(string name)
    {
        return await _context.ContentTypeDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Name == name);
    }

    public async Task<ContentTypeDefinition?> GetSystemDefaultAsync()
    {
        return await _context.ContentTypeDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.IsSystemDefault);
    }

    public async Task<bool> SetSystemDefaultAsync(Guid contentTypeId, Guid userId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;

                // Clear existing system default
                await _context.ContentTypeDefinitions
                    .Where(ct => ct.IsSystemDefault)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(ct => ct.IsSystemDefault, false)
                        .SetProperty(ct => ct.ModifiedBy, userId)
                        .SetProperty(ct => ct.ModifiedAt, now));

                // Set new system default
                var target = await _context.ContentTypeDefinitions.FindAsync(contentTypeId);
                if (target == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                target.IsSystemDefault = true;
                target.ModifiedBy = userId;
                target.ModifiedAt = now;

                await _context.SaveChangesAsync();
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

    public async Task<bool> ClearSystemDefaultAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var affected = await _context.ContentTypeDefinitions
            .Where(ct => ct.IsSystemDefault)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ct => ct.IsSystemDefault, false)
                .SetProperty(ct => ct.ModifiedBy, userId)
                .SetProperty(ct => ct.ModifiedAt, now));

        return affected > 0;
    }

    public async Task<Guid> CreateAsync(ContentTypeDefinition contentType)
    {
        contentType.Id = Guid.NewGuid();
        contentType.CreatedAt = DateTime.UtcNow;

        _context.ContentTypeDefinitions.Add(contentType);
        await _context.SaveChangesAsync();
        return contentType.Id;
    }

    public async Task<bool> UpdateAsync(ContentTypeDefinition contentType)
    {
        contentType.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.ContentTypeDefinitions.FindAsync(contentType.Id);
        if (existing == null) return false;

        _context.Entry(existing).CurrentValues.SetValues(contentType);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        // Soft delete - just mark as inactive
        var entity = await _context.ContentTypeDefinitions.FindAsync(id);
        if (entity == null) return false;

        entity.IsActive = false;
        entity.ModifiedAt = DateTime.UtcNow;
        return await _context.SaveChangesAsync() > 0;
    }

    #endregion

    #region Content Type Fields

    public async Task<IEnumerable<ContentTypeField>> GetFieldsAsync(Guid contentTypeId)
    {
        return await _context.ContentTypeFields
            .AsNoTracking()
            .Where(f => f.ContentTypeId == contentTypeId && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .ToListAsync();
    }

    public async Task<ContentTypeField?> GetFieldByIdAsync(Guid fieldId)
    {
        return await _context.ContentTypeFields
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fieldId);
    }

    public async Task<Guid> CreateFieldAsync(ContentTypeField field)
    {
        field.Id = Guid.NewGuid();
        field.CreatedAt = DateTime.UtcNow;

        // Get max sort order
        var maxOrder = await _context.ContentTypeFields
            .Where(f => f.ContentTypeId == field.ContentTypeId)
            .MaxAsync(f => (int?)f.SortOrder);
        field.SortOrder = (maxOrder ?? 0) + 1;

        _context.ContentTypeFields.Add(field);
        await _context.SaveChangesAsync();
        return field.Id;
    }

    public async Task<bool> UpdateFieldAsync(ContentTypeField field)
    {
        var existing = await _context.ContentTypeFields.FindAsync(field.Id);
        if (existing == null) return false;

        existing.FieldName = field.FieldName;
        existing.DisplayName = field.DisplayName;
        existing.Description = field.Description;
        existing.FieldType = field.FieldType;
        existing.IsRequired = field.IsRequired;
        existing.IsReadOnly = field.IsReadOnly;
        existing.ShowInList = field.ShowInList;
        existing.IsSearchable = field.IsSearchable;
        existing.DefaultValue = field.DefaultValue;
        existing.ValidationRules = field.ValidationRules;
        existing.LookupName = field.LookupName;
        existing.Options = field.Options;
        existing.SortOrder = field.SortOrder;
        existing.GroupName = field.GroupName;
        existing.ColumnSpan = field.ColumnSpan;
        existing.IsActive = field.IsActive;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteFieldAsync(Guid fieldId)
    {
        var entity = await _context.ContentTypeFields.FindAsync(fieldId);
        if (entity == null) return false;

        entity.IsActive = false;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ReorderFieldsAsync(Guid contentTypeId, List<Guid> fieldIds)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var fields = await _context.ContentTypeFields
                    .Where(f => f.ContentTypeId == contentTypeId && fieldIds.Contains(f.Id))
                    .ToListAsync();

                var orderLookup = fieldIds.Select((id, idx) => (id, order: idx + 1))
                    .ToDictionary(x => x.id, x => x.order);

                foreach (var field in fields)
                {
                    if (orderLookup.TryGetValue(field.Id, out var newOrder))
                        field.SortOrder = newOrder;
                }

                await _context.SaveChangesAsync();
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

    #endregion

    #region Document Metadata

    public async Task<IEnumerable<DocumentMetadata>> GetDocumentMetadataAsync(Guid documentId)
    {
        return await _context.DocumentMetadata
            .AsNoTracking()
            .Where(dm => dm.DocumentId == documentId)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentMetadata>> GetDocumentMetadataByContentTypeAsync(Guid documentId, Guid contentTypeId)
    {
        return await _context.DocumentMetadata
            .AsNoTracking()
            .Where(dm => dm.DocumentId == documentId && dm.ContentTypeId == contentTypeId)
            .ToListAsync();
    }

    public async Task<bool> SaveDocumentMetadataAsync(Guid documentId, Guid contentTypeId, List<DocumentMetadata> metadata, Guid userId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Delete existing metadata for this document/content type
                await _context.DocumentMetadata
                    .Where(dm => dm.DocumentId == documentId && dm.ContentTypeId == contentTypeId)
                    .ExecuteDeleteAsync();

                // Insert new metadata
                foreach (var item in metadata)
                {
                    item.Id = Guid.NewGuid();
                    item.DocumentId = documentId;
                    item.ContentTypeId = contentTypeId;
                    item.CreatedBy = userId;
                    item.CreatedAt = DateTime.UtcNow;

                    _context.DocumentMetadata.Add(item);
                }

                await _context.SaveChangesAsync();
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

    public async Task<bool> DeleteDocumentMetadataAsync(Guid documentId)
    {
        return await _context.DocumentMetadata
            .Where(dm => dm.DocumentId == documentId)
            .ExecuteDeleteAsync() > 0;
    }

    public async Task<bool> DeleteDocumentMetadataByContentTypeAsync(Guid documentId, Guid contentTypeId)
    {
        return await _context.DocumentMetadata
            .Where(dm => dm.DocumentId == documentId && dm.ContentTypeId == contentTypeId)
            .ExecuteDeleteAsync() > 0;
    }

    #endregion

    #region Folder Content Type Assignments

    public async Task<IEnumerable<FolderContentTypeAssignment>> GetFolderContentTypesAsync(Guid folderId)
    {
        var assignments = await _context.FolderContentTypeAssignments
            .AsNoTracking()
            .Include(a => a.ContentType)
            .Where(a => a.FolderId == folderId && a.ContentType!.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.ContentType!.Name)
            .ToListAsync();

        // Populate denormalized fields from navigation
        foreach (var a in assignments)
        {
            if (a.ContentType != null)
            {
                a.ContentTypeName = a.ContentType.Name;
                a.ContentTypeDescription = a.ContentType.Description;
                a.ContentTypeIcon = a.ContentType.Icon;
                a.ContentTypeColor = a.ContentType.Color;
            }
        }

        return assignments;
    }

    public async Task<IEnumerable<ContentTypeDefinition>> GetAvailableContentTypesForFolderAsync(Guid folderId)
    {
        var assignedIds = await _context.FolderContentTypeAssignments
            .AsNoTracking()
            .Where(a => a.FolderId == folderId)
            .Select(a => a.ContentTypeId)
            .ToListAsync();

        return await _context.ContentTypeDefinitions
            .AsNoTracking()
            .Where(ct => ct.AllowOnFolders && !assignedIds.Contains(ct.Id))
            .OrderBy(ct => ct.SortOrder)
            .ThenBy(ct => ct.Name)
            .ToListAsync();
    }

    public async Task<Guid> AssignContentTypeToFolderAsync(FolderContentTypeAssignment assignment)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                assignment.Id = Guid.NewGuid();
                assignment.CreatedAt = DateTime.UtcNow;

                // If setting as default, clear existing default first
                if (assignment.IsDefault)
                {
                    await _context.FolderContentTypeAssignments
                        .Where(a => a.FolderId == assignment.FolderId && a.IsDefault)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
                }

                _context.FolderContentTypeAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return assignment.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<bool> RemoveContentTypeFromFolderAsync(Guid assignmentId)
    {
        return await _context.FolderContentTypeAssignments
            .Where(a => a.Id == assignmentId)
            .ExecuteDeleteAsync() > 0;
    }

    public async Task<bool> RemoveContentTypeFromFolderAsync(Guid folderId, Guid contentTypeId)
    {
        return await _context.FolderContentTypeAssignments
            .Where(a => a.FolderId == folderId && a.ContentTypeId == contentTypeId)
            .ExecuteDeleteAsync() > 0;
    }

    public async Task<bool> UpdateFolderAssignmentAsync(Guid folderId, Guid contentTypeId, bool isRequired, bool isDefault, bool inheritToChildren, int displayOrder)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // If setting as default, clear existing default first
                if (isDefault)
                {
                    await _context.FolderContentTypeAssignments
                        .Where(a => a.FolderId == folderId && a.ContentTypeId != contentTypeId && a.IsDefault)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
                }

                var assignment = await _context.FolderContentTypeAssignments
                    .FirstOrDefaultAsync(a => a.FolderId == folderId && a.ContentTypeId == contentTypeId);

                if (assignment == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                assignment.IsRequired = isRequired;
                assignment.IsDefault = isDefault;
                assignment.InheritToChildren = inheritToChildren;
                assignment.DisplayOrder = displayOrder;

                await _context.SaveChangesAsync();
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

    public async Task<bool> SetFolderDefaultContentTypeAsync(Guid folderId, Guid contentTypeId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Clear existing default
                await _context.FolderContentTypeAssignments
                    .Where(a => a.FolderId == folderId && a.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));

                // Set new default
                var target = await _context.FolderContentTypeAssignments
                    .FirstOrDefaultAsync(a => a.FolderId == folderId && a.ContentTypeId == contentTypeId);

                if (target == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                target.IsDefault = true;
                await _context.SaveChangesAsync();
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

    #endregion

    #region Cabinet Content Type Assignments

    public async Task<IEnumerable<CabinetContentTypeAssignment>> GetCabinetContentTypesAsync(Guid cabinetId)
    {
        var assignments = await _context.CabinetContentTypeAssignments
            .AsNoTracking()
            .Include(a => a.ContentType)
            .Where(a => a.CabinetId == cabinetId && a.ContentType!.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.ContentType!.Name)
            .ToListAsync();

        // Populate denormalized fields from navigation
        foreach (var a in assignments)
        {
            if (a.ContentType != null)
            {
                a.ContentTypeName = a.ContentType.Name;
                a.ContentTypeDescription = a.ContentType.Description;
                a.ContentTypeIcon = a.ContentType.Icon;
                a.ContentTypeColor = a.ContentType.Color;
            }
        }

        return assignments;
    }

    public async Task<Guid> AssignContentTypeToCabinetAsync(CabinetContentTypeAssignment assignment)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                assignment.Id = Guid.NewGuid();
                assignment.CreatedAt = DateTime.UtcNow;

                // If setting as default, clear existing default first
                if (assignment.IsDefault)
                {
                    await _context.CabinetContentTypeAssignments
                        .Where(a => a.CabinetId == assignment.CabinetId && a.IsDefault)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
                }

                _context.CabinetContentTypeAssignments.Add(assignment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return assignment.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<bool> RemoveContentTypeFromCabinetAsync(Guid cabinetId, Guid contentTypeId)
    {
        return await _context.CabinetContentTypeAssignments
            .Where(a => a.CabinetId == cabinetId && a.ContentTypeId == contentTypeId)
            .ExecuteDeleteAsync() > 0;
    }

    public async Task<bool> UpdateCabinetAssignmentAsync(Guid cabinetId, Guid contentTypeId, bool isRequired, bool isDefault, bool inheritToChildren, int displayOrder)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // If setting as default, clear existing default first
                if (isDefault)
                {
                    await _context.CabinetContentTypeAssignments
                        .Where(a => a.CabinetId == cabinetId && a.ContentTypeId != contentTypeId && a.IsDefault)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));
                }

                var assignment = await _context.CabinetContentTypeAssignments
                    .FirstOrDefaultAsync(a => a.CabinetId == cabinetId && a.ContentTypeId == contentTypeId);

                if (assignment == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                assignment.IsRequired = isRequired;
                assignment.IsDefault = isDefault;
                assignment.InheritToChildren = inheritToChildren;
                assignment.DisplayOrder = displayOrder;

                await _context.SaveChangesAsync();
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

    public async Task<bool> SetCabinetDefaultContentTypeAsync(Guid cabinetId, Guid contentTypeId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Clear existing default
                await _context.CabinetContentTypeAssignments
                    .Where(a => a.CabinetId == cabinetId && a.IsDefault)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false));

                // Set new default
                var target = await _context.CabinetContentTypeAssignments
                    .FirstOrDefaultAsync(a => a.CabinetId == cabinetId && a.ContentTypeId == contentTypeId);

                if (target == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                target.IsDefault = true;
                await _context.SaveChangesAsync();
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

    #endregion

    #region Effective Content Types

    public async Task<IEnumerable<EffectiveContentTypeDto>> GetEffectiveContentTypesAsync(Guid folderId)
    {
        // Get folder info including cabinet and parent chain
        var folder = await _context.Folders
            .AsNoTracking()
            .Where(f => f.Id == folderId)
            .Select(f => new { f.Id, f.CabinetId, f.ParentFolderId, f.Name, f.BreakContentTypeInheritance })
            .FirstOrDefaultAsync();

        if (folder == null)
            return Enumerable.Empty<EffectiveContentTypeDto>();

        var results = new List<EffectiveContentTypeDto>();
        var addedContentTypeIds = new HashSet<Guid>();

        // 1. Get direct folder assignments (highest priority)
        var directAssignments = await _context.FolderContentTypeAssignments
            .AsNoTracking()
            .Include(a => a.ContentType)
            .Where(a => a.FolderId == folderId && a.ContentType!.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.ContentType!.Name)
            .Select(a => new EffectiveContentTypeDto
            {
                ContentTypeId = a.ContentTypeId,
                Name = a.ContentType!.Name,
                Description = a.ContentType.Description,
                Icon = a.ContentType.Icon,
                Color = a.ContentType.Color,
                Category = a.ContentType.Category,
                IsRequired = a.IsRequired,
                IsDefault = a.IsDefault,
                DisplayOrder = a.DisplayOrder,
                Source = "Direct",
                SourceName = null,
                SourceId = null
            })
            .ToListAsync();

        foreach (var assignment in directAssignments)
        {
            results.Add(assignment);
            addedContentTypeIds.Add(assignment.ContentTypeId);
        }

        // Check if folder breaks inheritance
        bool breakInheritance = folder.BreakContentTypeInheritance;

        if (!breakInheritance)
        {
            // 2. Get inherited from parent folders
            var currentParentId = folder.ParentFolderId;
            while (currentParentId.HasValue)
            {
                var parentFolder = await _context.Folders
                    .AsNoTracking()
                    .Where(f => f.Id == currentParentId.Value)
                    .Select(f => new { f.Id, f.Name, f.ParentFolderId, f.BreakContentTypeInheritance })
                    .FirstOrDefaultAsync();

                if (parentFolder == null || parentFolder.BreakContentTypeInheritance)
                    break;

                var inheritedAssignments = await _context.FolderContentTypeAssignments
                    .AsNoTracking()
                    .Include(a => a.ContentType)
                    .Where(a => a.FolderId == currentParentId.Value && a.InheritToChildren && a.ContentType!.IsActive)
                    .OrderBy(a => a.DisplayOrder)
                    .ThenBy(a => a.ContentType!.Name)
                    .Select(a => new EffectiveContentTypeDto
                    {
                        ContentTypeId = a.ContentTypeId,
                        Name = a.ContentType!.Name,
                        Description = a.ContentType.Description,
                        Icon = a.ContentType.Icon,
                        Color = a.ContentType.Color,
                        Category = a.ContentType.Category,
                        IsRequired = a.IsRequired,
                        IsDefault = a.IsDefault,
                        DisplayOrder = a.DisplayOrder,
                        Source = "Inherited",
                        SourceName = parentFolder.Name,
                        SourceId = parentFolder.Id
                    })
                    .ToListAsync();

                foreach (var assignment in inheritedAssignments)
                {
                    if (!addedContentTypeIds.Contains(assignment.ContentTypeId))
                    {
                        results.Add(assignment);
                        addedContentTypeIds.Add(assignment.ContentTypeId);
                    }
                }

                currentParentId = parentFolder.ParentFolderId;
            }

            // 3. Get cabinet assignments (lowest priority)
            var cabinet = await _context.Cabinets
                .AsNoTracking()
                .Where(c => c.Id == folder.CabinetId)
                .Select(c => new { c.Id, c.Name })
                .FirstOrDefaultAsync();

            if (cabinet != null)
            {
                var cabinetAssignments = await _context.CabinetContentTypeAssignments
                    .AsNoTracking()
                    .Include(a => a.ContentType)
                    .Where(a => a.CabinetId == folder.CabinetId && a.InheritToChildren && a.ContentType!.IsActive)
                    .OrderBy(a => a.DisplayOrder)
                    .ThenBy(a => a.ContentType!.Name)
                    .Select(a => new EffectiveContentTypeDto
                    {
                        ContentTypeId = a.ContentTypeId,
                        Name = a.ContentType!.Name,
                        Description = a.ContentType.Description,
                        Icon = a.ContentType.Icon,
                        Color = a.ContentType.Color,
                        Category = a.ContentType.Category,
                        IsRequired = a.IsRequired,
                        IsDefault = a.IsDefault,
                        DisplayOrder = a.DisplayOrder,
                        Source = "Cabinet",
                        SourceName = cabinet.Name,
                        SourceId = cabinet.Id
                    })
                    .ToListAsync();

                foreach (var assignment in cabinetAssignments)
                {
                    if (!addedContentTypeIds.Contains(assignment.ContentTypeId))
                    {
                        results.Add(assignment);
                        addedContentTypeIds.Add(assignment.ContentTypeId);
                    }
                }
            }
        }

        // 4. If no content types found, add system default
        if (results.Count == 0)
        {
            var systemDefault = await _context.ContentTypeDefinitions
                .AsNoTracking()
                .Where(ct => ct.IsSystemDefault)
                .Select(ct => new EffectiveContentTypeDto
                {
                    ContentTypeId = ct.Id,
                    Name = ct.Name,
                    Description = ct.Description,
                    Icon = ct.Icon,
                    Color = ct.Color,
                    Category = ct.Category,
                    IsRequired = false,
                    IsDefault = true,
                    DisplayOrder = 0,
                    Source = "SystemDefault",
                    SourceName = "System",
                    SourceId = null
                })
                .FirstOrDefaultAsync();

            if (systemDefault != null)
            {
                results.Add(systemDefault);
                addedContentTypeIds.Add(systemDefault.ContentTypeId);
            }
        }

        // Load fields for each content type
        var contentTypeIds = results.Select(r => r.ContentTypeId).ToList();
        var allFields = await _context.ContentTypeFields
            .AsNoTracking()
            .Where(f => contentTypeIds.Contains(f.ContentTypeId) && f.IsActive)
            .OrderBy(f => f.SortOrder)
            .Select(f => new ContentTypeFieldDto
            {
                Id = f.Id,
                ContentTypeId = f.ContentTypeId,
                FieldName = f.FieldName,
                DisplayName = f.DisplayName,
                Description = f.Description,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired,
                IsReadOnly = f.IsReadOnly,
                ShowInList = f.ShowInList,
                IsSearchable = f.IsSearchable,
                DefaultValue = f.DefaultValue,
                ValidationRules = f.ValidationRules,
                LookupName = f.LookupName,
                Options = f.Options,
                SortOrder = f.SortOrder,
                GroupName = f.GroupName,
                ColumnSpan = f.ColumnSpan
            })
            .ToListAsync();

        var fieldsByContentType = allFields.GroupBy(f => f.ContentTypeId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var ct in results)
        {
            ct.Fields = fieldsByContentType.TryGetValue(ct.ContentTypeId, out var fields) ? fields : new List<ContentTypeFieldDto>();
        }

        return results.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name);
    }

    public async Task<FolderContentTypeInfoDto?> GetFolderContentTypeInfoAsync(Guid folderId)
    {
        // Get folder info
        var folder = await _context.Folders
            .AsNoTracking()
            .Where(f => f.Id == folderId)
            .Select(f => new { f.Id, f.Name, f.BreakContentTypeInheritance })
            .FirstOrDefaultAsync();

        if (folder == null)
            return null;

        // Get effective content types
        var contentTypes = (await GetEffectiveContentTypesAsync(folderId)).ToList();

        // Find default
        var defaultCt = contentTypes.FirstOrDefault(x => x.IsDefault);

        return new FolderContentTypeInfoDto
        {
            FolderId = folderId,
            FolderName = folder.Name,
            HasRequiredContentTypes = contentTypes.Any(x => x.IsRequired),
            HasContentTypes = contentTypes.Count > 0,
            DefaultContentTypeId = defaultCt?.ContentTypeId,
            DefaultContentTypeName = defaultCt?.Name,
            TotalContentTypes = contentTypes.Count,
            BreaksInheritance = folder.BreakContentTypeInheritance,
            ContentTypes = contentTypes
        };
    }

    #endregion
}
