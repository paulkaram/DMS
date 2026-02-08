namespace DMS.BL.DTOs;

/// <summary>
/// Result of hash computation.
/// </summary>
public class IntegrityHashResult
{
    public string Hash { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "SHA256";
    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
    public long ContentLength { get; set; }
}

/// <summary>
/// Result of integrity verification.
/// </summary>
public class IntegrityVerificationResult
{
    public bool IsValid { get; set; }
    public string ExpectedHash { get; set; } = string.Empty;
    public string ComputedHash { get; set; } = string.Empty;
    public string Algorithm { get; set; } = "SHA256";
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public Guid? DocumentId { get; set; }
    public int? VersionNumber { get; set; }
}

/// <summary>
/// Result of batch integrity verification.
/// </summary>
public class IntegrityBatchResult
{
    public int TotalDocuments { get; set; }
    public int VerifiedCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<IntegrityVerificationResult> Failures { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
}

/// <summary>
/// DTO for integrity verification log display.
/// </summary>
public class IntegrityVerificationLogDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int? VersionNumber { get; set; }
    public string ExpectedHash { get; set; } = string.Empty;
    public string ComputedHash { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = "SHA256";
    public bool IsValid { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string VerificationType { get; set; } = string.Empty;
    public Guid? VerifiedBy { get; set; }
    public string? VerifiedByName { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ActionTaken { get; set; }
}
