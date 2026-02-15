using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class ApprovalsController : BaseApiController
{
    private readonly IApprovalService _approvalService;

    public ApprovalsController(IApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    // Workflows
    [HttpGet("workflows")]
    public async Task<ActionResult<IEnumerable<ApprovalWorkflowDto>>> GetWorkflows()
    {
        var workflows = await _approvalService.GetWorkflowsAsync();
        return Ok(workflows);
    }

    [HttpGet("workflows/{id}")]
    public async Task<ActionResult<ApprovalWorkflowDto>> GetWorkflow(Guid id)
    {
        var workflow = await _approvalService.GetWorkflowByIdAsync(id);
        if (workflow == null) return NotFound();
        return Ok(workflow);
    }

    [HttpPost("workflows")]
    public async Task<ActionResult<Guid>> CreateWorkflow([FromBody] CreateWorkflowRequest request)
    {
        var id = await _approvalService.CreateWorkflowAsync(GetCurrentUserId(), request);
        return Ok(id);
    }

    [HttpPut("workflows/{id}")]
    public async Task<ActionResult> UpdateWorkflow(Guid id, [FromBody] CreateWorkflowRequest request)
    {
        var result = await _approvalService.UpdateWorkflowAsync(id, request);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("workflows/{id}")]
    public async Task<ActionResult> DeleteWorkflow(Guid id)
    {
        var result = await _approvalService.DeleteWorkflowAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    // Requests
    [HttpGet("requests/pending")]
    public async Task<ActionResult<IEnumerable<ApprovalRequestDto>>> GetPendingRequests([FromQuery] int? take = null)
    {
        var requests = await _approvalService.GetPendingRequestsForUserAsync(GetCurrentUserId());
        return Ok(take.HasValue ? requests.Take(take.Value) : requests);
    }

    [HttpGet("requests/my")]
    public async Task<ActionResult<IEnumerable<ApprovalRequestDto>>> GetMyRequests()
    {
        var requests = await _approvalService.GetMyRequestsAsync(GetCurrentUserId());
        return Ok(requests);
    }

    [HttpGet("requests/document/{documentId}")]
    public async Task<ActionResult<IEnumerable<ApprovalRequestDto>>> GetDocumentRequests(Guid documentId)
    {
        var requests = await _approvalService.GetDocumentRequestsAsync(documentId);
        return Ok(requests);
    }

    [HttpGet("requests/{id}")]
    public async Task<ActionResult<ApprovalRequestDto>> GetRequest(Guid id)
    {
        var request = await _approvalService.GetRequestByIdAsync(id);
        if (request == null) return NotFound();
        return Ok(request);
    }

    [HttpPost("requests")]
    public async Task<ActionResult<Guid>> CreateRequest([FromBody] CreateApprovalRequestDto request)
    {
        var id = await _approvalService.CreateRequestAsync(GetCurrentUserId(), request);
        return Ok(id);
    }

    [HttpPost("requests/{id}/action")]
    public async Task<ActionResult> SubmitAction(Guid id, [FromBody] SubmitApprovalActionDto action)
    {
        var result = await _approvalService.SubmitActionAsync(id, GetCurrentUserId(), action);
        if (!result) return BadRequest(ErrorMessages.UnableToSubmitAction);
        return Ok();
    }

    [HttpPost("requests/{id}/cancel")]
    public async Task<ActionResult> CancelRequest(Guid id)
    {
        var result = await _approvalService.CancelRequestAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPost("requests/{id}/resubmit")]
    public async Task<ActionResult> ResubmitRequest(Guid id)
    {
        var result = await _approvalService.ResubmitRequestAsync(id, GetCurrentUserId());
        if (!result) return BadRequest("Unable to resubmit request");
        return Ok();
    }
}
