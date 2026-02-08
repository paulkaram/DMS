using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDocumentPasswordService
{
    Task<DocumentPasswordDto?> GetByDocumentIdAsync(Guid documentId);
    Task<bool> HasPasswordAsync(Guid documentId);
    Task<bool> SetPasswordAsync(SetPasswordRequest request, Guid userId);
    Task<bool> ValidatePasswordAsync(ValidatePasswordRequest request);
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request, Guid userId);
    Task<bool> RemovePasswordAsync(Guid documentId, Guid userId);
    Task<string?> GetHintAsync(Guid documentId);
}
