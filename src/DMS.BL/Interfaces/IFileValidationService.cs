namespace DMS.BL.Interfaces;

/// <summary>
/// Service for validating uploaded files against allowed content types,
/// size limits, and security checks (magic bytes validation).
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validates a file for upload based on extension, size, and content signature.
    /// </summary>
    /// <param name="fileStream">The file stream to validate (will be read and reset to beginning)</param>
    /// <param name="fileName">The original file name</param>
    /// <param name="claimedContentType">The MIME type claimed by the client</param>
    /// <returns>A validation result with allowed status and any errors</returns>
    Task<FileValidationResult> ValidateFileAsync(Stream fileStream, string fileName, string claimedContentType);

    /// <summary>
    /// Gets the validated and corrected content type for a file based on its actual content.
    /// </summary>
    /// <param name="fileStream">The file stream to analyze</param>
    /// <param name="fileName">The original file name</param>
    /// <returns>The detected MIME type or null if unknown</returns>
    Task<string?> DetectContentTypeAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Checks if a file extension is in the blocked list (dangerous file types).
    /// </summary>
    bool IsBlockedExtension(string extension);

    /// <summary>
    /// Checks if a PDF stream contains PDF/A metadata markers (ISO 19005 compliance).
    /// </summary>
    Task<bool> CheckPdfAComplianceAsync(Stream pdfStream);
}

/// <summary>
/// Result of file validation including any errors or warnings.
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public string? Warning { get; set; }
    public string? ValidatedExtension { get; set; }
    public string? ValidatedMimeType { get; set; }
    public int? MaxFileSizeMB { get; set; }
    public bool IsContentTypeMatch { get; set; }

    public static FileValidationResult Success(string extension, string mimeType, int maxSizeMB) =>
        new()
        {
            IsValid = true,
            ValidatedExtension = extension,
            ValidatedMimeType = mimeType,
            MaxFileSizeMB = maxSizeMB,
            IsContentTypeMatch = true
        };

    public static FileValidationResult Fail(string error) =>
        new()
        {
            IsValid = false,
            Error = error
        };

    public static FileValidationResult SuccessWithWarning(string extension, string mimeType, int maxSizeMB, string warning) =>
        new()
        {
            IsValid = true,
            ValidatedExtension = extension,
            ValidatedMimeType = mimeType,
            MaxFileSizeMB = maxSizeMB,
            Warning = warning,
            IsContentTypeMatch = false
        };
}
