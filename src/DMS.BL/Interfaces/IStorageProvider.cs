namespace DMS.BL.Interfaces;

/// <summary>
/// Storage provider abstraction for file persistence.
/// Supports standard filesystem and WORM (Write Once Read Many) immutable storage.
/// </summary>
public interface IStorageProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider supports immutable/WORM storage.
    /// </summary>
    bool IsImmutable { get; }

    Task<string> SaveAsync(Stream content, string relativePath);
    Task<Stream?> GetAsync(string relativePath);
    Task<bool> ExistsAsync(string relativePath);
    Task<bool> DeleteAsync(string relativePath);
}

/// <summary>
/// WORM storage configuration for compliance requirements (NCA/NCAR).
/// </summary>
public class WormStorageConfig
{
    /// <summary>
    /// Whether WORM storage is enabled for Record/Archived state documents.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Provider type: "filesystem" (default), "azure-immutable", "s3-object-lock"
    /// </summary>
    public string Provider { get; set; } = "filesystem";

    /// <summary>
    /// Base path for WORM storage (filesystem provider).
    /// </summary>
    public string BasePath { get; set; } = string.Empty;

    /// <summary>
    /// Immutability retention period in days (for cloud providers).
    /// </summary>
    public int RetentionDays { get; set; } = 2555; // ~7 years default
}
