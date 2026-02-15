using DMS.Api.Constants;
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
    public async Task<IActionResult> GetRecent([FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultActivityPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var result = await _activityLogService.GetRecentPagedAsync(page, pageSize);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("by-node/{nodeType}/{nodeId:guid}")]
    public async Task<IActionResult> GetByNode(string nodeType, Guid nodeId, [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var result = await _activityLogService.GetByNodePagedAsync(nodeType, nodeId, page, pageSize);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("by-user/{userId:guid}")]
    public async Task<IActionResult> GetByUser(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var result = await _activityLogService.GetByUserPagedAsync(userId, page, pageSize);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("my-activity")]
    public async Task<IActionResult> GetMyActivity([FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var userId = GetCurrentUserId();
        var result = await _activityLogService.GetByUserPagedAsync(userId, page, pageSize);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
