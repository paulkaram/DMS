using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class ArchivalExportService : IArchivalExportService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly DmsDbContext _context;
    private readonly ILogger<ArchivalExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ArchivalExportService(
        IDocumentRepository documentRepository,
        IActivityLogRepository activityLogRepository,
        IFileStorageService fileStorageService,
        DmsDbContext context,
        ILogger<ArchivalExportService> logger)
    {
        _documentRepository = documentRepository;
        _activityLogRepository = activityLogRepository;
        _fileStorageService = fileStorageService;
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<byte[]>> ExportDocumentArchiveAsync(Guid documentId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<byte[]>.Fail("Document not found");

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            await AddDocumentToArchive(archive, document, "");
        }

        _logger.LogInformation("Archival export package created for document {DocumentId}", documentId);
        return ServiceResult<byte[]>.Ok(memoryStream.ToArray());
    }

    public async Task<ServiceResult<byte[]>> ExportFolderArchiveAsync(Guid folderId)
    {
        var documents = await _documentRepository.GetByFolderIdAsync(folderId);
        var docList = documents.ToList();

        if (docList.Count == 0)
            return ServiceResult<byte[]>.Fail("No documents found in folder");

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var doc in docList)
            {
                var prefix = $"{SanitizeFileName(doc.Name)}-{doc.Id.ToString()[..8]}/";
                await AddDocumentToArchive(archive, doc, prefix);
            }
        }

        _logger.LogInformation("Archival export package created for folder {FolderId}: {Count} documents", folderId, docList.Count);
        return ServiceResult<byte[]>.Ok(memoryStream.ToArray());
    }

    private async Task AddDocumentToArchive(ZipArchive archive, Document document, string prefix)
    {
        var manifestEntries = new List<IntegrityManifestEntry>();

        // 1. Original file
        if (!string.IsNullOrEmpty(document.StoragePath))
        {
            var fileStream = await _fileStorageService.GetFileAsync(document.StoragePath, document.IsEncrypted);
            if (fileStream != null)
            {
                var fileName = $"{document.Name}{document.Extension}";
                var entry = archive.CreateEntry($"{prefix}{fileName}");
                using var entryStream = entry.Open();
                await fileStream.CopyToAsync(entryStream);

                // Compute hash for manifest
                fileStream.Position = 0;
                var hash = await ComputeHashAsync(fileStream);
                manifestEntries.Add(new IntegrityManifestEntry { FileName = fileName, Sha256Hash = hash, Size = fileStream.Length });
                await fileStream.DisposeAsync();
            }
        }

        // 2. metadata.json
        var metadata = BuildMetadataObject(document);
        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
        await AddTextEntry(archive, $"{prefix}metadata.json", metadataBytes);
        manifestEntries.Add(new IntegrityManifestEntry
        {
            FileName = "metadata.json",
            Sha256Hash = ComputeHashFromBytes(metadataBytes),
            Size = metadataBytes.Length
        });

        // 3. audit-trail.json
        var activityLogs = await _activityLogRepository.GetByNodeAsync(NodeType.Document, document.Id, 0, 10000);
        var auditTrail = activityLogs.Select(a => new
        {
            a.Id,
            a.Action,
            NodeType = a.NodeType.ToString(),
            a.NodeId,
            a.NodeName,
            a.Details,
            a.UserId,
            a.UserName,
            a.IpAddress,
            a.UserAgent,
            a.DeviceType,
            a.EntryHash,
            a.CreatedAt
        });
        var auditJson = JsonSerializer.Serialize(auditTrail, JsonOptions);
        var auditBytes = Encoding.UTF8.GetBytes(auditJson);
        await AddTextEntry(archive, $"{prefix}audit-trail.json", auditBytes);
        manifestEntries.Add(new IntegrityManifestEntry
        {
            FileName = "audit-trail.json",
            Sha256Hash = ComputeHashFromBytes(auditBytes),
            Size = auditBytes.Length
        });

        // 4. preservation-metadata.json (if exists)
        var preservation = await _context.Set<PreservationMetadata>()
            .AsNoTracking()
            .Where(p => p.DocumentId == document.Id)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (preservation != null)
        {
            var preservationJson = JsonSerializer.Serialize(new
            {
                preservation.FormatIdentifier,
                preservation.FormatName,
                preservation.FormatVersion,
                preservation.FormatRegistry,
                preservation.IsPreservationFormat,
                preservation.MigrationTargetFormat,
                preservation.IdentifiedAt,
                preservation.IdentificationTool,
                preservation.CreatingApplication,
                preservation.EnvironmentRequirements,
                preservation.SignificantProperties
            }, JsonOptions);
            var preservationBytes = Encoding.UTF8.GetBytes(preservationJson);
            await AddTextEntry(archive, $"{prefix}preservation-metadata.json", preservationBytes);
            manifestEntries.Add(new IntegrityManifestEntry
            {
                FileName = "preservation-metadata.json",
                Sha256Hash = ComputeHashFromBytes(preservationBytes),
                Size = preservationBytes.Length
            });
        }

        // 5. integrity-manifest.json (must be last — contains hashes of everything above)
        var manifest = new
        {
            DocumentId = document.Id,
            ExportedAt = DateTime.Now,
            HashAlgorithm = "SHA-256",
            Files = manifestEntries
        };
        var manifestJson = JsonSerializer.Serialize(manifest, JsonOptions);
        await AddTextEntry(archive, $"{prefix}integrity-manifest.json", Encoding.UTF8.GetBytes(manifestJson));
    }

    private static object BuildMetadataObject(Document document) => new
    {
        document.Id,
        document.Name,
        document.Description,
        document.Extension,
        document.ContentType,
        document.Size,
        document.CurrentVersion,
        document.CurrentMajorVersion,
        document.CurrentMinorVersion,
        State = document.State.ToString(),
        document.ClassificationId,
        document.ImportanceId,
        document.DocumentTypeId,
        document.ContentTypeId,
        document.RetentionPolicyId,
        document.IsOnLegalHold,
        document.LegalHoldId,
        document.IsOriginalRecord,
        document.ContentCategory,
        document.IntegrityHash,
        document.HashAlgorithm,
        document.IntegrityVerifiedAt,
        document.IsPdfACompliant,
        document.IsEncrypted,
        document.ExpiryDate,
        document.CreatedBy,
        document.CreatedAt,
        document.ModifiedBy,
        document.ModifiedAt,
        document.ArchivedAt,
        document.ArchivedBy,
        document.DisposedAt,
        document.DisposedBy
    };

    private static async Task AddTextEntry(ZipArchive archive, string path, byte[] content)
    {
        var entry = archive.CreateEntry(path);
        using var stream = entry.Open();
        await stream.WriteAsync(content);
    }

    private static async Task<string> ComputeHashAsync(Stream stream)
    {
        if (stream.CanSeek) stream.Position = 0;
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string ComputeHashFromBytes(byte[] data)
    {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(invalid.Contains(c) ? '_' : c);
        return sb.ToString();
    }

    private class IntegrityManifestEntry
    {
        public string FileName { get; set; } = string.Empty;
        public string Sha256Hash { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
