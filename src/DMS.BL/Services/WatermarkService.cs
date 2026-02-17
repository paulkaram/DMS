using DMS.BL.Interfaces;
using Microsoft.Extensions.Logging;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace DMS.BL.Services;

public class WatermarkService : IWatermarkService
{
    private readonly ILogger<WatermarkService> _logger;
    private static bool _fontResolverInitialized;
    private static readonly object _fontResolverLock = new();

    public WatermarkService(ILogger<WatermarkService> logger)
    {
        _logger = logger;
        EnsureFontResolver();
    }

    private static void EnsureFontResolver()
    {
        if (_fontResolverInitialized) return;
        lock (_fontResolverLock)
        {
            if (_fontResolverInitialized) return;
            if (GlobalFontSettings.FontResolver == null)
                GlobalFontSettings.FontResolver = new WindowsFontResolver();
            _fontResolverInitialized = true;
        }
    }

    public bool ShouldWatermark(string? privacyLevelName, int? privacyLevelValue)
    {
        return true;
    }

    public async Task<Stream> ApplyWatermarkAsync(Stream pdfStream, string userName, string? privacyLevelName = null)
    {
        try
        {
            if (pdfStream.CanSeek)
                pdfStream.Position = 0;

            // Read source PDF into a byte buffer
            var memoryStream = new MemoryStream();
            await pdfStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Use XPdfForm to treat the source PDF as a drawable image.
            // This avoids PdfReader.Open + Modify which is broken in PDFsharp 6
            // ("document already saved" error).
            var form = XPdfForm.FromStream(memoryStream);
            var outputDoc = new PdfDocument();

            var watermarkText = userName.ToUpperInvariant();
            var detailText = $"{(string.IsNullOrEmpty(privacyLevelName) ? "CONTROLLED COPY" : privacyLevelName.ToUpperInvariant())} | {DateTime.Now:yyyy-MM-dd HH:mm}";
            var font = new XFont("Arial", 32);
            var smallFont = new XFont("Arial", 9);
            var brush = new XSolidBrush(XColor.FromArgb(35, 120, 120, 120));
            var detailBrush = new XSolidBrush(XColor.FromArgb(50, 100, 100, 100));

            for (int i = 0; i < form.PageCount; i++)
            {
                form.PageIndex = i;

                var page = outputDoc.AddPage();
                page.Width = form.PointWidth;
                page.Height = form.PointHeight;

                var gfx = XGraphics.FromPdfPage(page);

                // Draw original page content
                gfx.DrawImage(form, 0, 0, form.PointWidth, form.PointHeight);

                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;

                // Tiled diagonal watermarks — subtle grey pattern across the page
                var textSize = gfx.MeasureString(watermarkText, font);
                double spacingX = textSize.Width + 120;
                double spacingY = textSize.Height + 160;

                // Calculate grid to cover page even when rotated
                double diagonal = Math.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
                int cols = (int)(diagonal / spacingX) + 2;
                int rows = (int)(diagonal / spacingY) + 2;

                gfx.Save();
                gfx.TranslateTransform(pageWidth / 2, pageHeight / 2);
                gfx.RotateTransform(-35);

                double startX = -(cols / 2.0) * spacingX;
                double startY = -(rows / 2.0) * spacingY;

                for (int row = 0; row < rows; row++)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        double x = startX + col * spacingX;
                        double y = startY + row * spacingY;
                        gfx.DrawString(watermarkText, font, brush, new XPoint(x, y));
                    }
                }

                gfx.Restore();

                // Bottom-right detail text — subtle footer
                var detailSize = gfx.MeasureString(detailText, smallFont);
                gfx.DrawString(detailText, smallFont, detailBrush,
                    new XPoint(pageWidth - detailSize.Width - 15, pageHeight - 15));

                gfx.Dispose();
            }

            var outputStream = new MemoryStream();
            outputDoc.Save(outputStream, false);
            outputStream.Position = 0;

            _logger.LogInformation(
                "Watermark applied: {WatermarkText}, User: {UserName}, Pages: {PageCount}",
                watermarkText, userName, form.PageCount);

            return outputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply watermark for user {UserName}. Error: {Message}", userName, ex.Message);
            if (pdfStream.CanSeek)
                pdfStream.Position = 0;
            return pdfStream;
        }
    }
}
