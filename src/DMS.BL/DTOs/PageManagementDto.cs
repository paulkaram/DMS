namespace DMS.BL.DTOs;

// =============================================
// PDF Page Management DTOs
// =============================================

/// <summary>
/// Request to reorganize pages in a PDF document.
/// </summary>
public class PageReorganizeRequest
{
    public List<PageEntry> Pages { get; set; } = new();
    public string? Comment { get; set; }
}

/// <summary>
/// A single page entry in the reorganize manifest.
/// </summary>
public class PageEntry
{
    /// <summary>"existing" = from current PDF, "upload" = from uploaded file</summary>
    public string Source { get; set; } = "existing";

    /// <summary>For source="existing": 1-based page number from the current PDF.</summary>
    public int? OriginalPage { get; set; }

    /// <summary>For source="upload": 0-based index into the files[] array.</summary>
    public int? FileIndex { get; set; }

    /// <summary>For source="upload" with multi-page PDF: which page (1-based) to take.</summary>
    public int? UploadPageNumber { get; set; }
}

/// <summary>
/// Result of a page reorganize operation.
/// </summary>
public class PageReorganizeResult
{
    public Guid DocumentId { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSize { get; set; }
}
