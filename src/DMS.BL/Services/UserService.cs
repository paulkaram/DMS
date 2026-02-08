using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<ServiceResult<UserDto>> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return ServiceResult<UserDto>.Fail("User not found");

        var roles = await _roleRepository.GetByUserIdAsync(id);
        var dto = MapToDto(user);
        dto.Roles = roles.Select(MapRoleToDto).ToList();
        return ServiceResult<UserDto>.Ok(dto);
    }

    public async Task<ServiceResult<UserDto>> GetByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
            return ServiceResult<UserDto>.Fail("User not found");

        return ServiceResult<UserDto>.Ok(MapToDto(user));
    }

    public async Task<ServiceResult<List<UserDto>>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var dto = MapToDto(user);
            var roles = await _roleRepository.GetByUserIdAsync(user.Id);
            dto.Roles = roles.Select(MapRoleToDto).ToList();
            userDtos.Add(dto);
        }
        return ServiceResult<List<UserDto>>.Ok(userDtos);
    }

    public async Task<ServiceResult<List<UserDto>>> SearchAsync(string? search)
    {
        var users = await _userRepository.SearchAsync(search);
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var dto = MapToDto(user);
            var roles = await _roleRepository.GetByUserIdAsync(user.Id);
            dto.Roles = roles.Select(MapRoleToDto).ToList();
            userDtos.Add(dto);
        }
        return ServiceResult<List<UserDto>>.Ok(userDtos);
    }

    public async Task<ServiceResult<UserDto>> CreateAsync(CreateUserDto dto)
    {
        var existing = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existing != null)
            return ServiceResult<UserDto>.Fail("Username already exists");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = $"{dto.FirstName} {dto.LastName}".Trim(),
            IsActive = true
        };

        var id = await _userRepository.CreateAsync(user);
        user.Id = id;

        return ServiceResult<UserDto>.Ok(MapToDto(user), "User created successfully");
    }

    public async Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return ServiceResult<UserDto>.Fail("User not found");

        user.Email = dto.Email;
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.DisplayName = $"{dto.FirstName} {dto.LastName}".Trim();

        await _userRepository.UpdateAsync(user);

        return ServiceResult<UserDto>.Ok(MapToDto(user), "User updated successfully");
    }

    public async Task<ServiceResult<List<RoleDto>>> GetRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return ServiceResult<List<RoleDto>>.Ok(roles.Select(MapRoleToDto).ToList());
    }

    public async Task<ServiceResult<List<RoleDto>>> GetUserRolesAsync(Guid userId)
    {
        var roles = await _roleRepository.GetByUserIdAsync(userId);
        return ServiceResult<List<RoleDto>>.Ok(roles.Select(MapRoleToDto).ToList());
    }

    public async Task<ServiceResult> AssignRoleAsync(Guid userId, Guid roleId)
    {
        var success = await _roleRepository.AddUserToRoleAsync(userId, roleId);
        return success ? ServiceResult.Ok("Role assigned") : ServiceResult.Fail("Failed to assign role");
    }

    public async Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId)
    {
        var success = await _roleRepository.RemoveUserFromRoleAsync(userId, roleId);
        return success ? ServiceResult.Ok("Role removed") : ServiceResult.Fail("Failed to remove role");
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt
        };
    }

    private static RoleDto MapRoleToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description
        };
    }
}
