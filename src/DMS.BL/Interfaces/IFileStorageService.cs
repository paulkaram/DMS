namespace DMS.BL.Interfaces;

/// <summary>
/// File storage service with ISO 27001 compliant integrity verification.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a file and returns the relative storage path.
    /// </summary>
    Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid documentId, int version);

    /// <summary>
    /// Saves a file and returns storage result including path and integrity hash.
    /// </summary>
    Task<FileStorageResult> SaveFileWithHashAsync(Stream fileStream, string fileName, Guid documentId, int version);

    /// <summary>
    /// Gets a file stream from storage.
    /// </summary>
    Task<Stream?> GetFileAsync(string storagePath);

    /// <summary>
    /// Gets a file stream from storage, decrypting if the file is encrypted.
    /// </summary>
    Task<Stream?> GetFileAsync(string storagePath, bool isEncrypted);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task<bool> DeleteFileAsync(string storagePath);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    Task<bool> FileExistsAsync(string storagePath);

    /// <summary>
    /// Computes SHA-256 hash of content for integrity verification.
    /// </summary>
    Task<string> ComputeHashAsync(Stream content);

    /// <summary>
    /// Gets the absolute path for a storage path.
    /// </summary>
    string GetAbsolutePath(string storagePath);
}

/// <summary>
/// Result of file storage operation including integrity hash.
/// </summary>
public class FileStorageResult
{
    public string StoragePath { get; set; } = string.Empty;
    public string IntegrityHash { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = "SHA256";
    public long Size { get; set; }
    public DateTime StoredAt { get; set; } = DateTime.Now;
    public bool IsEncrypted { get; set; }
}
