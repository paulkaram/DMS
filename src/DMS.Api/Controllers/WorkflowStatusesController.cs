using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
[Route("api/workflow-statuses")]
public class WorkflowStatusesController : BaseApiController
{
    private readonly IWorkflowStatusService _service;

    public WorkflowStatusesController(IWorkflowStatusService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _service.GetAllAsync(includeInactive);
        return OkOrBadRequest(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return OkOrNotFound(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowStatusRequest request)
    {
        var result = await _service.CreateAsync(request);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(result.Errors);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkflowStatusRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return OkOrBadRequest(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return OkOrBadRequest(result);
    }
}
