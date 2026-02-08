using DMS.DAL.Entities;

namespace DMS.DAL.Repositories;

public interface IPatternRepository
{
    Task<IEnumerable<Pattern>> GetAllAsync(bool includeInactive = false);
    Task<Pattern?> GetByIdAsync(Guid id);
    Task<IEnumerable<Pattern>> GetByTypeAsync(string patternType);
    Task<IEnumerable<Pattern>> GetByFolderAsync(Guid folderId);
    Task<Pattern?> FindMatchingPatternAsync(string value, string? patternType = null);
    Task<Guid> CreateAsync(Pattern pattern);
    Task<bool> UpdateAsync(Pattern pattern);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> TestPatternAsync(string regex, string testValue);
}
