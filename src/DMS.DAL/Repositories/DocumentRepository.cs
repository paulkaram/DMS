using DMS.DAL.DTOs;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DmsDbContext _context;

    public DocumentRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _context.Documents
            .AsNoTracking()
            .Include(d => d.PrivacyLevel)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        return await _context.Documents
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<PagedResult<Document>> GetAllPagedAsync(int page = 1, int pageSize = 50)
    {
        pageSize = Math.Min(pageSize, 200);
        var query = _context.Documents.AsNoTracking();
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<Document> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<Document>> GetByFolderIdAsync(Guid folderId)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(d => d.FolderId == folderId)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<PagedResult<Document>> GetByFolderIdPagedAsync(Guid folderId, int page = 1, int pageSize = 50)
    {
        pageSize = Math.Min(pageSize, 200);
        var query = _context.Documents.AsNoTracking().Where(d => d.FolderId == folderId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<Document> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }

    public async Task<IEnumerable<DocumentWithNames>> GetByFolderIdWithNamesAsync(Guid folderId)
    {
        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => d.FolderId == folderId)
            .ToListAsync();

        var shortcuts = await (
            from ds in _context.DocumentShortcuts.AsNoTracking()
            join d in _context.Documents.AsNoTracking() on ds.DocumentId equals d.Id
            where ds.FolderId == folderId
            select new { Doc = d, ShortcutId = ds.Id }
        ).ToListAsync();

        var result = await EnrichWithNamesAsync(
            documents,
            shortcuts.Select(s => (s.Doc, s.ShortcutId)).ToList());

        return result.OrderBy(d => d.Name);
    }

    public async Task<IEnumerable<Document>> SearchAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId)
    {
        var query = _context.Documents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(d => d.Name.Contains(name));
        if (folderId.HasValue)
            query = query.Where(d => d.FolderId == folderId.Value);
        if (classificationId.HasValue)
            query = query.Where(d => d.ClassificationId == classificationId.Value);
        if (documentTypeId.HasValue)
            query = query.Where(d => d.DocumentTypeId == documentTypeId.Value);

        return await query.OrderBy(d => d.Name).ToListAsync();
    }

    public async Task<IEnumerable<DocumentWithNames>> SearchWithNamesAsync(string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId)
    {
        var query = _context.Documents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(d => EF.Functions.Like(d.Name, $"%{name}%"));
        if (folderId.HasValue)
            query = query.Where(d => d.FolderId == folderId.Value);
        if (classificationId.HasValue)
            query = query.Where(d => d.ClassificationId == classificationId.Value);
        if (documentTypeId.HasValue)
            query = query.Where(d => d.DocumentTypeId == documentTypeId.Value);

        var documents = await query.ToListAsync();

        // Include shortcuts when searching within a specific folder
        List<(Document Doc, Guid ShortcutId)>? shortcutList = null;
        if (folderId.HasValue)
        {
            var shortcutQuery = from ds in _context.DocumentShortcuts.AsNoTracking()
                                join d in _context.Documents.AsNoTracking() on ds.DocumentId equals d.Id
                                where ds.FolderId == folderId.Value
                                select new { Doc = d, ShortcutId = ds.Id };

            if (!string.IsNullOrEmpty(name))
                shortcutQuery = shortcutQuery.Where(x => EF.Functions.Like(x.Doc.Name, $"%{name}%"));
            if (classificationId.HasValue)
                shortcutQuery = shortcutQuery.Where(x => x.Doc.ClassificationId == classificationId.Value);
            if (documentTypeId.HasValue)
                shortcutQuery = shortcutQuery.Where(x => x.Doc.DocumentTypeId == documentTypeId.Value);

            var shortcuts = await shortcutQuery.ToListAsync();
            shortcutList = shortcuts.Select(s => (s.Doc, s.ShortcutId)).ToList();
        }

        var result = await EnrichWithNamesAsync(documents, shortcutList);
        return result.OrderBy(d => d.Name);
    }

    public async Task<(List<DocumentWithNames> Items, int TotalCount)> SearchWithNamesPaginatedAsync(
        string? name, Guid? folderId, Guid? classificationId, Guid? documentTypeId, int page, int pageSize, int? userPrivacyLevel = null)
    {
        var query = _context.Documents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(name))
            query = query.Where(d => EF.Functions.Like(d.Name, $"%{name}%"));
        if (folderId.HasValue)
            query = query.Where(d => d.FolderId == folderId.Value);
        if (classificationId.HasValue)
            query = query.Where(d => d.ClassificationId == classificationId.Value);
        if (documentTypeId.HasValue)
            query = query.Where(d => d.DocumentTypeId == documentTypeId.Value);

        // Privacy level filtering: exclude documents above user's privacy level
        // Check both document-level privacy AND folder-level privacy
        if (userPrivacyLevel != null)
        {
            query = query.Where(d =>
                // Document-level privacy: if document has its own privacy level, check it
                (d.PrivacyLevelId == null || !_context.PrivacyLevels.Any(pl => pl.Id == d.PrivacyLevelId && pl.Level > userPrivacyLevel.Value))
                // Folder-level privacy: also check folder's privacy level
                && !_context.Folders.Any(f => f.Id == d.FolderId && f.PrivacyLevelId != null
                    && _context.PrivacyLevels.Any(pl => pl.Id == f.PrivacyLevelId && pl.Level > userPrivacyLevel.Value)));
        }

        var totalCount = await query.CountAsync();

        var documents = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Include shortcuts when searching within a specific folder
        List<(Document Doc, Guid ShortcutId)>? shortcutList = null;
        if (folderId.HasValue)
        {
            var shortcutQuery = from ds in _context.DocumentShortcuts.AsNoTracking()
                                join d in _context.Documents.AsNoTracking() on ds.DocumentId equals d.Id
                                where ds.FolderId == folderId.Value
                                select new { Doc = d, ShortcutId = ds.Id };

            if (!string.IsNullOrEmpty(name))
                shortcutQuery = shortcutQuery.Where(x => EF.Functions.Like(x.Doc.Name, $"%{name}%"));
            if (classificationId.HasValue)
                shortcutQuery = shortcutQuery.Where(x => x.Doc.ClassificationId == classificationId.Value);
            if (documentTypeId.HasValue)
                shortcutQuery = shortcutQuery.Where(x => x.Doc.DocumentTypeId == documentTypeId.Value);

            var shortcuts = await shortcutQuery.ToListAsync();
            shortcutList = shortcuts.Select(s => (s.Doc, s.ShortcutId)).ToList();
        }

        var result = await EnrichWithNamesAsync(documents, shortcutList);
        return (result.OrderBy(d => d.Name).ToList(), totalCount);
    }

    public async Task<IEnumerable<Document>> GetCheckedOutByUserAsync(Guid userId)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(d => d.CheckedOutBy == userId && d.IsCheckedOut)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentWithNames>> GetCheckedOutByUserWithNamesAsync(Guid userId)
    {
        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => d.CheckedOutBy == userId && d.IsCheckedOut)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return await EnrichWithNamesAsync(documents);
    }

    public async Task<IEnumerable<Document>> GetCreatedByUserAsync(Guid userId, int take = 50)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(d => d.CreatedBy == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<DocumentWithNames>> GetCreatedByUserWithNamesAsync(Guid userId, int take = 50)
    {
        var documents = await _context.Documents
            .AsNoTracking()
            .Where(d => d.CreatedBy == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .ToListAsync();

        return await EnrichWithNamesAsync(documents);
    }

    public async Task<bool> CheckOutAsync(Guid id, Guid userId)
    {
        var affected = await _context.Documents
            .Where(d => d.Id == id && !d.IsCheckedOut)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.IsCheckedOut, true)
                .SetProperty(d => d.CheckedOutBy, userId)
                .SetProperty(d => d.CheckedOutAt, DateTime.Now));

        return affected > 0;
    }

    public async Task<bool> CheckInAsync(Guid id, Guid userId, string? comment)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null || !document.IsCheckedOut || document.CheckedOutBy != userId)
            return false;

        var newVersion = document.CurrentVersion + 1;

        await _context.Documents
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.IsCheckedOut, false)
                .SetProperty(d => d.CheckedOutBy, (Guid?)null)
                .SetProperty(d => d.CheckedOutAt, (DateTime?)null)
                .SetProperty(d => d.CurrentVersion, newVersion)
                .SetProperty(d => d.ModifiedBy, (Guid?)userId)
                .SetProperty(d => d.ModifiedAt, DateTime.Now));

        return true;
    }

    public async Task<bool> DiscardCheckOutAsync(Guid id)
    {
        var affected = await _context.Documents
            .Where(d => d.Id == id && d.IsCheckedOut)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.IsCheckedOut, false)
                .SetProperty(d => d.CheckedOutBy, (Guid?)null)
                .SetProperty(d => d.CheckedOutAt, (DateTime?)null));

        return affected > 0;
    }

    public async Task<Document?> GetWithVersionsAsync(Guid id)
    {
        return await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Guid> CreateAsync(Document entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.Documents.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(Document entity)
    {
        entity.ModifiedAt = DateTime.Now;

        _context.Documents.Update(entity);
        var affected = await _context.SaveChangesAsync();

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var affected = await _context.Documents
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.IsActive, false)
                .SetProperty(d => d.ModifiedAt, DateTime.Now));

        return affected > 0;
    }

    /// <summary>
    /// Enriches a list of documents with user names, content type names, and attachment counts.
    /// </summary>
    private async Task<List<DocumentWithNames>> EnrichWithNamesAsync(
        List<Document> documents,
        List<(Document Doc, Guid ShortcutId)>? shortcuts = null)
    {
        var allDocs = shortcuts != null
            ? documents.Concat(shortcuts.Select(s => s.Doc)).ToList()
            : documents;

        if (allDocs.Count == 0)
            return new List<DocumentWithNames>();

        // Batch load user display names
        var userIds = allDocs
            .SelectMany(d => new[] { d.CreatedBy, d.CheckedOutBy })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct().ToList();

        var userNames = userIds.Count > 0
            ? await _context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.DisplayName })
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName)
            : new Dictionary<Guid, string>();

        // Batch load content type names
        var ctIds = allDocs
            .Select(d => d.ContentTypeId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct().ToList();

        var ctNames = ctIds.Count > 0
            ? await _context.ContentTypeDefinitions.AsNoTracking()
                .Where(ct => ctIds.Contains(ct.Id))
                .Select(ct => new { ct.Id, ct.Name })
                .ToDictionaryAsync(ct => ct.Id, ct => ct.Name)
            : new Dictionary<Guid, string>();

        // Batch load attachment counts
        var docIds = allDocs.Select(d => d.Id).Distinct().ToList();

        var attachCounts = await _context.DocumentAttachments.AsNoTracking()
            .Where(da => docIds.Contains(da.DocumentId))
            .GroupBy(da => da.DocumentId)
            .Select(g => new { DocumentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DocumentId, x => x.Count);

        // Batch load privacy levels for documents that have one
        var plIds = allDocs
            .Select(d => d.PrivacyLevelId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct().ToList();

        var privacyLevels = plIds.Count > 0
            ? await _context.PrivacyLevels.AsNoTracking()
                .Where(pl => plIds.Contains(pl.Id))
                .ToDictionaryAsync(pl => pl.Id)
            : new Dictionary<Guid, PrivacyLevel>();

        // Map regular documents
        var result = new List<DocumentWithNames>();

        foreach (var d in documents)
        {
            result.Add(MapToWithNames(d, userNames, ctNames, attachCounts, privacyLevels, false, null));
        }

        // Map shortcut documents
        if (shortcuts != null)
        {
            foreach (var (doc, shortcutId) in shortcuts)
            {
                result.Add(MapToWithNames(doc, userNames, ctNames, attachCounts, privacyLevels, true, shortcutId));
            }
        }

        return result;
    }

    private static DocumentWithNames MapToWithNames(
        Document d,
        Dictionary<Guid, string> userNames,
        Dictionary<Guid, string> ctNames,
        Dictionary<Guid, int> attachCounts,
        Dictionary<Guid, PrivacyLevel> privacyLevels,
        bool isShortcut,
        Guid? shortcutId)
    {
        PrivacyLevel? pl = null;
        if (d.PrivacyLevelId.HasValue)
            privacyLevels.TryGetValue(d.PrivacyLevelId.Value, out pl);

        return new DocumentWithNames
        {
            Id = d.Id,
            FolderId = d.FolderId,
            Name = d.Name,
            Description = d.Description,
            Extension = d.Extension,
            ContentType = d.ContentType,
            Size = d.Size,
            StoragePath = d.StoragePath,
            CurrentVersion = d.CurrentVersion,
            CurrentMajorVersion = d.CurrentMajorVersion,
            CurrentMinorVersion = d.CurrentMinorVersion,
            CurrentVersionId = d.CurrentVersionId,
            IsCheckedOut = d.IsCheckedOut,
            CheckedOutBy = d.CheckedOutBy,
            CheckedOutAt = d.CheckedOutAt,
            ClassificationId = d.ClassificationId,
            ImportanceId = d.ImportanceId,
            DocumentTypeId = d.DocumentTypeId,
            ContentTypeId = d.ContentTypeId,
            IntegrityHash = d.IntegrityHash,
            HashAlgorithm = d.HashAlgorithm,
            IntegrityVerifiedAt = d.IntegrityVerifiedAt,
            RetentionPolicyId = d.RetentionPolicyId,
            IsOnLegalHold = d.IsOnLegalHold,
            LegalHoldId = d.LegalHoldId,
            LegalHoldAppliedAt = d.LegalHoldAppliedAt,
            LegalHoldAppliedBy = d.LegalHoldAppliedBy,
            IsOriginalRecord = d.IsOriginalRecord,
            SourceDocumentId = d.SourceDocumentId,
            ContentCategory = d.ContentCategory,
            PrivacyLevelId = d.PrivacyLevelId,
            IsActive = d.IsActive,
            CreatedBy = d.CreatedBy,
            CreatedAt = d.CreatedAt,
            ModifiedBy = d.ModifiedBy,
            ModifiedAt = d.ModifiedAt,
            CreatedByName = d.CreatedBy.HasValue && userNames.TryGetValue(d.CreatedBy.Value, out var cn) ? cn : null,
            CheckedOutByName = d.CheckedOutBy.HasValue && userNames.TryGetValue(d.CheckedOutBy.Value, out var con) ? con : null,
            ContentTypeName = d.ContentTypeId.HasValue && ctNames.TryGetValue(d.ContentTypeId.Value, out var ctn) ? ctn : null,
            IsShortcut = isShortcut,
            ShortcutId = shortcutId,
            AttachmentCount = attachCounts.TryGetValue(d.Id, out var ac) ? ac : 0,
            PrivacyLevelName = pl?.Name,
            PrivacyLevelColor = pl?.Color,
            PrivacyLevelValue = pl?.Level
        };
    }
}
