namespace DMS.BL.DTOs;

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? NodeType { get; set; }
    public Guid? NodeId { get; set; }
    public string? NodeName { get; set; }
    public string? Details { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogQueryDto
{
    public Guid? NodeId { get; set; }
    public string? NodeType { get; set; }
    public Guid? UserId { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}
