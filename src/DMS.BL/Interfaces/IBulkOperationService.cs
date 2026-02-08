using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IBulkOperationService
{
    Task<ServiceResult<BulkOperationResult>> BulkDeleteAsync(List<Guid> documentIds, Guid userId);
    Task<ServiceResult<BulkOperationResult>> BulkMoveAsync(List<Guid> documentIds, Guid targetFolderId, Guid userId);
    Task<ServiceResult<Stream>> BulkDownloadAsync(List<Guid> documentIds, Guid userId);
}
