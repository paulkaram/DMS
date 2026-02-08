using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DelegationsController : ControllerBase
{
    private readonly IDelegationService _delegationService;

    public DelegationsController(IDelegationService delegationService)
    {
        _delegationService = delegationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyDelegations()
    {
        var userId = GetCurrentUserId();
        var result = await _delegationService.GetMyDelegationsAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("to-me")]
    public async Task<IActionResult> GetDelegationsToMe()
    {
        var userId = GetCurrentUserId();
        var result = await _delegationService.GetDelegationsToMeAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _delegationService.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDelegationDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _delegationService.CreateAsync(dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDelegationDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _delegationService.UpdateAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _delegationService.DeleteAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
