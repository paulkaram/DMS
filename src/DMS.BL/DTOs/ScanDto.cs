namespace DMS.BL.DTOs;

public class ScanProcessRequest
{
    public Guid? ScanConfigId { get; set; }
    public Guid TargetFolderId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<ScanPageInstruction> Pages { get; set; } = new();

    // Settings (overridden by ScanConfig if ScanConfigId is set)
    public bool EnableOCR { get; set; } = true;
    public string OcrLanguage { get; set; } = "eng";
    public bool AutoDeskew { get; set; } = true;
    public bool AutoCrop { get; set; } = false;
    public int CompressionQuality { get; set; } = 85;
}

public class ScanPageInstruction
{
    public int FileIndex { get; set; }
    public int RotationDegrees { get; set; } = 0;
}

public class ScanProcessResult
{
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSize { get; set; }
    public bool OcrApplied { get; set; }
    public string? OcrText { get; set; }
}

public class ScanOptions
{
    public const string SectionName = "Scan";
    public string TessDataPath { get; set; } = "./tessdata";
    public string DefaultLanguage { get; set; } = "eng";
    public int MaxPagesPerScan { get; set; } = 100;
    public string TempDirectory { get; set; } = "./Storage/temp-scan";
    public int DefaultCompressionQuality { get; set; } = 85;
}
