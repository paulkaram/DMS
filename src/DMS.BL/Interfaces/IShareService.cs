using DMS.BL.DTOs;
using DMS.DAL.Entities;

namespace DMS.BL.Interfaces;

public interface IShareService
{
    Task<IEnumerable<SharedDocumentDto>> GetSharedWithMeAsync(Guid userId);
    Task<IEnumerable<MySharedItemDto>> GetMySharedItemsAsync(Guid userId);
    Task<IEnumerable<DocumentShareDto>> GetDocumentSharesAsync(Guid documentId);
    Task<Guid> ShareDocumentAsync(Guid sharedByUserId, ShareDocumentRequest request);
    Task<bool> UpdateShareAsync(Guid shareId, int permissionLevel, DateTime? expiresAt);
    Task<bool> RevokeShareAsync(Guid shareId);
    Task<ServiceResult> VerifyOtpAsync(Guid shareId, string otpCode, Guid userId);
    Task<ServiceResult> ResendOtpAsync(Guid shareId, Guid userId);

    // Link sharing
    Task<ServiceResult<LinkShareDto>> CreateLinkShareAsync(Guid userId, CreateLinkShareRequest request);
    Task<ServiceResult<LinkShareDto>> GetLinkShareAsync(Guid documentId);
    Task<ServiceResult<DocumentShare>> ValidateShareTokenAsync(string shareToken);
}
