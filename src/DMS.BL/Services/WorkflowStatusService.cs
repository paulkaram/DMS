using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class WorkflowStatusService : IWorkflowStatusService
{
    private readonly IWorkflowStatusRepository _repository;

    public WorkflowStatusService(IWorkflowStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IEnumerable<WorkflowStatusDto>>> GetAllAsync(bool includeInactive = false)
    {
        var entities = await _repository.GetAllAsync(includeInactive);
        var dtos = entities.Select(MapToDto);
        return ServiceResult<IEnumerable<WorkflowStatusDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<WorkflowStatusDto>> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<WorkflowStatusDto>.Fail("Workflow status not found.");

        return ServiceResult<WorkflowStatusDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult<WorkflowStatusDto>> CreateAsync(CreateWorkflowStatusRequest request)
    {
        var entity = new WorkflowStatus
        {
            Name = request.Name,
            Color = request.Color,
            Icon = request.Icon,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = true
        };

        var id = await _repository.CreateAsync(entity);
        entity.Id = id;

        return ServiceResult<WorkflowStatusDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult<WorkflowStatusDto>> UpdateAsync(Guid id, UpdateWorkflowStatusRequest request)
    {
        var entity = new WorkflowStatus
        {
            Id = id,
            Name = request.Name,
            Color = request.Color,
            Icon = request.Icon,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        var updated = await _repository.UpdateAsync(entity);
        if (!updated)
            return ServiceResult<WorkflowStatusDto>.Fail("Workflow status not found.");

        return ServiceResult<WorkflowStatusDto>.Ok(MapToDto(entity));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        var deleted = await _repository.DeleteAsync(id);
        if (!deleted)
            return ServiceResult.Fail("Workflow status not found.");

        return ServiceResult.Ok("Workflow status deleted.");
    }

    private static WorkflowStatusDto MapToDto(WorkflowStatus entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Color = entity.Color,
        Icon = entity.Icon,
        Description = entity.Description,
        SortOrder = entity.SortOrder,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };
}
