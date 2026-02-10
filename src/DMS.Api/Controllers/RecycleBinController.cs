using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class RecycleBinController : BaseApiController
{
    private readonly IRecycleBinService _recycleBinService;

    public RecycleBinController(IRecycleBinService recycleBinService)
    {
        _recycleBinService = recycleBinService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<RecycleBinItemDto>>> GetMyRecycleBin(
        [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var result = await _recycleBinService.GetUserRecycleBinPaginatedAsync(GetCurrentUserId(), page, pageSize);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<ActionResult<PagedResultDto<RecycleBinItemDto>>> GetAll(
        [FromQuery] int? nodeType = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var result = await _recycleBinService.GetAllPaginatedAsync(nodeType, page, pageSize);
        return Ok(result);
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
        await _recycleBinService.EmptyRecycleBinAsync(GetCurrentUserId());
        return Ok();
    }

    [HttpPost("purge-expired")]
    public async Task<ActionResult> PurgeExpired()
    {
        await _recycleBinService.PurgeExpiredAsync();
        return Ok();
    }
}
