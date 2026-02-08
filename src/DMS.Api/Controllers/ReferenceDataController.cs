using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ReferenceDataController : ControllerBase
{
    private readonly IReferenceDataService _referenceDataService;

    public ReferenceDataController(IReferenceDataService referenceDataService)
    {
        _referenceDataService = referenceDataService;
    }

    // Classifications
    [HttpGet("classifications")]
    public async Task<IActionResult> GetClassifications([FromQuery] string? language)
    {
        var result = await _referenceDataService.GetClassificationsAsync(language);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("classifications/{id:guid}")]
    public async Task<IActionResult> GetClassification(Guid id)
    {
        var result = await _referenceDataService.GetClassificationByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost("classifications")]
    public async Task<IActionResult> CreateClassification([FromBody] ClassificationDto dto)
    {
        var result = await _referenceDataService.CreateClassificationAsync(dto);
        return result.Success ? CreatedAtAction(nameof(GetClassification), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("classifications/{id:guid}")]
    public async Task<IActionResult> UpdateClassification(Guid id, [FromBody] ClassificationDto dto)
    {
        var result = await _referenceDataService.UpdateClassificationAsync(id, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("classifications/{id:guid}")]
    public async Task<IActionResult> DeleteClassification(Guid id)
    {
        var result = await _referenceDataService.DeleteClassificationAsync(id);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    // Importances
    [HttpGet("importances")]
    public async Task<IActionResult> GetImportances([FromQuery] string? language)
    {
        var result = await _referenceDataService.GetImportancesAsync(language);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("importances/{id:guid}")]
    public async Task<IActionResult> GetImportance(Guid id)
    {
        var result = await _referenceDataService.GetImportanceByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost("importances")]
    public async Task<IActionResult> CreateImportance([FromBody] ImportanceDto dto)
    {
        var result = await _referenceDataService.CreateImportanceAsync(dto);
        return result.Success ? CreatedAtAction(nameof(GetImportance), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("importances/{id:guid}")]
    public async Task<IActionResult> UpdateImportance(Guid id, [FromBody] ImportanceDto dto)
    {
        var result = await _referenceDataService.UpdateImportanceAsync(id, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("importances/{id:guid}")]
    public async Task<IActionResult> DeleteImportance(Guid id)
    {
        var result = await _referenceDataService.DeleteImportanceAsync(id);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    // Document Types
    [HttpGet("document-types")]
    public async Task<IActionResult> GetDocumentTypes([FromQuery] string? language)
    {
        var result = await _referenceDataService.GetDocumentTypesAsync(language);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("document-types/{id:guid}")]
    public async Task<IActionResult> GetDocumentType(Guid id)
    {
        var result = await _referenceDataService.GetDocumentTypeByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost("document-types")]
    public async Task<IActionResult> CreateDocumentType([FromBody] DocumentTypeDto dto)
    {
        var result = await _referenceDataService.CreateDocumentTypeAsync(dto);
        return result.Success ? CreatedAtAction(nameof(GetDocumentType), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("document-types/{id:guid}")]
    public async Task<IActionResult> UpdateDocumentType(Guid id, [FromBody] DocumentTypeDto dto)
    {
        var result = await _referenceDataService.UpdateDocumentTypeAsync(id, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("document-types/{id:guid}")]
    public async Task<IActionResult> DeleteDocumentType(Guid id)
    {
        var result = await _referenceDataService.DeleteDocumentTypeAsync(id);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    // Lookups
    [HttpGet("lookups/{name}")]
    public async Task<IActionResult> GetLookupItems(string name, [FromQuery] string? language)
    {
        var result = await _referenceDataService.GetLookupItemsAsync(name, language);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
