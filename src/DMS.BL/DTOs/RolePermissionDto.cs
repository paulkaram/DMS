namespace DMS.BL.DTOs;

public class SystemActionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class RoleActionPermissionDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public Guid ActionId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string ActionCategory { get; set; } = string.Empty;
    public bool IsAllowed { get; set; }
}

public class RolePermissionMatrixDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }
    public List<string> AllowedActionCodes { get; set; } = new();
    public List<SystemActionDto> AllowedActions { get; set; } = new();
}

public class SetRolePermissionsDto
{
    public List<Guid> ActionIds { get; set; } = new();
}

public class UserPermissionsDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> AllowedActionCodes { get; set; } = new();
}
