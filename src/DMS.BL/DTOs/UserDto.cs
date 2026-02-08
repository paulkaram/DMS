namespace DMS.BL.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Password { get; set; }
}

public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class DelegationDto
{
    public Guid Id { get; set; }
    public Guid FromUserId { get; set; }
    public string? FromUserName { get; set; }
    public Guid ToUserId { get; set; }
    public string? ToUserName { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class CreateDelegationDto
{
    public Guid ToUserId { get; set; }
    public Guid? RoleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class UpdateDelegationDto
{
    public Guid ToUserId { get; set; }
    public Guid? RoleId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
}
