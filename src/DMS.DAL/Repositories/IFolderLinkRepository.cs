using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IFolderLinkRepository
{
    Task<IEnumerable<FolderLink>> GetBySourceFolderAsync(Guid sourceFolderId);
    Task<IEnumerable<FolderLink>> GetByTargetFolderAsync(Guid targetFolderId);
    Task<Guid> CreateAsync(FolderLink entity);
    Task<bool> DeleteAsync(Guid id);
}
