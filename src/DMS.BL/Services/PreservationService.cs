using DMS.BL.DTOs;
using DMS.BL.Interfaces;
using DMS.DAL.Data;
using DMS.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMS.BL.Services;

public class PreservationService : IPreservationService
{
    private readonly DmsDbContext _context;
    private readonly ILogger<PreservationService> _logger;

    // Approved long-term preservation formats per ISO 14721 (OAIS)
    private static readonly HashSet<string> PreservationFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".tiff", ".tif", ".png", ".xml", ".txt", ".csv", ".json"
    };

    public PreservationService(DmsDbContext context, ILogger<PreservationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<PreservationMetadataDto>> GetDocumentPreservationAsync(Guid documentId)
    {
        var metadata = await _context.PreservationMetadata
            .Where(p => p.DocumentId == documentId)
            .OrderByDescending(p => p.VersionNumber)
            .FirstOrDefaultAsync();

        if (metadata == null)
            return ServiceResult<PreservationMetadataDto>.Fail("No preservation metadata found");

        return ServiceResult<PreservationMetadataDto>.Ok(new PreservationMetadataDto
        {
            Id = metadata.Id,
            DocumentId = metadata.DocumentId,
            VersionNumber = metadata.VersionNumber,
            FormatName = metadata.FormatName,
            FormatIdentifier = metadata.FormatIdentifier,
            FormatRegistry = metadata.FormatRegistry,
            IsPreservationFormat = metadata.IsPreservationFormat,
            MigrationTargetFormat = metadata.MigrationTargetFormat,
            IdentifiedAt = metadata.IdentifiedAt,
            IdentificationTool = metadata.IdentificationTool,
            CreatingApplication = metadata.CreatingApplication
        });
    }

    public async Task<ServiceResult<PreservationSummaryDto>> GetPreservationSummaryAsync()
    {
        var totalDocs = await _context.Documents
            .Where(d => d.State == DocumentState.Record || d.State == DocumentState.Archived)
            .CountAsync();

        var pdfACount = await _context.Documents
            .Where(d => (d.State == DocumentState.Record || d.State == DocumentState.Archived)
                        && d.IsPdfACompliant == true)
            .CountAsync();

        var preservationCompliant = await _context.Documents
            .Where(d => (d.State == DocumentState.Record || d.State == DocumentState.Archived)
                        && d.Extension != null)
            .ToListAsync();

        var compliantCount = preservationCompliant.Count(d => PreservationFormats.Contains(d.Extension!));
        var needsMigration = preservationCompliant.Count(d => !PreservationFormats.Contains(d.Extension!));

        // Format distribution
        var formatDist = preservationCompliant
            .Where(d => d.Extension != null)
            .GroupBy(d => d.Extension!.ToLower())
            .Select(g => new FormatDistributionDto
            {
                Extension = g.Key,
                Count = g.Count(),
                IsPreservationFormat = PreservationFormats.Contains(g.Key)
            })
            .OrderByDescending(f => f.Count)
            .ToList();

        return ServiceResult<PreservationSummaryDto>.Ok(new PreservationSummaryDto
        {
            TotalDocuments = totalDocs,
            PreservationCompliant = compliantCount,
            NeedsMigration = needsMigration,
            PdfACompliant = pdfACount,
            FormatDistribution = formatDist
        });
    }

    public Task<ServiceResult<List<PreservationFormatDto>>> GetApprovedFormatsAsync()
    {
        var formats = new List<PreservationFormatDto>
        {
            new() { Extension = ".pdf", FormatName = "PDF/A-1b", PronomPuid = "fmt/354", IsApprovedForPreservation = true },
            new() { Extension = ".tiff", FormatName = "TIFF 6.0", PronomPuid = "fmt/353", IsApprovedForPreservation = true },
            new() { Extension = ".tif", FormatName = "TIFF 6.0", PronomPuid = "fmt/353", IsApprovedForPreservation = true },
            new() { Extension = ".png", FormatName = "PNG 1.2", PronomPuid = "fmt/13", IsApprovedForPreservation = true },
            new() { Extension = ".xml", FormatName = "XML 1.0", PronomPuid = "fmt/101", IsApprovedForPreservation = true },
            new() { Extension = ".txt", FormatName = "Plain Text (UTF-8)", PronomPuid = "x-fmt/111", IsApprovedForPreservation = true },
            new() { Extension = ".csv", FormatName = "CSV", PronomPuid = "x-fmt/18", IsApprovedForPreservation = true },
            new() { Extension = ".json", FormatName = "JSON", PronomPuid = "fmt/817", IsApprovedForPreservation = true },
            new() { Extension = ".docx", FormatName = "Office Open XML Document", PronomPuid = "fmt/412", IsApprovedForPreservation = false, MigrationTargetFormat = "PDF/A-1b" },
            new() { Extension = ".xlsx", FormatName = "Office Open XML Spreadsheet", PronomPuid = "fmt/214", IsApprovedForPreservation = false, MigrationTargetFormat = "CSV" },
            new() { Extension = ".pptx", FormatName = "Office Open XML Presentation", PronomPuid = "fmt/215", IsApprovedForPreservation = false, MigrationTargetFormat = "PDF/A-1b" },
            new() { Extension = ".jpg", FormatName = "JPEG", PronomPuid = "fmt/44", IsApprovedForPreservation = false, MigrationTargetFormat = "TIFF 6.0", Notes = "Lossy compression not ideal for preservation" },
        };

        return Task.FromResult(ServiceResult<List<PreservationFormatDto>>.Ok(formats));
    }
}
