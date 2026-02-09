using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class DocumentShortcutService : IDocumentShortcutService
{
    private readonly IDocumentShortcutRepository _shortcutRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IFolderRepository _folderRepository;

    public DocumentShortcutService(
        IDocumentShortcutRepository shortcutRepository,
        IDocumentRepository documentRepository,
        IFolderRepository folderRepository)
    {
        _shortcutRepository = shortcutRepository;
        _documentRepository = documentRepository;
        _folderRepository = folderRepository;
    }

    public async Task<ServiceResult<DocumentShortcutDto>> CreateShortcutAsync(CreateDocumentShortcutDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(dto.DocumentId);
        if (document == null)
            return ServiceResult<DocumentShortcutDto>.Fail("Document not found");

        var folder = await _folderRepository.GetByIdAsync(dto.FolderId);
        if (folder == null)
            return ServiceResult<DocumentShortcutDto>.Fail("Target folder not found");

        if (document.FolderId == dto.FolderId)
            return ServiceResult<DocumentShortcutDto>.Fail("Cannot create a shortcut in the document's own folder");

        var existing = await _shortcutRepository.GetByDocumentAndFolderAsync(dto.DocumentId, dto.FolderId);
        if (existing != null)
            return ServiceResult<DocumentShortcutDto>.Fail("A shortcut already exists for this document in the target folder");

        var entity = new DocumentShortcut
        {
            DocumentId = dto.DocumentId,
            FolderId = dto.FolderId,
            CreatedBy = userId
        };

        var id = await _shortcutRepository.CreateAsync(entity);

        var result = new DocumentShortcutDto
        {
            Id = id,
            DocumentId = dto.DocumentId,
            FolderId = dto.FolderId,
            DocumentName = document.Name,
            FolderName = folder.Name,
            CreatedAt = entity.CreatedAt
        };

        return ServiceResult<DocumentShortcutDto>.Ok(result, "Shortcut created successfully");
    }

    public async Task<ServiceResult> RemoveShortcutAsync(Guid shortcutId)
    {
        var deleted = await _shortcutRepository.DeleteAsync(shortcutId);
        if (!deleted)
            return ServiceResult.Fail("Shortcut not found");

        return ServiceResult.Ok("Shortcut removed successfully");
    }

    public async Task<ServiceResult<List<DocumentShortcutDto>>> GetShortcutsByDocumentAsync(Guid documentId)
    {
        var shortcuts = await _shortcutRepository.GetByDocumentIdAsync(documentId);
        var dtos = shortcuts.Select(s => new DocumentShortcutDto
        {
            Id = s.Id,
            DocumentId = s.DocumentId,
            FolderId = s.FolderId,
            DocumentName = s.DocumentName,
            FolderName = s.FolderName,
            FolderPath = s.FolderPath,
            CreatedAt = s.CreatedAt
        }).ToList();

        return ServiceResult<List<DocumentShortcutDto>>.Ok(dtos);
    }
}
