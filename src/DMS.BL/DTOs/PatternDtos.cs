namespace DMS.BL.DTOs;

public class CreatePatternRequest
{
    public string Name { get; set; } = string.Empty;
    public string Regex { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PatternType { get; set; } = "Naming";
    public Guid? TargetFolderId { get; set; }
    public Guid? ContentTypeId { get; set; }
    public Guid? ClassificationId { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public int Priority { get; set; } = 100;
}

public class UpdatePatternRequest : CreatePatternRequest
{
    public bool IsActive { get; set; } = true;
}

public class FindMatchRequest
{
    public string Value { get; set; } = string.Empty;
    public string? PatternType { get; set; }
}

public class TestPatternRequest
{
    public string Regex { get; set; } = string.Empty;
    public string TestValue { get; set; } = string.Empty;
}

public class TestPatternResult
{
    public bool Matches { get; set; }
}
