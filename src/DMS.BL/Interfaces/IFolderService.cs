using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IFolderService
{
    Task<ServiceResult<FolderDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<FolderDto>>> GetByCabinetIdAsync(Guid cabinetId, int? userPrivacyLevel = null);
    Task<ServiceResult<List<FolderDto>>> GetByParentIdAsync(Guid? parentId, Guid cabinetId, int? userPrivacyLevel = null);
    Task<ServiceResult<List<FolderDto>>> GetTreeAsync(Guid cabinetId, int? userPrivacyLevel = null);
    Task<ServiceResult<List<FolderDto>>> SearchAsync(string? name, Guid? cabinetId, int? userPrivacyLevel = null);
    Task<ServiceResult<PagedResultDto<FolderDto>>> SearchPaginatedAsync(string? name, Guid? cabinetId, int page, int pageSize, int? userPrivacyLevel = null);
    Task<ServiceResult<PagedResultDto<FolderDto>>> GetByParentIdPaginatedAsync(Guid? parentId, Guid cabinetId, int page, int pageSize, int? userPrivacyLevel = null);
    Task<ServiceResult<FolderDto>> CreateAsync(CreateFolderDto dto, Guid userId);
    Task<ServiceResult<FolderDto>> UpdateAsync(Guid id, UpdateFolderDto dto, Guid userId);
    Task<ServiceResult> MoveAsync(Guid id, MoveFolderDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id, Guid userId);
}
