using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly ICabinetRepository _cabinetRepository;
    private readonly IFolderRepository _folderRepository;
    private readonly IDocumentRepository _documentRepository;

    public FavoriteService(
        IFavoriteRepository favoriteRepository,
        ICabinetRepository cabinetRepository,
        IFolderRepository folderRepository,
        IDocumentRepository documentRepository)
    {
        _favoriteRepository = favoriteRepository;
        _cabinetRepository = cabinetRepository;
        _folderRepository = folderRepository;
        _documentRepository = documentRepository;
    }

    public async Task<IEnumerable<FavoriteItemDto>> GetUserFavoritesAsync(Guid userId)
    {
        var favorites = await _favoriteRepository.GetByUserIdAsync(userId);
        var result = new List<FavoriteItemDto>();

        foreach (var fav in favorites)
        {
            var item = new FavoriteItemDto
            {
                Id = fav.Id,
                NodeType = fav.NodeType,
                NodeId = fav.NodeId,
                FavoritedAt = fav.CreatedAt
            };

            switch (fav.NodeType)
            {
                case 1: // Cabinet
                    var cabinet = await _cabinetRepository.GetByIdAsync(fav.NodeId);
                    if (cabinet != null)
                    {
                        item.Name = cabinet.Name;
                        item.Description = cabinet.Description;
                        item.CreatedAt = cabinet.CreatedAt;
                        item.CabinetId = cabinet.Id;
                    }
                    break;
                case 2: // Folder
                    var folder = await _folderRepository.GetByIdAsync(fav.NodeId);
                    if (folder != null)
                    {
                        item.Name = folder.Name;
                        item.Description = folder.Description;
                        item.Path = folder.Path;
                        item.CreatedAt = folder.CreatedAt;
                        item.ParentFolderId = folder.ParentFolderId;
                        item.CabinetId = folder.CabinetId;
                    }
                    break;
                case 3: // Document
                    var doc = await _documentRepository.GetByIdAsync(fav.NodeId);
                    if (doc != null)
                    {
                        item.Name = doc.Name;
                        item.Description = doc.Description;
                        item.CreatedAt = doc.CreatedAt;
                        item.ParentFolderId = doc.FolderId;
                        // Get cabinet ID from the folder
                        if (doc.FolderId != Guid.Empty)
                        {
                            var parentFolder = await _folderRepository.GetByIdAsync(doc.FolderId);
                            item.CabinetId = parentFolder?.CabinetId;
                        }
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(item.Name))
                result.Add(item);
        }

        return result;
    }

    public async Task<bool> IsFavoriteAsync(Guid userId, int nodeType, Guid nodeId)
    {
        return await _favoriteRepository.IsFavoriteAsync(userId, nodeType, nodeId);
    }

    public async Task<bool> ToggleFavoriteAsync(Guid userId, int nodeType, Guid nodeId)
    {
        var isFavorite = await _favoriteRepository.IsFavoriteAsync(userId, nodeType, nodeId);

        if (isFavorite)
        {
            return await _favoriteRepository.RemoveAsync(userId, nodeType, nodeId);
        }
        else
        {
            await _favoriteRepository.AddAsync(new Favorite
            {
                UserId = userId,
                NodeType = nodeType,
                NodeId = nodeId
            });
            return true;
        }
    }

    public async Task<bool> AddFavoriteAsync(Guid userId, int nodeType, Guid nodeId)
    {
        var isFavorite = await _favoriteRepository.IsFavoriteAsync(userId, nodeType, nodeId);
        if (isFavorite) return true;

        await _favoriteRepository.AddAsync(new Favorite
        {
            UserId = userId,
            NodeType = nodeType,
            NodeId = nodeId
        });
        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid userId, int nodeType, Guid nodeId)
    {
        return await _favoriteRepository.RemoveAsync(userId, nodeType, nodeId);
    }
}
