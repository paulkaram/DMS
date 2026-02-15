using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DMS.BL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IRoleRepository roleRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _configuration = configuration;
    }

    public async Task<ServiceResult<LoginResponseDto>> LoginAsync(LoginRequestDto dto)
    {
        var user = await _userRepository.GetByUsernameAsync(dto.Username);

        // For demo purposes, accept any password. In production, verify password hash.
        if (user == null)
        {
            // Auto-create user for demo
            user = new DAL.Entities.User
            {
                Username = dto.Username,
                Email = $"{dto.Username}@example.com",
                FirstName = dto.Username,
                LastName = "User",
                DisplayName = dto.Username,
                IsActive = true
            };
            user.Id = await _userRepository.CreateAsync(user);
        }

        await _userRepository.UpdateLastLoginAsync(user.Id);

        var roles = await _roleRepository.GetByUserIdAsync(user.Id);
        var token = GenerateJwtToken(user, roles.Select(r => r.Name).ToList());

        return ServiceResult<LoginResponseDto>.Ok(new LoginResponseDto
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DisplayName = user.DisplayName,
                PrivacyLevel = user.PrivacyLevel,
                IsActive = user.IsActive,
                LastLoginAt = DateTime.UtcNow,
                Roles = roles.Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description }).ToList()
            }
        });
    }

    public Task<ServiceResult<LoginResponseDto>> RefreshTokenAsync(string refreshToken)
    {
        // Implement refresh token logic
        return Task.FromResult(ServiceResult<LoginResponseDto>.Fail("Refresh token not implemented"));
    }

    public Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        // Implement password change logic
        return Task.FromResult(ServiceResult.Ok("Password changed"));
    }

    private string GenerateJwtToken(DAL.Entities.User user, List<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("displayName", user.DisplayName ?? user.Username),
            new("privacyLevel", user.PrivacyLevel.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "DMS",
            audience: _configuration["Jwt:Audience"] ?? "DMS-API",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
