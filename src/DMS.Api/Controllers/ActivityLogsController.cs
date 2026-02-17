using DMS.Api.Constants;
using DMS.BL.DTOs;
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

    [HttpGet("search")]
    [Authorize(Roles = $"{AppConstants.Roles.Administrator},{AppConstants.Roles.Auditor}")]
    public async Task<IActionResult> Search([FromQuery] AuditExportQueryDto query)
    {
        var result = await _activityLogService.SearchAsync(query);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("export")]
    [Authorize(Roles = $"{AppConstants.Roles.Administrator},{AppConstants.Roles.Auditor}")]
    public async Task<IActionResult> ExportCsv([FromQuery] AuditExportQueryDto query)
    {
        var result = await _activityLogService.ExportToCsvAsync(query);
        if (!result.Success) return BadRequest(result.Errors);
        return File(result.Data!, "text/csv", $"audit-log-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
    }

    [HttpGet("verify-chain")]
    [Authorize(Roles = $"{AppConstants.Roles.Administrator},{AppConstants.Roles.Auditor}")]
    public async Task<IActionResult> VerifyChain([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var result = await _activityLogService.VerifyAuditChainAsync(from, to);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
