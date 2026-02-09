using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentShortcutRepository
{
    Task<IEnumerable<DocumentShortcut>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DocumentShortcut>> GetByFolderIdAsync(Guid folderId);
    Task<DocumentShortcut?> GetByDocumentAndFolderAsync(Guid documentId, Guid folderId);
    Task<Guid> CreateAsync(DocumentShortcut entity);
    Task<bool> DeleteAsync(Guid id);
    Task<int> DeleteAllByDocumentIdAsync(Guid documentId);
}
