using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByFolderIdAsync(Guid folderId);
    Task<IEnumerable<DocumentWithNames>> GetByFolderIdWithNamesAsync(Guid folderId);
    Task<IEnumerable<Document>> SearchAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId);
    Task<IEnumerable<DocumentWithNames>> SearchWithNamesAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId);
    Task<(List<DocumentWithNames> Items, int TotalCount)> SearchWithNamesPaginatedAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId, int page, int pageSize, int? userPrivacyLevel = null);
    Task<IEnumerable<Document>> GetCheckedOutByUserAsync(Guid userId);
    Task<IEnumerable<DocumentWithNames>> GetCheckedOutByUserWithNamesAsync(Guid userId);
    Task<IEnumerable<Document>> GetCreatedByUserAsync(Guid userId, int take = 50);
    Task<IEnumerable<DocumentWithNames>> GetCreatedByUserWithNamesAsync(Guid userId, int take = 50);
    Task<bool> CheckOutAsync(Guid id, Guid userId);
    Task<bool> CheckInAsync(Guid id, Guid userId, string? comment);
    Task<bool> DiscardCheckOutAsync(Guid id);
    Task<Document?> GetWithVersionsAsync(Guid id);
}
