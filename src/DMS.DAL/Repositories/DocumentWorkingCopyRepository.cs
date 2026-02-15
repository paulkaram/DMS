using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class DocumentWorkingCopyRepository : IDocumentWorkingCopyRepository
{
    private readonly DmsDbContext _context;

    public DocumentWorkingCopyRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentWorkingCopy?> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentWorkingCopies
            .AsNoTracking()
            .FirstOrDefaultAsync(wc => wc.DocumentId == documentId);
    }

    public async Task<IEnumerable<DocumentWorkingCopy>> GetAllByUserAsync(Guid userId)
    {
        return await _context.DocumentWorkingCopies
            .AsNoTracking()
            .Where(wc => wc.CheckedOutBy == userId)
            .OrderByDescending(wc => wc.CheckedOutAt)
            .ToListAsync();
    }

    public async Task<Guid> CreateAsync(DocumentWorkingCopy workingCopy)
    {
        workingCopy.Id = Guid.NewGuid();
        workingCopy.CheckedOutAt = DateTime.Now;

        _context.DocumentWorkingCopies.Add(workingCopy);
        await _context.SaveChangesAsync();

        return workingCopy.Id;
    }

    public async Task<bool> UpdateAsync(DocumentWorkingCopy workingCopy)
    {
        workingCopy.LastModifiedAt = DateTime.Now;

        var affected = await _context.DocumentWorkingCopies
            .Where(wc => wc.Id == workingCopy.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(wc => wc.DraftStoragePath, workingCopy.DraftStoragePath)
                .SetProperty(wc => wc.DraftSize, workingCopy.DraftSize)
                .SetProperty(wc => wc.DraftContentType, workingCopy.DraftContentType)
                .SetProperty(wc => wc.DraftOriginalFileName, workingCopy.DraftOriginalFileName)
                .SetProperty(wc => wc.DraftIntegrityHash, workingCopy.DraftIntegrityHash)
                .SetProperty(wc => wc.DraftMetadataJson, workingCopy.DraftMetadataJson)
                .SetProperty(wc => wc.DraftName, workingCopy.DraftName)
                .SetProperty(wc => wc.DraftDescription, workingCopy.DraftDescription)
                .SetProperty(wc => wc.DraftClassificationId, workingCopy.DraftClassificationId)
                .SetProperty(wc => wc.DraftImportanceId, workingCopy.DraftImportanceId)
                .SetProperty(wc => wc.DraftDocumentTypeId, workingCopy.DraftDocumentTypeId)
                .SetProperty(wc => wc.DraftExpiryDate, workingCopy.DraftExpiryDate)
                .SetProperty(wc => wc.DraftExpiryDateChanged, workingCopy.DraftExpiryDateChanged)
                .SetProperty(wc => wc.DraftPrivacyLevelId, workingCopy.DraftPrivacyLevelId)
                .SetProperty(wc => wc.DraftPrivacyLevelChanged, workingCopy.DraftPrivacyLevelChanged)
                .SetProperty(wc => wc.LastModifiedAt, workingCopy.LastModifiedAt)
                .SetProperty(wc => wc.AutoSaveEnabled, workingCopy.AutoSaveEnabled));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid documentId)
    {
        var affected = await _context.DocumentWorkingCopies
            .Where(wc => wc.DocumentId == documentId)
            .ExecuteDeleteAsync();

        return affected > 0;
    }

    public async Task<IEnumerable<DocumentWorkingCopy>> GetStaleCheckoutsAsync(int staleHours)
    {
        var cutoff = DateTime.Now.AddHours(-staleHours);
        return await _context.DocumentWorkingCopies
            .AsNoTracking()
            .Where(wc => wc.CheckedOutAt < cutoff)
            .OrderBy(wc => wc.CheckedOutAt)
            .ToListAsync();
    }

    public async Task<bool> DeleteAllByUserAsync(Guid userId)
    {
        var affected = await _context.DocumentWorkingCopies
            .Where(wc => wc.CheckedOutBy == userId)
            .ExecuteDeleteAsync();

        return affected > 0;
    }
}
