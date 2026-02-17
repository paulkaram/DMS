using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize(Roles = "Administrator,RecordsManager")]
public class PreservationController : BaseApiController
{
    private readonly IPreservationService _service;

    public PreservationController(IPreservationService service) => _service = service;

    [HttpGet("documents/{documentId:guid}")]
    public async Task<IActionResult> GetDocumentPreservation(Guid documentId)
        => OkOrNotFound(await _service.GetDocumentPreservationAsync(documentId));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => OkOrBadRequest(await _service.GetPreservationSummaryAsync());

    [HttpGet("formats")]
    public async Task<IActionResult> GetApprovedFormats()
        => OkOrBadRequest(await _service.GetApprovedFormatsAsync());
}
