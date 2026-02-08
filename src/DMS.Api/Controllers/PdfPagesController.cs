using System.Text.Json;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/documents/{documentId:guid}/pages")]
[Authorize]
public class PdfPagesController : ControllerBase
{
    private readonly IPdfPageService _pdfPageService;
    private readonly IPermissionService _permissionService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public PdfPagesController(
        IPdfPageService pdfPageService,
        IPermissionService permissionService)
    {
        _pdfPageService = pdfPageService;
        _permissionService = permissionService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private bool IsAdmin() => User.IsInRole("Admin");

    private async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        if (IsAdmin()) return true;
        var result = await _permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetPageCount(Guid documentId)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Read))
            return Forbid("You don't have permission to view this document");

        var result = await _pdfPageService.GetPageCountAsync(documentId);
        if (!result.Success)
            return BadRequest(result.Errors);

        return Ok(result.Data);
    }

    [HttpPost("reorganize")]
    [RequestSizeLimit(200_000_000)] // 200MB
    public async Task<ActionResult<PageReorganizeResult>> Reorganize(
        Guid documentId,
        [FromForm] string manifest,
        [FromForm] List<IFormFile>? files)
    {
        var userId = GetCurrentUserId();
        if (!await HasPermissionAsync(userId, "Document", documentId, (int)PermissionLevel.Write))
            return Forbid("You don't have write permission on this document");

        PageReorganizeRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<PageReorganizeRequest>(manifest, JsonOptions);
        }
        catch (JsonException)
        {
            return BadRequest(new[] { "Invalid manifest JSON" });
        }

        if (request == null || request.Pages.Count == 0)
            return BadRequest(new[] { "Manifest must contain at least one page" });

        var streams = new List<Stream>();
        var fileNames = new List<string>();
        var contentTypes = new List<string>();

        try
        {
            if (files != null)
            {
                foreach (var file in files)
                {
                    var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    ms.Position = 0;
                    streams.Add(ms);
                    fileNames.Add(file.FileName);
                    contentTypes.Add(file.ContentType);
                }
            }

            var result = await _pdfPageService.ReorganizePagesAsync(
                documentId, request, streams, fileNames, contentTypes, userId);

            if (!result.Success)
                return BadRequest(result.Errors);

            return Ok(result.Data);
        }
        finally
        {
            foreach (var s in streams)
                await s.DisposeAsync();
        }
    }
}
