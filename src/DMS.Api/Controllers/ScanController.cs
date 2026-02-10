using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class ScanController : BaseApiController
{
    private readonly IScanService _scanService;

    public ScanController(IScanService scanService)
    {
        _scanService = scanService;
    }

    [HttpPost("process")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> Process(
        [FromForm] ScanProcessRequest request,
        [FromForm] List<IFormFile> images)
    {
        if (images == null || images.Count == 0)
            return BadRequest(new[] { ErrorMessages.AtLeastOneImageRequired });

        var userId = GetCurrentUserId();

        // Check write permission on target folder
        if (!await HasPermissionAsync(userId, "Folder", request.TargetFolderId, (int)PermissionLevel.Write))
            return Forbid(ErrorMessages.Permissions.UploadToFolder);

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
}
