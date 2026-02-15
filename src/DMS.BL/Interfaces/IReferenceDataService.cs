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

    // Privacy Levels
    Task<ServiceResult<List<PrivacyLevelDto>>> GetPrivacyLevelsAsync(bool includeInactive = false);
    Task<ServiceResult<PrivacyLevelDto>> GetPrivacyLevelByIdAsync(Guid id);
    Task<ServiceResult<PrivacyLevelDto>> CreatePrivacyLevelAsync(PrivacyLevelDto dto);
    Task<ServiceResult<PrivacyLevelDto>> UpdatePrivacyLevelAsync(Guid id, PrivacyLevelDto dto);
    Task<ServiceResult> DeletePrivacyLevelAsync(Guid id);

    // Lookups
    Task<ServiceResult<List<LookupDto>>> GetLookupsAsync();
    Task<ServiceResult<LookupDetailDto>> GetLookupByIdAsync(Guid id);
    Task<ServiceResult<LookupDto>> CreateLookupAsync(LookupDto dto);
    Task<ServiceResult<LookupDto>> UpdateLookupAsync(Guid id, LookupDto dto);
    Task<ServiceResult> DeleteLookupAsync(Guid id);
    Task<ServiceResult<List<LookupItemDto>>> GetLookupItemsAsync(string lookupName, string? language = null);
    Task<ServiceResult<List<LookupItemDto>>> GetLookupItemsByIdAsync(Guid lookupId, string? language = null);
    Task<ServiceResult<LookupItemDto>> CreateLookupItemAsync(Guid lookupId, LookupItemDto dto);
    Task<ServiceResult<LookupItemDto>> UpdateLookupItemAsync(Guid itemId, LookupItemDto dto);
    Task<ServiceResult> DeleteLookupItemAsync(Guid itemId);
}
