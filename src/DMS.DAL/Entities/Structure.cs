using System.Text.Json.Serialization;
using DMS.DAL.Data;

namespace DMS.DAL.Entities;

/// <summary>
/// Represents an organizational structure (Ministry, Department, Division, Section, Unit)
/// Used for structure-based permission assignment
/// </summary>
public class Structure : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; } // Arabic name for government entities
    public string? Code { get; set; } // Unique code like "MOF-FIN-001"
    public StructureType StructureType { get; set; } = StructureType.Department;
    public string? Path { get; set; } // Materialized path: /id1/id2/id3/
    public int Level { get; set; } // Depth in hierarchy
    public Guid? ManagerId { get; set; } // Structure manager/head
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation properties
    [JsonIgnore]
    public Structure? Parent { get; set; }
    [JsonIgnore]
    public ICollection<Structure>? Children { get; set; }
    [JsonIgnore]
    public ICollection<StructureMember>? Members { get; set; }
}

/// <summary>
/// Types of organizational structures
/// </summary>
public enum StructureType
{
    Ministry = 1,      // Top-level organization
    Department = 2,    // Major division
    Division = 3,      // Sub-division
    Section = 4,       // Section within division
    Unit = 5           // Smallest unit
}

/// <summary>
/// Represents a user's membership in an organizational structure
/// </summary>
public class StructureMember
{
    public Guid Id { get; set; }
    public Guid StructureId { get; set; }
    public Guid UserId { get; set; }
    public bool IsPrimary { get; set; } // User's primary/home structure
    public string? Position { get; set; } // User's position/title in this structure
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; } // NULL = still active
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [JsonIgnore]
    public Structure? Structure { get; set; }
    [JsonIgnore]
    public User? User { get; set; }
}

/// <summary>
/// Cached effective permissions for fast lookup
/// Recalculated when permissions change
/// </summary>
public class EffectivePermission
{
    public Guid Id { get; set; }
    public NodeType NodeType { get; set; }
    public Guid NodeId { get; set; }
    public Guid UserId { get; set; }
    public PermissionLevel EffectiveLevel { get; set; }

    // Source tracking
    public string SourceType { get; set; } = string.Empty; // 'Direct', 'Role', 'Structure', 'Inherited'
    public Guid? SourcePermissionId { get; set; }
    public NodeType? SourceNodeType { get; set; }
    public Guid? SourceNodeId { get; set; }
    public string? InheritancePath { get; set; } // Full path for debugging

    public DateTime CalculatedAt { get; set; } = DateTime.Now;
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Audit log for all permission changes
/// </summary>
public class PermissionAuditLog
{
    public Guid Id { get; set; }

    // Action details
    public string Action { get; set; } = string.Empty; // 'Grant', 'Revoke', 'Modify', 'BreakInheritance', 'RestoreInheritance'

    // Target node
    public NodeType NodeType { get; set; }
    public Guid NodeId { get; set; }
    public string? NodeName { get; set; } // Denormalized for historical record

    // Principal affected
    public PrincipalType PrincipalType { get; set; }
    public Guid PrincipalId { get; set; }
    public string? PrincipalName { get; set; } // Denormalized

    // Permission change details
    public int? OldPermissionLevel { get; set; }
    public int? NewPermissionLevel { get; set; }

    // Context
    public string? Reason { get; set; }

    // Who performed the action
    public Guid PerformedBy { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.Now;

    // Additional context
    public string? IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? SessionId { get; set; }
    public bool IsSystemAction { get; set; } // True if automated
}

/// <summary>
/// Temporary permission delegation from one user to another
/// </summary>
public class PermissionDelegation
{
    public Guid Id { get; set; }
    public Guid DelegatorId { get; set; } // User delegating
    public Guid DelegateId { get; set; } // User receiving delegation

    // Scope (NULL = all permissions)
    public NodeType? NodeType { get; set; }
    public Guid? NodeId { get; set; }

    // What level is delegated
    public PermissionLevel PermissionLevel { get; set; }

    // Validity period
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } // Must have end date

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }

    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    [JsonIgnore]
    public User? Delegator { get; set; }
    [JsonIgnore]
    public User? Delegate { get; set; }
}
