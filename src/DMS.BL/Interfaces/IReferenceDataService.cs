using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IReferenceDataService
{
    // Classifications
    Task<ServiceResult<List<ClassificationDto>>> GetClassificationsAsync(string? language = null);
    Task<ServiceResult<ClassificationDto>> GetClassificationByIdAsync(Guid id);
    Task<ServiceResult<ClassificationDto>> CreateClassificationAsync(ClassificationDto dto);
    Task<ServiceResult<ClassificationDto>> UpdateClassificationAsync(Guid id, ClassificationDto dto);
    Task<ServiceResult> DeleteClassificationAsync(Guid id);

    // Importances
    Task<ServiceResult<List<ImportanceDto>>> GetImportancesAsync(string? language = null);
    Task<ServiceResult<ImportanceDto>> GetImportanceByIdAsync(Guid id);
    Task<ServiceResult<ImportanceDto>> CreateImportanceAsync(ImportanceDto dto);
    Task<ServiceResult<ImportanceDto>> UpdateImportanceAsync(Guid id, ImportanceDto dto);
    Task<ServiceResult> DeleteImportanceAsync(Guid id);

    // Document Types
    Task<ServiceResult<List<DocumentTypeDto>>> GetDocumentTypesAsync(string? language = null);
    Task<ServiceResult<DocumentTypeDto>> GetDocumentTypeByIdAsync(Guid id);
    Task<ServiceResult<DocumentTypeDto>> CreateDocumentTypeAsync(DocumentTypeDto dto);
    Task<ServiceResult<DocumentTypeDto>> UpdateDocumentTypeAsync(Guid id, DocumentTypeDto dto);
    Task<ServiceResult> DeleteDocumentTypeAsync(Guid id);

    // Lookups
    Task<ServiceResult<List<LookupItemDto>>> GetLookupItemsAsync(string lookupName, string? language = null);
}
