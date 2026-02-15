using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class FolderLinkRepository : IFolderLinkRepository
{
    private readonly DmsDbContext _context;

    public FolderLinkRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FolderLink>> GetBySourceFolderAsync(Guid sourceFolderId) =>
        await _context.FolderLinks.AsNoTracking()
            .Where(fl => fl.SourceFolderId == sourceFolderId)
            .Join(_context.Folders.AsNoTracking(), fl => fl.SourceFolderId, f => f.Id,
                (fl, sf) => new { fl, SourceFolderName = sf.Name })
            .Join(_context.Folders.AsNoTracking(), x => x.fl.TargetFolderId, f => f.Id,
                (x, tf) => new FolderLink
                {
                    Id = x.fl.Id,
                    SourceFolderId = x.fl.SourceFolderId,
                    TargetFolderId = x.fl.TargetFolderId,
                    CreatedBy = x.fl.CreatedBy,
                    CreatedAt = x.fl.CreatedAt,
                    SourceFolderName = x.SourceFolderName,
                    TargetFolderName = tf.Name,
                    TargetFolderPath = tf.Path
                })
            .OrderBy(fl => fl.TargetFolderName)
            .ToListAsync();

    public async Task<IEnumerable<FolderLink>> GetByTargetFolderAsync(Guid targetFolderId) =>
        await _context.FolderLinks.AsNoTracking()
            .Where(fl => fl.TargetFolderId == targetFolderId)
            .Join(_context.Folders.AsNoTracking(), fl => fl.SourceFolderId, f => f.Id,
                (fl, sf) => new { fl, SourceFolderName = sf.Name })
            .Join(_context.Folders.AsNoTracking(), x => x.fl.TargetFolderId, f => f.Id,
                (x, tf) => new FolderLink
                {
                    Id = x.fl.Id,
                    SourceFolderId = x.fl.SourceFolderId,
                    TargetFolderId = x.fl.TargetFolderId,
                    CreatedBy = x.fl.CreatedBy,
                    CreatedAt = x.fl.CreatedAt,
                    SourceFolderName = x.SourceFolderName,
                    TargetFolderName = tf.Name,
                    TargetFolderPath = tf.Path
                })
            .OrderBy(fl => fl.SourceFolderName)
            .ToListAsync();

    public async Task<Guid> CreateAsync(FolderLink entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.Now;

        _context.FolderLinks.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _context.FolderLinks.Where(fl => fl.Id == id)
            .ExecuteDeleteAsync() > 0;
}
