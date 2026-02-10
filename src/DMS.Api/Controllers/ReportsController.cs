using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class ReportsController : BaseApiController
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _reportsService.GetStatisticsAsync();
        return OkOrBadRequest(result);
    }

    [HttpGet("monthly-growth")]
    public async Task<IActionResult> GetMonthlyGrowth([FromQuery] int? year = null)
    {
        var result = await _reportsService.GetMonthlyGrowthAsync(year);
        return OkOrBadRequest(result);
    }

    [HttpGet("document-types")]
    public async Task<IActionResult> GetDocumentTypes()
    {
        var result = await _reportsService.GetDocumentTypesAsync();
        return OkOrBadRequest(result);
    }

    [HttpGet("recent-activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int take = 10)
    {
        var result = await _reportsService.GetRecentActivityAsync(take);
        return OkOrBadRequest(result);
    }
}
