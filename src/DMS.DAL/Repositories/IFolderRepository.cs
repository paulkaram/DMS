using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IFolderRepository : IRepository<Folder>
{
    Task<IEnumerable<Folder>> GetByCabinetIdAsync(Guid cabinetId);
    Task<IEnumerable<Folder>> GetByParentIdAsync(Guid? parentId, Guid cabinetId);
    Task<IEnumerable<Folder>> GetTreeAsync(Guid cabinetId);
    Task<IEnumerable<Folder>> SearchAsync(string? name, Guid? cabinetId);
    Task<string> GetPathAsync(Guid folderId);
    Task UpdatePathsAsync(Guid folderId, string newPath);
}
