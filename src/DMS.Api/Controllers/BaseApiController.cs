using System.Security.Claims;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    protected bool IsAdmin() =>
        User.IsInRole("Admin") || User.IsInRole("Administrator");

    protected async Task<bool> HasPermissionAsync(Guid userId, string nodeType, Guid nodeId, int requiredLevel)
    {
        if (IsAdmin())
            return true;

        var permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var result = await permissionService.HasPermissionAsync(userId, nodeType, nodeId, requiredLevel);
        return result.Success && result.Data;
    }

    protected IActionResult OkOrNotFound<T>(ServiceResult<T> result) =>
        result.Success ? Ok(result.Data) : NotFound(result.Errors);

    protected IActionResult OkOrBadRequest<T>(ServiceResult<T> result) =>
        result.Success ? Ok(result.Data) : BadRequest(result.Errors);

    protected IActionResult OkOrBadRequest(ServiceResult result) =>
        result.Success ? Ok(result.Message) : BadRequest(result.Errors);
}
