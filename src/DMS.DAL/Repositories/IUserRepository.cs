using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> SearchAsync(string? search);
    Task<(List<User> Items, int TotalCount)> GetAllPaginatedAsync(int page, int pageSize);
    Task<(List<User> Items, int TotalCount)> SearchPaginatedAsync(string? search, int page, int pageSize);
    Task<Guid> CreateAsync(User entity);
    Task<bool> UpdateAsync(User entity);
    Task<bool> UpdateLastLoginAsync(Guid id);
}

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id);
    Task<IEnumerable<Role>> GetAllAsync();
    Task<IEnumerable<Role>> GetByUserIdAsync(Guid userId);
    Task<Guid> CreateAsync(Role entity);
    Task<bool> AddUserToRoleAsync(Guid userId, Guid roleId);
    Task<bool> RemoveUserFromRoleAsync(Guid userId, Guid roleId);
}

