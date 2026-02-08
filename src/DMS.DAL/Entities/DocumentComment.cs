namespace DMS.DAL.Entities;

public class DocumentComment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation/display properties
    public string? CreatedByName { get; set; }
    public int ReplyCount { get; set; }
}
