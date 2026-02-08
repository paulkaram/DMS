using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> SearchAsync(string? search);
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

public interface IDelegationRepository
{
    Task<Delegation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Delegation>> GetByFromUserIdAsync(Guid userId);
    Task<IEnumerable<Delegation>> GetByToUserIdAsync(Guid userId);
    Task<IEnumerable<Delegation>> GetActiveAsync();
    Task<Guid> CreateAsync(Delegation entity);
    Task<bool> UpdateAsync(Delegation entity);
    Task<bool> DeleteAsync(Guid id);
}
