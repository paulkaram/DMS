namespace DMS.BL.DTOs;

public class RecycleBinItemDto
{
    public Guid Id { get; set; }
    public int NodeType { get; set; }
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string? OriginalPath { get; set; }
    public Guid? OriginalParentId { get; set; }
    public Guid DeletedBy { get; set; }
    public DateTime DeletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Display info
    public string? DeletedByUserName { get; set; }
    public string NodeTypeName => NodeType switch
    {
        1 => "Cabinet",
        2 => "Folder",
        3 => "Document",
        _ => "Unknown"
    };
}

public class RestoreRequest
{
    public Guid Id { get; set; }
    public Guid? RestoreToFolderId { get; set; }
}
