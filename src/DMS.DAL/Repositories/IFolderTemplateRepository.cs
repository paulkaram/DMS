using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IFolderTemplateRepository
{
    // Template CRUD
    Task<IEnumerable<FolderTemplate>> GetAllAsync(bool includeInactive = false);
    Task<FolderTemplate?> GetByIdAsync(Guid id);
    Task<FolderTemplate?> GetByIdWithNodesAsync(Guid id);
    Task<IEnumerable<FolderTemplate>> GetByCategoryAsync(string category);
    Task<FolderTemplate?> GetDefaultAsync();
    Task<Guid> CreateAsync(FolderTemplate template);
    Task<bool> UpdateAsync(FolderTemplate template);
    Task<bool> DeleteAsync(Guid id);

    // Node management
    Task<IEnumerable<FolderTemplateNode>> GetNodesByTemplateIdAsync(Guid templateId);
    Task<Guid> CreateNodeAsync(FolderTemplateNode node);
    Task<bool> UpdateNodeAsync(FolderTemplateNode node);
    Task<bool> DeleteNodeAsync(Guid nodeId);
    Task DeleteAllNodesByTemplateIdAsync(Guid templateId);

    // Usage tracking
    Task<Guid> RecordUsageAsync(FolderTemplateUsage usage);
    Task<IEnumerable<FolderTemplateUsage>> GetUsageByTemplateIdAsync(Guid templateId);
    Task<int> GetUsageCountAsync(Guid templateId);

    // Utilities
    Task<IEnumerable<string>> GetCategoriesAsync();
}
