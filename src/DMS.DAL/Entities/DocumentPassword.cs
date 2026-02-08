namespace DMS.DAL.Entities;

public class DocumentPassword
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
