using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Tesseract;

namespace DMS.Api.BackgroundJobs;

/// <summary>
/// Background job that extracts OCR text from image-based documents and PDF files,
/// stores OcrText on the Document entity, and queues for search indexing.
/// Runs every 30 seconds.
/// </summary>
public class OcrIndexingJob : RecurringJobService
{
    public OcrIndexingJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OcrIndexingJob> logger,
        IConfiguration configuration)
        : base(
            scopeFactory,
            logger,
            TimeSpan.FromSeconds(configuration.GetValue("BackgroundJobs:OcrIndexIntervalSeconds", 30)),
            "OcrIndexing")
    { }

    protected override async Task ExecuteJobAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var context = services.GetRequiredService<DmsDbContext>();
        var searchService = services.GetRequiredService<ISearchService>();
        var fileStorage = services.GetRequiredService<IFileStorageService>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<OcrIndexingJob>>();

        var ocrExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tiff", ".tif", ".bmp", ".gif"
        };

        // Find documents that need OCR: have supported extension, no OcrText yet, not disposed
        var documents = await context.Documents
            .Where(d => d.OcrText == null &&
                        d.Extension != null &&
                        d.State != DocumentState.Disposed &&
                        d.StoragePath != null)
            .OrderBy(d => d.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        // Filter to OCR-supported extensions in memory
        documents = documents.Where(d => ocrExtensions.Contains(d.Extension!)).ToList();

        if (documents.Count == 0) return;

        var tessDataPath = configuration.GetValue<string>("Scan:TessDataPath") ?? "./tessdata";
        var language = configuration.GetValue<string>("Scan:DefaultLanguage") ?? "eng";

        var processed = 0;
        foreach (var doc in documents)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                if (string.IsNullOrEmpty(doc.StoragePath)) continue;

                var stream = await fileStorage.GetFileAsync(doc.StoragePath);
                if (stream == null) continue;

                var ocrText = ExtractOcrText(stream, tessDataPath, language);
                if (!string.IsNullOrWhiteSpace(ocrText))
                {
                    doc.OcrText = ocrText;
                    await searchService.QueueForIndexingAsync(doc.Id, "Document", "Update");
                    processed++;
                }
                else
                {
                    // Mark as processed (empty) so we don't retry
                    doc.OcrText = "";
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "OCR extraction failed for document {Id}", doc.Id);
                doc.OcrText = ""; // Mark as attempted
            }
        }

        if (processed > 0 || documents.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("OCR indexing: {Processed}/{Total} documents extracted",
                processed, documents.Count);
        }
    }

    private static string? ExtractOcrText(Stream content, string tessDataPath, string language)
    {
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        var imageBytes = ms.ToArray();

        if (imageBytes.Length == 0) return null;

        try
        {
            using var engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
            using var img = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(img);
            var text = page.GetText()?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch
        {
            return null;
        }
    }
}
