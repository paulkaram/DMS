using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class PhysicalLocationsController : BaseApiController
{
    private readonly IPhysicalLocationService _service;

    public PhysicalLocationsController(IPhysicalLocationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => OkOrBadRequest(await _service.GetTreeAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) => OkOrNotFound(await _service.GetByIdAsync(id));

    [HttpGet("{id:guid}/children")]
    public async Task<IActionResult> GetChildren(Guid id) => OkOrBadRequest(await _service.GetChildrenAsync(id));

    [HttpPost]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Create([FromBody] CreatePhysicalLocationDto dto)
        => OkOrBadRequest(await _service.CreateAsync(dto, GetCurrentUserId()));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePhysicalLocationDto dto)
        => OkOrBadRequest(await _service.UpdateAsync(id, dto, GetCurrentUserId()));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid id)
        => OkOrBadRequest(await _service.DeleteAsync(id, GetCurrentUserId()));
}
