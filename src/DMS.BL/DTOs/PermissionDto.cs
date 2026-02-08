namespace DMS.BL.DTOs;

public class PermissionDto
{
    public Guid Id { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string PrincipalType { get; set; } = string.Empty;
    public Guid PrincipalId { get; set; }
    public string? PrincipalName { get; set; }
    public int PermissionLevel { get; set; }
    public bool IsInherited { get; set; }
    public Guid? InheritedFromNodeId { get; set; }
    public string? InheritedFromNodeType { get; set; }
    public string? InheritedFromNodeName { get; set; }
    public bool IncludeChildStructures { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? GrantedReason { get; set; }
    public Guid? GrantedBy { get; set; }
    public string? GrantedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePermissionDto
{
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string PrincipalType { get; set; } = string.Empty;
    public Guid PrincipalId { get; set; }
    public int PermissionLevel { get; set; }
    public bool IncludeChildStructures { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
    public string? GrantedReason { get; set; }
}

public class UpdatePermissionDto
{
    public int PermissionLevel { get; set; }
    public bool IncludeChildStructures { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? GrantedReason { get; set; }
}

public class NodePermissionsDto
{
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string? NodeName { get; set; }
    public bool BreakInheritance { get; set; }
    public List<PermissionDto> Permissions { get; set; } = new();
}

public class EffectivePermissionDto
{
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public int PermissionLevel { get; set; }
    public string SourceType { get; set; } = string.Empty;  // Direct, Inherited, Role, Structure
    public Guid? SourceNodeId { get; set; }
    public string? SourceNodeName { get; set; }
}

public class PermissionAuditDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string? NodeName { get; set; }
    public string PrincipalType { get; set; } = string.Empty;
    public Guid PrincipalId { get; set; }
    public string? PrincipalName { get; set; }
    public int? OldLevel { get; set; }
    public int? NewLevel { get; set; }
    public string? Reason { get; set; }
    public Guid PerformedBy { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime PerformedAt { get; set; }
}

public class PermissionDelegationDto
{
    public Guid Id { get; set; }
    public Guid DelegatorId { get; set; }
    public string? DelegatorName { get; set; }
    public Guid DelegateId { get; set; }
    public string? DelegateName { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public string? NodeName { get; set; }
    public int PermissionLevel { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePermissionDelegationDto
{
    public Guid DelegateId { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public Guid NodeId { get; set; }
    public int PermissionLevel { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}
