using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface ICabinetService
{
    Task<ServiceResult<CabinetDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<CabinetDto>>> GetAllAsync();
    Task<ServiceResult<List<CabinetDto>>> SearchAsync(string? name);
    Task<ServiceResult<CabinetDto>> CreateAsync(CreateCabinetDto dto, Guid userId);
    Task<ServiceResult<CabinetDto>> UpdateAsync(Guid id, UpdateCabinetDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id, Guid userId);
}
