using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecycleBinController : ControllerBase
{
    private readonly IRecycleBinService _recycleBinService;

    public RecycleBinController(IRecycleBinService recycleBinService)
    {
        _recycleBinService = recycleBinService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RecycleBinItemDto>>> GetMyRecycleBin()
    {
        var items = await _recycleBinService.GetUserRecycleBinAsync(GetUserId());
        return Ok(items);
    }

    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<RecycleBinItemDto>>> GetAll([FromQuery] int? nodeType = null)
    {
        var items = await _recycleBinService.GetAllAsync(nodeType);
        return Ok(items);
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult> RestoreItem(Guid id, [FromBody] RestoreRequest? request = null)
    {
        var result = await _recycleBinService.RestoreItemAsync(id, request?.RestoreToFolderId);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> PermanentlyDelete(Guid id)
    {
        var result = await _recycleBinService.PermanentlyDeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("empty")]
    public async Task<ActionResult> EmptyRecycleBin()
    {
        await _recycleBinService.EmptyRecycleBinAsync(GetUserId());
        return Ok();
    }

    [HttpPost("purge-expired")]
    public async Task<ActionResult> PurgeExpired()
    {
        await _recycleBinService.PurgeExpiredAsync();
        return Ok();
    }
}
