using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FolderLinksController : ControllerBase
{
    private readonly IFolderLinkRepository _folderLinkRepository;

    public FolderLinksController(IFolderLinkRepository folderLinkRepository)
    {
        _folderLinkRepository = folderLinkRepository;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet("from/{folderId}")]
    public async Task<ActionResult<IEnumerable<FolderLink>>> GetLinksFromFolder(Guid folderId)
    {
        var links = await _folderLinkRepository.GetBySourceFolderAsync(folderId);
        return Ok(links);
    }

    [HttpGet("to/{folderId}")]
    public async Task<ActionResult<IEnumerable<FolderLink>>> GetLinksToFolder(Guid folderId)
    {
        var links = await _folderLinkRepository.GetByTargetFolderAsync(folderId);
        return Ok(links);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateFolderLinkRequest request)
    {
        var link = new FolderLink
        {
            SourceFolderId = request.SourceFolderId,
            TargetFolderId = request.TargetFolderId,
            CreatedBy = GetUserId()
        };

        var id = await _folderLinkRepository.CreateAsync(link);
        return Ok(id);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _folderLinkRepository.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }
}

public class CreateFolderLinkRequest
{
    public Guid SourceFolderId { get; set; }
    public Guid TargetFolderId { get; set; }
}
