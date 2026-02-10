using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class ActivityLogsController : BaseApiController
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogsController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent([FromQuery] int take = 100)
    {
        var result = await _activityLogService.GetRecentAsync(take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("by-node/{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> GetByNode(string nodeType, Guid nodeId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _activityLogService.GetByNodeAsync(nodeType, nodeId, skip, take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("by-user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _activityLogService.GetByUserAsync(userId, skip, take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("my-activity")]
    public async Task<IActionResult> GetMyActivity([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = GetCurrentUserId();
        var result = await _activityLogService.GetByUserAsync(userId, skip, take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
