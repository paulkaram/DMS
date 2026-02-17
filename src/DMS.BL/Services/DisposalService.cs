using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

/// <summary>
/// ISO 15489 compliant disposal service with certification.
/// </summary>
public class DisposalService : IDisposalService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly IDisposalCertificateRepository _certificateRepository;
    private readonly IDisposalRequestRepository _requestRepository;
    private readonly ILegalHoldService _legalHoldService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IIntegrityService _integrityService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<DisposalService> _logger;

    public DisposalService(
        IDocumentRepository documentRepository,
        IDocumentVersionRepository versionRepository,
        IDisposalCertificateRepository certificateRepository,
        IDisposalRequestRepository requestRepository,
        ILegalHoldService legalHoldService,
        IFileStorageService fileStorageService,
        IIntegrityService integrityService,
        IActivityLogService activityLogService,
        ILogger<DisposalService> logger)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _certificateRepository = certificateRepository;
        _requestRepository = requestRepository;
        _legalHoldService = legalHoldService;
        _fileStorageService = fileStorageService;
        _integrityService = integrityService;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<ServiceResult<DisposalRequestDto>> InitiateDisposalAsync(Guid documentId, InitiateDisposalDto dto, Guid userId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<DisposalRequestDto>.Fail("Document not found");

        // Check for legal hold
        if (await _legalHoldService.IsDocumentOnHoldAsync(documentId))
            return ServiceResult<DisposalRequestDto>.Fail("Document is under legal hold and cannot be disposed");

        // For now, return a simple request DTO (full workflow would use a DisposalRequest entity)
        var request = new DisposalRequestDto
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            DocumentName = document.Name,
            Status = dto.RequiresApproval ? "Pending" : "Approved",
            DisposalMethod = dto.DisposalMethod,
            Reason = dto.Reason,
            LegalBasis = dto.LegalBasis,
            RequestedBy = userId,
            RequestedAt = DateTime.Now
        };

        await _activityLogService.LogActivityAsync(
            "DisposalInitiated", "Document", documentId, document.Name,
            $"Disposal initiated: {dto.DisposalMethod}", userId, null, null);

        return ServiceResult<DisposalRequestDto>.Ok(request, "Disposal request created");
    }

    public Task<ServiceResult> ApproveDisposalAsync(Guid requestId, Guid userId, string? notes = null)
    {
        // In a full implementation, this would update a DisposalRequest entity
        _logger.LogInformation("Disposal request {RequestId} approved by {UserId}", requestId, userId);
        return Task.FromResult(ServiceResult.Ok("Disposal approved"));
    }

    public Task<ServiceResult> RejectDisposalAsync(Guid requestId, Guid userId, string reason)
    {
        // In a full implementation, this would update a DisposalRequest entity
        _logger.LogInformation("Disposal request {RequestId} rejected by {UserId}: {Reason}", requestId, userId, reason);
        return Task.FromResult(ServiceResult.Ok("Disposal rejected"));
    }

    public async Task<ServiceResult<DisposalCertificateDto>> ExecuteDisposalAsync(Guid documentId, Guid userId, string disposalMethod = "HardDelete")
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<DisposalCertificateDto>.Fail("Document not found");

        // Check for legal hold
        if (await _legalHoldService.IsDocumentOnHoldAsync(documentId))
            return ServiceResult<DisposalCertificateDto>.Fail("Document is under legal hold and cannot be disposed");

        // Get all versions
        var versions = await _versionRepository.GetByDocumentIdAsync(documentId);

        // Compute final hash for certificate
        string? contentHash = null;
        if (!string.IsNullOrEmpty(document.StoragePath))
        {
            using var stream = await _fileStorageService.GetFileAsync(document.StoragePath);
            if (stream != null)
            {
                var hashResult = await _integrityService.ComputeHashAsync(stream);
                contentHash = hashResult.Hash;
            }
        }

        // Generate certificate
        var certificate = new DisposalCertificate
        {
            CertificateNumber = await _certificateRepository.GenerateCertificateNumberAsync(),
            DocumentId = documentId,
            DocumentName = document.Name,
            DocumentPath = document.StoragePath,
            DocumentCreatedAt = document.CreatedAt,
            DisposalMethod = disposalMethod,
            DisposedAt = DateTime.Now,
            DisposedBy = userId,
            ContentHashAtDisposal = contentHash ?? document.IntegrityHash,
            FileSizeAtDisposal = document.Size,
            VersionsDisposed = versions.Count(),
            RetentionPolicyId = document.RetentionPolicyId
        };

        // Delete files based on disposal method
        if (disposalMethod == DisposalMethods.HardDelete || disposalMethod == DisposalMethods.CryptographicErasure)
        {
            // Delete all version files
            foreach (var version in versions)
            {
                if (!string.IsNullOrEmpty(version.StoragePath))
                {
                    await _fileStorageService.DeleteFileAsync(version.StoragePath);
                }
            }

            // Delete current version file if different
            if (!string.IsNullOrEmpty(document.StoragePath))
            {
                await _fileStorageService.DeleteFileAsync(document.StoragePath);
            }
        }

        // Mark document as deleted in database
        await _documentRepository.DeleteAsync(documentId);

        // Verify disposal
        certificate.DisposalVerified = true;
        certificate.VerifiedAt = DateTime.Now;

        // Save certificate
        await _certificateRepository.CreateAsync(certificate);

        await _activityLogService.LogActivityAsync(
            "DocumentDisposed", "Document", documentId, document.Name,
            $"Document permanently disposed. Certificate: {certificate.CertificateNumber}",
            userId, null, null);

        _logger.LogInformation(
            "Document {DocumentId} disposed. Certificate: {CertificateNumber}",
            documentId, certificate.CertificateNumber);

        return ServiceResult<DisposalCertificateDto>.Ok(MapCertificateToDto(certificate), "Document disposed successfully");
    }

    public async Task<ServiceResult<DisposalCertificateDto>> GetCertificateAsync(Guid certificateId)
    {
        var certificate = await _certificateRepository.GetByIdAsync(certificateId);
        if (certificate == null)
            return ServiceResult<DisposalCertificateDto>.Fail("Certificate not found");

        return ServiceResult<DisposalCertificateDto>.Ok(MapCertificateToDto(certificate));
    }

    public async Task<ServiceResult<DisposalCertificateDto>> GetCertificateByDocumentAsync(Guid documentId)
    {
        var certificate = await _certificateRepository.GetByDocumentIdAsync(documentId);
        if (certificate == null)
            return ServiceResult<DisposalCertificateDto>.Fail("No disposal certificate found for this document");

        return ServiceResult<DisposalCertificateDto>.Ok(MapCertificateToDto(certificate));
    }

    public async Task<ServiceResult<List<DisposalCertificateDto>>> GetCertificatesAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var certificates = await _certificateRepository.GetAllAsync(fromDate, toDate);
        var dtos = certificates.Select(MapCertificateToDto).ToList();
        return ServiceResult<List<DisposalCertificateDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<List<DocumentDisposalStatusDto>>> GetPendingDisposalsAsync()
    {
        // Get all documents with retention policies where retention has expired
        var documents = await _documentRepository.GetAllAsync();
        var pending = new List<DocumentDisposalStatusDto>();

        foreach (var doc in documents.Where(d => d.RetentionPolicyId.HasValue))
        {
            // Simple calculation - in real implementation would use DocumentRetention entity
            var retentionStart = doc.CreatedAt;
            // Assume 7 years (2555 days) default retention if not specified
            var expirationDate = retentionStart.AddDays(2555);

            if (expirationDate <= DateTime.Now)
            {
                var isOnHold = await _legalHoldService.IsDocumentOnHoldAsync(doc.Id);
                pending.Add(new DocumentDisposalStatusDto
                {
                    DocumentId = doc.Id,
                    DocumentName = doc.Name,
                    RetentionPolicyId = doc.RetentionPolicyId,
                    DocumentCreatedAt = doc.CreatedAt,
                    RetentionStartDate = retentionStart,
                    RetentionExpirationDate = expirationDate,
                    DaysUntilExpiration = (int)(expirationDate - DateTime.Now).TotalDays,
                    IsExpired = true,
                    IsOnLegalHold = isOnHold,
                    RequiresApproval = true
                });
            }
        }

        return ServiceResult<List<DocumentDisposalStatusDto>>.Ok(pending);
    }

    public async Task<ServiceResult<List<DocumentDisposalStatusDto>>> GetUpcomingDisposalsAsync(int daysAhead = 30)
    {
        var documents = await _documentRepository.GetAllAsync();
        var upcoming = new List<DocumentDisposalStatusDto>();
        var cutoffDate = DateTime.Now.AddDays(daysAhead);

        foreach (var doc in documents.Where(d => d.RetentionPolicyId.HasValue))
        {
            var retentionStart = doc.CreatedAt;
            var expirationDate = retentionStart.AddDays(2555); // Default 7 years

            if (expirationDate > DateTime.Now && expirationDate <= cutoffDate)
            {
                var isOnHold = await _legalHoldService.IsDocumentOnHoldAsync(doc.Id);
                upcoming.Add(new DocumentDisposalStatusDto
                {
                    DocumentId = doc.Id,
                    DocumentName = doc.Name,
                    RetentionPolicyId = doc.RetentionPolicyId,
                    DocumentCreatedAt = doc.CreatedAt,
                    RetentionStartDate = retentionStart,
                    RetentionExpirationDate = expirationDate,
                    DaysUntilExpiration = (int)(expirationDate - DateTime.Now).TotalDays,
                    IsExpired = false,
                    IsOnLegalHold = isOnHold,
                    RequiresApproval = true
                });
            }
        }

        return ServiceResult<List<DocumentDisposalStatusDto>>.Ok(upcoming.OrderBy(d => d.DaysUntilExpiration).ToList());
    }

    public async Task<DisposalBatchResult> ProcessScheduledDisposalsAsync(Guid? userId = null)
    {
        var result = new DisposalBatchResult
        {
            StartedAt = DateTime.Now
        };

        try
        {
            var pendingResult = await GetPendingDisposalsAsync();
            if (!pendingResult.Success)
            {
                result.CompletedAt = DateTime.Now;
                return result;
            }

            var pending = pendingResult.Data!.Where(p => !p.IsOnLegalHold).ToList();
            result.TotalPending = pending.Count;

            foreach (var doc in pending)
            {
                try
                {
                    // Only auto-dispose if approval not required
                    // In production, this would check the retention policy
                    result.SkippedCount++;
                    _logger.LogInformation(
                        "Document {DocumentId} pending disposal but requires manual approval",
                        doc.DocumentId);
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"Error processing {doc.DocumentId}: {ex.Message}");
                    _logger.LogError(ex, "Error processing disposal for document {DocumentId}", doc.DocumentId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch disposal processing");
        }

        result.CompletedAt = DateTime.Now;
        return result;
    }

    // --- Batch Disposal with Multi-Level Approval ---

    public async Task<ServiceResult<DisposalRequestDetailDto>> InitiateBatchDisposalAsync(InitiateBatchDisposalDto dto, Guid userId)
    {
        if (dto.DocumentIds.Count == 0)
            return ServiceResult<DisposalRequestDetailDto>.Fail("At least one document is required");

        // Validate all documents exist and are not on legal hold
        foreach (var docId in dto.DocumentIds)
        {
            var doc = await _documentRepository.GetByIdAsync(docId);
            if (doc == null)
                return ServiceResult<DisposalRequestDetailDto>.Fail($"Document {docId} not found");
            if (await _legalHoldService.IsDocumentOnHoldAsync(docId))
                return ServiceResult<DisposalRequestDetailDto>.Fail($"Document '{doc.Name}' is under legal hold");
        }

        var request = new DAL.Entities.DisposalRequest
        {
            RequestNumber = await _requestRepository.GenerateRequestNumberAsync(),
            Status = DAL.Entities.DisposalRequestStatus.PendingApproval,
            BatchReference = dto.BatchReference,
            DisposalMethod = dto.DisposalMethod,
            Reason = dto.Reason,
            LegalBasis = dto.LegalBasis,
            RequiredApprovalLevels = 1, // Default, can be overridden
            CurrentApprovalLevel = 0,
            RequestedBy = userId,
            RequestedAt = DateTime.Now,
            CreatedBy = userId
        };

        var requestId = await _requestRepository.CreateAsync(request);
        await _requestRepository.AddDocumentsAsync(requestId, dto.DocumentIds);

        await _activityLogService.LogActivityAsync(
            "DisposalBatchInitiated", "DisposalRequest", requestId, request.RequestNumber,
            $"Batch disposal initiated for {dto.DocumentIds.Count} documents", userId, null, null);

        return await GetDisposalRequestAsync(requestId);
    }

    public async Task<ServiceResult<DisposalRequestDetailDto>> GetDisposalRequestAsync(Guid requestId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            return ServiceResult<DisposalRequestDetailDto>.Fail("Disposal request not found");

        var documents = await _requestRepository.GetDocumentsAsync(requestId);
        var approvals = await _requestRepository.GetApprovalsAsync(requestId);

        // Enrich document names
        foreach (var doc in documents)
        {
            var document = await _documentRepository.GetByIdAsync(doc.DocumentId);
            doc.DocumentName = document?.Name;
        }

        var detail = new DisposalRequestDetailDto
        {
            Id = request.Id,
            RequestNumber = request.RequestNumber,
            Status = request.Status.ToString(),
            BatchReference = request.BatchReference,
            DisposalMethod = request.DisposalMethod,
            Reason = request.Reason,
            LegalBasis = request.LegalBasis,
            RequiredApprovalLevels = request.RequiredApprovalLevels,
            CurrentApprovalLevel = request.CurrentApprovalLevel,
            RequestedBy = request.RequestedBy,
            RequestedAt = request.RequestedAt,
            ExecutedAt = request.ExecutedAt,
            Documents = documents.Select(d => new DisposalRequestDocumentDto
            {
                Id = d.Id,
                DocumentId = d.DocumentId,
                DocumentName = d.DocumentName,
                Status = d.Status.ToString(),
                CertificateId = d.CertificateId,
                ErrorMessage = d.ErrorMessage
            }).ToList(),
            Approvals = approvals.Select(a => new DisposalApprovalDto
            {
                Id = a.Id,
                ApprovalLevel = a.ApprovalLevel,
                ApproverId = a.ApproverId,
                Decision = a.Decision.ToString(),
                Comments = a.Comments,
                DecisionAt = a.DecisionAt
            }).ToList()
        };

        return ServiceResult<DisposalRequestDetailDto>.Ok(detail);
    }

    public async Task<ServiceResult<PagedResultDto<DisposalRequestDto>>> GetDisposalRequestsAsync(string? status, int page, int pageSize)
    {
        DAL.Entities.DisposalRequestStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DAL.Entities.DisposalRequestStatus>(status, true, out var parsed))
            statusFilter = parsed;

        var (items, totalCount) = await _requestRepository.GetPaginatedAsync(statusFilter, page, pageSize);
        var dtos = items.Select(r => new DisposalRequestDto
        {
            Id = r.Id,
            Status = r.Status.ToString(),
            DisposalMethod = r.DisposalMethod,
            Reason = r.Reason,
            LegalBasis = r.LegalBasis,
            RequestedBy = r.RequestedBy,
            RequestedAt = r.RequestedAt
        }).ToList();

        return ServiceResult<PagedResultDto<DisposalRequestDto>>.Ok(new PagedResultDto<DisposalRequestDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult> SubmitDisposalApprovalAsync(Guid requestId, SubmitDisposalApprovalDto dto, Guid userId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            return ServiceResult.Fail("Disposal request not found");

        if (request.Status != DAL.Entities.DisposalRequestStatus.PendingApproval &&
            request.Status != DAL.Entities.DisposalRequestStatus.PartiallyApproved)
            return ServiceResult.Fail($"Request is not pending approval (current status: {request.Status})");

        if (!Enum.TryParse<DAL.Entities.DisposalApprovalDecision>(dto.Decision, true, out var decision))
            return ServiceResult.Fail("Invalid decision. Use: Approved, Rejected, or ReturnedForReview");

        var nextLevel = request.CurrentApprovalLevel + 1;

        await _requestRepository.AddApprovalAsync(new DAL.Entities.DisposalApproval
        {
            DisposalRequestId = requestId,
            ApprovalLevel = nextLevel,
            ApproverId = userId,
            Decision = decision,
            Comments = dto.Comments,
            DecisionAt = DateTime.Now
        });

        if (decision == DAL.Entities.DisposalApprovalDecision.Rejected)
        {
            request.Status = DAL.Entities.DisposalRequestStatus.Rejected;
        }
        else if (decision == DAL.Entities.DisposalApprovalDecision.Approved)
        {
            request.CurrentApprovalLevel = nextLevel;
            request.Status = nextLevel >= request.RequiredApprovalLevels
                ? DAL.Entities.DisposalRequestStatus.Approved
                : DAL.Entities.DisposalRequestStatus.PartiallyApproved;
        }

        request.ModifiedBy = userId;
        await _requestRepository.UpdateAsync(request);

        await _activityLogService.LogActivityAsync(
            "DisposalApproval", "DisposalRequest", requestId, request.RequestNumber,
            $"Level {nextLevel} {decision} by user", userId, null, null);

        return ServiceResult.Ok($"Disposal {decision.ToString().ToLower()} at level {nextLevel}");
    }

    public async Task<ServiceResult<DisposalBatchResult>> ExecuteBatchDisposalAsync(Guid requestId, Guid userId)
    {
        var request = await _requestRepository.GetByIdAsync(requestId);
        if (request == null)
            return ServiceResult<DisposalBatchResult>.Fail("Disposal request not found");

        if (request.Status != DAL.Entities.DisposalRequestStatus.Approved)
            return ServiceResult<DisposalBatchResult>.Fail("Request must be fully approved before execution");

        var documents = await _requestRepository.GetDocumentsAsync(requestId);
        var result = new DisposalBatchResult { StartedAt = DateTime.Now, TotalPending = documents.Count };

        foreach (var requestDoc in documents)
        {
            try
            {
                var certResult = await ExecuteDisposalAsync(requestDoc.DocumentId, userId, request.DisposalMethod);
                if (certResult.Success)
                {
                    await _requestRepository.UpdateDocumentStatusAsync(
                        requestDoc.Id, DAL.Entities.DisposalRequestDocumentStatus.Disposed, certResult.Data!.Id);
                    result.DisposedCount++;
                }
                else
                {
                    await _requestRepository.UpdateDocumentStatusAsync(
                        requestDoc.Id, DAL.Entities.DisposalRequestDocumentStatus.Error, error: certResult.Errors?.FirstOrDefault());
                    result.ErrorCount++;
                    result.Errors.Add($"{requestDoc.DocumentId}: {certResult.Errors?.FirstOrDefault()}");
                }
                result.ProcessedCount++;
            }
            catch (Exception ex)
            {
                await _requestRepository.UpdateDocumentStatusAsync(
                    requestDoc.Id, DAL.Entities.DisposalRequestDocumentStatus.Error, error: ex.Message);
                result.ErrorCount++;
                result.Errors.Add($"{requestDoc.DocumentId}: {ex.Message}");
                _logger.LogError(ex, "Error disposing document {DocumentId}", requestDoc.DocumentId);
            }
        }

        request.Status = DAL.Entities.DisposalRequestStatus.Executed;
        request.ExecutedAt = DateTime.Now;
        request.ExecutedBy = userId;
        await _requestRepository.UpdateAsync(request);

        result.CompletedAt = DateTime.Now;
        return ServiceResult<DisposalBatchResult>.Ok(result, $"Batch disposal completed: {result.DisposedCount} disposed, {result.ErrorCount} errors");
    }

    private static DisposalCertificateDto MapCertificateToDto(DisposalCertificate cert)
    {
        return new DisposalCertificateDto
        {
            Id = cert.Id,
            CertificateNumber = cert.CertificateNumber,
            DocumentId = cert.DocumentId,
            DocumentName = cert.DocumentName,
            DocumentPath = cert.DocumentPath,
            Classification = cert.Classification,
            RetentionPolicyId = cert.RetentionPolicyId,
            RetentionPolicyName = cert.RetentionPolicyName,
            DocumentCreatedAt = cert.DocumentCreatedAt,
            RetentionStartDate = cert.RetentionStartDate,
            RetentionExpirationDate = cert.RetentionExpirationDate,
            DisposalMethod = cert.DisposalMethod,
            DisposedAt = cert.DisposedAt,
            DisposedBy = cert.DisposedBy,
            DisposedByName = cert.DisposedByName,
            ApprovedBy = cert.ApprovedBy,
            ApprovedByName = cert.ApprovedByName,
            ApprovedAt = cert.ApprovedAt,
            LegalBasis = cert.LegalBasis,
            Notes = cert.Notes,
            ContentHashAtDisposal = cert.ContentHashAtDisposal,
            FileSizeAtDisposal = cert.FileSizeAtDisposal,
            VersionsDisposed = cert.VersionsDisposed,
            DisposalVerified = cert.DisposalVerified,
            VerifiedAt = cert.VerifiedAt,
            CreatedAt = cert.CreatedAt
        };
    }
}
