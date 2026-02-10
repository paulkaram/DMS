using DMS.BL.DTOs;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class ContentTypesController : BaseApiController
{
    private readonly IContentTypeRepository _contentTypeRepository;

    public ContentTypesController(IContentTypeRepository contentTypeRepository)
    {
        _contentTypeRepository = contentTypeRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentType>>> GetAll()
    {
        var contentTypes = await _contentTypeRepository.GetAllAsync();
        return Ok(contentTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContentType>> GetById(Guid id)
    {
        var contentType = await _contentTypeRepository.GetByIdAsync(id);
        if (contentType == null) return NotFound();
        return Ok(contentType);
    }

    [HttpGet("by-extension/{extension}")]
    public async Task<ActionResult<ContentType>> GetByExtension(string extension)
    {
        var contentType = await _contentTypeRepository.GetByExtensionAsync(extension);
        if (contentType == null) return NotFound();
        return Ok(contentType);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateContentTypeRequest request)
    {
        var contentType = new ContentType
        {
            Extension = request.Extension,
            MimeType = request.MimeType,
            DisplayName = request.DisplayName,
            Icon = request.Icon,
            AllowPreview = request.AllowPreview,
            AllowThumbnail = request.AllowThumbnail,
            MaxFileSizeMB = request.MaxFileSizeMB,
            IsActive = true
        };

        var id = await _contentTypeRepository.CreateAsync(contentType);
        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateContentTypeRequest request)
    {
        var contentType = await _contentTypeRepository.GetByIdAsync(id);
        if (contentType == null) return NotFound();

        contentType.Extension = request.Extension;
        contentType.MimeType = request.MimeType;
        contentType.DisplayName = request.DisplayName;
        contentType.Icon = request.Icon;
        contentType.AllowPreview = request.AllowPreview;
        contentType.AllowThumbnail = request.AllowThumbnail;
        contentType.MaxFileSizeMB = request.MaxFileSizeMB;
        contentType.IsActive = request.IsActive;

        var result = await _contentTypeRepository.UpdateAsync(contentType);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _contentTypeRepository.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}
