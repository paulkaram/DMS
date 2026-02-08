using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IScanService
{
    Task<ServiceResult<ScanProcessResult>> ProcessAndCreateDocumentAsync(
        ScanProcessRequest request,
        List<Stream> imageStreams,
        List<string> fileNames,
        Guid userId);
}
