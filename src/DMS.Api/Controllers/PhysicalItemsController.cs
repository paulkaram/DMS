using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class PhysicalItemsController : BaseApiController
{
    private readonly IPhysicalItemService _service;
    private readonly ICustodyService _custodyService;

    public PhysicalItemsController(IPhysicalItemService service, ICustodyService custodyService)
    {
        _service = service;
        _custodyService = custodyService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] Guid? locationId,
        [FromQuery] string? itemType, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => OkOrBadRequest(await _service.SearchAsync(query, locationId, itemType, page, pageSize));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) => OkOrNotFound(await _service.GetByIdAsync(id));

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode) => OkOrNotFound(await _service.GetByBarcodeAsync(barcode));

    [HttpPost]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Create([FromBody] CreatePhysicalItemDto dto)
        => OkOrBadRequest(await _service.CreateAsync(dto, GetCurrentUserId()));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreatePhysicalItemDto dto)
        => OkOrBadRequest(await _service.UpdateAsync(id, dto, GetCurrentUserId()));

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid id)
        => OkOrBadRequest(await _service.DeleteAsync(id, GetCurrentUserId()));

    [HttpPost("{id:guid}/move")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> Move(Guid id, [FromBody] MovePhysicalItemDto dto)
        => OkOrBadRequest(await _service.MoveAsync(id, dto, GetCurrentUserId()));

    [HttpPut("{id:guid}/condition")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> UpdateCondition(Guid id, [FromBody] UpdateConditionDto dto)
        => OkOrBadRequest(await _service.UpdateConditionAsync(id, dto, GetCurrentUserId()));

    [HttpGet("{id:guid}/custody")]
    public async Task<IActionResult> GetCustodyChain(Guid id)
        => OkOrBadRequest(await _custodyService.GetChainOfCustodyAsync(id));

    [HttpPost("{id:guid}/custody/transfer")]
    [Authorize(Roles = "Administrator,Records")]
    public async Task<IActionResult> TransferCustody(Guid id, [FromBody] CreateCustodyTransferDto dto)
        => OkOrBadRequest(await _custodyService.RecordTransferAsync(id, dto, GetCurrentUserId()));

    [HttpPost("{id:guid}/custody/{transferId:guid}/acknowledge")]
    public async Task<IActionResult> AcknowledgeCustody(Guid id, Guid transferId)
        => OkOrBadRequest(await _custodyService.AcknowledgeTransferAsync(transferId, GetCurrentUserId()));
}
