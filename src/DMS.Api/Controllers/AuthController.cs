using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.ChangePasswordAsync(userId, dto);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(
        [FromServices] IUserService userService,
        [FromServices] IRolePermissionService rolePermissionService)
    {
        var userId = GetCurrentUserId();
        var userResult = await userService.GetByIdAsync(userId);
        if (!userResult.Success)
            return NotFound(userResult.Errors);

        var permissionsResult = await rolePermissionService.GetUserAllowedActionsAsync(userId);

        return Ok(new
        {
            userResult.Data?.Id,
            userResult.Data?.Username,
            userResult.Data?.Email,
            userResult.Data?.FirstName,
            userResult.Data?.LastName,
            userResult.Data?.DisplayName,
            userResult.Data?.Roles,
            AllowedActions = permissionsResult.Data ?? new List<string>()
        });
    }
}
