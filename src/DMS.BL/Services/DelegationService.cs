using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DelegationService : IDelegationService
{
    private readonly IDelegationRepository _delegationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public DelegationService(
        IDelegationRepository delegationRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository)
    {
        _delegationRepository = delegationRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<ServiceResult<DelegationDto>> GetByIdAsync(Guid id)
    {
        var delegation = await _delegationRepository.GetByIdAsync(id);
        if (delegation == null)
            return ServiceResult<DelegationDto>.Fail("Delegation not found");

        return ServiceResult<DelegationDto>.Ok(await MapToDtoAsync(delegation));
    }

    public async Task<ServiceResult<List<DelegationDto>>> GetMyDelegationsAsync(Guid userId)
    {
        var delegations = await _delegationRepository.GetByFromUserIdAsync(userId);
        var dtos = new List<DelegationDto>();
        foreach (var d in delegations)
            dtos.Add(await MapToDtoAsync(d));
        return ServiceResult<List<DelegationDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<DelegationDto>>> GetDelegationsToMeAsync(Guid userId)
    {
        var delegations = await _delegationRepository.GetByToUserIdAsync(userId);
        var dtos = new List<DelegationDto>();
        foreach (var d in delegations)
            dtos.Add(await MapToDtoAsync(d));
        return ServiceResult<List<DelegationDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<DelegationDto>> CreateAsync(CreateDelegationDto dto, Guid fromUserId)
    {
        var delegation = new Delegation
        {
            FromUserId = fromUserId,
            ToUserId = dto.ToUserId,
            RoleId = dto.RoleId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = true,
            CreatedBy = fromUserId
        };

        var id = await _delegationRepository.CreateAsync(delegation);
        delegation.Id = id;

        return ServiceResult<DelegationDto>.Ok(await MapToDtoAsync(delegation), "Delegation created");
    }

    public async Task<ServiceResult<DelegationDto>> UpdateAsync(Guid id, UpdateDelegationDto dto, Guid userId)
    {
        var delegation = await _delegationRepository.GetByIdAsync(id);
        if (delegation == null)
            return ServiceResult<DelegationDto>.Fail("Delegation not found");

        if (delegation.FromUserId != userId)
            return ServiceResult<DelegationDto>.Fail("You can only update your own delegations");

        delegation.ToUserId = dto.ToUserId;
        delegation.RoleId = dto.RoleId;
        delegation.StartDate = dto.StartDate;
        delegation.EndDate = dto.EndDate;
        delegation.IsActive = dto.IsActive;

        await _delegationRepository.UpdateAsync(delegation);

        return ServiceResult<DelegationDto>.Ok(await MapToDtoAsync(delegation), "Delegation updated");
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, Guid userId)
    {
        var delegation = await _delegationRepository.GetByIdAsync(id);
        if (delegation == null)
            return ServiceResult.Fail("Delegation not found");

        if (delegation.FromUserId != userId)
            return ServiceResult.Fail("You can only delete your own delegations");

        await _delegationRepository.DeleteAsync(id);
        return ServiceResult.Ok("Delegation deleted");
    }

    private async Task<DelegationDto> MapToDtoAsync(Delegation delegation)
    {
        var fromUser = await _userRepository.GetByIdAsync(delegation.FromUserId);
        var toUser = await _userRepository.GetByIdAsync(delegation.ToUserId);
        Role? role = null;
        if (delegation.RoleId.HasValue)
            role = await _roleRepository.GetByIdAsync(delegation.RoleId.Value);

        return new DelegationDto
        {
            Id = delegation.Id,
            FromUserId = delegation.FromUserId,
            FromUserName = fromUser?.DisplayName ?? fromUser?.Username,
            ToUserId = delegation.ToUserId,
            ToUserName = toUser?.DisplayName ?? toUser?.Username,
            RoleId = delegation.RoleId,
            RoleName = role?.Name,
            StartDate = delegation.StartDate,
            EndDate = delegation.EndDate,
            IsActive = delegation.IsActive
        };
    }
}
