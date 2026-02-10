using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IUserService
{
    Task<ServiceResult<UserDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<UserDto>> GetByUsernameAsync(string username);
    Task<ServiceResult<List<UserDto>>> GetAllAsync();
    Task<ServiceResult<List<UserDto>>> SearchAsync(string? search);
    Task<ServiceResult<PagedResultDto<UserDto>>> GetAllPaginatedAsync(int page, int pageSize);
    Task<ServiceResult<PagedResultDto<UserDto>>> SearchPaginatedAsync(string? search, int page, int pageSize);
    Task<ServiceResult<UserDto>> CreateAsync(CreateUserDto dto);
    Task<ServiceResult<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto);
    Task<ServiceResult<List<RoleDto>>> GetRolesAsync();
    Task<ServiceResult<List<RoleDto>>> GetUserRolesAsync(Guid userId);
    Task<ServiceResult> AssignRoleAsync(Guid userId, Guid roleId);
    Task<ServiceResult> RemoveRoleAsync(Guid userId, Guid roleId);
}

public interface IAuthService
{
    Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto);
    Task<ServiceResult<LoginResponseDto>> RefreshTokenAsync(string refreshToken);
    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
}
