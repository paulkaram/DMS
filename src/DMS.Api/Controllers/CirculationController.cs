using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class CirculationController : BaseApiController
{
    private readonly ICirculationService _service;

    public CirculationController(ICirculationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetActiveLoans() => OkOrBadRequest(await _service.GetActiveLoansAsync());

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue() => OkOrBadRequest(await _service.GetOverdueAsync());

    [HttpGet("history/{physicalItemId:guid}")]
    public async Task<IActionResult> GetItemHistory(Guid physicalItemId)
        => OkOrBadRequest(await _service.GetItemHistoryAsync(physicalItemId));

    [HttpPost("checkout")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutPhysicalItemDto dto)
        => OkOrBadRequest(await _service.CheckOutAsync(dto, GetCurrentUserId()));

    [HttpPost("{id:guid}/return")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Return(Guid id, [FromBody] ReturnPhysicalItemDto dto)
        => OkOrBadRequest(await _service.ReturnAsync(id, dto, GetCurrentUserId()));

    [HttpPost("{id:guid}/renew")]
    public async Task<IActionResult> Renew(Guid id)
        => OkOrBadRequest(await _service.RenewAsync(id, GetCurrentUserId()));

    [HttpPost("{id:guid}/report-lost")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> ReportLost(Guid id)
        => OkOrBadRequest(await _service.ReportLostAsync(id, GetCurrentUserId()));
}
