using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IRecycleBinService
{
    Task<IEnumerable<RecycleBinItemDto>> GetUserRecycleBinAsync(Guid userId);
    Task<IEnumerable<RecycleBinItemDto>> GetAllAsync(int? nodeType = null);
    Task<bool> RestoreItemAsync(Guid id, Guid? restoreToFolderId = null);
    Task<bool> PermanentlyDeleteAsync(Guid id);
    Task<bool> EmptyRecycleBinAsync(Guid userId);
    Task<bool> PurgeExpiredAsync();
}
