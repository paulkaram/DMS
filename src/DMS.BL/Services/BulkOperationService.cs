using System.IO.Compression;
using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;

namespace DMS.BL.Services;

public class BulkOperationService : IBulkOperationService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IActivityLogService _activityLogService;

    public BulkOperationService(
        IDocumentRepository documentRepository,
        IFileStorageService fileStorageService,
        IActivityLogService activityLogService)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
        _activityLogService = activityLogService;
    }

    public async Task<ServiceResult<BulkOperationResult>> BulkDeleteAsync(List<Guid> documentIds, Guid userId)
    {
        var result = new BulkOperationResult
        {
            TotalRequested = documentIds.Count
        };

        foreach (var docId in documentIds)
        {
            try
            {
                var document = await _documentRepository.GetByIdAsync(docId);
                if (document == null)
                {
                    result.FailedCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        DocumentId = docId,
                        DocumentName = "Unknown",
                        Error = "Document not found"
                    });
                    continue;
                }

                if (document.IsCheckedOut)
                {
                    result.FailedCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        DocumentId = docId,
                        DocumentName = document.Name,
                        Error = "Cannot delete a checked out document"
                    });
                    continue;
                }

                await _documentRepository.DeleteAsync(docId);
                await _activityLogService.LogActivityAsync(
                    ActivityActions.Deleted, "Document", docId, document.Name, "Bulk delete", userId, null, null);

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new BulkOperationError
                {
                    DocumentId = docId,
                    DocumentName = "Unknown",
                    Error = ex.Message
                });
            }
        }

        var message = $"Deleted {result.SuccessCount} of {result.TotalRequested} documents";
        return ServiceResult<BulkOperationResult>.Ok(result, message);
    }

    public async Task<ServiceResult<BulkOperationResult>> BulkMoveAsync(List<Guid> documentIds, Guid targetFolderId, Guid userId)
    {
        var result = new BulkOperationResult
        {
            TotalRequested = documentIds.Count
        };

        foreach (var docId in documentIds)
        {
            try
            {
                var document = await _documentRepository.GetByIdAsync(docId);
                if (document == null)
                {
                    result.FailedCount++;
                    result.Errors.Add(new BulkOperationError
                    {
                        DocumentId = docId,
                        DocumentName = "Unknown",
                        Error = "Document not found"
                    });
                    continue;
                }

                document.FolderId = targetFolderId;
                document.ModifiedBy = userId;

                await _documentRepository.UpdateAsync(document);
                await _activityLogService.LogActivityAsync(
                    ActivityActions.Moved, "Document", docId, document.Name, "Bulk move", userId, null, null);

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add(new BulkOperationError
                {
                    DocumentId = docId,
                    DocumentName = "Unknown",
                    Error = ex.Message
                });
            }
        }

        var message = $"Moved {result.SuccessCount} of {result.TotalRequested} documents";
        return ServiceResult<BulkOperationResult>.Ok(result, message);
    }

    public async Task<ServiceResult<Stream>> BulkDownloadAsync(List<Guid> documentIds, Guid userId)
    {
        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var fileNames = new Dictionary<string, int>(); // Track duplicate file names

            foreach (var docId in documentIds)
            {
                try
                {
                    var document = await _documentRepository.GetByIdAsync(docId);
                    if (document == null || string.IsNullOrEmpty(document.StoragePath))
                        continue;

                    var fileStream = await _fileStorageService.GetFileAsync(document.StoragePath);
                    if (fileStream == null)
                        continue;

                    // Handle duplicate file names
                    var fileName = document.Name + document.Extension;
                    if (fileNames.ContainsKey(fileName))
                    {
                        fileNames[fileName]++;
                        var nameWithoutExt = document.Name;
                        fileName = $"{nameWithoutExt} ({fileNames[fileName]}){document.Extension}";
                    }
                    else
                    {
                        fileNames[fileName] = 0;
                    }

                    var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await fileStream.CopyToAsync(entryStream);

                    await _activityLogService.LogActivityAsync(
                        ActivityActions.Downloaded, "Document", docId, document.Name, "Bulk download", userId, null, null);
                }
                catch
                {
                    // Skip files that fail to add
                }
            }
        }

        memoryStream.Position = 0;
        return ServiceResult<Stream>.Ok(memoryStream, "ZIP file created successfully");
    }
}
