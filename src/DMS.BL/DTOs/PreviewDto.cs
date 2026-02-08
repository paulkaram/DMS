namespace DMS.BL.DTOs;

public class PreviewInfo
{
    public PreviewType Type { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? TextContent { get; set; }
}

public enum PreviewType
{
    Pdf,
    Image,
    Text,
    Office,
    Video,
    Audio,
    Unsupported
}
