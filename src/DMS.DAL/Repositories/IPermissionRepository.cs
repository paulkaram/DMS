using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IPermissionRepository
{
    // Basic CRUD
    Task<Permission?> GetByIdAsync(Guid id);
    Task<IEnumerable<Permission>> GetByNodeAsync(NodeType nodeType, Guid nodeId);
    Task<IEnumerable<Permission>> GetByPrincipalAsync(PrincipalType principalType, Guid principalId);
    Task<Guid> CreateAsync(Permission entity);
    Task<bool> UpdateAsync(Permission entity);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> DeleteByNodeAsync(NodeType nodeType, Guid nodeId);

    // Permission checking
    Task<PermissionLevel> GetEffectivePermissionAsync(Guid userId, NodeType nodeType, Guid nodeId);
    Task<(PermissionLevel Level, string SourceType, Guid? SourceNodeId)> GetEffectivePermissionWithSourceAsync(Guid userId, NodeType nodeType, Guid nodeId);
    Task<bool> HasPermissionAsync(Guid userId, NodeType nodeType, Guid nodeId, PermissionLevel requiredLevel);

    // Get user's all principals (user, roles, structures)
    Task<IEnumerable<(PrincipalType Type, Guid Id)>> GetUserPrincipalsAsync(Guid userId);

    // Bulk operations
    Task<IEnumerable<Permission>> GetByNodeWithPrincipalNamesAsync(NodeType nodeType, Guid nodeId);
    Task<int> DeleteExpiredPermissionsAsync();
}
