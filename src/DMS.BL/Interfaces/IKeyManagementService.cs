using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

/// <summary>
/// Manages per-document envelope encryption with Key Encryption Keys (KEK)
/// and Data Encryption Keys (DEK).
/// </summary>
public interface IKeyManagementService
{
    /// <summary>
    /// Generate a new DEK for a document and store it wrapped with the KEK.
    /// </summary>
    Task<(byte[] Dek, Guid KeyId)> GenerateDocumentKeyAsync(Guid documentId);

    /// <summary>
    /// Retrieve and unwrap the DEK for a document.
    /// </summary>
    Task<byte[]?> RetrieveDocumentKeyAsync(Guid documentId);

    /// <summary>
    /// Rotate the KEK, re-wrapping all existing DEKs.
    /// </summary>
    Task<ServiceResult> RotateKeyEncryptionKeyAsync(byte[] newKek);
}
