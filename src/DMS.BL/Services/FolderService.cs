using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class FolderService : IFolderService
{
    private readonly IFolderRepository _folderRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly IRecycleBinRepository _recycleBinRepository;
    private readonly ICabinetRepository _cabinetRepository;

    public FolderService(
        IFolderRepository folderRepository,
        IActivityLogService activityLogService,
        IRecycleBinRepository recycleBinRepository,
        ICabinetRepository cabinetRepository)
    {
        _folderRepository = folderRepository;
        _activityLogService = activityLogService;
        _recycleBinRepository = recycleBinRepository;
        _cabinetRepository = cabinetRepository;
    }

    public async Task<ServiceResult<FolderDto>> GetByIdAsync(Guid id)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder == null)
            return ServiceResult<FolderDto>.Fail("Folder not found");

        return ServiceResult<FolderDto>.Ok(MapToDto(folder));
    }

    public async Task<ServiceResult<List<FolderDto>>> GetByCabinetIdAsync(Guid cabinetId, int? userPrivacyLevel = null)
    {
        var folders = await _folderRepository.GetByCabinetIdAsync(cabinetId, userPrivacyLevel);
        return ServiceResult<List<FolderDto>>.Ok(folders.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<FolderDto>>> GetByParentIdAsync(Guid? parentId, Guid cabinetId, int? userPrivacyLevel = null)
    {
        var folders = await _folderRepository.GetByParentIdAsync(parentId, cabinetId, userPrivacyLevel);
        return ServiceResult<List<FolderDto>>.Ok(folders.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<FolderDto>>> GetTreeAsync(Guid cabinetId, int? userPrivacyLevel = null)
    {
        var allFolders = await _folderRepository.GetTreeAsync(cabinetId, userPrivacyLevel: userPrivacyLevel);
        var folderList = allFolders.ToList();
        var rootFolders = folderList.Where(f => f.ParentFolderId == null).ToList();
        var result = rootFolders.Select(f => BuildTree(f, folderList)).ToList();
        return ServiceResult<List<FolderDto>>.Ok(result);
    }

    public async Task<ServiceResult<List<FolderDto>>> SearchAsync(string? name, Guid? cabinetId, int? userPrivacyLevel = null)
    {
        var folders = await _folderRepository.SearchAsync(name, cabinetId, userPrivacyLevel);
        return ServiceResult<List<FolderDto>>.Ok(folders.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<PagedResultDto<FolderDto>>> SearchPaginatedAsync(string? name, Guid? cabinetId, int page, int pageSize, int? userPrivacyLevel = null)
    {
        var (items, totalCount) = await _folderRepository.SearchPaginatedAsync(name, cabinetId, page, pageSize, userPrivacyLevel);
        return ServiceResult<PagedResultDto<FolderDto>>.Ok(new PagedResultDto<FolderDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<PagedResultDto<FolderDto>>> GetByParentIdPaginatedAsync(Guid? parentId, Guid cabinetId, int page, int pageSize, int? userPrivacyLevel = null)
    {
        var (items, totalCount) = await _folderRepository.GetByParentIdPaginatedAsync(parentId, cabinetId, page, pageSize, userPrivacyLevel);
        return ServiceResult<PagedResultDto<FolderDto>>.Ok(new PagedResultDto<FolderDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<FolderDto>> CreateAsync(CreateFolderDto dto, Guid userId)
    {
        var folder = new Folder
        {
            CabinetId = dto.CabinetId,
            ParentFolderId = dto.ParentFolderId,
            Name = dto.Name,
            Description = dto.Description,
            AccessMode = dto.AccessMode,
            PrivacyLevelId = dto.PrivacyLevelId,
            CreatedBy = userId,
            IsActive = true
        };

        var id = await _folderRepository.CreateAsync(folder);
        folder.Id = id;

        await _activityLogService.LogActivityAsync(
            ActivityActions.Created, "Folder", id, dto.Name, null, userId, null, null);

        return ServiceResult<FolderDto>.Ok(MapToDto(folder), "Folder created successfully");
    }

    public async Task<ServiceResult<FolderDto>> UpdateAsync(Guid id, UpdateFolderDto dto, Guid userId)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder == null)
            return ServiceResult<FolderDto>.Fail("Folder not found");

        folder.Name = dto.Name;
        folder.Description = dto.Description;
        folder.BreakInheritance = dto.BreakInheritance;
        folder.AccessMode = dto.AccessMode;
        folder.PrivacyLevelId = dto.PrivacyLevelId;
        folder.ModifiedBy = userId;

        await _folderRepository.UpdateAsync(folder);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Updated, "Folder", id, dto.Name, null, userId, null, null);

        return ServiceResult<FolderDto>.Ok(MapToDto(folder), "Folder updated successfully");
    }

    public async Task<ServiceResult> MoveAsync(Guid id, MoveFolderDto dto, Guid userId)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder == null)
            return ServiceResult.Fail("Folder not found");

        folder.ParentFolderId = dto.NewParentFolderId;
        if (dto.NewCabinetId.HasValue)
            folder.CabinetId = dto.NewCabinetId.Value;
        folder.ModifiedBy = userId;

        // Update path
        var newPath = await _folderRepository.GetPathAsync(dto.NewParentFolderId ?? Guid.Empty);
        if (!string.IsNullOrEmpty(newPath))
            folder.Path = newPath + "/" + folder.Name;
        else
            folder.Path = folder.Name;

        await _folderRepository.UpdateAsync(folder);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Moved, "Folder", id, folder.Name, $"Moved to {folder.Path}", userId, null, null);

        return ServiceResult.Ok("Folder moved successfully");
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, Guid userId)
    {
        var folder = await _folderRepository.GetByIdAsync(id);
        if (folder == null)
            return ServiceResult.Fail("Folder not found");

        // Build the original path for the folder
        string originalPath = await BuildFolderPathAsync(folder);

        // Add to recycle bin before soft delete
        var recycleBinItem = new RecycleBinItem
        {
            NodeType = 2, // Folder
            NodeId = folder.Id,
            NodeName = folder.Name,
            OriginalPath = originalPath,
            OriginalParentId = folder.ParentFolderId,
            DeletedBy = userId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                folder.CabinetId,
                folder.Description
            })
        };
        await _recycleBinRepository.AddAsync(recycleBinItem);

        await _folderRepository.DeleteAsync(id);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Deleted, "Folder", id, folder.Name, null, userId, null, null);

        return ServiceResult.Ok("Folder moved to recycle bin");
    }

    private async Task<string> BuildFolderPathAsync(Folder folder)
    {
        var pathParts = new List<string>();

        // Get cabinet name
        var cabinet = await _cabinetRepository.GetByIdAsync(folder.CabinetId);
        if (cabinet != null)
        {
            pathParts.Add(cabinet.Name);
        }

        // Build parent folder path
        if (folder.ParentFolderId.HasValue)
        {
            var parentFolder = await _folderRepository.GetByIdAsync(folder.ParentFolderId.Value);
            var parentPath = new List<string>();
            while (parentFolder != null)
            {
                parentPath.Insert(0, parentFolder.Name);
                if (parentFolder.ParentFolderId.HasValue)
                {
                    parentFolder = await _folderRepository.GetByIdAsync(parentFolder.ParentFolderId.Value);
                }
                else
                {
                    break;
                }
            }
            pathParts.AddRange(parentPath);
        }

        pathParts.Add(folder.Name);
        return "/" + string.Join("/", pathParts);
    }

    private FolderDto BuildTree(Folder folder, List<Folder> allFolders)
    {
        var dto = MapToDto(folder);
        var children = allFolders.Where(f => f.ParentFolderId == folder.Id).ToList();
        dto.Children = children.Select(c => BuildTree(c, allFolders)).ToList();
        return dto;
    }

    private static FolderDto MapToDto(Folder folder)
    {
        return new FolderDto
        {
            Id = folder.Id,
            CabinetId = folder.CabinetId,
            ParentFolderId = folder.ParentFolderId,
            Name = folder.Name,
            Description = folder.Description,
            Path = folder.Path,
            BreakInheritance = folder.BreakInheritance,
            AccessMode = folder.AccessMode,
            PrivacyLevelId = folder.PrivacyLevelId,
            PrivacyLevelName = folder.PrivacyLevel?.Name,
            PrivacyLevelColor = folder.PrivacyLevel?.Color,
            PrivacyLevelValue = folder.PrivacyLevel?.Level,
            CreatedAt = folder.CreatedAt,
            ModifiedAt = folder.ModifiedAt
        };
    }
}
