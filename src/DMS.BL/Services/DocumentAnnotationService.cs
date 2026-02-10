using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentAnnotationService : IDocumentAnnotationService
{
    private readonly IDocumentAnnotationRepository _annotationRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public DocumentAnnotationService(
        IDocumentAnnotationRepository annotationRepository,
        IActivityLogRepository activityLogRepository)
    {
        _annotationRepository = annotationRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<IEnumerable<DocumentAnnotationDto>> GetByDocumentIdAsync(Guid documentId)
    {
        var annotations = await _annotationRepository.GetByDocumentIdAsync(documentId);
        return annotations.Select(MapToDto);
    }

    public async Task<List<DocumentAnnotationDto>> SaveAnnotationsAsync(SaveAnnotationsRequest request, Guid userId)
    {
        var results = new List<DocumentAnnotationDto>();

        foreach (var page in request.Pages)
        {
            var annotation = new DocumentAnnotation
            {
                DocumentId = request.DocumentId,
                PageNumber = page.PageNumber,
                AnnotationData = page.AnnotationData,
                CreatedBy = userId
            };

            var id = await _annotationRepository.UpsertAsync(annotation);

            var saved = await _annotationRepository.GetByDocumentAndPageAsync(request.DocumentId, page.PageNumber);
            if (saved != null)
                results.Add(MapToDto(saved));
        }

        await _activityLogRepository.CreateAsync(new ActivityLog
        {
            NodeType = NodeType.Document,
            NodeId = request.DocumentId,
            Action = "AnnotationsSaved",
            Details = $"Annotations saved for {request.Pages.Count} page(s)",
            UserId = userId
        });

        return results;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var result = await _annotationRepository.DeleteAsync(id, userId);
        return result;
    }

    public async Task<bool> DeleteAllByDocumentAsync(Guid documentId, Guid userId)
    {
        var result = await _annotationRepository.DeleteAllByDocumentAsync(documentId, userId);

        if (result)
        {
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = documentId,
                Action = "AnnotationsCleared",
                Details = "All annotations cleared",
                UserId = userId
            });
        }

        return result;
    }

    public async Task<int> GetCountAsync(Guid documentId)
    {
        return await _annotationRepository.GetCountAsync(documentId);
    }

    private static DocumentAnnotationDto MapToDto(DocumentAnnotation annotation)
    {
        return new DocumentAnnotationDto
        {
            Id = annotation.Id,
            DocumentId = annotation.DocumentId,
            PageNumber = annotation.PageNumber,
            AnnotationData = annotation.AnnotationData,
            VersionNumber = annotation.VersionNumber,
            CreatedBy = annotation.CreatedBy ?? Guid.Empty,
            CreatedByName = annotation.CreatedByName,
            CreatedAt = annotation.CreatedAt,
            ModifiedAt = annotation.ModifiedAt
        };
    }
}

public class SavedSignatureService : ISavedSignatureService
{
    private readonly ISavedSignatureRepository _signatureRepository;

    public SavedSignatureService(ISavedSignatureRepository signatureRepository)
    {
        _signatureRepository = signatureRepository;
    }

    public async Task<IEnumerable<SavedSignatureDto>> GetByUserIdAsync(Guid userId)
    {
        var signatures = await _signatureRepository.GetByUserIdAsync(userId);
        return signatures.Select(MapToDto);
    }

    public async Task<SavedSignatureDto> AddAsync(CreateSignatureRequest request, Guid userId)
    {
        var signature = new SavedSignature
        {
            UserId = userId,
            Name = request.Name,
            SignatureData = request.SignatureData,
            SignatureType = request.SignatureType,
            IsDefault = request.IsDefault
        };

        // If setting as default, clear other defaults first
        if (request.IsDefault)
        {
            await _signatureRepository.SetDefaultAsync(Guid.Empty, userId); // clears all defaults
        }

        var id = await _signatureRepository.AddAsync(signature);
        signature.Id = id;
        return MapToDto(signature);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var signature = await _signatureRepository.GetByIdAsync(id);
        if (signature == null || signature.UserId != userId) return false;

        return await _signatureRepository.DeleteAsync(id);
    }

    public async Task<bool> SetDefaultAsync(Guid signatureId, Guid userId)
    {
        return await _signatureRepository.SetDefaultAsync(userId, signatureId);
    }

    private static SavedSignatureDto MapToDto(SavedSignature signature)
    {
        return new SavedSignatureDto
        {
            Id = signature.Id,
            Name = signature.Name,
            SignatureData = signature.SignatureData,
            SignatureType = signature.SignatureType,
            IsDefault = signature.IsDefault,
            CreatedAt = signature.CreatedAt
        };
    }
}
