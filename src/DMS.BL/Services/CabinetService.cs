using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class CabinetService : ICabinetService
{
    private readonly ICabinetRepository _cabinetRepository;
    private readonly IActivityLogService _activityLogService;
    private readonly IRecycleBinRepository _recycleBinRepository;

    public CabinetService(
        ICabinetRepository cabinetRepository,
        IActivityLogService activityLogService,
        IRecycleBinRepository recycleBinRepository)
    {
        _cabinetRepository = cabinetRepository;
        _activityLogService = activityLogService;
        _recycleBinRepository = recycleBinRepository;
    }

    public async Task<ServiceResult<CabinetDto>> GetByIdAsync(Guid id)
    {
        var cabinet = await _cabinetRepository.GetByIdAsync(id);
        if (cabinet == null)
            return ServiceResult<CabinetDto>.Fail("Cabinet not found");

        return ServiceResult<CabinetDto>.Ok(MapToDto(cabinet));
    }

    public async Task<ServiceResult<List<CabinetDto>>> GetAllAsync()
    {
        var cabinets = await _cabinetRepository.GetActiveAsync();
        return ServiceResult<List<CabinetDto>>.Ok(cabinets.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<List<CabinetDto>>> SearchAsync(string? name)
    {
        var cabinets = await _cabinetRepository.SearchAsync(name);
        return ServiceResult<List<CabinetDto>>.Ok(cabinets.Select(MapToDto).ToList());
    }

    public async Task<ServiceResult<CabinetDto>> CreateAsync(CreateCabinetDto dto, Guid userId)
    {
        var cabinet = new Cabinet
        {
            Name = dto.Name,
            Description = dto.Description,
            CreatedBy = userId,
            IsActive = true
        };

        var id = await _cabinetRepository.CreateAsync(cabinet);
        cabinet.Id = id;

        await _activityLogService.LogActivityAsync(
            ActivityActions.Created, "Cabinet", id, dto.Name, null, userId, null, null);

        return ServiceResult<CabinetDto>.Ok(MapToDto(cabinet), "Cabinet created successfully");
    }

    public async Task<ServiceResult<CabinetDto>> UpdateAsync(Guid id, UpdateCabinetDto dto, Guid userId)
    {
        var cabinet = await _cabinetRepository.GetByIdAsync(id);
        if (cabinet == null)
            return ServiceResult<CabinetDto>.Fail("Cabinet not found");

        cabinet.Name = dto.Name;
        cabinet.Description = dto.Description;
        cabinet.BreakInheritance = dto.BreakInheritance;
        cabinet.ModifiedBy = userId;

        await _cabinetRepository.UpdateAsync(cabinet);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Updated, "Cabinet", id, dto.Name, null, userId, null, null);

        return ServiceResult<CabinetDto>.Ok(MapToDto(cabinet), "Cabinet updated successfully");
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, Guid userId)
    {
        var cabinet = await _cabinetRepository.GetByIdAsync(id);
        if (cabinet == null)
            return ServiceResult.Fail("Cabinet not found");

        // Add to recycle bin before soft delete
        var recycleBinItem = new RecycleBinItem
        {
            NodeType = 1, // Cabinet
            NodeId = cabinet.Id,
            NodeName = cabinet.Name,
            OriginalPath = "/" + cabinet.Name,
            OriginalParentId = null,
            DeletedBy = userId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                cabinet.Description
            })
        };
        await _recycleBinRepository.AddAsync(recycleBinItem);

        await _cabinetRepository.DeleteAsync(id);

        await _activityLogService.LogActivityAsync(
            ActivityActions.Deleted, "Cabinet", id, cabinet.Name, null, userId, null, null);

        return ServiceResult.Ok("Cabinet moved to recycle bin");
    }

    private static CabinetDto MapToDto(Cabinet cabinet)
    {
        return new CabinetDto
        {
            Id = cabinet.Id,
            Name = cabinet.Name,
            Description = cabinet.Description,
            BreakInheritance = cabinet.BreakInheritance,
            CreatedAt = cabinet.CreatedAt,
            ModifiedAt = cabinet.ModifiedAt
        };
    }
}
