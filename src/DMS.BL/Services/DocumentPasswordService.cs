using System.Security.Cryptography;
using System.Text;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentPasswordService : IDocumentPasswordService
{
    private readonly IDocumentPasswordRepository _passwordRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public DocumentPasswordService(
        IDocumentPasswordRepository passwordRepository,
        IActivityLogRepository activityLogRepository)
    {
        _passwordRepository = passwordRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<DocumentPasswordDto?> GetByDocumentIdAsync(Guid documentId)
    {
        var password = await _passwordRepository.GetByDocumentIdAsync(documentId);
        return password != null ? MapToDto(password) : null;
    }

    public async Task<bool> HasPasswordAsync(Guid documentId)
    {
        return await _passwordRepository.HasPasswordAsync(documentId);
    }

    public async Task<bool> SetPasswordAsync(SetPasswordRequest request, Guid userId)
    {
        var passwordHash = HashPassword(request.Password);

        var password = new DocumentPassword
        {
            DocumentId = request.DocumentId,
            PasswordHash = passwordHash,
            Hint = request.Hint,
            ExpiresAt = request.ExpiresAt,
            CreatedBy = userId
        };

        await _passwordRepository.AddAsync(password);

        // Log activity
        await _activityLogRepository.CreateAsync(new ActivityLog
        {
            NodeType = NodeType.Document,
            NodeId = request.DocumentId,
            Action = "PasswordSet",
            Details = "Password protection enabled",
            UserId = userId
        });

        return true;
    }

    public async Task<bool> ValidatePasswordAsync(ValidatePasswordRequest request)
    {
        var passwordHash = HashPassword(request.Password);
        return await _passwordRepository.ValidatePasswordAsync(request.DocumentId, passwordHash);
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request, Guid userId)
    {
        // Validate current password first
        var currentHash = HashPassword(request.CurrentPassword);
        var isValid = await _passwordRepository.ValidatePasswordAsync(request.DocumentId, currentHash);

        if (!isValid)
        {
            return false;
        }

        // Set new password
        var newHash = HashPassword(request.NewPassword);
        var password = await _passwordRepository.GetByDocumentIdAsync(request.DocumentId);

        if (password == null) return false;

        password.PasswordHash = newHash;
        password.Hint = request.Hint;
        password.ModifiedBy = userId;

        var result = await _passwordRepository.UpdateAsync(password);

        if (result)
        {
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = request.DocumentId,
                Action = "PasswordChanged",
                Details = "Password changed",
                UserId = userId
            });
        }

        return result;
    }

    public async Task<bool> RemovePasswordAsync(Guid documentId, Guid userId)
    {
        var result = await _passwordRepository.DeleteAsync(documentId);

        if (result)
        {
            await _activityLogRepository.CreateAsync(new ActivityLog
            {
                NodeType = NodeType.Document,
                NodeId = documentId,
                Action = "PasswordRemoved",
                Details = "Password protection disabled",
                UserId = userId
            });
        }

        return result;
    }

    public async Task<string?> GetHintAsync(Guid documentId)
    {
        var password = await _passwordRepository.GetByDocumentIdAsync(documentId);
        return password?.Hint;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static DocumentPasswordDto MapToDto(DocumentPassword password)
    {
        return new DocumentPasswordDto
        {
            Id = password.Id,
            DocumentId = password.DocumentId,
            HasPassword = password.IsActive,
            Hint = password.Hint,
            ExpiresAt = password.ExpiresAt,
            CreatedAt = password.CreatedAt
        };
    }
}
