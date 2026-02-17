using DMS.BL.Interfaces;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

/// <summary>
/// Validates uploaded files against allowed content types, size limits,
/// and performs security checks using magic bytes (file signatures).
/// </summary>
public class FileValidationService : IFileValidationService
{
    private readonly IContentTypeRepository _contentTypeRepository;

    // Blocked extensions - executable and potentially dangerous files
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Windows executables
        ".exe", ".dll", ".com", ".scr", ".pif", ".msi", ".msp",
        // Scripts
        ".bat", ".cmd", ".ps1", ".psm1", ".psd1", ".vbs", ".vbe", ".js", ".jse", ".ws", ".wsf", ".wsc", ".wsh",
        // Shell scripts
        ".sh", ".bash", ".csh", ".ksh", ".zsh",
        // Other dangerous
        ".reg", ".inf", ".hta", ".cpl", ".msc",
        // Java/JAR
        ".jar", ".class",
        // Office macros (standalone)
        ".docm", ".xlsm", ".pptm", ".dotm", ".xltm", ".potm",
        // Archives that can contain executables (sometimes blocked)
        // ".zip", ".rar", ".7z", ".tar", ".gz" - typically allowed but monitored
    };

    // Magic bytes (file signatures) for common file types
    // Format: extension -> list of (magic bytes, offset)
    private static readonly Dictionary<string, List<(byte[] Signature, int Offset)>> MagicBytes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Images
        { ".jpg", new List<(byte[], int)> { (new byte[] { 0xFF, 0xD8, 0xFF }, 0) } },
        { ".jpeg", new List<(byte[], int)> { (new byte[] { 0xFF, 0xD8, 0xFF }, 0) } },
        { ".png", new List<(byte[], int)> { (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0) } },
        { ".gif", new List<(byte[], int)> { (new byte[] { 0x47, 0x49, 0x46, 0x38 }, 0) } }, // GIF87a or GIF89a
        { ".bmp", new List<(byte[], int)> { (new byte[] { 0x42, 0x4D }, 0) } },
        { ".webp", new List<(byte[], int)> { (new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0) } }, // RIFF header
        { ".ico", new List<(byte[], int)> { (new byte[] { 0x00, 0x00, 0x01, 0x00 }, 0) } },
        { ".tif", new List<(byte[], int)> { (new byte[] { 0x49, 0x49, 0x2A, 0x00 }, 0), (new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, 0) } },
        { ".tiff", new List<(byte[], int)> { (new byte[] { 0x49, 0x49, 0x2A, 0x00 }, 0), (new byte[] { 0x4D, 0x4D, 0x00, 0x2A }, 0) } },

        // Documents
        { ".pdf", new List<(byte[], int)> { (new byte[] { 0x25, 0x50, 0x44, 0x46 }, 0) } }, // %PDF

        // Microsoft Office (modern - ZIP-based OOXML)
        { ".docx", new List<(byte[], int)> { (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 0) } }, // PK (ZIP)
        { ".xlsx", new List<(byte[], int)> { (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 0) } },
        { ".pptx", new List<(byte[], int)> { (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 0) } },

        // Microsoft Office (legacy - OLE compound documents)
        { ".doc", new List<(byte[], int)> { (new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, 0) } },
        { ".xls", new List<(byte[], int)> { (new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, 0) } },
        { ".ppt", new List<(byte[], int)> { (new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, 0) } },

        // Archives
        { ".zip", new List<(byte[], int)> { (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, 0), (new byte[] { 0x50, 0x4B, 0x05, 0x06 }, 0) } },
        { ".rar", new List<(byte[], int)> { (new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }, 0) } },
        { ".7z", new List<(byte[], int)> { (new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, 0) } },
        { ".gz", new List<(byte[], int)> { (new byte[] { 0x1F, 0x8B }, 0) } },
        { ".tar", new List<(byte[], int)> { (new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 }, 257) } }, // "ustar" at offset 257

        // Audio
        { ".mp3", new List<(byte[], int)> { (new byte[] { 0x49, 0x44, 0x33 }, 0), (new byte[] { 0xFF, 0xFB }, 0), (new byte[] { 0xFF, 0xFA }, 0) } },
        { ".wav", new List<(byte[], int)> { (new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0) } }, // RIFF
        { ".flac", new List<(byte[], int)> { (new byte[] { 0x66, 0x4C, 0x61, 0x43 }, 0) } },
        { ".ogg", new List<(byte[], int)> { (new byte[] { 0x4F, 0x67, 0x67, 0x53 }, 0) } },

        // Video
        { ".mp4", new List<(byte[], int)> { (new byte[] { 0x00, 0x00, 0x00 }, 0) } }, // Simplified - ftyp at offset 4
        { ".avi", new List<(byte[], int)> { (new byte[] { 0x52, 0x49, 0x46, 0x46 }, 0) } }, // RIFF
        { ".mkv", new List<(byte[], int)> { (new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, 0) } },
        { ".webm", new List<(byte[], int)> { (new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, 0) } },
        { ".mov", new List<(byte[], int)> { (new byte[] { 0x00, 0x00, 0x00 }, 0) } }, // ftyp variations

        // Text/Code (no magic bytes - these are text files)
        // ".txt", ".csv", ".json", ".xml", ".html", ".css", ".md" - validated by extension only

        // Executables (BLOCKED but included for detection)
        { ".exe", new List<(byte[], int)> { (new byte[] { 0x4D, 0x5A }, 0) } }, // MZ header
        { ".dll", new List<(byte[], int)> { (new byte[] { 0x4D, 0x5A }, 0) } },
    };

    // MIME types that are text-based and don't have magic bytes
    private static readonly HashSet<string> TextBasedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".csv", ".json", ".xml", ".html", ".htm", ".css", ".js", ".ts",
        ".md", ".yaml", ".yml", ".ini", ".cfg", ".conf", ".log", ".sql", ".rtf"
    };

    public FileValidationService(IContentTypeRepository contentTypeRepository)
    {
        _contentTypeRepository = contentTypeRepository;
    }

    public async Task<FileValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string claimedContentType)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension))
            return FileValidationResult.Fail("File must have an extension");

        // Check if extension is blocked
        if (IsBlockedExtension(extension))
            return FileValidationResult.Fail($"File type '{extension}' is not allowed for security reasons");

        // Get allowed content type from database
        var allowedType = await _contentTypeRepository.GetByExtensionAsync(extension);
        if (allowedType == null)
            return FileValidationResult.Fail($"File type '{extension}' is not in the list of allowed file types");

        if (!allowedType.IsActive)
            return FileValidationResult.Fail($"File type '{extension}' is currently disabled");

        // Check file size
        var fileSizeBytes = fileStream.Length;
        var fileSizeMB = fileSizeBytes / (1024.0 * 1024.0);
        if (fileSizeMB > allowedType.MaxFileSizeMB)
            return FileValidationResult.Fail($"File size ({fileSizeMB:F2} MB) exceeds the maximum allowed size of {allowedType.MaxFileSizeMB} MB for {extension} files");

        // Validate magic bytes (file signature) if applicable
        if (!TextBasedExtensions.Contains(extension))
        {
            var magicBytesValid = await ValidateMagicBytesAsync(fileStream, extension);
            if (!magicBytesValid)
            {
                // Check if the file might be a disguised executable
                var isExecutable = await IsExecutableContentAsync(fileStream);
                if (isExecutable)
                    return FileValidationResult.Fail("File content appears to be an executable disguised as another file type. Upload rejected for security reasons.");

                // Not an executable, but content doesn't match extension - warn but allow
                return FileValidationResult.SuccessWithWarning(
                    extension,
                    allowedType.MimeType,
                    allowedType.MaxFileSizeMB,
                    $"File content signature does not match the '{extension}' extension. The file may be corrupted or mislabeled.");
            }
        }

        return FileValidationResult.Success(extension, allowedType.MimeType, allowedType.MaxFileSizeMB);
    }

    public async Task<string?> DetectContentTypeAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return null;

        var allowedType = await _contentTypeRepository.GetByExtensionAsync(extension);
        return allowedType?.MimeType;
    }

    public bool IsBlockedExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return false;

        // Ensure extension starts with dot
        if (!extension.StartsWith('.'))
            extension = "." + extension;

        return BlockedExtensions.Contains(extension);
    }

    /// <summary>
    /// Validates that the file's magic bytes match the expected signature for the extension.
    /// </summary>
    private async Task<bool> ValidateMagicBytesAsync(Stream fileStream, string extension)
    {
        if (!MagicBytes.TryGetValue(extension, out var signatures))
            return true; // No signature defined, allow

        // Store original position and seek to beginning
        var originalPosition = fileStream.Position;
        fileStream.Position = 0;

        try
        {
            // Find the maximum offset + signature length we need to read
            var maxReadLength = signatures.Max(s => s.Offset + s.Signature.Length);
            var buffer = new byte[Math.Max(maxReadLength, 512)]; // Read at least 512 bytes

            var bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
                return false;

            // Check if any of the signatures match
            foreach (var (signature, offset) in signatures)
            {
                if (bytesRead < offset + signature.Length)
                    continue;

                var matches = true;
                for (var i = 0; i < signature.Length; i++)
                {
                    if (buffer[offset + i] != signature[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return true;
            }

            return false;
        }
        finally
        {
            // Reset stream position
            fileStream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Checks for PDF/A compliance by looking for XMP metadata markers in the PDF.
    /// This is a lightweight check — looks for pdfaid:part in XMP metadata.
    /// For full verification, an external tool like VeraPDF would be needed.
    /// </summary>
    public async Task<bool> CheckPdfAComplianceAsync(Stream pdfStream)
    {
        var originalPosition = pdfStream.Position;
        pdfStream.Position = 0;

        try
        {
            // Read the PDF content as text to search for PDF/A XMP metadata markers
            using var reader = new StreamReader(pdfStream, System.Text.Encoding.Latin1, leaveOpen: true);
            var content = await reader.ReadToEndAsync();

            // PDF/A files contain XMP metadata with pdfaid namespace
            // Look for: <pdfaid:part>1</pdfaid:part> (PDF/A-1)
            //           <pdfaid:part>2</pdfaid:part> (PDF/A-2)
            //           <pdfaid:part>3</pdfaid:part> (PDF/A-3)
            return content.Contains("pdfaid:part", StringComparison.OrdinalIgnoreCase) ||
                   content.Contains("PDF/A", StringComparison.Ordinal);
        }
        finally
        {
            pdfStream.Position = originalPosition;
        }
    }

    /// <summary>
    /// Checks if the file content appears to be an executable (PE format).
    /// This is used to detect disguised executables.
    /// </summary>
    private async Task<bool> IsExecutableContentAsync(Stream fileStream)
    {
        var originalPosition = fileStream.Position;
        fileStream.Position = 0;

        try
        {
            var buffer = new byte[2];
            var bytesRead = await fileStream.ReadAsync(buffer, 0, 2);

            if (bytesRead < 2)
                return false;

            // Check for MZ header (DOS/Windows executables)
            return buffer[0] == 0x4D && buffer[1] == 0x5A;
        }
        finally
        {
            fileStream.Position = originalPosition;
        }
    }
}
