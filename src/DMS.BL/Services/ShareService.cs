using System.Security.Cryptography;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class ShareService : IShareService
{
    private readonly IDocumentShareRepository _shareRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentPasswordRepository _passwordRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ShareService> _logger;

    public ShareService(
        IDocumentShareRepository shareRepository,
        IDocumentRepository documentRepository,
        IDocumentPasswordRepository passwordRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<ShareService> logger)
    {
        _shareRepository = shareRepository;
        _documentRepository = documentRepository;
        _passwordRepository = passwordRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IEnumerable<SharedDocumentDto>> GetSharedWithMeAsync(Guid userId)
    {
        var shares = await _shareRepository.GetSharedWithUserAsync(userId);
        var result = new List<SharedDocumentDto>();

        // Get all document IDs for bulk password check
        var documentIds = shares.Select(s => s.DocumentId).Distinct().ToList();
        var passwordStatuses = await _passwordRepository.GetPasswordStatusBulkAsync(documentIds);

        foreach (var share in shares)
        {
            var doc = await _documentRepository.GetByIdAsync(share.DocumentId);
            if (doc != null)
            {
                result.Add(new SharedDocumentDto
                {
                    ShareId = share.Id,
                    DocumentId = share.DocumentId,
                    DocumentName = doc.Name,
                    Extension = doc.Extension,
                    Size = doc.Size,
                    PermissionLevel = share.PermissionLevel,
                    SharedByUserName = share.SharedByUserName,
                    SharedAt = share.CreatedAt,
                    ExpiresAt = share.ExpiresAt,
                    Message = share.Message,
                    HasPassword = passwordStatuses.TryGetValue(share.DocumentId, out var hasPassword) && hasPassword,
                    RequiresOtp = share.RequiresOtp,
                    OtpVerified = share.OtpVerified
                });
            }
        }

        return result;
    }

    public async Task<IEnumerable<MySharedItemDto>> GetMySharedItemsAsync(Guid userId)
    {
        var shares = await _shareRepository.GetSharedByUserAsync(userId);
        var result = new List<MySharedItemDto>();

        // Get all document IDs for bulk password check
        var documentIds = shares.Select(s => s.DocumentId).Distinct().ToList();
        var passwordStatuses = await _passwordRepository.GetPasswordStatusBulkAsync(documentIds);

        foreach (var share in shares)
        {
            var doc = await _documentRepository.GetByIdAsync(share.DocumentId);
            if (doc != null)
            {
                result.Add(new MySharedItemDto
                {
                    ShareId = share.Id,
                    DocumentId = share.DocumentId,
                    DocumentName = doc.Name,
                    Extension = doc.Extension,
                    SharedWithUserName = share.IsLinkShare ? "Anyone with the link" : (share.SharedWithUserName ?? "Unknown"),
                    PermissionLevel = share.PermissionLevel,
                    SharedAt = share.CreatedAt,
                    ExpiresAt = share.ExpiresAt,
                    HasPassword = passwordStatuses.TryGetValue(share.DocumentId, out var hasPassword) && hasPassword,
                    IsLinkShare = share.IsLinkShare
                });
            }
        }

        return result;
    }

    public async Task<IEnumerable<DocumentShareDto>> GetDocumentSharesAsync(Guid documentId)
    {
        var shares = await _shareRepository.GetByDocumentIdAsync(documentId);
        return shares.Select(s => new DocumentShareDto
        {
            Id = s.Id,
            DocumentId = s.DocumentId,
            SharedWithUserId = s.SharedWithUserId,
            SharedByUserId = s.SharedByUserId,
            PermissionLevel = s.PermissionLevel,
            ExpiresAt = s.ExpiresAt,
            Message = s.Message,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt,
            IsLinkShare = s.IsLinkShare,
            ShareToken = s.ShareToken,
            DocumentName = s.DocumentName,
            SharedWithUserName = s.SharedWithUserName,
            SharedByUserName = s.SharedByUserName
        });
    }

    public async Task<Guid> ShareDocumentAsync(Guid sharedByUserId, ShareDocumentRequest request)
    {
        var share = new DocumentShare
        {
            DocumentId = request.DocumentId,
            SharedWithUserId = request.SharedWithUserId,
            SharedByUserId = sharedByUserId,
            PermissionLevel = request.PermissionLevel,
            ExpiresAt = request.ExpiresAt,
            Message = request.Message,
            RequiresOtp = request.RequiresOtp,
            IsActive = true
        };

        // Generate OTP if required (NCA secure sharing requirement)
        if (request.RequiresOtp)
        {
            share.OtpCode = GenerateOtp();
            share.OtpExpiresAt = DateTime.Now.AddMinutes(10);
            share.OtpVerified = false;
        }

        var shareId = await _shareRepository.CreateAsync(share);

        // Send email notification
        try
        {
            var document = await _documentRepository.GetByIdAsync(request.DocumentId);
            var sharedByUser = await _userRepository.GetByIdAsync(sharedByUserId);
            var sharedWithUser = await _userRepository.GetByIdAsync(request.SharedWithUserId);

            if (document != null && sharedWithUser?.Email != null && sharedByUser != null)
            {
                var sharedByName = sharedByUser.DisplayName ?? sharedByUser.Username;
                var sharedWithName = sharedWithUser.DisplayName ?? sharedWithUser.Username;

                await _emailService.SendDocumentSharedNotificationAsync(
                    sharedWithUser.Email,
                    sharedWithName,
                    document.Name,
                    sharedByName,
                    request.RequiresOtp
                        ? $"{request.Message}\n\nYour OTP code is: {share.OtpCode} (expires in 10 minutes)"
                        : request.Message);

                _logger.LogInformation("Share notification email sent to {Email} for document {DocumentName}",
                    sharedWithUser.Email, document.Name);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the share if email fails
            _logger.LogWarning(ex, "Failed to send share notification email");
        }

        return shareId;
    }

    public async Task<bool> UpdateShareAsync(Guid shareId, int permissionLevel, DateTime? expiresAt)
    {
        var share = await _shareRepository.GetByIdAsync(shareId);
        if (share == null) return false;

        share.PermissionLevel = permissionLevel;
        share.ExpiresAt = expiresAt;

        return await _shareRepository.UpdateAsync(share);
    }

    public async Task<bool> RevokeShareAsync(Guid shareId)
    {
        return await _shareRepository.DeleteAsync(shareId);
    }

    public async Task<ServiceResult> VerifyOtpAsync(Guid shareId, string otpCode, Guid userId)
    {
        var share = await _shareRepository.GetByIdAsync(shareId);
        if (share == null)
            return ServiceResult.Fail("Share not found");

        if (share.SharedWithUserId == null || share.SharedWithUserId != userId)
            return ServiceResult.Fail("Unauthorized");

        if (!share.RequiresOtp)
            return ServiceResult.Fail("This share does not require OTP verification");

        if (share.OtpVerified)
            return ServiceResult.Ok("Already verified");

        if (share.OtpExpiresAt.HasValue && share.OtpExpiresAt.Value < DateTime.Now)
            return ServiceResult.Fail("OTP has expired. Please request a new code.");

        if (share.OtpCode != otpCode)
            return ServiceResult.Fail("Invalid OTP code");

        share.OtpVerified = true;
        await _shareRepository.UpdateAsync(share);

        _logger.LogInformation("OTP verified for share {ShareId} by user {UserId}", shareId, userId);
        return ServiceResult.Ok("OTP verified successfully");
    }

    public async Task<ServiceResult> ResendOtpAsync(Guid shareId, Guid userId)
    {
        var share = await _shareRepository.GetByIdAsync(shareId);
        if (share == null)
            return ServiceResult.Fail("Share not found");

        if (share.SharedWithUserId == null || share.SharedWithUserId != userId)
            return ServiceResult.Fail("Unauthorized");

        if (!share.RequiresOtp)
            return ServiceResult.Fail("This share does not require OTP verification");

        // Generate new OTP
        share.OtpCode = GenerateOtp();
        share.OtpExpiresAt = DateTime.Now.AddMinutes(10);
        share.OtpVerified = false;
        await _shareRepository.UpdateAsync(share);

        // Send OTP via email
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Email != null)
            {
                var document = await _documentRepository.GetByIdAsync(share.DocumentId);
                await _emailService.SendDocumentSharedNotificationAsync(
                    user.Email,
                    user.DisplayName ?? user.Username,
                    document?.Name ?? "Document",
                    "System",
                    $"Your new OTP code is: {share.OtpCode} (expires in 10 minutes)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send OTP resend email");
        }

        return ServiceResult.Ok("New OTP sent");
    }

    public async Task<ServiceResult<LinkShareDto>> CreateLinkShareAsync(Guid userId, CreateLinkShareRequest request)
    {
        // Check for existing active link share on this document
        var existing = await _shareRepository.GetActiveLinkShareByDocumentAsync(request.DocumentId);
        if (existing != null)
        {
            return ServiceResult<LinkShareDto>.Ok(new LinkShareDto
            {
                Id = existing.Id,
                DocumentId = existing.DocumentId,
                ShareToken = existing.ShareToken!,
                PermissionLevel = existing.PermissionLevel,
                ExpiresAt = existing.ExpiresAt,
                IsActive = existing.IsActive,
                CreatedAt = existing.CreatedAt,
                SharedByUserName = existing.SharedByUserName
            });
        }

        var share = new DocumentShare
        {
            DocumentId = request.DocumentId,
            SharedWithUserId = null,
            SharedByUserId = userId,
            PermissionLevel = request.PermissionLevel,
            ExpiresAt = request.ExpiresAt,
            IsLinkShare = true,
            ShareToken = GenerateShareToken(),
            IsActive = true
        };

        await _shareRepository.CreateAsync(share);

        var sharedByUser = await _userRepository.GetByIdAsync(userId);

        return ServiceResult<LinkShareDto>.Ok(new LinkShareDto
        {
            Id = share.Id,
            DocumentId = share.DocumentId,
            ShareToken = share.ShareToken!,
            PermissionLevel = share.PermissionLevel,
            ExpiresAt = share.ExpiresAt,
            IsActive = share.IsActive,
            CreatedAt = share.CreatedAt,
            SharedByUserName = sharedByUser?.DisplayName ?? sharedByUser?.Username
        });
    }

    public async Task<ServiceResult<LinkShareDto>> GetLinkShareAsync(Guid documentId)
    {
        var share = await _shareRepository.GetActiveLinkShareByDocumentAsync(documentId);
        if (share == null)
            return ServiceResult<LinkShareDto>.Fail("No active link share found");

        return ServiceResult<LinkShareDto>.Ok(new LinkShareDto
        {
            Id = share.Id,
            DocumentId = share.DocumentId,
            ShareToken = share.ShareToken!,
            PermissionLevel = share.PermissionLevel,
            ExpiresAt = share.ExpiresAt,
            IsActive = share.IsActive,
            CreatedAt = share.CreatedAt,
            SharedByUserName = share.SharedByUserName
        });
    }

    public async Task<ServiceResult<DocumentShare>> ValidateShareTokenAsync(string shareToken)
    {
        var share = await _shareRepository.GetByShareTokenAsync(shareToken);
        if (share == null)
            return ServiceResult<DocumentShare>.Fail("Share link is not available or has expired");

        return ServiceResult<DocumentShare>.Ok(share);
    }

    private static string GenerateShareToken()
    {
        var bytes = new byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }
}
