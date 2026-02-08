namespace DMS.DAL.Entities;

public class DocumentLink
{
    public Guid Id { get; set; }
    public Guid SourceDocumentId { get; set; }
    public Guid TargetDocumentId { get; set; }
    public string LinkType { get; set; } = "related"; // related, reference, supersedes, attachment
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation/display properties
    public string? SourceDocumentName { get; set; }
    public string? TargetDocumentName { get; set; }
    public string? TargetDocumentPath { get; set; }
    public string? CreatedByName { get; set; }
}
