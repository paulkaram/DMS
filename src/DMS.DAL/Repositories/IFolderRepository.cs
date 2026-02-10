using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IFolderRepository : IRepository<Folder>
{
    Task<IEnumerable<Folder>> GetByCabinetIdAsync(Guid cabinetId);
    Task<IEnumerable<Folder>> GetByParentIdAsync(Guid? parentId, Guid cabinetId);
    Task<IEnumerable<Folder>> GetTreeAsync(Guid cabinetId, int maxResults = 5000);
    Task<IEnumerable<Folder>> SearchAsync(string? name, Guid? cabinetId);
    Task<(List<Folder> Items, int TotalCount)> SearchPaginatedAsync(string? name, Guid? cabinetId, int page, int pageSize);
    Task<(List<Folder> Items, int TotalCount)> GetByParentIdPaginatedAsync(Guid? parentId, Guid cabinetId, int page, int pageSize);
    Task<string> GetPathAsync(Guid folderId);
    Task UpdatePathsAsync(Guid folderId, string newPath);
}
