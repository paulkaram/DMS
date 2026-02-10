using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentCommentService : IDocumentCommentService
{
    private readonly IDocumentCommentRepository _commentRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDocumentRepository _documentRepository;

    public DocumentCommentService(
        IDocumentCommentRepository commentRepository,
        IActivityLogRepository activityLogRepository,
        IUserRepository userRepository,
        IDocumentRepository documentRepository)
    {
        _commentRepository = commentRepository;
        _activityLogRepository = activityLogRepository;
        _userRepository = userRepository;
        _documentRepository = documentRepository;
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
        var user = await _userRepository.GetByIdAsync(userId);
        var userName = user?.DisplayName ?? user?.Username ?? "Unknown User";
        var document = await _documentRepository.GetByIdAsync(request.DocumentId);
        var nodeName = document != null ? document.Name + (document.Extension ?? "") : null;
        await _activityLogRepository.CreateAsync(new ActivityLog
        {
            NodeType = NodeType.Document,
            NodeId = request.DocumentId,
            NodeName = nodeName,
            Action = request.ParentCommentId.HasValue ? "ReplyAdded" : "CommentAdded",
            Details = $"Comment added: {request.Content.Substring(0, Math.Min(100, request.Content.Length))}...",
            UserId = userId,
            UserName = userName
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
            var user = await _userRepository.GetByIdAsync(userId);
            var userName = user?.DisplayName ?? user?.Username ?? "Unknown User";
            var document = await _documentRepository.GetByIdAsync(comment.DocumentId);
            var nodeName = document != null ? document.Name + (document.Extension ?? "") : null;
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = comment.DocumentId,
                NodeName = nodeName,
                Action = "CommentUpdated",
                Details = $"Comment updated",
                UserId = userId,
                UserName = userName
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
            var user = await _userRepository.GetByIdAsync(userId);
            var userName = user?.DisplayName ?? user?.Username ?? "Unknown User";
            var document = await _documentRepository.GetByIdAsync(comment.DocumentId);
            var nodeName = document != null ? document.Name + (document.Extension ?? "") : null;
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = comment.DocumentId,
                NodeName = nodeName,
                Action = "CommentDeleted",
                Details = "Comment deleted",
                UserId = userId,
                UserName = userName
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
