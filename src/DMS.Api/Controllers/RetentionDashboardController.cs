using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize(Roles = "Administrator,Admin,Records,Auditor")]
public class RetentionDashboardController : BaseApiController
{
    private readonly IRetentionDashboardService _dashboardService;

    public RetentionDashboardController(IRetentionDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _dashboardService.GetDashboardAsync();
        return OkOrBadRequest(result);
    }

    [HttpGet("actions")]
    public async Task<IActionResult> GetRecentActions([FromQuery] int take = 20)
    {
        var result = await _dashboardService.GetRecentActionsAsync(take);
        return OkOrBadRequest(result);
    }

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobHistory([FromQuery] int take = 10)
    {
        var result = await _dashboardService.GetJobHistoryAsync(take);
        return OkOrBadRequest(result);
    }
}
