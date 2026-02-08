using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IPermissionService
{
    // Basic permission operations
    Task<ServiceResult<NodePermissionsDto>> GetNodePermissionsAsync(string nodeType, Guid nodeId);
    Task<ServiceResult<PermissionDto>> GrantPermissionAsync(CreatePermissionDto dto, Guid userId);
    Task<ServiceResult<PermissionDto>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionDto dto, Guid userId);
    Task<ServiceResult> RevokePermissionAsync(Guid permissionId, Guid userId);

    // Permission checking
    Task<ServiceResult<bool>> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel);
    Task<ServiceResult<EffectivePermissionDto>> GetEffectivePermissionAsync(Guid userId, string nodeType, Guid nodeId);
    Task<ServiceResult<int>> GetMyPermissionLevelAsync(Guid userId, string nodeType, Guid nodeId);

    // Inheritance management
    Task<ServiceResult> BreakInheritanceAsync(string nodeType, Guid nodeId, Guid userId, bool copyInheritedPermissions = true);
    Task<ServiceResult> RestoreInheritanceAsync(string nodeType, Guid nodeId, Guid userId);

    // Delegation operations
    Task<ServiceResult<PermissionDelegationDto>> CreateDelegationAsync(CreatePermissionDelegationDto dto, Guid delegatorId);
    Task<ServiceResult> RevokeDelegationAsync(Guid delegationId, Guid userId);
    Task<ServiceResult<IEnumerable<PermissionDelegationDto>>> GetMyDelegationsAsync(Guid userId);
    Task<ServiceResult<IEnumerable<PermissionDelegationDto>>> GetDelegationsToMeAsync(Guid userId);

    // Audit trail
    Task<ServiceResult<IEnumerable<PermissionAuditDto>>> GetNodePermissionAuditAsync(string nodeType, Guid nodeId, int take = 100);
    Task<ServiceResult<IEnumerable<PermissionAuditDto>>> GetPrincipalPermissionAuditAsync(string principalType, Guid principalId, int take = 100);

    // Cache management
    Task InvalidatePermissionCacheAsync(string nodeType, Guid nodeId);
    Task InvalidateUserPermissionCacheAsync(Guid userId);
}
