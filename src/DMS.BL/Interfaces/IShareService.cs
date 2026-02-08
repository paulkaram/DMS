using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IShareService
{
    Task<IEnumerable<SharedDocumentDto>> GetSharedWithMeAsync(Guid userId);
    Task<IEnumerable<MySharedItemDto>> GetMySharedItemsAsync(Guid userId);
    Task<IEnumerable<DocumentShareDto>> GetDocumentSharesAsync(Guid documentId);
    Task<Guid> ShareDocumentAsync(Guid sharedByUserId, ShareDocumentRequest request);
    Task<bool> UpdateShareAsync(Guid shareId, int permissionLevel, DateTime? expiresAt);
    Task<bool> RevokeShareAsync(Guid shareId);
}
