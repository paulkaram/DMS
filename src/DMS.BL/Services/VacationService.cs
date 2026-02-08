using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class VacationService : IVacationService
{
    private readonly IVacationRepository _vacationRepository;

    public VacationService(IVacationRepository vacationRepository)
    {
        _vacationRepository = vacationRepository;
    }

    public async Task<VacationDto?> GetByIdAsync(Guid id)
    {
        var vacation = await _vacationRepository.GetByIdAsync(id);
        return vacation != null ? MapToDto(vacation) : null;
    }

    public async Task<IEnumerable<VacationDto>> GetUserVacationsAsync(Guid userId)
    {
        var vacations = await _vacationRepository.GetByUserIdAsync(userId);
        return vacations.Select(MapToDto);
    }

    public async Task<VacationDto?> GetActiveVacationAsync(Guid userId)
    {
        var vacation = await _vacationRepository.GetActiveVacationAsync(userId);
        return vacation != null ? MapToDto(vacation) : null;
    }

    public async Task<IEnumerable<VacationDto>> GetAllActiveAsync()
    {
        var vacations = await _vacationRepository.GetAllActiveAsync();
        return vacations.Select(MapToDto);
    }

    public async Task<Guid> CreateAsync(Guid userId, CreateVacationRequest request)
    {
        var vacation = new Vacation
        {
            UserId = userId,
            DelegateToUserId = request.DelegateToUserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Message = request.Message,
            AutoReply = request.AutoReply,
            IsActive = true
        };

        return await _vacationRepository.CreateAsync(vacation);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateVacationRequest request)
    {
        var vacation = await _vacationRepository.GetByIdAsync(id);
        if (vacation == null) return false;

        vacation.DelegateToUserId = request.DelegateToUserId;
        vacation.StartDate = request.StartDate;
        vacation.EndDate = request.EndDate;
        vacation.Message = request.Message;
        vacation.AutoReply = request.AutoReply;
        vacation.IsActive = request.IsActive;

        return await _vacationRepository.UpdateAsync(vacation);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _vacationRepository.DeleteAsync(id);
    }

    public async Task<bool> CancelAsync(Guid id)
    {
        var vacation = await _vacationRepository.GetByIdAsync(id);
        if (vacation == null) return false;

        vacation.IsActive = false;
        return await _vacationRepository.UpdateAsync(vacation);
    }

    private static VacationDto MapToDto(Vacation vacation)
    {
        return new VacationDto
        {
            Id = vacation.Id,
            UserId = vacation.UserId,
            DelegateToUserId = vacation.DelegateToUserId,
            StartDate = vacation.StartDate,
            EndDate = vacation.EndDate,
            Message = vacation.Message,
            AutoReply = vacation.AutoReply,
            IsActive = vacation.IsActive,
            CreatedAt = vacation.CreatedAt,
            ModifiedAt = vacation.ModifiedAt,
            UserName = vacation.UserName,
            DelegateToUserName = vacation.DelegateToUserName
        };
    }
}
