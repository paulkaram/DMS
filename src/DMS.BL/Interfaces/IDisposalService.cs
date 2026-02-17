using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

/// <summary>
/// ISO 15489 compliant disposal service.
/// Manages controlled document destruction with certification.
/// </summary>
public interface IDisposalService
{
    /// <summary>
    /// Initiates disposal of a document (creates disposal request).
    /// </summary>
    Task<ServiceResult<DisposalRequestDto>> InitiateDisposalAsync(Guid documentId, InitiateDisposalDto dto, Guid userId);

    /// <summary>
    /// Approves a disposal request.
    /// </summary>
    Task<ServiceResult> ApproveDisposalAsync(Guid requestId, Guid userId, string? notes = null);

    /// <summary>
    /// Rejects a disposal request.
    /// </summary>
    Task<ServiceResult> RejectDisposalAsync(Guid requestId, Guid userId, string reason);

    /// <summary>
    /// Executes approved disposal and generates certificate.
    /// </summary>
    Task<ServiceResult<DisposalCertificateDto>> ExecuteDisposalAsync(Guid documentId, Guid userId, string disposalMethod = "HardDelete");

    /// <summary>
    /// Gets disposal certificate for a document.
    /// </summary>
    Task<ServiceResult<DisposalCertificateDto>> GetCertificateAsync(Guid certificateId);

    /// <summary>
    /// Gets disposal certificate by document ID.
    /// </summary>
    Task<ServiceResult<DisposalCertificateDto>> GetCertificateByDocumentAsync(Guid documentId);

    /// <summary>
    /// Gets all disposal certificates.
    /// </summary>
    Task<ServiceResult<List<DisposalCertificateDto>>> GetCertificatesAsync(DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets documents pending disposal (retention expired).
    /// </summary>
    Task<ServiceResult<List<DocumentDisposalStatusDto>>> GetPendingDisposalsAsync();

    /// <summary>
    /// Gets documents approaching retention expiration.
    /// </summary>
    Task<ServiceResult<List<DocumentDisposalStatusDto>>> GetUpcomingDisposalsAsync(int daysAhead = 30);

    /// <summary>
    /// Processes scheduled disposals (for background job).
    /// </summary>
    Task<DisposalBatchResult> ProcessScheduledDisposalsAsync(Guid? userId = null);

    // --- Batch Disposal with Multi-Level Approval ---

    /// <summary>
    /// Initiate a batch disposal request for multiple documents.
    /// </summary>
    Task<ServiceResult<DisposalRequestDetailDto>> InitiateBatchDisposalAsync(InitiateBatchDisposalDto dto, Guid userId);

    /// <summary>
    /// Get a disposal request with full details (documents + approvals).
    /// </summary>
    Task<ServiceResult<DisposalRequestDetailDto>> GetDisposalRequestAsync(Guid requestId);

    /// <summary>
    /// Get paginated list of disposal requests.
    /// </summary>
    Task<ServiceResult<PagedResultDto<DisposalRequestDto>>> GetDisposalRequestsAsync(string? status, int page, int pageSize);

    /// <summary>
    /// Submit an approval decision for a disposal request (level-by-level).
    /// </summary>
    Task<ServiceResult> SubmitDisposalApprovalAsync(Guid requestId, SubmitDisposalApprovalDto dto, Guid userId);

    /// <summary>
    /// Execute an approved batch disposal request.
    /// </summary>
    Task<ServiceResult<DisposalBatchResult>> ExecuteBatchDisposalAsync(Guid requestId, Guid userId);
}
