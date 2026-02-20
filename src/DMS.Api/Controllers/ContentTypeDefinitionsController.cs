using DMS.Api.Constants;
using DMS.BL.DTOs;
using DMS.DAL.DTOs;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS.Api.Controllers;

[Route("api/content-type-definitions")]
[Authorize]
public class ContentTypeDefinitionsController : BaseApiController
{
    private readonly IContentTypeDefinitionRepository _repository;

    public ContentTypeDefinitionsController(IContentTypeDefinitionRepository repository)
    {
        _repository = repository;
    }

    #region Content Type Definitions

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContentTypeDefinition>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var contentTypes = await _repository.GetAllAsync(includeInactive);
        return Ok(contentTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContentTypeDefinition>> GetById(Guid id)
    {
        var contentType = await _repository.GetByIdWithFieldsAsync(id);
        if (contentType == null) return NotFound();
        return Ok(contentType);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateContentTypeDefinitionRequest request)
    {
        var contentType = new ContentTypeDefinition
        {
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            Color = request.Color,
            Category = request.Category,
            AllowOnFolders = request.AllowOnFolders,
            AllowOnDocuments = request.AllowOnDocuments,
            IsRequired = request.IsRequired,
            IsSystemDefault = request.IsSystemDefault,
            DefaultClassificationId = request.DefaultClassificationId,
            SortOrder = request.SortOrder,
            CreatedBy = GetCurrentUserId()
        };

        var id = await _repository.CreateAsync(contentType);

        // If setting as system default, clear any existing system default
        if (request.IsSystemDefault)
        {
            await _repository.SetSystemDefaultAsync(id, GetCurrentUserId());
        }

        return Ok(id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateContentTypeDefinitionRequest request)
    {
        var contentType = await _repository.GetByIdAsync(id);
        if (contentType == null) return NotFound();

        var wasSystemDefault = contentType.IsSystemDefault;

        contentType.Name = request.Name;
        contentType.Description = request.Description;
        contentType.Icon = request.Icon;
        contentType.Color = request.Color;
        contentType.Category = request.Category;
        contentType.AllowOnFolders = request.AllowOnFolders;
        contentType.AllowOnDocuments = request.AllowOnDocuments;
        contentType.IsRequired = request.IsRequired;
        contentType.IsSystemDefault = request.IsSystemDefault;
        contentType.DefaultClassificationId = request.DefaultClassificationId;
        contentType.IsActive = request.IsActive;
        contentType.SortOrder = request.SortOrder;
        contentType.ModifiedBy = GetCurrentUserId();

        // If setting as system default, handle clearing old default
        if (request.IsSystemDefault && !wasSystemDefault)
        {
            await _repository.SetSystemDefaultAsync(id, GetCurrentUserId());
        }

        var result = await _repository.UpdateAsync(contentType);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _repository.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok();
    }

    /// <summary>
    /// Gets the system default content type (used when no specific content type is assigned to a folder)
    /// </summary>
    [HttpGet("system-default")]
    public async Task<ActionResult<ContentTypeDefinition>> GetSystemDefault()
    {
        var contentType = await _repository.GetSystemDefaultAsync();
        if (contentType == null) return NotFound(ErrorMessages.NoSystemDefaultContentType);
        return Ok(contentType);
    }

    /// <summary>
    /// Sets a content type as the system default
    /// </summary>
    [HttpPut("{id}/set-system-default")]
    public async Task<ActionResult> SetSystemDefault(Guid id)
    {
        var contentType = await _repository.GetByIdAsync(id);
        if (contentType == null) return NotFound();

        var result = await _repository.SetSystemDefaultAsync(id, GetCurrentUserId());
        if (!result) return BadRequest(ErrorMessages.FailedToSetSystemDefault);
        return Ok();
    }

    /// <summary>
    /// Clears the system default content type
    /// </summary>
    [HttpDelete("system-default")]
    public async Task<ActionResult> ClearSystemDefault()
    {
        var result = await _repository.ClearSystemDefaultAsync(GetCurrentUserId());
        return Ok();
    }

    #endregion

    #region Content Type Fields

    [HttpGet("{contentTypeId}/fields")]
    public async Task<ActionResult<IEnumerable<ContentTypeField>>> GetFields(Guid contentTypeId)
    {
        var fields = await _repository.GetFieldsAsync(contentTypeId);
        return Ok(fields);
    }

    [HttpGet("fields/{fieldId}")]
    public async Task<ActionResult<ContentTypeField>> GetField(Guid fieldId)
    {
        var field = await _repository.GetFieldByIdAsync(fieldId);
        if (field == null) return NotFound();
        return Ok(field);
    }

    [HttpPost("{contentTypeId}/fields")]
    public async Task<ActionResult<Guid>> CreateField(Guid contentTypeId, [FromBody] CreateFieldRequest request)
    {
        var field = new ContentTypeField
        {
            ContentTypeId = contentTypeId,
            FieldName = request.FieldName,
            DisplayName = request.DisplayName,
            Description = request.Description,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            IsReadOnly = request.IsReadOnly,
            ShowInList = request.ShowInList,
            IsSearchable = request.IsSearchable,
            DefaultValue = request.DefaultValue,
            ValidationRules = request.ValidationRules,
            LookupName = request.LookupName,
            Options = request.Options,
            GroupName = request.GroupName,
            ColumnSpan = request.ColumnSpan
        };

        var id = await _repository.CreateFieldAsync(field);
        return Ok(id);
    }

    [HttpPut("fields/{fieldId}")]
    public async Task<ActionResult> UpdateField(Guid fieldId, [FromBody] UpdateFieldRequest request)
    {
        var field = await _repository.GetFieldByIdAsync(fieldId);
        if (field == null) return NotFound();

        field.FieldName = request.FieldName;
        field.DisplayName = request.DisplayName;
        field.Description = request.Description;
        field.FieldType = request.FieldType;
        field.IsRequired = request.IsRequired;
        field.IsReadOnly = request.IsReadOnly;
        field.ShowInList = request.ShowInList;
        field.IsSearchable = request.IsSearchable;
        field.DefaultValue = request.DefaultValue;
        field.ValidationRules = request.ValidationRules;
        field.LookupName = request.LookupName;
        field.Options = request.Options;
        field.GroupName = request.GroupName;
        field.ColumnSpan = request.ColumnSpan;
        field.SortOrder = request.SortOrder;
        field.IsActive = request.IsActive;

        var result = await _repository.UpdateFieldAsync(field);
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpDelete("fields/{fieldId}")]
    public async Task<ActionResult> DeleteField(Guid fieldId)
    {
        var result = await _repository.DeleteFieldAsync(fieldId);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpPut("{contentTypeId}/fields/reorder")]
    public async Task<ActionResult> ReorderFields(Guid contentTypeId, [FromBody] ReorderFieldsRequest request)
    {
        var result = await _repository.ReorderFieldsAsync(contentTypeId, request.FieldIds);
        if (!result) return BadRequest();
        return Ok();
    }

    #endregion

    #region Document Metadata

    [HttpGet("documents/{documentId}/metadata")]
    public async Task<ActionResult<IEnumerable<DocumentMetadata>>> GetDocumentMetadata(Guid documentId)
    {
        var metadata = await _repository.GetDocumentMetadataAsync(documentId);
        return Ok(metadata);
    }

    [HttpPost("documents/{documentId}/metadata/{contentTypeId}")]
    public async Task<ActionResult> SaveDocumentMetadata(Guid documentId, Guid contentTypeId, [FromBody] List<SaveMetadataRequest> metadata)
    {
        var items = metadata.Select(m => new DocumentMetadata
        {
            FieldId = m.FieldId,
            FieldName = m.FieldName,
            Value = m.Value,
            NumericValue = m.NumericValue,
            DateValue = m.DateValue
        }).ToList();

        var result = await _repository.SaveDocumentMetadataAsync(documentId, contentTypeId, items, GetCurrentUserId());
        if (!result) return BadRequest();
        return Ok();
    }

    [HttpDelete("documents/{documentId}/metadata")]
    public async Task<ActionResult> DeleteDocumentMetadata(Guid documentId)
    {
        var result = await _repository.DeleteDocumentMetadataAsync(documentId);
        return Ok();
    }

    #endregion

    #region Folder Content Type Assignments

    [HttpGet("folders/{folderId}/assignments")]
    public async Task<ActionResult<IEnumerable<FolderContentTypeAssignment>>> GetFolderContentTypes(Guid folderId)
    {
        var assignments = await _repository.GetFolderContentTypesAsync(folderId);
        return Ok(assignments);
    }

    [HttpGet("folders/{folderId}/available")]
    public async Task<ActionResult<IEnumerable<ContentTypeDefinition>>> GetAvailableContentTypes(Guid folderId)
    {
        var contentTypes = await _repository.GetAvailableContentTypesForFolderAsync(folderId);
        return Ok(contentTypes);
    }

    [HttpPost("folders/{folderId}/assign")]
    public async Task<ActionResult<Guid>> AssignContentTypeToFolder(Guid folderId, [FromBody] AssignContentTypeRequest request)
    {
        var assignment = new FolderContentTypeAssignment
        {
            FolderId = folderId,
            ContentTypeId = request.ContentTypeId,
            IsRequired = request.IsRequired,
            IsDefault = request.IsDefault,
            InheritToChildren = request.InheritToChildren,
            DisplayOrder = request.DisplayOrder,
            CreatedBy = GetCurrentUserId()
        };

        var id = await _repository.AssignContentTypeToFolderAsync(assignment);
        return Ok(id);
    }

    [HttpPut("folders/{folderId}/assignments/{contentTypeId}")]
    public async Task<ActionResult> UpdateFolderAssignment(Guid folderId, Guid contentTypeId, [FromBody] UpdateAssignmentRequest request)
    {
        var result = await _repository.UpdateFolderAssignmentAsync(
            folderId, contentTypeId,
            request.IsRequired, request.IsDefault,
            request.InheritToChildren, request.DisplayOrder);

        if (!result) return NotFound();
        return Ok();
    }

    [HttpPut("folders/{folderId}/default/{contentTypeId}")]
    public async Task<ActionResult> SetFolderDefaultContentType(Guid folderId, Guid contentTypeId)
    {
        var result = await _repository.SetFolderDefaultContentTypeAsync(folderId, contentTypeId);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("folders/{folderId}/unassign/{contentTypeId}")]
    public async Task<ActionResult> RemoveContentTypeFromFolder(Guid folderId, Guid contentTypeId)
    {
        var result = await _repository.RemoveContentTypeFromFolderAsync(folderId, contentTypeId);
        if (!result) return NotFound();
        return Ok();
    }

    #endregion

    #region Cabinet Content Type Assignments

    [HttpGet("cabinets/{cabinetId}/assignments")]
    public async Task<ActionResult<IEnumerable<CabinetContentTypeAssignment>>> GetCabinetContentTypes(Guid cabinetId)
    {
        var assignments = await _repository.GetCabinetContentTypesAsync(cabinetId);
        return Ok(assignments);
    }

    [HttpPost("cabinets/{cabinetId}/assign")]
    public async Task<ActionResult<Guid>> AssignContentTypeToCabinet(Guid cabinetId, [FromBody] AssignContentTypeRequest request)
    {
        var assignment = new CabinetContentTypeAssignment
        {
            CabinetId = cabinetId,
            ContentTypeId = request.ContentTypeId,
            IsRequired = request.IsRequired,
            IsDefault = request.IsDefault,
            InheritToChildren = request.InheritToChildren,
            DisplayOrder = request.DisplayOrder,
            CreatedBy = GetCurrentUserId()
        };

        var id = await _repository.AssignContentTypeToCabinetAsync(assignment);
        return Ok(id);
    }

    [HttpPut("cabinets/{cabinetId}/assignments/{contentTypeId}")]
    public async Task<ActionResult> UpdateCabinetAssignment(Guid cabinetId, Guid contentTypeId, [FromBody] UpdateAssignmentRequest request)
    {
        var result = await _repository.UpdateCabinetAssignmentAsync(
            cabinetId, contentTypeId,
            request.IsRequired, request.IsDefault,
            request.InheritToChildren, request.DisplayOrder);

        if (!result) return NotFound();
        return Ok();
    }

    [HttpPut("cabinets/{cabinetId}/default/{contentTypeId}")]
    public async Task<ActionResult> SetCabinetDefaultContentType(Guid cabinetId, Guid contentTypeId)
    {
        var result = await _repository.SetCabinetDefaultContentTypeAsync(cabinetId, contentTypeId);
        if (!result) return NotFound();
        return Ok();
    }

    [HttpDelete("cabinets/{cabinetId}/unassign/{contentTypeId}")]
    public async Task<ActionResult> RemoveContentTypeFromCabinet(Guid cabinetId, Guid contentTypeId)
    {
        var result = await _repository.RemoveContentTypeFromCabinetAsync(cabinetId, contentTypeId);
        if (!result) return NotFound();
        return Ok();
    }

    #endregion

    #region Effective Content Types

    /// <summary>
    /// Gets effective content types for a folder (includes inheritance from parent folders and cabinet)
    /// </summary>
    [HttpGet("folders/{folderId}/effective")]
    public async Task<ActionResult<IEnumerable<EffectiveContentTypeDto>>> GetEffectiveContentTypes(Guid folderId)
    {
        var contentTypes = await _repository.GetEffectiveContentTypesAsync(folderId);
        return Ok(contentTypes);
    }

    /// <summary>
    /// Gets content type info summary for a folder (includes default, required status, etc.)
    /// </summary>
    [HttpGet("folders/{folderId}/info")]
    public async Task<ActionResult<FolderContentTypeInfoDto>> GetFolderContentTypeInfo(Guid folderId)
    {
        var info = await _repository.GetFolderContentTypeInfoAsync(folderId);
        if (info == null) return NotFound();
        return Ok(info);
    }

    #endregion
}
