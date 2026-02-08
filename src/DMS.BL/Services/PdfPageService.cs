using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Entities;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DMS.BL.Services;

public class PdfPageService : IPdfPageService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly IDocumentVersionMetadataRepository _versionMetadataRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<PdfPageService> _logger;

    private static readonly HashSet<string> ImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/tiff", "image/bmp", "image/gif", "image/webp"
    };

    public PdfPageService(
        IDocumentRepository documentRepository,
        IDocumentVersionRepository versionRepository,
        IDocumentVersionMetadataRepository versionMetadataRepository,
        IFileStorageService fileStorageService,
        IActivityLogService activityLogService,
        ILogger<PdfPageService> logger)
    {
        _documentRepository = documentRepository;
        _versionRepository = versionRepository;
        _versionMetadataRepository = versionMetadataRepository;
        _fileStorageService = fileStorageService;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> GetPageCountAsync(Guid documentId)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<int>.Fail("Document not found");

        if (document.ContentType != "application/pdf")
            return ServiceResult<int>.Fail("Document is not a PDF");

        var fileStream = await _fileStorageService.GetFileAsync(document.StoragePath!);
        if (fileStream == null)
            return ServiceResult<int>.Fail("File not found in storage");

        try
        {
            using var pdf = PdfReader.Open(fileStream, PdfDocumentOpenMode.Import);
            return ServiceResult<int>.Ok(pdf.PageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read PDF for page count: {DocumentId}", documentId);
            return ServiceResult<int>.Fail("Failed to read PDF file");
        }
        finally
        {
            await fileStream.DisposeAsync();
        }
    }

    public async Task<ServiceResult<PageReorganizeResult>> ReorganizePagesAsync(
        Guid documentId,
        PageReorganizeRequest request,
        List<Stream> uploadStreams,
        List<string> uploadFileNames,
        List<string> uploadContentTypes,
        Guid userId)
    {
        // 1. Validate document
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
            return ServiceResult<PageReorganizeResult>.Fail("Document not found");

        if (document.ContentType != "application/pdf")
            return ServiceResult<PageReorganizeResult>.Fail("Page management is only supported for PDF documents");

        if (document.IsCheckedOut && document.CheckedOutBy != userId)
            return ServiceResult<PageReorganizeResult>.Fail("Document is currently checked out by another user");

        if (document.IsOnLegalHold)
            return ServiceResult<PageReorganizeResult>.Fail("Cannot modify a document under legal hold");

        if (request.Pages.Count == 0)
            return ServiceResult<PageReorganizeResult>.Fail("At least one page is required");

        // 2. Load current PDF
        var fileStream = await _fileStorageService.GetFileAsync(document.StoragePath!);
        if (fileStream == null)
            return ServiceResult<PageReorganizeResult>.Fail("Current PDF file not found in storage");

        try
        {
            using var sourcePdf = PdfReader.Open(fileStream, PdfDocumentOpenMode.Import);

            // Validate page references
            foreach (var entry in request.Pages)
            {
                if (entry.Source == "existing")
                {
                    if (!entry.OriginalPage.HasValue || entry.OriginalPage.Value < 1 || entry.OriginalPage.Value > sourcePdf.PageCount)
                        return ServiceResult<PageReorganizeResult>.Fail(
                            $"Invalid page reference: {entry.OriginalPage}. Document has {sourcePdf.PageCount} pages.");
                }
                else if (entry.Source == "upload")
                {
                    if (!entry.FileIndex.HasValue || entry.FileIndex.Value < 0 || entry.FileIndex.Value >= uploadStreams.Count)
                        return ServiceResult<PageReorganizeResult>.Fail(
                            $"Invalid file index: {entry.FileIndex}. {uploadStreams.Count} file(s) were uploaded.");
                }
            }

            // 3. Build new PDF
            using var outputStream = BuildReorganizedPdf(sourcePdf, request.Pages, uploadStreams, uploadFileNames, uploadContentTypes);

            // 4. Create new major version (mirrors RestoreVersionAsync pattern)
            var newMajorVersion = document.CurrentMajorVersion + 1;
            var newVersionNumber = document.CurrentVersion + 1;
            var currentVersion = await _versionRepository.GetLatestVersionAsync(documentId);

            var fileName = document.Name + (document.Extension ?? ".pdf");
            var storageResult = await _fileStorageService.SaveFileWithHashAsync(
                outputStream, fileName, documentId, newVersionNumber);

            // Count pages in the output
            outputStream.Position = 0;
            int outputPageCount;
            using (var verifyPdf = PdfReader.Open(outputStream, PdfDocumentOpenMode.Import))
            {
                outputPageCount = verifyPdf.PageCount;
            }

            var changeDesc = request.Comment ?? "Pages reorganized";
            var newVersion = new DocumentVersion
            {
                DocumentId = documentId,
                VersionNumber = newVersionNumber,
                StoragePath = storageResult.StoragePath,
                Size = storageResult.Size,
                Comment = changeDesc,
                CreatedBy = userId,
                IntegrityHash = storageResult.IntegrityHash,
                HashAlgorithm = storageResult.HashAlgorithm,
                IntegrityVerifiedAt = DateTime.UtcNow,
                ContentType = "application/pdf",
                OriginalFileName = fileName,
                VersionType = "Major",
                MajorVersion = newMajorVersion,
                MinorVersion = 0,
                VersionLabel = $"{newMajorVersion}.0",
                IsContentChanged = true,
                IsMetadataChanged = false,
                PreviousVersionId = currentVersion?.Id,
                ChangeDescription = changeDesc
            };

            var newVersionId = await _versionRepository.CreateAsync(newVersion);

            // Snapshot current metadata to the new version
            await _versionMetadataRepository.SnapshotCurrentMetadataToVersionAsync(documentId, newVersionId);

            // Update document record
            document.CurrentVersion = newVersionNumber;
            document.CurrentMajorVersion = newMajorVersion;
            document.CurrentMinorVersion = 0;
            document.CurrentVersionId = newVersionId;
            document.StoragePath = storageResult.StoragePath;
            document.Size = storageResult.Size;
            document.IntegrityHash = storageResult.IntegrityHash;
            document.HashAlgorithm = storageResult.HashAlgorithm;
            document.ModifiedBy = userId;
            await _documentRepository.UpdateAsync(document);

            // Log activity
            await _activityLogService.LogActivityAsync(
                "PagesReorganized", "Document", documentId, document.Name,
                $"Pages reorganized → v{newMajorVersion}.0 ({outputPageCount} pages)",
                userId, null, null);

            return ServiceResult<PageReorganizeResult>.Ok(new PageReorganizeResult
            {
                DocumentId = documentId,
                VersionLabel = $"{newMajorVersion}.0",
                PageCount = outputPageCount,
                FileSize = storageResult.Size
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorganize PDF pages for document {DocumentId}", documentId);
            return ServiceResult<PageReorganizeResult>.Fail("Failed to reorganize PDF pages: " + ex.Message);
        }
        finally
        {
            await fileStream.DisposeAsync();
        }
    }

    private MemoryStream BuildReorganizedPdf(
        PdfDocument sourcePdf,
        List<PageEntry> pageEntries,
        List<Stream> uploadStreams,
        List<string> uploadFileNames,
        List<string> uploadContentTypes)
    {
        var outputPdf = new PdfDocument();
        outputPdf.Info.Creator = "DMS Page Manager";

        // Cache opened upload PDFs so we don't re-open the same file for each page
        var uploadPdfCache = new Dictionary<int, PdfDocument>();

        try
        {
            foreach (var entry in pageEntries)
            {
                if (entry.Source == "existing" && entry.OriginalPage.HasValue)
                {
                    outputPdf.AddPage(sourcePdf.Pages[entry.OriginalPage.Value - 1]);
                }
                else if (entry.Source == "upload" && entry.FileIndex.HasValue)
                {
                    var fileIndex = entry.FileIndex.Value;
                    var contentType = uploadContentTypes[fileIndex];

                    if (contentType == "application/pdf")
                    {
                        // Get or open the uploaded PDF
                        if (!uploadPdfCache.TryGetValue(fileIndex, out var uploadPdf))
                        {
                            uploadStreams[fileIndex].Position = 0;
                            uploadPdf = PdfReader.Open(uploadStreams[fileIndex], PdfDocumentOpenMode.Import);
                            uploadPdfCache[fileIndex] = uploadPdf;
                        }

                        if (entry.UploadPageNumber.HasValue)
                        {
                            outputPdf.AddPage(uploadPdf.Pages[entry.UploadPageNumber.Value - 1]);
                        }
                        else
                        {
                            // Add all pages from the uploaded PDF
                            for (int i = 0; i < uploadPdf.PageCount; i++)
                                outputPdf.AddPage(uploadPdf.Pages[i]);
                        }
                    }
                    else if (ImageContentTypes.Contains(contentType))
                    {
                        AddImageAsPage(outputPdf, uploadStreams[fileIndex]);
                    }
                }
            }

            var output = new MemoryStream();
            outputPdf.Save(output, false);
            output.Position = 0;
            return output;
        }
        finally
        {
            foreach (var pdf in uploadPdfCache.Values)
                pdf.Dispose();
        }
    }

    /// <summary>
    /// Converts an image stream to a PDF page (same pattern as ScanService.BuildSearchablePdf).
    /// </summary>
    private static void AddImageAsPage(PdfDocument document, Stream imageStream)
    {
        imageStream.Position = 0;
        using var imgCopy = new MemoryStream();
        imageStream.CopyTo(imgCopy);
        imgCopy.Position = 0;

        var xImage = XImage.FromStream(imgCopy);

        // Assume 150 DPI (same as ScanService)
        var imageW = xImage.PixelWidth * 72.0 / 150;
        var imageH = xImage.PixelHeight * 72.0 / 150;

        var page = document.AddPage();
        page.Width = XUnit.FromPoint(imageW);
        page.Height = XUnit.FromPoint(imageH);

        var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawImage(xImage, 0, 0, page.Width.Point, page.Height.Point);
    }
}
