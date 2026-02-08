namespace DMS.DAL.Entities;

public class ContentType
{
    public Guid Id { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Icon { get; set; }
    public bool AllowPreview { get; set; } = true;
    public bool AllowThumbnail { get; set; } = true;
    public int MaxFileSizeMB { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
