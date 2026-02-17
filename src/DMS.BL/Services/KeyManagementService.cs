using System.Security.Cryptography;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

/// <summary>
/// Envelope encryption: each document gets a unique DEK (AES-256),
/// the DEK is wrapped/encrypted with a master KEK stored securely.
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly DmsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyManagementService> _logger;

    public KeyManagementService(DmsDbContext context, IConfiguration configuration, ILogger<KeyManagementService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(byte[] Dek, Guid KeyId)> GenerateDocumentKeyAsync(Guid documentId)
    {
        // Generate random 256-bit DEK
        var dek = RandomNumberGenerator.GetBytes(32);

        // Wrap DEK with KEK
        var kek = GetKeyEncryptionKey();
        var wrappedKey = WrapKey(dek, kek);

        var entry = new EncryptionKeyStore
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            KeyVersion = 1,
            WrappedKey = Convert.ToBase64String(wrappedKey),
            KeyAlgorithm = "AES-256-CBC",
            CreatedAt = DateTime.Now
        };

        // Check for existing key version
        var existing = await _context.EncryptionKeyStore
            .Where(k => k.DocumentId == documentId)
            .OrderByDescending(k => k.KeyVersion)
            .FirstOrDefaultAsync();

        if (existing != null)
            entry.KeyVersion = existing.KeyVersion + 1;

        _context.EncryptionKeyStore.Add(entry);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated encryption key v{Version} for document {DocumentId}",
            entry.KeyVersion, documentId);

        return (dek, entry.Id);
    }

    public async Task<byte[]?> RetrieveDocumentKeyAsync(Guid documentId)
    {
        var entry = await _context.EncryptionKeyStore
            .Where(k => k.DocumentId == documentId)
            .OrderByDescending(k => k.KeyVersion)
            .FirstOrDefaultAsync();

        if (entry == null)
            return null;

        var kek = GetKeyEncryptionKey();
        var wrappedKey = Convert.FromBase64String(entry.WrappedKey);
        return UnwrapKey(wrappedKey, kek);
    }

    public async Task<ServiceResult> RotateKeyEncryptionKeyAsync(byte[] newKek)
    {
        var oldKek = GetKeyEncryptionKey();
        var allKeys = await _context.EncryptionKeyStore.ToListAsync();

        var rotated = 0;
        foreach (var entry in allKeys)
        {
            var wrappedKey = Convert.FromBase64String(entry.WrappedKey);
            var dek = UnwrapKey(wrappedKey, oldKek);
            var newWrapped = WrapKey(dek, newKek);
            entry.WrappedKey = Convert.ToBase64String(newWrapped);
            rotated++;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Rotated KEK for {Count} document keys", rotated);

        return ServiceResult.Ok($"Rotated {rotated} document encryption keys");
    }

    private byte[] GetKeyEncryptionKey()
    {
        // In production, KEK would come from DPAPI, HSM, or Azure Key Vault
        // For now, derive from configuration
        var keyString = _configuration["Encryption:MasterKey"]
            ?? "DefaultKEK_CHANGE_IN_PRODUCTION_32B!";
        return System.Text.Encoding.UTF8.GetBytes(keyString.PadRight(32, '!')[..32]);
    }

    private static byte[] WrapKey(byte[] dek, byte[] kek)
    {
        using var aes = Aes.Create();
        aes.Key = kek;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(dek, 0, dek.Length);
        // Prepend IV to encrypted DEK
        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);
        return result;
    }

    private static byte[] UnwrapKey(byte[] wrappedKey, byte[] kek)
    {
        using var aes = Aes.Create();
        aes.Key = kek;
        var iv = new byte[16];
        Buffer.BlockCopy(wrappedKey, 0, iv, 0, 16);
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(wrappedKey, 16, wrappedKey.Length - 16);
    }
}
