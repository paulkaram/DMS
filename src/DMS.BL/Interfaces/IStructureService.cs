using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IStructureService
{
    // Structure CRUD
    Task<ServiceResult<StructureDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<StructureDto>> GetByCodeAsync(string code);
    Task<ServiceResult<IEnumerable<StructureDto>>> GetAllAsync(bool includeInactive = false);
    Task<ServiceResult<StructureDto>> CreateAsync(CreateStructureDto dto, Guid userId);
    Task<ServiceResult<StructureDto>> UpdateAsync(Guid id, UpdateStructureDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id, Guid userId);

    // Hierarchy operations
    Task<ServiceResult<IEnumerable<StructureDto>>> GetChildrenAsync(Guid parentId);
    Task<ServiceResult<IEnumerable<StructureDto>>> GetDescendantsAsync(Guid parentId);
    Task<ServiceResult<IEnumerable<StructureDto>>> GetAncestorsAsync(Guid structureId);
    Task<ServiceResult<IEnumerable<StructureDto>>> GetRootStructuresAsync();
    Task<ServiceResult<IEnumerable<StructureDto>>> GetByTypeAsync(string type);
    Task<ServiceResult<StructureTreeDto>> GetHierarchyTreeAsync();

    // Member management
    Task<ServiceResult<IEnumerable<StructureMemberDto>>> GetMembersAsync(Guid structureId);
    Task<ServiceResult<IEnumerable<StructureDto>>> GetUserStructuresAsync(Guid userId);
    Task<ServiceResult<StructureDto?>> GetUserPrimaryStructureAsync(Guid userId);
    Task<ServiceResult> AddMemberAsync(Guid structureId, AddStructureMemberDto dto, Guid userId);
    Task<ServiceResult> RemoveMemberAsync(Guid structureId, Guid memberId, Guid userId);
    Task<ServiceResult> SetPrimaryStructureAsync(Guid userId, Guid structureId, Guid performedBy);
}
