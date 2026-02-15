using DMS.DAL.DTOs;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IFolderRepository : IRepository<Folder>
{
    Task<PagedResult<Folder>> GetAllPagedAsync(int page = 1, int pageSize = 50);
    Task<IEnumerable<Folder>> GetByCabinetIdAsync(Guid cabinetId, int? userPrivacyLevel = null);
    Task<IEnumerable<Folder>> GetByParentIdAsync(Guid? parentId, Guid cabinetId, int? userPrivacyLevel = null);
    Task<IEnumerable<Folder>> GetTreeAsync(Guid cabinetId, int maxResults = 5000, int? userPrivacyLevel = null);
    Task<IEnumerable<Folder>> SearchAsync(string? name, Guid? cabinetId, int? userPrivacyLevel = null);
    Task<(List<Folder> Items, int TotalCount)> SearchPaginatedAsync(string? name, Guid? cabinetId, int page, int pageSize, int? userPrivacyLevel = null);
    Task<(List<Folder> Items, int TotalCount)> GetByParentIdPaginatedAsync(Guid? parentId, Guid cabinetId, int page, int pageSize, int? userPrivacyLevel = null);
    Task<string> GetPathAsync(Guid folderId);
    Task UpdatePathsAsync(Guid folderId, string newPath);
}
