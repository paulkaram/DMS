using DMS.ScanAgent.Models;

namespace DMS.ScanAgent;

public static class ScannerEndpoints
{
    public static void MapScannerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api");

        group.MapGet("/status", () => Results.Ok(new AgentStatus()));

        group.MapGet("/scanners", async (ScannerService service, string? driver) =>
        {
            try
            {
                var scanners = await service.GetScannersAsync(driver);
                return Results.Ok(scanners);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to list scanners: {ex.Message}");
            }
        });

        group.MapPost("/scan", async (ScanRequest request, ScannerService service) =>
        {
            try
            {
                var result = await service.ScanAsync(request);
                if (result.Pages.Count == 0)
                    return Results.BadRequest(new { error = "No pages scanned" });

                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Scan failed: {ex.Message}");
            }
        });
    }
}
