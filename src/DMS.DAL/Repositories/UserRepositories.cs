using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DmsDbContext _context;

    public UserRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> SearchAsync(string? search)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.Username.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.DisplayName != null && u.DisplayName.Contains(search)));

        return await query
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Username)
            .ToListAsync();
    }

    public async Task<(List<User> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<User> Items, int TotalCount)> SearchPaginatedAsync(string? search, int page, int pageSize)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u =>
                u.Username.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)) ||
                (u.DisplayName != null && u.DisplayName.Contains(search)));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Guid> CreateAsync(User entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(User entity)
    {
        entity.ModifiedAt = DateTime.UtcNow;
        _context.Users.Update(entity);
        var affected = await _context.SaveChangesAsync();
        return affected > 0;
    }

    public async Task<bool> UpdateLastLoginAsync(Guid id)
    {
        var affected = await _context.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.LastLoginAt, DateTime.UtcNow));
        return affected > 0;
    }
}

public class RoleRepository : IRoleRepository
{
    private readonly DmsDbContext _context;

    public RoleRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id)
    {
        return await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        return await _context.Roles
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Role>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(
                _context.Roles.Where(r => r.IsActive),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(Role entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        _context.Roles.Add(entity);
        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task<bool> AddUserToRoleAsync(Guid userId, Guid roleId)
    {
        try
        {
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleId = roleId,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return true;
        }
        catch { return false; }
    }

    public async Task<bool> RemoveUserFromRoleAsync(Guid userId, Guid roleId)
    {
        var affected = await _context.UserRoles
            .Where(ur => ur.UserId == userId && ur.RoleId == roleId)
            .ExecuteDeleteAsync();
        return affected > 0;
    }
}
