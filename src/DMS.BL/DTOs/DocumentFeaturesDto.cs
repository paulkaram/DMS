namespace DMS.BL.DTOs;

// =============================================
// Comment DTOs
// =============================================
public class DocumentCommentDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int ReplyCount { get; set; }
    public List<DocumentCommentDto>? Replies { get; set; }
}

public class CreateCommentRequest
{
    public Guid DocumentId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

// =============================================
// Attachment DTOs
// =============================================
public class DocumentAttachmentDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAttachmentRequest
{
    public Guid DocumentId { get; set; }
    public string? Description { get; set; }
}

// =============================================
// Link DTOs
// =============================================
public class DocumentLinkDto
{
    public Guid Id { get; set; }
    public Guid SourceDocumentId { get; set; }
    public string? SourceDocumentName { get; set; }
    public Guid TargetDocumentId { get; set; }
    public string? TargetDocumentName { get; set; }
    public string? TargetDocumentPath { get; set; }
    public string LinkType { get; set; } = "related";
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLinkRequest
{
    public Guid SourceDocumentId { get; set; }
    public Guid TargetDocumentId { get; set; }
    public string LinkType { get; set; } = "related";
    public string? Description { get; set; }
}

public class UpdateLinkRequest
{
    public string LinkType { get; set; } = "related";
    public string? Description { get; set; }
}

// =============================================
// Password DTOs
// =============================================
public class DocumentPasswordDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public bool HasPassword { get; set; }
    public string? Hint { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SetPasswordRequest
{
    public Guid DocumentId { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Hint { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ValidatePasswordRequest
{
    public Guid DocumentId { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public Guid DocumentId { get; set; }
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string? Hint { get; set; }
}

// =============================================
// Document Feature Summary
// =============================================
public class DocumentFeatureSummaryDto
{
    public int CommentCount { get; set; }
    public int AttachmentCount { get; set; }
    public int LinkCount { get; set; }
    public bool HasPassword { get; set; }
}
