using DMS.DAL.DTOs;
using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IContentTypeDefinitionRepository
{
    // Content Type Definitions
    Task<IEnumerable<ContentTypeDefinition>> GetAllAsync(bool includeInactive = false);
    Task<ContentTypeDefinition?> GetByIdAsync(Guid id);
    Task<ContentTypeDefinition?> GetByIdWithFieldsAsync(Guid id);
    Task<ContentTypeDefinition?> GetByNameAsync(string name);
    Task<ContentTypeDefinition?> GetSystemDefaultAsync();
    Task<bool> SetSystemDefaultAsync(Guid contentTypeId, Guid userId);
    Task<bool> ClearSystemDefaultAsync(Guid userId);
    Task<Guid> CreateAsync(ContentTypeDefinition contentType);
    Task<bool> UpdateAsync(ContentTypeDefinition contentType);
    Task<bool> DeleteAsync(Guid id);

    // Content Type Fields
    Task<IEnumerable<ContentTypeField>> GetFieldsAsync(Guid contentTypeId);
    Task<ContentTypeField?> GetFieldByIdAsync(Guid fieldId);
    Task<Guid> CreateFieldAsync(ContentTypeField field);
    Task<bool> UpdateFieldAsync(ContentTypeField field);
    Task<bool> DeleteFieldAsync(Guid fieldId);
    Task<bool> ReorderFieldsAsync(Guid contentTypeId, List<Guid> fieldIds);

    // Document Metadata
    Task<IEnumerable<DocumentMetadata>> GetDocumentMetadataAsync(Guid documentId);
    Task<IEnumerable<DocumentMetadata>> GetDocumentMetadataByContentTypeAsync(Guid documentId, Guid contentTypeId);
    Task<bool> SaveDocumentMetadataAsync(Guid documentId, Guid contentTypeId, List<DocumentMetadata> metadata, Guid userId);
    Task<bool> DeleteDocumentMetadataAsync(Guid documentId);
    Task<bool> DeleteDocumentMetadataByContentTypeAsync(Guid documentId, Guid contentTypeId);

    // Folder Content Type Assignments
    Task<IEnumerable<FolderContentTypeAssignment>> GetFolderContentTypesAsync(Guid folderId);
    Task<IEnumerable<ContentTypeDefinition>> GetAvailableContentTypesForFolderAsync(Guid folderId);
    Task<Guid> AssignContentTypeToFolderAsync(FolderContentTypeAssignment assignment);
    Task<bool> RemoveContentTypeFromFolderAsync(Guid assignmentId);
    Task<bool> RemoveContentTypeFromFolderAsync(Guid folderId, Guid contentTypeId);
    Task<bool> UpdateFolderAssignmentAsync(Guid folderId, Guid contentTypeId, bool isRequired, bool isDefault, bool inheritToChildren, int displayOrder);
    Task<bool> SetFolderDefaultContentTypeAsync(Guid folderId, Guid contentTypeId);

    // Cabinet Content Type Assignments
    Task<IEnumerable<CabinetContentTypeAssignment>> GetCabinetContentTypesAsync(Guid cabinetId);
    Task<Guid> AssignContentTypeToCabinetAsync(CabinetContentTypeAssignment assignment);
    Task<bool> RemoveContentTypeFromCabinetAsync(Guid cabinetId, Guid contentTypeId);
    Task<bool> UpdateCabinetAssignmentAsync(Guid cabinetId, Guid contentTypeId, bool isRequired, bool isDefault, bool inheritToChildren, int displayOrder);
    Task<bool> SetCabinetDefaultContentTypeAsync(Guid cabinetId, Guid contentTypeId);

    // Effective Content Types (with inheritance calculation)
    Task<IEnumerable<EffectiveContentTypeDto>> GetEffectiveContentTypesAsync(Guid folderId);
    Task<FolderContentTypeInfoDto?> GetFolderContentTypeInfoAsync(Guid folderId);
}
