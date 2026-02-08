using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDocumentAttachmentService
{
    Task<IEnumerable<DocumentAttachmentDto>> GetByDocumentIdAsync(Guid documentId);
    Task<DocumentAttachmentDto?> GetByIdAsync(Guid id);
    Task<DocumentAttachmentDto> AddAsync(Guid documentId, string fileName, string? description, string contentType, long size, string storagePath, Guid userId);
    Task<bool> DeleteAsync(Guid id);
    Task<(Stream stream, string fileName, string contentType)?> DownloadAsync(Guid id);
    Task<int> GetAttachmentCountAsync(Guid documentId);
}
