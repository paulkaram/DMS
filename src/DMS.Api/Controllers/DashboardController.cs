using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _dashboardService.GetStatisticsAsync(GetCurrentUserId());
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("recent-documents")]
    public async Task<IActionResult> GetRecentDocuments([FromQuery] int take = 10)
    {
        var privacyLevel = GetCurrentUserPrivacyLevel();
        var result = await _dashboardService.GetRecentDocumentsAsync(take, privacyLevel);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("my-checkouts")]
    public async Task<IActionResult> GetMyCheckouts()
    {
        var userId = GetCurrentUserId();
        var result = await _dashboardService.GetMyCheckedOutDocumentsAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("expired-documents")]
    public async Task<IActionResult> GetExpiredDocuments([FromQuery] int take = 5)
    {
        var privacyLevel = GetCurrentUserPrivacyLevel();
        var result = await _dashboardService.GetExpiredDocumentsAsync(take, privacyLevel);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
