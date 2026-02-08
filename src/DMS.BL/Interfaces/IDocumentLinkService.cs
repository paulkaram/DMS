using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDocumentLinkService
{
    Task<IEnumerable<DocumentLinkDto>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DocumentLinkDto>> GetIncomingLinksAsync(Guid documentId);
    Task<DocumentLinkDto?> GetByIdAsync(Guid id);
    Task<DocumentLinkDto> AddAsync(CreateLinkRequest request, Guid userId);
    Task<bool> UpdateAsync(Guid id, UpdateLinkRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetLinkCountAsync(Guid documentId);
}
