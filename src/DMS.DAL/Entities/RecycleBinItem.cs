namespace DMS.DAL.Entities;

public class RecycleBinItem
{
    public Guid Id { get; set; }
    public int NodeType { get; set; } // 1=Cabinet, 2=Folder, 3=Document
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string? OriginalPath { get; set; }
    public Guid? OriginalParentId { get; set; }
    public Guid DeletedBy { get; set; }
    public DateTime DeletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Metadata { get; set; }

    // Navigation properties (for display purposes)
    public string? DeletedByUserName { get; set; }
}
