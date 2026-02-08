using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IEmailService
{
    Task<ServiceResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task<ServiceResult> SendEmailAsync(EmailMessage message);
    Task<ServiceResult> SendDocumentSharedNotificationAsync(string toEmail, string toName, string documentName, string sharedByName, string? message = null);
    Task<ServiceResult> SendApprovalRequestedNotificationAsync(string toEmail, string toName, string documentName, string requestedByName);
    Task<ServiceResult> SendApprovalCompletedNotificationAsync(string toEmail, string toName, string documentName, bool isApproved, string? comments = null);
}
