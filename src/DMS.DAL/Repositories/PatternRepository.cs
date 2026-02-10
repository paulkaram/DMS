using System.Text.RegularExpressions;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DMS.DAL.Repositories;

public class PatternRepository : IPatternRepository
{
    private readonly DmsDbContext _context;

    public PatternRepository(DmsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pattern>> GetAllAsync(bool includeInactive = false)
    {
        var query = includeInactive
            ? _context.Patterns.IgnoreQueryFilters()
            : _context.Patterns.AsQueryable();

        return await query
            .AsNoTracking()
            .GroupJoin(_context.Folders.AsNoTracking(), p => p.TargetFolderId, f => f.Id, (p, folders) => new { p, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.p, f })
            .GroupJoin(_context.ContentTypeDefinitions.AsNoTracking(), x => x.p.ContentTypeId, ct => ct.Id, (x, cts) => new { x.p, x.f, cts })
            .SelectMany(x => x.cts.DefaultIfEmpty(), (x, ct) => new { x.p, x.f, ct })
            .GroupJoin(_context.Classifications.AsNoTracking(), x => x.p.ClassificationId, c => c.Id, (x, cs) => new { x.p, x.f, x.ct, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.p, x.f, x.ct, c })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.p.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.p, x.f, x.ct, x.c, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new Pattern
            {
                Id = x.p.Id,
                Name = x.p.Name,
                Regex = x.p.Regex,
                Description = x.p.Description,
                PatternType = x.p.PatternType,
                TargetFolderId = x.p.TargetFolderId,
                ContentTypeId = x.p.ContentTypeId,
                ClassificationId = x.p.ClassificationId,
                DocumentTypeId = x.p.DocumentTypeId,
                Priority = x.p.Priority,
                IsActive = x.p.IsActive,
                CreatedBy = x.p.CreatedBy,
                CreatedAt = x.p.CreatedAt,
                ModifiedBy = x.p.ModifiedBy,
                ModifiedAt = x.p.ModifiedAt,
                TargetFolderName = x.f != null ? x.f.Name : null,
                ContentTypeName = x.ct != null ? x.ct.Name : null,
                ClassificationName = x.c != null ? x.c.Name : null,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Pattern?> GetByIdAsync(Guid id)
    {
        return await _context.Patterns
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => p.Id == id)
            .GroupJoin(_context.Folders.AsNoTracking(), p => p.TargetFolderId, f => f.Id, (p, folders) => new { p, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new { x.p, f })
            .GroupJoin(_context.ContentTypeDefinitions.AsNoTracking(), x => x.p.ContentTypeId, ct => ct.Id, (x, cts) => new { x.p, x.f, cts })
            .SelectMany(x => x.cts.DefaultIfEmpty(), (x, ct) => new { x.p, x.f, ct })
            .GroupJoin(_context.Classifications.AsNoTracking(), x => x.p.ClassificationId, c => c.Id, (x, cs) => new { x.p, x.f, x.ct, cs })
            .SelectMany(x => x.cs.DefaultIfEmpty(), (x, c) => new { x.p, x.f, x.ct, c })
            .GroupJoin(_context.DocumentTypes.AsNoTracking(), x => x.p.DocumentTypeId, dt => dt.Id, (x, dts) => new { x.p, x.f, x.ct, x.c, dts })
            .SelectMany(x => x.dts.DefaultIfEmpty(), (x, dt) => new Pattern
            {
                Id = x.p.Id,
                Name = x.p.Name,
                Regex = x.p.Regex,
                Description = x.p.Description,
                PatternType = x.p.PatternType,
                TargetFolderId = x.p.TargetFolderId,
                ContentTypeId = x.p.ContentTypeId,
                ClassificationId = x.p.ClassificationId,
                DocumentTypeId = x.p.DocumentTypeId,
                Priority = x.p.Priority,
                IsActive = x.p.IsActive,
                CreatedBy = x.p.CreatedBy,
                CreatedAt = x.p.CreatedAt,
                ModifiedBy = x.p.ModifiedBy,
                ModifiedAt = x.p.ModifiedAt,
                TargetFolderName = x.f != null ? x.f.Name : null,
                ContentTypeName = x.ct != null ? x.ct.Name : null,
                ClassificationName = x.c != null ? x.c.Name : null,
                DocumentTypeName = dt != null ? dt.Name : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Pattern>> GetByTypeAsync(string patternType)
    {
        return await _context.Patterns
            .AsNoTracking()
            .Where(p => p.PatternType == patternType)
            .GroupJoin(_context.Folders.AsNoTracking(), p => p.TargetFolderId, f => f.Id, (p, folders) => new { p, folders })
            .SelectMany(x => x.folders.DefaultIfEmpty(), (x, f) => new Pattern
            {
                Id = x.p.Id,
                Name = x.p.Name,
                Regex = x.p.Regex,
                Description = x.p.Description,
                PatternType = x.p.PatternType,
                TargetFolderId = x.p.TargetFolderId,
                ContentTypeId = x.p.ContentTypeId,
                ClassificationId = x.p.ClassificationId,
                DocumentTypeId = x.p.DocumentTypeId,
                Priority = x.p.Priority,
                IsActive = x.p.IsActive,
                CreatedBy = x.p.CreatedBy,
                CreatedAt = x.p.CreatedAt,
                ModifiedBy = x.p.ModifiedBy,
                ModifiedAt = x.p.ModifiedAt,
                TargetFolderName = f != null ? f.Name : null
            })
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Pattern>> GetByFolderAsync(Guid folderId)
    {
        return await _context.Patterns
            .AsNoTracking()
            .Where(p => p.TargetFolderId == folderId)
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Pattern?> FindMatchingPatternAsync(string value, string? patternType = null)
    {
        var patterns = await _context.Patterns
            .AsNoTracking()
            .Where(p => patternType == null || p.PatternType == patternType)
            .OrderBy(p => p.Priority)
            .ToListAsync();

        foreach (var pattern in patterns)
        {
            try
            {
                if (Regex.IsMatch(value, pattern.Regex))
                {
                    return pattern;
                }
            }
            catch
            {
                // Invalid regex, skip
            }
        }

        return null;
    }

    public async Task<Guid> CreateAsync(Pattern pattern)
    {
        pattern.Id = Guid.NewGuid();
        pattern.CreatedAt = DateTime.UtcNow;

        _context.Patterns.Add(pattern);
        await _context.SaveChangesAsync();
        return pattern.Id;
    }

    public async Task<bool> UpdateAsync(Pattern pattern)
    {
        pattern.ModifiedAt = DateTime.UtcNow;

        var existing = await _context.Patterns.FindAsync(pattern.Id);
        if (existing == null) return false;

        existing.Name = pattern.Name;
        existing.Regex = pattern.Regex;
        existing.Description = pattern.Description;
        existing.PatternType = pattern.PatternType;
        existing.TargetFolderId = pattern.TargetFolderId;
        existing.ContentTypeId = pattern.ContentTypeId;
        existing.ClassificationId = pattern.ClassificationId;
        existing.DocumentTypeId = pattern.DocumentTypeId;
        existing.Priority = pattern.Priority;
        existing.IsActive = pattern.IsActive;
        existing.ModifiedBy = pattern.ModifiedBy;
        existing.ModifiedAt = pattern.ModifiedAt;

        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _context.Patterns
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.ModifiedAt, DateTime.UtcNow)) > 0;
    }

    public Task<bool> TestPatternAsync(string regex, string testValue)
    {
        try
        {
            var result = Regex.IsMatch(testValue, regex);
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
