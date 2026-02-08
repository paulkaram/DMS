using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class PreviewService : IPreviewService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly IFileStorageService _fileStorageService;

    private static readonly HashSet<string> PdfExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".ico"
    };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".json", ".xml", ".html", ".htm", ".css", ".js", ".ts",
        ".vue", ".jsx", ".tsx", ".yaml", ".yml", ".ini", ".conf", ".cfg",
        ".log", ".sql", ".sh", ".bat", ".ps1", ".py", ".rb", ".java",
        ".cs", ".cpp", ".c", ".h", ".go", ".rs", ".swift", ".kt"
    };

    private static readonly HashSet<string> OfficeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".ods", ".odp"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov", ".avi", ".mkv"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a"
    };

    public PreviewService(
        IDocumentRepository documentRepository,
        IDocumentVersionRepository versionRepository,
        IFileStorageService fileStorageService)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<ServiceResult<PreviewInfo>> GetPreviewInfoAsync(Guid documentId, int? version = null)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<PreviewInfo>.Fail("Document not found");

        string? storagePath;
        long size;

        if (version.HasValue)
        {
            var versions = await _versionRepository.GetByDocumentIdAsync(documentId);
            var targetVersion = versions.FirstOrDefault(v => v.VersionNumber == version.Value);
            if (targetVersion == null)
                return ServiceResult<PreviewInfo>.Fail("Version not found");
            storagePath = targetVersion.StoragePath;
            size = targetVersion.Size;
        }
        else
        {
            storagePath = document.StoragePath;
            size = document.Size;
        }

        var previewType = GetPreviewType(document.Extension, document.ContentType);

        var previewInfo = new PreviewInfo
        {
            Type = previewType,
            ContentType = document.ContentType ?? GetContentType(document.Extension),
            FileName = document.Name + document.Extension,
            FileSize = size
        };

        // For text files, we can include the content directly
        if (previewType == PreviewType.Text && !string.IsNullOrEmpty(storagePath))
        {
            try
            {
                var stream = await _fileStorageService.GetFileAsync(storagePath);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    // Limit text content to 500KB for preview
                    var content = new char[512 * 1024];
                    var length = await reader.ReadAsync(content, 0, content.Length);
                    previewInfo.TextContent = new string(content, 0, length);
                }
            }
            catch
            {
                // If we can't read the text, it's fine - preview will fall back to download
            }
        }

        return ServiceResult<PreviewInfo>.Ok(previewInfo);
    }

    public PreviewType GetPreviewType(string? extension, string? contentType)
    {
        if (string.IsNullOrEmpty(extension))
            return PreviewType.Unsupported;

        if (PdfExtensions.Contains(extension))
            return PreviewType.Pdf;

        if (ImageExtensions.Contains(extension))
            return PreviewType.Image;

        if (TextExtensions.Contains(extension))
            return PreviewType.Text;

        if (OfficeExtensions.Contains(extension))
            return PreviewType.Office;

        if (VideoExtensions.Contains(extension))
            return PreviewType.Video;

        if (AudioExtensions.Contains(extension))
            return PreviewType.Audio;

        // Check content type as fallback
        if (!string.IsNullOrEmpty(contentType))
        {
            if (contentType.StartsWith("image/"))
                return PreviewType.Image;
            if (contentType.StartsWith("text/"))
                return PreviewType.Text;
            if (contentType.StartsWith("video/"))
                return PreviewType.Video;
            if (contentType.StartsWith("audio/"))
                return PreviewType.Audio;
            if (contentType == "application/pdf")
                return PreviewType.Pdf;
        }

        return PreviewType.Unsupported;
    }

    public bool CanPreview(string? extension)
    {
        return GetPreviewType(extension, null) != PreviewType.Unsupported;
    }

    private static string GetContentType(string? extension)
    {
        if (string.IsNullOrEmpty(extension))
            return "application/octet-stream";

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            ".txt" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".md" => "text/markdown",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            _ => "application/octet-stream"
        };
    }
}
