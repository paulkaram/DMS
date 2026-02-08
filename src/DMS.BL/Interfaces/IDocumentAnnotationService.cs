using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IDocumentAnnotationService
{
    Task<IEnumerable<DocumentAnnotationDto>> GetByDocumentIdAsync(Guid documentId);
    Task<List<DocumentAnnotationDto>> SaveAnnotationsAsync(SaveAnnotationsRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<bool> DeleteAllByDocumentAsync(Guid documentId, Guid userId);
    Task<int> GetCountAsync(Guid documentId);
}

public interface ISavedSignatureService
{
    Task<IEnumerable<SavedSignatureDto>> GetByUserIdAsync(Guid userId);
    Task<SavedSignatureDto> AddAsync(CreateSignatureRequest request, Guid userId);
    Task<bool> DeleteAsync(Guid id, Guid userId);
    Task<bool> SetDefaultAsync(Guid id, Guid userId);
}
