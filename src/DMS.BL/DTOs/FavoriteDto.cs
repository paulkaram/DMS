namespace DMS.BL.DTOs;

public class FavoriteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int NodeType { get; set; }
    public Guid NodeId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Display info from related entity
    public string? NodeName { get; set; }
    public string? NodePath { get; set; }
}

public class FavoriteItemDto
{
    public Guid Id { get; set; }
    public int NodeType { get; set; }
    public Guid NodeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Path { get; set; }
    public Guid? ParentFolderId { get; set; }
    public Guid? CabinetId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime FavoritedAt { get; set; }
}

public class ToggleFavoriteRequest
{
    public int NodeType { get; set; }
    public Guid NodeId { get; set; }
}
