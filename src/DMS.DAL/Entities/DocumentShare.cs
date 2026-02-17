namespace DMS.DAL.Entities;

public class DocumentShare
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? SharedWithUserId { get; set; }
    public Guid SharedByUserId { get; set; }
    public int PermissionLevel { get; set; } = 1; // 1=Read, 2=Write

    // Link sharing ("Anyone with the link")
    public bool IsLinkShare { get; set; } = false;
    public string? ShareToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // OTP for secure sharing (NCA security requirement)
    public bool RequiresOtp { get; set; } = false;
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public bool OtpVerified { get; set; } = false;

    // Navigation properties (for display purposes)
    public string? DocumentName { get; set; }
    public string? SharedWithUserName { get; set; }
    public string? SharedByUserName { get; set; }
}
