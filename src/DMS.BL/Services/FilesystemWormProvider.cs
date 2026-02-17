using DMS.BL.Interfaces;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

/// <summary>
/// Filesystem-based WORM storage provider.
/// Files are stored in a separate directory and marked read-only to prevent modification.
/// For true WORM compliance, use Azure Immutable Blob or S3 Object Lock in production.
/// </summary>
public class FilesystemWormProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<FilesystemWormProvider> _logger;

    public string ProviderName => "filesystem-worm";
    public bool IsImmutable => true;

    public FilesystemWormProvider(string basePath, ILogger<FilesystemWormProvider> logger)
    {
        _basePath = basePath;
        _logger = logger;

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created WORM storage directory: {BasePath}", _basePath);
        }
    }

    public async Task<string> SaveAsync(Stream content, string relativePath)
    {
        var absolutePath = Path.Combine(_basePath, relativePath);
        var directory = Path.GetDirectoryName(absolutePath)!;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Write file
        using (var fs = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs);
            await fs.FlushAsync();
        }

        // Set read-only attribute to simulate immutability
        File.SetAttributes(absolutePath, FileAttributes.ReadOnly);

        _logger.LogInformation("File saved to WORM storage: {Path} (read-only)", absolutePath);
        return relativePath;
    }

    public async Task<Stream?> GetAsync(string relativePath)
    {
        var absolutePath = Path.Combine(_basePath, relativePath);
        if (!File.Exists(absolutePath))
            return null;

        var memoryStream = new MemoryStream();
        using var fs = new FileStream(absolutePath, FileMode.Open, FileAccess.Read);
        await fs.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public Task<bool> ExistsAsync(string relativePath)
    {
        var absolutePath = Path.Combine(_basePath, relativePath);
        return Task.FromResult(File.Exists(absolutePath));
    }

    public Task<bool> DeleteAsync(string relativePath)
    {
        // WORM storage does not support deletion
        _logger.LogWarning("Delete attempted on WORM storage — operation denied: {Path}", relativePath);
        return Task.FromResult(false);
    }
}
