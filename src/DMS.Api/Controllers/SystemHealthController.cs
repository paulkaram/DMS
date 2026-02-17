using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize(Roles = "Administrator")]
public class SystemHealthController : BaseApiController
{
    private readonly ISystemHealthService _healthService;

    public SystemHealthController(ISystemHealthService healthService) => _healthService = healthService;

    [HttpGet]
    public async Task<IActionResult> GetSystemHealth()
        => Ok(await _healthService.GetSystemHealthAsync());

    [HttpGet("jobs")]
    public async Task<IActionResult> GetJobHistory(
        [FromQuery] string? jobName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _healthService.GetJobHistoryAsync(jobName, page, pageSize));
}
