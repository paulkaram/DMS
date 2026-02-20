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
    private readonly IPrivacyLevelRepository _privacyLevelRepo;

    public ReferenceDataService(
        IClassificationRepository classificationRepo,
        IImportanceRepository importanceRepo,
        IDocumentTypeRepository documentTypeRepo,
        ILookupRepository lookupRepo,
        IPrivacyLevelRepository privacyLevelRepo)
    {
        _classificationRepo = classificationRepo;
        _importanceRepo = importanceRepo;
        _documentTypeRepo = documentTypeRepo;
        _lookupRepo = lookupRepo;
        _privacyLevelRepo = privacyLevelRepo;
    }

    // Classifications
    public async Task<ServiceResult<List<ClassificationDto>>> GetClassificationsAsync(string? language = null)
    {
        var items = await _classificationRepo.GetAllAsync(language);
        return ServiceResult<List<ClassificationDto>>.Ok(items.Select(MapClassification).ToList());
    }

    public async Task<ServiceResult<List<ClassificationDto>>> GetClassificationTreeAsync(string? language = null)
    {
        var allItems = await _classificationRepo.GetTreeAsync(language);
        var allDtos = allItems.Select(MapClassification).ToList();

        // Build tree in memory
        var lookup = allDtos.ToDictionary(d => d.Id);
        var roots = new List<ClassificationDto>();

        foreach (var dto in allDtos)
        {
            if (dto.ParentId.HasValue && lookup.TryGetValue(dto.ParentId.Value, out var parent))
            {
                parent.Children ??= new List<ClassificationDto>();
                parent.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        return ServiceResult<List<ClassificationDto>>.Ok(roots);
    }

    public async Task<ServiceResult<ClassificationDto>> GetClassificationByIdAsync(Guid id)
    {
        var item = await _classificationRepo.GetByIdAsync(id);
        if (item == null) return ServiceResult<ClassificationDto>.Fail("Not found");
        return ServiceResult<ClassificationDto>.Ok(MapClassification(item));
    }

    public async Task<ServiceResult<ClassificationDto>> CreateClassificationAsync(ClassificationDto dto)
    {
        // Check for duplicate code
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var existing = await _classificationRepo.GetByCodeAsync(dto.Code);
            if (existing != null)
                return ServiceResult<ClassificationDto>.Fail($"A classification with code '{dto.Code}' already exists.");
        }

        // Build FullPath from parent chain
        string? fullPath = dto.Name;
        if (dto.ParentId.HasValue)
        {
            var parent = await _classificationRepo.GetByIdAsync(dto.ParentId.Value);
            if (parent == null)
                return ServiceResult<ClassificationDto>.Fail("Parent classification not found");
            fullPath = (parent.FullPath ?? parent.Name) + " > " + dto.Name;
            dto.Level = parent.Level + 1;
        }

        var entity = new Classification
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId,
            Level = dto.Level,
            Code = dto.Code,
            FullPath = fullPath,
            Language = dto.Language,
            ConfidentialityLevel = dto.ConfidentialityLevel,
            DefaultRetentionPolicyId = dto.DefaultRetentionPolicyId,
            DefaultPrivacyLevelId = dto.DefaultPrivacyLevelId,
            RequiresDisposalApproval = dto.RequiresDisposalApproval,
            SortOrder = dto.SortOrder,
            IsActive = true
        };
        var id = await _classificationRepo.CreateAsync(entity);
        dto.Id = id;
        dto.FullPath = fullPath;
        return ServiceResult<ClassificationDto>.Ok(dto);
    }

    public async Task<ServiceResult<ClassificationDto>> UpdateClassificationAsync(Guid id, ClassificationDto dto)
    {
        var entity = await _classificationRepo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<ClassificationDto>.Fail("Not found");

        // Check for duplicate code (exclude self)
        if (!string.IsNullOrWhiteSpace(dto.Code))
        {
            var existing = await _classificationRepo.GetByCodeAsync(dto.Code);
            if (existing != null && existing.Id != id)
                return ServiceResult<ClassificationDto>.Fail($"A classification with code '{dto.Code}' already exists.");
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Code = dto.Code;
        entity.Language = dto.Language;
        entity.ConfidentialityLevel = dto.ConfidentialityLevel;
        entity.DefaultRetentionPolicyId = dto.DefaultRetentionPolicyId;
        entity.DefaultPrivacyLevelId = dto.DefaultPrivacyLevelId;
        entity.RequiresDisposalApproval = dto.RequiresDisposalApproval;
        entity.SortOrder = dto.SortOrder;
        entity.IsActive = dto.IsActive;

        // Recalculate FullPath if parent or name changed
        if (entity.ParentId.HasValue)
        {
            var parent = await _classificationRepo.GetByIdAsync(entity.ParentId.Value);
            entity.FullPath = parent != null
                ? (parent.FullPath ?? parent.Name) + " > " + dto.Name
                : dto.Name;
        }
        else
        {
            entity.FullPath = dto.Name;
        }

        await _classificationRepo.UpdateAsync(entity);
        dto.FullPath = entity.FullPath;
        return ServiceResult<ClassificationDto>.Ok(dto);
    }

    private static ClassificationDto MapClassification(Classification x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        ParentId = x.ParentId,
        Level = x.Level,
        Code = x.Code,
        FullPath = x.FullPath,
        ConfidentialityLevel = x.ConfidentialityLevel,
        DefaultRetentionPolicyId = x.DefaultRetentionPolicyId,
        DefaultPrivacyLevelId = x.DefaultPrivacyLevelId,
        RequiresDisposalApproval = x.RequiresDisposalApproval,
        SortOrder = x.SortOrder,
        IsActive = x.IsActive,
        Language = x.Language
    };

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
    public async Task<ServiceResult<List<LookupDto>>> GetLookupsAsync()
    {
        var lookups = await _lookupRepo.GetAllAsync();
        var dtos = new List<LookupDto>();
        foreach (var l in lookups)
        {
            var items = await _lookupRepo.GetItemsByLookupIdAsync(l.Id);
            dtos.Add(new LookupDto
            {
                Id = l.Id,
                Name = l.Name,
                Description = l.Description,
                ItemCount = items.Count()
            });
        }
        return ServiceResult<List<LookupDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<LookupDetailDto>> GetLookupByIdAsync(Guid id)
    {
        var lookup = await _lookupRepo.GetByIdAsync(id);
        if (lookup == null) return ServiceResult<LookupDetailDto>.Fail("Lookup not found");
        var items = await _lookupRepo.GetItemsByLookupIdAsync(id);
        return ServiceResult<LookupDetailDto>.Ok(new LookupDetailDto
        {
            Id = lookup.Id,
            Name = lookup.Name,
            Description = lookup.Description,
            Items = items.Select(x => new LookupItemDto
            {
                Id = x.Id,
                LookupId = x.LookupId,
                Value = x.Value,
                DisplayText = x.DisplayText,
                Language = x.Language,
                SortOrder = x.SortOrder
            }).ToList()
        });
    }

    public async Task<ServiceResult<LookupDto>> CreateLookupAsync(LookupDto dto)
    {
        var entity = new Lookup
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };
        var id = await _lookupRepo.CreateAsync(entity);
        dto.Id = id;
        return ServiceResult<LookupDto>.Ok(dto);
    }

    public async Task<ServiceResult<LookupDto>> UpdateLookupAsync(Guid id, LookupDto dto)
    {
        var entity = await _lookupRepo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<LookupDto>.Fail("Lookup not found");
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        await _lookupRepo.UpdateAsync(entity);
        dto.Id = id;
        return ServiceResult<LookupDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeleteLookupAsync(Guid id)
    {
        await _lookupRepo.DeleteAsync(id);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<List<LookupItemDto>>> GetLookupItemsAsync(string lookupName, string? language = null)
    {
        var items = await _lookupRepo.GetItemsByLookupNameAsync(lookupName, language);
        return ServiceResult<List<LookupItemDto>>.Ok(items.Select(x => new LookupItemDto
        {
            Id = x.Id,
            LookupId = x.LookupId,
            Value = x.Value,
            DisplayText = x.DisplayText,
            Language = x.Language,
            SortOrder = x.SortOrder
        }).ToList());
    }

    public async Task<ServiceResult<List<LookupItemDto>>> GetLookupItemsByIdAsync(Guid lookupId, string? language = null)
    {
        var items = await _lookupRepo.GetItemsByLookupIdAsync(lookupId, language);
        return ServiceResult<List<LookupItemDto>>.Ok(items.Select(x => new LookupItemDto
        {
            Id = x.Id,
            LookupId = x.LookupId,
            Value = x.Value,
            DisplayText = x.DisplayText,
            Language = x.Language,
            SortOrder = x.SortOrder
        }).ToList());
    }

    public async Task<ServiceResult<LookupItemDto>> CreateLookupItemAsync(Guid lookupId, LookupItemDto dto)
    {
        var lookup = await _lookupRepo.GetByIdAsync(lookupId);
        if (lookup == null) return ServiceResult<LookupItemDto>.Fail("Lookup not found");

        var entity = new LookupItem
        {
            LookupId = lookupId,
            Value = dto.Value,
            DisplayText = dto.DisplayText,
            Language = dto.Language,
            SortOrder = dto.SortOrder,
            IsActive = true
        };
        var id = await _lookupRepo.CreateItemAsync(entity);
        dto.Id = id;
        dto.LookupId = lookupId;
        return ServiceResult<LookupItemDto>.Ok(dto);
    }

    public async Task<ServiceResult<LookupItemDto>> UpdateLookupItemAsync(Guid itemId, LookupItemDto dto)
    {
        var entity = await _lookupRepo.GetItemByIdAsync(itemId);
        if (entity == null) return ServiceResult<LookupItemDto>.Fail("Lookup item not found");
        entity.Value = dto.Value;
        entity.DisplayText = dto.DisplayText;
        entity.Language = dto.Language;
        entity.SortOrder = dto.SortOrder;
        await _lookupRepo.UpdateItemAsync(entity);
        dto.Id = itemId;
        return ServiceResult<LookupItemDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeleteLookupItemAsync(Guid itemId)
    {
        await _lookupRepo.DeleteItemAsync(itemId);
        return ServiceResult.Ok();
    }

    // Privacy Levels
    public async Task<ServiceResult<List<PrivacyLevelDto>>> GetPrivacyLevelsAsync(bool includeInactive = false)
    {
        var items = await _privacyLevelRepo.GetAllAsync(includeInactive);
        return ServiceResult<List<PrivacyLevelDto>>.Ok(items.Select(x => new PrivacyLevelDto
        {
            Id = x.Id,
            Name = x.Name,
            Level = x.Level,
            Color = x.Color,
            Description = x.Description,
            IsActive = x.IsActive
        }).ToList());
    }

    public async Task<ServiceResult<PrivacyLevelDto>> GetPrivacyLevelByIdAsync(Guid id)
    {
        var item = await _privacyLevelRepo.GetByIdAsync(id);
        if (item == null) return ServiceResult<PrivacyLevelDto>.Fail("Not found");
        return ServiceResult<PrivacyLevelDto>.Ok(new PrivacyLevelDto
        {
            Id = item.Id,
            Name = item.Name,
            Level = item.Level,
            Color = item.Color,
            Description = item.Description,
            IsActive = item.IsActive
        });
    }

    public async Task<ServiceResult<PrivacyLevelDto>> CreatePrivacyLevelAsync(PrivacyLevelDto dto)
    {
        var entity = new PrivacyLevel
        {
            Name = dto.Name,
            Level = dto.Level,
            Color = dto.Color,
            Description = dto.Description,
            IsActive = true
        };
        var id = await _privacyLevelRepo.CreateAsync(entity);
        dto.Id = id;
        return ServiceResult<PrivacyLevelDto>.Ok(dto);
    }

    public async Task<ServiceResult<PrivacyLevelDto>> UpdatePrivacyLevelAsync(Guid id, PrivacyLevelDto dto)
    {
        var entity = await _privacyLevelRepo.GetByIdAsync(id);
        if (entity == null) return ServiceResult<PrivacyLevelDto>.Fail("Not found");
        entity.Name = dto.Name;
        entity.Level = dto.Level;
        entity.Color = dto.Color;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        await _privacyLevelRepo.UpdateAsync(entity);
        return ServiceResult<PrivacyLevelDto>.Ok(dto);
    }

    public async Task<ServiceResult> DeletePrivacyLevelAsync(Guid id)
    {
        await _privacyLevelRepo.DeleteAsync(id);
        return ServiceResult.Ok();
    }
}
