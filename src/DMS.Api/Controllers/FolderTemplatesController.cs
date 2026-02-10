using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Route("api/folder-templates")]
[Authorize]
public class FolderTemplatesController : BaseApiController
{
    private readonly IFolderTemplateService _templateService;

    public FolderTemplatesController(IFolderTemplateService templateService)
    {
        _templateService = templateService;
    }

    /// <summary>
    /// Get all folder templates
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<FolderTemplateDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        // Only admins can see inactive templates
        if (includeInactive && !IsAdmin())
            includeInactive = false;

        var result = await _templateService.GetAllAsync(includeInactive);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get template by ID with all nodes
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FolderTemplateDto>> GetById(Guid id)
    {
        var result = await _templateService.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get the default template
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<FolderTemplateDto>> GetDefault()
    {
        var result = await _templateService.GetDefaultAsync();
        if (!result.Success)
            return NotFound(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get templates by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<List<FolderTemplateDto>>> GetByCategory(string category)
    {
        var result = await _templateService.GetByCategoryAsync(category);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all available categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var result = await _templateService.GetCategoriesAsync();
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new folder template (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<FolderTemplateDto>> Create([FromBody] CreateFolderTemplateDto dto)
    {
        var result = await _templateService.CreateAsync(dto, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Update a folder template (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<FolderTemplateDto>> Update(Guid id, [FromBody] UpdateFolderTemplateDto dto)
    {
        var result = await _templateService.UpdateAsync(id, dto, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a folder template (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _templateService.DeleteAsync(id);
        if (!result.Success)
            return BadRequest(result.Errors);

        return NoContent();
    }

    /// <summary>
    /// Duplicate a template (Admin only)
    /// </summary>
    [HttpPost("{id:guid}/duplicate")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<FolderTemplateDto>> Duplicate(Guid id, [FromBody] DuplicateTemplateRequest request)
    {
        var result = await _templateService.DuplicateAsync(id, request.NewName, GetCurrentUserId());
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Get usage history for a template
    /// </summary>
    [HttpGet("{id:guid}/usage")]
    public async Task<ActionResult<List<FolderTemplateUsageDto>>> GetUsageHistory(Guid id)
    {
        var result = await _templateService.GetUsageHistoryAsync(id);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    #region Node Management

    /// <summary>
    /// Add a node to a template (Admin only)
    /// </summary>
    [HttpPost("{templateId:guid}/nodes")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<FolderTemplateNodeDto>> AddNode(Guid templateId, [FromBody] CreateTemplateNodeDto dto)
    {
        var result = await _templateService.AddNodeAsync(templateId, dto);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Update a node (Admin only)
    /// </summary>
    [HttpPut("nodes/{nodeId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<FolderTemplateNodeDto>> UpdateNode(Guid nodeId, [FromBody] UpdateTemplateNodeDto dto)
    {
        var result = await _templateService.UpdateNodeAsync(nodeId, dto);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a node (Admin only)
    /// </summary>
    [HttpDelete("nodes/{nodeId:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> DeleteNode(Guid nodeId)
    {
        var result = await _templateService.DeleteNodeAsync(nodeId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return NoContent();
    }

    #endregion
}
