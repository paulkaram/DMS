using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepo;
    private readonly IEffectivePermissionRepository _effectivePermissionRepo;
    private readonly IPermissionAuditRepository _auditRepo;
    private readonly IPermissionDelegationRepository _delegationRepo;
    private readonly IStructureRepository _structureRepo;
    private readonly ICabinetRepository _cabinetRepo;
    private readonly IFolderRepository _folderRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IActivityLogService _activityLogService;

    public PermissionService(
        IPermissionRepository permissionRepo,
        IEffectivePermissionRepository effectivePermissionRepo,
        IPermissionAuditRepository auditRepo,
        IPermissionDelegationRepository delegationRepo,
        IStructureRepository structureRepo,
        ICabinetRepository cabinetRepo,
        IFolderRepository folderRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IActivityLogService activityLogService)
    {
        _permissionRepo = permissionRepo;
        _effectivePermissionRepo = effectivePermissionRepo;
        _auditRepo = auditRepo;
        _delegationRepo = delegationRepo;
        _structureRepo = structureRepo;
        _cabinetRepo = cabinetRepo;
        _folderRepo = folderRepo;
        _documentRepo = documentRepo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _activityLogService = activityLogService;
    }

    #region Basic Permission Operations

    public async Task<ServiceResult<NodePermissionsDto>> GetNodePermissionsAsync(string nodeType, Guid nodeId)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult<NodePermissionsDto>.Fail("Invalid node type");

        var permissionDtos = new List<PermissionDto>();

        // Get direct permissions on this node
        var directPermissions = await _permissionRepo.GetByNodeWithPrincipalNamesAsync(parsedNodeType, nodeId);
        foreach (var p in directPermissions)
        {
            var dto = await MapToPermissionDtoAsync(p);
            permissionDtos.Add(dto);
        }

        var (breakInheritance, nodeName) = await GetNodeInfoAsync(parsedNodeType, nodeId);

        // If inheritance is NOT broken, walk up the hierarchy and collect inherited permissions
        if (!breakInheritance)
        {
            var inheritedPermissions = await GetInheritedPermissionsAsync(parsedNodeType, nodeId);
            permissionDtos.AddRange(inheritedPermissions);
        }

        return ServiceResult<NodePermissionsDto>.Ok(new NodePermissionsDto
        {
            NodeType = nodeType,
            NodeId = nodeId,
            NodeName = nodeName,
            BreakInheritance = breakInheritance,
            Permissions = permissionDtos
        });
    }

    private async Task<List<PermissionDto>> GetInheritedPermissionsAsync(NodeType nodeType, Guid nodeId)
    {
        var inheritedPermissions = new List<PermissionDto>();
        var currentNodeType = nodeType;
        var currentNodeId = nodeId;
        var level = 0;
        const int maxLevel = 50; // Safety limit

        // Move to parent first (we already have direct permissions)
        (currentNodeType, currentNodeId) = await GetParentNodeAsync(currentNodeType, currentNodeId);

        while (currentNodeId != Guid.Empty && level < maxLevel)
        {
            // Get the node info (including BreakInheritance flag)
            var (nodeBreakInheritance, currentNodeName) = await GetNodeInfoAsync(currentNodeType, currentNodeId);

            // Get permissions on this ancestor node
            var ancestorPermissions = await _permissionRepo.GetByNodeWithPrincipalNamesAsync(currentNodeType, currentNodeId);
            foreach (var p in ancestorPermissions)
            {
                var dto = await MapToPermissionDtoAsync(p,
                    isInherited: true,
                    inheritedFromNodeType: currentNodeType,
                    inheritedFromNodeId: currentNodeId,
                    inheritedFromNodeName: currentNodeName);
                inheritedPermissions.Add(dto);
            }

            // If this node has BreakInheritance, stop walking up
            if (nodeBreakInheritance)
                break;

            // Move to parent
            (currentNodeType, currentNodeId) = await GetParentNodeAsync(currentNodeType, currentNodeId);
            level++;
        }

        return inheritedPermissions;
    }

    private async Task<(NodeType NodeType, Guid NodeId)> GetParentNodeAsync(NodeType nodeType, Guid nodeId)
    {
        switch (nodeType)
        {
            case NodeType.Document:
                var doc = await _documentRepo.GetByIdAsync(nodeId);
                if (doc != null && doc.FolderId != Guid.Empty)
                    return (NodeType.Folder, doc.FolderId);
                return (NodeType.Folder, Guid.Empty);

            case NodeType.Folder:
                var folder = await _folderRepo.GetByIdAsync(nodeId);
                if (folder == null)
                    return (NodeType.Folder, Guid.Empty);
                if (folder.ParentFolderId.HasValue)
                    return (NodeType.Folder, folder.ParentFolderId.Value);
                return (NodeType.Cabinet, folder.CabinetId);

            case NodeType.Cabinet:
                // Cabinets have no parent
                return (NodeType.Cabinet, Guid.Empty);

            default:
                return (nodeType, Guid.Empty);
        }
    }

    public async Task<ServiceResult<PermissionDto>> GrantPermissionAsync(CreatePermissionDto dto, Guid userId)
    {
        if (!Enum.TryParse<NodeType>(dto.NodeType, true, out var nodeType))
            return ServiceResult<PermissionDto>.Fail("Invalid node type");

        if (!Enum.TryParse<PrincipalType>(dto.PrincipalType, true, out var principalType))
            return ServiceResult<PermissionDto>.Fail("Invalid principal type");

        // Validate expiry date
        if (dto.ExpiresAt.HasValue && dto.ExpiresAt.Value <= DateTime.UtcNow)
            return ServiceResult<PermissionDto>.Fail("Expiry date must be in the future");

        var permission = new Permission
        {
            NodeType = nodeType,
            NodeId = dto.NodeId,
            PrincipalType = principalType,
            PrincipalId = dto.PrincipalId,
            PermissionLevel = (PermissionLevel)dto.PermissionLevel,
            IsInherited = false,
            IncludeChildStructures = dto.IncludeChildStructures,
            ExpiresAt = dto.ExpiresAt,
            GrantedReason = dto.GrantedReason,
            GrantedBy = userId,
            CreatedBy = userId
        };

        var id = await _permissionRepo.CreateAsync(permission);

        // Log audit
        await LogPermissionAuditAsync("Grant", nodeType, dto.NodeId, principalType, dto.PrincipalId,
            null, dto.PermissionLevel, dto.GrantedReason, userId);

        // Invalidate cache
        await _effectivePermissionRepo.InvalidateByNodeAsync(nodeType, dto.NodeId);
        if (principalType == PrincipalType.User)
            await _effectivePermissionRepo.InvalidateByUserAsync(dto.PrincipalId);
        else
            await _effectivePermissionRepo.InvalidateByPrincipalAsync(principalType, dto.PrincipalId);

        // Activity log
        await _activityLogService.LogActivityAsync(
            ActivityActions.PermissionGranted, dto.NodeType, dto.NodeId, null,
            $"Permission {dto.PermissionLevel} granted to {dto.PrincipalType} {dto.PrincipalId}", userId, null, null);

        permission.Id = id;
        return ServiceResult<PermissionDto>.Ok(await MapToPermissionDtoAsync(permission));
    }

    public async Task<ServiceResult<PermissionDto>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionDto dto, Guid userId)
    {
        var permission = await _permissionRepo.GetByIdAsync(permissionId);
        if (permission == null)
            return ServiceResult<PermissionDto>.Fail("Permission not found");

        var oldLevel = (int)permission.PermissionLevel;

        permission.PermissionLevel = (PermissionLevel)dto.PermissionLevel;
        permission.IncludeChildStructures = dto.IncludeChildStructures;
        permission.ExpiresAt = dto.ExpiresAt;
        permission.GrantedReason = dto.GrantedReason;

        await _permissionRepo.UpdateAsync(permission);

        // Log audit
        await LogPermissionAuditAsync("Update", permission.NodeType, permission.NodeId,
            permission.PrincipalType, permission.PrincipalId,
            oldLevel, dto.PermissionLevel, dto.GrantedReason, userId);

        // Invalidate cache
        await _effectivePermissionRepo.InvalidateByNodeAsync(permission.NodeType, permission.NodeId);

        return ServiceResult<PermissionDto>.Ok(await MapToPermissionDtoAsync(permission));
    }

    public async Task<ServiceResult> RevokePermissionAsync(Guid permissionId, Guid userId)
    {
        var permission = await _permissionRepo.GetByIdAsync(permissionId);
        if (permission == null)
            return ServiceResult.Fail("Permission not found");

        await _permissionRepo.DeleteAsync(permissionId);

        // Log audit
        await LogPermissionAuditAsync("Revoke", permission.NodeType, permission.NodeId,
            permission.PrincipalType, permission.PrincipalId,
            (int)permission.PermissionLevel, null, null, userId);

        // Invalidate cache
        await _effectivePermissionRepo.InvalidateByNodeAsync(permission.NodeType, permission.NodeId);
        if (permission.PrincipalType == PrincipalType.User)
            await _effectivePermissionRepo.InvalidateByUserAsync(permission.PrincipalId);
        else
            await _effectivePermissionRepo.InvalidateByPrincipalAsync(permission.PrincipalType, permission.PrincipalId);

        await _activityLogService.LogActivityAsync(
            ActivityActions.PermissionRevoked, permission.NodeType.ToString(), permission.NodeId, null,
            $"Permission revoked from {permission.PrincipalType} {permission.PrincipalId}", userId, null, null);

        return ServiceResult.Ok("Permission revoked");
    }

    #endregion

    #region Permission Checking

    public async Task<ServiceResult<bool>> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult<bool>.Fail("Invalid node type");

        // Check if user is Administrator - they have full permissions
        var userRoles = await _roleRepo.GetByUserIdAsync(userId);
        if (userRoles.Any(r => r.Name == "Administrator"))
            return ServiceResult<bool>.Ok(true);

        // Check if user is the creator/owner of the node - creators have full access
        var isCreator = await IsCreatorOfNodeAsync(userId, parsedNodeType, nodeId);
        if (isCreator)
            return ServiceResult<bool>.Ok(true);

        var effectiveLevel = await _permissionRepo.GetEffectivePermissionAsync(userId, parsedNodeType, nodeId);
        var hasPermission = ((int)effectiveLevel & requiredLevel) == requiredLevel;

        // If user doesn't have direct permission but only needs READ access,
        // check if they have permission on any child node (for navigation purposes)
        // This implements SharePoint-like "implied parent read" for navigation
        if (!hasPermission && requiredLevel == (int)PermissionLevel.Read)
        {
            var hasChildPermission = await HasAnyChildPermissionAsync(userId, parsedNodeType, nodeId);
            if (hasChildPermission)
                return ServiceResult<bool>.Ok(true);
        }

        return ServiceResult<bool>.Ok(hasPermission);
    }

    /// <summary>
    /// Checks if the user has any permission on any child node.
    /// Used for "implied parent read" - allowing navigation to children the user has access to.
    /// </summary>
    private async Task<bool> HasAnyChildPermissionAsync(Guid userId, NodeType nodeType, Guid nodeId)
    {
        switch (nodeType)
        {
            case NodeType.Cabinet:
                // Check if user has direct permission on any folder in this cabinet (all levels)
                var allFolders = await _folderRepo.GetTreeAsync(nodeId);
                foreach (var folder in allFolders)
                {
                    var folderLevel = await _permissionRepo.GetEffectivePermissionAsync(userId, NodeType.Folder, folder.Id);
                    if (folderLevel > 0)
                        return true;

                    // Check if user is creator of the folder
                    if (folder.CreatedBy == userId)
                        return true;
                }
                return false;

            case NodeType.Folder:
                // Get the folder to find its cabinetId
                var parentFolder = await _folderRepo.GetByIdAsync(nodeId);
                if (parentFolder == null)
                    return false;

                // Check child folders
                var childFolders = await _folderRepo.GetByParentIdAsync(nodeId, parentFolder.CabinetId);
                foreach (var childFolder in childFolders)
                {
                    var childFolderLevel = await _permissionRepo.GetEffectivePermissionAsync(userId, NodeType.Folder, childFolder.Id);
                    if (childFolderLevel > 0)
                        return true;

                    if (childFolder.CreatedBy == userId)
                        return true;

                    // Recursively check grandchildren
                    if (await HasAnyChildPermissionAsync(userId, NodeType.Folder, childFolder.Id))
                        return true;
                }

                // Check documents in this folder
                var documents = await _documentRepo.GetByFolderIdAsync(nodeId);
                foreach (var doc in documents)
                {
                    var docLevel = await _permissionRepo.GetEffectivePermissionAsync(userId, NodeType.Document, doc.Id);
                    if (docLevel > 0)
                        return true;

                    if (doc.CreatedBy == userId)
                        return true;
                }
                return false;

            default:
                return false;
        }
    }

    private async Task<bool> IsCreatorOfNodeAsync(Guid userId, NodeType nodeType, Guid nodeId)
    {
        switch (nodeType)
        {
            case NodeType.Document:
                var doc = await _documentRepo.GetByIdAsync(nodeId);
                return doc != null && doc.CreatedBy == userId;

            case NodeType.Folder:
                var folder = await _folderRepo.GetByIdAsync(nodeId);
                return folder != null && folder.CreatedBy == userId;

            case NodeType.Cabinet:
                var cabinet = await _cabinetRepo.GetByIdAsync(nodeId);
                return cabinet != null && cabinet.CreatedBy == userId;

            default:
                return false;
        }
    }

    public async Task<ServiceResult<EffectivePermissionDto>> GetEffectivePermissionAsync(Guid userId, string nodeType, Guid nodeId)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult<EffectivePermissionDto>.Fail("Invalid node type");

        var (level, sourceType, sourceNodeId) = await _permissionRepo.GetEffectivePermissionWithSourceAsync(userId, parsedNodeType, nodeId);

        var user = await _userRepo.GetByIdAsync(userId);
        string? sourceNodeName = null;
        if (sourceNodeId.HasValue)
        {
            var (_, name) = await GetNodeInfoAsync(parsedNodeType, sourceNodeId.Value);
            sourceNodeName = name;
        }

        return ServiceResult<EffectivePermissionDto>.Ok(new EffectivePermissionDto
        {
            UserId = userId,
            UserName = user?.DisplayName ?? user?.Username,
            NodeType = nodeType,
            NodeId = nodeId,
            PermissionLevel = (int)level,
            SourceType = sourceType,
            SourceNodeId = sourceNodeId,
            SourceNodeName = sourceNodeName
        });
    }

    public async Task<ServiceResult<int>> GetMyPermissionLevelAsync(Guid userId, string nodeType, Guid nodeId)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult<int>.Fail("Invalid node type");

        // Check if user is Administrator - they have full permissions
        var userRoles = await _roleRepo.GetByUserIdAsync(userId);
        if (userRoles.Any(r => r.Name == "Administrator"))
        {
            return ServiceResult<int>.Ok((int)PermissionLevel.Full);
        }

        var level = await _permissionRepo.GetEffectivePermissionAsync(userId, parsedNodeType, nodeId);
        return ServiceResult<int>.Ok((int)level);
    }

    #endregion

    #region Inheritance Management

    public async Task<ServiceResult> BreakInheritanceAsync(string nodeType, Guid nodeId, Guid userId, bool copyInheritedPermissions = true)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult.Fail("Invalid node type");

        // Get inherited permissions before breaking (to copy them)
        List<PermissionDto> inheritedPerms = new();
        if (copyInheritedPermissions)
        {
            inheritedPerms = await GetInheritedPermissionsAsync(parsedNodeType, nodeId);
        }

        if (parsedNodeType == NodeType.Cabinet)
        {
            var cabinet = await _cabinetRepo.GetByIdAsync(nodeId);
            if (cabinet == null) return ServiceResult.Fail("Cabinet not found");
            cabinet.BreakInheritance = true;
            cabinet.ModifiedBy = userId;
            await _cabinetRepo.UpdateAsync(cabinet);
        }
        else if (parsedNodeType == NodeType.Folder)
        {
            var folder = await _folderRepo.GetByIdAsync(nodeId);
            if (folder == null) return ServiceResult.Fail("Folder not found");
            folder.BreakInheritance = true;
            folder.ModifiedBy = userId;
            await _folderRepo.UpdateAsync(folder);
        }
        else
        {
            return ServiceResult.Fail("Cannot break inheritance on documents");
        }

        // Copy inherited permissions as direct permissions on this node
        if (copyInheritedPermissions && inheritedPerms.Any())
        {
            // Group by principal to avoid duplicates (take highest permission level per principal)
            var groupedByPrincipal = inheritedPerms
                .GroupBy(p => new { p.PrincipalType, p.PrincipalId })
                .Select(g => g.OrderByDescending(p => p.PermissionLevel).First())
                .ToList();

            foreach (var perm in groupedByPrincipal)
            {
                // Check if direct permission already exists for this principal
                var existingPerms = await _permissionRepo.GetByNodeAsync(parsedNodeType, nodeId);
                var alreadyExists = existingPerms.Any(ep =>
                    ep.PrincipalType.ToString() == perm.PrincipalType &&
                    ep.PrincipalId == perm.PrincipalId);

                if (!alreadyExists)
                {
                    var newPermission = new Permission
                    {
                        NodeType = parsedNodeType,
                        NodeId = nodeId,
                        PrincipalType = Enum.Parse<PrincipalType>(perm.PrincipalType),
                        PrincipalId = perm.PrincipalId,
                        PermissionLevel = (PermissionLevel)perm.PermissionLevel,
                        IsInherited = false,
                        IncludeChildStructures = perm.IncludeChildStructures,
                        GrantedReason = "Copied from inherited permissions when breaking inheritance",
                        GrantedBy = userId,
                        CreatedBy = userId
                    };
                    await _permissionRepo.CreateAsync(newPermission);
                }
            }
        }

        // IMPORTANT: Ensure the user who breaks inheritance retains Admin access
        // This prevents the creator/admin from losing control of the folder
        var currentUserPerms = await _permissionRepo.GetByNodeAsync(parsedNodeType, nodeId);
        var userHasAdmin = currentUserPerms.Any(p =>
            p.PrincipalType == PrincipalType.User &&
            p.PrincipalId == userId &&
            ((int)p.PermissionLevel & (int)PermissionLevel.Admin) == (int)PermissionLevel.Admin);

        if (!userHasAdmin)
        {
            // Grant Admin permission to the user who broke inheritance
            var adminPermission = new Permission
            {
                NodeType = parsedNodeType,
                NodeId = nodeId,
                PrincipalType = PrincipalType.User,
                PrincipalId = userId,
                PermissionLevel = PermissionLevel.Admin,
                IsInherited = false,
                IncludeChildStructures = false,
                GrantedReason = "Auto-granted Admin access when breaking inheritance",
                GrantedBy = userId,
                CreatedBy = userId
            };
            await _permissionRepo.CreateAsync(adminPermission);
        }

        // Log audit
        await LogPermissionAuditAsync("BreakInheritance", parsedNodeType, nodeId,
            PrincipalType.User, userId, null, null,
            copyInheritedPermissions ? "Inheritance broken (permissions copied)" : "Inheritance broken", userId);

        // Invalidate cache for this node and all descendants
        await _effectivePermissionRepo.InvalidateByNodeAsync(parsedNodeType, nodeId);

        return ServiceResult.Ok("Inheritance broken");
    }

    public async Task<ServiceResult> RestoreInheritanceAsync(string nodeType, Guid nodeId, Guid userId)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult.Fail("Invalid node type");

        if (parsedNodeType == NodeType.Cabinet)
        {
            var cabinet = await _cabinetRepo.GetByIdAsync(nodeId);
            if (cabinet == null) return ServiceResult.Fail("Cabinet not found");
            cabinet.BreakInheritance = false;
            cabinet.ModifiedBy = userId;
            await _cabinetRepo.UpdateAsync(cabinet);
        }
        else if (parsedNodeType == NodeType.Folder)
        {
            var folder = await _folderRepo.GetByIdAsync(nodeId);
            if (folder == null) return ServiceResult.Fail("Folder not found");
            folder.BreakInheritance = false;
            folder.ModifiedBy = userId;
            await _folderRepo.UpdateAsync(folder);
        }

        // Delete non-inherited permissions
        await _permissionRepo.DeleteByNodeAsync(parsedNodeType, nodeId);

        // Log audit
        await LogPermissionAuditAsync("RestoreInheritance", parsedNodeType, nodeId,
            PrincipalType.User, userId, null, null, "Inheritance restored", userId);

        // Invalidate cache
        await _effectivePermissionRepo.InvalidateByNodeAsync(parsedNodeType, nodeId);

        return ServiceResult.Ok("Inheritance restored");
    }

    #endregion

    #region Delegation Operations

    public async Task<ServiceResult<PermissionDelegationDto>> CreateDelegationAsync(CreatePermissionDelegationDto dto, Guid delegatorId)
    {
        if (!Enum.TryParse<NodeType>(dto.NodeType, true, out var nodeType))
            return ServiceResult<PermissionDelegationDto>.Fail("Invalid node type");

        // Verify delegator has the permission they're trying to delegate
        var delegatorLevel = await _permissionRepo.GetEffectivePermissionAsync(delegatorId, nodeType, dto.NodeId);
        if (((int)delegatorLevel & dto.PermissionLevel) != dto.PermissionLevel)
            return ServiceResult<PermissionDelegationDto>.Fail("You cannot delegate permissions you don't have");

        if (dto.StartDate >= dto.EndDate)
            return ServiceResult<PermissionDelegationDto>.Fail("End date must be after start date");

        var delegation = new PermissionDelegation
        {
            DelegatorId = delegatorId,
            DelegateId = dto.DelegateId,
            NodeType = nodeType,
            NodeId = dto.NodeId,
            PermissionLevel = (PermissionLevel)dto.PermissionLevel,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Reason = dto.Reason
        };

        var id = await _delegationRepo.CreateAsync(delegation);

        // Log audit
        await LogPermissionAuditAsync("Delegate", nodeType, dto.NodeId,
            PrincipalType.User, dto.DelegateId, null, dto.PermissionLevel,
            $"Delegated by {delegatorId}: {dto.Reason}", delegatorId);

        delegation.Id = id;
        return ServiceResult<PermissionDelegationDto>.Ok(await MapToDelegationDtoAsync(delegation));
    }

    public async Task<ServiceResult> RevokeDelegationAsync(Guid delegationId, Guid userId)
    {
        var delegation = await _delegationRepo.GetByIdAsync(delegationId);
        if (delegation == null)
            return ServiceResult.Fail("Delegation not found");

        // Only the delegator can revoke (or admin - which should be handled at controller level)
        if (delegation.DelegatorId != userId)
            return ServiceResult.Fail("Only the delegator can revoke this delegation");

        await _delegationRepo.RevokeAsync(delegationId, userId);

        // Log audit - handle nullable NodeType/NodeId
        if (delegation.NodeType.HasValue && delegation.NodeId.HasValue)
        {
            await LogPermissionAuditAsync("RevokeDelegation", delegation.NodeType.Value, delegation.NodeId.Value,
                PrincipalType.User, delegation.DelegateId, (int)delegation.PermissionLevel, null,
                "Delegation revoked", userId);
        }

        return ServiceResult.Ok("Delegation revoked");
    }

    public async Task<ServiceResult<IEnumerable<PermissionDelegationDto>>> GetMyDelegationsAsync(Guid userId)
    {
        var delegations = await _delegationRepo.GetByDelegatorAsync(userId);
        var dtos = new List<PermissionDelegationDto>();
        foreach (var d in delegations)
        {
            dtos.Add(await MapToDelegationDtoAsync(d));
        }
        return ServiceResult<IEnumerable<PermissionDelegationDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<IEnumerable<PermissionDelegationDto>>> GetDelegationsToMeAsync(Guid userId)
    {
        var delegations = await _delegationRepo.GetActiveByDelegateAsync(userId);
        var dtos = new List<PermissionDelegationDto>();
        foreach (var d in delegations)
        {
            dtos.Add(await MapToDelegationDtoAsync(d));
        }
        return ServiceResult<IEnumerable<PermissionDelegationDto>>.Ok(dtos);
    }

    #endregion

    #region Audit Trail

    public async Task<ServiceResult<IEnumerable<PermissionAuditDto>>> GetNodePermissionAuditAsync(string nodeType, Guid nodeId, int take = 100)
    {
        if (!Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
            return ServiceResult<IEnumerable<PermissionAuditDto>>.Fail("Invalid node type");

        var logs = await _auditRepo.GetByNodeAsync(parsedNodeType, nodeId, take);
        var dtos = new List<PermissionAuditDto>();
        foreach (var log in logs)
        {
            dtos.Add(await MapToAuditDtoAsync(log));
        }
        return ServiceResult<IEnumerable<PermissionAuditDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<IEnumerable<PermissionAuditDto>>> GetPrincipalPermissionAuditAsync(string principalType, Guid principalId, int take = 100)
    {
        if (!Enum.TryParse<PrincipalType>(principalType, true, out var parsedPrincipalType))
            return ServiceResult<IEnumerable<PermissionAuditDto>>.Fail("Invalid principal type");

        var logs = await _auditRepo.GetByPrincipalAsync(parsedPrincipalType, principalId, take);
        var dtos = new List<PermissionAuditDto>();
        foreach (var log in logs)
        {
            dtos.Add(await MapToAuditDtoAsync(log));
        }
        return ServiceResult<IEnumerable<PermissionAuditDto>>.Ok(dtos);
    }

    #endregion

    #region Cache Management

    public async Task InvalidatePermissionCacheAsync(string nodeType, Guid nodeId)
    {
        if (Enum.TryParse<NodeType>(nodeType, true, out var parsedNodeType))
        {
            await _effectivePermissionRepo.InvalidateByNodeAsync(parsedNodeType, nodeId);
        }
    }

    public async Task InvalidateUserPermissionCacheAsync(Guid userId)
    {
        await _effectivePermissionRepo.InvalidateByUserAsync(userId);
    }

    #endregion

    #region Private Helpers

    private async Task<PermissionDto> MapToPermissionDtoAsync(Permission p,
        bool isInherited = false, NodeType? inheritedFromNodeType = null,
        Guid? inheritedFromNodeId = null, string? inheritedFromNodeName = null)
    {
        string? principalName = null;
        string? grantedByName = null;

        if (p.PrincipalType == PrincipalType.User)
        {
            var user = await _userRepo.GetByIdAsync(p.PrincipalId);
            principalName = user?.DisplayName ?? user?.Username;
        }
        else if (p.PrincipalType == PrincipalType.Role)
        {
            var role = await _roleRepo.GetByIdAsync(p.PrincipalId);
            principalName = role?.Name;
        }
        else if (p.PrincipalType == PrincipalType.Structure)
        {
            var structure = await _structureRepo.GetByIdAsync(p.PrincipalId);
            principalName = structure?.Name;
        }

        if (p.GrantedBy.HasValue)
        {
            var granter = await _userRepo.GetByIdAsync(p.GrantedBy.Value);
            grantedByName = granter?.DisplayName ?? granter?.Username;
        }

        return new PermissionDto
        {
            Id = p.Id,
            NodeType = p.NodeType.ToString(),
            NodeId = p.NodeId,
            PrincipalType = p.PrincipalType.ToString(),
            PrincipalId = p.PrincipalId,
            PrincipalName = principalName,
            PermissionLevel = (int)p.PermissionLevel,
            IsInherited = isInherited || p.IsInherited,
            InheritedFromNodeId = inheritedFromNodeId,
            InheritedFromNodeType = inheritedFromNodeType?.ToString(),
            InheritedFromNodeName = inheritedFromNodeName,
            IncludeChildStructures = p.IncludeChildStructures,
            ExpiresAt = p.ExpiresAt,
            GrantedReason = p.GrantedReason,
            GrantedBy = p.GrantedBy,
            GrantedByName = grantedByName,
            CreatedAt = p.CreatedAt
        };
    }

    private async Task<PermissionDelegationDto> MapToDelegationDtoAsync(PermissionDelegation d)
    {
        var delegator = await _userRepo.GetByIdAsync(d.DelegatorId);
        var delegatee = await _userRepo.GetByIdAsync(d.DelegateId);

        string? nodeName = null;
        if (d.NodeType.HasValue && d.NodeId.HasValue)
        {
            var (_, name) = await GetNodeInfoAsync(d.NodeType.Value, d.NodeId.Value);
            nodeName = name;
        }

        return new PermissionDelegationDto
        {
            Id = d.Id,
            DelegatorId = d.DelegatorId,
            DelegatorName = delegator?.DisplayName ?? delegator?.Username,
            DelegateId = d.DelegateId,
            DelegateName = delegatee?.DisplayName ?? delegatee?.Username,
            NodeType = d.NodeType?.ToString() ?? "All",
            NodeId = d.NodeId ?? Guid.Empty,
            NodeName = nodeName,
            PermissionLevel = (int)d.PermissionLevel,
            StartDate = d.StartDate,
            EndDate = d.EndDate,
            Reason = d.Reason,
            IsActive = d.RevokedAt == null && d.StartDate <= DateTime.UtcNow && d.EndDate > DateTime.UtcNow,
            CreatedAt = d.CreatedAt
        };
    }

    private async Task<PermissionAuditDto> MapToAuditDtoAsync(PermissionAuditLog log)
    {
        string? principalName = null;
        if (log.PrincipalType == PrincipalType.User)
        {
            var user = await _userRepo.GetByIdAsync(log.PrincipalId);
            principalName = user?.DisplayName ?? user?.Username;
        }
        else if (log.PrincipalType == PrincipalType.Role)
        {
            var role = await _roleRepo.GetByIdAsync(log.PrincipalId);
            principalName = role?.Name;
        }
        else if (log.PrincipalType == PrincipalType.Structure)
        {
            var structure = await _structureRepo.GetByIdAsync(log.PrincipalId);
            principalName = structure?.Name;
        }

        var performer = await _userRepo.GetByIdAsync(log.PerformedBy);
        var (_, nodeName) = await GetNodeInfoAsync(log.NodeType, log.NodeId);

        return new PermissionAuditDto
        {
            Id = log.Id,
            Action = log.Action,
            NodeType = log.NodeType.ToString(),
            NodeId = log.NodeId,
            NodeName = nodeName,
            PrincipalType = log.PrincipalType.ToString(),
            PrincipalId = log.PrincipalId,
            PrincipalName = principalName,
            OldLevel = log.OldPermissionLevel,
            NewLevel = log.NewPermissionLevel,
            Reason = log.Reason,
            PerformedBy = log.PerformedBy,
            PerformedByName = performer?.DisplayName ?? performer?.Username,
            PerformedAt = log.PerformedAt
        };
    }

    private async Task<(bool BreakInheritance, string? Name)> GetNodeInfoAsync(NodeType nodeType, Guid nodeId)
    {
        switch (nodeType)
        {
            case NodeType.Cabinet:
                var cabinet = await _cabinetRepo.GetByIdAsync(nodeId);
                return (cabinet?.BreakInheritance ?? false, cabinet?.Name);

            case NodeType.Folder:
                var folder = await _folderRepo.GetByIdAsync(nodeId);
                return (folder?.BreakInheritance ?? false, folder?.Name);

            case NodeType.Document:
                var doc = await _documentRepo.GetByIdAsync(nodeId);
                return (false, doc?.Name);

            default:
                return (false, null);
        }
    }

    private async Task LogPermissionAuditAsync(string action, NodeType nodeType, Guid nodeId,
        PrincipalType principalType, Guid principalId, int? oldLevel, int? newLevel, string? reason, Guid performedBy)
    {
        var log = new PermissionAuditLog
        {
            Action = action,
            NodeType = nodeType,
            NodeId = nodeId,
            PrincipalType = principalType,
            PrincipalId = principalId,
            OldPermissionLevel = oldLevel,
            NewPermissionLevel = newLevel,
            Reason = reason,
            PerformedBy = performedBy
        };

        await _auditRepo.LogAsync(log);
    }

    #endregion
}
