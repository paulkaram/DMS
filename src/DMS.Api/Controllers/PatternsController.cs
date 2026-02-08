using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/patterns")]
[Authorize]
public class PatternsController : ControllerBase
{
    private readonly IPatternRepository _repository;

    public PatternsController(IPatternRepository repository)
    {
        _repository = repository;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Pattern>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var patterns = await _repository.GetAllAsync(includeInactive);
        return Ok(patterns);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Pattern>> GetById(Guid id)
    {
        var pattern = await _repository.GetByIdAsync(id);
        if (pattern == null) return NotFound();
        return Ok(pattern);
    }

    [HttpGet("type/{patternType}")]
    public async Task<ActionResult<IEnumerable<Pattern>>> GetByType(string patternType)
    {
        var patterns = await _repository.GetByTypeAsync(patternType);
        return Ok(patterns);
    }

    [HttpGet("folder/{folderId}")]
    public async Task<ActionResult<IEnumerable<Pattern>>> GetByFolder(Guid folderId)
    {
        var patterns = await _repository.GetByFolderAsync(folderId);
        return Ok(patterns);
    }

    [HttpPost("match")]
    public async Task<ActionResult<Pattern>> FindMatch([FromBody] FindMatchRequest request)
    {
        var pattern = await _repository.FindMatchingPatternAsync(request.Value, request.PatternType);
        if (pattern == null) return NotFound();
        return Ok(pattern);
    }

    [HttpPost("test")]
    public async Task<ActionResult<TestPatternResult>> TestPattern([FromBody] TestPatternRequest request)
    {
        var matches = await _repository.TestPatternAsync(request.Regex, request.TestValue);
        return Ok(new TestPatternResult { Matches = matches });
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreatePatternRequest request)
    {
        var pattern = new Pattern
        {
            Name = request.Name,
            Regex = request.Regex,
            Description = request.Description,
            PatternType = request.PatternType,
            TargetFolderId = request.TargetFolderId,
            ContentTypeId = request.ContentTypeId,
            ClassificationId = request.ClassificationId,
            DocumentTypeId = request.DocumentTypeId,
            Priority = request.Priority,
            CreatedBy = GetUserId()
        };

        var id = await _repository.CreateAsync(pattern);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdatePatternRequest request)
    {
        var pattern = await _repository.GetByIdAsync(id);
        if (pattern == null) return NotFound();

        pattern.Name = request.Name;
        pattern.Regex = request.Regex;
        pattern.Description = request.Description;
        pattern.PatternType = request.PatternType;
        pattern.TargetFolderId = request.TargetFolderId;
        pattern.ContentTypeId = request.ContentTypeId;
        pattern.ClassificationId = request.ClassificationId;
        pattern.DocumentTypeId = request.DocumentTypeId;
        pattern.Priority = request.Priority;
        pattern.IsActive = request.IsActive;
        pattern.ModifiedBy = GetUserId();

        var result = await _repository.UpdateAsync(pattern);
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
}

#region Request DTOs

public class CreatePatternRequest
{
    public string Name { get; set; } = string.Empty;
    public string Regex { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PatternType { get; set; } = "Naming";
    public Guid? TargetFolderId { get; set; }
    public Guid? ContentTypeId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public int Priority { get; set; } = 100;
}

public class UpdatePatternRequest : CreatePatternRequest
{
    public bool IsActive { get; set; } = true;
}

public class FindMatchRequest
{
    public string Value { get; set; } = string.Empty;
    public string? PatternType { get; set; }
}

public class TestPatternRequest
{
    public string Regex { get; set; } = string.Empty;
    public string TestValue { get; set; } = string.Empty;
}

public class TestPatternResult
{
    public bool Matches { get; set; }
}

#endregion
