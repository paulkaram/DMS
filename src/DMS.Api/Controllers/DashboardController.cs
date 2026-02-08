using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _dashboardService.GetStatisticsAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("recent-documents")]
    public async Task<IActionResult> GetRecentDocuments([FromQuery] int take = 10)
    {
        var result = await _dashboardService.GetRecentDocumentsAsync(take);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("my-checkouts")]
    public async Task<IActionResult> GetMyCheckouts()
    {
        var userId = GetCurrentUserId();
        var result = await _dashboardService.GetMyCheckedOutDocumentsAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
