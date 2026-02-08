using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentAttachmentService : IDocumentAttachmentService
{
    private readonly IDocumentAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IActivityLogRepository _activityLogRepository;

    public DocumentAttachmentService(
        IDocumentAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService,
        IActivityLogRepository activityLogRepository)
    {
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<IEnumerable<DocumentAttachmentDto>> GetByDocumentIdAsync(Guid documentId)
    {
        var attachments = await _attachmentRepository.GetByDocumentIdAsync(documentId);
        return attachments.Select(MapToDto);
    }

    public async Task<DocumentAttachmentDto?> GetByIdAsync(Guid id)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(id);
        return attachment != null ? MapToDto(attachment) : null;
    }

    public async Task<DocumentAttachmentDto> AddAsync(
        Guid documentId,
        string fileName,
        string? description,
        string contentType,
        long size,
        string storagePath,
        Guid userId)
    {
        var attachment = new DocumentAttachment
        {
            DocumentId = documentId,
            FileName = fileName,
            Description = description,
            ContentType = contentType,
            Size = size,
            StoragePath = storagePath,
            CreatedBy = userId
        };

        var id = await _attachmentRepository.AddAsync(attachment);
        attachment.Id = id;

        // Log activity
        await _activityLogRepository.CreateAsync(new ActivityLog
        {
            NodeType = NodeType.Document,
            NodeId = documentId,
            Action = "AttachmentAdded",
            Details = $"Attachment added: {fileName}",
            UserId = userId
        });

        return await GetByIdAsync(id) ?? MapToDto(attachment);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(id);
        if (attachment == null) return false;

        // Delete file from storage
        if (!string.IsNullOrEmpty(attachment.StoragePath))
        {
            await _fileStorageService.DeleteFileAsync(attachment.StoragePath);
        }

        var result = await _attachmentRepository.DeleteAsync(id);

        if (result)
        {
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = attachment.DocumentId,
                Action = "AttachmentDeleted",
                Details = $"Attachment deleted: {attachment.FileName}",
                UserId = attachment.CreatedBy
            });
        }

        return result;
    }

    public async Task<(Stream stream, string fileName, string contentType)?> DownloadAsync(Guid id)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(id);
        if (attachment == null || string.IsNullOrEmpty(attachment.StoragePath))
            return null;

        var stream = await _fileStorageService.GetFileAsync(attachment.StoragePath);
        if (stream == null) return null;

        return (stream, attachment.FileName, attachment.ContentType ?? "application/octet-stream");
    }

    public async Task<int> GetAttachmentCountAsync(Guid documentId)
    {
        return await _attachmentRepository.GetAttachmentCountAsync(documentId);
    }

    private static DocumentAttachmentDto MapToDto(DocumentAttachment attachment)
    {
        return new DocumentAttachmentDto
        {
            Id = attachment.Id,
            DocumentId = attachment.DocumentId,
            FileName = attachment.FileName,
            Description = attachment.Description,
            ContentType = attachment.ContentType,
            Size = attachment.Size,
            CreatedBy = attachment.CreatedBy,
            CreatedByName = attachment.CreatedByName,
            CreatedAt = attachment.CreatedAt
        };
    }
}
