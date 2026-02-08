using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IFolderTemplateService
{
    // Template management
    Task<ServiceResult<List<FolderTemplateDto>>> GetAllAsync(bool includeInactive = false);
    Task<ServiceResult<FolderTemplateDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<List<FolderTemplateDto>>> GetByCategoryAsync(string category);
    Task<ServiceResult<FolderTemplateDto>> GetDefaultAsync();
    Task<ServiceResult<FolderTemplateDto>> CreateAsync(CreateFolderTemplateDto dto, Guid userId);
    Task<ServiceResult<FolderTemplateDto>> UpdateAsync(Guid id, UpdateFolderTemplateDto dto, Guid userId);
    Task<ServiceResult> DeleteAsync(Guid id);

    // Node management
    Task<ServiceResult<FolderTemplateNodeDto>> AddNodeAsync(Guid templateId, CreateTemplateNodeDto dto);
    Task<ServiceResult<FolderTemplateNodeDto>> UpdateNodeAsync(Guid nodeId, UpdateTemplateNodeDto dto);
    Task<ServiceResult> DeleteNodeAsync(Guid nodeId);

    // Application
    Task<ServiceResult<ApplyTemplateResultDto>> ApplyTemplateAsync(Guid targetFolderId, ApplyTemplateDto dto, Guid userId);
    Task<ServiceResult<ApplyTemplateResultDto>> ApplyTemplateToCabinetAsync(Guid cabinetId, ApplyTemplateDto dto, Guid userId);
    Task<ServiceResult<ApplyTemplateResultDto>> PreviewTemplateAsync(Guid targetFolderId, Guid templateId);
    Task<ServiceResult<ApplyTemplateResultDto>> PreviewTemplateToCabinetAsync(Guid cabinetId, Guid templateId);

    // Utilities
    Task<ServiceResult<List<string>>> GetCategoriesAsync();
    Task<ServiceResult<FolderTemplateDto>> DuplicateAsync(Guid templateId, string newName, Guid userId);
    Task<ServiceResult<List<FolderTemplateUsageDto>>> GetUsageHistoryAsync(Guid templateId);
}
