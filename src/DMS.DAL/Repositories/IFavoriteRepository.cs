using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IFavoriteRepository
{
    Task<IEnumerable<Favorite>> GetByUserIdAsync(Guid userId);
    Task<Favorite?> GetAsync(Guid userId, int nodeType, Guid nodeId);
    Task<bool> IsFavoriteAsync(Guid userId, int nodeType, Guid nodeId);
    Task<Guid> AddAsync(Favorite entity);
    Task<bool> RemoveAsync(Guid userId, int nodeType, Guid nodeId);
}
