namespace DMS.DAL.Entities;

/// <summary>
/// Represents a permission grant on a node (Cabinet, Folder, or Document)
/// to a principal (User, Role, or Structure)
/// </summary>
public class Permission
{
    public Guid Id { get; set; }

    // Target node
    public NodeType NodeType { get; set; }
    public Guid NodeId { get; set; }

    // Principal (who has the permission)
    public PrincipalType PrincipalType { get; set; }
    public Guid PrincipalId { get; set; }

    // Permission level (flags can be combined)
    public PermissionLevel PermissionLevel { get; set; }

    // Inheritance tracking
    public bool IsInherited { get; set; }

    // For structure-based permissions: include child structures?
    public bool IncludeChildStructures { get; set; } = true;

    // Temporary permissions
    public DateTime? ExpiresAt { get; set; }

    // Audit trail
    public string? GrantedReason { get; set; }
    public Guid? GrantedBy { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Types of nodes that can have permissions
/// </summary>
public enum NodeType
{
    Cabinet = 1,
    Folder = 2,
    Document = 3
}

/// <summary>
/// Types of principals that can be granted permissions
/// </summary>
public enum PrincipalType
{
    User = 1,
    Role = 2,
    Structure = 3  // Organizational structure (Department, Division, etc.)
}

/// <summary>
/// Permission levels (flags - can be combined)
/// </summary>
[Flags]
public enum PermissionLevel
{
    None = 0,
    Read = 1,       // View document/folder
    Write = 2,      // Edit, check-out/check-in
    Delete = 4,     // Delete document/folder
    Admin = 8,      // Manage permissions
    Full = Read | Write | Delete | Admin  // All permissions
}
