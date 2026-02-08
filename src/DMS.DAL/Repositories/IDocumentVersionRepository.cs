using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IDocumentVersionRepository
{
    Task<DocumentVersion?> GetByIdAsync(Guid id);
    Task<IEnumerable<DocumentVersion>> GetByDocumentIdAsync(Guid documentId);
    Task<DocumentVersion?> GetLatestVersionAsync(Guid documentId);
    Task<Guid> CreateAsync(DocumentVersion entity);
    Task<bool> UpdateAsync(DocumentVersion entity);

    /// <summary>
    /// Gets a specific version by major and minor version numbers.
    /// </summary>
    Task<DocumentVersion?> GetByVersionNumberAsync(Guid documentId, int majorVersion, int minorVersion);

    /// <summary>
    /// Gets the latest major version (e.g., 2.0, ignoring 2.1, 2.2).
    /// </summary>
    Task<DocumentVersion?> GetLatestMajorVersionAsync(Guid documentId);

    /// <summary>
    /// Gets all major versions for a document (published releases).
    /// </summary>
    Task<IEnumerable<DocumentVersion>> GetMajorVersionsAsync(Guid documentId);

    /// <summary>
    /// Gets minor versions for a specific major version (e.g., all 2.x versions).
    /// </summary>
    Task<IEnumerable<DocumentVersion>> GetMinorVersionsAsync(Guid documentId, int majorVersion);
}
