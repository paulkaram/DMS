using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/retention-policies")]
[Authorize]
public class RetentionPoliciesController : ControllerBase
{
    private readonly IRetentionPolicyRepository _repository;

    public RetentionPoliciesController(IRetentionPolicyRepository repository)
    {
        _repository = repository;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    #region Retention Policies

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RetentionPolicy>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var policies = await _repository.GetAllAsync(includeInactive);
        return Ok(policies);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RetentionPolicy>> GetById(Guid id)
    {
        var policy = await _repository.GetByIdAsync(id);
        if (policy == null) return NotFound();
        return Ok(policy);
    }

    [HttpGet("folder/{folderId}")]
    public async Task<ActionResult<IEnumerable<RetentionPolicy>>> GetByFolder(Guid folderId)
    {
        var policies = await _repository.GetByFolderAsync(folderId);
        return Ok(policies);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateRetentionPolicyRequest request)
    {
        var policy = new RetentionPolicy
        {
            Name = request.Name,
            Description = request.Description,
            RetentionDays = request.RetentionDays,
            ExpirationAction = request.ExpirationAction,
            NotifyBeforeExpiration = request.NotifyBeforeExpiration,
            NotificationDays = request.NotificationDays,
            FolderId = request.FolderId,
            ClassificationId = request.ClassificationId,
            DocumentTypeId = request.DocumentTypeId,
            RequiresApproval = request.RequiresApproval,
            InheritToSubfolders = request.InheritToSubfolders,
            IsLegalHold = request.IsLegalHold,
            CreatedBy = GetUserId()
        };

        var id = await _repository.CreateAsync(policy);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateRetentionPolicyRequest request)
    {
        var policy = await _repository.GetByIdAsync(id);
        if (policy == null) return NotFound();

        policy.Name = request.Name;
        policy.Description = request.Description;
        policy.RetentionDays = request.RetentionDays;
        policy.ExpirationAction = request.ExpirationAction;
        policy.NotifyBeforeExpiration = request.NotifyBeforeExpiration;
        policy.NotificationDays = request.NotificationDays;
        policy.FolderId = request.FolderId;
        policy.ClassificationId = request.ClassificationId;
        policy.DocumentTypeId = request.DocumentTypeId;
        policy.RequiresApproval = request.RequiresApproval;
        policy.InheritToSubfolders = request.InheritToSubfolders;
        policy.IsLegalHold = request.IsLegalHold;
        policy.IsActive = request.IsActive;
        policy.ModifiedBy = GetUserId();

        var result = await _repository.UpdateAsync(policy);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _repository.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    #endregion

    #region Document Retention

    [HttpGet("documents/{documentId}/retention")]
    public async Task<ActionResult<IEnumerable<DocumentRetention>>> GetDocumentRetentions(Guid documentId)
    {
        var retentions = await _repository.GetDocumentRetentionsAsync(documentId);
        return Ok(retentions);
    }

    [HttpGet("expiring")]
    public async Task<ActionResult<IEnumerable<DocumentRetention>>> GetExpiringDocuments([FromQuery] int daysAhead = 30)
    {
        var retentions = await _repository.GetExpiringDocumentsAsync(daysAhead);
        return Ok(retentions);
    }

    [HttpGet("pending-review")]
    public async Task<ActionResult<IEnumerable<DocumentRetention>>> GetPendingReview()
    {
        var retentions = await _repository.GetPendingReviewAsync();
        return Ok(retentions);
    }

    [HttpPost("documents/{documentId}/apply/{policyId}")]
    public async Task<ActionResult> ApplyPolicyToDocument(Guid documentId, Guid policyId)
    {
        var result = await _repository.ApplyPolicyToDocumentAsync(documentId, policyId, GetUserId());
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpPost("retentions/{retentionId}/approve")]
    public async Task<ActionResult> ApproveRetentionAction(Guid retentionId, [FromBody] ApproveRetentionRequest request)
    {
        var result = await _repository.ApproveRetentionActionAsync(retentionId, GetUserId(), request.Notes);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpPost("documents/{documentId}/hold")]
    public async Task<ActionResult> PlaceOnHold(Guid documentId, [FromBody] HoldRequest request)
    {
        var result = await _repository.PlaceOnHoldAsync(documentId, GetUserId(), request.Notes);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpPost("documents/{documentId}/release-hold")]
    public async Task<ActionResult> ReleaseHold(Guid documentId)
    {
        var result = await _repository.ReleaseHoldAsync(documentId, GetUserId());
        if (!result) return BadRequest();
        return Ok();
    }

    #endregion
}

#region Request DTOs

public class CreateRetentionPolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int RetentionDays { get; set; }
    public string ExpirationAction { get; set; } = "Review";
    public bool NotifyBeforeExpiration { get; set; } = true;
    public int NotificationDays { get; set; } = 30;
    public Guid? FolderId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool InheritToSubfolders { get; set; } = true;
    public bool IsLegalHold { get; set; } = false;
}

public class UpdateRetentionPolicyRequest : CreateRetentionPolicyRequest
{
    public bool IsActive { get; set; } = true;
}

public class ApproveRetentionRequest
{
    public string? Notes { get; set; }
}

public class HoldRequest
{
    public string? Notes { get; set; }
}

#endregion
