using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/documents/{documentId}/comments")]
[Authorize]
public class DocumentCommentsController : ControllerBase
{
    private readonly IDocumentCommentService _commentService;

    public DocumentCommentsController(IDocumentCommentService commentService)
    {
        _commentService = commentService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetComments(Guid documentId)
    {
        var comments = await _commentService.GetByDocumentIdAsync(documentId);
        return Ok(comments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentCommentDto>> GetComment(Guid documentId, Guid id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null) return NotFound();
        return Ok(comment);
    }

    [HttpGet("{id}/replies")]
    public async Task<ActionResult<IEnumerable<DocumentCommentDto>>> GetReplies(Guid documentId, Guid id)
    {
        var replies = await _commentService.GetRepliesAsync(id);
        return Ok(replies);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCommentCount(Guid documentId)
    {
        var count = await _commentService.GetCommentCountAsync(documentId);
        return Ok(count);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentCommentDto>> AddComment(Guid documentId, [FromBody] CreateCommentRequest request)
    {
        request.DocumentId = documentId;
        var comment = await _commentService.AddAsync(request, GetUserId());
        return CreatedAtAction(nameof(GetComment), new { documentId, id = comment.Id }, comment);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateComment(Guid documentId, Guid id, [FromBody] UpdateCommentRequest request)
    {
        var result = await _commentService.UpdateAsync(id, request, GetUserId());
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteComment(Guid documentId, Guid id)
    {
        var result = await _commentService.DeleteAsync(id, GetUserId());
        if (!result) return NotFound();
        return NoContent();
    }
}
