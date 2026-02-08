using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class FolderTemplateService : IFolderTemplateService
{
    private readonly IFolderTemplateRepository _templateRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IContentTypeDefinitionRepository _contentTypeRepository;
    private readonly IActivityLogService _activityLogService;

    public FolderTemplateService(
        IFolderTemplateRepository templateRepository,
        IFolderRepository folderRepository,
        IContentTypeDefinitionRepository contentTypeRepository,
        IActivityLogService activityLogService)
    {
        _templateRepository = templateRepository;
        _folderRepository = folderRepository;
        _contentTypeRepository = contentTypeRepository;
        _activityLogService = activityLogService;
    }

    #region Template Management

    public async Task<ServiceResult<List<FolderTemplateDto>>> GetAllAsync(bool includeInactive = false)
    {
        var templates = await _templateRepository.GetAllAsync(includeInactive);
        return ServiceResult<List<FolderTemplateDto>>.Ok(templates.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<FolderTemplateDto>> GetByIdAsync(Guid id)
    {
        var template = await _templateRepository.GetByIdWithNodesAsync(id);
        if (template == null)
            return ServiceResult<FolderTemplateDto>.Fail("Template not found");

        return ServiceResult<FolderTemplateDto>.Ok(MapToDtoWithNodes(template));
    }

    public async Task<ServiceResult<List<FolderTemplateDto>>> GetByCategoryAsync(string category)
    {
        var templates = await _templateRepository.GetByCategoryAsync(category);
        return ServiceResult<List<FolderTemplateDto>>.Ok(templates.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<FolderTemplateDto>> GetDefaultAsync()
    {
        var template = await _templateRepository.GetDefaultAsync();
        if (template == null)
            return ServiceResult<FolderTemplateDto>.Fail("No default template configured");

        return ServiceResult<FolderTemplateDto>.Ok(MapToDtoWithNodes(template));
    }

    public async Task<ServiceResult<FolderTemplateDto>> CreateAsync(CreateFolderTemplateDto dto, Guid userId)
    {
        var template = new FolderTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            Icon = dto.Icon,
            IsDefault = dto.IsDefault,
            CreatedBy = userId
        };

        var templateId = await _templateRepository.CreateAsync(template);

        // Create nodes recursively
        if (dto.Nodes.Any())
        {
            await CreateNodesRecursively(templateId, null, dto.Nodes);
        }

        await _activityLogService.LogActivityAsync(
            ActivityActions.Created, "FolderTemplate", templateId, dto.Name,
            null, userId, null, null);

        return await GetByIdAsync(templateId);
    }

    public async Task<ServiceResult<FolderTemplateDto>> UpdateAsync(Guid id, UpdateFolderTemplateDto dto, Guid userId)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
            return ServiceResult<FolderTemplateDto>.Fail("Template not found");

        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Category = dto.Category;
        template.Icon = dto.Icon;
        template.IsDefault = dto.IsDefault;
        template.IsActive = dto.IsActive;
        template.ModifiedBy = userId;

        await _templateRepository.UpdateAsync(template);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Updated, "FolderTemplate", id, dto.Name,
            null, userId, null, null);

        return await GetByIdAsync(id);
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null)
            return ServiceResult.Fail("Template not found");

        await _templateRepository.DeleteAsync(id);

        return ServiceResult.Ok("Template deleted successfully");
    }

    #endregion

    #region Node Management

    public async Task<ServiceResult<FolderTemplateNodeDto>> AddNodeAsync(Guid templateId, CreateTemplateNodeDto dto)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null)
            return ServiceResult<FolderTemplateNodeDto>.Fail("Template not found");

        var node = new FolderTemplateNode
        {
            TemplateId = templateId,
            ParentNodeId = dto.ParentNodeId,
            Name = dto.Name,
            Description = dto.Description,
            ContentTypeId = dto.ContentTypeId,
            SortOrder = dto.SortOrder,
            BreakContentTypeInheritance = dto.BreakContentTypeInheritance
        };

        var nodeId = await _templateRepository.CreateNodeAsync(node);
        node.Id = nodeId;

        // Recursively create children if any
        if (dto.Children.Any())
        {
            await CreateNodesRecursively(templateId, nodeId, dto.Children);
        }

        return ServiceResult<FolderTemplateNodeDto>.Ok(MapNodeToDto(node));
    }

    public async Task<ServiceResult<FolderTemplateNodeDto>> UpdateNodeAsync(Guid nodeId, UpdateTemplateNodeDto dto)
    {
        var nodes = await _templateRepository.GetNodesByTemplateIdAsync(Guid.Empty);
        // This is a workaround - we need to find the node first
        // In production, add a GetNodeByIdAsync method

        var node = new FolderTemplateNode
        {
            Id = nodeId,
            ParentNodeId = dto.ParentNodeId,
            Name = dto.Name,
            Description = dto.Description,
            ContentTypeId = dto.ContentTypeId,
            SortOrder = dto.SortOrder,
            BreakContentTypeInheritance = dto.BreakContentTypeInheritance
        };

        await _templateRepository.UpdateNodeAsync(node);

        return ServiceResult<FolderTemplateNodeDto>.Ok(MapNodeToDto(node));
    }

    public async Task<ServiceResult> DeleteNodeAsync(Guid nodeId)
    {
        await _templateRepository.DeleteNodeAsync(nodeId);
        return ServiceResult.Ok("Node deleted successfully");
    }

    private async Task CreateNodesRecursively(Guid templateId, Guid? parentNodeId, List<CreateTemplateNodeDto> nodeDtos)
    {
        var sortOrder = 0;
        foreach (var dto in nodeDtos)
        {
            var node = new FolderTemplateNode
            {
                TemplateId = templateId,
                ParentNodeId = parentNodeId,
                Name = dto.Name,
                Description = dto.Description,
                ContentTypeId = dto.ContentTypeId,
                SortOrder = dto.SortOrder > 0 ? dto.SortOrder : sortOrder++,
                BreakContentTypeInheritance = dto.BreakContentTypeInheritance
            };

            var nodeId = await _templateRepository.CreateNodeAsync(node);

            if (dto.Children.Any())
            {
                await CreateNodesRecursively(templateId, nodeId, dto.Children);
            }
        }
    }

    #endregion

    #region Application

    public async Task<ServiceResult<ApplyTemplateResultDto>> ApplyTemplateAsync(
        Guid targetFolderId, ApplyTemplateDto dto, Guid userId)
    {
        var template = await _templateRepository.GetByIdWithNodesAsync(dto.TemplateId);
        if (template == null)
            return ServiceResult<ApplyTemplateResultDto>.Fail("Template not found");

        var targetFolder = await _folderRepository.GetByIdAsync(targetFolderId);
        if (targetFolder == null)
            return ServiceResult<ApplyTemplateResultDto>.Fail("Target folder not found");

        var result = new ApplyTemplateResultDto
        {
            Success = true,
            CreatedFolderPaths = new List<string>(),
            Errors = new List<string>()
        };

        // Create folders recursively
        var rootNodes = template.Nodes.Where(n => n.ParentNodeId == null).OrderBy(n => n.SortOrder);
        await CreateFoldersFromNodes(
            rootNodes,
            targetFolderId,
            targetFolder.CabinetId,
            dto.NamePrefix,
            userId,
            result,
            "");

        // Record usage
        await _templateRepository.RecordUsageAsync(new FolderTemplateUsage
        {
            TemplateId = dto.TemplateId,
            RootFolderId = targetFolderId,
            CabinetId = targetFolder.CabinetId,
            AppliedBy = userId,
            FoldersCreated = result.FoldersCreated
        });

        // Log activity
        await _activityLogService.LogActivityAsync(
            "TemplateApplied", "Folder", targetFolderId,
            $"Applied template '{template.Name}'",
            $"Created {result.FoldersCreated} folders",
            userId, null, null);

        return ServiceResult<ApplyTemplateResultDto>.Ok(result,
            $"Successfully created {result.FoldersCreated} folders from template '{template.Name}'");
    }

    public async Task<ServiceResult<ApplyTemplateResultDto>> PreviewTemplateAsync(Guid targetFolderId, Guid templateId)
    {
        var template = await _templateRepository.GetByIdWithNodesAsync(templateId);
        if (template == null)
            return ServiceResult<ApplyTemplateResultDto>.Fail("Template not found");

        var targetFolder = await _folderRepository.GetByIdAsync(targetFolderId);
        if (targetFolder == null)
            return ServiceResult<ApplyTemplateResultDto>.Fail("Target folder not found");

        var result = new ApplyTemplateResultDto
        {
            Success = true,
            CreatedFolderPaths = new List<string>(),
            Errors = new List<string>()
        };

        // Preview - just count and list paths without creating
        PreviewFoldersFromNodes(template.Nodes.Where(n => n.ParentNodeId == null), result, "");

        return ServiceResult<ApplyTemplateResultDto>.Ok(result);
    }

    public async Task<ServiceResult<ApplyTemplateResultDto>> ApplyTemplateToCabinetAsync(
        Guid cabinetId, ApplyTemplateDto dto, Guid userId)
    {
        var template = await _templateRepository.GetByIdWithNodesAsync(dto.TemplateId);
        if (template == null)
            return ServiceResult<ApplyTemplateResultDto>.Fail("Template not found");

        var result = new ApplyTemplateResultDto
        {
            Success = true,
            CreatedFolderPaths = new List<string>(),
            Errors = new List<string>()
        };

        // Create folders recursively (null parent means directly under cabinet)
        var rootNodes = template.Nodes.Where(n => n.ParentNodeId == null).OrderBy(n => n.SortOrder);
        await CreateFoldersFromNodes(
            rootNodes,
            null, // No parent folder - folders will be created directly in cabinet
            cabinetId,
            dto.NamePrefix,
            userId,
            result,
            "");

        // Record usage
        await _templateRepository.RecordUsageAsync(new FolderTemplateUsage
        {
            TemplateId = dto.TemplateId,
            RootFolderId = null,
            CabinetId = cabinetId,
            AppliedBy = userId,
            FoldersCreated = result.FoldersCreated
        });

        // Log activity
        await _activityLogService.LogActivityAsync(
            "TemplateApplied", "Cabinet", cabinetId,
            $"Applied template '{template.Name}'",
            $"Created {result.FoldersCreated} folders",
            userId, null, null);

        return ServiceResult<ApplyTemplateResultDto>.Ok(result,
            $"Successfully created {result.FoldersCreated} folders from template '{template.Name}'");
    }

    public async Task<ServiceResult<ApplyTemplateResultDto>> PreviewTemplateToCabinetAsync(Guid cabinetId, Guid templateId)
    {
        var template = await _templateRepository.GetByIdWithNodesAsync(templateId);
        if (template == null)
            return ServiceResult<ApplyTemplateResultDto>.Fail("Template not found");

        var result = new ApplyTemplateResultDto
        {
            Success = true,
            CreatedFolderPaths = new List<string>(),
            Errors = new List<string>()
        };

        // Preview - just count and list paths without creating
        PreviewFoldersFromNodes(template.Nodes.Where(n => n.ParentNodeId == null), result, "");

        return ServiceResult<ApplyTemplateResultDto>.Ok(result);
    }

    private async Task CreateFoldersFromNodes(
        IEnumerable<FolderTemplateNode> nodes,
        Guid? parentFolderId,
        Guid cabinetId,
        string? namePrefix,
        Guid userId,
        ApplyTemplateResultDto result,
        string currentPath)
    {
        foreach (var node in nodes.OrderBy(n => n.SortOrder))
        {
            try
            {
                var folderName = string.IsNullOrEmpty(namePrefix)
                    ? node.Name
                    : $"{namePrefix} - {node.Name}";

                var fullPath = string.IsNullOrEmpty(currentPath)
                    ? folderName
                    : $"{currentPath}/{folderName}";

                // Create folder
                var folder = new Folder
                {
                    CabinetId = cabinetId,
                    ParentFolderId = parentFolderId,
                    Name = folderName,
                    Description = node.Description,
                    BreakContentTypeInheritance = node.BreakContentTypeInheritance,
                    CreatedBy = userId
                };

                var folderId = await _folderRepository.CreateAsync(folder);
                result.FoldersCreated++;
                result.CreatedFolderPaths.Add(fullPath);

                // Assign content type if specified
                if (node.ContentTypeId.HasValue)
                {
                    try
                    {
                        var assignment = new FolderContentTypeAssignment
                        {
                            FolderId = folderId,
                            ContentTypeId = node.ContentTypeId.Value,
                            IsDefault = true,
                            IsRequired = false,
                            InheritToChildren = true,
                            CreatedBy = userId
                        };
                        await _contentTypeRepository.AssignContentTypeToFolderAsync(assignment);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Failed to assign content type to '{folderName}': {ex.Message}");
                    }
                }

                // Recurse for children
                if (node.Children.Any())
                {
                    await CreateFoldersFromNodes(node.Children, folderId, cabinetId, null, userId, result, fullPath);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to create folder '{node.Name}': {ex.Message}");
            }
        }
    }

    private void PreviewFoldersFromNodes(
        IEnumerable<FolderTemplateNode> nodes,
        ApplyTemplateResultDto result,
        string currentPath)
    {
        foreach (var node in nodes.OrderBy(n => n.SortOrder))
        {
            var fullPath = string.IsNullOrEmpty(currentPath)
                ? node.Name
                : $"{currentPath}/{node.Name}";

            result.FoldersCreated++;
            result.CreatedFolderPaths.Add(fullPath);

            if (node.Children.Any())
            {
                PreviewFoldersFromNodes(node.Children, result, fullPath);
            }
        }
    }

    #endregion

    #region Utilities

    public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
    {
        var categories = await _templateRepository.GetCategoriesAsync();
        return ServiceResult<List<string>>.Ok(categories.ToList());
    }

    public async Task<ServiceResult<FolderTemplateDto>> DuplicateAsync(Guid templateId, string newName, Guid userId)
    {
        var template = await _templateRepository.GetByIdWithNodesAsync(templateId);
        if (template == null)
            return ServiceResult<FolderTemplateDto>.Fail("Template not found");

        // Create new template
        var newTemplate = new FolderTemplate
        {
            Name = newName,
            Description = template.Description,
            Category = template.Category,
            Icon = template.Icon,
            IsDefault = false, // Never duplicate as default
            CreatedBy = userId
        };

        var newTemplateId = await _templateRepository.CreateAsync(newTemplate);

        // Duplicate nodes
        await DuplicateNodesRecursively(newTemplateId, null, template.Nodes.Where(n => n.ParentNodeId == null).ToList());

        await _activityLogService.LogActivityAsync(
            ActivityActions.Created, "FolderTemplate", newTemplateId, newName,
            $"Duplicated from '{template.Name}'", userId, null, null);

        return await GetByIdAsync(newTemplateId);
    }

    private async Task DuplicateNodesRecursively(Guid templateId, Guid? parentNodeId, List<FolderTemplateNode> nodes)
    {
        foreach (var sourceNode in nodes.OrderBy(n => n.SortOrder))
        {
            var node = new FolderTemplateNode
            {
                TemplateId = templateId,
                ParentNodeId = parentNodeId,
                Name = sourceNode.Name,
                Description = sourceNode.Description,
                ContentTypeId = sourceNode.ContentTypeId,
                SortOrder = sourceNode.SortOrder,
                BreakContentTypeInheritance = sourceNode.BreakContentTypeInheritance
            };

            var nodeId = await _templateRepository.CreateNodeAsync(node);

            if (sourceNode.Children.Any())
            {
                await DuplicateNodesRecursively(templateId, nodeId, sourceNode.Children);
            }
        }
    }

    public async Task<ServiceResult<List<FolderTemplateUsageDto>>> GetUsageHistoryAsync(Guid templateId)
    {
        var usages = await _templateRepository.GetUsageByTemplateIdAsync(templateId);
        return ServiceResult<List<FolderTemplateUsageDto>>.Ok(usages.Select(u => new FolderTemplateUsageDto
        {
            Id = u.Id,
            TemplateId = u.TemplateId,
            TemplateName = u.TemplateName,
            RootFolderId = u.RootFolderId,
            FolderName = u.FolderName,
            CabinetId = u.CabinetId,
            AppliedBy = u.AppliedBy,
            AppliedByName = u.AppliedByName,
            AppliedAt = u.AppliedAt,
            FoldersCreated = u.FoldersCreated
        }).ToList());
    }

    #endregion

    #region Mapping

    private static FolderTemplateDto MapToDto(FolderTemplate template)
    {
        return new FolderTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Category = template.Category,
            Icon = template.Icon,
            IsActive = template.IsActive,
            IsDefault = template.IsDefault,
            CreatedAt = template.CreatedAt,
            CreatedByName = template.CreatedByName,
            UsageCount = template.UsageCount
        };
    }

    private static FolderTemplateDto MapToDtoWithNodes(FolderTemplate template)
    {
        var dto = MapToDto(template);
        dto.Nodes = template.Nodes.Select(MapNodeToDtoRecursive).ToList();
        return dto;
    }

    private static FolderTemplateNodeDto MapNodeToDto(FolderTemplateNode node)
    {
        return new FolderTemplateNodeDto
        {
            Id = node.Id,
            ParentNodeId = node.ParentNodeId,
            Name = node.Name,
            Description = node.Description,
            ContentTypeId = node.ContentTypeId,
            ContentTypeName = node.ContentTypeName,
            SortOrder = node.SortOrder,
            BreakContentTypeInheritance = node.BreakContentTypeInheritance
        };
    }

    private static FolderTemplateNodeDto MapNodeToDtoRecursive(FolderTemplateNode node)
    {
        var dto = MapNodeToDto(node);
        dto.Children = node.Children.Select(MapNodeToDtoRecursive).ToList();
        return dto;
    }

    #endregion
}
