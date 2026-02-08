namespace DMS.DAL.Entities;

public class ActivityLog
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public NodeType? NodeType { get; set; }
    public Guid? NodeId { get; set; }
    public string? NodeName { get; set; }
    public string? Details { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public static class ActivityActions
{
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Viewed = "Viewed";
    public const string Downloaded = "Downloaded";
    public const string CheckedOut = "CheckedOut";
    public const string CheckedIn = "CheckedIn";
    public const string DiscardedCheckout = "DiscardedCheckout";
    public const string Moved = "Moved";
    public const string Copied = "Copied";
    public const string PermissionGranted = "PermissionGranted";
    public const string PermissionRevoked = "PermissionRevoked";
    public const string VersionCreated = "VersionCreated";
    public const string VersionRestored = "VersionRestored";
}
