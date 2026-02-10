using DMS.BL.DTOs;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

#region Bookmarks Controller

[Route("api/bookmarks")]
[Authorize]
public class BookmarksController : BaseApiController
{
    private readonly IBookmarkRepository _repository;
    public BookmarksController(IBookmarkRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bookmark>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<Bookmark>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] Bookmark request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] Bookmark request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Cases Controller

[Route("api/cases")]
[Authorize]
public class CasesController : BaseApiController
{
    private readonly ICaseRepository _repository;
    public CasesController(ICaseRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Case>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<Case>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<Case>>> GetByStatus(string status) =>
        Ok(await _repository.GetByStatusAsync(status));

    [HttpGet("my-cases")]
    public async Task<ActionResult<IEnumerable<Case>>> GetMyCases() =>
        Ok(await _repository.GetByAssigneeAsync(GetCurrentUserId()));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] Case request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] Case request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request) =>
        await _repository.UpdateStatusAsync(id, request.Status, GetCurrentUserId()) ? Ok() : NotFound();

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Endpoints Controller

[Route("api/endpoints")]
[Authorize]
public class EndpointsController : BaseApiController
{
    private readonly IEndpointRepository _repository;
    public EndpointsController(IEndpointRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServiceEndpoint>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceEndpoint>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("type/{endpointType}")]
    public async Task<ActionResult<IEnumerable<ServiceEndpoint>>> GetByType(string endpointType) =>
        Ok(await _repository.GetByTypeAsync(endpointType));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ServiceEndpoint request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] ServiceEndpoint request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpPost("{id}/health-check")]
    public async Task<ActionResult> HealthCheck(Guid id)
    {
        // Simple health check - would need actual HTTP call implementation
        await _repository.UpdateHealthStatusAsync(id, "OK");
        return Ok(new { status = "OK" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Export Config Controller

[Route("api/export-configs")]
[Authorize]
public class ExportConfigsController : BaseApiController
{
    private readonly IExportConfigRepository _repository;
    public ExportConfigsController(IExportConfigRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExportConfig>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<ExportConfig>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("default")]
    public async Task<ActionResult<ExportConfig>> GetDefault()
    {
        var item = await _repository.GetDefaultAsync();
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ExportConfig request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] ExportConfig request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(Guid id) =>
        await _repository.SetDefaultAsync(id) ? Ok() : BadRequest();

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Naming Conventions Controller

[Route("api/naming-conventions")]
[Authorize]
public class NamingConventionsController : BaseApiController
{
    private readonly INamingConventionRepository _repository;
    public NamingConventionsController(INamingConventionRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NamingConvention>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<NamingConvention>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("folder/{folderId}")]
    public async Task<ActionResult<IEnumerable<NamingConvention>>> GetByFolder(Guid folderId) =>
        Ok(await _repository.GetByFolderAsync(folderId));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] NamingConvention request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] NamingConvention request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Organization Templates Controller

[Route("api/organization-templates")]
[Authorize]
public class OrganizationTemplatesController : BaseApiController
{
    private readonly IOrganizationTemplateRepository _repository;
    public OrganizationTemplatesController(IOrganizationTemplateRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrganizationTemplate>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<OrganizationTemplate>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("default")]
    public async Task<ActionResult<OrganizationTemplate>> GetDefault()
    {
        var item = await _repository.GetDefaultAsync();
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] OrganizationTemplate request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] OrganizationTemplate request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(Guid id) =>
        await _repository.SetDefaultAsync(id) ? Ok() : BadRequest();

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Permission Level Definitions Controller

[Route("api/permission-levels")]
[Authorize]
public class PermissionLevelsController : BaseApiController
{
    private readonly IPermissionLevelDefinitionRepository _repository;
    public PermissionLevelsController(IPermissionLevelDefinitionRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PermissionLevelDefinition>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<PermissionLevelDefinition>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("level/{level}")]
    public async Task<ActionResult<PermissionLevelDefinition>> GetByLevel(int level)
    {
        var item = await _repository.GetByLevelAsync(level);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] PermissionLevelDefinition request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] PermissionLevelDefinition request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Purposes Controller

[Route("api/purposes")]
[Authorize]
public class PurposesController : BaseApiController
{
    private readonly IPurposeRepository _repository;
    public PurposesController(IPurposeRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Purpose>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<Purpose>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("type/{purposeType}")]
    public async Task<ActionResult<IEnumerable<Purpose>>> GetByType(string purposeType) =>
        Ok(await _repository.GetByTypeAsync(purposeType));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] Purpose request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] Purpose request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Scan Config Controller

[Route("api/scan-configs")]
[Authorize]
public class ScanConfigsController : BaseApiController
{
    private readonly IScanConfigRepository _repository;
    public ScanConfigsController(IScanConfigRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScanConfig>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("{id}")]
    public async Task<ActionResult<ScanConfig>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("default")]
    public async Task<ActionResult<ScanConfig>> GetDefault()
    {
        var item = await _repository.GetDefaultAsync();
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] ScanConfig request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] ScanConfig request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(Guid id) =>
        await _repository.SetDefaultAsync(id) ? Ok() : BadRequest();

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion

#region Search Config Controller

[Route("api/search-configs")]
[Authorize]
public class SearchConfigsController : BaseApiController
{
    private readonly ISearchConfigRepository _repository;
    public SearchConfigsController(ISearchConfigRepository repository) => _repository = repository;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SearchConfig>>> GetAll([FromQuery] bool includeInactive = false) =>
        Ok(await _repository.GetAllAsync(includeInactive));

    [HttpGet("global")]
    public async Task<ActionResult<IEnumerable<SearchConfig>>> GetGlobal() =>
        Ok(await _repository.GetGlobalAsync());

    [HttpGet("my-configs")]
    public async Task<ActionResult<IEnumerable<SearchConfig>>> GetMyConfigs() =>
        Ok(await _repository.GetByUserAsync(GetCurrentUserId()));

    [HttpGet("{id}")]
    public async Task<ActionResult<SearchConfig>> GetById(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpGet("default")]
    public async Task<ActionResult<SearchConfig>> GetDefault()
    {
        var item = await _repository.GetDefaultAsync();
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] SearchConfig request)
    {
        request.CreatedBy = GetCurrentUserId();
        return Ok(await _repository.CreateAsync(request));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] SearchConfig request)
    {
        request.Id = id;
        request.ModifiedBy = GetCurrentUserId();
        return await _repository.UpdateAsync(request) ? Ok() : NotFound();
    }

    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(Guid id) =>
        await _repository.SetDefaultAsync(id) ? Ok() : BadRequest();

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id) =>
        await _repository.DeleteAsync(id) ? Ok() : NotFound();
}

#endregion
