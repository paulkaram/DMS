using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDocumentShortcutService
{
    Task<ServiceResult<DocumentShortcutDto>> CreateShortcutAsync(CreateDocumentShortcutDto dto, Guid userId);
    Task<ServiceResult> RemoveShortcutAsync(Guid shortcutId);
    Task<ServiceResult<List<DocumentShortcutDto>>> GetShortcutsByDocumentAsync(Guid documentId);
}
