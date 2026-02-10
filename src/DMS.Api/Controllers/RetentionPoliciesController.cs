using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Route("api/retention-policies")]
[Authorize]
public class RetentionPoliciesController : BaseApiController
{
    private readonly IRetentionPolicyRepository _repository;

    public RetentionPoliciesController(IRetentionPolicyRepository repository)
    {
        _repository = repository;
    }

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
            CreatedBy = GetCurrentUserId()
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
        policy.ModifiedBy = GetCurrentUserId();

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
    public async Task<ActionResult<PagedResultDto<DocumentRetention>>> GetExpiringDocuments(
        [FromQuery] int daysAhead = 30,
        [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var (items, totalCount) = await _repository.GetExpiringDocumentsPaginatedAsync(daysAhead, page, pageSize);
        return Ok(new PagedResultDto<DocumentRetention>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpGet("pending-review")]
    public async Task<ActionResult<PagedResultDto<DocumentRetention>>> GetPendingReview(
        [FromQuery] int page = 1, [FromQuery] int pageSize = AppConstants.DefaultPageSize)
    {
        pageSize = Math.Min(pageSize, AppConstants.MaxPageSize);
        var (items, totalCount) = await _repository.GetPendingReviewPaginatedAsync(page, pageSize);
        return Ok(new PagedResultDto<DocumentRetention>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    [HttpPost("documents/{documentId}/apply/{policyId}")]
    public async Task<ActionResult> ApplyPolicyToDocument(Guid documentId, Guid policyId)
    {
        var result = await _repository.ApplyPolicyToDocumentAsync(documentId, policyId, GetCurrentUserId());
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpPost("retentions/{retentionId}/approve")]
    public async Task<ActionResult> ApproveRetentionAction(Guid retentionId, [FromBody] ApproveRetentionRequest request)
    {
        var result = await _repository.ApproveRetentionActionAsync(retentionId, GetCurrentUserId(), request.Notes);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpPost("documents/{documentId}/hold")]
    public async Task<ActionResult> PlaceOnHold(Guid documentId, [FromBody] HoldRequest request)
    {
        var result = await _repository.PlaceOnHoldAsync(documentId, GetCurrentUserId(), request.Notes);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpPost("documents/{documentId}/release-hold")]
    public async Task<ActionResult> ReleaseHold(Guid documentId)
    {
        var result = await _repository.ReleaseHoldAsync(documentId, GetCurrentUserId());
        if (!result) return BadRequest();
        return Ok();
    }

    #endregion
}
