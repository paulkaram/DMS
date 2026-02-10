using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Authorize]
public class FavoritesController : BaseApiController
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FavoriteItemDto>>> GetMyFavorites()
    {
        var favorites = await _favoriteService.GetUserFavoritesAsync(GetCurrentUserId());
        return Ok(favorites);
    }

    [HttpGet("check")]
    public async Task<ActionResult<bool>> IsFavorite([FromQuery] int nodeType, [FromQuery] Guid nodeId)
    {
        var isFavorite = await _favoriteService.IsFavoriteAsync(GetCurrentUserId(), nodeType, nodeId);
        return Ok(isFavorite);
    }

    [HttpPost("toggle")]
    public async Task<ActionResult<bool>> ToggleFavorite([FromBody] ToggleFavoriteRequest request)
    {
        var result = await _favoriteService.ToggleFavoriteAsync(GetCurrentUserId(), request.NodeType, request.NodeId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> AddFavorite([FromBody] ToggleFavoriteRequest request)
    {
        await _favoriteService.AddFavoriteAsync(GetCurrentUserId(), request.NodeType, request.NodeId);
        return Ok();
    }

    [HttpDelete]
    public async Task<ActionResult> RemoveFavorite([FromQuery] int nodeType, [FromQuery] Guid nodeId)
    {
        await _favoriteService.RemoveFavoriteAsync(GetCurrentUserId(), nodeType, nodeId);
        return Ok();
    }
}
