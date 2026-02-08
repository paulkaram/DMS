namespace DMS.DAL.Entities;

public class Vacation
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DelegateToUserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Message { get; set; }
    public bool AutoReply { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Navigation properties (for display purposes)
    public string? UserName { get; set; }
    public string? DelegateToUserName { get; set; }
}
