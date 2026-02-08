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
                    HasPassword = passwordStatuses.TryGetValue(share.DocumentId, out var hasPassword) && hasPassword
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
                    SharedWithUserName = share.SharedWithUserName ?? "Unknown",
                    PermissionLevel = share.PermissionLevel,
                    SharedAt = share.CreatedAt,
                    ExpiresAt = share.ExpiresAt,
                    HasPassword = passwordStatuses.TryGetValue(share.DocumentId, out var hasPassword) && hasPassword
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
            IsActive = true
        };

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
                    request.Message);

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
}
