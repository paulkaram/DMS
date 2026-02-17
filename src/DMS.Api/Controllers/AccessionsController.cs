using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class AccessionsController : BaseApiController
{
    private readonly IAccessionService _service;

    public AccessionsController(IAccessionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => OkOrBadRequest(await _service.GetPaginatedAsync(status, page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) => OkOrNotFound(await _service.GetByIdAsync(id));

    [HttpPost]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Create([FromBody] CreateAccessionRequestDto dto)
        => OkOrBadRequest(await _service.CreateRequestAsync(dto, GetCurrentUserId()));

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id) => OkOrBadRequest(await _service.SubmitAsync(id, GetCurrentUserId()));

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewAccessionDto dto)
        => OkOrBadRequest(await _service.ReviewAsync(id, dto, GetCurrentUserId()));

    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] ReviewAccessionDto dto)
        => OkOrBadRequest(await _service.AcceptAsync(id, dto, GetCurrentUserId()));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewAccessionDto dto)
        => OkOrBadRequest(await _service.RejectAsync(id, dto.Notes ?? "Rejected", GetCurrentUserId()));

    [HttpPost("{id:guid}/transfer")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Transfer(Guid id) => OkOrBadRequest(await _service.ExecuteTransferAsync(id, GetCurrentUserId()));

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddAccessionItemDto dto)
        => OkOrBadRequest(await _service.AddItemAsync(id, dto));

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
        => OkOrBadRequest(await _service.RemoveItemAsync(id, itemId));
}
