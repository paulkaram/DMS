using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

/// <summary>
/// Repository for managing version-specific metadata snapshots.
/// Enables metadata comparison between document versions.
/// </summary>
public interface IDocumentVersionMetadataRepository
{
    /// <summary>
    /// Gets all metadata for a specific version.
    /// </summary>
    Task<IEnumerable<DocumentVersionMetadata>> GetByVersionIdAsync(Guid versionId);

    /// <summary>
    /// Gets all metadata snapshots for a document across all versions.
    /// </summary>
    Task<IEnumerable<DocumentVersionMetadata>> GetByDocumentIdAsync(Guid documentId);

    /// <summary>
    /// Gets metadata for multiple versions at once (for comparison).
    /// </summary>
    Task<Dictionary<Guid, List<DocumentVersionMetadata>>> GetMetadataForVersionsAsync(
        IEnumerable<Guid> versionIds);

    /// <summary>
    /// Saves a metadata snapshot for a version.
    /// </summary>
    Task SaveVersionMetadataAsync(Guid versionId, Guid documentId,
        IEnumerable<DocumentVersionMetadata> metadata);

    /// <summary>
    /// Copies current document metadata to a new version snapshot.
    /// </summary>
    Task<bool> SnapshotCurrentMetadataToVersionAsync(Guid documentId, Guid versionId);
}
