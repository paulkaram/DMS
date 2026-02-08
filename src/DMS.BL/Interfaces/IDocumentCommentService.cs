using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDocumentCommentService
{
    Task<IEnumerable<DocumentCommentDto>> GetByDocumentIdAsync(Guid documentId);
    Task<IEnumerable<DocumentCommentDto>> GetRepliesAsync(Guid parentCommentId);
    Task<DocumentCommentDto?> GetByIdAsync(Guid id);
    Task<DocumentCommentDto> AddAsync(CreateCommentRequest request, Guid userId);
    Task<bool> UpdateAsync(Guid id, UpdateCommentRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<int> GetCommentCountAsync(Guid documentId);
}
