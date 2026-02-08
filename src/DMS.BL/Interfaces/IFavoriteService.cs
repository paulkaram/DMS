using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IFavoriteService
{
    Task<IEnumerable<FavoriteItemDto>> GetUserFavoritesAsync(Guid userId);
    Task<bool> IsFavoriteAsync(Guid userId, int nodeType, Guid nodeId);
    Task<bool> ToggleFavoriteAsync(Guid userId, int nodeType, Guid nodeId);
    Task<bool> AddFavoriteAsync(Guid userId, int nodeType, Guid nodeId);
    Task<bool> RemoveFavoriteAsync(Guid userId, int nodeType, Guid nodeId);
}
