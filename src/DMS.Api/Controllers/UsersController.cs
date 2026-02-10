using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var result = string.IsNullOrEmpty(search)
            ? await _userService.GetAllPaginatedAsync(page, pageSize)
            : await _userService.SearchPaginatedAsync(search, page, pageSize);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        var result = await _userService.CreateAsync(dto);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("{id:guid}/roles")]
    public async Task<IActionResult> GetUserRoles(Guid id)
    {
        var result = await _userService.GetUserRolesAsync(id);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("{id:guid}/roles/{roleId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> AssignRole(Guid id, Guid roleId)
    {
        var result = await _userService.AssignRoleAsync(id, roleId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId)
    {
        var result = await _userService.RemoveRoleAsync(id, roleId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }
}
