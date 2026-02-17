namespace DMS.DAL.Entities;

/// <summary>
/// Stores per-document encryption keys (DEK wrapped with KEK) for envelope encryption.
/// </summary>
public class EncryptionKeyStore
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int KeyVersion { get; set; } = 1;

    /// <summary>
    /// Data Encryption Key (DEK) encrypted/wrapped with the Key Encryption Key (KEK), base64 encoded.
    /// </summary>
    public string WrappedKey { get; set; } = string.Empty;

    /// <summary>
    /// Algorithm used: AES-256-CBC, AES-256-GCM, etc.
    /// </summary>
    public string KeyAlgorithm { get; set; } = "AES-256-CBC";

    public DateTime CreatedAt { get; set; }
}
