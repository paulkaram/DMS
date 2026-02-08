using System.Drawing;
using System.Drawing.Imaging;
using DMS.ScanAgent.Models;
using NAPS2.Images;
using NAPS2.Images.Gdi;
using NAPS2.Scan;

namespace DMS.ScanAgent;

public class ScannerService : IDisposable
{
    private const string TestScannerId = "test-scanner-001";
    private const string TestScannerName = "DMS Test Scanner (Virtual)";

    private readonly ScanningContext _scanningContext;
    private readonly ILogger<ScannerService> _logger;

    public ScannerService(ILogger<ScannerService> logger)
    {
        _logger = logger;
        _scanningContext = new ScanningContext(new GdiImageContext());

        // Enable 32-bit TWAIN worker for 64-bit processes
        try
        {
            _scanningContext.SetUpWin32Worker();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set up Win32 worker for TWAIN, TWAIN scanning may not work");
        }
    }

    public async Task<List<ScannerInfo>> GetScannersAsync(string? driverFilter = null)
    {
        var result = new List<ScannerInfo>();
        var controller = new ScanController(_scanningContext);

        var drivers = new[] { Driver.Wia, Driver.Escl, Driver.Twain };
        if (!string.IsNullOrEmpty(driverFilter))
        {
            drivers = driverFilter.ToLower() switch
            {
                "wia" => new[] { Driver.Wia },
                "twain" => new[] { Driver.Twain },
                "escl" => new[] { Driver.Escl },
                _ => drivers
            };
        }

        foreach (var driver in drivers)
        {
            try
            {
                var devices = await controller.GetDeviceList(driver);
                foreach (var device in devices)
                {
                    // Avoid duplicates by ID
                    if (result.All(r => r.Id != device.ID))
                    {
                        result.Add(new ScannerInfo
                        {
                            Id = device.ID,
                            Name = device.Name,
                            Driver = driver.ToString().ToLower()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list {Driver} scanners", driver);
            }
        }

        // Always include the test scanner for development/testing
        result.Insert(0, new ScannerInfo
        {
            Id = TestScannerId,
            Name = TestScannerName,
            Driver = "test"
        });

        return result;
    }

    public async Task<ScanResponse> ScanAsync(ScanRequest request)
    {
        // Handle test scanner
        if (request.ScannerId == TestScannerId)
        {
            return GenerateTestScan(request);
        }

        var controller = new ScanController(_scanningContext);

        var driver = ParseDriver(request.Driver);

        // Find the device
        var devices = await controller.GetDeviceList(driver);
        var device = devices.FirstOrDefault(d => d.ID == request.ScannerId)
            ?? throw new InvalidOperationException($"Scanner '{request.ScannerId}' not found");

        var options = new ScanOptions
        {
            Device = device,
            Driver = driver,
            Dpi = request.Dpi,
            BitDepth = ParseBitDepth(request.ColorMode),
            PageSize = ParsePageSize(request.PageSize),
            PaperSource = ParsePaperSource(request.PaperSource, request.Duplex),
        };

        var response = new ScanResponse();

        await foreach (var image in controller.Scan(options))
        {
            using (image)
            {
                using var ms = image.SaveToMemoryStream(ImageFileFormat.Jpeg);
                response.Pages.Add(new ScannedPage
                {
                    Data = Convert.ToBase64String(ms.ToArray()),
                    Format = "jpeg"
                });
            }
        }

        return response;
    }

    private ScanResponse GenerateTestScan(ScanRequest request)
    {
        _logger.LogInformation("Generating test scan page ({Dpi} DPI, {Color}, {PageSize})",
            request.Dpi, request.ColorMode, request.PageSize);

        // Generate an A4-proportioned test image
        var scale = request.Dpi / 72.0;
        var width = (int)(595 * scale);  // A4 width in points * scale
        var height = (int)(842 * scale); // A4 height in points * scale

        using var bitmap = new Bitmap(width, height);
        using var gfx = Graphics.FromImage(bitmap);

        // White background
        gfx.Clear(Color.White);

        // Draw border
        using var borderPen = new Pen(Color.FromArgb(200, 200, 200), 2);
        gfx.DrawRectangle(borderPen, 10, 10, width - 20, height - 20);

        // Header area
        var headerHeight = (int)(80 * scale);
        using var headerBrush = new SolidBrush(Color.FromArgb(0, 174, 140)); // teal
        gfx.FillRectangle(headerBrush, 20, 20, width - 40, headerHeight);

        // Header text
        using var titleFont = new Font("Arial", (float)(18 * scale / 3), FontStyle.Bold);
        using var whiteBrush = new SolidBrush(Color.White);
        gfx.DrawString("DMS Test Scanner - Sample Document", titleFont, whiteBrush, 40, 30 + headerHeight / 4);

        // Body text lines
        using var bodyFont = new Font("Arial", (float)(11 * scale / 3));
        using var textBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        var y = headerHeight + 60;
        var lineHeight = (int)(20 * scale / 2);

        var lines = new[]
        {
            $"Scan Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"Resolution: {request.Dpi} DPI",
            $"Color Mode: {request.ColorMode}",
            $"Page Size: {request.PageSize}",
            "",
            "This is a test page generated by the DMS Scan Agent.",
            "It simulates a scanned document for testing the",
            "scan-to-upload workflow without a physical scanner.",
            "",
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
            "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            "Ut enim ad minim veniam, quis nostrud exercitation ullamco.",
            "Laboris nisi ut aliquip ex ea commodo consequat.",
            "",
            "Document Management System - Scan Module Test",
        };

        foreach (var line in lines)
        {
            gfx.DrawString(line, bodyFont, textBrush, 40, y);
            y += lineHeight;
        }

        // Stamp / watermark
        using var stampFont = new Font("Arial", (float)(36 * scale / 3), FontStyle.Bold);
        using var stampBrush = new SolidBrush(Color.FromArgb(30, 0, 174, 140));
        var stampText = "TEST SCAN";
        gfx.TranslateTransform(width / 2f, height / 2f);
        gfx.RotateTransform(-30);
        var stampSize = gfx.MeasureString(stampText, stampFont);
        gfx.DrawString(stampText, stampFont, stampBrush, -stampSize.Width / 2, -stampSize.Height / 2);
        gfx.ResetTransform();

        // Convert to JPEG
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Jpeg);

        return new ScanResponse
        {
            Pages = new List<ScannedPage>
            {
                new()
                {
                    Data = Convert.ToBase64String(ms.ToArray()),
                    Format = "jpeg"
                }
            }
        };
    }

    private static Driver ParseDriver(string driver) => driver.ToLower() switch
    {
        "wia" => Driver.Wia,
        "twain" => Driver.Twain,
        "escl" => Driver.Escl,
        "sane" => Driver.Sane,
        _ => Driver.Default
    };

    private static BitDepth ParseBitDepth(string colorMode) => colorMode.ToLower() switch
    {
        "color" => BitDepth.Color,
        "grayscale" or "gray" => BitDepth.Grayscale,
        "bw" or "blackwhite" or "blackandwhite" => BitDepth.BlackAndWhite,
        _ => BitDepth.Color
    };

    private static PageSize ParsePageSize(string pageSize) => pageSize.ToUpper() switch
    {
        "A3" => PageSize.A3,
        "A4" => PageSize.A4,
        "A5" => PageSize.A5,
        "LETTER" => PageSize.Letter,
        "LEGAL" => PageSize.Legal,
        "B4" => PageSize.B4,
        "B5" => PageSize.B5,
        _ => PageSize.A4
    };

    private static PaperSource ParsePaperSource(string source, bool duplex)
    {
        if (duplex) return PaperSource.Duplex;
        return source.ToLower() switch
        {
            "flatbed" => PaperSource.Flatbed,
            "feeder" or "adf" => PaperSource.Feeder,
            "duplex" => PaperSource.Duplex,
            _ => PaperSource.Auto
        };
    }

    public void Dispose()
    {
        _scanningContext.Dispose();
    }
}
