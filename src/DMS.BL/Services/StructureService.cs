using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class StructureService : IStructureService
{
    private readonly IStructureRepository _structureRepo;
    private readonly IUserRepository _userRepo;

    public StructureService(IStructureRepository structureRepo, IUserRepository userRepo)
    {
        _structureRepo = structureRepo;
        _userRepo = userRepo;
    }

    #region Structure CRUD

    public async Task<ServiceResult<StructureDto>> GetByIdAsync(Guid id)
    {
        var structure = await _structureRepo.GetByIdAsync(id);
        if (structure == null)
            return ServiceResult<StructureDto>.Fail("Structure not found");

        return ServiceResult<StructureDto>.Ok(await MapToStructureDtoAsync(structure));
    }

    public async Task<ServiceResult<StructureDto>> GetByCodeAsync(string code)
    {
        var structure = await _structureRepo.GetByCodeAsync(code);
        if (structure == null)
            return ServiceResult<StructureDto>.Fail("Structure not found");

        return ServiceResult<StructureDto>.Ok(await MapToStructureDtoAsync(structure));
    }

    public async Task<ServiceResult<IEnumerable<StructureDto>>> GetAllAsync(bool includeInactive = false)
    {
        var structures = await _structureRepo.GetAllAsync(includeInactive);
        var dtos = new List<StructureDto>();
        foreach (var s in structures)
        {
            dtos.Add(await MapToStructureDtoAsync(s));
        }
        return ServiceResult<IEnumerable<StructureDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<StructureDto>> CreateAsync(CreateStructureDto dto, Guid userId)
    {
        if (!Enum.TryParse<StructureType>(dto.Type, true, out var structureType))
            return ServiceResult<StructureDto>.Fail("Invalid structure type");

        // Check if code already exists
        var existing = await _structureRepo.GetByCodeAsync(dto.Code);
        if (existing != null)
            return ServiceResult<StructureDto>.Fail("Structure code already exists");

        // Validate parent if provided
        if (dto.ParentId.HasValue)
        {
            var parent = await _structureRepo.GetByIdAsync(dto.ParentId.Value);
            if (parent == null)
                return ServiceResult<StructureDto>.Fail("Parent structure not found");
        }

        var structure = new Structure
        {
            Code = dto.Code,
            Name = dto.Name,
            NameAr = dto.NameAr,
            StructureType = structureType,
            ParentId = dto.ParentId,
            SortOrder = dto.SortOrder ?? 0,
            IsActive = true,
            CreatedBy = userId
        };

        var id = await _structureRepo.CreateAsync(structure);
        structure.Id = id;

        // Update paths
        await _structureRepo.UpdatePathsAsync(id);

        var created = await _structureRepo.GetByIdAsync(id);
        return ServiceResult<StructureDto>.Ok(await MapToStructureDtoAsync(created!));
    }

    public async Task<ServiceResult<StructureDto>> UpdateAsync(Guid id, UpdateStructureDto dto, Guid userId)
    {
        var structure = await _structureRepo.GetByIdAsync(id);
        if (structure == null)
            return ServiceResult<StructureDto>.Fail("Structure not found");

        if (!Enum.TryParse<StructureType>(dto.Type, true, out var structureType))
            return ServiceResult<StructureDto>.Fail("Invalid structure type");

        // Check code uniqueness if changed
        if (structure.Code != dto.Code)
        {
            var existing = await _structureRepo.GetByCodeAsync(dto.Code);
            if (existing != null && existing.Id != id)
                return ServiceResult<StructureDto>.Fail("Structure code already exists");
        }

        // Validate parent if changed
        if (dto.ParentId != structure.ParentId)
        {
            if (dto.ParentId.HasValue)
            {
                // Cannot set self as parent
                if (dto.ParentId.Value == id)
                    return ServiceResult<StructureDto>.Fail("Cannot set structure as its own parent");

                var parent = await _structureRepo.GetByIdAsync(dto.ParentId.Value);
                if (parent == null)
                    return ServiceResult<StructureDto>.Fail("Parent structure not found");

                // Cannot set a descendant as parent
                var descendants = await _structureRepo.GetDescendantsAsync(id);
                if (descendants.Any(d => d.Id == dto.ParentId.Value))
                    return ServiceResult<StructureDto>.Fail("Cannot set a descendant as parent");
            }
        }

        structure.Code = dto.Code;
        structure.Name = dto.Name;
        structure.NameAr = dto.NameAr;
        structure.StructureType = structureType;
        structure.ParentId = dto.ParentId;
        structure.IsActive = dto.IsActive;
        structure.SortOrder = dto.SortOrder ?? 0;
        structure.ModifiedBy = userId;
        structure.ModifiedAt = DateTime.Now;

        await _structureRepo.UpdateAsync(structure);

        // Update paths if parent changed
        await _structureRepo.UpdatePathsAsync(id);

        var updated = await _structureRepo.GetByIdAsync(id);
        return ServiceResult<StructureDto>.Ok(await MapToStructureDtoAsync(updated!));
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, Guid userId)
    {
        var structure = await _structureRepo.GetByIdAsync(id);
        if (structure == null)
            return ServiceResult.Fail("Structure not found");

        // Check if has children
        var children = await _structureRepo.GetChildrenAsync(id);
        if (children.Any())
            return ServiceResult.Fail("Cannot delete structure with children");

        // Check if has members
        var members = await _structureRepo.GetMembersAsync(id);
        if (members.Any())
            return ServiceResult.Fail("Cannot delete structure with members");

        await _structureRepo.DeleteAsync(id);
        return ServiceResult.Ok("Structure deleted");
    }

    #endregion

    #region Hierarchy Operations

    public async Task<ServiceResult<IEnumerable<StructureDto>>> GetChildrenAsync(Guid parentId)
    {
        var children = await _structureRepo.GetChildrenAsync(parentId);
        var dtos = new List<StructureDto>();
        foreach (var c in children)
        {
            dtos.Add(await MapToStructureDtoAsync(c));
        }
        return ServiceResult<IEnumerable<StructureDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<IEnumerable<StructureDto>>> GetDescendantsAsync(Guid parentId)
    {
        var descendants = await _structureRepo.GetDescendantsAsync(parentId);
        var dtos = new List<StructureDto>();
        foreach (var d in descendants)
        {
            dtos.Add(await MapToStructureDtoAsync(d));
        }
        return ServiceResult<IEnumerable<StructureDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<IEnumerable<StructureDto>>> GetAncestorsAsync(Guid structureId)
    {
        var ancestors = await _structureRepo.GetAncestorsAsync(structureId);
        var dtos = new List<StructureDto>();
        foreach (var a in ancestors)
        {
            dtos.Add(await MapToStructureDtoAsync(a));
        }
        return ServiceResult<IEnumerable<StructureDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<IEnumerable<StructureDto>>> GetRootStructuresAsync()
    {
        var roots = await _structureRepo.GetRootStructuresAsync();
        var dtos = new List<StructureDto>();
        foreach (var r in roots)
        {
            dtos.Add(await MapToStructureDtoAsync(r));
        }
        return ServiceResult<IEnumerable<StructureDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<IEnumerable<StructureDto>>> GetByTypeAsync(string type)
    {
        if (!Enum.TryParse<StructureType>(type, true, out var structureType))
            return ServiceResult<IEnumerable<StructureDto>>.Fail("Invalid structure type");

        var structures = await _structureRepo.GetByTypeAsync(structureType);
        var dtos = new List<StructureDto>();
        foreach (var s in structures)
        {
            dtos.Add(await MapToStructureDtoAsync(s));
        }
        return ServiceResult<IEnumerable<StructureDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<StructureTreeDto>> GetHierarchyTreeAsync()
    {
        var all = await _structureRepo.GetAllAsync(false);

        // Build tree recursively - wrap in a virtual root
        var tree = new StructureTreeDto
        {
            Id = Guid.Empty,
            Code = "ROOT",
            Name = "Organization",
            Type = "Root",
            IsActive = true,
            Children = BuildTreeRecursive(all.ToList(), null)
        };

        return ServiceResult<StructureTreeDto>.Ok(tree);
    }

    private List<StructureTreeDto> BuildTreeRecursive(List<Structure> all, Guid? parentId)
    {
        return all
            .Where(s => s.ParentId == parentId)
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Select(s => new StructureTreeDto
            {
                Id = s.Id,
                Code = s.Code ?? string.Empty,
                Name = s.Name,
                Type = s.StructureType.ToString(),
                IsActive = s.IsActive,
                Children = BuildTreeRecursive(all, s.Id)
            })
            .ToList();
    }

    #endregion

    #region Member Management

    public async Task<ServiceResult<IEnumerable<StructureMemberDto>>> GetMembersAsync(Guid structureId)
    {
        var structure = await _structureRepo.GetByIdAsync(structureId);
        if (structure == null)
            return ServiceResult<IEnumerable<StructureMemberDto>>.Fail("Structure not found");

        var members = await _structureRepo.GetMembersAsync(structureId);
        var dtos = new List<StructureMemberDto>();

        foreach (var m in members)
        {
            var user = await _userRepo.GetByIdAsync(m.UserId);
            dtos.Add(new StructureMemberDto
            {
                Id = m.Id,
                StructureId = m.StructureId,
                StructureName = structure.Name,
                UserId = m.UserId,
                UserName = user?.Username,
                UserDisplayName = user?.DisplayName,
                Position = m.Position,
                IsPrimary = m.IsPrimary,
                StartDate = m.StartDate ?? DateTime.MinValue,
                EndDate = m.EndDate,
                IsActive = m.EndDate == null || m.EndDate > DateTime.Now
            });
        }

        return ServiceResult<IEnumerable<StructureMemberDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<IEnumerable<StructureDto>>> GetUserStructuresAsync(Guid userId)
    {
        var structures = await _structureRepo.GetUserStructuresAsync(userId);
        var dtos = new List<StructureDto>();
        foreach (var s in structures)
        {
            dtos.Add(await MapToStructureDtoAsync(s));
        }
        return ServiceResult<IEnumerable<StructureDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<StructureDto?>> GetUserPrimaryStructureAsync(Guid userId)
    {
        var structure = await _structureRepo.GetUserPrimaryStructureAsync(userId);
        if (structure == null)
            return ServiceResult<StructureDto?>.Ok(null);

        return ServiceResult<StructureDto?>.Ok(await MapToStructureDtoAsync(structure));
    }

    public async Task<ServiceResult> AddMemberAsync(Guid structureId, AddStructureMemberDto dto, Guid userId)
    {
        var structure = await _structureRepo.GetByIdAsync(structureId);
        if (structure == null)
            return ServiceResult.Fail("Structure not found");

        var user = await _userRepo.GetByIdAsync(dto.UserId);
        if (user == null)
            return ServiceResult.Fail("User not found");

        var member = new StructureMember
        {
            StructureId = structureId,
            UserId = dto.UserId,
            Position = dto.Position,
            IsPrimary = dto.IsPrimary,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedBy = userId
        };

        await _structureRepo.AddMemberAsync(member);

        if (dto.IsPrimary)
        {
            await _structureRepo.SetPrimaryStructureAsync(dto.UserId, structureId);
        }

        return ServiceResult.Ok("Member added");
    }

    public async Task<ServiceResult> RemoveMemberAsync(Guid structureId, Guid memberId, Guid userId)
    {
        await _structureRepo.RemoveMemberAsync(structureId, memberId);
        return ServiceResult.Ok("Member removed");
    }

    public async Task<ServiceResult> SetPrimaryStructureAsync(Guid userId, Guid structureId, Guid performedBy)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return ServiceResult.Fail("User not found");

        var structure = await _structureRepo.GetByIdAsync(structureId);
        if (structure == null)
            return ServiceResult.Fail("Structure not found");

        await _structureRepo.SetPrimaryStructureAsync(userId, structureId);
        return ServiceResult.Ok("Primary structure set");
    }

    #endregion

    #region Private Helpers

    private async Task<StructureDto> MapToStructureDtoAsync(Structure s)
    {
        Structure? parent = null;
        if (s.ParentId.HasValue)
        {
            parent = await _structureRepo.GetByIdAsync(s.ParentId.Value);
        }

        var children = await _structureRepo.GetChildrenAsync(s.Id);
        var members = await _structureRepo.GetMembersAsync(s.Id);

        return new StructureDto
        {
            Id = s.Id,
            Code = s.Code ?? string.Empty,
            Name = s.Name,
            NameAr = s.NameAr,
            Type = s.StructureType.ToString(),
            ParentId = s.ParentId,
            ParentName = parent?.Name,
            Path = s.Path,
            Level = s.Level,
            IsActive = s.IsActive,
            SortOrder = s.SortOrder,
            CreatedAt = s.CreatedAt,
            MemberCount = members.Count(),
            ChildCount = children.Count()
        };
    }

    #endregion
}
