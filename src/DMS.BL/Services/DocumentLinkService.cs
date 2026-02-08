using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentLinkService : IDocumentLinkService
{
    private readonly IDocumentLinkRepository _linkRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public DocumentLinkService(
        IDocumentLinkRepository linkRepository,
        IActivityLogRepository activityLogRepository)
    {
        _linkRepository = linkRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<IEnumerable<DocumentLinkDto>> GetByDocumentIdAsync(Guid documentId)
    {
        var links = await _linkRepository.GetByDocumentIdAsync(documentId);
        return links.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentLinkDto>> GetIncomingLinksAsync(Guid documentId)
    {
        var links = await _linkRepository.GetIncomingLinksAsync(documentId);
        return links.Select(MapToDto);
    }

    public async Task<DocumentLinkDto?> GetByIdAsync(Guid id)
    {
        var link = await _linkRepository.GetByIdAsync(id);
        return link != null ? MapToDto(link) : null;
    }

    public async Task<DocumentLinkDto> AddAsync(CreateLinkRequest request, Guid userId)
    {
        // Check if link already exists
        var existing = await _linkRepository.GetExistingLinkAsync(request.SourceDocumentId, request.TargetDocumentId);
        if (existing != null)
        {
            throw new InvalidOperationException("Link already exists between these documents");
        }

        // Cannot link document to itself
        if (request.SourceDocumentId == request.TargetDocumentId)
        {
            throw new InvalidOperationException("Cannot link a document to itself");
        }

        var link = new DocumentLink
        {
            SourceDocumentId = request.SourceDocumentId,
            TargetDocumentId = request.TargetDocumentId,
            LinkType = request.LinkType,
            Description = request.Description,
            CreatedBy = userId
        };

        var id = await _linkRepository.AddAsync(link);
        link.Id = id;

        // Log activity
        await _activityLogRepository.CreateAsync(new ActivityLog
        {
            NodeType = NodeType.Document,
            NodeId = request.SourceDocumentId,
            Action = "LinkCreated",
            Details = $"Linked to document: {request.TargetDocumentId} (Type: {request.LinkType})",
            UserId = userId
        });

        return await GetByIdAsync(id) ?? MapToDto(link);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateLinkRequest request)
    {
        var link = await _linkRepository.GetByIdAsync(id);
        if (link == null) return false;

        link.LinkType = request.LinkType;
        link.Description = request.Description;

        return await _linkRepository.UpdateAsync(link);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var link = await _linkRepository.GetByIdAsync(id);
        if (link == null) return false;

        var result = await _linkRepository.DeleteAsync(id);

        if (result)
        {
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = link.SourceDocumentId,
                Action = "LinkDeleted",
                Details = $"Link removed to document: {link.TargetDocumentId}",
                UserId = link.CreatedBy
            });
        }

        return result;
    }

    public async Task<int> GetLinkCountAsync(Guid documentId)
    {
        return await _linkRepository.GetLinkCountAsync(documentId);
    }

    private static DocumentLinkDto MapToDto(DocumentLink link)
    {
        return new DocumentLinkDto
        {
            Id = link.Id,
            SourceDocumentId = link.SourceDocumentId,
            SourceDocumentName = link.SourceDocumentName,
            TargetDocumentId = link.TargetDocumentId,
            TargetDocumentName = link.TargetDocumentName,
            TargetDocumentPath = link.TargetDocumentPath,
            LinkType = link.LinkType,
            Description = link.Description,
            CreatedBy = link.CreatedBy,
            CreatedByName = link.CreatedByName,
            CreatedAt = link.CreatedAt
        };
    }
}
