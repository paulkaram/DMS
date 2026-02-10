using DMS.BL.DTOs;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class FilingPlansController : BaseApiController
{
    private readonly IFilingPlanRepository _filingPlanRepository;

    public FilingPlansController(IFilingPlanRepository filingPlanRepository)
    {
        _filingPlanRepository = filingPlanRepository;
    }

    [HttpGet("folder/{folderId}")]
    public async Task<ActionResult<IEnumerable<FilingPlan>>> GetByFolder(Guid folderId)
    {
        var plans = await _filingPlanRepository.GetByFolderAsync(folderId);
        return Ok(plans);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FilingPlan>> GetById(Guid id)
    {
        var plan = await _filingPlanRepository.GetByIdAsync(id);
        if (plan == null) return NotFound();
        return Ok(plan);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateFilingPlanRequest request)
    {
        var plan = new FilingPlan
        {
            FolderId = request.FolderId,
            Name = request.Name,
            Description = request.Description,
            Pattern = request.Pattern,
            ClassificationId = request.ClassificationId,
            DocumentTypeId = request.DocumentTypeId,
            IsActive = true,
            CreatedBy = GetCurrentUserId()
        };

        var id = await _filingPlanRepository.CreateAsync(plan);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateFilingPlanRequest request)
    {
        var plan = await _filingPlanRepository.GetByIdAsync(id);
        if (plan == null) return NotFound();

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.Pattern = request.Pattern;
        plan.ClassificationId = request.ClassificationId;
        plan.DocumentTypeId = request.DocumentTypeId;
        plan.IsActive = request.IsActive;

        var result = await _filingPlanRepository.UpdateAsync(plan);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _filingPlanRepository.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
