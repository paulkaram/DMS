using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StructuresController : ControllerBase
{
    private readonly IStructureService _structureService;

    public StructuresController(IStructureService structureService)
    {
        _structureService = structureService;
    }

    #region Structure CRUD

    /// <summary>
    /// Get all structures (organizational units)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _structureService.GetAllAsync(includeInactive);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get structure by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _structureService.GetByIdAsync(id);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    /// <summary>
    /// Get structure by code
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _structureService.GetByCodeAsync(code);
        return result.Success ? Ok(result.Data) : NotFound(result.Errors);
    }

    /// <summary>
    /// Create a new structure
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStructureDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _structureService.CreateAsync(dto, userId);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Update an existing structure
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStructureDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _structureService.UpdateAsync(id, dto, userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Delete a structure
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _structureService.DeleteAsync(id, userId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    #endregion

    #region Hierarchy Operations

    /// <summary>
    /// Get root structures (top-level organizational units)
    /// </summary>
    [HttpGet("roots")]
    public async Task<IActionResult> GetRoots()
    {
        var result = await _structureService.GetRootStructuresAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get children of a structure
    /// </summary>
    [HttpGet("{id:guid}/children")]
    public async Task<IActionResult> GetChildren(Guid id)
    {
        var result = await _structureService.GetChildrenAsync(id);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get all descendants of a structure
    /// </summary>
    [HttpGet("{id:guid}/descendants")]
    public async Task<IActionResult> GetDescendants(Guid id)
    {
        var result = await _structureService.GetDescendantsAsync(id);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get ancestors (parents) of a structure
    /// </summary>
    [HttpGet("{id:guid}/ancestors")]
    public async Task<IActionResult> GetAncestors(Guid id)
    {
        var result = await _structureService.GetAncestorsAsync(id);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get structures by type (Ministry, Department, Division, Section, Unit)
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetByType(string type)
    {
        var result = await _structureService.GetByTypeAsync(type);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get the full organization hierarchy tree
    /// </summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var result = await _structureService.GetHierarchyTreeAsync();
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    #endregion

    #region Member Management

    /// <summary>
    /// Get members of a structure
    /// </summary>
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var result = await _structureService.GetMembersAsync(id);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Add a member to a structure
    /// </summary>
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddStructureMemberDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _structureService.AddMemberAsync(id, dto, userId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Remove a member from a structure
    /// </summary>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _structureService.RemoveMemberAsync(id, userId, currentUserId);
        return result.Success ? NoContent() : BadRequest(result.Errors);
    }

    #endregion

    #region User Structure Operations

    /// <summary>
    /// Get structures for a specific user
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    public async Task<IActionResult> GetUserStructures(Guid userId)
    {
        var result = await _structureService.GetUserStructuresAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get structures for the current user
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyStructures()
    {
        var userId = GetCurrentUserId();
        var result = await _structureService.GetUserStructuresAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Get primary structure for a user
    /// </summary>
    [HttpGet("user/{userId:guid}/primary")]
    public async Task<IActionResult> GetUserPrimaryStructure(Guid userId)
    {
        var result = await _structureService.GetUserPrimaryStructureAsync(userId);
        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }

    /// <summary>
    /// Set primary structure for a user
    /// </summary>
    [HttpPut("user/{userId:guid}/primary/{structureId:guid}")]
    public async Task<IActionResult> SetUserPrimaryStructure(Guid userId, Guid structureId)
    {
        var currentUserId = GetCurrentUserId();
        var result = await _structureService.SetPrimaryStructureAsync(userId, structureId, currentUserId);
        return result.Success ? Ok(result.Message) : BadRequest(result.Errors);
    }

    #endregion

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
