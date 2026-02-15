using System.Security.Cryptography;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

/// <summary>
/// ISO 15489/27001 compliant integrity verification service.
/// Provides cryptographic hash computation and verification for document authenticity.
/// </summary>
public class IntegrityService : IIntegrityService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly IIntegrityVerificationLogRepository _verificationLogRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<IntegrityService> _logger;

    public IntegrityService(
        IDocumentRepository documentRepository,
        IDocumentVersionRepository versionRepository,
        IIntegrityVerificationLogRepository verificationLogRepository,
        IFileStorageService fileStorageService,
        IActivityLogService activityLogService,
        ILogger<IntegrityService> logger)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _verificationLogRepository = verificationLogRepository;
        _fileStorageService = fileStorageService;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    /// <summary>
    /// Computes SHA-256 hash of a stream.
    /// </summary>
    public async Task<IntegrityHashResult> ComputeHashAsync(Stream content)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var contentLength = content.Length;

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(content);
        var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Reset stream position for subsequent operations
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return new IntegrityHashResult
        {
            Hash = hashString,
            Algorithm = "SHA256",
            ComputedAt = DateTime.Now,
            ContentLength = contentLength
        };
    }

    /// <summary>
    /// Verifies content integrity against stored hash.
    /// </summary>
    public async Task<IntegrityVerificationResult> VerifyIntegrityAsync(Stream content, string expectedHash, string algorithm = "SHA256")
    {
        var result = new IntegrityVerificationResult
        {
            ExpectedHash = expectedHash,
            Algorithm = algorithm,
            VerifiedAt = DateTime.Now
        };

        try
        {
            if (algorithm != "SHA256")
            {
                result.IsValid = false;
                result.ErrorMessage = $"Unsupported hash algorithm: {algorithm}";
                return result;
            }

            var hashResult = await ComputeHashAsync(content);
            result.ComputedHash = hashResult.Hash;
            result.IsValid = string.Equals(expectedHash, hashResult.Hash, StringComparison.OrdinalIgnoreCase);

            if (!result.IsValid)
            {
                result.ErrorMessage = "Hash mismatch - content may have been tampered with";
                _logger.LogWarning("Integrity verification failed. Expected: {Expected}, Computed: {Computed}",
                    expectedHash, hashResult.Hash);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Verification error: {ex.Message}";
            _logger.LogError(ex, "Error during integrity verification");
        }

        return result;
    }

    /// <summary>
    /// Verifies a document's current version integrity.
    /// </summary>
    public async Task<ServiceResult<IntegrityVerificationResult>> VerifyDocumentIntegrityAsync(Guid documentId, Guid? userId = null)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            return ServiceResult<IntegrityVerificationResult>.Fail("Document not found");
        }

        return await VerifyVersionIntegrityAsync(documentId, document.CurrentVersion, userId);
    }

    /// <summary>
    /// Verifies a specific document version's integrity.
    /// </summary>
    public async Task<ServiceResult<IntegrityVerificationResult>> VerifyVersionIntegrityAsync(Guid documentId, int versionNumber, Guid? userId = null)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            return ServiceResult<IntegrityVerificationResult>.Fail("Document not found");
        }

        var versions = await _versionRepository.GetByDocumentIdAsync(documentId);
        var version = versions.FirstOrDefault(v => v.VersionNumber == versionNumber);

        if (version == null)
        {
            return ServiceResult<IntegrityVerificationResult>.Fail("Version not found");
        }

        // Get expected hash from version or document
        var expectedHash = version.IntegrityHash ?? document.IntegrityHash;
        if (string.IsNullOrEmpty(expectedHash))
        {
            return ServiceResult<IntegrityVerificationResult>.Fail("No integrity hash stored for this version");
        }

        // Get file content
        var storagePath = version.StoragePath ?? document.StoragePath;
        if (string.IsNullOrEmpty(storagePath))
        {
            return ServiceResult<IntegrityVerificationResult>.Fail("Storage path not found");
        }

        using var fileStream = await _fileStorageService.GetFileAsync(storagePath);
        if (fileStream == null)
        {
            return ServiceResult<IntegrityVerificationResult>.Fail("File not found in storage");
        }

        // Verify integrity
        var algorithm = version.HashAlgorithm ?? document.HashAlgorithm ?? "SHA256";
        var result = await VerifyIntegrityAsync(fileStream, expectedHash, algorithm);
        result.DocumentId = documentId;
        result.VersionNumber = versionNumber;

        // Log verification
        var log = new IntegrityVerificationLog
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            VersionNumber = versionNumber,
            ExpectedHash = expectedHash,
            ComputedHash = result.ComputedHash,
            HashAlgorithm = algorithm,
            IsValid = result.IsValid,
            VerifiedAt = DateTime.Now,
            VerificationType = userId.HasValue ? "Manual" : "Scheduled",
            VerifiedBy = userId,
            ErrorMessage = result.ErrorMessage
        };

        await _verificationLogRepository.CreateAsync(log);

        // Update document/version verification timestamp if successful
        if (result.IsValid)
        {
            version.IntegrityVerifiedAt = DateTime.Now;
            await _versionRepository.UpdateAsync(version);

            if (versionNumber == document.CurrentVersion)
            {
                document.IntegrityVerifiedAt = DateTime.Now;
                await _documentRepository.UpdateAsync(document);
            }
        }
        else
        {
            // Log security event for failed verification
            await _activityLogService.LogActivityAsync(
                "IntegrityViolation",
                "Document",
                documentId,
                document.Name,
                $"Integrity verification failed for version {versionNumber}. Expected: {expectedHash}, Got: {result.ComputedHash}",
                userId,
                null,
                null);
        }

        return ServiceResult<IntegrityVerificationResult>.Ok(result);
    }

    /// <summary>
    /// Runs scheduled integrity verification for all documents.
    /// </summary>
    public async Task<IntegrityBatchResult> RunScheduledVerificationAsync(int batchSize = 100, Guid? userId = null)
    {
        var result = new IntegrityBatchResult
        {
            StartedAt = DateTime.Now
        };

        try
        {
            // Get documents that need verification (not verified recently or never verified)
            var documents = await _documentRepository.GetAllAsync();
            var documentsToVerify = documents
                .Where(d => d.IntegrityHash != null &&
                           (d.IntegrityVerifiedAt == null ||
                            d.IntegrityVerifiedAt < DateTime.Now.AddDays(-30)))
                .Take(batchSize)
                .ToList();

            result.TotalDocuments = documentsToVerify.Count;

            foreach (var document in documentsToVerify)
            {
                try
                {
                    var verifyResult = await VerifyDocumentIntegrityAsync(document.Id, userId);
                    result.VerifiedCount++;

                    if (verifyResult.Success && verifyResult.Data!.IsValid)
                    {
                        result.PassedCount++;
                    }
                    else if (verifyResult.Success && !verifyResult.Data!.IsValid)
                    {
                        result.FailedCount++;
                        result.Failures.Add(verifyResult.Data);
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error verifying document {DocumentId}", document.Id);
                    result.SkippedCount++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch integrity verification");
        }

        result.CompletedAt = DateTime.Now;
        return result;
    }

    /// <summary>
    /// Gets integrity verification history for a document.
    /// </summary>
    public async Task<ServiceResult<List<IntegrityVerificationLogDto>>> GetVerificationHistoryAsync(Guid documentId)
    {
        var logs = await _verificationLogRepository.GetByDocumentIdAsync(documentId);
        var dtos = logs.Select(l => new IntegrityVerificationLogDto
        {
            Id = l.Id,
            DocumentId = l.DocumentId,
            VersionNumber = l.VersionNumber,
            ExpectedHash = l.ExpectedHash,
            ComputedHash = l.ComputedHash,
            HashAlgorithm = l.HashAlgorithm,
            IsValid = l.IsValid,
            VerifiedAt = l.VerifiedAt,
            VerificationType = l.VerificationType,
            VerifiedBy = l.VerifiedBy,
            ErrorMessage = l.ErrorMessage,
            ActionTaken = l.ActionTaken
        }).ToList();

        return ServiceResult<List<IntegrityVerificationLogDto>>.Ok(dtos);
    }
}
