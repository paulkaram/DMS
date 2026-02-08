using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScanController : ControllerBase
{
    private readonly IScanService _scanService;
    private readonly IPermissionService _permissionService;

    public ScanController(IScanService scanService, IPermissionService permissionService)
    {
        _scanService = scanService;
        _permissionService = permissionService;
    }

    [HttpPost("process")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Process(
        [FromForm] ScanProcessRequest request,
        [FromForm] List<IFormFile> images)
    {
        if (images == null || images.Count == 0)
            return BadRequest(new[] { "At least one image is required" });

        var userId = GetCurrentUserId();

        // Check write permission on target folder
        if (!await HasPermissionAsync(userId, "Folder", request.TargetFolderId, (int)PermissionLevel.Write))
            return Forbid("You don't have permission to upload to this folder");

        var streams = new List<Stream>();
        var fileNames = new List<string>();

        try
        {
            foreach (var image in images)
            {
                var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                ms.Position = 0;
                streams.Add(ms);
                fileNames.Add(image.FileName);
            }

            var result = await _scanService.ProcessAndCreateDocumentAsync(
                request, streams, fileNames, userId);

            return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    private async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        if (IsAdmin()) return true;
        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }
}
