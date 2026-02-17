namespace DMS.DAL.Entities;

public class Classification
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Language { get; set; }
    public int SortOrder { get; set; }

    // Hierarchy fields for taxonomy tree
    public Guid? ParentId { get; set; }
    public int Level { get; set; } = 0;
    public string? Code { get; set; }
    public string? FullPath { get; set; }

    // NCAR governance defaults
    /// <summary>
    /// Default retention policy auto-applied to documents with this classification.
    /// </summary>
    public Guid? DefaultRetentionPolicyId { get; set; }

    /// <summary>
    /// Default privacy level auto-applied to documents with this classification.
    /// </summary>
    public Guid? DefaultPrivacyLevelId { get; set; }

    /// <summary>
    /// NCAR confidentiality level: Public, Internal, Confidential, Secret, TopSecret.
    /// </summary>
    public string? ConfidentialityLevel { get; set; }

    /// <summary>
    /// Whether documents with this classification require explicit disposal approval.
    /// </summary>
    public bool RequiresDisposalApproval { get; set; } = true;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
