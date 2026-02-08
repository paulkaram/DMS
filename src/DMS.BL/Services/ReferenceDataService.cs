using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class ReferenceDataService : IReferenceDataService
{
    private readonly IClassificationRepository _classificationRepo;
    private readonly IImportanceRepository _importanceRepo;
    private readonly IDocumentTypeRepository _documentTypeRepo;
    private readonly ILookupRepository _lookupRepo;

    public ReferenceDataService(
        IClassificationRepository classificationRepo,
        IImportanceRepository importanceRepo,
        IDocumentTypeRepository documentTypeRepo,
        ILookupRepository lookupRepo)
    {
        _classificationRepo = classificationRepo;
        _importanceRepo = importanceRepo;
        _documentTypeRepo = documentTypeRepo;
        _lookupRepo = lookupRepo;
    }

    // Classifications
    public async Task<ServiceResult<List<ClassificationDto>>> GetClassificationsAsync(string? language = null)
    {
        var items = await _classificationRepo.GetAllAsync(language);
        return ServiceResult<List<ClassificationDto>>.Ok(items.Select(x => new ClassificationDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description
        }).ToList());
    }

    public async Task<ServiceResult<ClassificationDto>> GetClassificationByIdAsync(Guid id)
    {
        var item = await _classificationRepo.GetByIdAsync(id);
        if (item == null) return ServiceResult<ClassificationDto>.Fail("Not found");
        return ServiceResult<ClassificationDto>.Ok(new ClassificationDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description
        });
    }

    public async Task<ServiceResult<ClassificationDto>> CreateClassificationAsync(ClassificationDto dto)
    {
        var entity = new Classification
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };
        var id = await _classificationRepo.CreateAsync(entity);
        dto.Id = id;
        return ServiceResult<ClassificationDto>.Ok(dto);
    }

    public async Task<ServiceResult<ClassificationDto>> UpdateClassificationAsync(Guid id, ClassificationDto dto)
    {
        var entity = await _classificationRepo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<ClassificationDto>.Fail("Not found");
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        await _classificationRepo.UpdateAsync(entity);
        return ServiceResult<ClassificationDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeleteClassificationAsync(Guid id)
    {
        await _classificationRepo.DeleteAsync(id);
        return ServiceResult.Ok();
    }

    // Importances
    public async Task<ServiceResult<List<ImportanceDto>>> GetImportancesAsync(string? language = null)
    {
        var items = await _importanceRepo.GetAllAsync(language);
        return ServiceResult<List<ImportanceDto>>.Ok(items.Select(x => new ImportanceDto
        {
            Id = x.Id,
            Name = x.Name,
            Level = x.Level,
            Color = x.Color
        }).ToList());
    }

    public async Task<ServiceResult<ImportanceDto>> GetImportanceByIdAsync(Guid id)
    {
        var item = await _importanceRepo.GetByIdAsync(id);
        if (item == null) return ServiceResult<ImportanceDto>.Fail("Not found");
        return ServiceResult<ImportanceDto>.Ok(new ImportanceDto
        {
            Id = item.Id,
            Name = item.Name,
            Level = item.Level,
            Color = item.Color
        });
    }

    public async Task<ServiceResult<ImportanceDto>> CreateImportanceAsync(ImportanceDto dto)
    {
        var entity = new Importance
        {
            Name = dto.Name,
            Level = dto.Level,
            Color = dto.Color,
            IsActive = true
        };
        var id = await _importanceRepo.CreateAsync(entity);
        dto.Id = id;
        return ServiceResult<ImportanceDto>.Ok(dto);
    }

    public async Task<ServiceResult<ImportanceDto>> UpdateImportanceAsync(Guid id, ImportanceDto dto)
    {
        var entity = await _importanceRepo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<ImportanceDto>.Fail("Not found");
        entity.Name = dto.Name;
        entity.Level = dto.Level;
        entity.Color = dto.Color;
        await _importanceRepo.UpdateAsync(entity);
        return ServiceResult<ImportanceDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeleteImportanceAsync(Guid id)
    {
        await _importanceRepo.DeleteAsync(id);
        return ServiceResult.Ok();
    }

    // Document Types
    public async Task<ServiceResult<List<DocumentTypeDto>>> GetDocumentTypesAsync(string? language = null)
    {
        var items = await _documentTypeRepo.GetAllAsync(language);
        return ServiceResult<List<DocumentTypeDto>>.Ok(items.Select(x => new DocumentTypeDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description
        }).ToList());
    }

    public async Task<ServiceResult<DocumentTypeDto>> GetDocumentTypeByIdAsync(Guid id)
    {
        var item = await _documentTypeRepo.GetByIdAsync(id);
        if (item == null) return ServiceResult<DocumentTypeDto>.Fail("Not found");
        return ServiceResult<DocumentTypeDto>.Ok(new DocumentTypeDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description
        });
    }

    public async Task<ServiceResult<DocumentTypeDto>> CreateDocumentTypeAsync(DocumentTypeDto dto)
    {
        var entity = new DocumentType
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };
        var id = await _documentTypeRepo.CreateAsync(entity);
        dto.Id = id;
        return ServiceResult<DocumentTypeDto>.Ok(dto);
    }

    public async Task<ServiceResult<DocumentTypeDto>> UpdateDocumentTypeAsync(Guid id, DocumentTypeDto dto)
    {
        var entity = await _documentTypeRepo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<DocumentTypeDto>.Fail("Not found");
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        await _documentTypeRepo.UpdateAsync(entity);
        return ServiceResult<DocumentTypeDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeleteDocumentTypeAsync(Guid id)
    {
        await _documentTypeRepo.DeleteAsync(id);
        return ServiceResult.Ok();
    }

    // Lookups
    public async Task<ServiceResult<List<LookupItemDto>>> GetLookupItemsAsync(string lookupName, string? language = null)
    {
        var items = await _lookupRepo.GetItemsByLookupNameAsync(lookupName, language);
        return ServiceResult<List<LookupItemDto>>.Ok(items.Select(x => new LookupItemDto
        {
            Id = x.Id,
            Value = x.Value,
            DisplayText = x.DisplayText
        }).ToList());
    }
}
