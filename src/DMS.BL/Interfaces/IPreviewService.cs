using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IPreviewService
{
    Task<ServiceResult<PreviewInfo>> GetPreviewInfoAsync(Guid documentId, int? version = null);
    PreviewType GetPreviewType(string? extension, string? contentType);
    bool CanPreview(string? extension);
}
