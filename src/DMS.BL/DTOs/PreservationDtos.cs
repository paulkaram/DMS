namespace DMS.BL.DTOs;

public class PreservationFormatDto
{
    public string Extension { get; set; } = string.Empty;
    public string FormatName { get; set; } = string.Empty;
    public string? PronomPuid { get; set; }
    public bool IsApprovedForPreservation { get; set; }
    public string? MigrationTargetFormat { get; set; }
    public string? Notes { get; set; }
}

public class PreservationMetadataDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string? FormatName { get; set; }
    public string? FormatIdentifier { get; set; }
    public string? FormatRegistry { get; set; }
    public bool IsPreservationFormat { get; set; }
    public string? MigrationTargetFormat { get; set; }
    public DateTime IdentifiedAt { get; set; }
    public string? IdentificationTool { get; set; }
    public string? CreatingApplication { get; set; }
}

public class PreservationSummaryDto
{
    public int TotalDocuments { get; set; }
    public int PreservationCompliant { get; set; }
    public int NeedsMigration { get; set; }
    public int PdfACompliant { get; set; }
    public List<FormatDistributionDto> FormatDistribution { get; set; } = new();
}

public class FormatDistributionDto
{
    public string Extension { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsPreservationFormat { get; set; }
}
