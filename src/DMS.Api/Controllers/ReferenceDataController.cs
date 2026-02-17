using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Route("api")]
[Authorize]
public class ReferenceDataController : BaseApiController
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

    [HttpGet("classifications/tree")]
    public async Task<IActionResult> GetClassificationTree([FromQuery] string? language)
    {
        var result = await _referenceDataService.GetClassificationTreeAsync(language);
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
    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups()
    {
        var result = await _referenceDataService.GetLookupsAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("lookups/{id:guid}")]
    public async Task<IActionResult> GetLookupById(Guid id)
    {
        var result = await _referenceDataService.GetLookupByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpGet("lookups/by-name/{name}")]
    public async Task<IActionResult> GetLookupItems(string name, [FromQuery] string? language)
    {
        var result = await _referenceDataService.GetLookupItemsAsync(name, language);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("lookups")]
    public async Task<IActionResult> CreateLookup([FromBody] DMS.BL.DTOs.LookupDto dto)
    {
        var result = await _referenceDataService.CreateLookupAsync(dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("lookups/{id:guid}")]
    public async Task<IActionResult> UpdateLookup(Guid id, [FromBody] DMS.BL.DTOs.LookupDto dto)
    {
        var result = await _referenceDataService.UpdateLookupAsync(id, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("lookups/{id:guid}")]
    public async Task<IActionResult> DeleteLookup(Guid id)
    {
        var result = await _referenceDataService.DeleteLookupAsync(id);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    // Lookup Items
    [HttpGet("lookups/{lookupId:guid}/items")]
    public async Task<IActionResult> GetLookupItemsById(Guid lookupId, [FromQuery] string? language)
    {
        var result = await _referenceDataService.GetLookupItemsByIdAsync(lookupId, language);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost("lookups/{lookupId:guid}/items")]
    public async Task<IActionResult> CreateLookupItem(Guid lookupId, [FromBody] DMS.BL.DTOs.LookupItemDto dto)
    {
        var result = await _referenceDataService.CreateLookupItemAsync(lookupId, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("lookups/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateLookupItem(Guid itemId, [FromBody] DMS.BL.DTOs.LookupItemDto dto)
    {
        var result = await _referenceDataService.UpdateLookupItemAsync(itemId, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("lookups/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteLookupItem(Guid itemId)
    {
        var result = await _referenceDataService.DeleteLookupItemAsync(itemId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    // Privacy Levels
    [HttpGet("privacy-levels")]
    public async Task<IActionResult> GetPrivacyLevels([FromQuery] bool includeInactive = false)
    {
        var result = await _referenceDataService.GetPrivacyLevelsAsync(includeInactive);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("privacy-levels/{id:guid}")]
    public async Task<IActionResult> GetPrivacyLevel(Guid id)
    {
        var result = await _referenceDataService.GetPrivacyLevelByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost("privacy-levels")]
    public async Task<IActionResult> CreatePrivacyLevel([FromBody] PrivacyLevelDto dto)
    {
        var result = await _referenceDataService.CreatePrivacyLevelAsync(dto);
        return result.Success ? CreatedAtAction(nameof(GetPrivacyLevel), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    [HttpPut("privacy-levels/{id:guid}")]
    public async Task<IActionResult> UpdatePrivacyLevel(Guid id, [FromBody] PrivacyLevelDto dto)
    {
        var result = await _referenceDataService.UpdatePrivacyLevelAsync(id, dto);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpDelete("privacy-levels/{id:guid}")]
    public async Task<IActionResult> DeletePrivacyLevel(Guid id)
    {
        var result = await _referenceDataService.DeletePrivacyLevelAsync(id);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }
}
