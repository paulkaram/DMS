using DMS.DAL.Data;

namespace DMS.DAL.Entities;

public class DocumentPassword : IAuditable, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
}
