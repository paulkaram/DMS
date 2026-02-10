using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class RecycleBinService : IRecycleBinService
{
    private readonly IRecycleBinRepository _recycleBinRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly ICabinetRepository _cabinetRepository;

    public RecycleBinService(
        IRecycleBinRepository recycleBinRepository,
        IDocumentRepository documentRepository,
        IFolderRepository folderRepository,
        ICabinetRepository cabinetRepository)
    {
        _recycleBinRepository = recycleBinRepository;
        _documentRepository = documentRepository;
        _folderRepository = folderRepository;
        _cabinetRepository = cabinetRepository;
    }

    public async Task<IEnumerable<RecycleBinItemDto>> GetUserRecycleBinAsync(Guid userId)
    {
        var items = await _recycleBinRepository.GetByUserIdAsync(userId);
        return items.Select(MapToDto);
    }

    public async Task<IEnumerable<RecycleBinItemDto>> GetAllAsync(int? nodeType = null)
    {
        var items = await _recycleBinRepository.GetAllAsync(nodeType);
        return items.Select(MapToDto);
    }

    public async Task<PagedResultDto<RecycleBinItemDto>> GetUserRecycleBinPaginatedAsync(Guid userId, int page, int pageSize)
    {
        var (items, totalCount) = await _recycleBinRepository.GetByUserIdPaginatedAsync(userId, page, pageSize);
        return new PagedResultDto<RecycleBinItemDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResultDto<RecycleBinItemDto>> GetAllPaginatedAsync(int? nodeType, int page, int pageSize)
    {
        var (items, totalCount) = await _recycleBinRepository.GetAllPaginatedAsync(nodeType, page, pageSize);
        return new PagedResultDto<RecycleBinItemDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> RestoreItemAsync(Guid id, Guid? restoreToFolderId = null)
    {
        var item = await _recycleBinRepository.GetByIdAsync(id);
        if (item == null) return false;

        // Restore the item based on its type
        switch (item.NodeType)
        {
            case 3: // Document
                var doc = await _documentRepository.GetByIdAsync(item.NodeId);
                if (doc != null)
                {
                    doc.IsActive = true;
                    if (restoreToFolderId.HasValue)
                        doc.FolderId = restoreToFolderId.Value;
                    await _documentRepository.UpdateAsync(doc);
                }
                break;

            case 2: // Folder
                var folder = await _folderRepository.GetByIdAsync(item.NodeId);
                if (folder != null)
                {
                    folder.IsActive = true;
                    if (restoreToFolderId.HasValue)
                        folder.ParentFolderId = restoreToFolderId.Value;
                    await _folderRepository.UpdateAsync(folder);
                }
                break;

            case 1: // Cabinet
                var cabinet = await _cabinetRepository.GetByIdAsync(item.NodeId);
                if (cabinet != null)
                {
                    cabinet.IsActive = true;
                    await _cabinetRepository.UpdateAsync(cabinet);
                }
                break;
        }

        // Remove from recycle bin
        return await _recycleBinRepository.RemoveAsync(id);
    }

    public async Task<bool> PermanentlyDeleteAsync(Guid id)
    {
        var item = await _recycleBinRepository.GetByIdAsync(id);
        if (item == null) return false;

        // Permanently delete the item (in a real implementation, this would delete physical files too)
        // For now, just remove from recycle bin - the actual data remains marked as inactive
        return await _recycleBinRepository.RemoveAsync(id);
    }

    public async Task<bool> EmptyRecycleBinAsync(Guid userId)
    {
        var items = await _recycleBinRepository.GetByUserIdAsync(userId);
        foreach (var item in items)
        {
            await _recycleBinRepository.RemoveAsync(item.Id);
        }
        return true;
    }

    public async Task<bool> PurgeExpiredAsync()
    {
        return await _recycleBinRepository.PurgeExpiredAsync();
    }

    private static RecycleBinItemDto MapToDto(RecycleBinItem item)
    {
        return new RecycleBinItemDto
        {
            Id = item.Id,
            NodeType = item.NodeType,
            NodeId = item.NodeId,
            NodeName = item.NodeName,
            OriginalPath = item.OriginalPath,
            OriginalParentId = item.OriginalParentId,
            DeletedBy = item.DeletedBy,
            DeletedAt = item.DeletedAt,
            ExpiresAt = item.ExpiresAt,
            DeletedByUserName = item.DeletedByUserName
        };
    }
}
