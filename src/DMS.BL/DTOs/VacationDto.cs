namespace DMS.BL.DTOs;

public class VacationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? DelegateToUserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Message { get; set; }
    public bool AutoReply { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    // Display names
    public string? UserName { get; set; }
    public string? DelegateToUserName { get; set; }
}

public class CreateVacationRequest
{
    public Guid? DelegateToUserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Message { get; set; }
    public bool AutoReply { get; set; }
}

public class UpdateVacationRequest
{
    public Guid? DelegateToUserId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Message { get; set; }
    public bool AutoReply { get; set; }
    public bool IsActive { get; set; }
}
