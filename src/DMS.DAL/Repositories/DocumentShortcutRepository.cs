using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentShortcutRepository : IDocumentShortcutRepository
{
    private readonly DmsDbContext _context;

    public DocumentShortcutRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentShortcut>> GetByDocumentIdAsync(Guid documentId) =>
        await _context.DocumentShortcuts.AsNoTracking()
            .Where(ds => ds.DocumentId == documentId)
            .GroupJoin(_context.Documents.AsNoTracking(), ds => ds.DocumentId, d => d.Id, (ds, docs) => new { ds, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.ds, d })
            .GroupJoin(_context.Folders.AsNoTracking(), x => x.ds.FolderId, f => f.Id, (x, folders) => new { x.ds, x.d, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new DocumentShortcut
            {
                Id = x.ds.Id,
                DocumentId = x.ds.DocumentId,
                FolderId = x.ds.FolderId,
                CreatedBy = x.ds.CreatedBy,
                CreatedAt = x.ds.CreatedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                FolderName = f != null ? f.Name : null,
                FolderPath = f != null ? f.Path : null
            })
            .OrderBy(ds => ds.FolderName)
            .ToListAsync();

    public async Task<IEnumerable<DocumentShortcut>> GetByFolderIdAsync(Guid folderId) =>
        await _context.DocumentShortcuts.AsNoTracking()
            .Where(ds => ds.FolderId == folderId)
            .GroupJoin(_context.Documents.AsNoTracking(), ds => ds.DocumentId, d => d.Id, (ds, docs) => new { ds, docs })
            .SelectMany(x => x.docs.DefaultIfEmpty(), (x, d) => new { x.ds, d })
            .GroupJoin(_context.Folders.AsNoTracking(), x => x.ds.FolderId, f => f.Id, (x, folders) => new { x.ds, x.d, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new DocumentShortcut
            {
                Id = x.ds.Id,
                DocumentId = x.ds.DocumentId,
                FolderId = x.ds.FolderId,
                CreatedBy = x.ds.CreatedBy,
                CreatedAt = x.ds.CreatedAt,
                DocumentName = x.d != null ? x.d.Name : null,
                FolderName = f != null ? f.Name : null,
                FolderPath = f != null ? f.Path : null
            })
            .OrderBy(ds => ds.DocumentName)
            .ToListAsync();

    public async Task<DocumentShortcut?> GetByDocumentAndFolderAsync(Guid documentId, Guid folderId) =>
        await _context.DocumentShortcuts.AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.FolderId == folderId);

    public async Task<Guid> CreateAsync(DocumentShortcut entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;

        _context.DocumentShortcuts.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _context.DocumentShortcuts
            .Where(ds => ds.Id == id)
            .ExecuteDeleteAsync() > 0;

    public async Task<int> DeleteAllByDocumentIdAsync(Guid documentId) =>
        await _context.DocumentShortcuts
            .Where(ds => ds.DocumentId == documentId)
            .ExecuteDeleteAsync();
}
