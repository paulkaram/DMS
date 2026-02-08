using System.Net;
using System.Net.Mail;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        var smtpSection = configuration.GetSection("Smtp");
        _smtpSettings = new SmtpSettings
        {
            Host = smtpSection["Host"] ?? "localhost",
            Port = int.TryParse(smtpSection["Port"], out var port) ? port : 25,
            EnableSsl = bool.TryParse(smtpSection["EnableSsl"], out var ssl) && ssl,
            Username = smtpSection["Username"],
            Password = smtpSection["Password"],
            FromAddress = smtpSection["FromAddress"] ?? "noreply@intalio.com",
            FromName = smtpSection["FromName"] ?? "INTALIO DMS"
        };
    }

    public async Task<ServiceResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        return await SendEmailAsync(new EmailMessage
        {
            To = new List<string> { to },
            Subject = subject,
            Body = body,
            IsHtml = isHtml
        });
    }

    public async Task<ServiceResult> SendEmailAsync(EmailMessage message)
    {
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.FromAddress, _smtpSettings.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };

            foreach (var to in message.To)
                mailMessage.To.Add(to);

            foreach (var cc in message.Cc)
                mailMessage.CC.Add(cc);

            foreach (var bcc in message.Bcc)
                mailMessage.Bcc.Add(bcc);

            if (message.Attachments != null)
            {
                foreach (var attachment in message.Attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
                }
            }

            using var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
            {
                EnableSsl = _smtpSettings.EnableSsl
            };

            if (!string.IsNullOrEmpty(_smtpSettings.Username))
            {
                client.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
            }

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Recipients}", string.Join(", ", message.To));
            return ServiceResult.Ok("Email sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", message.To));
            return ServiceResult.Fail($"Failed to send email: {ex.Message}");
        }
    }

    public async Task<ServiceResult> SendDocumentSharedNotificationAsync(string toEmail, string toName, string documentName, string sharedByName, string? message = null)
    {
        var subject = $"Document Shared: {documentName}";
        var body = GenerateDocumentSharedEmail(toName, documentName, sharedByName, message);
        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<ServiceResult> SendApprovalRequestedNotificationAsync(string toEmail, string toName, string documentName, string requestedByName)
    {
        var subject = $"Approval Requested: {documentName}";
        var body = GenerateApprovalRequestedEmail(toName, documentName, requestedByName);
        return await SendEmailAsync(toEmail, subject, body);
    }

    public async Task<ServiceResult> SendApprovalCompletedNotificationAsync(string toEmail, string toName, string documentName, bool isApproved, string? comments = null)
    {
        var status = isApproved ? "Approved" : "Rejected";
        var subject = $"Document {status}: {documentName}";
        var body = GenerateApprovalCompletedEmail(toName, documentName, isApproved, comments);
        return await SendEmailAsync(toEmail, subject, body);
    }

    private static string GenerateDocumentSharedEmail(string toName, string documentName, string sharedByName, string? message)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #112d48 0%, #00ae8c 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0; }}
        .footer {{ background: #1e293b; color: #94a3b8; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #00ae8c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin-top: 20px; }}
        .document-name {{ background: white; padding: 15px; border-left: 4px solid #00ae8c; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin:0"">Document Shared With You</h1>
        </div>
        <div class=""content"">
            <p>Hello {toName},</p>
            <p><strong>{sharedByName}</strong> has shared a document with you:</p>
            <div class=""document-name"">
                <strong>{documentName}</strong>
            </div>
            {(string.IsNullOrEmpty(message) ? "" : $"<p><em>Message: {message}</em></p>")}
            <p>You can access this document through the INTALIO DMS portal.</p>
        </div>
        <div class=""footer"">
            <p>INTALIO Document Management System</p>
            <p>This is an automated notification. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GenerateApprovalRequestedEmail(string toName, string documentName, string requestedByName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #112d48 0%, #00ae8c 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0; }}
        .footer {{ background: #1e293b; color: #94a3b8; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; background: #00ae8c; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin-top: 20px; }}
        .document-name {{ background: white; padding: 15px; border-left: 4px solid #f59e0b; margin: 20px 0; }}
        .alert {{ background: #fef3c7; border: 1px solid #f59e0b; padding: 15px; border-radius: 6px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin:0"">Approval Required</h1>
        </div>
        <div class=""content"">
            <p>Hello {toName},</p>
            <div class=""alert"">
                <strong>Action Required:</strong> A document requires your approval.
            </div>
            <p><strong>{requestedByName}</strong> has requested your approval for:</p>
            <div class=""document-name"">
                <strong>{documentName}</strong>
            </div>
            <p>Please review the document and submit your decision through the INTALIO DMS portal.</p>
        </div>
        <div class=""footer"">
            <p>INTALIO Document Management System</p>
            <p>This is an automated notification. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }

    private static string GenerateApprovalCompletedEmail(string toName, string documentName, bool isApproved, string? comments)
    {
        var statusColor = isApproved ? "#10b981" : "#ef4444";
        var statusText = isApproved ? "Approved" : "Rejected";
        var statusIcon = isApproved ? "✓" : "✗";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #112d48 0%, #00ae8c 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border: 1px solid #e2e8f0; }}
        .footer {{ background: #1e293b; color: #94a3b8; padding: 20px; text-align: center; font-size: 12px; border-radius: 0 0 8px 8px; }}
        .status {{ display: inline-block; background: {statusColor}; color: white; padding: 8px 16px; border-radius: 20px; font-weight: bold; }}
        .document-name {{ background: white; padding: 15px; border-left: 4px solid {statusColor}; margin: 20px 0; }}
        .comments {{ background: #f1f5f9; padding: 15px; border-radius: 6px; margin: 20px 0; font-style: italic; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1 style=""margin:0"">Approval Decision</h1>
        </div>
        <div class=""content"">
            <p>Hello {toName},</p>
            <p>Your document approval request has been processed:</p>
            <div class=""document-name"">
                <strong>{documentName}</strong>
                <br><br>
                <span class=""status"">{statusIcon} {statusText}</span>
            </div>
            {(string.IsNullOrEmpty(comments) ? "" : $@"<div class=""comments""><strong>Comments:</strong><br>{comments}</div>")}
            <p>You can view the full details in the INTALIO DMS portal.</p>
        </div>
        <div class=""footer"">
            <p>INTALIO Document Management System</p>
            <p>This is an automated notification. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";
    }
}
