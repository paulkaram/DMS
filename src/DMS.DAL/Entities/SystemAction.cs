namespace DMS.DAL.Entities;

/// <summary>
/// Defines a system action that can be permitted to roles.
/// </summary>
public class SystemAction
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Maps a role to an allowed system action.
/// </summary>
public class RoleActionPermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid ActionId { get; set; }
    public bool IsAllowed { get; set; } = true;
    public Guid? GrantedBy { get; set; }
    public DateTime GrantedAt { get; set; }

    // Navigation properties (populated by repository)
    public string? RoleName { get; set; }
    public string? ActionCode { get; set; }
    public string? ActionName { get; set; }
    public string? ActionCategory { get; set; }
}
