using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

/// <summary>
/// ISO 15489/27001 compliant integrity verification service.
/// Provides cryptographic hash computation and verification for document authenticity.
/// </summary>
public interface IIntegrityService
{
    /// <summary>
    /// Computes SHA-256 hash of a stream.
    /// </summary>
    Task<IntegrityHashResult> ComputeHashAsync(Stream content);

    /// <summary>
    /// Verifies content integrity against stored hash.
    /// </summary>
    Task<IntegrityVerificationResult> VerifyIntegrityAsync(Stream content, string expectedHash, string algorithm = "SHA256");

    /// <summary>
    /// Verifies a document's current version integrity.
    /// </summary>
    Task<ServiceResult<IntegrityVerificationResult>> VerifyDocumentIntegrityAsync(Guid documentId, Guid? userId = null);

    /// <summary>
    /// Verifies a specific document version's integrity.
    /// </summary>
    Task<ServiceResult<IntegrityVerificationResult>> VerifyVersionIntegrityAsync(Guid documentId, int versionNumber, Guid? userId = null);

    /// <summary>
    /// Runs scheduled integrity verification for all documents.
    /// </summary>
    Task<IntegrityBatchResult> RunScheduledVerificationAsync(int batchSize = 100, Guid? userId = null);

    /// <summary>
    /// Gets integrity verification history for a document.
    /// </summary>
    Task<ServiceResult<List<IntegrityVerificationLogDto>>> GetVerificationHistoryAsync(Guid documentId);
}
