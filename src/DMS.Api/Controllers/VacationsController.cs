using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VacationsController : ControllerBase
{
    private readonly IVacationService _vacationService;

    public VacationsController(IVacationService vacationService)
    {
        _vacationService = vacationService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VacationDto>>> GetMyVacations()
    {
        var vacations = await _vacationService.GetUserVacationsAsync(GetUserId());
        return Ok(vacations);
    }

    [HttpGet("active")]
    public async Task<ActionResult<VacationDto>> GetActiveVacation()
    {
        var vacation = await _vacationService.GetActiveVacationAsync(GetUserId());
        return Ok(vacation);
    }

    [HttpGet("all-active")]
    public async Task<ActionResult<IEnumerable<VacationDto>>> GetAllActive()
    {
        var vacations = await _vacationService.GetAllActiveAsync();
        return Ok(vacations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VacationDto>> GetById(Guid id)
    {
        var vacation = await _vacationService.GetByIdAsync(id);
        if (vacation == null) return NotFound();
        return Ok(vacation);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateVacationRequest request)
    {
        var id = await _vacationService.CreateAsync(GetUserId(), request);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateVacationRequest request)
    {
        var result = await _vacationService.UpdateAsync(id, request);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _vacationService.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult> Cancel(Guid id)
    {
        var result = await _vacationService.CancelAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
