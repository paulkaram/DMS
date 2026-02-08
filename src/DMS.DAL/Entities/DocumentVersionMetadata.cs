namespace DMS.DAL.Entities;

/// <summary>
/// Stores a snapshot of metadata for a specific document version.
/// ISO 23081 compliant metadata versioning - enables historical comparison.
/// </summary>
public class DocumentVersionMetadata
{
    public Guid Id { get; set; }

    /// <summary>
    /// The document version this metadata belongs to.
    /// </summary>
    public Guid DocumentVersionId { get; set; }

    /// <summary>
    /// The parent document ID (denormalized for query performance).
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// The content type (metadata schema) this metadata belongs to.
    /// </summary>
    public Guid ContentTypeId { get; set; }

    /// <summary>
    /// The field definition ID.
    /// </summary>
    public Guid FieldId { get; set; }

    /// <summary>
    /// Field name (denormalized for query performance).
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// The text value of the metadata field.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Numeric value for range queries (stored separately for performance).
    /// </summary>
    public decimal? NumericValue { get; set; }

    /// <summary>
    /// Date value for date range queries (stored separately for performance).
    /// </summary>
    public DateTime? DateValue { get; set; }

    /// <summary>
    /// When this metadata snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
