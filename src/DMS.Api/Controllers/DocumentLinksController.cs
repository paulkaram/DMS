using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Route("api/documents/{documentId}/links")]
[Authorize]
public class DocumentLinksController : BaseApiController
{
    private readonly IDocumentLinkService _linkService;

    public DocumentLinksController(IDocumentLinkService linkService)
    {
        _linkService = linkService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentLinkDto>>> GetLinks(Guid documentId)
    {
        var links = await _linkService.GetByDocumentIdAsync(documentId);
        return Ok(links);
    }

    [HttpGet("incoming")]
    public async Task<ActionResult<IEnumerable<DocumentLinkDto>>> GetIncomingLinks(Guid documentId)
    {
        var links = await _linkService.GetIncomingLinksAsync(documentId);
        return Ok(links);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentLinkDto>> GetLink(Guid documentId, Guid id)
    {
        var link = await _linkService.GetByIdAsync(id);
        if (link == null) return NotFound();
        return Ok(link);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetLinkCount(Guid documentId)
    {
        var count = await _linkService.GetLinkCountAsync(documentId);
        return Ok(count);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentLinkDto>> CreateLink(Guid documentId, [FromBody] CreateLinkRequest request)
    {
        request.SourceDocumentId = documentId;

        try
        {
            var link = await _linkService.AddAsync(request, GetCurrentUserId());
            return CreatedAtAction(nameof(GetLink), new { documentId, id = link.Id }, link);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateLink(Guid documentId, Guid id, [FromBody] UpdateLinkRequest request)
    {
        var result = await _linkService.UpdateAsync(id, request);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteLink(Guid documentId, Guid id)
    {
        var result = await _linkService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
