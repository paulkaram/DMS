using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IVacationService
{
    Task<VacationDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<VacationDto>> GetUserVacationsAsync(Guid userId);
    Task<VacationDto?> GetActiveVacationAsync(Guid userId);
    Task<IEnumerable<VacationDto>> GetAllActiveAsync();
    Task<Guid> CreateAsync(Guid userId, CreateVacationRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateVacationRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> CancelAsync(Guid id);
}
