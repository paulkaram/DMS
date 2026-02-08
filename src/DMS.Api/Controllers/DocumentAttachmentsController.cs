using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/documents/{documentId}/attachments")]
[Authorize]
public class DocumentAttachmentsController : ControllerBase
{
    private readonly IDocumentAttachmentService _attachmentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileValidationService _fileValidationService;

    public DocumentAttachmentsController(
        IDocumentAttachmentService attachmentService,
        IFileStorageService fileStorageService,
        IFileValidationService fileValidationService)
    {
        _attachmentService = attachmentService;
        _fileStorageService = fileStorageService;
        _fileValidationService = fileValidationService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentAttachmentDto>>> GetAttachments(Guid documentId)
    {
        var attachments = await _attachmentService.GetByDocumentIdAsync(documentId);
        return Ok(attachments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentAttachmentDto>> GetAttachment(Guid documentId, Guid id)
    {
        var attachment = await _attachmentService.GetByIdAsync(id);
        if (attachment == null) return NotFound();
        return Ok(attachment);
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetAttachmentCount(Guid documentId)
    {
        var count = await _attachmentService.GetAttachmentCountAsync(documentId);
        return Ok(count);
    }

    [HttpGet("{id}/download")]
    public async Task<ActionResult> DownloadAttachment(Guid documentId, Guid id)
    {
        var result = await _attachmentService.DownloadAsync(id);
        if (result == null) return NotFound();

        return File(result.Value.stream, result.Value.contentType, result.Value.fileName);
    }

    [HttpPost]
    public async Task<ActionResult<DocumentAttachmentDto>> UploadAttachment(
        Guid documentId,
        [FromForm] IFormFile file,
        [FromForm] string? description = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        // Validate file before processing
        using var fileStream = file.OpenReadStream();
        var validationResult = await _fileValidationService.ValidateFileAsync(fileStream, file.FileName, file.ContentType);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Error);

        // Reset stream position after validation
        fileStream.Position = 0;

        // Use validated content type
        var validatedContentType = validationResult.ValidatedMimeType ?? file.ContentType;

        // Save file to storage (using version 0 for attachments)
        var storagePath = await _fileStorageService.SaveFileAsync(fileStream, file.FileName, documentId, 0);

        // Create attachment record
        var attachment = await _attachmentService.AddAsync(
            documentId,
            file.FileName,
            description,
            validatedContentType,
            file.Length,
            storagePath,
            GetUserId());

        return CreatedAtAction(nameof(GetAttachment), new { documentId, id = attachment.Id }, attachment);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAttachment(Guid documentId, Guid id)
    {
        var result = await _attachmentService.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}
