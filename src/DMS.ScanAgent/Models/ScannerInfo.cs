namespace DMS.ScanAgent.Models;

public class ScannerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
}

public class ScanRequest
{
    public string ScannerId { get; set; } = string.Empty;
    public string Driver { get; set; } = "wia";
    public int Dpi { get; set; } = 300;
    public string ColorMode { get; set; } = "color";
    public string PageSize { get; set; } = "A4";
    public string PaperSource { get; set; } = "flatbed";
    public bool Duplex { get; set; } = false;
}

public class ScanResponse
{
    public List<ScannedPage> Pages { get; set; } = new();
}

public class ScannedPage
{
    public string Data { get; set; } = string.Empty; // base64
    public string Format { get; set; } = "jpeg";
}

public class AgentStatus
{
    public string Version { get; set; } = "1.0.0";
    public string Status { get; set; } = "ready";
}
