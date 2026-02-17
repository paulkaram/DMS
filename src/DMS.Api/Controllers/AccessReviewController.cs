using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize(Roles = "Administrator,SecurityOfficer")]
public class AccessReviewController : BaseApiController
{
    private readonly IAccessReviewService _service;

    public AccessReviewController(IAccessReviewService service) => _service = service;

    [HttpGet("campaigns")]
    public async Task<IActionResult> GetCampaigns()
        => OkOrBadRequest(await _service.GetCampaignsAsync());

    [HttpGet("campaigns/{id:guid}")]
    public async Task<IActionResult> GetCampaign(Guid id)
        => OkOrNotFound(await _service.GetCampaignAsync(id));

    [HttpPost("campaigns")]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateAccessReviewCampaignDto dto)
        => OkOrBadRequest(await _service.CreateCampaignAsync(dto, GetCurrentUserId()));

    [HttpGet("campaigns/{id:guid}/entries")]
    public async Task<IActionResult> GetEntries(Guid id)
        => OkOrBadRequest(await _service.GetCampaignEntriesAsync(id));

    [HttpPost("entries/{id:guid}/decide")]
    public async Task<IActionResult> SubmitDecision(Guid id, [FromBody] SubmitAccessReviewDto dto)
        => OkOrBadRequest(await _service.SubmitReviewDecisionAsync(id, dto, GetCurrentUserId()));

    [HttpGet("stale-permissions")]
    public async Task<IActionResult> GetStalePermissions([FromQuery] int inactiveDays = 90)
        => OkOrBadRequest(await _service.GetStalePermissionsAsync(inactiveDays));
}
