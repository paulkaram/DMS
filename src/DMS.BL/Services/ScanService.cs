using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DMS.BL.Services;

public class ScanService : IScanService
{
    private readonly IDocumentService _documentService;
    private readonly IScanConfigRepository _scanConfigRepository;
    private readonly ILogger<ScanService> _logger;
    private readonly ScanOptions _options;

    public ScanService(
        IDocumentService documentService,
        IScanConfigRepository scanConfigRepository,
        IOptions<ScanOptions> options,
        ILogger<ScanService> logger)
    {
        _documentService = documentService;
        _scanConfigRepository = scanConfigRepository;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ServiceResult<ScanProcessResult>> ProcessAndCreateDocumentAsync(
        ScanProcessRequest request,
        List<Stream> imageStreams,
        List<string> fileNames,
        Guid userId)
    {
        if (imageStreams.Count == 0)
            return ServiceResult<ScanProcessResult>.Fail("At least one image is required");

        if (string.IsNullOrWhiteSpace(request.DocumentName))
            return ServiceResult<ScanProcessResult>.Fail("Document name is required");

        if (request.Pages.Count == 0)
            return ServiceResult<ScanProcessResult>.Fail("At least one page instruction is required");

        if (request.Pages.Count > _options.MaxPagesPerScan)
            return ServiceResult<ScanProcessResult>.Fail($"Maximum {_options.MaxPagesPerScan} pages per scan");

        // Load scan config if provided
        var enableOCR = request.EnableOCR;
        var ocrLanguage = request.OcrLanguage;

        if (request.ScanConfigId.HasValue)
        {
            var config = await _scanConfigRepository.GetByIdAsync(request.ScanConfigId.Value);
            if (config != null)
            {
                enableOCR = config.EnableOCR;
                ocrLanguage = config.OcrLanguage;
            }
        }

        try
        {
            // Process pages: collect image bytes and OCR text
            var processedPages = new List<(byte[] imageBytes, int rotationDegrees, string? ocrText)>();

            foreach (var instruction in request.Pages)
            {
                if (instruction.FileIndex < 0 || instruction.FileIndex >= imageStreams.Count)
                {
                    _logger.LogWarning("Invalid file index {Index} for scan page", instruction.FileIndex);
                    continue;
                }

                var stream = imageStreams[instruction.FileIndex];
                stream.Position = 0;

                // Read raw image bytes
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                // Run OCR if enabled (on the raw image bytes)
                string? ocrText = null;
                if (enableOCR)
                {
                    ocrText = RunOcr(imageBytes, ocrLanguage);
                }

                processedPages.Add((imageBytes, instruction.RotationDegrees, ocrText));
            }

            if (processedPages.Count == 0)
                return ServiceResult<ScanProcessResult>.Fail("No valid pages to process");

            // Build searchable PDF
            var pdfStream = BuildSearchablePdf(processedPages);

            // Collect all OCR text
            var allOcrText = string.Join("\n\n", processedPages
                .Where(p => !string.IsNullOrEmpty(p.ocrText))
                .Select(p => p.ocrText));

            // Save as document via existing DocumentService
            var createDto = new CreateDocumentDto
            {
                FolderId = request.TargetFolderId,
                Name = request.DocumentName,
                Description = request.Description
            };

            pdfStream.Position = 0;
            var docResult = await _documentService.CreateAsync(
                createDto,
                pdfStream,
                $"{request.DocumentName}.pdf",
                "application/pdf",
                userId);

            await pdfStream.DisposeAsync();

            if (!docResult.Success)
                return ServiceResult<ScanProcessResult>.Fail(docResult.Errors);

            return ServiceResult<ScanProcessResult>.Ok(new ScanProcessResult
            {
                DocumentId = docResult.Data!.Id,
                DocumentName = docResult.Data.Name,
                PageCount = processedPages.Count,
                FileSize = docResult.Data.Size,
                OcrApplied = enableOCR,
                OcrText = allOcrText.Length > 500 ? allOcrText[..500] + "..." : allOcrText
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan processing failed");
            return ServiceResult<ScanProcessResult>.Fail($"Scan processing failed: {ex.Message}");
        }
    }

    private string? RunOcr(byte[] imageBytes, string language)
    {
        try
        {
            var tessDataPath = Path.GetFullPath(_options.TessDataPath);
            if (!Directory.Exists(tessDataPath))
            {
                _logger.LogWarning("Tesseract data directory not found at {Path}, skipping OCR", tessDataPath);
                return null;
            }

            using var engine = new Tesseract.TesseractEngine(tessDataPath, language, Tesseract.EngineMode.Default);
            using var pix = Tesseract.Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(pix);

            var text = page.GetText()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OCR failed for page, skipping");
            return null;
        }
    }

    private MemoryStream BuildSearchablePdf(List<(byte[] imageBytes, int rotationDegrees, string? ocrText)> pages)
    {
        var document = new PdfDocument();
        document.Info.Title = "Scanned Document";
        document.Info.Creator = "DMS Scanner";

        foreach (var (imageBytes, rotationDegrees, ocrText) in pages)
        {
            // Load image via PDFsharp
            using var imgStream = new MemoryStream(imageBytes);
            var xImage = XImage.FromStream(imgStream);

            // Determine page dimensions based on rotation
            var needsSwap = rotationDegrees == 90 || rotationDegrees == 270;
            var imageW = xImage.PixelWidth * 72.0 / 150; // Assume 150 DPI
            var imageH = xImage.PixelHeight * 72.0 / 150;

            var page = document.AddPage();
            page.Width = XUnit.FromPoint(needsSwap ? imageH : imageW);
            page.Height = XUnit.FromPoint(needsSwap ? imageW : imageH);

            var gfx = XGraphics.FromPdfPage(page);

            // Apply rotation transform and draw image
            if (rotationDegrees != 0)
            {
                var state = gfx.Save();
                // Move origin to center, rotate, then draw centered
                gfx.TranslateTransform(page.Width.Point / 2, page.Height.Point / 2);
                gfx.RotateTransform(rotationDegrees);
                gfx.DrawImage(xImage, -imageW / 2, -imageH / 2, imageW, imageH);
                gfx.Restore(state);
            }
            else
            {
                gfx.DrawImage(xImage, 0, 0, page.Width, page.Height);
            }

            // Add invisible OCR text layer for searchability
            if (!string.IsNullOrEmpty(ocrText))
            {
                var font = new XFont("Arial", 1);
                var brush = new XSolidBrush(XColor.FromArgb(0, 0, 0, 0));
                gfx.DrawString(ocrText, font, brush, new XRect(0, 0, page.Width, page.Height),
                    XStringFormats.TopLeft);
            }
        }

        var output = new MemoryStream();
        document.Save(output, false);
        output.Position = 0;
        return output;
    }
}
