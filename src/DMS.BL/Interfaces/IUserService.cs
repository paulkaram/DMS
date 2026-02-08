using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IUserService
{
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);
    Task<ServiceResult<List<UserDto>>> GetAllAsync();
    Task<ServiceResult<List<UserDto>>> SearchAsync(string? search);
    Task<ServiceResult<UserDto>> CreateAsync(CreateUserDto dto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<ServiceResult<List<RoleDto>>> GetRolesAsync();
    Task<ServiceResult<List<RoleDto>>> GetUserRolesAsync(Guid userId);
    Task<ServiceResult> AssignRoleAsync(Guid userId, Guid roleId);
    Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId);
}

public interface IDelegationService
{
    Task<ServiceResult<DelegationDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<DelegationDto>>> GetMyDelegationsAsync(Guid userId);
    Task<ServiceResult<List<DelegationDto>>> GetDelegationsToMeAsync(Guid userId);
    Task<ServiceResult<DelegationDto>> CreateAsync(CreateDelegationDto dto, Guid fromUserId);
    Task<ServiceResult<DelegationDto>> UpdateAsync(Guid id, UpdateDelegationDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id, Guid userId);
}

public interface IAuthService
{
    Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<ServiceResult<LoginResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
}
