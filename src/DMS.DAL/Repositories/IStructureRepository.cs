using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IStructureRepository
{
    // Basic CRUD
    Task<Structure?> GetByIdAsync(Guid id);
    Task<Structure?> GetByCodeAsync(string code);
    Task<IEnumerable<Structure>> GetAllAsync(bool includeInactive = false);
    Task<Guid> CreateAsync(Structure entity);
    Task<bool> UpdateAsync(Structure entity);
    Task<bool> DeleteAsync(Guid id);

    // Hierarchy operations
    Task<IEnumerable<Structure>> GetChildrenAsync(Guid parentId);
    Task<IEnumerable<Structure>> GetDescendantsAsync(Guid parentId);
    Task<IEnumerable<Structure>> GetAncestorsAsync(Guid structureId);
    Task<IEnumerable<Structure>> GetRootStructuresAsync();
    Task<IEnumerable<Structure>> GetByTypeAsync(StructureType type);

    // Member management
    Task<IEnumerable<StructureMember>> GetMembersAsync(Guid structureId);
    Task<IEnumerable<Structure>> GetUserStructuresAsync(Guid userId);
    Task<Structure?> GetUserPrimaryStructureAsync(Guid userId);
    Task<Guid> AddMemberAsync(StructureMember member);
    Task<bool> RemoveMemberAsync(Guid structureId, Guid userId);
    Task<bool> SetPrimaryStructureAsync(Guid userId, Guid structureId);

    // Path management
    Task UpdatePathsAsync(Guid structureId);
}

public interface IEffectivePermissionRepository
{
    Task<EffectivePermission?> GetAsync(NodeType nodeType, Guid nodeId, Guid userId);
    Task<IEnumerable<EffectivePermission>> GetByNodeAsync(NodeType nodeType, Guid nodeId);
    Task<IEnumerable<EffectivePermission>> GetByUserAsync(Guid userId);
    Task<Guid> UpsertAsync(EffectivePermission entity);
    Task InvalidateByNodeAsync(NodeType nodeType, Guid nodeId);
    Task InvalidateByUserAsync(Guid userId);
    Task InvalidateByPrincipalAsync(PrincipalType principalType, Guid principalId);
    Task CleanupExpiredAsync();
}

public interface IPermissionAuditRepository
{
    Task<Guid> LogAsync(PermissionAuditLog entry);
    Task<IEnumerable<PermissionAuditLog>> GetByNodeAsync(NodeType nodeType, Guid nodeId, int take = 100);
    Task<IEnumerable<PermissionAuditLog>> GetByPrincipalAsync(PrincipalType principalType, Guid principalId, int take = 100);
    Task<IEnumerable<PermissionAuditLog>> GetByPerformerAsync(Guid userId, int take = 100);
    Task<IEnumerable<PermissionAuditLog>> GetRecentAsync(int take = 100);
}

public interface IPermissionDelegationRepository
{
    Task<PermissionDelegation?> GetByIdAsync(Guid id);
    Task<IEnumerable<PermissionDelegation>> GetByDelegatorAsync(Guid delegatorId);
    Task<IEnumerable<PermissionDelegation>> GetByDelegateAsync(Guid delegateId);
    Task<IEnumerable<PermissionDelegation>> GetActiveByDelegateAsync(Guid delegateId);
    Task<Guid> CreateAsync(PermissionDelegation entity);
    Task<bool> RevokeAsync(Guid id, Guid revokedBy);
    Task ExpireOldDelegationsAsync();
}
