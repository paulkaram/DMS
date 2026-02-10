using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class ContentTypeRepository : IContentTypeRepository
{
    private readonly DmsDbContext _context;

    public ContentTypeRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ContentType>> GetAllAsync() =>
        await _context.ContentTypes.AsNoTracking()
            .OrderBy(ct => ct.Extension)
            .ToListAsync();

    public async Task<ContentType?> GetByIdAsync(Guid id) =>
        await _context.ContentTypes.AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Id == id);

    public async Task<ContentType?> GetByExtensionAsync(string extension) =>
        await _context.ContentTypes.AsNoTracking()
            .FirstOrDefaultAsync(ct => ct.Extension == extension.ToLower());

    public async Task<Guid> CreateAsync(ContentType entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        entity.Extension = entity.Extension.ToLower();

        _context.ContentTypes.Add(entity);
        await _context.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(ContentType entity)
    {
        entity.Extension = entity.Extension.ToLower();

        _context.ContentTypes.Update(entity);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id) =>
        await _context.ContentTypes.Where(ct => ct.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ct => ct.IsActive, false)) > 0;
}
