using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IPreservationService
{
    Task<ServiceResult<PreservationMetadataDto>> GetDocumentPreservationAsync(Guid documentId);
    Task<ServiceResult<PreservationSummaryDto>> GetPreservationSummaryAsync();
    Task<ServiceResult<List<PreservationFormatDto>>> GetApprovedFormatsAsync();
}
