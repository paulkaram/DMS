using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentCommentService : IDocumentCommentService
{
    private readonly IDocumentCommentRepository _commentRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public DocumentCommentService(
        IDocumentCommentRepository commentRepository,
        IActivityLogRepository activityLogRepository)
    {
        _commentRepository = commentRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<IEnumerable<DocumentCommentDto>> GetByDocumentIdAsync(Guid documentId)
    {
        var comments = await _commentRepository.GetByDocumentIdAsync(documentId);
        return comments.Select(MapToDto);
    }

    public async Task<IEnumerable<DocumentCommentDto>> GetRepliesAsync(Guid parentCommentId)
    {
        var replies = await _commentRepository.GetRepliesAsync(parentCommentId);
        return replies.Select(MapToDto);
    }

    public async Task<DocumentCommentDto?> GetByIdAsync(Guid id)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        return comment != null ? MapToDto(comment) : null;
    }

    public async Task<DocumentCommentDto> AddAsync(CreateCommentRequest request, Guid userId)
    {
        var comment = new DocumentComment
        {
            DocumentId = request.DocumentId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content,
            CreatedBy = userId
        };

        var id = await _commentRepository.AddAsync(comment);
        comment.Id = id;

        // Log activity
        await _activityLogRepository.CreateAsync(new ActivityLog
        {
            NodeType = NodeType.Document,
            NodeId = request.DocumentId,
            Action = request.ParentCommentId.HasValue ? "ReplyAdded" : "CommentAdded",
            Details = $"Comment added: {request.Content.Substring(0, Math.Min(100, request.Content.Length))}...",
            UserId = userId
        });

        return await GetByIdAsync(id) ?? MapToDto(comment);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateCommentRequest request, Guid userId)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        if (comment == null) return false;

        comment.Content = request.Content;
        comment.ModifiedBy = userId;

        var result = await _commentRepository.UpdateAsync(comment);

        if (result)
        {
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = comment.DocumentId,
                Action = "CommentUpdated",
                Details = $"Comment updated",
                UserId = userId
            });
        }

        return result;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var comment = await _commentRepository.GetByIdAsync(id);
        if (comment == null) return false;

        var result = await _commentRepository.DeleteAsync(id, userId);

        if (result)
        {
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = comment.DocumentId,
                Action = "CommentDeleted",
                Details = "Comment deleted",
                UserId = userId
            });
        }

        return result;
    }

    public async Task<int> GetCommentCountAsync(Guid documentId)
    {
        return await _commentRepository.GetCommentCountAsync(documentId);
    }

    private static DocumentCommentDto MapToDto(DocumentComment comment)
    {
        return new DocumentCommentDto
        {
            Id = comment.Id,
            DocumentId = comment.DocumentId,
            ParentCommentId = comment.ParentCommentId,
            Content = comment.Content,
            CreatedBy = comment.CreatedBy,
            CreatedByName = comment.CreatedByName,
            CreatedAt = comment.CreatedAt,
            ModifiedAt = comment.ModifiedAt,
            ReplyCount = comment.ReplyCount
        };
    }
}
