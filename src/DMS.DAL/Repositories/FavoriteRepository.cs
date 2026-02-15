using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly DmsDbContext _context;

    public FavoriteRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Favorite>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Favorite?> GetAsync(Guid userId, int nodeType, Guid nodeId)
    {
        return await _context.Favorites
            .AsNoTracking()
            .FirstOrDefaultAsync(f =>
                f.UserId == userId &&
                f.NodeType == nodeType &&
                f.NodeId == nodeId);
    }

    public async Task<bool> IsFavoriteAsync(Guid userId, int nodeType, Guid nodeId)
    {
        return await _context.Favorites
            .AnyAsync(f =>
                f.UserId == userId &&
                f.NodeType == nodeType &&
                f.NodeId == nodeId);
    }

    public async Task<Guid> AddAsync(Favorite entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;
        _context.Favorites.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> RemoveAsync(Guid userId, int nodeType, Guid nodeId)
    {
        var affected = await _context.Favorites
            .Where(f =>
                f.UserId == userId &&
                f.NodeType == nodeType &&
                f.NodeId == nodeId)
            .ExecuteDeleteAsync();
        return affected > 0;
    }
}
