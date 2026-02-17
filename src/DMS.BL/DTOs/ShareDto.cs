namespace DMS.BL.DTOs;

public class DocumentShareDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? SharedWithUserId { get; set; }
    public Guid SharedByUserId { get; set; }
    public int PermissionLevel { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsLinkShare { get; set; }
    public string? ShareToken { get; set; }

    // Display names
    public string? DocumentName { get; set; }
    public string? SharedWithUserName { get; set; }
    public string? SharedByUserName { get; set; }
}

public class ShareDocumentRequest
{
    public Guid DocumentId { get; set; }
    public Guid SharedWithUserId { get; set; }
    public int PermissionLevel { get; set; } = 1;
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool RequiresOtp { get; set; } = false;
}

public class SharedDocumentDto
{
    public Guid ShareId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public long Size { get; set; }
    public int PermissionLevel { get; set; }
    public string? SharedByUserName { get; set; }
    public DateTime SharedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool HasPassword { get; set; }
    public bool RequiresOtp { get; set; }
    public bool OtpVerified { get; set; }
}

public class MySharedItemDto
{
    public Guid ShareId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string? Extension { get; set; }
    public string SharedWithUserName { get; set; } = string.Empty;
    public int PermissionLevel { get; set; }
    public DateTime SharedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool HasPassword { get; set; }
    public bool IsLinkShare { get; set; }
}

public class UpdateShareRequest
{
    public int PermissionLevel { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class VerifyOtpRequest
{
    public string OtpCode { get; set; } = string.Empty;
}

public class CreateLinkShareRequest
{
    public Guid DocumentId { get; set; }
    public int PermissionLevel { get; set; } = 1;
    public DateTime? ExpiresAt { get; set; }
}

public class LinkShareDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string ShareToken { get; set; } = string.Empty;
    public int PermissionLevel { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? SharedByUserName { get; set; }
}
