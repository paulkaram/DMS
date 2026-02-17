using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

public class SearchController : BaseApiController
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService) => _searchService = searchService;

    [HttpPost("documents")]
    public async Task<IActionResult> SearchDocuments([FromBody] SearchDocumentsRequest request)
        => OkOrBadRequest(await _searchService.SearchDocumentsAsync(request, GetCurrentUserId()));

    [HttpPost("all")]
    public async Task<IActionResult> SearchAll([FromBody] SearchDocumentsRequest request)
        => OkOrBadRequest(await _searchService.SearchAllAsync(request, GetCurrentUserId()));

    [Authorize(Roles = "Administrator")]
    [HttpPost("reindex")]
    public async Task<IActionResult> FullReindex(CancellationToken cancellationToken)
        => OkOrBadRequest(await _searchService.FullReindexAsync(cancellationToken));

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
        => Ok(await _searchService.GetHealthAsync());
}
