using System.Security.Cryptography;
using DMS.BL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(IConfiguration configuration, ILogger<FileStorageService> logger)
    {
        _logger = logger;
        _basePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
        _logger.LogInformation("FileStorageService initialized with base path: {BasePath}", _basePath);

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created storage directory: {BasePath}", _basePath);
        }
    }

    /// <summary>
    /// Computes SHA-256 hash of stream content (ISO 27001 integrity requirement).
    /// </summary>
    public async Task<string> ComputeHashAsync(Stream content)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(content);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return hashString;
    }

    /// <summary>
    /// Saves a file and returns the RELATIVE path (not absolute).
    /// The relative path format is: {documentId}/v{version}{extension}
    /// </summary>
    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, Guid documentId, int version)
    {
        // Create relative path: {documentId}/v{version}{extension}
        var extension = Path.GetExtension(fileName);
        var relativePath = Path.Combine(documentId.ToString(), $"v{version}{extension}");

        // Build absolute path for actual file operations
        var absolutePath = Path.Combine(_basePath, relativePath);
        var documentFolder = Path.GetDirectoryName(absolutePath)!;

        if (!Directory.Exists(documentFolder))
        {
            Directory.CreateDirectory(documentFolder);
            _logger.LogInformation("Created document folder: {DocumentFolder}", documentFolder);
        }

        _logger.LogInformation("Saving file. RelativePath: {RelativePath}, AbsolutePath: {AbsolutePath}, Stream Length: {Length}",
            relativePath, absolutePath, fileStream.Length);

        // Copy to memory stream first if the source stream doesn't support seeking
        Stream sourceStream = fileStream;
        if (!fileStream.CanSeek)
        {
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            sourceStream = memoryStream;
        }
        else if (sourceStream.Position != 0)
        {
            sourceStream.Position = 0;
        }

        using (var fs = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(fs);
            await fs.FlushAsync();
            _logger.LogInformation("File saved successfully, bytes written: {Bytes}", fs.Length);
        }

        // Dispose memory stream if we created one
        if (sourceStream != fileStream)
        {
            await sourceStream.DisposeAsync();
        }

        // Verify file was created
        if (File.Exists(absolutePath))
        {
            var fileInfo = new FileInfo(absolutePath);
            _logger.LogInformation("File verified: {AbsolutePath}, Size: {Size} bytes", absolutePath, fileInfo.Length);
        }
        else
        {
            _logger.LogError("File was NOT created at: {AbsolutePath}", absolutePath);
        }

        // Return RELATIVE path to be stored in database
        return relativePath;
    }

    /// <summary>
    /// Saves a file and returns storage result including integrity hash (ISO 27001 compliant).
    /// </summary>
    public async Task<FileStorageResult> SaveFileWithHashAsync(Stream fileStream, string fileName, Guid documentId, int version)
    {
        // Create relative path: {documentId}/v{version}{extension}
        var extension = Path.GetExtension(fileName);
        var relativePath = Path.Combine(documentId.ToString(), $"v{version}{extension}");

        // Build absolute path for actual file operations
        var absolutePath = Path.Combine(_basePath, relativePath);
        var documentFolder = Path.GetDirectoryName(absolutePath)!;

        if (!Directory.Exists(documentFolder))
        {
            Directory.CreateDirectory(documentFolder);
            _logger.LogInformation("Created document folder: {DocumentFolder}", documentFolder);
        }

        // Copy to memory stream first if the source stream doesn't support seeking
        Stream sourceStream = fileStream;
        MemoryStream? tempMemoryStream = null;

        if (!fileStream.CanSeek)
        {
            tempMemoryStream = new MemoryStream();
            await fileStream.CopyToAsync(tempMemoryStream);
            tempMemoryStream.Position = 0;
            sourceStream = tempMemoryStream;
        }
        else if (sourceStream.Position != 0)
        {
            sourceStream.Position = 0;
        }

        // Compute hash before saving (ISO 27001 integrity requirement)
        var hash = await ComputeHashAsync(sourceStream);
        sourceStream.Position = 0;

        var fileSize = sourceStream.Length;

        _logger.LogInformation(
            "Saving file with hash. RelativePath: {RelativePath}, Size: {Size}, Hash: {Hash}",
            relativePath, fileSize, hash);

        // Save to file
        using (var fs = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await sourceStream.CopyToAsync(fs);
            await fs.FlushAsync();
        }

        // Dispose temp memory stream if we created one
        if (tempMemoryStream != null)
        {
            await tempMemoryStream.DisposeAsync();
        }

        // Verify file was created and hash matches
        if (File.Exists(absolutePath))
        {
            using var verifyStream = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);
            var verifyHash = await ComputeHashAsync(verifyStream);

            if (verifyHash != hash)
            {
                _logger.LogError(
                    "File integrity verification failed after save. Expected: {Expected}, Got: {Got}",
                    hash, verifyHash);
                throw new IOException("File integrity verification failed after save");
            }

            _logger.LogInformation(
                "File saved and verified: {AbsolutePath}, Size: {Size}, Hash: {Hash}",
                absolutePath, fileSize, hash);
        }
        else
        {
            _logger.LogError("File was NOT created at: {AbsolutePath}", absolutePath);
            throw new IOException($"Failed to create file at: {absolutePath}");
        }

        return new FileStorageResult
        {
            StoragePath = relativePath,
            IntegrityHash = hash,
            HashAlgorithm = "SHA256",
            Size = fileSize,
            StoredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets a file using its relative path (or handles legacy absolute paths).
    /// </summary>
    public async Task<Stream?> GetFileAsync(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath))
        {
            _logger.LogError("Storage path is null or empty");
            return null;
        }

        // Determine the absolute path
        string absolutePath;

        // Check if it's already an absolute path (legacy data)
        if (Path.IsPathRooted(storagePath))
        {
            _logger.LogInformation("Detected absolute path (legacy): {StoragePath}", storagePath);
            absolutePath = storagePath;

            // If absolute path doesn't exist, try to extract relative part and use current base path
            if (!File.Exists(absolutePath))
            {
                // Try to extract the relative part (documentId/filename)
                var parts = storagePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    // Get last two parts: documentId folder and filename
                    var relativePath = Path.Combine(parts[^2], parts[^1]);
                    var newAbsolutePath = Path.Combine(_basePath, relativePath);

                    if (File.Exists(newAbsolutePath))
                    {
                        _logger.LogInformation("Found file using extracted relative path: {NewPath}", newAbsolutePath);
                        absolutePath = newAbsolutePath;
                    }
                }
            }
        }
        else
        {
            // It's a relative path - combine with base path
            absolutePath = Path.Combine(_basePath, storagePath);
            _logger.LogInformation("Using relative path: {StoragePath} -> {AbsolutePath}", storagePath, absolutePath);
        }

        if (!File.Exists(absolutePath))
        {
            _logger.LogError("File not found: {AbsolutePath} (original: {StoragePath}, basePath: {BasePath})",
                absolutePath, storagePath, _basePath);
            return null;
        }

        var memoryStream = new MemoryStream();
        using var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);
        await fs.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        _logger.LogInformation("File read successfully: {AbsolutePath}, Size: {Size} bytes", absolutePath, memoryStream.Length);
        return memoryStream;
    }

    public Task<bool> DeleteFileAsync(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath))
            return Task.FromResult(false);

        var absolutePath = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.Combine(_basePath, storagePath);

        if (!File.Exists(absolutePath))
            return Task.FromResult(false);

        File.Delete(absolutePath);
        _logger.LogInformation("File deleted: {AbsolutePath}", absolutePath);
        return Task.FromResult(true);
    }

    public Task<bool> FileExistsAsync(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath))
            return Task.FromResult(false);

        var absolutePath = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.Combine(_basePath, storagePath);

        return Task.FromResult(File.Exists(absolutePath));
    }

    /// <summary>
    /// Gets the absolute path for a storage path (for external use if needed).
    /// </summary>
    public string GetAbsolutePath(string storagePath)
    {
        if (string.IsNullOrEmpty(storagePath))
            return string.Empty;

        return Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.Combine(_basePath, storagePath);
    }
}
