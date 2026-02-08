using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IPdfPageService
{
    Task<ServiceResult<int>> GetPageCountAsync(Guid documentId);

    Task<ServiceResult<PageReorganizeResult>> ReorganizePagesAsync(
        Guid documentId,
        PageReorganizeRequest request,
        List<Stream> uploadStreams,
        List<string> uploadFileNames,
        List<string> uploadContentTypes,
        Guid userId);
}
