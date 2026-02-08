using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FavoriteItemDto>>> GetMyFavorites()
    {
        var favorites = await _favoriteService.GetUserFavoritesAsync(GetUserId());
        return Ok(favorites);
    }

    [HttpGet("check")]
    public async Task<ActionResult<bool>> IsFavorite([FromQuery] int nodeType, [FromQuery] Guid nodeId)
    {
        var isFavorite = await _favoriteService.IsFavoriteAsync(GetUserId(), nodeType, nodeId);
        return Ok(isFavorite);
    }

    [HttpPost("toggle")]
    public async Task<ActionResult<bool>> ToggleFavorite([FromBody] ToggleFavoriteRequest request)
    {
        var result = await _favoriteService.ToggleFavoriteAsync(GetUserId(), request.NodeType, request.NodeId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> AddFavorite([FromBody] ToggleFavoriteRequest request)
    {
        await _favoriteService.AddFavoriteAsync(GetUserId(), request.NodeType, request.NodeId);
        return Ok();
    }

    [HttpDelete]
    public async Task<ActionResult> RemoveFavorite([FromQuery] int nodeType, [FromQuery] Guid nodeId)
    {
        await _favoriteService.RemoveFavoriteAsync(GetUserId(), nodeType, nodeId);
        return Ok();
    }
}
